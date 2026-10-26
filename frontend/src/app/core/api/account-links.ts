import { Pipe, PipeTransform, inject } from '@angular/core';
import { AccountData, Link } from './account-data';
import { GameDetails } from './models';

/**
 * Where a listed game opens, for the login `AccountData` names. Pure, so a `[routerLink]` gets the same array until the
 * game changes instead of a new one, worked out again, on every check.
 */
@Pipe({ name: 'gameLink' })
export class GameLinkPipe implements PipeTransform {
  private readonly data = inject(AccountData);

  transform(game: Pick<GameDetails, 'id' | 'finished'>): Link {
    return this.data.gameLink(game);
  }
}

/** A player's statistics, for the login `AccountData` names; pure like `GameLinkPipe`. */
@Pipe({ name: 'statisticsLink' })
export class StatisticsLinkPipe implements PipeTransform {
  private readonly data = inject(AccountData);

  transform(playerId: number): Link {
    return this.data.statisticsLink(playerId);
  }
}
