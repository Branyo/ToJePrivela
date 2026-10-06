import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideTranslateService } from '@ngx-translate/core';
import { AuthStore } from '../../core/auth/auth-store';
import { signedInAs } from '../../core/auth/testing';
import { GameRulesStore } from '../../core/rules/game-rules-store';
import { TEST_RULES } from '../../core/rules/testing';
import { Settings } from './settings';

describe('Settings', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      imports: [Settings],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([]), provideTranslateService()],
    });
    http = TestBed.inject(HttpTestingController);
    TestBed.inject(GameRulesStore).rules.set(TEST_RULES);
  });

  afterEach(() => {
    http.verify();
    localStorage.clear();
  });

  async function render(isAdmin: boolean): Promise<HTMLElement> {
    await signedInAs(TestBed.inject(AuthStore), http, { name: 'Brano', isAdmin });
    const fixture = TestBed.createComponent(Settings);
    fixture.detectChanges();
    http.expectOne('/api/players').flush([]);
    if (isAdmin) {
      http.expectOne('/api/question-categories').flush([]);
      http.expectOne('/api/questions').flush([]);
    }
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  it('lets every login manage its players but keeps AI generation from non-admins', async () => {
    const page = await render(false);

    expect(page.querySelector('app-players-section')).not.toBeNull();
    expect(page.querySelector('app-ai-questions-section')).toBeNull();
    expect(page.querySelector('.badge')).toBeNull();
  });

  it('gives an admin the AI questions too', async () => {
    const page = await render(true);

    expect(page.querySelector('app-players-section')).not.toBeNull();
    expect(page.querySelector('app-ai-questions-section')).not.toBeNull();
    expect(page.querySelector('.badge')).not.toBeNull();
  });
});
