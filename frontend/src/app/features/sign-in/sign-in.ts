import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  computed,
  effect,
  inject,
  input,
  signal,
  viewChild,
} from '@angular/core';
import { Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthApi } from '../../core/api/auth-api';
import { IdentityProvider, SignInProvider } from '../../core/api/models';
import { toProblem } from '../../core/api/problem';
import { AuthStore } from '../../core/auth/auth-store';
import { ProviderSdks } from '../../core/auth/provider-sdks';
import { LanguageService, Message } from '../../core/i18n/language';
import { MessagePipe } from '../../core/i18n/message.pipe';

/** Where to go after signing in: only a path inside this app, never back here. */
export function safeReturnUrl(url: string | undefined): string {
  return url && url.startsWith('/') && !url.startsWith('//') && !url.startsWith('/sign-in') ? url : '/';
}

@Component({
  selector: 'app-sign-in',
  imports: [TranslatePipe, MessagePipe],
  templateUrl: './sign-in.html',
  styleUrl: './sign-in.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SignIn {
  private readonly api = inject(AuthApi);
  private readonly auth = inject(AuthStore);
  private readonly sdks = inject(ProviderSdks);
  private readonly router = inject(Router);
  private readonly i18n = inject(LanguageService);

  /** Query parameter: the screen that sent the user here. */
  readonly returnUrl = input<string>();

  protected readonly providers = signal<SignInProvider[] | null>(null);
  protected readonly google = computed(() => this.find('Google'));
  protected readonly facebook = computed(() => this.find('Facebook'));
  protected readonly facebookReady = signal(false);
  protected readonly busy = signal(false);
  protected readonly error = signal<Message | null>(null);

  private readonly googleButton = viewChild<ElementRef<HTMLElement>>('googleButton');

  constructor() {
    this.api.getProviders().subscribe({
      next: (providers) => this.providers.set(providers),
      error: (error) => this.error.set(toProblem(error).message),
    });

    // Google draws its own button; it is drawn again in the new language after a switch.
    effect(() => {
      const google = this.google();
      const element = this.googleButton()?.nativeElement;
      const locale = this.i18n.language();
      if (google && element) {
        this.sdks
          .renderGoogleButton(element, google.clientId, locale, (token) => void this.complete('Google', token))
          .catch(() => this.error.set({ key: 'signIn.sdkFailed', params: { provider: 'Google' } }));
      }
    });

    // Facebook's popup must open straight from the click, so its SDK is ready before then.
    effect(() => {
      const facebook = this.facebook();
      if (facebook) {
        this.sdks
          .prepareFacebook(facebook.clientId)
          .then(() => this.facebookReady.set(true))
          .catch(() => this.error.set({ key: 'signIn.sdkFailed', params: { provider: 'Facebook' } }));
      }
    });
  }

  protected async signInWithFacebook(): Promise<void> {
    if (this.busy() || !this.facebookReady()) {
      return;
    }
    const token = await this.sdks.facebookLogin();
    if (token) {
      await this.complete('Facebook', token);
    }
  }

  private async complete(provider: IdentityProvider, token: string): Promise<void> {
    this.busy.set(true);
    this.error.set(null);
    try {
      await this.auth.signIn(provider, token);
      await this.router.navigateByUrl(safeReturnUrl(this.returnUrl()));
    } catch (error) {
      this.error.set(toProblem(error).message);
    } finally {
      this.busy.set(false);
    }
  }

  private find(provider: IdentityProvider): SignInProvider | null {
    return this.providers()?.find((p) => p.provider === provider) ?? null;
  }
}
