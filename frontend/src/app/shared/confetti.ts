import { ChangeDetectionStrategy, Component } from '@angular/core';

const COLORS = ['#ff5d5d', '#3ddc97', '#4cc9f0', '#f15bb5', '#9b5de5', '#ff9f1c', '#fee440'];

/** Decorative CSS confetti rain. */
@Component({
  selector: 'app-confetti',
  template: `
    @for (piece of pieces; track $index) {
      <i
        [class.round]="piece.round"
        [style.left.%]="piece.left"
        [style.background]="piece.color"
        [style.animation-delay.s]="piece.delay"
        [style.animation-duration.s]="piece.duration"
      ></i>
    }
  `,
  styles: `
    :host {
      position: fixed;
      inset: 0;
      overflow: hidden;
      pointer-events: none;
      z-index: 50;
    }
    i {
      position: absolute;
      top: -20px;
      width: 10px;
      height: 16px;
      border-radius: 2px;
      animation: fall linear infinite both;
    }
    i.round {
      width: 12px;
      height: 12px;
      border-radius: 50%;
    }
    @keyframes fall {
      0% { transform: translateY(0) rotate(0); }
      100% { transform: translateY(110vh) rotate(720deg); }
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { 'aria-hidden': 'true' },
})
export class Confetti {
  protected readonly pieces = Array.from({ length: 70 }, (_, i) => ({
    left: Math.random() * 100,
    delay: Math.random() * 4,
    duration: 3 + Math.random() * 3,
    color: COLORS[i % COLORS.length],
    round: i % 3 === 0,
  }));
}
