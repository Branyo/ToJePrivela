import { ChangeDetectionStrategy, Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { forkJoin } from 'rxjs';
import { CategoriesApi } from '../../core/api/categories-api';
import { GameRules, Limit, QuestionCategory } from '../../core/api/models';
import { toProblem } from '../../core/api/problem';
import { QuestionsApi } from '../../core/api/questions-api';
import { LanguageCode, LanguageService, Message, compareNames } from '../../core/i18n/language';
import { MessagePipe } from '../../core/i18n/message.pipe';
import { GameRulesStore } from '../../core/rules/game-rules-store';

/** The generator exists to make questions, so it asks for at least one (the API alone also allows 0). */
const GENERATOR_MIN_QUESTIONS = 1;
/** How many AI questions a new category asks for, unless the admin changes it. */
const NEW_CATEGORY_COUNT = 100;
/** How many more AI questions an existing category asks for, unless the admin changes it. */
const MORE_COUNT = 20;

interface CategoryRow {
  category: QuestionCategory;
  total: number;
  ai: number;
}

/** What waits for a second tap before it runs. */
type PendingDelete = { id: number; what: 'ai' | 'category' };

/**
 * Admins manage the questions every login plays with: categories and their AI questions. Shown to admins only; the API
 * refuses everyone else anyway.
 */
@Component({
  selector: 'app-ai-questions-section',
  imports: [TranslatePipe, MessagePipe],
  templateUrl: './ai-questions-section.html',
  styleUrl: './ai-questions-section.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AiQuestionsSection {
  private readonly categoriesApi = inject(CategoriesApi);
  private readonly questionsApi = inject(QuestionsApi);
  protected readonly i18n = inject(LanguageService);
  private readonly rulesStore = inject(GameRulesStore);

  /** `null` while the backend's rules are missing; the inputs then take anything and the actions say so. */
  protected readonly nameLimit = computed(() => this.rulesStore.rules()?.categoryName ?? null);
  protected readonly countLimit = computed(() => countLimitOf(this.rulesStore.rules()));

  private readonly categories = signal<QuestionCategory[] | null>(null);
  private readonly counts = signal<ReadonlyMap<number, { total: number; ai: number }>>(new Map());

  protected readonly newName = signal('');
  /** The language the new name is typed in; the AI translates it to the other one. */
  protected readonly newNameLanguage = signal<LanguageCode>(this.i18n.language());
  protected readonly newCount = signal(NEW_CATEGORY_COUNT);
  /** Per category: how many AI questions to add. */
  protected readonly moreCounts = signal<ReadonlyMap<number, number>>(new Map());

  /** `'new'` while a new category is generated, else the id of the category being worked on. */
  protected readonly working = signal<number | 'new' | null>(null);
  protected readonly pending = signal<PendingDelete | null>(null);
  protected readonly notice = signal<Message | null>(null);
  protected readonly error = signal<Message | null>(null);

  protected readonly loaded = computed(() => this.categories() !== null);

  protected readonly rows = computed<CategoryRow[]>(() => {
    const locale = this.i18n.locale();
    const counts = this.counts();
    return [...(this.categories() ?? [])]
      .sort((a, b) => compareNames(a.name, b.name, locale))
      .map((category) => ({ category, total: counts.get(category.id)?.total ?? 0, ai: counts.get(category.id)?.ai ?? 0 }));
  });

  constructor() {
    void this.rulesStore.ensureLoaded();
    // Names arrive in the shown language, so they are fetched again after a switch.
    effect(() => {
      this.i18n.language();
      untracked(() => this.load());
    });
  }

  /** The category's name in the language that is not shown, so admins can check the AI's translation. */
  protected otherName(category: QuestionCategory): string {
    return this.i18n.language() === 'sk' ? category.nameEn : category.nameSk;
  }

  protected async create(): Promise<void> {
    const rules = this.rulesStore.rules() ?? (await this.reloadRules());
    if (!rules) {
      return;
    }
    const name = this.newName().trim();
    const nameLimit = rules.categoryName;
    if (name.length < nameLimit.min || name.length > nameLimit.max) {
      this.error.set({ key: 'setup.errors.categoryNameLength', params: { min: nameLimit.min, max: nameLimit.max } });
      return;
    }
    const count = this.newCount();
    if (!this.isValidCount(count, rules)) {
      return;
    }

    this.start('new');
    this.categoriesApi.create(name, this.newNameLanguage(), count).subscribe({
      next: (category) => {
        this.newName.set('');
        this.done({
          key: 'admin.created',
          params: { name: category.name, count: category.questionGeneration.created },
        });
      },
      error: (error) => this.fail(error),
    });
  }

  protected async generateMore(row: CategoryRow): Promise<void> {
    const rules = this.rulesStore.rules() ?? (await this.reloadRules());
    if (!rules) {
      return;
    }
    const count = this.moreCountFor(row.category.id);
    if (!this.isValidCount(count, rules)) {
      return;
    }

    this.start(row.category.id);
    this.categoriesApi.generateAiQuestions(row.category.id, count).subscribe({
      next: (result) =>
        this.done({ key: 'admin.added', params: { name: row.category.name, count: result.summary.created } }),
      error: (error) => this.fail(error),
    });
  }

  protected askToDelete(id: number, what: PendingDelete['what']): void {
    this.error.set(null);
    this.pending.set({ id, what });
  }

  protected isPending(id: number, what: PendingDelete['what']): boolean {
    const pending = this.pending();
    return pending?.id === id && pending.what === what;
  }

  protected cancelDelete(): void {
    this.pending.set(null);
  }

  protected confirmDelete(row: CategoryRow): void {
    const pending = this.pending();
    if (!pending || pending.id !== row.category.id) {
      return;
    }

    this.start(row.category.id);
    if (pending.what === 'ai') {
      this.categoriesApi.deleteAiQuestions(row.category.id).subscribe({
        next: (result) => this.done({ key: 'admin.aiDeleted', params: { name: row.category.name, count: result.deleted } }),
        error: (error) => this.fail(error),
      });
    } else {
      this.categoriesApi.delete(row.category.id).subscribe({
        next: () => this.done({ key: 'admin.categoryDeleted', params: { name: row.category.name } }),
        error: (error) => this.fail(error),
      });
    }
  }

  protected moreCountFor(id: number): number {
    return this.moreCounts().get(id) ?? MORE_COUNT;
  }

  protected onNameInput(event: Event): void {
    this.newName.set((event.target as HTMLInputElement).value);
  }

  protected onCountInput(event: Event): void {
    this.newCount.set(Number((event.target as HTMLInputElement).value));
  }

  protected onMoreCountInput(id: number, event: Event): void {
    const value = Number((event.target as HTMLInputElement).value);
    this.moreCounts.update((counts) => new Map(counts).set(id, value));
  }

  /** Tries to load the missing rules again; `null` after saying that they still cannot be had. */
  private async reloadRules(): Promise<GameRules | null> {
    const rules = await this.rulesStore.ensureLoaded();
    if (!rules) {
      this.error.set({ key: 'errors.rulesUnavailable' });
    }
    return rules;
  }

  private isValidCount(count: number, rules: GameRules): boolean {
    const min = GENERATOR_MIN_QUESTIONS;
    const max = rules.maxAiQuestionCount;
    if (Number.isInteger(count) && count >= min && count <= max) {
      return true;
    }
    this.error.set({ key: 'setup.errors.questionCount', params: { min, max } });
    return false;
  }

  private start(target: number | 'new'): void {
    this.working.set(target);
    this.error.set(null);
    this.notice.set(null);
  }

  private done(notice: Message): void {
    this.working.set(null);
    this.pending.set(null);
    this.notice.set(notice);
    this.load();
  }

  private fail(error: unknown): void {
    this.working.set(null);
    this.error.set(toProblem(error).message);
  }

  private load(): void {
    forkJoin([this.categoriesApi.getAll(), this.questionsApi.getAll()]).subscribe({
      next: ([categories, questions]) => {
        const counts = new Map<number, { total: number; ai: number }>();
        for (const question of questions) {
          const count = counts.get(question.categoryId) ?? { total: 0, ai: 0 };
          counts.set(question.categoryId, {
            total: count.total + 1,
            ai: count.ai + (question.source === 'Ai' ? 1 : 0),
          });
        }
        this.categories.set(categories);
        this.counts.set(counts);
      },
      error: (error) => this.error.set(toProblem(error).message),
    });
  }
}

function countLimitOf(rules: GameRules | null): Limit | null {
  return rules ? { min: GENERATOR_MIN_QUESTIONS, max: rules.maxAiQuestionCount } : null;
}
