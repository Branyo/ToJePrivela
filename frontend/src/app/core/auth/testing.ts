import { HttpTestingController } from '@angular/common/http/testing';
import { Account, SignedIn } from '../api/models';
import { AuthStore } from './auth-store';

/** A sign-in as the backend returns it, for specs. */
export function testSession(account: Partial<Account> = {}, expiresInMs = 60 * 60 * 1000): SignedIn {
  return {
    accessToken: 'test-token',
    expiresAt: new Date(Date.now() + expiresInMs).toISOString(),
    account: { id: 7, provider: 'Google', displayName: 'Tester', email: 'tester@example.com', isAdmin: false, ...account },
  };
}

/** Signs the store in the way a reload does: from storage, confirmed by `GET /api/auth/me`. */
export async function signedInAs(
  store: AuthStore,
  http: HttpTestingController,
  account: Partial<Account> = {},
): Promise<void> {
  const session = testSession(account);
  localStorage.setItem('session', JSON.stringify(session));
  const restoring = store.restore();
  http.expectOne('/api/auth/me').flush(session.account);
  await restoring;
}
