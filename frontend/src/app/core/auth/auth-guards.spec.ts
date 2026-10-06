import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree, provideRouter } from '@angular/router';
import { signedInGuard, signedOutGuard } from './auth-guards';
import { AuthStore } from './auth-store';
import { signedInAs } from './testing';

describe('auth guards', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])] });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => localStorage.clear());

  const run = (guard: typeof signedInGuard, url = '/games/4') =>
    TestBed.runInInjectionContext(() =>
      guard({} as ActivatedRouteSnapshot, { url } as RouterStateSnapshot),
    ) as boolean | UrlTree;

  const serialize = (result: boolean | UrlTree) =>
    typeof result === 'boolean' ? result : TestBed.inject(Router).serializeUrl(result);

  it('sends a visitor to sign in and back to where they were going', () => {
    expect(serialize(run(signedInGuard))).toBe('/sign-in?returnUrl=%2Fgames%2F4');
  });

  it('lets a signed-in account through', async () => {
    await signedInAs(TestBed.inject(AuthStore), http);

    expect(run(signedInGuard)).toBe(true);
    expect(serialize(run(signedOutGuard))).toBe('/');
  });
});
