import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { historyParams } from './games-api';
import { AccountOverview, GameDetails, GameHistory, GameHistoryStatus, Player, PlayerStatistics } from './models';

/**
 * Every login, for admins only (403 `Auth.Forbidden` for everyone else): the list, and read only, each login's
 * players, statistics and games, answered like the login's own. 404 `Account.NotFound` for a login that is gone.
 */
@Injectable({ providedIn: 'root' })
export class AdminApi {
  private readonly http = inject(HttpClient);

  /** The most recently signed in first; logins nobody has signed in to yet last. */
  getAccounts(): Observable<AccountOverview[]> {
    return this.http.get<AccountOverview[]>('/api/admin/accounts');
  }

  /** One login, as the list shows it. */
  getAccount(accountId: number): Observable<AccountOverview> {
    return this.http.get<AccountOverview>(`/api/admin/accounts/${accountId}`);
  }

  /**
   * Deletes the login with all its players and games; 409 `Account.CannotDeleteOwn` for the admin's own and
   * `Account.CannotDeleteReserved` for the first login (neither has `canDelete`).
   */
  deleteAccount(accountId: number): Observable<void> {
    return this.http.delete<void>(`/api/admin/accounts/${accountId}`);
  }

  getPlayers(accountId: number): Observable<Player[]> {
    return this.http.get<Player[]>(`/api/admin/accounts/${accountId}/players`);
  }

  getStatistics(accountId: number, playerId: number): Observable<PlayerStatistics> {
    return this.http.get<PlayerStatistics>(`/api/admin/accounts/${accountId}/players/${playerId}/statistics`);
  }

  /** Paged like the login's own history (`GamesApi.getHistory`). */
  getHistory(
    accountId: number,
    status: GameHistoryStatus,
    after: Pick<GameDetails, 'id' | 'started'> | null,
    take: number,
  ): Observable<GameHistory> {
    return this.http.get<GameHistory>(`/api/admin/accounts/${accountId}/games/history`, {
      params: historyParams(status, after, take),
    });
  }

  getDetails(accountId: number, gameId: number): Observable<GameDetails> {
    return this.http.get<GameDetails>(`/api/admin/accounts/${accountId}/games/${gameId}/details`);
  }
}
