import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { GameRulesStore } from '../core/rules/game-rules-store';
import { UNKNOWN_PLAYER_AVATAR, playerLook } from './player-look';

@Component({
  selector: 'app-player-avatar',
  template: `{{ look().animal }}`,
  styles: `
    :host {
      display: inline-grid;
      place-items: center;
      flex: none;
      width: var(--avatar-size, 48px);
      height: var(--avatar-size, 48px);
      border: var(--border);
      border-radius: 50%;
      font-size: calc(var(--avatar-size, 48px) * 0.55);
      line-height: 1;
      box-shadow: var(--shadow-sm);
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    'aria-hidden': 'true',
    '[style.background]': 'look().color',
  },
})
export class PlayerAvatar {
  /** The animal the server assigned to the player; `null` for a player deleted since. */
  readonly avatar = input.required<string | null>();
  private readonly rules = inject(GameRulesStore).rules;
  protected readonly look = computed(() => playerLook(this.avatar() ?? UNKNOWN_PLAYER_AVATAR, this.rules()?.avatars ?? []));
}
