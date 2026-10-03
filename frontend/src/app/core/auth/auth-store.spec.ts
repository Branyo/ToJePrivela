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

  it('signs in with the provider token and remembers the session', async () => {
    const signingIn = store.signIn('Google', 'google-id-token');
    const request = http.expectOne('/api/auth/sign-in');
    expect(request.request.body).toEqual({ provider: 'Google', token: 'google-id-token' });
    request.flush(testSession({ displayName: 'Brano' }));
    await signingIn;

    expect(store.isSignedIn()).toBe(true);
    expect(store.account()?.displayName).toBe('Brano');
    expect(store.token()).toBe('test-token');
    expect(JSON.parse(localStorage.getItem('session')!).accessToken).toBe('test-token');
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
    const signingIn = store.signIn('Facebook', 'fb-token');
    http.expectOne('/api/auth/sign-in').flush(testSession());
    await signingIn;

    store.signOut();

    expect(store.token()).toBeNull();
    expect(store.account()).toBeNull();
    expect(localStorage.getItem('session')).toBeNull();
  });
});
