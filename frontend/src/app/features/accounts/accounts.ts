import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AdminApi } from '../../core/api/admin-api';
import { AccountOverview } from '../../core/api/models';
import { toProblem } from '../../core/api/problem';
import { AuthStore } from '../../core/auth/auth-store';
import { Message } from '../../core/i18n/language';
import { MessagePipe } from '../../core/i18n/message.pipe';
import { AccountDates } from './account-dates';

/**
 * Every login, for admins: the most recently signed in first, in the server's order. Opening one shows its players,
 * games and statistics read only; deleting one (never your own) takes all its players and games along, after a second
 * tap.
 */
@Component({
  selector: 'app-accounts',
  imports: [RouterLink, AccountDates, TranslatePipe, MessagePipe],
  templateUrl: './accounts.html',
  styleUrl: './accounts.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Accounts {
  private readonly api = inject(AdminApi);
  protected readonly auth = inject(AuthStore);

  protected readonly accounts = signal<AccountOverview[] | null>(null);
  /** The login whose deletion waits for a second tap, and the one being deleted. */
  protected readonly confirmingId = signal<number | null>(null);
  protected readonly deletingId = signal<number | null>(null);
  protected readonly error = signal<Message | null>(null);

  constructor() {
    this.api.getAccounts().subscribe({
      next: (accounts) => this.accounts.set(accounts),
      error: (error) => this.error.set(toProblem(error).message),
    });
  }

  protected askToDelete(id: number): void {
    this.error.set(null);
    this.confirmingId.set(id);
  }

  protected cancelDelete(): void {
    this.confirmingId.set(null);
  }

  protected delete(id: number): void {
    if (this.deletingId() !== null) {
      return;
    }

    this.deletingId.set(id);
    this.api.deleteAccount(id).subscribe({
      next: () => this.forget(id),
      error: (error) => {
        const problem = toProblem(error);
        // Deleted meanwhile (another admin, another tab): it is gone either way.
        if (problem.code === 'Account.NotFound') {
          this.forget(id);
          return;
        }
        this.error.set(problem.message);
        this.deletingId.set(null);
      },
    });
  }

  private forget(id: number): void {
    this.accounts.update((accounts) => (accounts ?? []).filter((account) => account.id !== id));
    this.confirmingId.set(null);
    this.deletingId.set(null);
  }
}
