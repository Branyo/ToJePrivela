import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { Question, QuestionCategory } from '../../core/api/models';
import { GameRulesStore } from '../../core/rules/game-rules-store';
import { TEST_RULES } from '../../core/rules/testing';
import { Admin } from './admin';

const CATEGORIES: QuestionCategory[] = [
  { id: 2, name: 'Sport' },
  { id: 1, name: 'Cars' },
];

const question = (id: number, categoryId: number, source: Question['source']): Question => ({
  id,
  text: `Question ${id}?`,
  answer: '1',
  categoryId,
  categoryName: '',
  badPoints: 3,
  source,
  createdAt: '2026-10-01T10:00:00Z',
  viewCount: 0,
  lastViewedAt: null,
});

describe('Admin', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [Admin],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideTranslateService()],
    });
    http = TestBed.inject(HttpTestingController);
    TestBed.inject(GameRulesStore).rules.set(TEST_RULES);
  });

  afterEach(() => http.verify());

  function flushLoad(questions: Question[] = [question(1, 1, 'Ai'), question(2, 1, 'Manual'), question(3, 2, 'Ai')]) {
    http.expectOne('/api/question-categories').flush(CATEGORIES);
    http.expectOne('/api/questions').flush(questions);
  }

  async function render(questions?: Question[]) {
    const fixture = TestBed.createComponent(Admin);
    fixture.detectChanges();
    flushLoad(questions);
    await fixture.whenStable();
    return { fixture, page: fixture.nativeElement as HTMLElement };
  }

  it('lists the categories by name', async () => {
    const { page } = await render();

    expect([...page.querySelectorAll('.row__name')].map((name) => name.textContent?.trim())).toEqual(['Cars', 'Sport']);
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

  it('deletes a category only after a second tap', async () => {
    const { fixture, page } = await render();

    page.querySelector<HTMLButtonElement>('.row__actions .danger')!.click();
    await fixture.whenStable();
    http.expectNone({ method: 'DELETE' });

    page.querySelector<HTMLButtonElement>('.row__confirm .btn--danger')!.click();
    http.expectOne({ method: 'DELETE', url: '/api/question-categories/1' }).flush(null);
    flushLoad([]);
    await fixture.whenStable();

    expect(page.querySelector('.row__confirm')).toBeNull();
  });

  it('offers to delete AI questions only where there are some', async () => {
    const { page } = await render([question(1, 1, 'Ai'), question(2, 2, 'Manual')]);

    const deleteAi = [...page.querySelectorAll<HTMLButtonElement>('.row__actions .btn--ghost:not(.danger)')];
    expect(deleteAi.map((button) => button.disabled)).toEqual([false, true]);
  });
});
