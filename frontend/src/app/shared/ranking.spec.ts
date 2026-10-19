import { GamePlayer } from '../core/api/models';
import { rankPlayers } from './ranking';

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
  tooMuchCalls: 0,
  falseAlarms: 0,
  correctCalls: 0,
  caught: 0,
  previousPlayerId: null,
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

  it('orders an unknown player within a shared rank without a name', () => {
    const ranked = rankPlayers([player(1, 'Ana', 1), { ...player(2, 'Bo', 1), playerId: null, name: null }]);

    expect(ranked.map((p) => p.name)).toEqual([null, 'Ana']);
  });

  it('handles an empty table', () => {
    expect(rankPlayers([])).toEqual([]);
  });
});
