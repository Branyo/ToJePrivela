import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { forkJoin } from 'rxjs';
import { CategoriesApi } from '../../core/api/categories-api';
import { Limit, QuestionCategory } from '../../core/api/models';
import { toProblem } from '../../core/api/problem';
import { QuestionsApi } from '../../core/api/questions-api';
import { LanguageService, Message, compareNames } from '../../core/i18n/language';
import { MessagePipe } from '../../core/i18n/message.pipe';
import { GameRulesStore } from '../../core/rules/game-rules-store';

/** The generator exists to make questions, so it asks for at least one (the API alone also allows 0). */
const GENERATOR_MIN_QUESTIONS = 1;
const DEFAULT_COUNT = 20;
const NOTHING: Limit = { min: 0, max: 0 };

interface CategoryRow {
  category: QuestionCategory;
  total: number;
  ai: number;
}

/** What waits for a second tap before it runs. */
type PendingDelete = { id: number; what: 'ai' | 'category' };

/** Admins manage the questions every login plays with: categories and their AI questions. */
@Component({
  selector: 'app-admin',
  imports: [TranslatePipe, MessagePipe],
  templateUrl: './admin.html',
  styleUrl: './admin.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Admin {
  private readonly categoriesApi = inject(CategoriesApi);
  private readonly questionsApi = inject(QuestionsApi);
  private readonly i18n = inject(LanguageService);
  private readonly rules = inject(GameRulesStore).rules;

  protected readonly nameLimit = computed(() => this.rules()?.categoryName ?? NOTHING);
  protected readonly countLimit = computed<Limit>(() => ({
    min: GENERATOR_MIN_QUESTIONS,
    max: this.rules()?.maxAiQuestionCount ?? 0,
  }));

  private readonly categories = signal<QuestionCategory[] | null>(null);
  private readonly counts = signal<ReadonlyMap<number, { total: number; ai: number }>>(new Map());

  protected readonly newName = signal('');
  protected readonly newCount = signal(DEFAULT_COUNT);
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
    this.load();
  }

  protected create(): void {
    const name = this.newName().trim();
    const nameLimit = this.nameLimit();
    if (name.length < nameLimit.min || name.length > nameLimit.max) {
      this.error.set({ key: 'setup.errors.categoryNameLength', params: { min: nameLimit.min, max: nameLimit.max } });
      return;
    }
    const count = this.newCount();
    if (!this.isValidCount(count)) {
      return;
    }

    this.start('new');
    this.categoriesApi.create(name, count).subscribe({
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

  protected generateMore(row: CategoryRow): void {
    const count = this.moreCountFor(row.category.id);
    if (!this.isValidCount(count)) {
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
    return this.moreCounts().get(id) ?? DEFAULT_COUNT;
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

  private isValidCount(count: number): boolean {
    const { min, max } = this.countLimit();
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
