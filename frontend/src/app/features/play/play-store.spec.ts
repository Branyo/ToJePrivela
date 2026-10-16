import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { GameDetails, Question } from '../../core/api/models';
import { GameRulesStore } from '../../core/rules/game-rules-store';
import { TEST_RULES } from '../../core/rules/testing';
import { PlayStore } from './play-store';

const game = (overrides: Partial<GameDetails> = {}): GameDetails => ({
  id: 5,
  started: '2026-09-25T18:00:00Z',
  finished: null,
  badCardLimit: 3,
  badPointsMode: 'Question',
  cancelled: false,
  hasResult: false,
  players: [
    { playerId: 2, name: 'Bo', avatar: '🐼', badPoints: 0, badCards: 0, doubles: 0, finalBadPoints: 0, rank: 1, isLoser: false, tooMuchCalls: 0, falseAlarms: 0, correctCalls: 0, caught: 0 },
    { playerId: 1, name: 'Ana', avatar: '🦊', badPoints: 0, badCards: 0, doubles: 0, finalBadPoints: 0, rank: 1, isLoser: false, tooMuchCalls: 0, falseAlarms: 0, correctCalls: 0, caught: 0 },
  ],
  ...overrides,
});

const question: Question = {
  id: 42,
  text: 'Koľko klávesov má klavír?',
  textSk: 'Koľko klávesov má klavír?',
  textEn: 'How many keys does a piano have?',
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
    TestBed.inject(GameRulesStore).rules.set(TEST_RULES);
    store = TestBed.inject(PlayStore);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  async function startWithQuestion(categoryIds: number[] = [], details: GameDetails = game()): Promise<void> {
    const started = store.start(5, categoryIds);
    http.expectOne('/api/games/5/details').flush(details);
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
    expect(store.seats().map((p) => p.playerId)).toEqual([2, 1]); // the order of play the server sent
  });

  it('fetches the current question again in place, keeping the round where it is', async () => {
    await startWithQuestion();
    store.reveal(1);

    const refreshed = store.refreshQuestion();
    http
      .expectOne({ method: 'GET', url: '/api/questions/42' })
      .flush({ ...question, text: 'How many keys does a piano have?', categoryName: 'Music' });
    await refreshed;

    expect(store.question()?.text).toBe('How many keys does a piano have?');
    expect(store.question()?.categoryName).toBe('Music');
    expect(store.phase()).toBe('revealed');
    expect(store.questionNumber()).toBe(1);
  });

  it('keeps the question as it was when fetching it again fails', async () => {
    await startWithQuestion();

    const refreshed = store.refreshQuestion();
    http.expectOne('/api/questions/42').flush(null, { status: 500, statusText: 'Server Error' });
    await refreshed;

    expect(store.question()).toEqual(question);
  });

  it('has nothing to fetch again before the first question', async () => {
    await store.refreshQuestion();

    http.expectNone('/api/questions/42');
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

  it('seats only players who still exist', async () => {
    await startWithQuestion(
      [],
      game({
        players: [
          { playerId: 2, name: 'Bo', avatar: '🐼', badPoints: 0, badCards: 0, doubles: 0, finalBadPoints: 0, rank: 1, isLoser: false, tooMuchCalls: 0, falseAlarms: 0, correctCalls: 0, caught: 0 },
          { playerId: null, name: null, avatar: null, badPoints: 3, badCards: 1, doubles: 0, finalBadPoints: 3, rank: 1, isLoser: false, tooMuchCalls: 0, falseAlarms: 0, correctCalls: 0, caught: 0 },
        ],
      }),
    );

    expect(store.seats().map((p) => p.playerId)).toEqual([2]);
  });

  it('in a chooser game shows only the category until the starting player sets the bad points', async () => {
    await startWithQuestion([], game({ badPointsMode: 'Chooser' }));

    expect(store.phase()).toBe('choosing');
    expect(store.canDouble()).toBe(false);

    store.reveal(1);
    store.choose(6);
    expect(store.phase()).toBe('choosing');

    store.choose(2);
    expect(store.phase()).toBe('asking');
    expect(store.badPoints()).toBe(2);
  });

  it('in a chooser game accepts only worths within the served limits', async () => {
    TestBed.inject(GameRulesStore).rules.set({ ...TEST_RULES, badPoints: { min: 1, max: 3 } });
    await startWithQuestion([], game({ badPointsMode: 'Chooser' }));

    store.choose(4);
    expect(store.phase()).toBe('choosing');

    store.choose(3);
    expect(store.phase()).toBe('asking');
  });

  it('in a chooser game sends the chosen bad points with the card and asks again for the next question', async () => {
    await startWithQuestion([], game({ badPointsMode: 'Chooser' }));
    store.choose(1);
    store.reveal(1);

    const awarded = store.award(2);
    const request = http.expectOne('/api/games/5/bad-cards');
    expect(request.request.body).toEqual({ playerId: 2, calledByPlayerId: 1, questionId: 42, badPoints: 1 });
    request.flush(game({ badPointsMode: 'Chooser' }));
    await flush();
    http.expectOne((req) => req.url === '/api/questions/random').flush({ ...question, id: 44 });
    await flush();
    http.expectOne('/api/questions/44/views').flush(question);
    await awarded;

    expect(store.phase()).toBe('choosing');
    expect(store.chosenBadPoints()).toBeNull();
  });

  it('in a question game uses the stored bad points and never asks for them', async () => {
    await startWithQuestion();

    store.choose(2);
    expect(store.phase()).toBe('asking');
    expect(store.badPoints()).toBe(4);
  });

  it('reveals the answer only while asking', async () => {
    await startWithQuestion();

    store.reveal(1);
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

  it('reveals the answer for the player who called "too much", and goes back to bidding on a mis-tap', async () => {
    await startWithQuestion();

    store.reveal(99);
    expect(store.phase()).toBe('asking');
    expect(store.caller()).toBeNull();

    store.reveal(2);
    expect(store.phase()).toBe('revealed');
    expect(store.caller()?.name).toBe('Bo');

    store.back();
    expect(store.phase()).toBe('asking');
    expect(store.callerId()).toBeNull();

    store.reveal(1);
    expect(store.caller()?.name).toBe('Ana');
  });

  it('lets only the caller or the player before them, whose bid was called, take the card', async () => {
    const seat = (playerId: number, name: string) => ({ ...game().players[0], playerId, name });
    await startWithQuestion([], game({ players: [seat(3, 'Cy'), seat(1, 'Ana'), seat(2, 'Bo')] }));

    store.reveal(3);
    expect(store.takerIds()).toEqual([3, 2]); // round the table: Bo sits before Cy
    expect([1, 2, 3].map((id) => store.canTake(id))).toEqual([false, true, true]);

    await store.award(1);
    http.expectNone('/api/games/5/bad-cards');
    expect(store.phase()).toBe('revealed');

    store.back();
    store.reveal(1);
    expect(store.takerIds()).toEqual([1, 3]);
  });

  it('sends the caller with the card, also when the caller takes it (a false alarm), and forgets them for the next question', async () => {
    await startWithQuestion();
    store.reveal(2);

    const awarded = store.award(2);
    const request = http.expectOne('/api/games/5/bad-cards');
    expect(request.request.body).toEqual({ playerId: 2, calledByPlayerId: 2, questionId: 42 });
    request.flush(game());
    await flush();
    http.expectOne((req) => req.url === '/api/questions/random').flush({ ...question, id: 44 });
    await flush();
    http.expectOne('/api/questions/44/views').flush(question);
    await awarded;

    expect(store.phase()).toBe('asking');
    expect(store.callerId()).toBeNull();
  });

  it('hands the saved game to onSaved once the card is counted', async () => {
    await startWithQuestion();
    store.reveal(1);
    const saved = game({ finished: '2026-09-25T19:00:00Z' });
    const onSaved = vi.fn();

    const awarded = store.award(2, Promise.resolve(), onSaved);
    http.expectOne('/api/games/5/bad-cards').flush(saved);
    await awarded;

    expect(onSaved).toHaveBeenCalledExactlyOnceWith(saved);
  });

  it('never calls onSaved for a card the server refused', async () => {
    await startWithQuestion();
    store.reveal(1);
    const onSaved = vi.fn();

    const awarded = store.award(2, Promise.resolve(), onSaved);
    http.expectOne('/api/games/5/bad-cards').flush({ detail: 'Gone.' }, { status: 409, statusText: 'Conflict' });
    await flush();
    http.expectOne('/api/games/5/details').flush(game({ finished: '2026-09-25T19:00:00Z' }));
    await awarded;

    expect(onSaved).not.toHaveBeenCalled();
  });

  it('awards the card, waits for it to land, then draws the next question', async () => {
    await startWithQuestion();
    store.reveal(1);

    let land!: () => void;
    const landing = new Promise<void>((resolve) => (land = resolve));
    const awarded = store.award(2, landing);

    const request = http.expectOne('/api/games/5/bad-cards');
    expect(request.request.body).toEqual({ playerId: 2, calledByPlayerId: 1, questionId: 42 });
    request.flush(
      game({
        players: [
          { playerId: 2, name: 'Bo', avatar: '🐼', badPoints: 4, badCards: 1, doubles: 0, finalBadPoints: 4, rank: 1, isLoser: false, tooMuchCalls: 0, falseAlarms: 0, correctCalls: 0, caught: 0 },
          { playerId: 1, name: 'Ana', avatar: '🦊', badPoints: 0, badCards: 0, doubles: 0, finalBadPoints: 0, rank: 1, isLoser: false, tooMuchCalls: 0, falseAlarms: 0, correctCalls: 0, caught: 0 },
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

  it('saves a double while bidding without leaving the question', async () => {
    await startWithQuestion();

    const doubled = store.awardDouble(1);
    await flush();
    const request = http.expectOne('/api/games/5/doubles');
    expect(request.request.body).toEqual({ playerId: 1 });
    request.flush(
      game({
        players: [
          { playerId: 2, name: 'Bo', avatar: '🐼', badPoints: 0, badCards: 0, doubles: 0, finalBadPoints: 0, rank: 1, isLoser: false, tooMuchCalls: 0, falseAlarms: 0, correctCalls: 0, caught: 0 },
          { playerId: 1, name: 'Ana', avatar: '🦊', badPoints: 0, badCards: 0, doubles: 1, finalBadPoints: -1, rank: 1, isLoser: false, tooMuchCalls: 0, falseAlarms: 0, correctCalls: 0, caught: 0 },
        ],
      }),
    );
    await doubled;

    expect(store.seats().find((p) => p.playerId === 1)?.doubles).toBe(1);
    expect(store.phase()).toBe('asking');
  });

  it('hands out the card only after the doubles tapped before it are saved', async () => {
    await startWithQuestion();
    store.reveal(1);

    void store.awardDouble(1);
    const awarded = store.award(1);
    await flush();

    http.expectNone('/api/games/5/bad-cards');
    http.expectOne('/api/games/5/doubles').flush(game());
    await flush();

    http.expectOne('/api/games/5/bad-cards').flush(game({ finished: '2026-09-25T19:00:00Z' }));
    await awarded;

    expect(store.phase()).toBe('finished');
  });

  it('takes back a double tapped by mistake', async () => {
    await startWithQuestion();

    const added = store.awardDouble(1);
    await flush();
    http.expectOne('/api/games/5/doubles').flush(
      game({
        players: [
          { playerId: 2, name: 'Bo', avatar: '🐼', badPoints: 0, badCards: 0, doubles: 0, finalBadPoints: 0, rank: 1, isLoser: false, tooMuchCalls: 0, falseAlarms: 0, correctCalls: 0, caught: 0 },
          { playerId: 1, name: 'Ana', avatar: '🦊', badPoints: 0, badCards: 0, doubles: 1, finalBadPoints: -1, rank: 1, isLoser: false, tooMuchCalls: 0, falseAlarms: 0, correctCalls: 0, caught: 0 },
        ],
      }),
    );
    await added;

    const removed = store.removeDouble(1);
    await flush();
    const request = http.expectOne('/api/games/5/doubles/1');
    expect(request.request.method).toBe('DELETE');
    request.flush(game());
    await removed;

    expect(store.seats().find((p) => p.playerId === 1)?.doubles).toBe(0);
  });

  it('does not take back a double the player does not have', async () => {
    await startWithQuestion();

    await store.removeDouble(1);

    http.expectNone('/api/games/5/doubles/1');
  });

  it('ignores a double once the card is on its way', async () => {
    await startWithQuestion();
    store.reveal(1);

    const awarded = store.award(2);
    await store.awardDouble(1);

    http.expectNone('/api/games/5/doubles');
    http.expectOne('/api/games/5/bad-cards').flush(game({ finished: '2026-09-25T19:00:00Z' }));
    await awarded;
  });

  it('ignores an award before the answer is revealed', async () => {
    await startWithQuestion();

    await store.award(2);

    http.expectNone('/api/games/5/bad-cards');
    expect(store.phase()).toBe('asking');
  });

  it('ends when the server finishes the game at the limit', async () => {
    await startWithQuestion();
    store.reveal(1);

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
    await flush();
    http.expectOne('/api/games/5/details').flush(game());
    await started;

    expect(store.phase()).toBe('no-questions');
  });

  it('reloads the game once out of questions, so a first card from another device offers the results', async () => {
    const started = store.start(5, []);
    http.expectOne('/api/games/5/details').flush(game());
    await flush();
    http
      .expectOne((req) => req.url === '/api/questions/random')
      .flush({ code: 'Question.NoneAvailable', detail: 'No questions.' }, { status: 404, statusText: 'Not Found' });
    await flush();
    http.expectOne('/api/games/5/details').flush(game({ hasResult: true }));
    await started;

    expect(store.phase()).toBe('no-questions');
    expect(store.hasResult()).toBe(true);
  });

  it('reloads the game on request, e.g. before asking how to end it', async () => {
    await startWithQuestion();

    const refreshed = store.refreshGame();
    http.expectOne('/api/games/5/details').flush(game({ hasResult: true }));
    await refreshed;

    expect(store.hasResult()).toBe(true);
    expect(store.phase()).toBe('asking');
  });

  it('follows a game finished from another device when reloading it', async () => {
    await startWithQuestion();

    const refreshed = store.refreshGame();
    http.expectOne('/api/games/5/details').flush(game({ hasResult: true, finished: '2026-09-25T19:00:00Z' }));
    await refreshed;

    expect(store.phase()).toBe('finished');
  });

  it('does not reload the game while a double of this device is being saved', async () => {
    await startWithQuestion();

    const doubled = store.awardDouble(1);
    await store.refreshGame();
    http.expectNone('/api/games/5/details');
    http.expectOne('/api/games/5/doubles').flush(game({ hasResult: true }));
    await doubled;

    expect(store.hasResult()).toBe(true);
  });

  it('drops a reload that a double saved meanwhile has overtaken', async () => {
    await startWithQuestion();

    const refreshed = store.refreshGame();
    const doubled = store.awardDouble(1);
    await flush();
    const afterDouble = game({ players: game().players.map((p) => (p.playerId === 1 ? { ...p, doubles: 1, finalBadPoints: -1 } : p)) });
    http.expectOne('/api/games/5/doubles').flush(afterDouble);
    await doubled;
    http.expectOne('/api/games/5/details').flush(game());
    await refreshed;

    expect(store.game()?.players.find((p) => p.playerId === 1)?.doubles).toBe(1);
  });

  it('keeps the game as it was when reloading it fails', async () => {
    await startWithQuestion([], game({ hasResult: true }));

    const refreshed = store.refreshGame();
    http.expectOne('/api/games/5/details').flush({ detail: 'Boom.' }, { status: 500, statusText: 'Server Error' });
    await refreshed;

    expect(store.hasResult()).toBe(true);
    expect(store.error()).toBeNull();
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

  it('has no result to count while the server says there is none', async () => {
    await startWithQuestion();

    expect(store.hasResult()).toBe(false);
  });

  it('has a result to count once the server says so, whatever the seats show', async () => {
    // Only an unknown (deleted) player holds a card: the seats show none, the server still counts a result.
    await startWithQuestion([], game({ hasResult: true }));

    expect(store.hasResult()).toBe(true);
  });

  it('deletes a game', async () => {
    await startWithQuestion();

    const deleted = store.delete();
    const request = http.expectOne('/api/games/5');
    expect(request.request.method).toBe('DELETE');
    request.flush(null, { status: 204, statusText: 'No Content' });

    expect(await deleted).toBe(true);
    expect(store.phase()).toBe('deleted');
    expect(store.game()).toBeNull();
    expect(store.canEnd()).toBe(false);
  });

  it('deletes the game only after the doubles tapped before are saved', async () => {
    await startWithQuestion();

    void store.awardDouble(1);
    const deleted = store.delete();
    await flush();

    http.expectNone('/api/games/5');
    http.expectOne('/api/games/5/doubles').flush(game());
    await flush();

    http.expectOne('/api/games/5').flush(null, { status: 204, statusText: 'No Content' });
    expect(await deleted).toBe(true);
  });

  it('sends nothing else while the game is being deleted', async () => {
    await startWithQuestion();
    store.reveal(1);

    const deleted = store.delete();
    await store.award(1);
    await store.awardDouble(1);

    http.expectNone('/api/games/5/bad-cards');
    http.expectNone('/api/games/5/doubles');
    http.expectOne('/api/games/5').flush(null, { status: 204, statusText: 'No Content' });
    expect(await deleted).toBe(true);
  });

  it('does not delete a game while its card is being handed out', async () => {
    await startWithQuestion();
    store.reveal(1);
    const awarded = store.award(1);

    expect(await store.delete()).toBe(false);

    http.expectNone('/api/games/5');
    http.expectOne('/api/games/5/bad-cards').flush(game({ finished: '2026-09-25T19:00:00Z' }));
    await awarded;
  });

  it('treats a game deleted elsewhere as deleted', async () => {
    await startWithQuestion();

    const deleted = store.delete();
    http
      .expectOne('/api/games/5')
      .flush({ code: 'Game.NotFound', detail: 'Game with id 5 was not found.' }, { status: 404, statusText: 'Not Found' });

    expect(await deleted).toBe(true);
  });

  it('keeps the round where it was when deleting fails', async () => {
    await startWithQuestion();
    store.reveal(1);

    const deleted = store.delete();
    http.expectOne('/api/games/5').flush({ detail: 'Boom.' }, { status: 500, statusText: 'Server Error' });

    expect(await deleted).toBe(false);
    expect(store.phase()).toBe('revealed');
    expect(store.question()?.id).toBe(42);
    expect(store.error()).not.toBeNull();
  });

  it('finishes the game only after the doubles tapped before are saved', async () => {
    await startWithQuestion();
    store.reveal(1);

    void store.awardDouble(1);
    const finished = store.finish();
    await flush();

    http.expectNone('/api/games/5/finish');
    http.expectOne('/api/games/5/doubles').flush(game());
    await flush();

    http.expectOne('/api/games/5/finish').flush(game({ finished: '2026-09-25T19:00:00Z' }));
    await finished;
    expect(store.phase()).toBe('finished');
  });

  it('sends no card while the game is being finished', async () => {
    await startWithQuestion();
    store.reveal(1);

    const finished = store.finish();
    await store.award(1);

    http.expectNone('/api/games/5/bad-cards');
    http.expectOne('/api/games/5/finish').flush(game({ finished: '2026-09-25T19:00:00Z' }));
    await finished;
  });

  it('does not finish a game while its card is being handed out', async () => {
    await startWithQuestion();
    store.reveal(1);
    const awarded = store.award(1);

    await store.finish();

    http.expectNone('/api/games/5/finish');
    http.expectOne('/api/games/5/bad-cards').flush(game());
    await flush();
    http.expectOne((req) => req.url === '/api/questions/random').flush(question);
    await flush();
    http.expectOne('/api/questions/42/views').flush(question);
    await awarded;
  });

  it('keeps the round where it was when finishing fails', async () => {
    await startWithQuestion();
    store.reveal(1);

    const finished = store.finish();
    http.expectOne('/api/games/5/finish').flush({ detail: 'Boom.' }, { status: 500, statusText: 'Server Error' });
    await flush();
    http.expectOne('/api/games/5/details').flush(game());
    await finished;

    expect(store.phase()).toBe('revealed');
    expect(store.question()?.id).toBe(42);
    expect(store.error()).not.toBeNull();
  });

  it('reloads the game when finishing is refused for want of a bad card', async () => {
    await startWithQuestion([], game({ hasResult: true }));

    const finished = store.finish();
    http
      .expectOne('/api/games/5/finish')
      .flush({ code: 'Game.NoBadCardsYet', detail: 'No bad cards yet.' }, { status: 409, statusText: 'Conflict' });
    await flush();
    http.expectOne('/api/games/5/details').flush(game({ hasResult: false }));
    await finished;

    expect(store.hasResult()).toBe(false);
    expect(store.phase()).toBe('asking');
  });

  it('can be ended while the next question is still on its way, which is then dropped unseen', async () => {
    await startWithQuestion();

    const next = store.nextQuestion();
    expect(store.canEnd()).toBe(true);
    const deleted = store.delete();

    http.expectOne('/api/games/5').flush(null, { status: 204, statusText: 'No Content' });
    expect(await deleted).toBe(true);

    http.expectOne((req) => req.url === '/api/questions/random').flush({ ...question, id: 43 });
    await next;

    http.expectNone('/api/questions/43/views');

    expect(store.phase()).toBe('deleted');
    expect(store.question()).toBeNull();
  });

  it('offers a retry when ending fails while the next question was on its way', async () => {
    await startWithQuestion();

    const next = store.nextQuestion();
    const deleted = store.delete();
    http.expectOne('/api/games/5').flush({ detail: 'Boom.' }, { status: 500, statusText: 'Server Error' });
    expect(await deleted).toBe(false);
    expect(store.phase()).toBe('error');

    http.expectOne((req) => req.url === '/api/questions/random').flush({ ...question, id: 43 });
    await next;

    http.expectNone('/api/questions/43/views');
    expect(store.phase()).toBe('error');
  });

  it('does not let a reload still running reopen a game being ended', async () => {
    await startWithQuestion();
    store.reveal(1);

    // A failed card reloads the game; meanwhile its players can end it.
    const awarded = store.award(1);
    http.expectOne('/api/games/5/bad-cards').flush({ detail: 'Boom.' }, { status: 500, statusText: 'Server Error' });
    await flush();
    expect(store.phase()).toBe('loading');

    const finished = store.finish();
    expect(store.phase()).toBe('ending');
    http.expectOne('/api/games/5/details').flush(game());
    await awarded;

    http.expectNone((req) => req.url === '/api/questions/random');
    expect(store.phase()).toBe('ending');
    expect(store.canEnd()).toBe(false);
    expect(store.error()).toBeNull();

    http.expectOne('/api/games/5/finish').flush(game({ finished: '2026-09-25T19:00:00Z' }));
    await finished;
    expect(store.phase()).toBe('finished');
    expect(store.error()).toBeNull();
  });

  it('does not end the game when a double tapped before fails, and keeps its error', async () => {
    await startWithQuestion();
    store.reveal(1);

    void store.awardDouble(1);
    const finished = store.finish();
    await flush();

    http.expectOne('/api/games/5/doubles').flush({ detail: 'Nope.' }, { status: 409, statusText: 'Conflict' });
    await flush();
    http.expectOne('/api/games/5/details').flush(game());
    await finished;

    http.expectNone('/api/games/5/finish');
    expect(store.phase()).toBe('revealed');
    expect(store.error()).not.toBeNull();
  });

  it('cannot end a game that was never loaded', async () => {
    const started = store.start(5, []);
    http.expectOne('/api/games/5/details').flush({ detail: 'Boom.' }, { status: 500, statusText: 'Server Error' });
    await started;

    expect(store.phase()).toBe('error');
    expect(store.canEnd()).toBe(false);
    expect(await store.delete()).toBe(false);
    await store.finish();

    http.expectNone('/api/games/5');
    http.expectNone('/api/games/5/finish');
  });

  it('flags a player one card away from the limit', async () => {
    await startWithQuestion();

    expect(store.isOnTheEdge({ playerId: 1, name: 'Ana', avatar: '🦊', badPoints: 5, badCards: 2, doubles: 0, finalBadPoints: 5, rank: 1, isLoser: false, tooMuchCalls: 0, falseAlarms: 0, correctCalls: 0, caught: 0 })).toBe(true);
    expect(store.isOnTheEdge({ playerId: 1, name: 'Ana', avatar: '🦊', badPoints: 5, badCards: 1, doubles: 0, finalBadPoints: 5, rank: 1, isLoser: false, tooMuchCalls: 0, falseAlarms: 0, correctCalls: 0, caught: 0 })).toBe(false);
  });
});
