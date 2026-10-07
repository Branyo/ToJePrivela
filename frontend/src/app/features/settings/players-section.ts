import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { GameRules, Player } from '../../core/api/models';
import { PlayersApi } from '../../core/api/players-api';
import { toProblem } from '../../core/api/problem';
import { LanguageService, Message, compareNames } from '../../core/i18n/language';
import { MessagePipe } from '../../core/i18n/message.pipe';
import { GameRulesStore } from '../../core/rules/game-rules-store';
import { PlayerAvatar } from '../../shared/player-avatar';

/**
 * The signed-in login's own players: add them here or at the table, delete the ones you no longer need. A login keeps
 * at most `maxPlayersPerAccount` of them. Without the backend's rules the limits are unknown rather than zero: the list
 * still shows, and adding says that the rules are missing.
 */
@Component({
  selector: 'app-players-section',
  imports: [RouterLink, PlayerAvatar, TranslatePipe, MessagePipe],
  templateUrl: './players-section.html',
  styleUrl: './players-section.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlayersSection {
  private readonly api = inject(PlayersApi);
  private readonly i18n = inject(LanguageService);
  private readonly rulesStore = inject(GameRulesStore);

  protected readonly nameLimit = computed(() => this.rulesStore.rules()?.playerName ?? null);
  protected readonly maxPlayers = computed(() => this.rulesStore.rules()?.maxPlayersPerAccount ?? null);
  protected readonly full = computed(() => {
    const max = this.maxPlayers();
    return max !== null && (this.players()?.length ?? 0) >= max;
  });
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
    void this.rulesStore.ensureLoaded();
    this.api.getAll().subscribe({
      next: (players) => this.players.set(players),
      error: (error) => this.error.set(toProblem(error).message),
    });
  }

  protected async add(): Promise<void> {
    if (this.adding()) {
      return;
    }

    this.adding.set(true);
    this.error.set(null);
    // Only waits when the rules are missing, to try loading them again.
    const rules = this.rulesStore.rules() ?? (await this.rulesStore.ensureLoaded());
    const problem = this.validateNewPlayer(rules);
    if (problem) {
      this.error.set(problem);
      this.adding.set(false);
      return;
    }

    this.api.create(this.newName().trim()).subscribe({
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

  private validateNewPlayer(rules: GameRules | null): Message | null {
    if (!rules) {
      return { key: 'errors.rulesUnavailable' };
    }

    if ((this.players()?.length ?? 0) >= rules.maxPlayersPerAccount) {
      return { key: 'players.full', params: { max: rules.maxPlayersPerAccount } };
    }

    const name = this.newName().trim();
    const { min, max } = rules.playerName;
    if (name.length < min || name.length > max) {
      return { key: 'setup.errors.nameLength', params: { min, max } };
    }

    return null;
  }
}
