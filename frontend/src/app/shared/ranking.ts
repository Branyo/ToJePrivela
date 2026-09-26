import { GamePlayer } from '../core/api/models';
import { compareNames } from '../core/i18n/language';

export interface RankedPlayer extends GamePlayer {
  /** 1-based; players with equal points and cards share a rank. */
  rank: number;
  isLoser: boolean;
  /** Position in the game's player list, which decides the player's animal and colour. */
  seat: number;
}

/** Seat order: by player id, the same on every screen so a player keeps their animal. */
export function seatPlayers<T extends { playerId: number }>(players: readonly T[]): T[] {
  return [...players].sort((a, b) => a.playerId - b.playerId);
}

/**
 * Expects players in seat order. Most bad points first. The loser is whoever has the most bad points; a tie on points is broken by
 * the number of cards, then by name in the `locale`'s order, and a full tie means several losers. Nobody loses while nobody has a card.
 */
export function rankPlayers(players: readonly GamePlayer[], locale?: string): RankedPlayer[] {
  const sorted = players
    .map((player, seat) => ({ ...player, seat }))
    .sort((a, b) => b.badPoints - a.badPoints || b.badCards - a.badCards || compareNames(a.name, b.name, locale));

  const worst = sorted[0];
  let rank = 0;

  return sorted.map((player, index) => {
    const previous = sorted[index - 1];
    if (!previous || previous.badPoints !== player.badPoints || previous.badCards !== player.badCards) {
      rank = index + 1;
    }

    const isLoser =
      worst.badPoints > 0 && player.badPoints === worst.badPoints && player.badCards === worst.badCards;

    return { ...player, rank, isLoser };
  });
}
