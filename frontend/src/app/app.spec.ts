import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TranslateService, provideTranslateService } from '@ngx-translate/core';
import { App } from './app';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter([]), provideTranslateService()],
    }).compileComponents();

    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('sk', { app: { name: 'To je priveľa!' } });
    translate.use('sk');
  });

  it('shows the logo linking home', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();

    const logo = (fixture.nativeElement as HTMLElement).querySelector('a.logo');
    expect(logo?.textContent).toContain('To je priveľa!');
    expect(logo?.getAttribute('href')).toBe('/');
  });

  it('offers every language and marks the active one', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();

    const options = [...(fixture.nativeElement as HTMLElement).querySelectorAll('.languages__option')];
    expect(options.map((o) => o.textContent?.trim())).toEqual(['SK', 'EN']);
    expect(options.map((o) => o.getAttribute('aria-pressed'))).toEqual(['true', 'false']);
  });
});
