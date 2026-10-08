import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  afterRenderEffect,
  computed,
  inject,
  input,
  signal,
  viewChild,
} from '@angular/core';
import { Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { GameRules } from '../../core/api/models';
import { toProblem } from '../../core/api/problem';
import { AuthStore } from '../../core/auth/auth-store';
import { Message } from '../../core/i18n/language';
import { MessagePipe } from '../../core/i18n/message.pipe';
import { GameRulesStore } from '../../core/rules/game-rules-store';

/** Where to go after signing in: only a path inside this app, never back here. */
export function safeReturnUrl(url: string | undefined): string {
  return url && url.startsWith('/') && !url.startsWith('//') && !url.startsWith('/sign-in') ? url : '/';
}

/**
 * `form`: name and password, only ever to sign in. `create`: a small window, opened by the button next to the "new here"
 * hint, with the new login's name, the password and its repetition.
 */
type Step = 'form' | 'create';

@Component({
  selector: 'app-sign-in',
  imports: [TranslatePipe, MessagePipe],
  templateUrl: './sign-in.html',
  styleUrl: './sign-in.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { '(document:keydown.escape)': 'closeDialog()' },
})
export class SignIn {
  private readonly auth = inject(AuthStore);
  private readonly router = inject(Router);
  private readonly rulesStore = inject(GameRulesStore);

  /** Query parameter: the screen that sent the user here. */
  readonly returnUrl = input<string>();

  /** `null` while the backend's rules are missing; the inputs then take any length and creating a login says so. */
  protected readonly nameLimit = computed(() => this.rulesStore.rules()?.loginName ?? null);
  protected readonly passwordLimit = computed(() => this.rulesStore.rules()?.password ?? null);

  protected readonly step = signal<Step>('form');
  protected readonly name = signal('');
  protected readonly password = signal('');
  protected readonly newName = signal('');
  protected readonly newPassword = signal('');
  protected readonly repeatPassword = signal('');
  protected readonly busy = signal(false);
  protected readonly error = signal<Message | null>(null);
  protected readonly dialogError = signal<Message | null>(null);

  private readonly createName = viewChild<ElementRef<HTMLInputElement>>('createName');

  constructor() {
    // The backend may have been down when the app started; try again so the limits are there for a new login.
    void this.rulesStore.ensureLoaded();

    // The window takes the focus when it opens, so the keyboard lands inside it.
    afterRenderEffect(() => {
      if (this.step() === 'create') {
        this.createName()?.nativeElement.focus();
      }
    });
  }

  protected async signIn(): Promise<void> {
    const name = this.name().trim();
    if (!name || this.busy()) {
      return;
    }

    this.busy.set(true);
    this.error.set(null);
    try {
      await this.auth.signIn(name, this.password());
      await this.leave();
    } catch (error) {
      this.error.set(toProblem(error).message);
    } finally {
      this.busy.set(false);
    }
  }

  /** "Create a login": a fresh window; whatever is typed in the sign-in form stays there. */
  protected startCreating(): void {
    if (this.busy()) {
      return;
    }

    this.newName.set('');
    this.newPassword.set('');
    this.repeatPassword.set('');
    this.dialogError.set(null);
    this.error.set(null);
    this.step.set('create');
  }

  protected async create(): Promise<void> {
    if (this.busy()) {
      return;
    }

    this.busy.set(true);
    this.dialogError.set(null);
    try {
      // Only waits when the rules are missing, to try loading them again.
      const rules = this.rulesStore.rules() ?? (await this.rulesStore.ensureLoaded());
      const problem = this.validateNewLogin(rules);
      if (problem) {
        this.dialogError.set(problem);
        return;
      }

      await this.auth.createAccount(this.newName().trim(), this.newPassword());
      await this.leave();
    } catch (error) {
      this.dialogError.set(toProblem(error).message);
    } finally {
      this.busy.set(false);
    }
  }

  protected closeDialog(): void {
    if (this.step() !== 'form' && !this.busy()) {
      this.step.set('form');
    }
  }

  protected value(event: Event): string {
    return (event.target as HTMLInputElement).value;
  }

  private validateNewLogin(rules: GameRules | null): Message | null {
    if (!rules) {
      return { key: 'errors.rulesUnavailable' };
    }

    const name = this.newName().trim();
    const names = rules.loginName;
    if (name.length < names.min || name.length > names.max) {
      return { key: 'signIn.errors.nameLength', params: names };
    }

    const passwords = rules.password;
    if (this.newPassword().length < passwords.min || this.newPassword().length > passwords.max) {
      return { key: 'signIn.errors.passwordLength', params: passwords };
    }

    if (this.newPassword() !== this.repeatPassword()) {
      return { key: 'signIn.errors.passwordsDiffer' };
    }

    return null;
  }

  private async leave(): Promise<void> {
    this.step.set('form');
    await this.router.navigateByUrl(safeReturnUrl(this.returnUrl()));
  }
}
