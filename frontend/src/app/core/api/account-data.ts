import { Injectable, forwardRef, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Observable } from 'rxjs';
import { AdminApi } from './admin-api';
import { GamesApi } from './games-api';
import { GameDetails, GameHistory, GameHistoryStatus, Player, PlayerStatistics } from './models';
import { PlayersApi } from './players-api';

/** Router commands for `[routerLink]`. */
export type Link = readonly (string | number)[];

/**
 * Whose players, games and statistics the screens showing them (history, statistics, a game's results) read, and
 * where their links lead. By default the signed-in login's own; an admin's view of another login provides
 * `AdminAccountData` instead, so the same screens show that login, read only.
 */
@Injectable({ providedIn: 'root', useExisting: forwardRef(() => OwnAccountData) })
export abstract class AccountData {
  /** Every link handed out so far, so the same link is the same array (see `link`). */
  private readonly links = new Map<string, Link>();

  /** Another login's data, shown but never changed: no new game, no rematch, no going back into a running game. */
  abstract readonly readOnly: boolean;

  abstract getPlayers(): Observable<Player[]>;

  abstract getStatistics(playerId: number): Observable<PlayerStatistics>;

  abstract getHistory(
    status: GameHistoryStatus,
    after: Pick<GameDetails, 'id' | 'started'> | null,
    take: number,
  ): Observable<GameHistory>;

  abstract getDetails(gameId: number): Observable<GameDetails>;

  abstract historyLink(): Link;

  /** The statistics screen, with a player picked or not. */
  abstract statisticsLink(playerId?: number): Link;

  /** Where a listed game opens. */
  abstract gameLink(game: Pick<GameDetails, 'id' | 'finished'>): Link;

  /**
   * The link with these commands, the same array every time: templates bind the link methods, and a `[routerLink]`
   * given a new array works its URL out again on every check, as many times as a history page lists games.
   */
  protected link(...commands: (string | number)[]): Link {
    const key = commands.join('/');
    let link = this.links.get(key);
    if (!link) {
      link = commands;
      this.links.set(key, link);
    }
    return link;
  }
}

/** The signed-in login's own data; its running games open the play screen. */
@Injectable({ providedIn: 'root' })
export class OwnAccountData extends AccountData {
  private readonly players = inject(PlayersApi);
  private readonly games = inject(GamesApi);

  readonly readOnly = false;

  getPlayers(): Observable<Player[]> {
    return this.players.getAll();
  }

  getStatistics(playerId: number): Observable<PlayerStatistics> {
    return this.players.getStatistics(playerId);
  }

  getHistory(
    status: GameHistoryStatus,
    after: Pick<GameDetails, 'id' | 'started'> | null,
    take: number,
  ): Observable<GameHistory> {
    return this.games.getHistory(status, after, take);
  }

  getDetails(gameId: number): Observable<GameDetails> {
    return this.games.getDetails(gameId);
  }

  historyLink(): Link {
    return this.link('/history');
  }

  statisticsLink(playerId?: number): Link {
    return playerId === undefined ? this.link('/statistics') : this.link('/statistics', playerId);
  }

  gameLink(game: Pick<GameDetails, 'id' | 'finished'>): Link {
    return game.finished ? this.link('/games', game.id, 'summary') : this.link('/games', game.id);
  }
}

/**
 * Another login's data for an admin, read only: the login is the `accountId` of the route whose component provides
 * this, read on every call, and every link stays under `/accounts/:accountId`. Every game, running or not, opens its
 * results.
 */
@Injectable()
export class AdminAccountData extends AccountData {
  private readonly api = inject(AdminApi);
  private readonly route = inject(ActivatedRoute);

  readonly readOnly = true;

  getPlayers(): Observable<Player[]> {
    return this.api.getPlayers(this.accountId);
  }

  getStatistics(playerId: number): Observable<PlayerStatistics> {
    return this.api.getStatistics(this.accountId, playerId);
  }

  getHistory(
    status: GameHistoryStatus,
    after: Pick<GameDetails, 'id' | 'started'> | null,
    take: number,
  ): Observable<GameHistory> {
    return this.api.getHistory(this.accountId, status, after, take);
  }

  getDetails(gameId: number): Observable<GameDetails> {
    return this.api.getDetails(this.accountId, gameId);
  }

  historyLink(): Link {
    return this.link('/accounts', this.accountId, 'history');
  }

  statisticsLink(playerId?: number): Link {
    return playerId === undefined
      ? this.link('/accounts', this.accountId, 'statistics')
      : this.link('/accounts', this.accountId, 'statistics', playerId);
  }

  gameLink(game: Pick<GameDetails, 'id' | 'finished'>): Link {
    return this.link('/accounts', this.accountId, 'games', game.id);
  }

  private get accountId(): number {
    return Number(this.route.snapshot.paramMap.get('accountId'));
  }
}
