import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { authInterceptor } from './auth-interceptor';
import { AuthStore } from './auth-store';
import { signedInAs } from './testing';

describe('authInterceptor', () => {
  let http: HttpTestingController;
  let client: HttpClient;
  let store: AuthStore;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    });
    http = TestBed.inject(HttpTestingController);
    client = TestBed.inject(HttpClient);
    store = TestBed.inject(AuthStore);
  });

  afterEach(() => {
    http.verify();
    localStorage.clear();
  });

  it('sends the access token with API requests', async () => {
    await signedInAs(store, http);

    const response = firstValueFrom(client.get('/api/players'));
    const request = http.expectOne('/api/players');
    request.flush([]);
    await response;

    expect(request.request.headers.get('Authorization')).toBe('Bearer test-token');
  });

  it('sends no token before signing in, nor to other hosts', async () => {
    const api = firstValueFrom(client.get('/api/rules'));
    const apiRequest = http.expectOne('/api/rules');
    apiRequest.flush({});
    await api;

    await signedInAs(store, http);
    const other = firstValueFrom(client.get('/i18n/en.json'));
    const otherRequest = http.expectOne('/i18n/en.json');
    otherRequest.flush({});
    await other;

    expect(apiRequest.request.headers.has('Authorization')).toBe(false);
    expect(otherRequest.request.headers.has('Authorization')).toBe(false);
  });

  it('signs out and asks to sign in again when the API answers 401', async () => {
    await signedInAs(store, http);
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    const response = firstValueFrom(client.get('/api/games'));
    http.expectOne('/api/games').flush({ code: 'Auth.Unauthenticated' }, { status: 401, statusText: 'Unauthorized' });

    await expect(response).rejects.toBeTruthy();
    expect(store.isSignedIn()).toBe(false);
    expect(navigate).toHaveBeenCalledWith(['/sign-in'], { queryParams: { returnUrl: '/' } });
  });

  it('leaves a refused sign-in to the sign-in screen', async () => {
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate');

    const response = firstValueFrom(client.post('/api/auth/sign-in', {}));
    http.expectOne('/api/auth/sign-in').flush({ code: 'Auth.InvalidToken' }, { status: 401, statusText: 'Unauthorized' });

    await expect(response).rejects.toBeTruthy();
    expect(navigate).not.toHaveBeenCalled();
  });
});
