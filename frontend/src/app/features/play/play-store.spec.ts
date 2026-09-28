import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { GameDetails, Question } from '../../core/api/models';
import { PlayStore } from './play-store';

const game = (overrides: Partial<GameDetails> = {}): GameDetails => ({
  id: 5,
  started: '2026-09-25T18:00:00Z',
  finished: null,
  badCardLimit: 3,
  players: [
    { playerId: 2, name: 'Bo', badPoints: 0, badCards: 0 },
    { playerId: 1, name: 'Ana', badPoints: 0, badCards: 0 },
  ],
  ...overrides,
});

const question: Question = {
  id: 42,
  text: 'How many keys does a piano have?',
  answer: '88',
  categoryId: 1,
  categoryName: 'Music',
  badPoints: 4,
  source: 'Ai',
  createdAt: '2026-09-25T10:00:00Z',
  viewCount: 0,
  lastViewedAt: null,
};

/** Lets pending promise chains run so the next request is on the wire. */
const flush = () => new Promise((resolve) => setTimeout(resolve));

describe('PlayStore', () => {
  let store: PlayStore;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [PlayStore, provideHttpClient(), provideHttpClientTesting()],
    });
    store = TestBed.inject(PlayStore);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  async function startWithQuestion(categoryIds: number[] = []): Promise<void> {
    const started = store.start(5, categoryIds);
    http.expectOne('/api/games/5/details').flush(game());
    await flush();
    http.expectOne((req) => req.url === '/api/questions/random').flush(question);
    await flush();
    http.expectOne('/api/questions/42/views').flush(question);
    await started;
  }

  it('loads the game, draws a question and counts the view', async () => {
    await startWithQuestion();

    expect(store.phase()).toBe('asking');
    expect(store.question()?.id).toBe(42);
    expect(store.questionNumber()).toBe(1);
    expect(store.seats().map((p) => p.playerId)).toEqual([1, 2]);
  });

  it('asks only for the chosen categories', async () => {
    const started = store.start(5, [3, 4]);
    http.expectOne('/api/games/5/details').flush(game());
    await flush();

    const draw = http.expectOne((req) => req.url === '/api/questions/random');
    expect(draw.request.params.getAll('categoryIds')).toEqual(['3', '4']);
    draw.flush(question);
    await flush();
    http.expectOne('/api/questions/42/views').flush(question);
    await started;
  });

  it('reveals the answer only while asking', async () => {
    await startWithQuestion();

    store.reveal();
    expect(store.phase()).toBe('revealed');
  });

  it('skips a question without giving anybody a card', async () => {
    await startWithQuestion();

    const skipped = store.skip();
    http.expectNone('/api/games/5/bad-cards');
    http.expectOne((req) => req.url === '/api/questions/random').flush({ ...question, id: 43 });
    await flush();
    http.expectOne('/api/questions/43/views').flush(question);
    await skipped;

    expect(store.question()?.id).toBe(43);
    expect(store.questionNumber()).toBe(2);
  });

  it('awards the card, waits for it to land, then draws the next question', async () => {
    await startWithQuestion();
    store.reveal();

    let land!: () => void;
    const landing = new Promise<void>((resolve) => (land = resolve));
    const awarded = store.award(2, landing);

    const request = http.expectOne('/api/games/5/bad-cards');
    expect(request.request.body).toEqual({ playerId: 2, questionId: 42 });
    request.flush(
      game({
        players: [
          { playerId: 2, name: 'Bo', badPoints: 4, badCards: 1 },
          { playerId: 1, name: 'Ana', badPoints: 0, badCards: 0 },
        ],
      }),
    );
    await flush();

    expect(store.phase()).toBe('awarding');
    expect(store.seats().find((p) => p.playerId === 2)?.badPoints).toBe(0);

    land();
    await flush();
    expect(store.seats().find((p) => p.playerId === 2)?.badPoints).toBe(4);

    http.expectOne((req) => req.url === '/api/questions/random').flush({ ...question, id: 44 });
    await flush();
    http.expectOne('/api/questions/44/views').flush(question);
    await awarded;

    expect(store.phase()).toBe('asking');
  });

  it('ignores an award before the answer is revealed', async () => {
    await startWithQuestion();

    await store.award(2);

    http.expectNone('/api/games/5/bad-cards');
    expect(store.phase()).toBe('asking');
  });

  it('ends when the server finishes the game at the limit', async () => {
    await startWithQuestion();
    store.reveal();

    const awarded = store.award(1);
    http.expectOne('/api/games/5/bad-cards').flush(game({ finished: '2026-09-25T19:00:00Z' }));
    await awarded;

    expect(store.phase()).toBe('finished');
  });

  it('reports when the categories ran out of questions', async () => {
    const started = store.start(5, []);
    http.expectOne('/api/games/5/details').flush(game());
    await flush();
    http
      .expectOne((req) => req.url === '/api/questions/random')
      .flush({ code: 'Question.NoneAvailable', detail: 'No questions.' }, { status: 404, statusText: 'Not Found' });
    await started;

    expect(store.phase()).toBe('no-questions');
  });

  it('goes straight to the results for a finished game', async () => {
    const started = store.start(5, []);
    http.expectOne('/api/games/5/details').flush(game({ finished: '2026-09-25T19:00:00Z' }));
    await started;

    expect(store.phase()).toBe('finished');
  });

  it('finishes a game early', async () => {
    await startWithQuestion();

    const finished = store.finish();
    http.expectOne('/api/games/5/finish').flush(game({ finished: '2026-09-25T19:00:00Z' }));
    await finished;

    expect(store.phase()).toBe('finished');
  });

  it('flags a player one card away from the limit', async () => {
    await startWithQuestion();

    expect(store.isOnTheEdge({ playerId: 1, name: 'Ana', badPoints: 5, badCards: 2 })).toBe(true);
    expect(store.isOnTheEdge({ playerId: 1, name: 'Ana', badPoints: 5, badCards: 1 })).toBe(false);
  });
});
