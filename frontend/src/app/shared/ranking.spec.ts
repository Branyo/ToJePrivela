import { GamePlayer } from '../core/api/models';
import { rankPlayers, seatPlayers } from './ranking';

const player = (playerId: number, name: string, badPoints: number, badCards: number, doubles = 0): GamePlayer => ({
  playerId,
  name,
  avatar: '🦊',
  badPoints,
  badCards,
  doubles,
  finalBadPoints: badPoints - doubles,
});

describe('seatPlayers', () => {
  it('seats players by id', () => {
    const seated = seatPlayers([player(7, 'C', 0, 0), player(2, 'A', 0, 0), player(5, 'B', 0, 0)]);

    expect(seated.map((p) => p.playerId)).toEqual([2, 5, 7]);
  });
});

describe('rankPlayers', () => {
  it('ranks the most bad points first and makes that player the loser', () => {
    const ranked = rankPlayers([player(1, 'Ana', 3, 1), player(2, 'Bo', 9, 3), player(3, 'Cy', 5, 2)]);

    expect(ranked.map((p) => p.name)).toEqual(['Bo', 'Cy', 'Ana']);
    expect(ranked.map((p) => p.rank)).toEqual([1, 2, 3]);
    expect(ranked.filter((p) => p.isLoser).map((p) => p.name)).toEqual(['Bo']);
  });

  it('breaks a points tie with the number of cards', () => {
    const ranked = rankPlayers([player(1, 'Ana', 6, 2), player(2, 'Bo', 6, 3)]);

    expect(ranked[0].name).toBe('Bo');
    expect(ranked.filter((p) => p.isLoser)).toHaveLength(1);
  });

  it('shares the rank and the defeat on a full tie', () => {
    const ranked = rankPlayers([player(1, 'Ana', 6, 2), player(2, 'Bo', 6, 2), player(3, 'Cy', 1, 1)]);

    expect(ranked.map((p) => p.rank)).toEqual([1, 1, 3]);
    expect(ranked.filter((p) => p.isLoser).map((p) => p.name)).toEqual(['Ana', 'Bo']);
  });

  it('has no loser when nobody took a card', () => {
    const ranked = rankPlayers([player(1, 'Ana', 0, 0), player(2, 'Bo', 0, 0)]);

    expect(ranked.some((p) => p.isLoser)).toBe(false);
  });

  it('ranks by bad points after doubles are taken off', () => {
    const ranked = rankPlayers([player(1, 'Ana', 7, 2, 3), player(2, 'Bo', 5, 2)]);

    expect(ranked.map((p) => p.name)).toEqual(['Bo', 'Ana']);
    expect(ranked.filter((p) => p.isLoser).map((p) => p.name)).toEqual(['Bo']);
  });

  it('can make a player without cards the loser when doubles push everyone else below them', () => {
    const ranked = rankPlayers([player(1, 'Ana', 1, 1, 2), player(2, 'Bo', 0, 0)]);

    expect(ranked.filter((p) => p.isLoser).map((p) => p.name)).toEqual(['Bo']);
  });

  it('has no loser when there are only doubles and no cards', () => {
    const ranked = rankPlayers([player(1, 'Ana', 0, 0, 1), player(2, 'Bo', 0, 0)]);

    expect(ranked.some((p) => p.isLoser)).toBe(false);
  });

  it('handles an empty table', () => {
    expect(rankPlayers([])).toEqual([]);
  });
});
