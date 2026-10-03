import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Account, IdentityProvider, SignInProvider, SignedIn } from './models';

@Injectable({ providedIn: 'root' })
export class AuthApi {
  private readonly http = inject(HttpClient);

  /** The configured providers; readable before signing in. */
  getProviders(): Observable<SignInProvider[]> {
    return this.http.get<SignInProvider[]>('/api/auth/providers');
  }

  /** `token` is what the provider's SDK returned: Google's ID token (`credential`) or Facebook's access token. */
  signIn(provider: IdentityProvider, token: string): Observable<SignedIn> {
    return this.http.post<SignedIn>('/api/auth/sign-in', { provider, token });
  }

  me(): Observable<Account> {
    return this.http.get<Account>('/api/auth/me');
  }
}
