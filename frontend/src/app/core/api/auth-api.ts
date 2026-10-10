import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Account, SignedIn } from './models';

@Injectable({ providedIn: 'root' })
export class AuthApi {
  private readonly http = inject(HttpClient);

  /** 404 `Auth.UnknownLogin` when no login has the name, 401 `Auth.WrongPassword` for a wrong password. */
  signIn(name: string, password: string): Observable<SignedIn> {
    return this.http.post<SignedIn>('/api/auth/sign-in', { name, password });
  }

  /** Creates a login (never an admin) and signs it in; 409 `Auth.NameTaken` when the name is taken. */
  createAccount(name: string, password: string): Observable<SignedIn> {
    return this.http.post<SignedIn>('/api/auth/accounts', { name, password });
  }

  /**
   * 400 `Auth.CurrentPasswordWrong` / `Auth.SamePassword`. Ends every other sign-in of the login and answers with a
   * fresh token for this one.
   */
  changePassword(currentPassword: string, newPassword: string): Observable<SignedIn> {
    return this.http.put<SignedIn>('/api/auth/password', { currentPassword, newPassword });
  }

  me(): Observable<Account> {
    return this.http.get<Account>('/api/auth/me');
  }
}
