import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { GamesApi } from '../../core/api/games-api';
import { GameDetails } from '../../core/api/models';
import { toProblem } from '../../core/api/problem';
import { LanguageService, Message } from '../../core/i18n/language';
import { MessagePipe } from '../../core/i18n/message.pipe';
import { badPointsParams } from '../../core/rules/bad-points-params';
import { GameRulesStore } from '../../core/rules/game-rules-store';

/** How many running games the home screen offers to go back to; the history lists them all. */
export const RUNNING_SHOWN = 5;

interface RunningGame {
  game: GameDetails;
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
  protected readonly i18n = inject(LanguageService);
  protected readonly rules = RULES;
  private readonly gameRules = inject(GameRulesStore).rules;
  protected readonly badPoints = computed(() => badPointsParams(this.gameRules()));

  private readonly games = signal<GameDetails[]>([]);
  protected readonly error = signal<Message | null>(null);

  /** The newest unfinished games as the server lists them (newest start first), so a refreshed browser can jump back in. */
  protected readonly running = computed<RunningGame[]>(() =>
    this.games().map((game) => ({ game, names: game.players.map((player) => player.name ?? '?') })),
  );

  constructor() {
    // Only the few games shown are fetched, however long the login's history grows.
    this.gamesApi.getHistory('Running', null, RUNNING_SHOWN).subscribe({
      next: (page) => this.games.set(page.games),
      error: (error) => this.error.set(toProblem(error).message),
    });
  }
}
