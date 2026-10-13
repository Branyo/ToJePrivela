import { ChangeDetectionStrategy, Component, computed, inject, input, numberAttribute, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { EMPTY, catchError, switchMap, tap } from 'rxjs';
import { GameScore, Player, PlayerStatistics } from '../../core/api/models';
import { PlayersApi } from '../../core/api/players-api';
import { toProblem } from '../../core/api/problem';
import { LanguageService, Message, compareNames } from '../../core/i18n/language';
import { MessagePipe } from '../../core/i18n/message.pipe';
import { PlayerAvatar } from '../../shared/player-avatar';

interface Tile {
  /** Texts live under `statistics.metrics.<key>`. */
  key: string;
  icon: string;
  value: string;
  /** The per-game average shown under a total. */
  average?: string;
}

interface TileGroup {
  /** Title under `statistics.groups.<key>`. */
  key: string;
  tiles: Tile[];
}

interface ScoreRecord {
  key: 'worstGame' | 'bestGame';
  icon: string;
  score: GameScore | null;
}

/** Shown for an average (or the loss index) with nothing to average. */
export const NONE = '—';

/** Players alphabetically; picking one (`/statistics/:playerId`) shows their record over finished games. */
@Component({
  selector: 'app-statistics',
  imports: [RouterLink, RouterLinkActive, DatePipe, PlayerAvatar, TranslatePipe, MessagePipe],
  templateUrl: './statistics.html',
  styleUrl: './statistics.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Statistics {
  private readonly playersApi = inject(PlayersApi);
  protected readonly i18n = inject(LanguageService);
  protected readonly none = NONE;

  readonly playerId = input<number | undefined, string | undefined>(undefined, {
    transform: (value) => {
      const id = numberAttribute(value, NaN);
      return Number.isInteger(id) ? id : undefined;
    },
  });

  private readonly players = signal<Player[] | null>(null);
  protected readonly playersError = signal<Message | null>(null);
  protected readonly statistics = signal<PlayerStatistics | null>(null);
  protected readonly statisticsError = signal<Message | null>(null);

  protected readonly sortedPlayers = computed(() => {
    const locale = this.i18n.locale();
    const players = this.players();
    return players ? [...players].sort((a, b) => compareNames(a.name, b.name, locale)) : null;
  });

  protected readonly groups = computed<TileGroup[]>(() => {
    const s = this.statistics();
    if (!s) {
      return [];
    }

    const count = (value: number) => this.format(value, 0);
    const average = (value: number | null) => (value === null ? NONE : this.format(value, 2));

    return [
      {
        key: 'games',
        tiles: [
          { key: 'gamesPlayed', icon: '🎲', value: count(s.gamesPlayed) },
          { key: 'averagePlayers', icon: '👥', value: average(s.averagePlayers) },
        ],
      },
      {
        key: 'cards',
        tiles: [
          { key: 'badCards', icon: '🫏', value: count(s.badCards), average: average(s.averageBadCards) },
          { key: 'finalBadPoints', icon: '🎯', value: count(s.finalBadPoints), average: average(s.averageFinalBadPoints) },
          { key: 'doubles', icon: '✌️', value: count(s.doubles), average: average(s.averageDoubles) },
        ],
      },
      {
        key: 'losses',
        tiles: [
          { key: 'losses', icon: '😵', value: count(s.losses) },
          { key: 'sharedLosses', icon: '🤝', value: count(s.sharedLosses) },
          { key: 'lossIndex', icon: '⚖️', value: average(s.lossIndex) },
        ],
      },
      {
        key: 'feats',
        tiles: [
          { key: 'flawlessVictories', icon: '✨', value: count(s.flawlessVictories) },
          { key: 'luckyFinishes', icon: '🍀', value: count(s.luckyFinishes) },
          { key: 'closestEscapes', icon: '😅', value: count(s.closestEscapes) },
          { key: 'reversals', icon: '🔄', value: count(s.reversals) },
        ],
      },
    ];
  });

  protected readonly records = computed<ScoreRecord[]>(() => {
    const s = this.statistics();
    return s
      ? [
          { key: 'worstGame', icon: '💀', score: s.worstGame },
          { key: 'bestGame', icon: '🏆', score: s.bestGame },
        ]
      : [];
  });

  constructor() {
    this.playersApi.getAll().subscribe({
      next: (players) => this.players.set(players),
      error: (error) => this.playersError.set(toProblem(error).message),
    });

    // Another player drops what was shown and fetches theirs; a late reply for the previous one is ignored.
    toObservable(this.playerId)
      .pipe(
        tap(() => {
          this.statistics.set(null);
          this.statisticsError.set(null);
        }),
        switchMap((id) =>
          id === undefined
            ? EMPTY
            : this.playersApi.getStatistics(id).pipe(
                catchError((error) => {
                  this.statisticsError.set(toProblem(error).message);
                  return EMPTY;
                }),
              ),
        ),
        takeUntilDestroyed(),
      )
      .subscribe((statistics) => this.statistics.set(statistics));
  }

  protected format(value: number, fractionDigits: number): string {
    return this.i18n.formatNumber(value, {
      minimumFractionDigits: fractionDigits,
      maximumFractionDigits: fractionDigits,
    });
  }
}
