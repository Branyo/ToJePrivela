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
import { MAX_JSON_LENGTH } from './json-questions-input';

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

  function typeName(page: HTMLElement, language: 'sk' | 'en', value: string) {
    const name = page.querySelector<HTMLInputElement>(`.new-category__name[data-language="${language}"]`)!;
    name.value = value;
    name.dispatchEvent(new Event('input'));
  }

  it('sends the new name in the language it was typed in, for the AI to translate', async () => {
    const { fixture, page } = await render();

    typeName(page, 'en', 'Birds');
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

    const deleteAi = [...page.querySelectorAll<HTMLButtonElement>('.row__actions .btn--ghost:not(.danger):not(.row__import-open)')];
    expect(deleteAi.map((button) => button.disabled)).toEqual([false, true]);
  });

  async function openImport(fixture: { whenStable(): Promise<unknown> }, page: HTMLElement) {
    page.querySelector<HTMLButtonElement>('.row__import-open')!.click();
    await fixture.whenStable();
    return page.querySelector<HTMLTextAreaElement>('.row__import .json-input__text')!;
  }

  it('imports pasted JSON into the category and lists what it left out', async () => {
    const { fixture, page } = await render();
    const json = '[{"questionSk": "Koľko kolies má auto?", "questionEn": "How many wheels does a car have?", "answer": 4}]';

    const text = await openImport(fixture, page);
    text.value = json;
    text.dispatchEvent(new Event('input'));
    await fixture.whenStable();
    page.querySelector<HTMLButtonElement>('.row__import button[type="submit"]')!.click();

    const request = http.expectOne({ method: 'POST', url: '/api/question-categories/1/imported-questions' });
    expect(request.request.body).toEqual({ json });
    request.flush({
      summary: {
        received: 3,
        imported: 1,
        rejected: [
          { position: 2, reason: 'Duplicate' },
          { position: 3, reason: 'InvalidAnswer' },
        ],
      },
      questions: [],
    });
    flushLoad();
    await fixture.whenStable();

    expect(page.querySelector('.row__import')).toBeNull();
    expect(page.querySelector('.notice')).not.toBeNull();
    expect([...page.querySelectorAll('.notice__details li')].map((item) => item.textContent?.trim())).toEqual([
      'admin.import.reasons.Duplicate',
      'admin.import.reasons.InvalidAnswer',
    ]);
  });

  it('puts a picked file into the text box before importing it', async () => {
    const { fixture, page } = await render();
    const json = '[{"questionSk": "Koľko kolies má auto?", "questionEn": "How many wheels does a car have?", "answer": 4}]';

    await openImport(fixture, page);
    const file = page.querySelector<HTMLInputElement>('.row__import .json-input__file')!;
    Object.defineProperty(file, 'files', { value: [new File([json], 'questions.json', { type: 'application/json' })] });
    file.dispatchEvent(new Event('change'));
    await new Promise((resolve) => setTimeout(resolve));
    await fixture.whenStable();

    expect(page.querySelector<HTMLTextAreaElement>('.row__import .json-input__text')!.value).toBe(json);
    http.expectNone({ method: 'POST' });
  });

  it('refuses a picked file too large to import without reading it', async () => {
    const { fixture, page } = await render();

    const text = await openImport(fixture, page);
    const file = page.querySelector<HTMLInputElement>('.row__import .json-input__file')!;
    const huge = new File(['[]'], 'huge.json', { type: 'application/json' });
    Object.defineProperty(huge, 'size', { value: MAX_JSON_LENGTH * 4 + 1 });
    const read = vi.spyOn(huge, 'text');
    Object.defineProperty(file, 'files', { value: [huge] });
    file.dispatchEvent(new Event('change'));
    await new Promise((resolve) => setTimeout(resolve));
    await fixture.whenStable();

    expect(read).not.toHaveBeenCalled();
    expect(text.value).toBe('');
    expect(page.querySelector('[role="alert"]')?.textContent).toContain('admin.import.tooLarge');
    http.expectNone({ method: 'POST' });
  });

  it('keeps the import open and shows why the server refused the JSON', async () => {
    const { fixture, page } = await render();

    const text = await openImport(fixture, page);
    text.value = 'no json here';
    text.dispatchEvent(new Event('input'));
    await fixture.whenStable();
    page.querySelector<HTMLButtonElement>('.row__import button[type="submit"]')!.click();

    http
      .expectOne({ method: 'POST', url: '/api/question-categories/1/imported-questions' })
      .flush({ code: 'QuestionImport.InvalidJson', detail: 'Not a JSON array.' }, { status: 400, statusText: 'Bad Request' });
    await fixture.whenStable();

    expect(page.querySelector('[role="alert"]')).not.toBeNull();
    expect(page.querySelector<HTMLTextAreaElement>('.row__import .json-input__text')!.value).toBe('no json here');
  });

  it('closes the import without sending anything', async () => {
    const { fixture, page } = await render();

    await openImport(fixture, page);
    page.querySelector<HTMLButtonElement>('.row__import .row__actions .btn--ghost')!.click();
    await fixture.whenStable();

    expect(page.querySelector('.row__import')).toBeNull();
    http.expectNone({ method: 'POST' });
  });

  async function startImportedCategory(fixture: { whenStable(): Promise<unknown> }, page: HTMLElement, json: string) {
    sourceChip(page, 'json').click();
    await fixture.whenStable();
    typeName(page, 'sk', 'Autobusy');
    const text = page.querySelector<HTMLTextAreaElement>('.new-category .json-input__text')!;
    text.value = json;
    text.dispatchEvent(new Event('input'));
    await fixture.whenStable();
  }

  it('creates a new category with imported JSON questions instead of AI ones', async () => {
    const { fixture, page } = await render();
    const json = '[{"questionSk": "Koľko kolies má autobus?", "questionEn": "How many wheels does a bus have?", "answer": 6}]';

    await startImportedCategory(fixture, page, json);
    expect(page.querySelector('.new-category .count')).toBeNull();

    page.querySelector<HTMLButtonElement>('.new-category button[type="submit"]')!.click();
    const request = http.expectOne({ method: 'POST', url: '/api/question-categories' });
    expect(request.request.body).toEqual({ nameSk: 'Autobusy', questionCount: 0, questionsJson: json });
    request.flush({
      id: 3,
      name: 'Autobusy',
      nameSk: 'Autobusy',
      nameEn: 'Buses',
      questionCount: 1,
      aiQuestionCount: 1,
      questionGeneration: { requested: 0, created: 0, discarded: 0 },
      questionImport: { received: 2, imported: 1, rejected: [{ position: 2, reason: 'StatesAnswer' }] },
    });
    flushLoad();
    await fixture.whenStable();

    expect(page.querySelector('.notice')).not.toBeNull();
    expect([...page.querySelectorAll('.notice__details li')].map((item) => item.textContent?.trim())).toEqual([
      'admin.import.reasons.StatesAnswer',
    ]);
    expect(page.querySelector<HTMLTextAreaElement>('.new-category .json-input__text')!.value).toBe('');
  });

  it('puts a picked file into the new category\'s JSON box', async () => {
    const { fixture, page } = await render();
    const json = '[{"questionSk": "Koľko kolies má autobus?", "questionEn": "How many wheels does a bus have?", "answer": 6}]';

    await startImportedCategory(fixture, page, '');
    const file = page.querySelector<HTMLInputElement>('.new-category .json-input__file')!;
    Object.defineProperty(file, 'files', { value: [new File([json], 'buses.json', { type: 'application/json' })] });
    file.dispatchEvent(new Event('change'));
    await new Promise((resolve) => setTimeout(resolve));
    await fixture.whenStable();

    expect(page.querySelector<HTMLTextAreaElement>('.new-category .json-input__text')!.value).toBe(json);
    expect(page.querySelector<HTMLButtonElement>('.new-category button[type="submit"]')!.disabled).toBe(false);
    http.expectNone({ method: 'POST' });
  });

  it('does not create an imported category without JSON', async () => {
    const { fixture, page } = await render();

    await startImportedCategory(fixture, page, '   ');

    expect(page.querySelector<HTMLButtonElement>('.new-category button[type="submit"]')!.disabled).toBe(true);
    http.expectNone({ method: 'POST' });
  });

  function sourceChip(page: HTMLElement, source: 'none' | 'ai' | 'json'): HTMLButtonElement {
    const index = ['none', 'ai', 'json'].indexOf(source);
    return page.querySelectorAll<HTMLButtonElement>('.question-source .chip')[index];
  }

  it('offers a new category without questions, with AI questions or with imported ones, AI by default', async () => {
    const { page } = await render();

    const chips = [...page.querySelectorAll<HTMLButtonElement>('.question-source .chip')];
    expect(chips.map((chip) => chip.textContent?.trim())).toEqual([
      'admin.newCategory.sources.none',
      'admin.newCategory.sources.ai',
      'admin.newCategory.sources.json',
    ]);
    expect(chips.map((chip) => chip.getAttribute('aria-checked'))).toEqual(['false', 'true', 'false']);
  });

  it('creates an empty category', async () => {
    const { fixture, page } = await render();

    sourceChip(page, 'none').click();
    await fixture.whenStable();
    expect(page.querySelector('.new-category .count')).toBeNull();
    expect(page.querySelector('.new-category .json-input__text')).toBeNull();
    typeName(page, 'sk', 'Lode');
    await fixture.whenStable();

    const submit = page.querySelector<HTMLButtonElement>('.new-category button[type="submit"]')!;
    expect(submit.textContent?.trim()).toBe('admin.newCategory.submit.none');
    submit.click();
    const request = http.expectOne({ method: 'POST', url: '/api/question-categories' });
    expect(request.request.body).toEqual({ nameSk: 'Lode', questionCount: 0 });
    request.flush({
      id: 3,
      name: 'Lode',
      nameSk: 'Lode',
      nameEn: 'Ships',
      questionCount: 0,
      aiQuestionCount: 0,
      questionGeneration: { requested: 0, created: 0, discarded: 0 },
      questionImport: null,
    });
    flushLoad();
    await fixture.whenStable();

    expect(page.querySelector('.notice')).not.toBeNull();
    expect(page.querySelector('.notice__details')).toBeNull();
  });

  it('says the AI translates a missing name only while one of the two is missing', async () => {
    const { fixture, page } = await render();
    const hint = () => page.querySelector('.step .hint')?.textContent?.trim();
    expect(hint()).toBe('admin.newCategory.namesHint');

    typeName(page, 'sk', 'Vtáky');
    await fixture.whenStable();
    expect(hint()).toBe('admin.newCategory.translatedTo.en');

    typeName(page, 'en', 'Birds');
    await fixture.whenStable();
    expect(hint()).toBe('admin.newCategory.namesHint');
  });

  it('sends both names when both are filled in, so the AI need not translate', async () => {
    const { fixture, page } = await render();

    sourceChip(page, 'none').click();
    typeName(page, 'sk', ' Vtáky ');
    typeName(page, 'en', 'Birds');
    await fixture.whenStable();
    page.querySelector<HTMLButtonElement>('.new-category button[type="submit"]')!.click();

    const request = http.expectOne({ method: 'POST', url: '/api/question-categories' });
    expect(request.request.body).toEqual({ nameSk: 'Vtáky', nameEn: 'Birds', questionCount: 0 });
    request.flush({
      id: 3,
      name: 'Vtáky',
      nameSk: 'Vtáky',
      nameEn: 'Birds',
      questionCount: 0,
      aiQuestionCount: 0,
      questionGeneration: { requested: 0, created: 0, discarded: 0 },
      questionImport: null,
    });
    flushLoad();
    await fixture.whenStable();
  });
});
