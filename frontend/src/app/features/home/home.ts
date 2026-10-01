import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { forkJoin } from 'rxjs';
import { GamesApi } from '../../core/api/games-api';
import { Game, Player } from '../../core/api/models';
import { PlayersApi } from '../../core/api/players-api';
import { toProblem } from '../../core/api/problem';
import { LanguageService, Message } from '../../core/i18n/language';
import { MessagePipe } from '../../core/i18n/message.pipe';
import { badPointsParams } from '../../core/rules/bad-points-params';
import { GameRulesStore } from '../../core/rules/game-rules-store';

interface RunningGame {
  game: Game;
  names: string[];
}

/** The "how to play" cards; texts live under `home.rules.<key>`. */
const RULES = [
  { key: 'question', icon: '❓', tilt: '-2deg', color: 'var(--sky)' },
  { key: 'estimate', icon: '🗣️', tilt: '1.5deg', color: 'var(--mint)' },
  { key: 'double', icon: '✌️', tilt: '-2.5deg', color: 'var(--sky)' },
  { key: 'call', icon: '✋', tilt: '-1deg', color: 'var(--bubblegum)' },
  { key: 'card', icon: '🫏', tilt: '2deg', color: 'var(--grape)' },
  { key: 'next', icon: '🔁', tilt: '-1.5deg', color: 'var(--sun)' },
  { key: 'end', icon: '🏁', tilt: '1deg', color: 'var(--tangerine)' },
] as const;

@Component({
  selector: 'app-home',
  imports: [RouterLink, DatePipe, TranslatePipe, MessagePipe],
  templateUrl: './home.html',
  styleUrl: './home.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Home {
  private readonly gamesApi = inject(GamesApi);
  private readonly playersApi = inject(PlayersApi);
  protected readonly i18n = inject(LanguageService);
  protected readonly rules = RULES;
  private readonly gameRules = inject(GameRulesStore).rules;
  protected readonly badPoints = computed(() => badPointsParams(this.gameRules()));

  private readonly games = signal<Game[]>([]);
  private readonly players = signal<Player[]>([]);
  protected readonly error = signal<Message | null>(null);

  /** Unfinished games, newest first, so a refreshed browser can jump back in. */
  protected readonly running = computed<RunningGame[]>(() => {
    const names = new Map(this.players().map((p) => [p.id, p.name]));
    return this.games()
      .filter((game) => game.finished === null)
      .sort((a, b) => b.id - a.id)
      .slice(0, 5)
      .map((game) => ({ game, names: game.playerIds.map((id) => names.get(id) ?? '?') }));
  });

  constructor() {
    forkJoin([this.gamesApi.getAll(), this.playersApi.getAll()]).subscribe({
      next: ([games, players]) => {
        this.games.set(games);
        this.players.set(players);
      },
      error: (error) => this.error.set(toProblem(error).message),
    });
  }
}
