import { registerLocaleData } from '@angular/common';
import localeSk from '@angular/common/locales/sk';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { routes } from '../../app.routes';
import { AccountOverview } from '../../core/api/models';
import { AuthStore } from '../../core/auth/auth-store';
import { signedInAs } from '../../core/auth/testing';

// The app registers it in `app.config.ts`; dates on the page are formatted for the Slovak default.
registerLocaleData(localeSk, 'sk-SK');

/** The signed-in admin is login 7 (`testSession`). */
const accounts: AccountOverview[] = [
  { id: 5, name: 'Rodina', isAdmin: false, createdAt: '2026-09-01T10:00:00Z', lastSignedInAt: '2026-10-20T18:00:00Z', canDelete: true },
  { id: 7, name: 'Brano', isAdmin: true, createdAt: '2026-08-01T10:00:00Z', lastSignedInAt: '2026-10-19T18:00:00Z', canDelete: false },
  { id: 9, name: 'Šéf', isAdmin: true, createdAt: '2026-08-01T10:00:00Z', lastSignedInAt: null, canDelete: true },
  { id: 1, name: 'Prvý', isAdmin: true, createdAt: '2026-07-01T10:00:00Z', lastSignedInAt: null, canDelete: false },
];

describe('Accounts', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter(routes, withComponentInputBinding()), provideTranslateService()],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    localStorage.clear();
  });

  async function open() {
    await signedInAs(TestBed.inject(AuthStore), http, { name: 'Brano', isAdmin: true });
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/settings/accounts');
    http.expectOne('/api/admin/accounts').flush(accounts);
    await harness.fixture.whenStable();
    return { harness, page: harness.routeNativeElement! };
  }

  const rows = (page: HTMLElement) => [...page.querySelectorAll<HTMLElement>('.row')];

  it("lists every login in the server's order, each opening its own view", async () => {
    const { page } = await open();

    expect(rows(page).map((row) => row.querySelector('.row__name')!.childNodes[0].textContent?.trim())).toEqual([
      'Rodina',
      'Brano',
      'Šéf',
      'Prvý',
    ]);
    expect(rows(page).map((row) => row.querySelector('.row__open')!.getAttribute('href'))).toEqual([
      '/accounts/5',
      '/accounts/7',
      '/accounts/9',
      '/accounts/1',
    ]);
    expect(rows(page)[2].querySelector('.badge')).not.toBeNull();
    expect(rows(page)[0].querySelector('.badge')).toBeNull();
    expect(rows(page)[2].querySelector('.row__dates')?.textContent).toContain('accounts.neverSignedIn');
  });

  it('marks the admin\'s own login, which offers no deletion', async () => {
    const { page } = await open();

    expect(rows(page)[1].querySelector('.badge--quiet')).not.toBeNull();
    expect(rows(page)[1].querySelector('.row__remove')).toBeNull();
    expect(rows(page)[0].querySelector('.row__remove')).not.toBeNull();
  });

  it('offers no deletion of a login the server says cannot be deleted', async () => {
    const { page } = await open();

    expect(rows(page)[3].querySelector('.badge--quiet')).toBeNull();
    expect(rows(page)[3].querySelector('.row__remove')).toBeNull();
  });

  it('runs one deletion at a time: no other can be asked for until it is done', async () => {
    const { harness, page } = await open();

    rows(page)[0].querySelector<HTMLButtonElement>('.row__remove')!.click();
    await harness.fixture.whenStable();
    rows(page)[0].querySelector<HTMLButtonElement>('.btn--danger')!.click();
    await harness.fixture.whenStable();
    const request = http.expectOne('/api/admin/accounts/5');

    expect(rows(page)[0].querySelector<HTMLButtonElement>('.btn--danger')!.disabled).toBe(true);
    expect(rows(page)[2].querySelector<HTMLButtonElement>('.row__remove')!.disabled).toBe(true);
    rows(page)[2].querySelector<HTMLButtonElement>('.row__remove')!.click();
    await harness.fixture.whenStable();
    expect(page.querySelectorAll('.row--confirming').length).toBe(1);

    request.flush(null, { status: 204, statusText: 'No Content' });
    await harness.fixture.whenStable();

    expect(page.querySelector('.row--confirming')).toBeNull();
    expect(rows(page)[1].querySelector<HTMLButtonElement>('.row__remove')!.disabled).toBe(false);
  });

  it('deletes a login after a second tap', async () => {
    const { harness, page } = await open();

    rows(page)[0].querySelector<HTMLButtonElement>('.row__remove')!.click();
    await harness.fixture.whenStable();
    expect(rows(page)[0].classList).toContain('row--confirming');
    rows(page)[0].querySelector<HTMLButtonElement>('.btn--danger')!.click();
    const request = http.expectOne('/api/admin/accounts/5');
    expect(request.request.method).toBe('DELETE');
    request.flush(null, { status: 204, statusText: 'No Content' });
    await harness.fixture.whenStable();

    expect(rows(page).length).toBe(3);
    expect(page.querySelector('.row--confirming')).toBeNull();
  });

  it('warns that a configured admin comes back when deleting one', async () => {
    const { harness, page } = await open();

    rows(page)[2].querySelector<HTMLButtonElement>('.row__remove')!.click();
    await harness.fixture.whenStable();

    expect(rows(page)[2].querySelector('.row__question')?.textContent).toContain('accounts.confirmAdmin');
  });

  it('drops a login deleted meanwhile, and keeps one whose deletion failed', async () => {
    const { harness, page } = await open();

    rows(page)[0].querySelector<HTMLButtonElement>('.row__remove')!.click();
    await harness.fixture.whenStable();
    rows(page)[0].querySelector<HTMLButtonElement>('.btn--danger')!.click();
    http
      .expectOne('/api/admin/accounts/5')
      .flush({ code: 'Account.NotFound' }, { status: 404, statusText: 'Not Found' });
    await harness.fixture.whenStable();
    expect(rows(page).length).toBe(3);

    rows(page)[1].querySelector<HTMLButtonElement>('.row__remove')!.click();
    await harness.fixture.whenStable();
    rows(page)[1].querySelector<HTMLButtonElement>('.btn--danger')!.click();
    http.expectOne('/api/admin/accounts/9').flush({ code: 'Concurrency.Conflict' }, { status: 409, statusText: 'Conflict' });
    await harness.fixture.whenStable();

    expect(rows(page).length).toBe(3);
    expect(page.querySelector('.error-banner')).not.toBeNull();
    expect(rows(page)[1].querySelector<HTMLButtonElement>('.btn--danger')!.disabled).toBe(false);
  });
});
