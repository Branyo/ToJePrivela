import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { provideTranslateService } from '@ngx-translate/core';
import { AuthStore } from '../../core/auth/auth-store';
import { testSession } from '../../core/auth/testing';
import { GameRulesStore } from '../../core/rules/game-rules-store';
import { TEST_RULES } from '../../core/rules/testing';
import { SignIn, safeReturnUrl } from './sign-in';

describe('safeReturnUrl', () => {
  it('returns to a screen of this app', () => {
    expect(safeReturnUrl('/games/4?categories=1,2')).toBe('/games/4?categories=1,2');
  });

  it.each([undefined, '', 'https://evil.example', '//evil.example', '/sign-in?returnUrl=%2F'])(
    'goes home instead of to %s',
    (url) => {
      expect(safeReturnUrl(url)).toBe('/');
    },
  );
});

describe('SignIn', () => {
  let http: HttpTestingController;
  let navigateByUrl: ReturnType<typeof vi.spyOn>;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      imports: [SignIn],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([]), provideTranslateService()],
    });
    http = TestBed.inject(HttpTestingController);
    TestBed.inject(GameRulesStore).rules.set(TEST_RULES);
    navigateByUrl = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
  });

  afterEach(() => {
    http.verify();
    localStorage.clear();
  });

  async function render(returnUrl?: string) {
    const fixture = TestBed.createComponent(SignIn);
    if (returnUrl) {
      fixture.componentRef.setInput('returnUrl', returnUrl);
    }
    await fixture.whenStable();
    const page = fixture.nativeElement as HTMLElement;
    return { fixture, page };
  }

  function type(page: HTMLElement, selector: string, value: string): void {
    const input = page.querySelector<HTMLInputElement>(selector)!;
    input.value = value;
    input.dispatchEvent(new Event('input'));
  }

  function click(page: HTMLElement, selector: string): void {
    page.querySelector<HTMLButtonElement>(selector)!.click();
  }

  async function settle(fixture: ComponentFixture<SignIn>): Promise<void> {
    await fixture.whenStable();
    // The store's promise resolves a microtask after the response.
    await new Promise((resolve) => setTimeout(resolve));
    await fixture.whenStable();
  }

  async function openCreate(fixture: ComponentFixture<SignIn>, page: HTMLElement, name = 'Novak') {
    click(page, '.new-login__start');
    await fixture.whenStable();
    type(page, '.create__name', name);
    await fixture.whenStable();
  }

  it('needs a name before it signs in', async () => {
    const { page } = await render();

    expect(page.querySelector<HTMLButtonElement>('.sign-in__submit')!.disabled).toBe(true);
  });

  it('signs in with name and password and goes back where the user came from', async () => {
    const { fixture, page } = await render('/settings');

    type(page, '.sign-in__name', ' Brano ');
    type(page, '.sign-in__password', 'secret-password');
    await fixture.whenStable();
    click(page, '.sign-in__submit');

    const request = http.expectOne('/api/auth/sign-in');
    expect(request.request.body).toEqual({ name: 'Brano', password: 'secret-password' });
    request.flush(testSession({ name: 'Brano' }));
    await settle(fixture);

    expect(TestBed.inject(AuthStore).account()?.name).toBe('Brano');
    expect(navigateByUrl).toHaveBeenCalledWith('/settings');
  });

  it('reports a wrong password and offers nothing else', async () => {
    const { fixture, page } = await render();

    type(page, '.sign-in__name', 'Brano');
    type(page, '.sign-in__password', 'wrong-password');
    await fixture.whenStable();
    click(page, '.sign-in__submit');
    http
      .expectOne('/api/auth/sign-in')
      .flush({ code: 'Auth.WrongPassword' }, { status: 401, statusText: 'Unauthorized' });
    await settle(fixture);

    expect(page.querySelector('.error-banner')?.textContent).toContain('errors.api.Auth.WrongPassword');
    expect(page.querySelector('[role="alertdialog"]')).toBeNull();
    expect(navigateByUrl).not.toHaveBeenCalled();
  });

  it('only reports an unknown login and never offers to create it from the sign-in form', async () => {
    const { fixture, page } = await render();

    type(page, '.sign-in__name', 'Novak');
    await fixture.whenStable();
    click(page, '.sign-in__submit');
    http
      .expectOne('/api/auth/sign-in')
      .flush({ code: 'Auth.UnknownLogin' }, { status: 404, statusText: 'Not Found' });
    await settle(fixture);

    expect(page.querySelector('.error-banner')?.textContent).toContain('errors.api.Auth.UnknownLogin');
    expect(page.querySelector('[role="dialog"]')).toBeNull();
    http.expectNone('/api/auth/accounts');
  });

  it('opens an empty create window from the button next to the hint, and can close it', async () => {
    const { fixture, page } = await render();
    type(page, '.sign-in__name', 'Brano');
    await fixture.whenStable();

    click(page, '.new-login__start');
    await fixture.whenStable();

    expect(page.querySelector('[role="dialog"]')).not.toBeNull();
    expect(page.querySelector<HTMLInputElement>('.create__name')!.value).toBe('');
    expect(page.querySelector<HTMLInputElement>('.create__password')!.value).toBe('');

    click(page, '[role="dialog"] .btn--ghost');
    await fixture.whenStable();

    expect(page.querySelector('[role="dialog"]')).toBeNull();
    expect(page.querySelector<HTMLInputElement>('.sign-in__name')!.value).toBe('Brano');
  });

  it('creates the login, then signs it in', async () => {
    const { fixture, page } = await render('/new');
    await openCreate(fixture, page, 'Novakova');

    type(page, '.create__password', 'new-password');
    type(page, '.create__repeat', 'new-password');
    await fixture.whenStable();
    click(page, '.create__submit');

    const request = http.expectOne('/api/auth/accounts');
    expect(request.request.body).toEqual({ name: 'Novakova', password: 'new-password' });
    request.flush(testSession({ name: 'Novakova' }));
    await settle(fixture);

    expect(TestBed.inject(AuthStore).account()?.name).toBe('Novakova');
    expect(navigateByUrl).toHaveBeenCalledWith('/new');
    expect(page.querySelector('[role="dialog"]')).toBeNull();
  });

  it('refuses passwords that differ or are too short before asking the server', async () => {
    const { fixture, page } = await render();
    await openCreate(fixture, page);

    type(page, '.create__password', 'new-password');
    type(page, '.create__repeat', 'other-password');
    await fixture.whenStable();
    click(page, '.create__submit');
    await fixture.whenStable();
    expect(page.querySelector('[role="dialog"] .error-banner')?.textContent).toContain('signIn.errors.passwordsDiffer');

    type(page, '.create__password', 'short');
    type(page, '.create__repeat', 'short');
    await fixture.whenStable();
    click(page, '.create__submit');
    await fixture.whenStable();
    expect(page.querySelector('[role="dialog"] .error-banner')?.textContent).toContain('signIn.errors.passwordLength');

    http.expectNone('/api/auth/accounts');
  });

  it('keeps the window open and says so when the name is taken', async () => {
    const { fixture, page } = await render();
    await openCreate(fixture, page);

    type(page, '.create__password', 'new-password');
    type(page, '.create__repeat', 'new-password');
    await fixture.whenStable();
    click(page, '.create__submit');
    http
      .expectOne('/api/auth/accounts')
      .flush({ code: 'Auth.NameTaken' }, { status: 409, statusText: 'Conflict' });
    await settle(fixture);

    expect(page.querySelector('[role="dialog"] .error-banner')?.textContent).toContain('errors.api.Auth.NameTaken');
    expect(TestBed.inject(AuthStore).isSignedIn()).toBe(false);
  });

  it('says the rules are missing instead of refusing every name when the backend was down', async () => {
    TestBed.inject(GameRulesStore).rules.set(null);
    const { fixture, page } = await render();
    http.expectOne('/api/rules').error(new ProgressEvent('error'));
    await settle(fixture);

    expect(page.querySelector('.sign-in__name')!.hasAttribute('maxlength')).toBe(false);

    await openCreate(fixture, page);
    expect(page.querySelector('.field__help')).toBeNull();

    type(page, '.create__password', 'new-password');
    type(page, '.create__repeat', 'new-password');
    await fixture.whenStable();
    click(page, '.create__submit');
    http.expectOne('/api/rules').error(new ProgressEvent('error'));
    await settle(fixture);

    expect(page.querySelector('[role="dialog"] .error-banner')?.textContent).toContain('errors.rulesUnavailable');
    http.expectNone('/api/auth/accounts');
  });

  it('creates the login once the backend is back, with the rules it now serves', async () => {
    TestBed.inject(GameRulesStore).rules.set(null);
    const { fixture, page } = await render();
    http.expectOne('/api/rules').error(new ProgressEvent('error'));
    await settle(fixture);
    await openCreate(fixture, page);

    type(page, '.create__password', 'new-password');
    type(page, '.create__repeat', 'new-password');
    await fixture.whenStable();
    click(page, '.create__submit');
    http.expectOne('/api/rules').flush(TEST_RULES);
    await settle(fixture);

    http.expectOne('/api/auth/accounts').flush(testSession({ name: 'Novak' }));
    await settle(fixture);

    expect(TestBed.inject(AuthStore).account()?.name).toBe('Novak');
  });
});
