import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { GameRules } from '../api/models';
import { RulesApi } from '../api/rules-api';

/**
 * The backend's limits, loaded once before the app renders. `null` while the backend cannot be reached: the
 * screens then fail on their own requests anyway, so they treat missing rules as nothing being allowed. Screens that
 * check input against them call `ensureLoaded()`, which tries again, and say that the rules are missing rather than
 * quoting limits of nothing.
 */
@Injectable({ providedIn: 'root' })
export class GameRulesStore {
  private readonly api = inject(RulesApi);
  private loading: Promise<void> | null = null;

  readonly rules = signal<GameRules | null>(null);

  load(): Promise<void> {
    this.loading ??= this.fetch().finally(() => (this.loading = null));
    return this.loading;
  }

  /** The rules, loading them again first when the backend could not be reached so far; `null` while it still cannot. */
  async ensureLoaded(): Promise<GameRules | null> {
    if (!this.rules()) {
      await this.load();
    }
    return this.rules();
  }

  private async fetch(): Promise<void> {
    try {
      this.rules.set(await firstValueFrom(this.api.get()));
    } catch {
      // Unreachable backend: the app still starts and shows its usual "cannot connect" errors.
    }
  }
}
