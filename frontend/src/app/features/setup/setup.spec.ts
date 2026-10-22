import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideTranslateService } from '@ngx-translate/core';
import { GameRulesStore } from '../../core/rules/game-rules-store';
import { TEST_RULES } from '../../core/rules/testing';
import { Setup } from './setup';

describe('Setup', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [Setup],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([]), provideTranslateService()],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  async function render() {
    const fixture = TestBed.createComponent(Setup);
    fixture.detectChanges();
    http.expectOne('/api/players').flush([]);
    http.expectOne('/api/question-categories').flush([]);
    await fixture.whenStable();
    const page = fixture.nativeElement as HTMLElement;
    const limit = () => page.querySelector('.limit__label strong')?.textContent?.trim();
    return { fixture, page, limit };
  }

  it('starts from the default bad card limit', async () => {
    TestBed.inject(GameRulesStore).rules.set(TEST_RULES);

    const { limit } = await render();

    expect(limit()).toBe(String(TEST_RULES.defaultBadCardLimit));
  });

  it('takes the default bad card limit once rules missing at startup arrive', async () => {
    const { fixture, limit } = await render();
    // The screen asked for the rules again, since they were missing.
    http.expectOne('/api/rules').flush(TEST_RULES);
    await TestBed.inject(GameRulesStore).ensureLoaded();
    await fixture.whenStable();

    expect(limit()).toBe(String(TEST_RULES.defaultBadCardLimit));
  });
});
