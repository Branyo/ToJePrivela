import { Injectable, signal } from '@angular/core';
import { Observable, Subject } from 'rxjs';
import { toProblem } from '../../core/api/problem';
import { Message } from '../../core/i18n/language';

/**
 * What the categories tab is busy with, kept outside the tab: generating AI questions takes long, and an admin who
 * opens another tab meanwhile still finds the work running, its buttons disabled and its result, on the way back.
 */
@Injectable({ providedIn: 'root' })
export class AiQuestionsStore {
  /** `'new'` while a new category is generated, else the id of the category being worked on. */
  readonly working = signal<number | 'new' | null>(null);
  readonly notice = signal<Message | null>(null);
  readonly error = signal<Message | null>(null);
  /** Emits after every change that went through, so the tab shown then fetches the categories again. */
  readonly finished = new Subject<void>();

  /**
   * Runs one change to completion whether or not the tab still shows; `onSuccess` lets the tab that started it
   * tidy up its own inputs.
   */
  run<T>(target: number | 'new', request: Observable<T>, toNotice: (result: T) => Message, onSuccess?: () => void): void {
    this.working.set(target);
    this.error.set(null);
    this.notice.set(null);
    request.subscribe({
      next: (result) => {
        this.working.set(null);
        this.notice.set(toNotice(result));
        onSuccess?.();
        this.finished.next();
      },
      error: (error: unknown) => {
        this.working.set(null);
        this.error.set(toProblem(error).message);
      },
    });
  }
}
