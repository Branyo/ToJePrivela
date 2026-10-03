import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideTranslateService } from '@ngx-translate/core';
import { ProviderSdks } from '../../core/auth/provider-sdks';
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
  const sdks = {
    renderGoogleButton: vi.fn().mockResolvedValue(undefined),
    prepareFacebook: vi.fn().mockResolvedValue(undefined),
    facebookLogin: vi.fn(),
  };

  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.clear();
    TestBed.configureTestingModule({
      imports: [SignIn],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        provideTranslateService(),
        { provide: ProviderSdks, useValue: sdks },
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    localStorage.clear();
  });

  async function render(providers: { provider: string; clientId: string }[]) {
    const fixture = TestBed.createComponent(SignIn);
    fixture.detectChanges();
    http.expectOne('/api/auth/providers').flush(providers);
    await fixture.whenStable();
    return { fixture, page: fixture.nativeElement as HTMLElement };
  }

  it('offers only the configured providers', async () => {
    const { page } = await render([{ provider: 'Google', clientId: 'google-client' }]);

    expect(page.querySelector('.providers__google')).not.toBeNull();
    expect(page.querySelector('.providers__facebook')).toBeNull();
    expect(sdks.renderGoogleButton).toHaveBeenCalledWith(
      expect.any(HTMLElement),
      'google-client',
      expect.any(String),
      expect.any(Function),
    );
  });

  it("signs in with Facebook's token", async () => {
    sdks.facebookLogin.mockResolvedValue('fb-token');
    const { fixture, page } = await render([{ provider: 'Facebook', clientId: 'fb-app' }]);
    expect(sdks.prepareFacebook).toHaveBeenCalledWith('fb-app');

    page.querySelector<HTMLButtonElement>('.providers__facebook')!.click();
    await fixture.whenStable();

    const request = http.expectOne('/api/auth/sign-in');
    expect(request.request.body).toEqual({ provider: 'Facebook', token: 'fb-token' });
    request.flush({
      accessToken: 't',
      expiresAt: new Date(Date.now() + 60_000).toISOString(),
      account: { id: 1, provider: 'Facebook', displayName: 'Duri', email: null, isAdmin: false },
    });
  });

  it('says so when no provider is configured', async () => {
    const { page } = await render([]);

    expect(page.textContent).toContain('signIn.noProviders');
  });
});
