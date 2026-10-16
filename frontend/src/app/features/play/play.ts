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
import { GameDetails } from '../../core/api/models';
import { DEFAULT_LANGUAGE, LanguageService, localeOf } from '../../core/i18n/language';
import { MessagePipe } from '../../core/i18n/message.pipe';
import { GameRulesStore } from '../../core/rules/game-rules-store';
import { BadCard } from '../../shared/bad-card';
import { flyCard } from '../../shared/fly-card';
import { PlayerAvatar } from '../../shared/player-avatar';
import { PlayStore } from './play-store';

const GAME_OVER_PAUSE_MS = 1800;
const HIT_EFFECT_MS = 900;
const VERDICT_MS = 1700;

/** How a round's call turned out: the caller took the card themselves, or caught somebody overdoing it. */
export type Verdict = 'false-alarm' | 'too-much';

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

/** The caller's false alarms as the server counted them; comparing two games tells whether a card made one. */
function falseAlarmsOf(game: GameDetails | null, callerId: number | null): number {
  return game?.players.find((p) => p.playerId === callerId)?.falseAlarms ?? 0;
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
  /** Where the focus goes back once a confirmation closes. */
  private readonly endButton = viewChild<ElementRef<HTMLElement>>('endButton');

  protected readonly hitPlayerId = signal<number | null>(null);
  protected readonly doubledPlayerId = signal<number | null>(null);
  /** Shown for a moment once the card lands, with the player who took it. */
  protected readonly verdict = signal<{ kind: Verdict; playerId: number } | null>(null);
  /** Which confirmation is open: ending the game (results) or deleting it (no results). */
  protected readonly confirming = signal<'end' | 'delete' | null>(null);
  /** Whether "No" in the delete confirmation goes back to the end one it was opened from, or just closes it. */
  private deleteFromEnd = false;
  protected readonly formatAnswer = formatAnswer;
  private readonly rules = inject(GameRulesStore).rules;
  /** Every worth the starting player may choose, from the backend's limits. */
  protected readonly badPointOptions = computed(() => {
    const limit = this.rules()?.badPoints;
    return limit ? Array.from({ length: limit.max - limit.min + 1 }, (_, i) => limit.min + i) : [];
  });

  private timers: ReturnType<typeof setTimeout>[] = [];
  /** Set once the screen is left, so a request answering later does not navigate away from wherever the user went. */
  private destroyed = false;

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

    // Each confirmation is rendered anew, so its safe answer is focused for the keyboard (`autofocus` is not). Once
    // it closes, the focus goes back to "End" instead of being lost with the removed dialog; while the game is being
    // ended that button is disabled, so it waits until the request failed and the button is usable again; once the
    // game is over there is nothing to go back to.
    let returnFocus = false;
    afterRenderEffect(() => {
      if (this.confirming()) {
        this.confirmDefault()?.nativeElement.focus();
        returnFocus = true;
      } else if (returnFocus) {
        if (this.store.canEnd()) {
          this.endButton()?.nativeElement.focus();
          returnFocus = false;
        } else if (this.store.phase() !== 'ending') {
          returnFocus = false;
        }
      }
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
    this.destroyed = true;
    this.timers.forEach(clearTimeout);
  }

  protected award(playerId: number, seat: number): void {
    const from = this.cardSource()?.nativeElement as HTMLElement | undefined;
    const to = this.seatButtons()[seat]?.nativeElement;
    const points = this.store.badPoints();
    const callerId = this.store.callerId();
    const falseAlarmsBefore = falseAlarmsOf(this.store.game(), callerId);

    const landing = from && to ? flyCard(from, to, points) : Promise.resolve();
    // Only a card the server counted gets its effect and verdict; whether the call was a false alarm is its decision.
    void this.store.award(playerId, landing, (saved) => {
      const kind: Verdict = falseAlarmsOf(saved, callerId) > falseAlarmsBefore ? 'false-alarm' : 'too-much';
      this.hitPlayerId.set(playerId);
      this.later(() => this.hitPlayerId.set(null), HIT_EFFECT_MS);
      const shown = { kind, playerId };
      this.verdict.set(shown);
      this.later(() => this.verdict.update((current) => (current === shown ? null : current)), VERDICT_MS);
    });
  }

  protected double(playerId: number): void {
    this.doubledPlayerId.set(playerId);
    this.later(() => this.doubledPlayerId.set(null), HIT_EFFECT_MS);

    void this.store.awardDouble(playerId);
  }

  protected undouble(playerId: number): void {
    void this.store.removeDouble(playerId);
  }

  /** Asks how to end, on the game as it is now: another device may have given it its first card meanwhile. */
  protected askEnd(): void {
    this.confirming.set('end');
    void this.store.refreshGame();
  }

  /** @param fromEnd Opened from the end confirmation, which "No" then goes back to. */
  protected askDelete(fromEnd: boolean): void {
    this.deleteFromEnd = fromEnd;
    this.confirming.set('delete');
  }

  protected cancelDelete(): void {
    if (this.deleteFromEnd) {
      this.askEnd();
    } else {
      this.closeConfirm();
    }
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
    if (!(await this.store.delete()) || this.destroyed) {
      return;
    }

    // If the navigation is refused, the screen still says the game is gone and links home; an error banner there
    // would only suggest the delete failed.
    await this.router.navigate(['/']).catch((error: unknown) => console.error('Navigating home failed.', error));
  }

  private later(action: () => void, ms: number): void {
    this.timers.push(setTimeout(action, ms));
  }
}
