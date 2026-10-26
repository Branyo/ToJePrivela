import { ChangeDetectionStrategy, Component, inject, input, numberAttribute, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { EMPTY, catchError, of, switchMap, tap } from 'rxjs';
import { AccountData, AdminAccountData } from '../../core/api/account-data';
import { AdminApi } from '../../core/api/admin-api';
import { AccountOverview } from '../../core/api/models';
import { toProblem } from '../../core/api/problem';
import { Message } from '../../core/i18n/language';
import { MessagePipe } from '../../core/i18n/message.pipe';
import { AccountDates } from './account-dates';

/** The child routes, in tab order; `/accounts/:accountId` itself opens the first. */
const TABS = [
  { path: 'players', label: 'accounts.tabs.players' },
  { path: 'history', label: 'accounts.tabs.history' },
  { path: 'statistics', label: 'accounts.tabs.statistics' },
] as const;

/**
 * One login as an admin sees it: who it is, and one tab per child route showing its players, games and statistics on
 * the screens the login itself uses, which read the login's data through the `AdminAccountData` provided here. The tabs
 * open only once the login is known to exist, so a missing one is told once, not by every screen.
 */
@Component({
  selector: 'app-account-view',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, AccountDates, TranslatePipe, MessagePipe],
  templateUrl: './account-view.html',
  styleUrl: './account-view.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [{ provide: AccountData, useClass: AdminAccountData }],
})
export class AccountView {
  private readonly api = inject(AdminApi);
  protected readonly tabs = TABS;

  /** NaN for an id that is not a number, which no login has. */
  readonly accountId = input.required({ transform: (value: string) => numberAttribute(value, NaN) });

  /** Undefined until the login is read, null when none has the id (deleted meanwhile, or a mistyped address). */
  protected readonly account = signal<AccountOverview | null | undefined>(undefined);
  protected readonly error = signal<Message | null>(null);

  constructor() {
    // Another login in the address drops what was shown and reads that one; a late reply for the previous is ignored.
    toObservable(this.accountId)
      .pipe(
        tap(() => {
          this.account.set(undefined);
          this.error.set(null);
        }),
        switchMap((id) =>
          Number.isNaN(id)
            ? of(null)
            : this.api.getAccount(id).pipe(
                catchError((error) => {
                  const problem = toProblem(error);
                  if (problem.code === 'Account.NotFound') {
                    return of(null);
                  }
                  this.error.set(problem.message);
                  return EMPTY;
                }),
              ),
        ),
        takeUntilDestroyed(),
      )
      .subscribe((account) => this.account.set(account));
  }
}
