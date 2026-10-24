import { ChangeDetectionStrategy, Component, computed, inject, input, numberAttribute, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
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
 * the screens the login itself uses, which read the login's data through the `AdminAccountData` provided here.
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

  private readonly accounts = signal<AccountOverview[] | null>(null);
  protected readonly error = signal<Message | null>(null);

  /** Undefined until the logins are there, null when none has the id (deleted meanwhile, or a mistyped address). */
  protected readonly account = computed(() => {
    const accounts = this.accounts();
    return accounts === null ? undefined : (accounts.find((account) => account.id === this.accountId()) ?? null);
  });

  constructor() {
    this.api.getAccounts().subscribe({
      next: (accounts) => this.accounts.set(accounts),
      error: (error) => this.error.set(toProblem(error).message),
    });
  }
}
