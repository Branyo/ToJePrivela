import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AuthStore } from './auth-store';
import { testSession } from './testing';

describe('AuthStore', () => {
  let store: AuthStore;
  let http: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    store = TestBed.inject(AuthStore);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    localStorage.clear();
  });

  it('signs in with name and password and remembers the session', async () => {
    const signingIn = store.signIn('Brano', 'secret-password');
    const request = http.expectOne('/api/auth/sign-in');
    expect(request.request.body).toEqual({ name: 'Brano', password: 'secret-password' });
    request.flush(testSession({ name: 'Brano' }));
    await signingIn;

    expect(store.isSignedIn()).toBe(true);
    expect(store.account()?.name).toBe('Brano');
    expect(store.token()).toBe('test-token');
    expect(JSON.parse(localStorage.getItem('session')!).accessToken).toBe('test-token');
  });

  it('creates a login and stays signed in to it', async () => {
    const creating = store.createAccount('Novak', 'novak-password');
    const request = http.expectOne('/api/auth/accounts');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ name: 'Novak', password: 'novak-password' });
    request.flush(testSession({ name: 'Novak' }));
    await creating;

    expect(store.account()?.name).toBe('Novak');
    expect(store.token()).toBe('test-token');
  });

  it('rejects and stays signed out when the sign-in is refused', async () => {
    const signingIn = store.signIn('Brano', 'wrong');
    http
      .expectOne('/api/auth/sign-in')
      .flush({ code: 'Auth.WrongPassword' }, { status: 401, statusText: 'Unauthorized' });

    await expect(signingIn).rejects.toBeTruthy();
    expect(store.isSignedIn()).toBe(false);
  });

  it('restores a stored session and refreshes the account', async () => {
    localStorage.setItem('session', JSON.stringify(testSession({ isAdmin: false })));

    const restoring = store.restore();
    http.expectOne('/api/auth/me').flush(testSession({ isAdmin: true }).account);
    await restoring;

    expect(store.isAdmin()).toBe(true);
    expect(JSON.parse(localStorage.getItem('session')!).account.isAdmin).toBe(true);
  });

  it('drops a stored session the server no longer accepts', async () => {
    localStorage.setItem('session', JSON.stringify(testSession()));

    const restoring = store.restore();
    http.expectOne('/api/auth/me').flush({ code: 'Auth.Unauthenticated' }, { status: 401, statusText: 'Unauthorized' });
    await restoring;

    expect(store.isSignedIn()).toBe(false);
    expect(localStorage.getItem('session')).toBeNull();
  });

  it('keeps the session when the server is unreachable', async () => {
    localStorage.setItem('session', JSON.stringify(testSession()));

    const restoring = store.restore();
    http.expectOne('/api/auth/me').error(new ProgressEvent('error'));
    await restoring;

    expect(store.isSignedIn()).toBe(true);
  });

  it('ignores an expired session without asking the server', async () => {
    localStorage.setItem('session', JSON.stringify(testSession({}, -1000)));

    await store.restore();

    expect(store.isSignedIn()).toBe(false);
    expect(localStorage.getItem('session')).toBeNull();
  });

  it('forgets everything on sign-out', async () => {
    const signingIn = store.signIn('Brano', 'secret-password');
    http.expectOne('/api/auth/sign-in').flush(testSession());
    await signingIn;

    store.signOut();

    expect(store.token()).toBeNull();
    expect(store.account()).toBeNull();
    expect(localStorage.getItem('session')).toBeNull();
  });
});
