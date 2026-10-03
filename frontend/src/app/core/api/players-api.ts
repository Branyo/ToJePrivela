import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Player } from './models';

@Injectable({ providedIn: 'root' })
export class PlayersApi {
  private readonly http = inject(HttpClient);

  getAll(): Observable<Player[]> {
    return this.http.get<Player[]>('/api/players');
  }

  create(name: string): Observable<Player> {
    return this.http.post<Player>('/api/players', { name });
  }

  /** Their finished games keep them as an unknown player; a game still running is cancelled. */
  delete(id: number): Observable<void> {
    return this.http.delete<void>(`/api/players/${id}`);
  }
}
