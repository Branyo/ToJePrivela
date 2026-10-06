import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TranslateService, provideTranslateService } from '@ngx-translate/core';
import { App } from './app';
import { AuthStore } from './core/auth/auth-store';
import { signedInAs } from './core/auth/testing';

describe('App', () => {
  beforeEach(async () => {
    localStorage.clear();
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([]), provideTranslateService()],
    }).compileComponents();

    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('sk', { app: { name: 'To je priveľa!' } });
    translate.use('sk');
  });

  afterEach(() => localStorage.clear());

  async function render(): Promise<HTMLElement> {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  it('shows the logo linking home', async () => {
    const logo = (await render()).querySelector('a.logo');

    expect(logo?.textContent).toContain('To je priveľa!');
    expect(logo?.getAttribute('href')).toBe('/');
  });

  it('offers every language and marks the active one', async () => {
    const options = [...(await render()).querySelectorAll('.languages__option')];

    expect(options.map((o) => o.textContent?.trim())).toEqual(['SK', 'EN']);
    expect(options.map((o) => o.getAttribute('aria-pressed'))).toEqual(['true', 'false']);
  });

  it('shows no menu or account before signing in', async () => {
    const page = await render();

    expect(page.querySelector('.nav')).toBeNull();
    expect(page.querySelector('.account')).toBeNull();
  });

  it('shows the signed-in login and a link to its settings', async () => {
    await signedInAs(TestBed.inject(AuthStore), TestBed.inject(HttpTestingController), { name: 'Brano' });

    const page = await render();

    expect(page.querySelector('.account__name')?.textContent).toContain('Brano');
    expect([...page.querySelectorAll('.nav__link')].map((a) => a.getAttribute('href'))).toEqual(['/settings']);
  });
});
