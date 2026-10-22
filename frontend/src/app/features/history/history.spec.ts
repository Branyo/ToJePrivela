import { registerLocaleData } from '@angular/common';
import localeSk from '@angular/common/locales/sk';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, TestRequest, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideTranslateService } from '@ngx-translate/core';
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

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [History],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([]), provideTranslateService()],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  const expectPage = (status: string, skip: number): TestRequest => {
    const request = http.expectOne((r) => r.url === '/api/games/history');
    expect(request.request.params.get('status')).toBe(status);
    expect(request.request.params.get('skip')).toBe(String(skip));
    expect(request.request.params.get('take')).toBe(String(PAGE_SIZE));
    return request;
  };

  async function render(reply: GameHistory, status?: string): Promise<ComponentFixture<History>> {
    const fixture = TestBed.createComponent(History);
    if (status !== undefined) {
      fixture.componentRef.setInput('status', status);
    }
    fixture.detectChanges();
    expectPage(status === 'running' ? 'Running' : status === 'finished' ? 'Finished' : 'All', 0).flush(reply);
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

  it('asks for the page after the listed games when showing older ones', async () => {
    const fixture = await render({ games: [game(9), game(8)], hasMore: true });
    const page = fixture.nativeElement as HTMLElement;

    (page.querySelector('.history__more') as HTMLButtonElement).click();
    fixture.detectChanges();
    expectPage('All', 2).flush({ games: [finished], hasMore: false });
    await settle(fixture);

    expect(links(page)).toEqual(['/games/9', '/games/8', '/games/4/summary']);
    expect(page.querySelector('.history__more')).toBeNull();
  });

  it('starts over from the newest game of another filter', async () => {
    const fixture = await render({ games: [game(9), finished], hasMore: true });

    fixture.componentRef.setInput('status', 'finished');
    fixture.detectChanges();
    expectPage('Finished', 0).flush({ games: [finished], hasMore: false });
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

  it('shows the error and keeps the listed games when an older page fails', async () => {
    const fixture = await render({ games: [game(9)], hasMore: true });
    const page = fixture.nativeElement as HTMLElement;

    (page.querySelector('.history__more') as HTMLButtonElement).click();
    fixture.detectChanges();
    expectPage('All', 1).flush({ title: 'Down' }, { status: 503, statusText: 'Unavailable' });
    await settle(fixture);

    expect(page.querySelector('.error-banner')).not.toBeNull();
    expect(links(page)).toEqual(['/games/9']);
    expect(page.querySelector('.history__more')).not.toBeNull();
  });
});
