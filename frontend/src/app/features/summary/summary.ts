import { ChangeDetectionStrategy, Component, OnInit, computed, inject, input, numberAttribute, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { GamesApi } from '../../core/api/games-api';
import { GameDetails } from '../../core/api/models';
import { toProblem } from '../../core/api/problem';
import { LanguageService, Message } from '../../core/i18n/language';
import { MessagePipe } from '../../core/i18n/message.pipe';
import { Confetti } from '../../shared/confetti';
import { PlayerAvatar } from '../../shared/player-avatar';
import { rankPlayers, seatPlayers } from '../../shared/ranking';

@Component({
  selector: 'app-summary',
  imports: [RouterLink, PlayerAvatar, Confetti, TranslatePipe, MessagePipe],
  templateUrl: './summary.html',
  styleUrl: './summary.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Summary implements OnInit {
  private readonly gamesApi = inject(GamesApi);
  private readonly router = inject(Router);
  protected readonly i18n = inject(LanguageService);

  readonly id = input.required({ transform: numberAttribute });
  /** Passed through so a rematch draws from the same categories. */
  readonly categories = input<string>();

  protected readonly game = signal<GameDetails | null>(null);
  protected readonly error = signal<Message | null>(null);
  protected readonly rematching = signal(false);

  protected readonly ranking = computed(() => rankPlayers(seatPlayers(this.game()?.players ?? []), this.i18n.locale()));
  protected readonly losers = computed(() => this.ranking().filter((player) => player.isLoser));
  protected readonly loserNames = computed(() =>
    this.losers()
      .map((p) => p.name ?? this.i18n.instant('common.unknownPlayer'))
      .join(` ${this.i18n.instant('common.and')} `),
  );
  /** The donkeys doing a Mexican wave next to the loser's name. */
  protected readonly waveDonkeys = Array.from({ length: 5 });
  /** A rematch needs every player of this game; a deleted one cannot be seated again. */
  protected readonly canRematch = computed(() => this.game()?.players.every((p) => p.playerId !== null) ?? false);
  protected readonly maxPoints = computed(() => Math.max(1, ...this.ranking().map((p) => p.finalBadPoints)));
  protected readonly totalCards = computed(() => this.ranking().reduce((sum, p) => sum + p.badCards, 0));

  ngOnInit(): void {
    this.gamesApi.getDetails(this.id()).subscribe({
      next: (game) => this.game.set(game),
      error: (error) => this.error.set(toProblem(error).message),
    });
  }

  /** Final points can drop below zero thanks to doubles; such a bar stays empty. */
  protected barWidth(points: number): number {
    return Math.round((Math.max(0, points) / this.maxPoints()) * 100);
  }

  protected rematch(): void {
    const game = this.game();
    if (!game || this.rematching() || !this.canRematch()) {
      return;
    }

    this.rematching.set(true);
    const playerIds = game.players.flatMap((p) => (p.playerId === null ? [] : [p.playerId]));
    this.gamesApi.create(playerIds, game.badCardLimit, game.badPointsMode).subscribe({
      next: (created) => {
        void this.router.navigate(['/games', created.id], {
          queryParams: this.categories() ? { categories: this.categories() } : {},
        });
      },
      error: (error) => {
        this.rematching.set(false);
        this.error.set(toProblem(error).message);
      },
    });
  }
}
