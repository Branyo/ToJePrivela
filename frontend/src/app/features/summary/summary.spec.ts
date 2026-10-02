import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideTranslateService } from '@ngx-translate/core';
import { GameDetails, GamePlayer } from '../../core/api/models';
import { Summary } from './summary';

const seat = (playerId: number | null, name: string | null, overrides: Partial<GamePlayer> = {}): GamePlayer => ({
  playerId,
  name,
  avatar: playerId === null ? null : '🦊',
  badPoints: 0,
  badCards: 0,
  doubles: 0,
  finalBadPoints: 0,
  rank: 1,
  isLoser: false,
  ...overrides,
});

const game = (overrides: Partial<GameDetails> = {}): GameDetails => ({
  id: 5,
  started: '2026-09-25T18:00:00Z',
  finished: '2026-09-25T19:00:00Z',
  badCardLimit: 3,
  badPointsMode: 'Question',
  cancelled: false,
  players: [seat(1, 'Ana'), seat(2, 'Bo')],
  ...overrides,
});

describe('Summary', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [Summary],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([]), provideTranslateService()],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  async function render(details: GameDetails): Promise<HTMLElement> {
    const fixture = TestBed.createComponent(Summary);
    fixture.componentRef.setInput('id', 5);
    fixture.detectChanges();
    http.expectOne('/api/games/5/details').flush(details);
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  it('shows a cancelled game without a loser and offers no rematch', async () => {
    const page = await render(
      game({ cancelled: true, players: [seat(1, 'Ana', { badCards: 2, finalBadPoints: 6 }), seat(null, null)] }),
    );

    expect(page.querySelector('.head__title')?.textContent).toContain('summary.cancelled');
    expect(page.querySelector('.spotlight')?.textContent).toContain('summary.cancelledText');
    expect(page.querySelector('.spotlight__loser')).toBeNull();
    expect(page.querySelector('.btn--fun')).toBeNull();
    expect(page.querySelector('app-confetti')).toBeNull();
  });

  it('names a player deleted since as an unknown player', async () => {
    const page = await render(game({ players: [seat(1, 'Ana'), seat(null, null, { rank: 2 })] }));

    const names = [...page.querySelectorAll('.row__name')].map((name) => name.textContent?.trim());
    expect(names).toEqual(['Ana', 'common.unknownPlayer']);
    expect(page.querySelector('.btn--fun')).toBeNull();
  });

  it('offers a rematch when every player still exists', async () => {
    const page = await render(game());

    expect(page.querySelector('.btn--fun')).not.toBeNull();
  });
});
