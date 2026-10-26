import { ChangeDetectionStrategy, Component, inject, input, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { EMPTY, Subject, catchError, exhaustMap, startWith, switchMap, tap } from 'rxjs';
import { AccountData } from '../../core/api/account-data';
import { GameLinkPipe } from '../../core/api/account-links';
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
 * The filter lives in the URL, so coming back from a game keeps it. An admin looking at another login sees its games
 * the same way (`AccountData`), each opening its results.
 */
@Component({
  selector: 'app-history',
  imports: [RouterLink, GameLinkPipe, DatePipe, PlayerAvatar, TranslatePipe, MessagePipe],
  templateUrl: './history.html',
  styleUrl: './history.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class History {
  protected readonly data = inject(AccountData);
  protected readonly historyLink = this.data.historyLink();
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
    // Another filter starts over from its newest game (a late page of the previous one is dropped); "show older" (and
    // "try again") asks for the page after the last game received, ignored while a page is on its way.
    toObservable(this.status)
      .pipe(
        switchMap((status) => {
          this.games.set([]);
          this.hasMore.set(false);
          this.error.set(null);
          // The last game the server sent, not the last one listed: a page may end with games already listed (moved
          // since), and continuing from an earlier one would fetch them again, page after page.
          let after: GameDetails | null = null;

          return this.more.pipe(
            startWith(undefined),
            exhaustMap(() => {
              this.loading.set(true);
              this.error.set(null);
              return this.data.getHistory(status, after, PAGE_SIZE).pipe(
                catchError((error) => {
                  this.error.set(toProblem(error).message);
                  this.loading.set(false);
                  return EMPTY;
                }),
              );
            }),
            tap((page) => (after = page.games.at(-1) ?? after)),
          );
        }),
        tap((page) => {
          // A game whose start was corrected meanwhile may come again; it stays where it was first listed.
          this.games.update((games) => {
            const listed = new Set(games.map((game) => game.id));
            return [...games, ...page.games.filter((game) => !listed.has(game.id))];
          });
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
}
