import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { provideTranslateService } from '@ngx-translate/core';
import { Question, QuestionCategory } from '../../core/api/models';
import { GameRulesStore } from '../../core/rules/game-rules-store';
import { TEST_RULES } from '../../core/rules/testing';
import { Questions } from './questions';

const CATEGORIES: QuestionCategory[] = [
  { id: 2, name: 'Šport', nameSk: 'Šport', nameEn: 'Sport' },
  { id: 1, name: 'Autá', nameSk: 'Autá', nameEn: 'Cars' },
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

const QUESTIONS = [question(1, 1), question(2, 1, { source: 'Manual', textEn: null }), question(3, 2)];

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

  function flushLoad(questions: Question[] = QUESTIONS) {
    http.expectOne('/api/question-categories').flush(CATEGORIES);
    http.expectOne('/api/questions').flush(questions);
  }

  async function render(category?: number) {
    const fixture = TestBed.createComponent(Questions);
    if (category !== undefined) {
      fixture.componentRef.setInput('category', String(category));
    }
    fixture.detectChanges();
    flushLoad();
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
    flushLoad();
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
    flushLoad();
    await fixture.whenStable();

    expect(page.querySelector('app-question-form')).toBeNull();
    expect(page.textContent).toContain('questionAdmin.updated');
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
    flushLoad([question(1, 1), question(3, 2)]);
    await fixture.whenStable();

    expect(page.querySelectorAll('.list .item').length).toBe(1);
    expect(page.textContent).toContain('questionAdmin.deleted');
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
