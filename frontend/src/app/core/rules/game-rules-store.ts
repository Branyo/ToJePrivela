import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { GameRules } from '../api/models';
import { RulesApi } from '../api/rules-api';

/**
 * The backend's limits, loaded once before the app renders. `null` while the backend cannot be reached: the
 * screens then fail on their own requests anyway, so they treat missing rules as nothing being allowed.
 */
@Injectable({ providedIn: 'root' })
export class GameRulesStore {
  private readonly api = inject(RulesApi);

  readonly rules = signal<GameRules | null>(null);

  async load(): Promise<void> {
    try {
      this.rules.set(await firstValueFrom(this.api.get()));
    } catch {
      // Unreachable backend: the app still starts and shows its usual "cannot connect" errors.
    }
  }
}
