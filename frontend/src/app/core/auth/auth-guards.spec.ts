import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree, provideRouter } from '@angular/router';
import { adminGuard, notAdminGuard, signedInGuard, signedOutGuard } from './auth-guards';
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

  it('sends a signed-in account that is not an admin back to the settings', async () => {
    await signedInAs(TestBed.inject(AuthStore), http);

    expect(serialize(run(adminGuard))).toBe('/settings');
  });

  it('lets an admin through to the admin screens', async () => {
    await signedInAs(TestBed.inject(AuthStore), http, { isAdmin: true });

    expect(run(adminGuard)).toBe(true);
  });

  it('sends an admin away from the password tab, since the configuration sets their password', async () => {
    await signedInAs(TestBed.inject(AuthStore), http, { isAdmin: true });

    expect(serialize(run(notAdminGuard))).toBe('/settings');
  });

  it('lets a login that is not an admin through to the password tab', async () => {
    await signedInAs(TestBed.inject(AuthStore), http);

    expect(run(notAdminGuard)).toBe(true);
  });
});
