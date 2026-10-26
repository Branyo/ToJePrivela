import { registerLocaleData } from '@angular/common';
import localeSk from '@angular/common/locales/sk';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, TestRequest, provideHttpClientTesting } from '@angular/common/http/testing';
import { inject } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideTranslateService } from '@ngx-translate/core';
import { AccountData, OwnAccountData } from '../../core/api/account-data';
import { GameDetails, GamePlayer, GameHistory } from '../../core/api/models';
import { History, PAGE_SIZE } from './history';

const player = (playerId: number | null, name: string | null, finalBadPoints: number, isLoser = false): GamePlayer => ({
  playerId,
  name,
  avatar: playerId === null ? null : '🦊',
  badPoints: finalBadPoints,
  badCards: finalBadPoints > 0 ? 1 : 0,
  doubles: 0,
  finalBadPoints,
  rank: 1,
  isLoser,
  tooMuchCalls: 0,
  falseAlarms: 0,
  correctCalls: 0,
  caught: 0,
  previousPlayerId: null,
});

const game = (id: number, overrides: Partial<GameDetails> = {}): GameDetails => ({
  id,
  started: '2026-10-20T18:00:00Z',
  finished: null,
  badCardLimit: 3,
  badPointsMode: 'Question',
  cancelled: false,
  hasResult: false,
  starterId: 1,
  players: [player(1, 'Zuzka', 0), player(2, 'Adam', 0)],
  ...overrides,
});

const finished = game(4, {
  finished: '2026-10-19T20:00:00Z',
  hasResult: true,
  players: [player(1, 'Zuzka', 5, true), player(null, null, 2)],
});

// The app registers it in `app.config.ts`; dates on the page are formatted for the Slovak default.
registerLocaleData(localeSk, 'sk-SK');

describe('History', () => {
  let http: HttpTestingController;
  /** Whether the screen shows another login's data, as an admin's view of it does: read the same way, only read only. */
  let readOnly: boolean;

  beforeEach(() => {
    readOnly = false;
    TestBed.configureTestingModule({
      imports: [History],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        provideTranslateService(),
        {
          provide: AccountData,
          useFactory: () => {
            const own = inject(OwnAccountData);
            return readOnly ? Object.assign(Object.create(own), { readOnly: true }) : own;
          },
        },
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  /** A page request: the first one, or the one after the game `after` (by its id and start). */
  const expectPage = (status: string, after?: GameDetails): TestRequest => {
    const request = http.expectOne((r) => r.url === '/api/games/history');
    const params = request.request.params;
    expect(params.get('status')).toBe(status);
    expect(params.get('take')).toBe(String(PAGE_SIZE));
    expect(params.get('beforeId')).toBe(after ? String(after.id) : null);
    expect(params.get('before')).toBe(after?.started ?? null);
    return request;
  };

  async function render(reply: GameHistory, status?: string): Promise<ComponentFixture<History>> {
    const fixture = TestBed.createComponent(History);
    if (status !== undefined) {
      fixture.componentRef.setInput('status', status);
    }
    fixture.detectChanges();
    expectPage(status === 'running' ? 'Running' : status === 'finished' ? 'Finished' : 'All').flush(reply);
    await settle(fixture);
    return fixture;
  }

  async function settle(fixture: ComponentFixture<History>): Promise<void> {
    await fixture.whenStable();
    fixture.detectChanges();
  }

  const links = (page: HTMLElement) => Array.from(page.querySelectorAll('a.game')).map((a) => a.getAttribute('href'));

  it('lists the games newest first, a running one opening the play screen and a finished one its summary', async () => {
    const page = (await render({ games: [game(7), finished], hasMore: false })).nativeElement as HTMLElement;

    expect(links(page)).toEqual(['/games/7', '/games/4/summary']);
    expect(page.querySelector('.history__more')).toBeNull();
    expect(page.querySelector('.history__empty')).toBeNull();
  });

  it('shows every player in seat order with final bad points and marks the loser', async () => {
    const page = (await render({ games: [finished], hasMore: false })).nativeElement as HTMLElement;

    const names = Array.from(page.querySelectorAll('.player__name')).map((el) => el.textContent?.trim());
    expect(names).toEqual(['Zuzka', 'common.unknownPlayer']);
    expect(Array.from(page.querySelectorAll('.player__points')).map((el) => el.textContent?.trim())).toEqual(['5', '2']);
    expect(page.querySelectorAll('.player--loser')).toHaveLength(1);
    expect(page.querySelector('.player--loser .player__donkey')).not.toBeNull();
    expect(page.querySelector('.game__state')?.textContent).toContain('history.state.finished');
  });

  it('labels a cancelled game as such', async () => {
    const cancelled = game(3, { finished: '2026-10-19T20:00:00Z', cancelled: true });
    const page = (await render({ games: [cancelled], hasMore: false })).nativeElement as HTMLElement;

    expect(page.querySelector('.game__state')?.textContent).toContain('history.state.cancelled');
  });

  it('asks for the page after the last listed game when showing older ones', async () => {
    const last = game(8, { started: '2026-10-20T17:00:00.1234567Z' });
    const fixture = await render({ games: [game(9), last], hasMore: true });
    const page = fixture.nativeElement as HTMLElement;

    (page.querySelector('.history__more') as HTMLButtonElement).click();
    fixture.detectChanges();
    expectPage('All', last).flush({ games: [finished], hasMore: false });
    await settle(fixture);

    expect(links(page)).toEqual(['/games/9', '/games/8', '/games/4/summary']);
    expect(page.querySelector('.history__more')).toBeNull();
  });

  it('starts over from the newest game of another filter', async () => {
    const fixture = await render({ games: [game(9), finished], hasMore: true });

    fixture.componentRef.setInput('status', 'finished');
    fixture.detectChanges();
    expectPage('Finished').flush({ games: [finished], hasMore: false });
    await settle(fixture);

    expect(links(fixture.nativeElement)).toEqual(['/games/4/summary']);
  });

  it('lists every game for a status it does not know', async () => {
    await render({ games: [], hasMore: false }, 'lost');
  });

  it('offers a new game when there is nothing to list', async () => {
    const page = (await render({ games: [], hasMore: false }, 'running')).nativeElement as HTMLElement;

    expect(page.querySelector('.history__empty')?.textContent).toContain('history.empty.Running');
    expect(page.querySelector('.history__empty a')?.getAttribute('href')).toBe('/new');
  });

  it("tells an admin looking at another login that it is that login's history, with no new game", async () => {
    readOnly = true;

    const page = (await render({ games: [], hasMore: false }, 'running')).nativeElement as HTMLElement;

    expect(page.querySelector('.history__empty')?.textContent).toContain('history.emptyOther.Running');
    expect(page.querySelector('.history__empty a')).toBeNull();
  });

  it('shows the error and keeps the listed games when an older page fails', async () => {
    const fixture = await render({ games: [game(9)], hasMore: true });
    const page = fixture.nativeElement as HTMLElement;

    (page.querySelector('.history__more') as HTMLButtonElement).click();
    fixture.detectChanges();
    expectPage('All', game(9)).flush({ title: 'Down' }, { status: 503, statusText: 'Unavailable' });
    await settle(fixture);

    expect(page.querySelector('.error-banner')).not.toBeNull();
    expect(links(page)).toEqual(['/games/9']);
    expect(page.querySelector('.history__more')?.textContent).toContain('history.retry');
  });

  it('tries the first page again after it failed', async () => {
    const fixture = TestBed.createComponent(History);
    fixture.detectChanges();
    expectPage('All').flush({ title: 'Down' }, { status: 503, statusText: 'Unavailable' });
    await settle(fixture);
    const page = fixture.nativeElement as HTMLElement;

    expect(page.querySelector('.history__empty')).toBeNull();
    (page.querySelector('.history__more') as HTMLButtonElement).click();
    fixture.detectChanges();
    expectPage('All').flush({ games: [game(7)], hasMore: false });
    await settle(fixture);

    expect(page.querySelector('.error-banner')).toBeNull();
    expect(links(page)).toEqual(['/games/7']);
  });

  it('continues after a game without a start by its id alone', async () => {
    const last = game(5, { started: null });
    const fixture = await render({ games: [last], hasMore: true });

    (fixture.nativeElement.querySelector('.history__more') as HTMLButtonElement).click();
    fixture.detectChanges();
    expectPage('All', last).flush({ games: [], hasMore: false });
  });

  it('continues after the last game received even when it was already listed', async () => {
    const fixture = await render({ games: [game(9), game(8)], hasMore: true });
    const more = () => (fixture.nativeElement.querySelector('.history__more') as HTMLButtonElement).click();

    // Game 9's start was moved back meanwhile: it ends the next page, where it is not listed again.
    const moved = game(9, { started: '2026-10-19T08:00:00Z' });
    more();
    fixture.detectChanges();
    expectPage('All', game(8)).flush({ games: [finished, moved], hasMore: true });
    await settle(fixture);

    more();
    fixture.detectChanges();
    expectPage('All', moved).flush({ games: [game(2)], hasMore: false });
    await settle(fixture);

    expect(links(fixture.nativeElement)).toEqual(['/games/9', '/games/8', '/games/4/summary', '/games/2']);
  });

  it('keeps its place after an empty page', async () => {
    const fixture = await render({ games: [game(9)], hasMore: true });
    const more = () => (fixture.nativeElement.querySelector('.history__more') as HTMLButtonElement).click();

    more();
    fixture.detectChanges();
    expectPage('All', game(9)).flush({ games: [], hasMore: true });
    await settle(fixture);

    more();
    fixture.detectChanges();
    expectPage('All', game(9)).flush({ games: [], hasMore: false });
  });

  it('starts the next filter from its newest game, not after the last game of the previous one', async () => {
    const fixture = await render({ games: [game(9)], hasMore: true });

    fixture.componentRef.setInput('status', 'running');
    fixture.detectChanges();
    expectPage('Running').flush({ games: [game(7)], hasMore: true });
    await settle(fixture);

    (fixture.nativeElement.querySelector('.history__more') as HTMLButtonElement).click();
    fixture.detectChanges();
    expectPage('Running', game(7)).flush({ games: [], hasMore: false });
  });

  it('lists a game that comes again on a later page once', async () => {
    const fixture = await render({ games: [game(9), game(8)], hasMore: true });

    (fixture.nativeElement.querySelector('.history__more') as HTMLButtonElement).click();
    fixture.detectChanges();
    expectPage('All', game(8)).flush({ games: [game(9), finished], hasMore: false });
    await settle(fixture);

    expect(links(fixture.nativeElement)).toEqual(['/games/9', '/games/8', '/games/4/summary']);
  });
});
