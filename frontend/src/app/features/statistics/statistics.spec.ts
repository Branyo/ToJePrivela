import { registerLocaleData } from '@angular/common';
import localeSk from '@angular/common/locales/sk';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideTranslateService } from '@ngx-translate/core';
import { Player, PlayerStatistics } from '../../core/api/models';
import { NONE, Statistics } from './statistics';

const players: Player[] = [
  { id: 2, name: 'Zuzka', avatar: '🦊' },
  { id: 1, name: 'Čaba', avatar: '🐙' },
  { id: 3, name: 'Adam', avatar: '🐸' },
];

const statistics = (overrides: Partial<PlayerStatistics> = {}): PlayerStatistics => ({
  player: players[0],
  gamesPlayed: 3,
  badCards: 7,
  averageBadCards: 7 / 3,
  finalBadPoints: 12,
  averageFinalBadPoints: 4,
  doubles: 1,
  averageDoubles: 1 / 3,
  averagePlayers: 3.5,
  losses: 1,
  sharedLosses: 0,
  lossIndex: 1.25,
  flawlessVictories: 1,
  luckyFinishes: 0,
  closestEscapes: 2,
  reversals: 0,
  worstGame: { gameId: 9, finished: '2026-09-25T19:00:00Z', finalBadPoints: 8 },
  bestGame: { gameId: 4, finished: '2026-09-20T19:00:00Z', finalBadPoints: 0 },
  gamesWithCalls: 2,
  tooMuchCalls: 6,
  averageTooMuchCalls: 3,
  correctCalls: 4,
  falseAlarms: 2,
  callAccuracy: 2 / 3,
  caught: 3,
  averageCaught: 1.5,
  calledOn: 5,
  averageCalledOn: 2.5,
  caughtCardShare: 0.6,
  criedWolfLosses: 1,
  mostCorrectCalls: { gameId: 7, finished: '2026-09-24T19:00:00Z', count: 3 },
  ...overrides,
});

// The app registers it in `app.config.ts`; dates on the page are formatted for the Slovak default.
registerLocaleData(localeSk, 'sk-SK');

describe('Statistics', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [Statistics],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([]), provideTranslateService()],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  async function render(playerId?: number, reply?: PlayerStatistics): Promise<ComponentFixture<Statistics>> {
    const fixture = TestBed.createComponent(Statistics);
    if (playerId !== undefined) {
      fixture.componentRef.setInput('playerId', String(playerId));
    }
    fixture.detectChanges();
    http.expectOne('/api/players').flush(players);
    if (playerId !== undefined && reply) {
      http.expectOne(`/api/players/${playerId}/statistics`).flush(reply);
    }
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture;
  }

  const metric = (page: HTMLElement, key: string) =>
    page.querySelector(`[data-metric="${key}"] .tile__value`)?.textContent?.trim();

  it('lists the players alphabetically by the Slovak rules', async () => {
    const page = (await render()).nativeElement as HTMLElement;

    const names = Array.from(page.querySelectorAll('.roster__name')).map((el) => el.textContent?.trim());
    expect(names).toEqual(['Adam', 'Čaba', 'Zuzka']);
    expect(page.querySelector('.tile')).toBeNull();
  });

  it("shows the picked player's totals and averages rounded to two decimals", async () => {
    const page = (await render(2, statistics())).nativeElement as HTMLElement;

    expect(page.querySelector('.detail__head h2')?.textContent).toContain('Zuzka');
    expect(metric(page, 'gamesPlayed')).toBe('3');
    expect(metric(page, 'badCards')).toBe('7');
    expect(page.querySelector('[data-metric="badCards"] .tile__average')).not.toBeNull();
    expect(metric(page, 'averagePlayers')).toBe('3,50');
    expect(metric(page, 'lossIndex')).toBe('1,25');
    expect(metric(page, 'worstGame')).toBe('8');
    expect(page.querySelector('[data-metric="worstGame"] a')?.getAttribute('href')).toBe('/games/9/summary');
  });

  it('shows the call figures as counts and percentages, from the games with calls', async () => {
    const page = (await render(2, statistics())).nativeElement as HTMLElement;

    expect(metric(page, 'tooMuchCalls')).toBe('6');
    expect(metric(page, 'correctCalls')).toBe('4');
    expect(metric(page, 'falseAlarms')).toBe('2');
    expect(metric(page, 'callAccuracy')?.replace(/\s/g, ' ')).toBe('67 %');
    expect(metric(page, 'caught')).toBe('3');
    expect(metric(page, 'calledOn')).toBe('5');
    expect(metric(page, 'caughtCardShare')?.replace(/\s/g, ' ')).toBe('60 %');
    expect(metric(page, 'criedWolfLosses')).toBe('1');
    expect(metric(page, 'mostCorrectCalls')).toBe('3');
    expect(page.querySelector('[data-metric="mostCorrectCalls"] a')?.getAttribute('href')).toBe('/games/7/summary');
    expect(page.querySelectorAll('.group__note')).toHaveLength(2);
    expect(page.querySelector('[data-metric="mostCorrectCalls"] .tile__note')).not.toBeNull();
  });

  it('shows the cried-wolf losses among the figures from the games with calls', async () => {
    const page = (await render(2, statistics())).nativeElement as HTMLElement;

    const group = page.querySelector('[data-metric="criedWolfLosses"]')?.closest('.group');
    expect(group?.querySelector('[data-metric="caughtCardShare"]')).not.toBeNull();
    expect(group?.querySelector('.group__note')).not.toBeNull();
  });

  it('leaves the games-with-calls note out when every game has calls', async () => {
    const page = (await render(2, statistics({ gamesWithCalls: 3 }))).nativeElement as HTMLElement;

    expect(page.querySelector('.group__note')).toBeNull();
    expect(page.querySelector('.tile__note')).toBeNull();
  });

  it('shows card shares that add up to 100 %', async () => {
    const page = (await render(2, statistics({ caughtCardShare: 0.125 }))).nativeElement as HTMLElement;

    expect(metric(page, 'caughtCardShare')?.replace(/\s/g, ' ')).toBe('13 %');
    expect(metric(page, 'falseAlarmCardShare')?.replace(/\s/g, ' ')).toBe('87 %');
  });

  it('rounds a card share that floating point puts just below the half', async () => {
    // 0.285 * 100 is 28.499999999999996.
    const page = (await render(2, statistics({ caughtCardShare: 0.285 }))).nativeElement as HTMLElement;

    expect(metric(page, 'caughtCardShare')?.replace(/\s/g, ' ')).toBe('29 %');
    expect(metric(page, 'falseAlarmCardShare')?.replace(/\s/g, ' ')).toBe('71 %');
  });

  it('shows a dash where there is nothing to average', async () => {
    const empty = statistics({
      gamesPlayed: 0,
      averageBadCards: null,
      averagePlayers: null,
      lossIndex: null,
      worstGame: null,
      bestGame: null,
      gamesWithCalls: 0,
      callAccuracy: null,
      caughtCardShare: null,
      mostCorrectCalls: null,
    });
    const page = (await render(2, empty)).nativeElement as HTMLElement;

    expect(metric(page, 'averagePlayers')).toBe(NONE);
    expect(metric(page, 'lossIndex')).toBe(NONE);
    expect(metric(page, 'bestGame')).toBe(NONE);
    expect(page.querySelector('[data-metric="bestGame"] a')).toBeNull();
    expect(metric(page, 'callAccuracy')).toBe(NONE);
    expect(metric(page, 'caughtCardShare')).toBe(NONE);
    expect(metric(page, 'mostCorrectCalls')).toBe(NONE);
  });

  it('shows the error for a player that cannot be found', async () => {
    const fixture = await render(42);
    http
      .expectOne('/api/players/42/statistics')
      .flush({ code: 'Player.NotFound', detail: 'Player with id 42 was not found.' }, { status: 404, statusText: 'Not Found' });
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).querySelector('.error-banner')).not.toBeNull();
  });

  it('says a player id that is not a number cannot be found, without asking the server', async () => {
    const fixture = TestBed.createComponent(Statistics);
    fixture.componentRef.setInput('playerId', 'abc');
    fixture.detectChanges();
    http.expectOne('/api/players').flush(players);
    await fixture.whenStable();
    fixture.detectChanges();

    const page = fixture.nativeElement as HTMLElement;
    http.expectNone((request) => request.url.includes('/statistics'));
    expect(page.querySelector('.error-banner')?.textContent).toContain('errors.api.Player.NotFound');
    expect(page.querySelector('.detail__back')).not.toBeNull();
    expect(page.querySelector('.stats--picked')).not.toBeNull();
  });
});
