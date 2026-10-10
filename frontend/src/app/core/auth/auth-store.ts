import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { AuthApi } from '../api/auth-api';
import { SignedIn } from '../api/models';

const STORAGE_KEY = 'session';

/**
 * The signed-in account and its access token, kept across reloads until the token expires. The server decides who
 * may do what; `isAdmin` only decides what the screens offer. Tabs share the stored sign-in: a fresh token another tab
 * stores for the same login (after a password change) is picked up here, and signing out never removes a sign-in
 * another tab stored since.
 */
@Injectable({ providedIn: 'root' })
export class AuthStore {
  private readonly api = inject(AuthApi);
  private readonly session = signal<SignedIn | null>(null);

  readonly account = computed(() => this.session()?.account ?? null);
  readonly isSignedIn = computed(() => this.session() !== null);
  readonly isAdmin = computed(() => this.session()?.account.isAdmin ?? false);

  constructor() {
    window.addEventListener('storage', (event) => {
      if (event.key === STORAGE_KEY) {
        this.adoptStored();
      }
    });
  }

  /** The token to send, or `null` when nobody is signed in or the sign-in has expired. */
  token(): string | null {
    const session = this.session();
    if (!session) {
      return null;
    }
    if (isExpired(session)) {
      this.signOut();
      return null;
    }
    return session.accessToken;
  }

  /** Picks up the stored sign-in before the app renders and refreshes the account (admin rights may have changed). */
  async restore(): Promise<void> {
    const stored = read();
    if (!stored || isExpired(stored)) {
      this.signOut();
      return;
    }

    this.session.set(stored);
    try {
      const account = await firstValueFrom(this.api.me());
      this.keep({ ...stored, account });
    } catch (error) {
      if (error instanceof HttpErrorResponse && error.status === 401) {
        this.signOut();
      }
      // Otherwise the server is unreachable: keep the session, the screens report the outage.
    }
  }

  async signIn(name: string, password: string): Promise<void> {
    this.keep(await firstValueFrom(this.api.signIn(name, password)));
  }

  /** Creates the login and stays signed in to it. */
  async createAccount(name: string, password: string): Promise<void> {
    this.keep(await firstValueFrom(this.api.createAccount(name, password)));
  }

  /**
   * Changes the password and keeps the fresh token, since the change ends the token in use. A 401 means this sign-in
   * had already ended (the password was changed elsewhere), so the session goes; `authInterceptor` leaves the
   * `/api/auth/` calls to their callers.
   */
  async changePassword(currentPassword: string, newPassword: string): Promise<void> {
    try {
      this.keep(await firstValueFrom(this.api.changePassword(currentPassword, newPassword)));
    } catch (error) {
      if (error instanceof HttpErrorResponse && error.status === 401) {
        this.signOut();
      }
      throw error;
    }
  }

  signOut(): void {
    const mine = this.session()?.accessToken;
    this.session.set(null);
    try {
      // Another tab may have stored a newer sign-in (a password change there ends this tab's token); keep it.
      const stored = read();
      if (!stored || !mine || stored.accessToken === mine || isExpired(stored)) {
        localStorage.removeItem(STORAGE_KEY);
      }
    } catch {
      // Storage can be blocked; the session then only lived in memory.
    }
  }

  /** Takes over a sign-in another tab stored for the login this tab is signed in to; another login is left alone. */
  private adoptStored(): void {
    const current = this.session();
    const stored = read();
    if (current && stored && !isExpired(stored) && stored.account.id === current.account.id) {
      this.session.set(stored);
    }
  }

  private keep(session: SignedIn): void {
    this.session.set(session);
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(session));
    } catch {
      // Storage can be blocked; the sign-in then lasts only for this visit.
    }
  }
}

function isExpired(session: SignedIn): boolean {
  return Date.parse(session.expiresAt) <= Date.now();
}

function read(): SignedIn | null {
  try {
    const value = localStorage.getItem(STORAGE_KEY);
    const parsed = value ? (JSON.parse(value) as Partial<SignedIn>) : null;
    return parsed?.accessToken && parsed.expiresAt && parsed.account ? (parsed as SignedIn) : null;
  } catch {
    return null;
  }
}
