import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { playerLook } from './player-look';

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
  readonly seat = input.required<number>();
  protected readonly look = computed(() => playerLook(this.seat()));
}
