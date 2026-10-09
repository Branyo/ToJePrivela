import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { Observable, forkJoin } from 'rxjs';
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
  imports: [TranslatePipe, MessagePipe, QuestionForm],
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
  private readonly questions = signal<Question[]>([]);

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

  protected readonly counts = computed(() => {
    const counts = new Map<number, number>();
    for (const question of this.questions()) {
      counts.set(question.categoryId, (counts.get(question.categoryId) ?? 0) + 1);
    }
    return counts;
  });

  /** `null` until a category that exists is picked. */
  protected readonly selected = computed(() => {
    const id = Number(this.category());
    return this.categories()?.find((category) => category.id === id) ?? null;
  });

  /** Equal across reloads of the same category, unlike `selected`. */
  private readonly selectedId = computed(() => this.selected()?.id ?? null);

  /** Newest first, so a question just added shows up on top, where it was written. */
  protected readonly shown = computed(() => {
    const selected = this.selected();
    return selected
      ? this.questions()
          .filter((question) => question.categoryId === selected.id)
          .sort((a, b) => b.createdAt.localeCompare(a.createdAt) || b.id - a.id)
      : [];
  });

  constructor() {
    void inject(GameRulesStore).ensureLoaded();
    // Category names arrive in the shown language, so they are fetched again after a switch.
    effect(() => {
      this.i18n.language();
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
    this.editing.set(null);
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

  private load(): void {
    forkJoin([this.categoriesApi.getAll(), this.questionsApi.getAll()]).subscribe({
      next: ([categories, questions]) => {
        this.categories.set(categories);
        this.questions.set(questions);
      },
      error: (error) => this.error.set(toProblem(error).message),
    });
  }
}
