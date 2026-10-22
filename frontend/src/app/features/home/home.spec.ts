import { registerLocaleData } from '@angular/common';
import localeSk from '@angular/common/locales/sk';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideTranslateService } from '@ngx-translate/core';
import { GameDetails, GamePlayer } from '../../core/api/models';
import { Home, RUNNING_SHOWN } from './home';

const player = (playerId: number, name: string): GamePlayer => ({
  playerId,
  name,
  avatar: '🦊',
  badPoints: 0,
  badCards: 0,
  doubles: 0,
  finalBadPoints: 0,
  rank: 1,
  isLoser: false,
  tooMuchCalls: 0,
  falseAlarms: 0,
  correctCalls: 0,
  caught: 0,
  previousPlayerId: null,
});

const game = (id: number, names: string[]): GameDetails => ({
  id,
  started: '2026-10-20T18:00:00Z',
  finished: null,
  badCardLimit: 3,
  badPointsMode: 'Question',
  cancelled: false,
  hasResult: false,
  starterId: 1,
  players: names.map((name, index) => player(index + 1, name)),
});

// The app registers it in `app.config.ts`; dates on the page are formatted for the Slovak default.
registerLocaleData(localeSk, 'sk-SK');

describe('Home', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [Home],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([]), provideTranslateService()],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('asks only for the few newest running games and lists them in the order received', async () => {
    const fixture = TestBed.createComponent(Home);
    fixture.detectChanges();

    const request = http.expectOne((r) => r.url === '/api/games/history');
    expect(request.request.params.get('status')).toBe('Running');
    expect(request.request.params.get('take')).toBe(String(RUNNING_SHOWN));
    expect(request.request.params.has('beforeId')).toBe(false);
    request.flush({ games: [game(9, ['Zuzka', 'Adam']), game(3, ['Ema', 'Ivo'])], hasMore: true });
    await fixture.whenStable();

    const page = fixture.nativeElement as HTMLElement;
    const listed = [...page.querySelectorAll('.running__game strong')].map((name) => name.textContent?.trim());
    expect(listed).toEqual(['Zuzka, Adam', 'Ema, Ivo']);
    http.expectNone('/api/games');
    http.expectNone('/api/players');
  });

  it('shows no running section without running games', async () => {
    const fixture = TestBed.createComponent(Home);
    fixture.detectChanges();

    http.expectOne((r) => r.url === '/api/games/history').flush({ games: [], hasMore: false });
    await fixture.whenStable();

    expect((fixture.nativeElement as HTMLElement).querySelector('.running')).toBeNull();
  });
});
