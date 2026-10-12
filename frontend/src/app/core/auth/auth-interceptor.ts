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
 * and the request is sent once more with it (a 401 comes before the request does anything); otherwise the session is
 * dropped and the user is sent to sign in again, coming back to the same screen.
 */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  if (!request.url.startsWith('/api/')) {
    return next(request);
  }

  const auth = inject(AuthStore);
  const router = inject(Router);
  const token = auth.token();

  return next(withToken(request, token)).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse) || error.status !== 401 || HANDLE_THEIR_OWN_401.includes(request.url)) {
        return throwError(() => error);
      }

      if (!token) {
        auth.signOut();
      } else if (auth.ended(token)) {
        return next(withToken(request, auth.token()));
      }

      void router.navigate(['/sign-in'], { queryParams: { returnUrl: router.url } });
      return throwError(() => error);
    }),
  );
};

function withToken<T>(request: HttpRequest<T>, token: string | null): HttpRequest<T> {
  return token ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : request;
}
