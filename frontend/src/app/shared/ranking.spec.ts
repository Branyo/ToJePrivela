import { GamePlayer } from '../core/api/models';
import { rankPlayers, seatPlayers } from './ranking';

const player = (playerId: number, name: string, rank: number, isLoser = false): GamePlayer => ({
  playerId,
  name,
  avatar: '🦊',
  badPoints: 0,
  badCards: 0,
  doubles: 0,
  finalBadPoints: 0,
  rank,
  isLoser,
});

describe('seatPlayers', () => {
  it('seats players by id', () => {
    const seated = seatPlayers([player(7, 'C', 1), player(2, 'A', 1), player(5, 'B', 1)]);

    expect(seated.map((p) => p.playerId)).toEqual([2, 5, 7]);
  });
});

describe('rankPlayers', () => {
  it('orders by the rank the server gave, worst first', () => {
    const ranked = rankPlayers([player(1, 'Ana', 3), player(2, 'Bo', 1, true), player(3, 'Cy', 2)]);

    expect(ranked.map((p) => p.name)).toEqual(['Bo', 'Cy', 'Ana']);
  });

  it('orders a shared rank by name in the locale', () => {
    const ranked = rankPlayers([player(1, 'Zuza', 1, true), player(2, 'Čenek', 1, true), player(3, 'Cyril', 1, true)], 'sk-SK');

    expect(ranked.map((p) => p.name)).toEqual(['Cyril', 'Čenek', 'Zuza']);
  });

  it('keeps the server verdict on who lost', () => {
    const ranked = rankPlayers([player(1, 'Ana', 2), player(2, 'Bo', 1, true)]);

    expect(ranked.filter((p) => p.isLoser).map((p) => p.name)).toEqual(['Bo']);
  });

  it('handles an empty table', () => {
    expect(rankPlayers([])).toEqual([]);
  });
});
