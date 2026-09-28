import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  OnDestroy,
  OnInit,
  effect,
  inject,
  input,
  numberAttribute,
  signal,
  viewChild,
  viewChildren,
} from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { DEFAULT_LANGUAGE, LanguageService, localeOf } from '../../core/i18n/language';
import { MessagePipe } from '../../core/i18n/message.pipe';
import { BadCard } from '../../shared/bad-card';
import { flyCard } from '../../shared/fly-card';
import { PlayerAvatar } from '../../shared/player-avatar';
import { PlayStore } from './play-store';

const GAME_OVER_PAUSE_MS = 1800;
const HIT_EFFECT_MS = 900;

/** `"1,2,3"` from the query string; anything unparsable is dropped. */
export function parseCategoryIds(value: string | undefined): number[] {
  return (value ?? '')
    .split(',')
    .map((part) => Number(part.trim()))
    .filter((id) => Number.isInteger(id) && id > 0);
}

/** Answers are numeric strings; show them in the locale's style (`1 234 567`, `3,5` in Slovak) so big numbers are readable aloud. */
export function formatAnswer(answer: string, locale: string = localeOf(DEFAULT_LANGUAGE)): string {
  const value = Number(answer);
  return Number.isFinite(value) ? value.toLocaleString(locale, { maximumFractionDigits: 10 }) : answer;
}

@Component({
  selector: 'app-play',
  imports: [BadCard, PlayerAvatar, RouterLink, TranslatePipe, MessagePipe],
  templateUrl: './play.html',
  styleUrl: './play.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [PlayStore],
})
export class Play implements OnInit, OnDestroy {
  protected readonly store = inject(PlayStore);
  private readonly router = inject(Router);
  protected readonly i18n = inject(LanguageService);

  /** Route parameter. */
  readonly id = input.required({ transform: numberAttribute });
  /** Optional `?categories=1,2` query parameter. */
  readonly categories = input<string>();

  private readonly cardSource = viewChild('cardSource', { read: ElementRef });
  private readonly seatButtons = viewChildren<ElementRef<HTMLElement>>('seatButton');

  protected readonly hitPlayerId = signal<number | null>(null);
  protected readonly doubledPlayerId = signal<number | null>(null);
  protected readonly confirmingEnd = signal(false);
  protected readonly formatAnswer = formatAnswer;

  private timers: ReturnType<typeof setTimeout>[] = [];

  constructor() {
    effect(() => {
      if (this.store.phase() === 'finished') {
        this.later(() => {
          void this.router.navigate(['/games', this.id(), 'summary'], {
            queryParams: this.categories() ? { categories: this.categories() } : {},
          });
        }, GAME_OVER_PAUSE_MS);
      }
    });
  }

  ngOnInit(): void {
    void this.store.start(this.id(), parseCategoryIds(this.categories()));
  }

  ngOnDestroy(): void {
    this.timers.forEach(clearTimeout);
  }

  protected award(playerId: number, seat: number): void {
    const from = this.cardSource()?.nativeElement as HTMLElement | undefined;
    const to = this.seatButtons()[seat]?.nativeElement;
    const points = this.store.question()?.badPoints ?? 0;

    const landing = from && to ? flyCard(from, to, points) : Promise.resolve();
    void landing.then(() => {
      this.hitPlayerId.set(playerId);
      this.later(() => this.hitPlayerId.set(null), HIT_EFFECT_MS);
    });

    void this.store.award(playerId, landing);
  }

  protected double(playerId: number): void {
    this.doubledPlayerId.set(playerId);
    this.later(() => this.doubledPlayerId.set(null), HIT_EFFECT_MS);

    void this.store.awardDouble(playerId);
  }

  protected undouble(playerId: number): void {
    void this.store.removeDouble(playerId);
  }

  protected askEnd(): void {
    this.confirmingEnd.set(true);
  }

  protected cancelEnd(): void {
    this.confirmingEnd.set(false);
  }

  protected endGame(): void {
    this.confirmingEnd.set(false);
    void this.store.finish();
  }

  private later(action: () => void, ms: number): void {
    this.timers.push(setTimeout(action, ms));
  }
}
