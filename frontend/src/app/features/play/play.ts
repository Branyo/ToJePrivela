import {
  afterRenderEffect,
  ChangeDetectionStrategy,
  Component,
  computed,
  ElementRef,
  OnDestroy,
  OnInit,
  effect,
  inject,
  input,
  numberAttribute,
  signal,
  untracked,
  viewChild,
  viewChildren,
} from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { DEFAULT_LANGUAGE, LanguageService, localeOf } from '../../core/i18n/language';
import { MessagePipe } from '../../core/i18n/message.pipe';
import { GameRulesStore } from '../../core/rules/game-rules-store';
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
  /** The safe answer of the open confirmation, which takes the focus. */
  private readonly confirmDefault = viewChild<ElementRef<HTMLElement>>('confirmDefault');

  protected readonly hitPlayerId = signal<number | null>(null);
  protected readonly doubledPlayerId = signal<number | null>(null);
  /** Which confirmation is open: ending the game (results) or deleting it (no results). */
  protected readonly confirming = signal<'end' | 'delete' | null>(null);
  protected readonly formatAnswer = formatAnswer;
  private readonly rules = inject(GameRulesStore).rules;
  /** Every worth the starting player may choose, from the backend's limits. */
  protected readonly badPointOptions = computed(() => {
    const limit = this.rules()?.badPoints;
    return limit ? Array.from({ length: limit.max - limit.min + 1 }, (_, i) => limit.min + i) : [];
  });

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

    // A game can only be ended or deleted while it runs, so a confirmation still open then is moot.
    effect(() => {
      if (!this.store.canEnd()) {
        untracked(() => this.confirming.set(null));
      }
    });

    // Each confirmation is rendered anew, so its safe answer is focused for the keyboard (`autofocus` is not).
    afterRenderEffect(() => {
      this.confirmDefault()?.nativeElement.focus();
    });

    // The question arrives in the shown language, so it is fetched again after a switch (a no-op before the first one).
    effect(() => {
      this.i18n.language();
      untracked(() => void this.store.refreshQuestion());
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
    const points = this.store.badPoints();

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
    this.confirming.set('end');
  }

  protected askDelete(): void {
    this.confirming.set('delete');
  }

  protected closeConfirm(): void {
    this.confirming.set(null);
  }

  protected endGame(): void {
    this.confirming.set(null);
    void this.store.finish();
  }

  protected async deleteGame(): Promise<void> {
    this.confirming.set(null);
    if (await this.store.delete()) {
      void this.router.navigate(['/']);
    }
  }

  private later(action: () => void, ms: number): void {
    this.timers.push(setTimeout(action, ms));
  }
}
