import { ChangeDetectionStrategy, Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { EMPTY, Subject, catchError, switchMap } from 'rxjs';
import { CategoriesApi } from '../../core/api/categories-api';
import { GameRules, Limit, QuestionCategory } from '../../core/api/models';
import { toProblem } from '../../core/api/problem';
import { LanguageCode, LanguageService, compareNames } from '../../core/i18n/language';
import { MessagePipe } from '../../core/i18n/message.pipe';
import { GameRulesStore } from '../../core/rules/game-rules-store';
import { AiQuestionsStore } from './ai-questions-store';

/** The generator exists to make questions, so it asks for at least one (the API alone also allows 0). */
const GENERATOR_MIN_QUESTIONS = 1;
/** How many AI questions a new category asks for, unless the admin changes it. */
const NEW_CATEGORY_COUNT = 100;
/** How many more AI questions an existing category asks for, unless the admin changes it. */
const MORE_COUNT = 20;

/** What waits for a second tap before it runs. */
type PendingDelete = { id: number; what: 'ai' | 'category' };

/**
 * Admins manage the questions every login plays with: categories and their AI questions; each category's questions
 * open in the questions tab. Shown to admins only; the API
 * refuses everyone else anyway.
 */
@Component({
  selector: 'app-ai-questions-section',
  imports: [RouterLink, TranslatePipe, MessagePipe],
  templateUrl: './ai-questions-section.html',
  styleUrl: './ai-questions-section.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AiQuestionsSection {
  private readonly categoriesApi = inject(CategoriesApi);
  protected readonly i18n = inject(LanguageService);
  private readonly rulesStore = inject(GameRulesStore);
  private readonly store = inject(AiQuestionsStore);

  /** `null` while the backend's rules are missing; the inputs then take anything and the actions say so. */
  protected readonly nameLimit = computed(() => this.rulesStore.rules()?.categoryName ?? null);
  protected readonly countLimit = computed(() => countLimitOf(this.rulesStore.rules()));

  private readonly categories = signal<QuestionCategory[] | null>(null);
  /** Each value fetches the categories again, dropping a fetch still running. */
  private readonly reloads = new Subject<void>();

  protected readonly newName = signal('');
  /** The language the new name is typed in; the AI translates it to the other one. */
  protected readonly newNameLanguage = signal<LanguageCode>(this.i18n.language());
  protected readonly newCount = signal(NEW_CATEGORY_COUNT);
  /** Per category: how many AI questions to add. */
  protected readonly moreCounts = signal<ReadonlyMap<number, number>>(new Map());

  /** Kept in the store, so work still running shows again when the admin comes back to this tab. */
  protected readonly working = this.store.working;
  protected readonly pending = signal<PendingDelete | null>(null);
  protected readonly notice = this.store.notice;
  protected readonly error = this.store.error;

  protected readonly loaded = computed(() => this.categories() !== null);

  protected readonly sortedCategories = computed(() => {
    const locale = this.i18n.locale();
    return [...(this.categories() ?? [])].sort((a, b) => compareNames(a.name, b.name, locale));
  });

  constructor() {
    void this.rulesStore.ensureLoaded();
    this.reloads
      .pipe(
        switchMap(() =>
          this.categoriesApi.getAll().pipe(
            catchError((error: unknown) => {
              this.error.set(toProblem(error).message);
              return EMPTY;
            }),
          ),
        ),
        takeUntilDestroyed(),
      )
      .subscribe((categories) => this.categories.set(categories));
    // Counts change with every generation or deletion, even one started before this tab was opened again.
    this.store.finished.pipe(takeUntilDestroyed()).subscribe(() => this.load());
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

    this.store.run(
      'new',
      this.categoriesApi.create(name, this.newNameLanguage(), count),
      (category) => ({ key: 'admin.created', params: { name: category.name, count: category.questionGeneration.created } }),
      () => this.newName.set(''),
    );
  }

  protected async generateMore(category: QuestionCategory): Promise<void> {
    const rules = this.rulesStore.rules() ?? (await this.reloadRules());
    if (!rules) {
      return;
    }
    const count = this.moreCountFor(category.id);
    if (!this.isValidCount(count, rules)) {
      return;
    }

    this.store.run(
      category.id,
      this.categoriesApi.generateAiQuestions(category.id, count),
      (result) => ({ key: 'admin.added', params: { name: category.name, count: result.summary.created } }),
    );
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

  protected confirmDelete(category: QuestionCategory): void {
    const pending = this.pending();
    if (!pending || pending.id !== category.id) {
      return;
    }

    const clearPending = () => this.pending.set(null);
    if (pending.what === 'ai') {
      this.store.run(
        category.id,
        this.categoriesApi.deleteAiQuestions(category.id),
        (result) => ({ key: 'admin.aiDeleted', params: { name: category.name, count: result.deleted } }),
        clearPending,
      );
    } else {
      this.store.run(
        category.id,
        this.categoriesApi.delete(category.id),
        () => ({ key: 'admin.categoryDeleted', params: { name: category.name } }),
        clearPending,
      );
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

  private load(): void {
    this.reloads.next();
  }
}

function countLimitOf(rules: GameRules | null): Limit | null {
  return rules ? { min: GENERATOR_MIN_QUESTIONS, max: rules.maxAiQuestionCount } : null;
}
