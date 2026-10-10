import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideTranslateService } from '@ngx-translate/core';
import { QuestionCategory } from '../../core/api/models';
import { AuthStore } from '../../core/auth/auth-store';
import { signedInAs } from '../../core/auth/testing';
import { LanguageService } from '../../core/i18n/language';
import { GameRulesStore } from '../../core/rules/game-rules-store';
import { TEST_RULES } from '../../core/rules/testing';
import { AiQuestionsSection } from './ai-questions-section';

const CATEGORIES: QuestionCategory[] = [
  { id: 2, name: 'Šport', nameSk: 'Šport', nameEn: 'Sport', questionCount: 1, aiQuestionCount: 1 },
  { id: 1, name: 'Autá', nameSk: 'Autá', nameEn: 'Cars', questionCount: 2, aiQuestionCount: 1 },
];

describe('AiQuestionsSection', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [AiQuestionsSection],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([]), provideTranslateService()],
    });
    http = TestBed.inject(HttpTestingController);
    TestBed.inject(GameRulesStore).rules.set(TEST_RULES);
  });

  afterEach(() => http.verify());

  function flushLoad(categories = CATEGORIES) {
    http.expectOne('/api/question-categories').flush(categories);
  }

  async function render(categories?: QuestionCategory[]) {
    const fixture = TestBed.createComponent(AiQuestionsSection);
    fixture.detectChanges();
    flushLoad(categories);
    await fixture.whenStable();
    return { fixture, page: fixture.nativeElement as HTMLElement };
  }

  it('lists the categories by name', async () => {
    const { page } = await render();

    expect([...page.querySelectorAll('.row__name')].map((name) => name.textContent?.trim())).toEqual(['Autá', 'Šport']);
    expect([...page.querySelectorAll('.row__other')].map((name) => name.textContent?.trim())).toEqual(['Cars', 'Sport']);
  });

  it('loads the categories again after a language switch, for their names in that language', async () => {
    const { fixture } = await render();

    await TestBed.inject(LanguageService).use('en');
    fixture.detectChanges();

    const reload = http.expectOne('/api/question-categories');
    expect(reload.request.method).toBe('GET');
    reload.flush(CATEGORIES);
  });

  it('sends the new name in the language picked for it, for the AI to translate', async () => {
    const { fixture, page } = await render();

    [...page.querySelectorAll<HTMLButtonElement>('.name-language .chip')].find((chip) => chip.textContent?.trim() === 'EN')!.click();
    const name = page.querySelector<HTMLInputElement>('form .text-input:not(.count)')!;
    name.value = 'Birds';
    name.dispatchEvent(new Event('input'));
    await fixture.whenStable();

    page.querySelector<HTMLButtonElement>('form button[type="submit"]')!.click();
    const request = http.expectOne({ method: 'POST', url: '/api/question-categories' });
    expect(request.request.body).toEqual({ nameEn: 'Birds', questionCount: 100 });
    request.flush({ id: 3, name: 'Vtáky', nameSk: 'Vtáky', nameEn: 'Birds', questionGeneration: { requested: 100, created: 100, discarded: 0 } });
    flushLoad();
    await fixture.whenStable();
  });

  it('creates a new category with 100 AI questions unless told otherwise', async () => {
    const { fixture, page } = await render();

    const name = page.querySelector<HTMLInputElement>('form .text-input:not(.count)')!;
    name.value = 'Music';
    name.dispatchEvent(new Event('input'));
    await fixture.whenStable();
    expect(page.querySelector<HTMLInputElement>('form .count')!.value).toBe('100');

    page.querySelector<HTMLButtonElement>('form button[type="submit"]')!.click();
    const request = http.expectOne({ method: 'POST', url: '/api/question-categories' });
    expect(request.request.body).toEqual({ nameSk: 'Music', questionCount: 100 });
    request.flush({ id: 3, name: 'Music', nameSk: 'Music', nameEn: 'Music', questionGeneration: { requested: 100, created: 100, discarded: 0 }, questions: [] });
    flushLoad();
    await fixture.whenStable();
  });

  it('adds AI questions to a category and reloads the counts', async () => {
    const { fixture, page } = await render();

    page.querySelector<HTMLButtonElement>('.row__actions .btn--sky')!.click();
    const request = http.expectOne({ method: 'POST', url: '/api/question-categories/1/ai-questions' });
    expect(request.request.body).toEqual({ count: 20 });
    request.flush({ summary: { requested: 20, created: 18, discarded: 2 }, questions: [] });
    flushLoad();
    await fixture.whenStable();

    expect(page.querySelector('.notice')).not.toBeNull();
  });

  it('still shows AI questions being generated after the tab was left and opened again, and their result', async () => {
    const { fixture, page } = await render();
    page.querySelector<HTMLButtonElement>('.row__actions .btn--sky')!.click();
    const request = http.expectOne({ method: 'POST', url: '/api/question-categories/1/ai-questions' });
    fixture.destroy();

    const again = await render();
    expect(again.page.querySelector('.row--busy .row__name')?.textContent?.trim()).toBe('Autá');

    request.flush({ summary: { requested: 20, created: 18, discarded: 2 }, questions: [] });
    flushLoad();
    await again.fixture.whenStable();

    expect(again.page.querySelector('.row--busy')).toBeNull();
    expect(again.page.querySelector('.notice')).not.toBeNull();
  });

  it("forgets a failed load and a refused input once the tab is left, and a load that worked clears the error", async () => {
    const fixture = TestBed.createComponent(AiQuestionsSection);
    fixture.detectChanges();
    http.expectOne('/api/question-categories').flush(null, { status: 503, statusText: 'Service Unavailable' });
    await fixture.whenStable();
    expect(fixture.nativeElement.querySelector('[role="alert"]')).not.toBeNull();
    fixture.destroy();

    const again = await render();
    expect(again.page.querySelector('[role="alert"]')).toBeNull();

    const name = again.page.querySelector<HTMLInputElement>('form .text-input:not(.count)')!;
    name.value = 'X'; // too short
    name.dispatchEvent(new Event('input'));
    await again.fixture.whenStable();
    again.page.querySelector<HTMLButtonElement>('form button[type="submit"]')!.click();
    await again.fixture.whenStable();
    expect(again.page.querySelector('[role="alert"]')).not.toBeNull();
    again.fixture.destroy();

    const third = await render();
    expect(third.page.querySelector('[role="alert"]')).toBeNull();
  });

  it('keeps the result of a change for the account that made it, not for the next sign-in', async () => {
    localStorage.clear();
    const auth = TestBed.inject(AuthStore);
    await signedInAs(auth, http, { id: 1, isAdmin: true });
    const { fixture, page } = await render();
    page.querySelector<HTMLButtonElement>('.row__actions .btn--sky')!.click();
    http.expectOne({ method: 'POST', url: '/api/question-categories/1/ai-questions' }).flush({
      summary: { requested: 20, created: 18, discarded: 2 },
      questions: [],
    });
    flushLoad();
    await fixture.whenStable();
    expect(page.querySelector('.notice')).not.toBeNull();
    fixture.destroy();

    // The same account confirmed again keeps it.
    await signedInAs(auth, http, { id: 1, isAdmin: true });
    const same = await render();
    expect(same.page.querySelector('.notice')).not.toBeNull();
    same.fixture.destroy();

    auth.signOut();
    await signedInAs(auth, http, { id: 2, isAdmin: true });
    const other = await render();
    expect(other.page.querySelector('.notice')).toBeNull();
    localStorage.clear();
  });

  it('deletes a category only after a second tap', async () => {
    const { fixture, page } = await render();

    page.querySelector<HTMLButtonElement>('.row__actions .danger')!.click();
    await fixture.whenStable();
    http.expectNone({ method: 'DELETE' });

    page.querySelector<HTMLButtonElement>('.row__confirm .btn--danger')!.click();
    http.expectOne({ method: 'DELETE', url: '/api/question-categories/1' }).flush(null);
    flushLoad([CATEGORIES[0]]);
    await fixture.whenStable();

    expect(page.querySelector('.row__confirm')).toBeNull();
  });

  it('offers to delete AI questions only where there are some', async () => {
    const { page } = await render([CATEGORIES[1], { ...CATEGORIES[0], aiQuestionCount: 0 }]);

    const deleteAi = [...page.querySelectorAll<HTMLButtonElement>('.row__actions .btn--ghost:not(.danger)')];
    expect(deleteAi.map((button) => button.disabled)).toEqual([false, true]);
  });
});
