import { DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { EMPTY, Subject, catchError, switchMap, tap } from 'rxjs';
import { CategoriesApi } from '../core/api/categories-api';
import { QuestionCategory } from '../core/api/models';
import { toProblem } from '../core/api/problem';
import { LanguageService, Message, compareNames } from '../core/i18n/language';

/**
 * The question categories as an admin tab lists them: names in the shown language, with their question counts,
 * sorted by name. `reload()` fetches them again and drops a fetch still running. Create it while the component is
 * created (it needs the injection context); it stops with the component. Fetching again after a language switch is
 * left to the component, which may have more to fetch with it.
 */
export class CategoryList {
  private readonly api = inject(CategoriesApi);
  private readonly i18n = inject(LanguageService);
  private readonly reloads = new Subject<void>();
  private readonly list = signal<QuestionCategory[] | null>(null);

  /** `null` until the first fetch arrives. */
  readonly all = this.list.asReadonly();
  readonly loaded = computed(() => this.list() !== null);
  readonly sorted = computed(() => {
    const locale = this.i18n.locale();
    return [...(this.list() ?? [])].sort((a, b) => compareNames(a.name, b.name, locale));
  });
  /** Why the last fetch failed; cleared by the next one that arrives. */
  readonly error = signal<Message | null>(null);

  constructor() {
    this.reloads
      .pipe(
        switchMap(() =>
          this.api.getAll().pipe(
            tap(() => this.error.set(null)),
            catchError((error: unknown) => {
              this.error.set(toProblem(error).message);
              return EMPTY;
            }),
          ),
        ),
        takeUntilDestroyed(inject(DestroyRef)),
      )
      .subscribe((categories) => this.list.set(categories));
  }

  reload(): void {
    this.reloads.next();
  }
}
