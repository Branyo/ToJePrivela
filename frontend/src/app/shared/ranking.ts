import { GamePlayer } from '../core/api/models';
import { compareNames } from '../core/i18n/language';

/**
 * Orders players by the rank the server gave them, worst first; players sharing a rank follow the `locale`'s name
 * order. Who lost is the server's call (`isLoser`), never worked out here.
 */
export function rankPlayers(players: readonly GamePlayer[], locale?: string): GamePlayer[] {
  return [...players].sort((a, b) => a.rank - b.rank || compareNames(a.name ?? '', b.name ?? '', locale));
}
