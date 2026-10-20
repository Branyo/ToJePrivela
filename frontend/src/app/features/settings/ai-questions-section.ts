import { ChangeDetectionStrategy, Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { CategoriesApi, CategoryNames } from '../../core/api/categories-api';
import { GameRules, Limit, QuestionCategory, QuestionImportSummary } from '../../core/api/models';
import { LanguageCode, LanguageService, Message } from '../../core/i18n/language';
import { MessagePipe } from '../../core/i18n/message.pipe';
import { GameRulesStore } from '../../core/rules/game-rules-store';
import { CategoryList } from '../../shared/category-list';
import { AiQuestionsStore } from './ai-questions-store';
import { JsonQuestionsInput } from './json-questions-input';

/** The generator exists to make questions, so it asks for at least one (the API alone also allows 0). */
const GENERATOR_MIN_QUESTIONS = 1;
/** How many AI questions a new category asks for, unless the admin changes it. */
const NEW_CATEGORY_COUNT = 100;
/** How many more AI questions an existing category asks for, unless the admin changes it. */
const MORE_COUNT = 20;

/** Where a new category's first questions come from: none yet, the AI, or imported JSON. */
type QuestionSource = 'none' | 'ai' | 'json';

/** What waits for a second tap before it runs. */
type PendingDelete = { id: number; what: 'ai' | 'category' };

/**
 * Admins manage the questions every login plays with: categories and their AI questions, generated or imported as
 * JSON in the generator's format; each category's questions open in the questions tab. Shown to admins only; the API
 * refuses everyone else anyway.
 */
@Component({
  selector: 'app-ai-questions-section',
  imports: [RouterLink, TranslatePipe, MessagePipe, JsonQuestionsInput],
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

  private readonly categories = new CategoryList();

  /** The new category's name per language; at least one is needed, the AI translates a missing one. */
  protected readonly newNames = { sk: signal(''), en: signal('') } as const;
  /** The name fields in the order shown: the shown language first. */
  protected readonly nameLanguages = computed<readonly LanguageCode[]>(() => (this.i18n.language() === 'en' ? ['en', 'sk'] : ['sk', 'en']));
  /** The language the AI will translate the name into: only while exactly one name is given. */
  protected readonly translatedLanguage = computed<LanguageCode | null>(() => {
    const sk = this.newNames.sk().trim().length > 0;
    const en = this.newNames.en().trim().length > 0;
    return sk === en ? null : sk ? 'en' : 'sk';
  });
  protected readonly questionSources: readonly QuestionSource[] = ['none', 'ai', 'json'];
  protected readonly newSource = signal<QuestionSource>('ai');
  protected readonly newCount = signal(NEW_CATEGORY_COUNT);
  /** The new category's questions as JSON, when they are imported. */
  protected readonly newJson = signal('');
  /** Whether the new category has all it needs to be sent. */
  protected readonly canCreate = computed(
    () =>
      (this.newNames.sk().trim().length > 0 || this.newNames.en().trim().length > 0) &&
      (this.newSource() !== 'json' || this.newJson().trim().length > 0),
  );
  /** Per category: how many AI questions to add. */
  protected readonly moreCounts = signal<ReadonlyMap<number, number>>(new Map());

  /** Kept in the store, so work still running shows again when the admin comes back to this tab. */
  protected readonly working = this.store.working;
  protected readonly pending = signal<PendingDelete | null>(null);
  /** The category whose JSON import form is open, if any. */
  protected readonly importing = signal<number | null>(null);
  protected readonly importText = signal('');
  protected readonly notice = this.store.notice;
  protected readonly details = this.store.details;
  /** What this visit of the tab found wrong with the inputs; it goes with the tab. */
  private readonly localError = signal<Message | null>(null);
  /** The tab's own problems first (inputs, then loading), else why the last change in the store failed. */
  protected readonly error = computed(() => this.localError() ?? this.categories.error() ?? this.store.error());

  protected readonly loaded = this.categories.loaded;
  protected readonly sortedCategories = this.categories.sorted;

  constructor() {
    void this.rulesStore.ensureLoaded();
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
    const names = this.namesToSend(rules.categoryName);
    if (!names) {
      return;
    }

    switch (this.newSource()) {
      case 'none':
        this.createEmpty(names);
        return;
      case 'ai':
        this.createGenerated(names, rules);
        return;
      case 'json':
        this.createImported(names);
        return;
    }
  }

  /** The given names, trimmed; `null` (and the error said) when none is given or one has the wrong length. */
  private namesToSend(limit: Limit): CategoryNames | null {
    const sk = this.newNames.sk().trim();
    const en = this.newNames.en().trim();
    const wrongLength = (name: string) => name.length > 0 && (name.length < limit.min || name.length > limit.max);
    if ((sk.length === 0 && en.length === 0) || wrongLength(sk) || wrongLength(en)) {
      this.localError.set({ key: 'setup.errors.categoryNameLength', params: { min: limit.min, max: limit.max } });
      return null;
    }
    return sk.length === 0 ? { nameEn: en } : en.length === 0 ? { nameSk: sk } : { nameSk: sk, nameEn: en };
  }

  private clearNames(): void {
    this.newNames.sk.set('');
    this.newNames.en.set('');
  }

  private createEmpty(names: CategoryNames): void {
    this.localError.set(null);
    this.store.run(
      'new',
      this.categoriesApi.create(names, 0),
      (category) => ({ key: 'admin.newCategory.createdEmpty', params: { name: category.name } }),
      () => this.clearNames(),
    );
  }

  private createGenerated(names: CategoryNames, rules: GameRules): void {
    const count = this.newCount();
    if (!this.isValidCount(count, rules)) {
      return;
    }

    this.localError.set(null);
    this.store.run(
      'new',
      this.categoriesApi.create(names, count),
      (category) => ({ key: 'admin.created', params: { name: category.name, count: category.questionGeneration.created } }),
      () => this.clearNames(),
    );
  }

  private createImported(names: CategoryNames): void {
    const json = this.newJson();
    if (json.trim().length === 0) {
      this.localError.set({ key: 'admin.import.empty' });
      return;
    }

    this.localError.set(null);
    this.store.run(
      'new',
      this.categoriesApi.create(names, 0, json),
      (category) => ({
        key: 'admin.import.created',
        params: { name: category.name, count: category.questionImport?.imported ?? 0, total: category.questionImport?.received ?? 0 },
      }),
      () => {
        this.clearNames();
        this.newJson.set('');
      },
      (category) => (category.questionImport ? rejectionsOf(category.questionImport) : []),
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

    this.localError.set(null);
    this.store.run(
      category.id,
      this.categoriesApi.generateAiQuestions(category.id, count),
      (result) => ({ key: 'admin.added', params: { name: category.name, count: result.summary.created } }),
    );
  }

  protected openImport(id: number): void {
    this.localError.set(null);
    this.store.error.set(null);
    this.pending.set(null);
    this.importText.set('');
    this.importing.set(id);
  }

  protected closeImport(): void {
    this.importing.set(null);
    this.importText.set('');
  }

  protected onUnreadableFile(): void {
    this.localError.set({ key: 'admin.import.unreadable' });
  }

  protected importQuestions(category: QuestionCategory): void {
    const json = this.importText();
    if (json.trim().length === 0) {
      this.localError.set({ key: 'admin.import.empty' });
      return;
    }

    this.localError.set(null);
    this.store.run(
      category.id,
      this.categoriesApi.importQuestions(category.id, json),
      (result) => ({ key: 'admin.import.done', params: { name: category.name, count: result.imported, total: result.received } }),
      () => this.closeImport(),
      rejectionsOf,
    );
  }

  protected askToDelete(id: number, what: PendingDelete['what']): void {
    this.localError.set(null);
    this.store.error.set(null);
    this.importing.set(null);
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
    this.localError.set(null);
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

  protected onNameInput(language: LanguageCode, event: Event): void {
    this.newNames[language].set((event.target as HTMLInputElement).value);
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
      this.localError.set({ key: 'errors.rulesUnavailable' });
    }
    return rules;
  }

  private isValidCount(count: number, rules: GameRules): boolean {
    const min = GENERATOR_MIN_QUESTIONS;
    const max = rules.maxAiQuestionCount;
    if (Number.isInteger(count) && count >= min && count <= max) {
      return true;
    }
    this.localError.set({ key: 'setup.errors.questionCount', params: { min, max } });
    return false;
  }

  private load(): void {
    this.categories.reload();
  }
}

function rejectionsOf(result: QuestionImportSummary): Message[] {
  return result.rejected.map(({ position, reason }) => ({ key: `admin.import.reasons.${reason}`, params: { position } }));
}

function countLimitOf(rules: GameRules | null): Limit | null {
  return rules ? { min: GENERATOR_MIN_QUESTIONS, max: rules.maxAiQuestionCount } : null;
}
