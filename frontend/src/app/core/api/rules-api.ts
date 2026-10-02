import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { GameRules } from './models';

@Injectable({ providedIn: 'root' })
export class RulesApi {
  private readonly http = inject(HttpClient);

  get(): Observable<GameRules> {
    return this.http.get<GameRules>('/api/rules');
  }
}
