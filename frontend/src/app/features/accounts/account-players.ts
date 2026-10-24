import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AccountData } from '../../core/api/account-data';
import { Player } from '../../core/api/models';
import { toProblem } from '../../core/api/problem';
import { LanguageService, Message, compareNames } from '../../core/i18n/language';
import { MessagePipe } from '../../core/i18n/message.pipe';
import { PlayerAvatar } from '../../shared/player-avatar';

/** A login's players alphabetically, read only; each opens their statistics. */
@Component({
  selector: 'app-account-players',
  imports: [RouterLink, PlayerAvatar, TranslatePipe, MessagePipe],
  templateUrl: './account-players.html',
  styleUrl: './account-players.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AccountPlayers {
  protected readonly data = inject(AccountData);
  private readonly i18n = inject(LanguageService);

  private readonly players = signal<Player[] | null>(null);
  protected readonly error = signal<Message | null>(null);

  protected readonly sorted = computed(() => {
    const locale = this.i18n.locale();
    const players = this.players();
    return players ? [...players].sort((a, b) => compareNames(a.name, b.name, locale)) : null;
  });

  constructor() {
    this.data.getPlayers().subscribe({
      next: (players) => this.players.set(players),
      error: (error) => this.error.set(toProblem(error).message),
    });
  }
}
