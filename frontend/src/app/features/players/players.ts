import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { Limit, Player } from '../../core/api/models';
import { PlayersApi } from '../../core/api/players-api';
import { toProblem } from '../../core/api/problem';
import { LanguageService, Message, compareNames } from '../../core/i18n/language';
import { MessagePipe } from '../../core/i18n/message.pipe';
import { GameRulesStore } from '../../core/rules/game-rules-store';
import { PlayerAvatar } from '../../shared/player-avatar';

const NOTHING: Limit = { min: 0, max: 0 };

/** The signed-in login's own players: add them here or at the table, delete the ones you no longer need. */
@Component({
  selector: 'app-players',
  imports: [RouterLink, PlayerAvatar, TranslatePipe, MessagePipe],
  templateUrl: './players.html',
  styleUrl: './players.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Players {
  private readonly api = inject(PlayersApi);
  private readonly i18n = inject(LanguageService);
  private readonly rules = inject(GameRulesStore).rules;

  protected readonly nameLimit = computed(() => this.rules()?.playerName ?? NOTHING);
  protected readonly players = signal<Player[] | null>(null);
  protected readonly newName = signal('');
  protected readonly adding = signal(false);
  /** The player whose deletion waits for a second tap. */
  protected readonly confirmingId = signal<number | null>(null);
  protected readonly deletingId = signal<number | null>(null);
  protected readonly error = signal<Message | null>(null);

  protected readonly sorted = computed(() => {
    const locale = this.i18n.locale();
    return [...(this.players() ?? [])].sort((a, b) => compareNames(a.name, b.name, locale));
  });

  constructor() {
    this.api.getAll().subscribe({
      next: (players) => this.players.set(players),
      error: (error) => this.error.set(toProblem(error).message),
    });
  }

  protected add(): void {
    const name = this.newName().trim();
    const { min, max } = this.nameLimit();
    if (name.length < min || name.length > max) {
      this.error.set({ key: 'setup.errors.nameLength', params: { min, max } });
      return;
    }

    this.adding.set(true);
    this.error.set(null);
    this.api.create(name).subscribe({
      next: (player) => {
        this.players.update((players) => [...(players ?? []), player]);
        this.newName.set('');
        this.adding.set(false);
      },
      error: (error) => {
        this.error.set(toProblem(error).message);
        this.adding.set(false);
      },
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
    this.deletingId.set(id);
    this.api.delete(id).subscribe({
      next: () => {
        this.players.update((players) => (players ?? []).filter((player) => player.id !== id));
        this.confirmingId.set(null);
        this.deletingId.set(null);
      },
      error: (error) => {
        this.error.set(toProblem(error).message);
        this.deletingId.set(null);
      },
    });
  }

  protected onNameInput(event: Event): void {
    this.newName.set((event.target as HTMLInputElement).value);
  }
}
