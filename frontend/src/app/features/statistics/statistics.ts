import { ChangeDetectionStrategy, Component, computed, inject, input, numberAttribute, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { EMPTY, catchError, switchMap, tap } from 'rxjs';
import { Player, PlayerStatistics } from '../../core/api/models';
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
  /** Number of games the group is counted from, when that is not all of them (`statistics.fromGamesWithCalls`). */
  fromGames?: number;
}

/** One game standing out, with its value and the game to link to. */
interface GameRecord {
  key: 'worstGame' | 'bestGame' | 'mostCorrectCalls';
  icon: string;
  game: { gameId: number; finished: string; value: number } | null;
  /** Number of games the record is looked for in, when that is not all of them (`statistics.fromGamesWithCalls`). */
  fromGames?: number;
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

  /** Undefined when no player is picked; NaN for an id that is not a whole number, which no player can have. */
  readonly playerId = input<number | undefined, string | undefined>(undefined, {
    transform: (value) => {
      if (value === undefined) {
        return undefined;
      }

      const id = numberAttribute(value, NaN);
      return Number.isInteger(id) ? id : NaN;
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
    const share = (value: number | null) =>
      value === null ? NONE : this.i18n.formatNumber(value, { style: 'percent', maximumFractionDigits: 0 });
    // The two card shares add up to 1; rounded each on its own they could show 99 % or 101 %, so the false-alarm
    // share is what the rounded caught share leaves. Rounded as whole percents: 0.285 * 100 is 28.499…
    const caughtPercent = s.caughtCardShare === null ? null : Math.round(s.caughtCardShare * 100 + 1e-9);
    const fromGames = s.gamesWithCalls < s.gamesPlayed ? s.gamesWithCalls : undefined;

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
        key: 'calls',
        fromGames,
        tiles: [
          { key: 'tooMuchCalls', icon: '🤚', value: count(s.tooMuchCalls), average: average(s.averageTooMuchCalls) },
          { key: 'correctCalls', icon: '🧑‍⚖️', value: count(s.correctCalls) },
          { key: 'falseAlarms', icon: '🤨', value: count(s.falseAlarms) },
          { key: 'callAccuracy', icon: '🔍', value: share(s.callAccuracy) },
        ],
      },
      {
        key: 'calledOn',
        fromGames,
        tiles: [
          { key: 'caught', icon: '🫣', value: count(s.caught), average: average(s.averageCaught) },
          { key: 'calledOn', icon: '👉', value: count(s.calledOn), average: average(s.averageCalledOn) },
          { key: 'caughtCardShare', icon: '📈', value: share(caughtPercent === null ? null : caughtPercent / 100) },
          { key: 'falseAlarmCardShare', icon: '📢', value: share(caughtPercent === null ? null : (100 - caughtPercent) / 100) },
          { key: 'criedWolfLosses', icon: '🚨', value: count(s.criedWolfLosses) },
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

  protected readonly records = computed<GameRecord[]>(() => {
    const s = this.statistics();
    const score = (g: PlayerStatistics['worstGame']) => g && { gameId: g.gameId, finished: g.finished, value: g.finalBadPoints };
    const tally = (g: PlayerStatistics['mostCorrectCalls']) => g && { gameId: g.gameId, finished: g.finished, value: g.count };
    return s
      ? [
          { key: 'worstGame', icon: '💀', game: score(s.worstGame) },
          { key: 'bestGame', icon: '🏆', game: score(s.bestGame) },
          {
            key: 'mostCorrectCalls',
            icon: '🦅',
            game: tally(s.mostCorrectCalls),
            fromGames: s.gamesWithCalls < s.gamesPlayed ? s.gamesWithCalls : undefined,
          },
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
        switchMap((id) => {
          if (Number.isNaN(id)) {
            this.statisticsError.set({ key: 'errors.api.Player.NotFound' });
          }

          return id === undefined || Number.isNaN(id)
            ? EMPTY
            : this.playersApi.getStatistics(id).pipe(
                catchError((error) => {
                  this.statisticsError.set(toProblem(error).message);
                  return EMPTY;
                }),
              );
        }),
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
