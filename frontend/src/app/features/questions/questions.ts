import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { EMPTY, Observable, Subject, catchError, forkJoin, map, of, switchMap } from 'rxjs';
import { CategoriesApi } from '../../core/api/categories-api';
import { Question, QuestionCategory, QuestionDraft } from '../../core/api/models';
import { toProblem } from '../../core/api/problem';
import { QuestionsApi } from '../../core/api/questions-api';
import { LanguageService, Message, compareNames } from '../../core/i18n/language';
import { MessagePipe } from '../../core/i18n/message.pipe';
import { GameRulesStore } from '../../core/rules/game-rules-store';
import { QuestionForm } from './question-form';

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

  /** The `category` query parameter. */
  readonly category = input<string>();

  private readonly categories = signal<QuestionCategory[] | null>(null);
  /** The questions of the category picked when they were fetched; `null` until they arrive. */
  private readonly questions = signal<{ categoryId: number | null; list: Question[] } | null>(null);
  /** Each value fetches the categories and the picked category's questions again, dropping a fetch still running. */
  private readonly reloads = new Subject<void>();

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
    return Number.isInteger(id) && id > 0 ? id : null;
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
  protected readonly shown = computed(() =>
    this.questionsLoaded()
      ? [...this.questions()!.list].sort((a, b) => Date.parse(b.createdAt) - Date.parse(a.createdAt) || b.id - a.id)
      : [],
  );

  constructor() {
    void inject(GameRulesStore).ensureLoaded();
    this.reloads
      .pipe(
        switchMap(() => {
          const id = untracked(() => this.requestedId());
          return forkJoin([this.categoriesApi.getAll(), id === null ? of([]) : this.questionsApi.getByCategory(id)]).pipe(
            map(([categories, list]) => ({ categories, questions: { categoryId: id, list } })),
            catchError((error: unknown) => {
              this.error.set(toProblem(error).message);
              return EMPTY;
            }),
          );
        }),
        takeUntilDestroyed(),
      )
      .subscribe(({ categories, questions }) => {
        this.categories.set(categories);
        this.questions.set(questions);
      });
    // Category names arrive in the shown language, so they are fetched again after a switch; the counts and the
    // questions are fetched again for another category.
    effect(() => {
      this.i18n.language();
      this.requestedId();
      untracked(() => this.load());
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
    request.subscribe({
      next: () => {
        this.editing.set(null);
        this.done({ key: question ? 'questionAdmin.updated' : 'questionAdmin.created' });
      },
      error: (error) => {
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
    this.working.set(id);
    this.error.set(null);
    this.questionsApi.delete(id).subscribe({
      next: () => {
        this.pendingDelete.set(null);
        this.done({ key: 'questionAdmin.deleted' });
      },
      error: (error) => {
        this.working.set(null);
        this.error.set(toProblem(error).message);
      },
    });
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

  protected load(): void {
    this.error.set(null);
    this.reloads.next();
  }
}
