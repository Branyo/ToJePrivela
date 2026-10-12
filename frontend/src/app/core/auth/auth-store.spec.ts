import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AuthStore } from './auth-store';
import { signedInAs, testSession } from './testing';

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

  describe('with other tabs', () => {
    function storeFromOtherTab(session: ReturnType<typeof testSession>): void {
      localStorage.setItem('session', JSON.stringify(session));
      window.dispatchEvent(new StorageEvent('storage', { key: 'session' }));
    }

    const later = 2 * 60 * 60 * 1000;

    it('picks up the fresh token another tab stored for this login after changing its password', async () => {
      await signedInAs(store, http, { id: 7 });

      storeFromOtherTab({ ...testSession({ id: 7 }, later), accessToken: 'fresh-token' });

      expect(store.token()).toBe('fresh-token');
    });

    it('leaves alone an older token another tab stored for this login', async () => {
      await signedInAs(store, http, { id: 7 });

      storeFromOtherTab({ ...testSession({ id: 7 }, 60 * 1000), accessToken: 'old-token' });

      expect(store.token()).toBe('test-token');
    });

    it('does not write its token back over a newer one another tab stored while restoring', async () => {
      localStorage.setItem('session', JSON.stringify(testSession({ id: 7 })));

      const restoring = store.restore();
      localStorage.setItem('session', JSON.stringify({ ...testSession({ id: 7 }, later), accessToken: 'fresh-token' }));
      http.expectOne('/api/auth/me').flush(testSession({ id: 7 }).account);
      await restoring;

      expect(store.token()).toBe('fresh-token');
      expect(JSON.parse(localStorage.getItem('session')!).accessToken).toBe('fresh-token');
    });

    it('takes over a newer sign-in another tab stored when the server ends this one', async () => {
      await signedInAs(store, http, { id: 7 });
      localStorage.setItem('session', JSON.stringify({ ...testSession({ id: 7 }, later), accessToken: 'fresh-token' }));

      expect(store.ended('test-token')).toBe(true);
      expect(store.token()).toBe('fresh-token');
    });

    it('signs out when the server ends this sign-in and no newer one is stored', async () => {
      await signedInAs(store, http, { id: 7 });

      expect(store.ended('test-token')).toBe(false);
      expect(store.isSignedIn()).toBe(false);
      expect(localStorage.getItem('session')).toBeNull();
    });

    it('picks up the sign-in another tab stores later for the login the server ended here', async () => {
      await signedInAs(store, http, { id: 7 });
      store.ended('test-token');

      storeFromOtherTab({ ...testSession({ id: 7 }, later), accessToken: 'fresh-token' });

      expect(store.token()).toBe('fresh-token');
    });

    it('stays signed out of a login signed out of on purpose', async () => {
      await signedInAs(store, http, { id: 7 });
      store.signOut();

      storeFromOtherTab({ ...testSession({ id: 7 }, later), accessToken: 'fresh-token' });

      expect(store.isSignedIn()).toBe(false);
    });

    it('leaves alone a sign-in another tab stored for another login', async () => {
      await signedInAs(store, http, { id: 7 });

      storeFromOtherTab({ ...testSession({ id: 8 }), accessToken: 'other-login-token' });

      expect(store.token()).toBe('test-token');
    });

    it('signing out with an old token keeps the newer sign-in another tab stored', async () => {
      await signedInAs(store, http, { id: 7 });
      localStorage.setItem('session', JSON.stringify({ ...testSession({ id: 7 }), accessToken: 'fresh-token' }));

      store.signOut();

      expect(store.isSignedIn()).toBe(false);
      expect(JSON.parse(localStorage.getItem('session')!).accessToken).toBe('fresh-token');
    });
  });
});
