import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { routes } from '../../app.routes';
import { AuthStore } from '../../core/auth/auth-store';
import { signedInAs } from '../../core/auth/testing';
import { GameRulesStore } from '../../core/rules/game-rules-store';
import { TEST_RULES } from '../../core/rules/testing';

describe('Settings', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter(routes), provideTranslateService()],
    });
    http = TestBed.inject(HttpTestingController);
    TestBed.inject(GameRulesStore).rules.set(TEST_RULES);
  });

  afterEach(() => {
    http.verify();
    localStorage.clear();
  });

  async function open(url: string, isAdmin: boolean) {
    await signedInAs(TestBed.inject(AuthStore), http, { name: 'Brano', isAdmin });
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl(url);
    return { harness, page: harness.routeNativeElement! };
  }

  const tabs = (page: HTMLElement) => [...page.querySelectorAll('.tab')].map((tab) => tab.getAttribute('href'));

  function flushCategories() {
    http.expectOne('/api/question-categories').flush([]);
  }

  it("opens on the login's players, with only the players and password tabs for a login that is not an admin", async () => {
    const { harness, page } = await open('/settings', false);
    http.expectOne('/api/players').flush([]);
    await harness.fixture.whenStable();

    expect(TestBed.inject(Router).url).toBe('/settings/players');
    expect(page.querySelector('app-players-section')).not.toBeNull();
    expect(tabs(page)).toEqual(['/settings/players', '/settings/password']);
    expect(page.querySelector('.badge')).toBeNull();
  });

  it('opens the password tab for a login that is not an admin', async () => {
    const { harness, page } = await open('/settings/password', false);
    await harness.fixture.whenStable();

    expect(TestBed.inject(Router).url).toBe('/settings/password');
    expect(page.querySelector('app-password-section')).not.toBeNull();
  });

  it('keeps a login that is not an admin out of the admin tabs', async () => {
    await open('/settings/questions', false);
    http.expectOne('/api/players').flush([]);

    expect(TestBed.inject(Router).url).toBe('/settings/players');
  });

  it('leaves an admin tab once the session check finds the login is no admin any more', async () => {
    const { harness } = await open('/settings/questions', true);
    flushCategories();
    await harness.fixture.whenStable();

    await signedInAs(TestBed.inject(AuthStore), http, { name: 'Brano', isAdmin: false });
    await harness.fixture.whenStable();
    http.expectOne('/api/players').flush([]);
    await harness.fixture.whenStable();

    expect(TestBed.inject(Router).url).toBe('/settings/players');
  });

  it('lets signing out on an admin tab go to the sign-in screen', async () => {
    const { harness } = await open('/settings/questions', true);
    flushCategories();
    await harness.fixture.whenStable();

    TestBed.inject(AuthStore).signOut();
    await harness.navigateByUrl('/sign-in');
    await harness.fixture.whenStable();

    expect(TestBed.inject(Router).url).toBe('/sign-in');
  });

  it('gives an admin tabs for players, password, categories and questions, marking the open one', async () => {
    const { harness, page } = await open('/settings/categories', true);
    flushCategories();
    await harness.fixture.whenStable();

    expect(tabs(page)).toEqual(['/settings/players', '/settings/password', '/settings/categories', '/settings/questions']);
    expect(page.querySelector('.tab--on')?.getAttribute('href')).toBe('/settings/categories');
    expect(page.querySelector('.tab--on')?.getAttribute('aria-current')).toBe('page');
    expect(page.querySelector('app-ai-questions-section')).not.toBeNull();
    expect(page.querySelector('app-players-section')).toBeNull();
    expect(page.querySelector('.badge')).not.toBeNull();
  });

  it('switches to the questions tab', async () => {
    const { harness, page } = await open('/settings/players', true);
    http.expectOne('/api/players').flush([]);
    await harness.fixture.whenStable();

    page.querySelector<HTMLAnchorElement>('.tab[href="/settings/questions"]')!.click();
    await harness.fixture.whenStable();
    flushCategories();
    await harness.fixture.whenStable();

    expect(TestBed.inject(Router).url).toBe('/settings/questions');
    expect(page.querySelector('app-questions')).not.toBeNull();
  });
});
