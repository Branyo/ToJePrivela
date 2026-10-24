import { registerLocaleData } from '@angular/common';
import localeSk from '@angular/common/locales/sk';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { routes } from '../../app.routes';
import { AccountOverview, GameDetails, GamePlayer, PlayerStatistics } from '../../core/api/models';
import { AuthStore } from '../../core/auth/auth-store';
import { signedInAs } from '../../core/auth/testing';
import { GameRulesStore } from '../../core/rules/game-rules-store';
import { TEST_RULES } from '../../core/rules/testing';

// The app registers it in `app.config.ts`; dates on the page are formatted for the Slovak default.
registerLocaleData(localeSk, 'sk-SK');

const accounts: AccountOverview[] = [
  { id: 5, name: 'Rodina', isAdmin: false, createdAt: '2026-09-01T10:00:00Z', lastSignedInAt: '2026-10-20T18:00:00Z' },
  { id: 6, name: 'Susedia', isAdmin: false, createdAt: '2026-09-02T10:00:00Z', lastSignedInAt: null },
];

const seat = (playerId: number, name: string, finalBadPoints: number, isLoser = false): GamePlayer => ({
  playerId,
  name,
  avatar: '🦊',
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

const game = (id: number, finished: string | null): GameDetails => ({
  id,
  started: '2026-10-20T18:00:00Z',
  finished,
  badCardLimit: 3,
  badPointsMode: 'Question',
  cancelled: false,
  hasResult: true,
  starterId: 1,
  players: [seat(1, 'Zuzka', 5, finished !== null), seat(2, 'Adam', 0)],
});

const statistics: PlayerStatistics = {
  player: { id: 1, name: 'Zuzka', avatar: '🦊' },
  gamesPlayed: 1,
  badCards: 1,
  averageBadCards: 1,
  finalBadPoints: 5,
  averageFinalBadPoints: 5,
  doubles: 0,
  averageDoubles: 0,
  averagePlayers: 2,
  losses: 1,
  sharedLosses: 0,
  lossIndex: 2,
  flawlessVictories: 0,
  luckyFinishes: 0,
  closestEscapes: 0,
  reversals: 0,
  worstGame: { gameId: 4, finished: '2026-10-20T20:00:00Z', finalBadPoints: 5 },
  bestGame: { gameId: 4, finished: '2026-10-20T20:00:00Z', finalBadPoints: 5 },
  gamesWithCalls: 0,
  tooMuchCalls: 0,
  averageTooMuchCalls: null,
  correctCalls: 0,
  falseAlarms: 0,
  callAccuracy: null,
  caught: 0,
  averageCaught: null,
  calledOn: 0,
  averageCalledOn: null,
  caughtCardShare: null,
  criedWolfLosses: 0,
  mostCorrectCalls: null,
};

describe('AccountView', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter(routes, withComponentInputBinding()), provideTranslateService()],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    localStorage.clear();
  });

  async function open(url: string, isAdmin = true) {
    await signedInAs(TestBed.inject(AuthStore), http, { name: 'Brano', isAdmin });
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl(url);
    if (isAdmin) {
      http.expectOne('/api/admin/accounts').flush(accounts);
    }
    return { harness, page: harness.routeNativeElement! };
  }

  const hrefs = (page: HTMLElement, selector: string) =>
    [...page.querySelectorAll(selector)].map((link) => link.getAttribute('href'));

  it("opens on the login's players, each linking to their statistics under the login", async () => {
    const { harness, page } = await open('/accounts/5');
    http.expectOne('/api/admin/accounts/5/players').flush([
      { id: 2, name: 'Zuzka', avatar: '🦊' },
      { id: 1, name: 'Adam', avatar: '🐸' },
    ]);
    await harness.fixture.whenStable();

    expect(TestBed.inject(Router).url).toBe('/accounts/5/players');
    expect(page.querySelector('.account__name')?.textContent).toContain('Rodina');
    expect(hrefs(page, '.tabs__tab')).toEqual(['/accounts/5/players', '/accounts/5/history', '/accounts/5/statistics']);
    expect(hrefs(page, '.player')).toEqual(['/accounts/5/statistics/1', '/accounts/5/statistics/2']);
  });

  it("lists the login's games, every one opening its results there, and offers no new game", async () => {
    const { harness, page } = await open('/accounts/5/history');
    const request = http.expectOne((r) => r.url === '/api/admin/accounts/5/games/history');
    expect(request.request.params.get('status')).toBe('All');
    request.flush({ games: [game(8, null), game(4, '2026-10-20T20:00:00Z')], hasMore: false });
    await harness.fixture.whenStable();

    expect(hrefs(page, '.game')).toEqual(['/accounts/5/games/8', '/accounts/5/games/4']);
    expect(hrefs(page, '.filters .chip')).toEqual([
      '/accounts/5/history',
      '/accounts/5/history?status=running',
      '/accounts/5/history?status=finished',
    ]);

    await harness.navigateByUrl('/accounts/5/history?status=running');
    await harness.fixture.whenStable();
    http.expectOne((r) => r.url === '/api/admin/accounts/5/games/history').flush({ games: [], hasMore: false });
    await harness.fixture.whenStable();

    expect(page.querySelector('.history__empty')).not.toBeNull();
    expect(page.querySelector('.history__empty a')).toBeNull();
  });

  it("shows a game's results read only, with the way back to the login's history", async () => {
    const { harness, page } = await open('/accounts/5/games/4');
    http.expectOne('/api/admin/accounts/5/games/4/details').flush(game(4, '2026-10-20T20:00:00Z'));
    await harness.fixture.whenStable();

    expect(page.querySelector('.board')).not.toBeNull();
    expect(hrefs(page, '.actions a')).toEqual(['/accounts/5/history']);
    expect(page.querySelector('.actions button')).toBeNull();
  });

  it("shows a player's statistics, linking back and to their games under the login", async () => {
    const { harness, page } = await open('/accounts/5/statistics/1');
    http.expectOne('/api/admin/accounts/5/players').flush([statistics.player]);
    http.expectOne('/api/admin/accounts/5/players/1/statistics').flush(statistics);
    await harness.fixture.whenStable();

    expect(hrefs(page, '.roster__item')).toEqual(['/accounts/5/statistics/1']);
    expect(hrefs(page, '.detail__back')).toEqual(['/accounts/5/statistics']);
    expect(hrefs(page, '.tile__link')).toEqual(['/accounts/5/games/4', '/accounts/5/games/4']);
  });

  it('starts the screen afresh when only the login in the address changes', async () => {
    const { harness } = await open('/accounts/5/history');
    http.expectOne((r) => r.url === '/api/admin/accounts/5/games/history').flush({ games: [], hasMore: false });
    await harness.fixture.whenStable();

    await harness.navigateByUrl('/accounts/6/history');
    await harness.fixture.whenStable();

    http.expectOne((r) => r.url === '/api/admin/accounts/6/games/history').flush({ games: [], hasMore: false });
  });

  it('says so for a login that does not exist, and shows none of its screens', async () => {
    const { harness, page } = await open('/accounts/99/history');
    http.expectOne((r) => r.url === '/api/admin/accounts/99/games/history').flush(
      { code: 'Account.NotFound' },
      { status: 404, statusText: 'Not Found' },
    );
    await harness.fixture.whenStable();

    expect(page.querySelector('.error-banner')?.textContent).toContain('errors.api.Account.NotFound');
    expect(page.querySelector('.tabs')).toBeNull();
    expect(page.querySelector('app-history')).toBeNull();
  });

  it('sends a login that is not an admin back to the settings', async () => {
    TestBed.inject(GameRulesStore).rules.set(TEST_RULES);
    await open('/accounts/5/history', false);
    http.expectOne('/api/players').flush([]);

    expect(TestBed.inject(Router).url).toBe('/settings/players');
  });
});
