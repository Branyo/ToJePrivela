import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Game, GameDetails } from './models';

@Injectable({ providedIn: 'root' })
export class GamesApi {
  private readonly http = inject(HttpClient);

  getAll(): Observable<Game[]> {
    return this.http.get<Game[]>('/api/games');
  }

  getDetails(id: number): Observable<GameDetails> {
    return this.http.get<GameDetails>(`/api/games/${id}/details`);
  }

  create(playerIds: number[], badCardLimit: number): Observable<Game> {
    return this.http.post<Game>('/api/games', { playerIds, badCardLimit });
  }

  /** The card is worth the question's stored bad points; the server finishes the game at the limit. */
  awardBadCard(id: number, playerId: number, questionId: number): Observable<GameDetails> {
    return this.http.post<GameDetails>(`/api/games/${id}/bad-cards`, { playerId, questionId });
  }

  /** A double that held: one bad point off at the end; never finishes the game. */
  awardDouble(id: number, playerId: number): Observable<GameDetails> {
    return this.http.post<GameDetails>(`/api/games/${id}/doubles`, { playerId });
  }

  /** Takes back one double tapped by mistake; 409 when the player has none. */
  removeDouble(id: number, playerId: number): Observable<GameDetails> {
    return this.http.delete<GameDetails>(`/api/games/${id}/doubles/${playerId}`);
  }

  finish(id: number): Observable<GameDetails> {
    return this.http.post<GameDetails>(`/api/games/${id}/finish`, null);
  }
}
