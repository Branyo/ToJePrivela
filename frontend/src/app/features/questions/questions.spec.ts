import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { provideTranslateService } from '@ngx-translate/core';
import { Question, QuestionCategory } from '../../core/api/models';
import { LanguageService } from '../../core/i18n/language';
import { GameRulesStore } from '../../core/rules/game-rules-store';
import { TEST_RULES } from '../../core/rules/testing';
import { Questions } from './questions';

const CATEGORIES: QuestionCategory[] = [
  { id: 2, name: 'Šport', nameSk: 'Šport', nameEn: 'Sport', questionCount: 1, aiQuestionCount: 1 },
  { id: 1, name: 'Autá', nameSk: 'Autá', nameEn: 'Cars', questionCount: 2, aiQuestionCount: 1 },
];

const question = (id: number, categoryId: number, extra: Partial<Question> = {}): Question => ({
  id,
  text: `Otázka číslo ${id}?`,
  textSk: `Otázka číslo ${id}?`,
  textEn: `Question number ${id}?`,
  answer: `${id}00`,
  categoryId,
  categoryName: '',
  badPoints: 3,
  source: 'Ai',
  createdAt: `2026-10-0${id}T10:00:00Z`,
  viewCount: 0,
  lastViewedAt: null,
  ...extra,
});

/** The questions of the category Autá. */
const CARS = [question(1, 1), question(2, 1, { source: 'Manual', textEn: null })];

describe('Questions', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [Questions],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([]), provideTranslateService()],
    });
    http = TestBed.inject(HttpTestingController);
    TestBed.inject(GameRulesStore).rules.set(TEST_RULES);
  });

  afterEach(() => http.verify());

  /** Answers the fetch of the categories and, when one is picked, of its questions. */
  function flushLoad(category: number | undefined, questions: Question[] = CARS, categories = CATEGORIES) {
    http.expectOne('/api/question-categories').flush(categories);
    if (category !== undefined) {
      http.expectOne(`/api/questions?categoryId=${category}`).flush(questions);
    }
  }

  async function render(category?: number) {
    const fixture = TestBed.createComponent(Questions);
    if (category !== undefined) {
      fixture.componentRef.setInput('category', String(category));
    }
    fixture.detectChanges();
    flushLoad(category);
    await fixture.whenStable();
    return { fixture, page: fixture.nativeElement as HTMLElement };
  }

  const texts = (page: HTMLElement, selector: string) =>
    [...page.querySelectorAll(selector)].map((element) => element.textContent?.replace(/\s+/g, ' ').trim());

  const button = (root: ParentNode, text: string) =>
    [...root.querySelectorAll<HTMLButtonElement>('button')].find((candidate) => candidate.textContent?.includes(text))!;

  async function type(fixture: { whenStable(): Promise<unknown> }, field: HTMLInputElement | HTMLTextAreaElement, value: string) {
    field.value = value;
    field.dispatchEvent(new Event('input'));
    await fixture.whenStable();
  }

  it('lists the categories by name with their question counts and asks to pick one', async () => {
    const { page } = await render();

    expect(texts(page, '.categories .chip')).toEqual(['Autá 2', 'Šport 1']);
    expect(page.querySelector('.list')).toBeNull();
    expect(page.textContent).toContain('questionAdmin.pickCategory');
  });

  it("fetches only the picked category's questions, and only another one's once it is picked", async () => {
    const { fixture } = await render(1);

    fixture.componentRef.setInput('category', '2');
    fixture.detectChanges();
    await fixture.whenStable();

    http.expectNone('/api/question-categories');
    http.expectOne('/api/questions?categoryId=2').flush([question(3, 2)]);
  });

  it('ignores an id in the URL that the API could not take, and still lists the categories', async () => {
    const fixture = TestBed.createComponent(Questions);
    fixture.componentRef.setInput('category', '3000000000');
    fixture.detectChanges();
    flushLoad(undefined);
    await fixture.whenStable();
    const page = fixture.nativeElement as HTMLElement;

    expect(texts(page, '.categories .chip')).toEqual(['Autá 2', 'Šport 1']);
    expect(page.textContent).toContain('questionAdmin.pickCategory');
  });

  it("still lists the categories when the questions of the one in the URL cannot be fetched", async () => {
    const fixture = TestBed.createComponent(Questions);
    fixture.componentRef.setInput('category', '1');
    fixture.detectChanges();
    http.expectOne('/api/question-categories').flush(CATEGORIES);
    http.expectOne('/api/questions?categoryId=1').flush(null, { status: 400, statusText: 'Bad Request' });
    await fixture.whenStable();
    const page = fixture.nativeElement as HTMLElement;

    expect(page.querySelector('[role="alert"]')).not.toBeNull();
    expect(texts(page, '.categories .chip')).toEqual(['Autá 2', 'Šport 1']);
  });

  it('links to the categories tab when there are no categories', async () => {
    const fixture = TestBed.createComponent(Questions);
    fixture.detectChanges();
    flushLoad(undefined, [], []);
    await fixture.whenStable();
    const page = fixture.nativeElement as HTMLElement;

    expect(page.textContent).toContain('questionAdmin.noCategories');
    expect(page.querySelector('a')?.getAttribute('href')).toBe('/settings/categories');
  });

  it('offers to try again instead of loading forever when the first fetch fails', async () => {
    const fixture = TestBed.createComponent(Questions);
    fixture.detectChanges();
    http.expectOne('/api/question-categories').flush(null, { status: 503, statusText: 'Service Unavailable' });
    await fixture.whenStable();
    const page = fixture.nativeElement as HTMLElement;

    expect(page.querySelector('[role="alert"]')).not.toBeNull();
    expect(page.textContent).not.toContain('questionAdmin.loading');

    button(page, 'questionAdmin.retry').click();
    await fixture.whenStable();
    flushLoad(undefined);
    await fixture.whenStable();

    expect(page.querySelector('[role="alert"]')).toBeNull();
    expect(texts(page, '.categories .chip')).toEqual(['Autá 2', 'Šport 1']);
  });

  it("offers to try again when a category's questions cannot be fetched after the categories were", async () => {
    const { fixture, page } = await render(1);

    fixture.componentRef.setInput('category', '2');
    fixture.detectChanges();
    await fixture.whenStable();
    http.expectOne('/api/questions?categoryId=2').flush(null, { status: 503, statusText: 'Service Unavailable' });
    await fixture.whenStable();

    expect(page.querySelector('[role="alert"]')).not.toBeNull();
    expect(page.textContent).not.toContain('questionAdmin.loading');

    button(page, 'questionAdmin.retry').click();
    await fixture.whenStable();
    flushLoad(2, [question(3, 2)]);
    await fixture.whenStable();

    expect(page.querySelector('[role="alert"]')).toBeNull();
    expect(texts(page, '.text[lang="sk"]')).toEqual(['SK Otázka číslo 3?']);
  });

  it('shows only the newest fetch when fetches overlap', async () => {
    const { fixture, page } = await render(1);

    await TestBed.inject(LanguageService).use('en');
    fixture.detectChanges();
    await fixture.whenStable();
    const categories = http.expectOne('/api/question-categories');
    const stale = http.expectOne('/api/questions?categoryId=1');
    fixture.componentRef.setInput('category', '2');
    fixture.detectChanges();
    await fixture.whenStable();

    expect(stale.cancelled).toBe(true);
    categories.flush(CATEGORIES);
    http.expectOne('/api/questions?categoryId=2').flush([question(3, 2)]);
    await fixture.whenStable();

    expect(texts(page, '.text[lang="sk"]')).toEqual(['SK Otázka číslo 3?']);
  });

  it('puts the picked category into the URL', async () => {
    const { page } = await render();
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    button(page, 'Šport').click();

    expect(navigate).toHaveBeenCalledWith([], { queryParams: { category: 2 } });
  });

  it("shows both texts of the category's questions, newest first, with their answers hidden", async () => {
    const { page } = await render(1);

    expect(texts(page, '.text[lang="sk"]')).toEqual(['SK Otázka číslo 2?', 'SK Otázka číslo 1?']);
    const english = texts(page, '.text[lang="en"]');
    expect(english[0]).toContain('questionAdmin.noEnglish');
    expect(english[1]).toBe('EN Question number 1?');
    expect(page.textContent).not.toContain('100');
    expect(page.querySelectorAll('.show-answer').length).toBe(2);
  });

  it('sorts by time, not by the text of the timestamps', async () => {
    const fixture = TestBed.createComponent(Questions);
    fixture.componentRef.setInput('category', '1');
    fixture.detectChanges();
    flushLoad(1, [
      question(1, 1, { createdAt: '2026-10-01T10:00:00.5Z' }),
      question(2, 1, { createdAt: '2026-10-01T10:00:00Z' }),
    ]);
    await fixture.whenStable();

    expect(texts(fixture.nativeElement, '.text[lang="sk"]')).toEqual(['SK Otázka číslo 1?', 'SK Otázka číslo 2?']);
  });

  it('shows an answer only for the question asked, and hides it again', async () => {
    const { fixture, page } = await render(1);

    page.querySelectorAll<HTMLButtonElement>('.show-answer')[1].click();
    await fixture.whenStable();

    expect(texts(page, '.answer')).toEqual(['questionAdmin.answer: 100']);
    expect(page.textContent).not.toContain('200');

    button(page, 'questionAdmin.hideAnswer').click();
    await fixture.whenStable();

    expect(page.querySelector('.answer')).toBeNull();
  });

  it('adds a manual question to the category with both texts, the answer and the picked bad points', async () => {
    const { fixture, page } = await render(1);

    button(page, 'questionAdmin.add').click();
    await fixture.whenStable();
    const form = page.querySelector('app-question-form')!;
    await type(fixture, form.querySelector('textarea[name="textSk"]')!, '  Koľko kolies má auto?  ');
    await type(fixture, form.querySelector('textarea[name="textEn"]')!, 'How many wheels has a car?');
    await type(fixture, form.querySelector('input[name="answer"]')!, '4');
    button(form, '2').click();
    await fixture.whenStable();
    button(form, 'questionAdmin.form.save').click();
    await fixture.whenStable();

    const create = http.expectOne('/api/questions');
    expect(create.request.method).toBe('POST');
    expect(create.request.body).toEqual({
      textSk: 'Koľko kolies má auto?',
      textEn: 'How many wheels has a car?',
      answer: '4',
      badPoints: 2,
      categoryId: 1,
    });
    create.flush(question(4, 1, { source: 'Manual' }));
    flushLoad(1);
    await fixture.whenStable();

    expect(page.querySelector('app-question-form')).toBeNull();
    expect(page.textContent).toContain('questionAdmin.created');
  });

  it('asks for the bad points of a new question instead of sending it without them', async () => {
    const { fixture, page } = await render(1);

    button(page, 'questionAdmin.add').click();
    await fixture.whenStable();
    const form = page.querySelector('app-question-form')!;
    await type(fixture, form.querySelector('textarea[name="textSk"]')!, 'Koľko kolies má auto?');
    await type(fixture, form.querySelector('textarea[name="textEn"]')!, 'How many wheels has a car?');
    await type(fixture, form.querySelector('input[name="answer"]')!, '4');
    button(form, 'questionAdmin.form.save').click();
    await fixture.whenStable();

    expect(form.querySelector('[role="alert"]')?.textContent).toContain('questionAdmin.form.errors.badPoints');
  });

  it('refuses texts the backend would refuse, without asking it', async () => {
    const { fixture, page } = await render(1);

    button(page, 'questionAdmin.add').click();
    await fixture.whenStable();
    const form = page.querySelector('app-question-form')!;
    await type(fixture, form.querySelector('textarea[name="textSk"]')!, 'Koľko kolies má auto?');
    await type(fixture, form.querySelector('textarea[name="textEn"]')!, 'Short?');
    button(form, 'questionAdmin.form.save').click();
    await fixture.whenStable();

    expect(form.querySelector('[role="alert"]')?.textContent).toContain('questionAdmin.form.errors.textEn');
  });

  it('rolls random bad points other than the current ones', async () => {
    const { fixture, page } = await render(1);
    vi.spyOn(Math, 'random').mockReturnValue(0);

    button(page, 'questionAdmin.edit').click();
    await fixture.whenStable();
    const form = page.querySelector('app-question-form')!;
    button(form, 'questionAdmin.form.roll').click();
    await fixture.whenStable();

    // The question has 3, so the dice picks the first of the others: 1.
    expect(texts(form as HTMLElement, '.bad-points .chip--on')).toEqual(['1']);
  });

  it('saves an edited question in its category and keeps the form open with the reason when refused', async () => {
    const { fixture, page } = await render(1);

    page.querySelectorAll<HTMLButtonElement>('.actions button')[2].click(); // edit the older one, question 1
    await fixture.whenStable();
    const form = page.querySelector('app-question-form')!;
    expect(form.textContent).toContain('questionAdmin.form.aiNote');
    expect(form.querySelector<HTMLInputElement>('input[name="answer"]')!.value).toBe('100');

    await type(fixture, form.querySelector('input[name="answer"]')!, '1,5');
    button(form, 'questionAdmin.form.save').click();
    await fixture.whenStable();

    const refused = http.expectOne('/api/questions/1');
    expect(refused.request.method).toBe('PUT');
    expect(refused.request.body).toEqual({
      textSk: 'Otázka číslo 1?',
      textEn: 'Question number 1?',
      answer: '1,5',
      badPoints: 3,
      categoryId: 1,
    });
    refused.flush({ code: 'Request.Invalid', detail: 'Answer should be a number.' }, { status: 400, statusText: 'Bad Request' });
    await fixture.whenStable();

    // Untranslated codes show the backend's own text.
    expect(page.querySelector('app-question-form [role="alert"]')?.textContent).toContain('Answer should be a number.');

    await type(fixture, form.querySelector('input[name="answer"]')!, '1.5');
    button(form, 'questionAdmin.form.save').click();
    await fixture.whenStable();

    http.expectOne('/api/questions/1').flush(null, { status: 204, statusText: 'No Content' });
    flushLoad(1);
    await fixture.whenStable();

    expect(page.querySelector('app-question-form')).toBeNull();
    expect(page.textContent).toContain('questionAdmin.updated');
  });

  it('fills a form opened again before the saved question arrives with the saved texts once they do', async () => {
    const { fixture, page } = await render(1);

    page.querySelectorAll<HTMLButtonElement>('.actions button')[2].click(); // edit question 1
    await fixture.whenStable();
    await type(fixture, page.querySelector('app-question-form input[name="answer"]')!, '7');
    button(page.querySelector('app-question-form')!, 'questionAdmin.form.save').click();
    await fixture.whenStable();
    http.expectOne('/api/questions/1').flush(null, { status: 204, statusText: 'No Content' });
    await fixture.whenStable();

    page.querySelectorAll<HTMLButtonElement>('.actions button')[2].click(); // again, while the list is still the old one
    await fixture.whenStable();
    expect(page.querySelector<HTMLInputElement>('app-question-form input[name="answer"]')!.value).toBe('100');

    flushLoad(1, [question(1, 1, { answer: '7', source: 'Manual' }), CARS[1]]);
    await fixture.whenStable();

    expect(page.querySelector<HTMLInputElement>('app-question-form input[name="answer"]')!.value).toBe('7');
  });

  it('fetches the missing rules again when a question is saved', async () => {
    const rules = TestBed.inject(GameRulesStore).rules;
    rules.set(null);
    const fixture = TestBed.createComponent(Questions);
    fixture.componentRef.setInput('category', '1');
    fixture.detectChanges();
    http.expectOne('/api/rules').flush(null, { status: 503, statusText: 'Service Unavailable' });
    flushLoad(1);
    await fixture.whenStable();
    const page = fixture.nativeElement as HTMLElement;

    button(page, 'questionAdmin.add').click();
    await fixture.whenStable();
    const form = page.querySelector('app-question-form')!;
    expect(form.querySelectorAll('.bad-points .chip').length).toBe(0);

    button(form, 'questionAdmin.form.save').click();
    http.expectOne('/api/rules').flush(TEST_RULES);
    await fixture.whenStable();

    expect(form.querySelectorAll('.bad-points .chip').length).toBeGreaterThan(0);
    expect(form.querySelector('[role="alert"]')?.textContent).toContain('questionAdmin.form.errors.textSk');
  });

  it('drops the outcome of a save that arrives after another category was picked', async () => {
    const { fixture, page } = await render(1);

    page.querySelectorAll<HTMLButtonElement>('.actions button')[2].click();
    await fixture.whenStable();
    button(page.querySelector('app-question-form')!, 'questionAdmin.form.save').click();
    await fixture.whenStable();
    const save = http.expectOne('/api/questions/1');

    fixture.componentRef.setInput('category', '2');
    fixture.detectChanges();
    await fixture.whenStable();
    http.expectOne('/api/questions?categoryId=2').flush([question(3, 2)]);
    await fixture.whenStable();
    save.flush(null, { status: 204, statusText: 'No Content' });
    await fixture.whenStable();

    // No fetch follows (`http.verify()`), no notice, and the list can be worked on again.
    expect(page.textContent).not.toContain('questionAdmin.updated');
    expect(button(page, 'questionAdmin.add').disabled).toBe(false);
  });

  it('keeps what was typed when the questions are fetched again during an edit', async () => {
    const { fixture, page } = await render(1);

    button(page, 'questionAdmin.edit').click();
    await fixture.whenStable();
    const form = page.querySelector('app-question-form')!;
    await type(fixture, form.querySelector('input[name="answer"]')!, '7');

    await TestBed.inject(LanguageService).use('en');
    fixture.detectChanges();
    await fixture.whenStable();
    flushLoad(1);
    await fixture.whenStable();

    expect(page.querySelector<HTMLInputElement>('app-question-form input[name="answer"]')!.value).toBe('7');
  });

  it('offers no other edit or deletion while a question is being written', async () => {
    const { fixture, page } = await render(1);

    button(page, 'questionAdmin.add').click();
    await fixture.whenStable();

    const actions = [...page.querySelectorAll<HTMLButtonElement>('.actions button')];
    expect(actions.length).toBe(4);
    expect(actions.every((action) => action.disabled)).toBe(true);
  });

  it('deletes a question only after a second tap', async () => {
    const { fixture, page } = await render(1);

    button(page, 'questionAdmin.delete').click();
    await fixture.whenStable();
    http.expectNone('/api/questions/2');

    button(page.querySelector('.confirm')!, 'questionAdmin.delete').click();
    await fixture.whenStable();

    const deletion = http.expectOne('/api/questions/2');
    expect(deletion.request.method).toBe('DELETE');
    deletion.flush(null, { status: 204, statusText: 'No Content' });
    flushLoad(1, [question(1, 1)]);
    await fixture.whenStable();

    expect(page.querySelectorAll('.list .item').length).toBe(1);
    expect(page.textContent).toContain('questionAdmin.deleted');
  });

  it('fetches the list again and closes the confirmation when a deletion fails', async () => {
    const { fixture, page } = await render(1);

    button(page, 'questionAdmin.delete').click();
    await fixture.whenStable();
    button(page.querySelector('.confirm')!, 'questionAdmin.delete').click();
    await fixture.whenStable();
    http
      .expectOne('/api/questions/2')
      .flush({ code: 'Question.NotFound', detail: 'Gone.' }, { status: 404, statusText: 'Not Found' });
    flushLoad(1, [question(1, 1)]);
    await fixture.whenStable();

    expect(page.querySelector('[role="alert"]')).not.toBeNull();
    expect(page.querySelector('.confirm')).toBeNull();
    expect(page.querySelectorAll('.list .item').length).toBe(1);
  });

  it('keeps the question when the deletion is called off', async () => {
    const { fixture, page } = await render(1);

    button(page, 'questionAdmin.delete').click();
    await fixture.whenStable();
    button(page, 'questionAdmin.keep').click();
    await fixture.whenStable();

    expect(page.querySelector('.confirm')).toBeNull();
  });
});
