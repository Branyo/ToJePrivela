import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

/** The playing card that carries a question's donkeys (bad points). */
@Component({
  selector: 'app-bad-card',
  imports: [TranslatePipe],
  template: `
    <span class="corner">{{ points() }}</span>
    <span class="donkeys" role="img" [attr.aria-label]="'common.badPoints' | translate: { points: points() }">
      @for (donkey of donkeys(); track $index) {
        <span class="donkey" [style.animation-delay.ms]="$index * 90">🫏</span>
      }
    </span>
    <span class="corner corner--bottom">{{ points() }}</span>
  `,
  styles: `
    :host {
      position: relative;
      display: grid;
      place-items: center;
      width: var(--card-w, 96px);
      aspect-ratio: 5 / 7;
      border: var(--border);
      border-radius: 14px;
      background:
        repeating-linear-gradient(45deg, rgb(241 91 181 / 0.08) 0 8px, transparent 8px 16px),
        #fff;
      box-shadow: var(--shadow-sm);
      transform: rotate(4deg);
    }
    .donkeys {
      display: flex;
      flex-wrap: wrap;
      justify-content: center;
      gap: 2px;
      max-width: 80%;
      font-size: var(--donkey-size, 1.4rem);
    }
    .donkey {
      display: inline-block;
      animation: pop-in 0.45s var(--bounce) both;
    }
    .corner {
      position: absolute;
      top: 4px;
      left: 8px;
      font-weight: 700;
      color: var(--bubblegum);
    }
    .corner--bottom {
      top: auto;
      left: auto;
      bottom: 4px;
      right: 8px;
      transform: rotate(180deg);
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BadCard {
  readonly points = input.required<number>();
  protected readonly donkeys = computed(() => Array.from({ length: this.points() }));
}
