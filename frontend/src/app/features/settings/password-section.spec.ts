import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { AuthStore } from '../../core/auth/auth-store';
import { signedInAs, testSession } from '../../core/auth/testing';
import { GameRulesStore } from '../../core/rules/game-rules-store';
import { TEST_RULES } from '../../core/rules/testing';
import { PasswordSection } from './password-section';

describe('PasswordSection', () => {
  let http: HttpTestingController;

  beforeEach(async () => {
    localStorage.clear();
    TestBed.configureTestingModule({
      imports: [PasswordSection],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideTranslateService()],
    });
    http = TestBed.inject(HttpTestingController);
    TestBed.inject(GameRulesStore).rules.set(TEST_RULES);
    await signedInAs(TestBed.inject(AuthStore), http, { name: 'Brano' });
  });

  afterEach(() => {
    http.verify();
    localStorage.clear();
  });

  async function render() {
    const fixture = TestBed.createComponent(PasswordSection);
    await fixture.whenStable();
    return { fixture, page: fixture.nativeElement as HTMLElement };
  }

  function type(page: HTMLElement, selector: string, value: string): void {
    const input = page.querySelector<HTMLInputElement>(selector)!;
    input.value = value;
    input.dispatchEvent(new Event('input'));
  }

  function fill(page: HTMLElement, current: string, next: string, repeat = next): void {
    type(page, '.password__current', current);
    type(page, '.password__new', next);
    type(page, '.password__repeat', repeat);
  }

  function submit(page: HTMLElement): void {
    page.querySelector('form')!.dispatchEvent(new Event('submit'));
  }

  it('changes the password, keeps the fresh token and clears the form', async () => {
    const { fixture, page } = await render();
    fill(page, 'old-password', 'new-password');
    await fixture.whenStable();
    submit(page);

    const request = http.expectOne('/api/auth/password');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({ currentPassword: 'old-password', newPassword: 'new-password' });
    request.flush({ ...testSession({ name: 'Brano' }), accessToken: 'fresh-token' });
    await fixture.whenStable();

    expect(TestBed.inject(AuthStore).token()).toBe('fresh-token');
    expect(page.querySelector('.password__done')).not.toBeNull();
    expect(page.querySelector<HTMLInputElement>('.password__current')!.value).toBe('');
    expect(page.querySelector<HTMLInputElement>('.password__new')!.value).toBe('');
  });

  it('shows the error when the current password is wrong', async () => {
    const { fixture, page } = await render();
    fill(page, 'wrong-password', 'new-password');
    submit(page);

    http
      .expectOne('/api/auth/password')
      .flush({ code: 'Auth.CurrentPasswordWrong' }, { status: 400, statusText: 'Bad Request' });
    await fixture.whenStable();

    expect(page.querySelector('.error-banner')).not.toBeNull();
    expect(page.querySelector('.password__done')).toBeNull();
    expect(TestBed.inject(AuthStore).token()).toBe('test-token');
  });

  it('drops the notice and the error once the user types again', async () => {
    const { fixture, page } = await render();
    fill(page, 'old-password', 'new-password');
    submit(page);
    http.expectOne('/api/auth/password').flush(testSession({ name: 'Brano' }));
    await fixture.whenStable();
    expect(page.querySelector('.password__done')).not.toBeNull();

    type(page, '.password__current', 'n');
    await fixture.whenStable();
    expect(page.querySelector('.password__done')).toBeNull();

    fill(page, 'new-password', 'short');
    submit(page);
    await fixture.whenStable();
    expect(page.querySelector('.error-banner')).not.toBeNull();

    type(page, '.password__new', 'shorter-no-more');
    await fixture.whenStable();
    expect(page.querySelector('.error-banner')).toBeNull();
  });

  it.each([
    ['a too short new password', 'old-password', 'short', 'short'],
    ['a repetition that differs', 'old-password', 'new-password', 'other-password'],
    ['the same password again', 'old-password', 'old-password', 'old-password'],
  ])('does not send %s', async (_, current, next, repeat) => {
    const { fixture, page } = await render();
    fill(page, current, next, repeat);
    submit(page);
    await fixture.whenStable();

    http.expectNone('/api/auth/password');
    expect(page.querySelector('.error-banner')).not.toBeNull();
  });
});
