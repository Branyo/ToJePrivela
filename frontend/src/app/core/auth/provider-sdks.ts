import { DOCUMENT, Injectable, inject } from '@angular/core';

/** The parts of Google Identity Services this app uses. */
interface GoogleIdentity {
  accounts: {
    id: {
      initialize(config: { client_id: string; callback: (response: { credential?: string }) => void; ux_mode?: 'popup' }): void;
      renderButton(
        parent: HTMLElement,
        options: { theme?: string; size?: string; shape?: string; text?: string; width?: number; locale?: string },
      ): void;
      disableAutoSelect(): void;
    };
  };
}

/** The parts of the Facebook JS SDK this app uses. */
interface FacebookSdk {
  init(config: { appId: string; version: string; cookie?: boolean; xfbml?: boolean }): void;
  login(
    callback: (response: { status: string; authResponse?: { accessToken: string } | null }) => void,
    options: { scope: string },
  ): void;
}

declare global {
  interface Window {
    google?: GoogleIdentity;
    FB?: FacebookSdk;
  }
}

const GOOGLE_SDK = 'https://accounts.google.com/gsi/client';
const FACEBOOK_SDK = 'https://connect.facebook.net/en_US/sdk.js';
const FACEBOOK_API_VERSION = 'v23.0';

/**
 * Loads the providers' own browser SDKs on demand, so the app pulls in third-party scripts only on the sign-in screen.
 * Each SDK signs the user in at the provider and hands back a token; the backend verifies that token.
 */
@Injectable({ providedIn: 'root' })
export class ProviderSdks {
  private readonly document = inject(DOCUMENT);
  private readonly scripts = new Map<string, Promise<void>>();
  private facebookAppId: string | null = null;

  /** Renders Google's own button into `parent`; `onToken` gets the ID token once the user picked an account. */
  async renderGoogleButton(
    parent: HTMLElement,
    clientId: string,
    locale: string,
    onToken: (idToken: string) => void,
  ): Promise<void> {
    await this.load(GOOGLE_SDK);
    const google = this.window().google!;
    google.accounts.id.initialize({
      client_id: clientId,
      ux_mode: 'popup',
      callback: (response) => {
        if (response.credential) {
          onToken(response.credential);
        }
      },
    });
    google.accounts.id.renderButton(parent, { theme: 'outline', size: 'large', shape: 'pill', text: 'signin_with', locale });
  }

  /** Loads and initialises the SDK ahead of the click, so `facebookLogin` can open its popup straight from it. */
  async prepareFacebook(appId: string): Promise<void> {
    await this.load(FACEBOOK_SDK);
    if (this.facebookAppId !== appId) {
      this.window().FB!.init({ appId, version: FACEBOOK_API_VERSION, cookie: false, xfbml: false });
      this.facebookAppId = appId;
    }
  }

  /** Opens Facebook's login popup; must run in the click handler. Resolves `null` when the user cancels. */
  facebookLogin(): Promise<string | null> {
    const facebook = this.window().FB;
    if (!facebook || this.facebookAppId === null) {
      return Promise.resolve(null);
    }
    return new Promise((resolve) =>
      facebook.login((response) => resolve(response.authResponse?.accessToken ?? null), { scope: 'public_profile,email' }),
    );
  }

  /** Stops Google from signing the same account in again automatically. */
  forgetGoogleChoice(): void {
    this.window().google?.accounts.id.disableAutoSelect();
  }

  private load(src: string): Promise<void> {
    let loading = this.scripts.get(src);
    if (!loading) {
      loading = new Promise<void>((resolve, reject) => {
        const script = this.document.createElement('script');
        script.src = src;
        script.async = true;
        script.defer = true;
        script.onload = () => resolve();
        script.onerror = () => {
          this.scripts.delete(src);
          script.remove();
          reject(new Error(`Could not load ${src}`));
        };
        this.document.head.appendChild(script);
      });
      this.scripts.set(src, loading);
    }
    return loading;
  }

  private window(): Window {
    return this.document.defaultView!;
  }
}
