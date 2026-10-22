import { ChangeDetectionStrategy, Component, inject, input, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { EMPTY, Subject, catchError, exhaustMap, startWith, switchMap, tap } from 'rxjs';
import { GamesApi } from '../../core/api/games-api';
import { GameDetails, GameHistoryStatus } from '../../core/api/models';
import { toProblem } from '../../core/api/problem';
import { LanguageService, Message } from '../../core/i18n/language';
import { MessagePipe } from '../../core/i18n/message.pipe';
import { PlayerAvatar } from '../../shared/player-avatar';

/** Games asked for at a time; the server allows up to 50. */
export const PAGE_SIZE = 20;

/** The filter as the URL has it (`/history?status=running`); anything else lists every game. */
const STATUSES: Record<string, GameHistoryStatus> = { running: 'Running', finished: 'Finished' };

/**
 * The login's games, newest first, a page at a time; running ones open the play screen, the others their summary.
 * The filter lives in the URL, so coming back from a game keeps it.
 */
@Component({
  selector: 'app-history',
  imports: [RouterLink, DatePipe, PlayerAvatar, TranslatePipe, MessagePipe],
  templateUrl: './history.html',
  styleUrl: './history.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class History {
  private readonly gamesApi = inject(GamesApi);
  protected readonly i18n = inject(LanguageService);
  protected readonly filters: { status: GameHistoryStatus; query: string | null }[] = [
    { status: 'All', query: null },
    { status: 'Running', query: 'running' },
    { status: 'Finished', query: 'finished' },
  ];

  readonly status = input<GameHistoryStatus, string | undefined>('All', {
    transform: (value) => (value === undefined ? undefined : STATUSES[value]) ?? 'All',
  });

  protected readonly games = signal<GameDetails[]>([]);
  protected readonly hasMore = signal(false);
  protected readonly loading = signal(true);
  protected readonly error = signal<Message | null>(null);

  private readonly more = new Subject<void>();

  constructor() {
    // Another filter starts over from its newest game (a late page of the previous one is dropped); "show older"
    // asks for the page after what is listed, ignored while a page is on its way.
    toObservable(this.status)
      .pipe(
        switchMap((status) => {
          this.games.set([]);
          this.hasMore.set(false);
          this.error.set(null);

          return this.more.pipe(
            startWith(undefined),
            exhaustMap(() => {
              this.loading.set(true);
              this.error.set(null);
              return this.gamesApi.getHistory(status, this.games().length, PAGE_SIZE).pipe(
                catchError((error) => {
                  this.error.set(toProblem(error).message);
                  this.loading.set(false);
                  return EMPTY;
                }),
              );
            }),
          );
        }),
        tap((page) => {
          this.games.update((games) => [...games, ...page.games]);
          this.hasMore.set(page.hasMore);
          this.loading.set(false);
        }),
        takeUntilDestroyed(),
      )
      .subscribe();
  }

  protected loadMore(): void {
    this.more.next();
  }

  protected losers(game: GameDetails) {
    return game.players.filter((player) => player.isLoser);
  }
}
