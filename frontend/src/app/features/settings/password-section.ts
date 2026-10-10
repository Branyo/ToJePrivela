import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, WritableSignal, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { GameRules } from '../../core/api/models';
import { toProblem } from '../../core/api/problem';
import { AuthStore } from '../../core/auth/auth-store';
import { Message } from '../../core/i18n/language';
import { MessagePipe } from '../../core/i18n/message.pipe';
import { GameRulesStore } from '../../core/rules/game-rules-store';
import { newPasswordProblem } from '../../core/rules/new-password';

/**
 * Changing the login's password: the current one, the new one and its repetition. The server ends every other
 * sign-in of the login; this device stays signed in with the fresh token it answers with.
 */
@Component({
  selector: 'app-password-section',
  imports: [TranslatePipe, MessagePipe],
  templateUrl: './password-section.html',
  styleUrl: './password-section.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PasswordSection {
  private readonly auth = inject(AuthStore);
  private readonly rulesStore = inject(GameRulesStore);
  private readonly router = inject(Router);

  /** `null` while the backend's rules are missing; the inputs then take any length and saving says so. */
  protected readonly passwordLimit = computed(() => this.rulesStore.rules()?.password ?? null);

  protected readonly name = computed(() => this.auth.account()?.name ?? '');
  protected readonly currentPassword = signal('');
  protected readonly newPassword = signal('');
  protected readonly repeatPassword = signal('');
  protected readonly busy = signal(false);
  protected readonly error = signal<Message | null>(null);
  protected readonly changed = signal(false);

  protected readonly canSubmit = computed(
    () => !this.busy() && this.currentPassword().length > 0 && this.newPassword().length > 0,
  );

  constructor() {
    void this.rulesStore.ensureLoaded();
  }

  protected async change(): Promise<void> {
    if (!this.canSubmit()) {
      return;
    }

    this.busy.set(true);
    this.error.set(null);
    this.changed.set(false);
    try {
      // Only waits when the rules are missing, to try loading them again.
      const rules = this.rulesStore.rules() ?? (await this.rulesStore.ensureLoaded());
      const problem = this.validate(rules);
      if (problem) {
        this.error.set(problem);
        return;
      }

      await this.auth.changePassword(this.currentPassword(), this.newPassword());
      this.currentPassword.set('');
      this.newPassword.set('');
      this.repeatPassword.set('');
      this.changed.set(true);
    } catch (error) {
      if (error instanceof HttpErrorResponse && error.status === 401) {
        // The sign-in had already ended (`AuthStore` dropped it); sign in again and come back here.
        void this.router.navigate(['/sign-in'], { queryParams: { returnUrl: this.router.url } });
        return;
      }
      this.error.set(toProblem(error).message);
    } finally {
      this.busy.set(false);
    }
  }

  /** Typing again makes an earlier result (the notice or an error) out of date, so it goes. */
  protected edit(field: WritableSignal<string>, event: Event): void {
    field.set((event.target as HTMLInputElement).value);
    this.error.set(null);
    this.changed.set(false);
  }

  private validate(rules: GameRules | null): Message | null {
    if (!rules) {
      return { key: 'errors.rulesUnavailable' };
    }

    const problem = newPasswordProblem(rules.password, this.newPassword(), this.repeatPassword());
    if (problem) {
      return problem;
    }

    if (this.newPassword() === this.currentPassword()) {
      return { key: 'errors.api.Auth.SamePassword' };
    }

    return null;
  }
}
