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
import { Limit } from '../../core/api/models';
import { toProblem } from '../../core/api/problem';
import { AuthStore } from '../../core/auth/auth-store';
import { Message } from '../../core/i18n/language';
import { MessagePipe } from '../../core/i18n/message.pipe';
import { GameRulesStore } from '../../core/rules/game-rules-store';

const NOTHING: Limit = { min: 0, max: 0 };

/** Where to go after signing in: only a path inside this app, never back here. */
export function safeReturnUrl(url: string | undefined): string {
  return url && url.startsWith('/') && !url.startsWith('//') && !url.startsWith('/sign-in') ? url : '/';
}

/**
 * `form`: name and password. `ask`: the name is unknown, so the user is asked whether to create it. `create`: a small
 * window with the name (prefilled, still editable), the password and its repetition.
 */
type Step = 'form' | 'ask' | 'create';

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
  private readonly rules = inject(GameRulesStore).rules;

  /** Query parameter: the screen that sent the user here. */
  readonly returnUrl = input<string>();

  protected readonly nameLimit = computed(() => this.rules()?.loginName ?? NOTHING);
  protected readonly passwordLimit = computed(() => this.rules()?.password ?? NOTHING);

  protected readonly step = signal<Step>('form');
  protected readonly name = signal('');
  protected readonly password = signal('');
  protected readonly newName = signal('');
  protected readonly newPassword = signal('');
  protected readonly repeatPassword = signal('');
  protected readonly busy = signal(false);
  protected readonly error = signal<Message | null>(null);
  protected readonly dialogError = signal<Message | null>(null);

  /** The name the server did not know, as typed. */
  protected readonly unknownName = signal('');

  private readonly askYes = viewChild<ElementRef<HTMLButtonElement>>('askYes');
  private readonly createName = viewChild<ElementRef<HTMLInputElement>>('createName');

  constructor() {
    // Each dialog takes the focus when it opens, so the keyboard lands inside it.
    afterRenderEffect(() => {
      const step = this.step();
      if (step === 'ask') {
        this.askYes()?.nativeElement.focus();
      } else if (step === 'create') {
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
      const problem = toProblem(error);
      if (problem.code === 'Auth.UnknownLogin') {
        this.unknownName.set(name);
        this.step.set('ask');
      } else {
        this.error.set(problem.message);
      }
    } finally {
      this.busy.set(false);
    }
  }

  /** "Yes, create it": the name comes along, the password is chosen (and repeated) in the next window. */
  protected startCreating(): void {
    this.newName.set(this.unknownName());
    this.newPassword.set('');
    this.repeatPassword.set('');
    this.dialogError.set(null);
    this.step.set('create');
  }

  protected async create(): Promise<void> {
    if (this.busy()) {
      return;
    }

    const problem = this.validateNewLogin();
    if (problem) {
      this.dialogError.set(problem);
      return;
    }

    this.busy.set(true);
    this.dialogError.set(null);
    try {
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

  private validateNewLogin(): Message | null {
    const name = this.newName().trim();
    const names = this.nameLimit();
    if (name.length < names.min || name.length > names.max) {
      return { key: 'signIn.errors.nameLength', params: names };
    }

    const passwords = this.passwordLimit();
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
