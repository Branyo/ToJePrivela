import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthStore } from './auth-store';

/** Every screen but sign-in needs a signed-in account; the user comes back here once signed in. */
export const signedInGuard: CanActivateFn = (_route, state) =>
  inject(AuthStore).token() !== null
    ? true
    : inject(Router).createUrlTree(['/sign-in'], { queryParams: { returnUrl: state.url } });

/** Signing in again while signed in makes no sense, so the sign-in screen sends you home. */
export const signedOutGuard: CanActivateFn = () =>
  inject(AuthStore).token() === null ? true : inject(Router).createUrlTree(['/']);
