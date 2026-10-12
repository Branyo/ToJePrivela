import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthStore } from './auth-store';

/** Calls whose 401 is theirs to handle: a refused sign-in or new login, and the stale-session check at startup. */
const HANDLE_THEIR_OWN_401 = ['/api/auth/sign-in', '/api/auth/accounts', '/api/auth/me'];

/**
 * Sends the access token with every API request. A 401 means the sign-in is gone (expired, ended by a password change,
 * or the account no longer exists). When another tab has stored a newer sign-in for the same login, it is taken over
 * and the request is sent once more with it (a 401 comes before the request does anything); otherwise, or when that
 * retry is answered 401 too, the session is dropped and the user is sent to sign in again, coming back to the same
 * screen.
 */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  if (!request.url.startsWith('/api/')) {
    return next(request);
  }

  const auth = inject(AuthStore);
  const router = inject(Router);
  const token = auth.token();

  const isUnauthorized = (error: unknown): boolean =>
    error instanceof HttpErrorResponse && error.status === 401 && !HANDLE_THEIR_OWN_401.includes(request.url);

  /** Whether the sign-in is gone after a 401 to `rejected`; `false` when another tab's newer one was taken over. */
  const signedOut = (rejected: string | null): boolean => {
    if (!rejected) {
      auth.signOut();
    } else if (auth.ended(rejected)) {
      return false;
    }
    void router.navigate(['/sign-in'], { queryParams: { returnUrl: router.url } });
    return true;
  };

  return next(withToken(request, token)).pipe(
    catchError((error: unknown) => {
      if (!isUnauthorized(error) || signedOut(token)) {
        return throwError(() => error);
      }

      // Sent once more only: should the sign-in taken over be ended too, this request fails like any other 401.
      const retryToken = auth.token();
      return next(withToken(request, retryToken)).pipe(
        catchError((retryError: unknown) => {
          if (isUnauthorized(retryError)) {
            signedOut(retryToken);
          }
          return throwError(() => retryError);
        }),
      );
    }),
  );
};

function withToken<T>(request: HttpRequest<T>, token: string | null): HttpRequest<T> {
  return token ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : request;
}
