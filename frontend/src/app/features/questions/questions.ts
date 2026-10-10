import { ChangeDetectionStrategy, Component, DestroyRef, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { EMPTY, Observable, Subject, catchError, map, of, skip, switchMap } from 'rxjs';
import { CategoriesApi } from '../../core/api/categories-api';
import { Question, QuestionCategory, QuestionDraft } from '../../core/api/models';
import { toProblem } from '../../core/api/problem';
import { QuestionsApi } from '../../core/api/questions-api';
import { LanguageService, Message, compareNames } from '../../core/i18n/language';
import { MessagePipe } from '../../core/i18n/message.pipe';
import { GameRulesStore } from '../../core/rules/game-rules-store';
import { QuestionForm } from './question-form';

/** The largest id the API takes (a C# `int`); a longer one in the URL is no id at all. */
const MAX_ID = 2_147_483_647;

/** A question with its `createdAt` parsed once, for sorting. */
type Timed = { question: Question; time: number };

/**
 * The settings' questions tab: admins go through the categories and their questions: both texts are shown, the answer only on request. They add
 * manual questions, edit any (an AI question becomes a manual one) and delete any. The picked category lives in the
 * URL (`?category=`), so a reload or the back button keeps it.
 */
@Component({
  selector: 'app-questions',
  imports: [RouterLink, TranslatePipe, MessagePipe, QuestionForm],
  templateUrl: './questions.html',
  styleUrl: './questions.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Questions {
  private readonly router = inject(Router);
  private readonly categoriesApi = inject(CategoriesApi);
  private readonly questionsApi = inject(QuestionsApi);
  private readonly i18n = inject(LanguageService);
  private readonly destroyRef = inject(DestroyRef);

  /** The `category` query parameter. */
  readonly category = input<string>();

  private readonly categories = signal<QuestionCategory[] | null>(null);
  /** The questions of the category picked when they were fetched, newest first; `null` until they arrive. */
  private readonly questions = signal<{ categoryId: number | null; list: Question[] } | null>(null);
  /** Each value fetches the categories (names and counts) again, dropping a fetch still running. */
  private readonly categoryReloads = new Subject<void>();
  /** Each value fetches the picked category's questions again, dropping a fetch still running. */
  private readonly questionReloads = new Subject<void>();

  /** `'new'` while a new question is written, else the id of the question being edited. */
  protected readonly editing = signal<number | 'new' | null>(null);
  /** The ids whose answers are shown. */
  protected readonly revealed = signal<ReadonlySet<number>>(new Set());
  protected readonly pendingDelete = signal<number | null>(null);
  /** `'new'` while a new question is saved, else the id of the question being saved or deleted. */
  protected readonly working = signal<number | 'new' | null>(null);
  protected readonly formProblem = signal<Message | null>(null);
  protected readonly notice = signal<Message | null>(null);
  protected readonly error = signal<Message | null>(null);

  protected readonly loaded = computed(() => this.categories() !== null);

  protected readonly sortedCategories = computed(() => {
    const locale = this.i18n.locale();
    return [...(this.categories() ?? [])].sort((a, b) => compareNames(a.name, b.name, locale));
  });

  /** The id in the URL, or `null` when it cannot be one. */
  private readonly requestedId = computed(() => {
    const id = Number(this.category());
    return Number.isInteger(id) && id > 0 && id <= MAX_ID ? id : null;
  });

  /** `null` until a category that exists is picked. */
  protected readonly selected = computed(() => {
    const id = this.requestedId();
    return this.categories()?.find((category) => category.id === id) ?? null;
  });

  /** Equal across reloads of the same category, unlike `selected`. */
  private readonly selectedId = computed(() => this.selected()?.id ?? null);

  /** `false` while the picked category's questions are still on their way. */
  protected readonly questionsLoaded = computed(() => this.questions()?.categoryId === this.selectedId());

  /** Newest first, so a question just added shows up on top, where it was written. */
  protected readonly shown = computed(() => (this.questionsLoaded() ? this.questions()!.list : []));

  constructor() {
    void inject(GameRulesStore).ensureLoaded();
    // The two fetches fail on their own: categories still arrive, with chips to pick another category, when the
    // questions of the one in the URL cannot be had.
    this.categoryReloads
      .pipe(
        switchMap(() => this.categoriesApi.getAll().pipe(catchError((error: unknown) => this.fail(error)))),
        takeUntilDestroyed(),
      )
      .subscribe((categories) => this.categories.set(categories));
    this.questionReloads
      .pipe(
        switchMap(() => {
          const id = untracked(() => this.requestedId());
          const list = id === null ? of([]) : this.questionsApi.getByCategory(id).pipe(map(newestFirst));
          return list.pipe(
            map((questions) => ({ categoryId: id, list: questions })),
            catchError((error: unknown) => this.fail(error)),
          );
        }),
        takeUntilDestroyed(),
      )
      .subscribe((questions) => this.questions.set(questions));
    // Names and texts arrive in the shown language, so both are fetched again after a switch.
    effect(() => {
      this.i18n.language();
      untracked(() => this.load());
    });
    // Another category needs only its own questions; the categories and their counts stay as they are.
    // The first id is fetched by `load()` above.
    toObservable(this.requestedId)
      .pipe(skip(1), takeUntilDestroyed())
      .subscribe(() => {
        this.error.set(null);
        this.questionReloads.next();
      });
    // Another category starts with nothing open.
    effect(() => {
      this.selectedId();
      untracked(() => this.reset());
    });
  }

  protected pick(category: QuestionCategory): void {
    void this.router.navigate([], { queryParams: { category: category.id } });
  }

  protected isRevealed(id: number): boolean {
    return this.revealed().has(id);
  }

  protected toggleAnswer(id: number): void {
    this.revealed.update((ids) => {
      const next = new Set(ids);
      if (!next.delete(id)) {
        next.add(id);
      }
      return next;
    });
  }

  protected edit(target: number | 'new'): void {
    this.pendingDelete.set(null);
    this.formProblem.set(null);
    this.notice.set(null);
    this.editing.set(target);
  }

  protected cancelEdit(): void {
    this.editing.set(null);
    this.formProblem.set(null);
  }

  protected save(draft: QuestionDraft, question: Question | null): void {
    const category = this.selected();
    if (!category) {
      return;
    }

    this.working.set(question?.id ?? 'new');
    this.formProblem.set(null);
    const request: Observable<unknown> = question
      ? this.questionsApi.update(question.id, category.id, draft)
      : this.questionsApi.create(category.id, draft);
    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        if (this.left(category.id)) {
          return;
        }
        this.editing.set(null);
        this.done({ key: question ? 'questionAdmin.updated' : 'questionAdmin.created' });
      },
      error: (error) => {
        if (this.left(category.id)) {
          return;
        }
        this.working.set(null);
        this.formProblem.set(toProblem(error).message);
      },
    });
  }

  protected askToDelete(id: number): void {
    this.error.set(null);
    this.pendingDelete.set(id);
  }

  protected confirmDelete(id: number): void {
    const categoryId = this.selectedId();
    this.working.set(id);
    this.error.set(null);
    this.questionsApi.delete(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        if (this.left(categoryId)) {
          return;
        }
        this.pendingDelete.set(null);
        this.done({ key: 'questionAdmin.deleted' });
      },
      error: (error) => {
        if (this.left(categoryId)) {
          return;
        }
        // The list may be out of date (say, the question was deleted elsewhere), so it is fetched again; `load()`
        // clears the error, so the error is set after it.
        this.working.set(null);
        this.pendingDelete.set(null);
        this.load();
        this.error.set(toProblem(error).message);
      },
    });
  }

  /**
   * Whether another category was picked while a save or delete was on its way: its outcome then belongs to a list
   * no longer shown, which has already been reset and fetched on its own.
   */
  private left(categoryId: number | null): boolean {
    if (this.selectedId() === categoryId) {
      return false;
    }
    this.working.set(null);
    return true;
  }

  private done(notice: Message): void {
    this.working.set(null);
    this.error.set(null);
    this.notice.set(notice);
    this.load();
  }

  private reset(): void {
    this.editing.set(null);
    this.revealed.set(new Set());
    this.pendingDelete.set(null);
    this.formProblem.set(null);
    this.notice.set(null);
  }

  /** Fetches the categories and the picked category's questions again: their counts change with every save. */
  protected load(): void {
    this.error.set(null);
    this.categoryReloads.next();
    this.questionReloads.next();
  }

  private fail(error: unknown): Observable<never> {
    this.error.set(toProblem(error).message);
    return EMPTY;
  }
}

function newestFirst(questions: Question[]): Question[] {
  return questions
    .map((question): Timed => ({ question, time: Date.parse(question.createdAt) }))
    .sort((a, b) => b.time - a.time || b.question.id - a.question.id)
    .map(({ question }) => question);
}
