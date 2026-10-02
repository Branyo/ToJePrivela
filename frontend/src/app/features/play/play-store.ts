import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, firstValueFrom } from 'rxjs';
import { GamesApi } from '../../core/api/games-api';
import { GameDetails, GamePlayer, Question, isKnownPlayer } from '../../core/api/models';
import { toProblem } from '../../core/api/problem';
import { Message } from '../../core/i18n/language';
import { QuestionsApi } from '../../core/api/questions-api';
import { GameRulesStore } from '../../core/rules/game-rules-store';
import { seatPlayers } from '../../shared/ranking';

export type PlayPhase =
  | 'loading'
  | 'choosing'
  | 'asking'
  | 'revealed'
  | 'awarding'
  | 'no-questions'
  | 'finished'
  | 'error';

/**
 * State of one running game screen: the scoreboard, the current question and where the round is. In a `Chooser` game
 * every question starts in `choosing`: only its category is shown until the starting player sets its bad points.
 */
@Injectable()
export class PlayStore {
  private readonly gamesApi = inject(GamesApi);
  private readonly questionsApi = inject(QuestionsApi);
  private readonly rules = inject(GameRulesStore).rules;

  private gameId = 0;
  private categoryIds: readonly number[] = [];
  /** Doubles still being saved; they go out one after another, and the round's card only after all of them. */
  private doubles: Promise<void> | null = null;

  readonly game = signal<GameDetails | null>(null);
  readonly question = signal<Question | null>(null);
  readonly phase = signal<PlayPhase>('loading');
  readonly error = signal<Message | null>(null);
  readonly questionNumber = signal(0);
  /** What the starting player set for the current question in a `Chooser` game; `null` until then. */
  readonly chosenBadPoints = signal<number | null>(null);

  /**
   * The players to play with. Deleting a player cancels their running games, so a game played here only ever
   * holds known players; an unknown one shows up only in a finished game, which goes straight to its summary.
   */
  readonly seats = computed(() => seatPlayers((this.game()?.players ?? []).filter(isKnownPlayer)));
  readonly badCardLimit = computed(() => this.game()?.badCardLimit ?? 0);
  readonly cardSlots = computed(() => Array.from({ length: this.badCardLimit() }));
  readonly choosesBadPoints = computed(() => this.game()?.badPointsMode === 'Chooser');
  /** What the current question's card is worth: the chosen value in a `Chooser` game, else the stored one. */
  readonly badPoints = computed(() =>
    this.choosesBadPoints() ? (this.chosenBadPoints() ?? 0) : (this.question()?.badPoints ?? 0),
  );
  /** While bidding and after "To je priveľa!", but before the card is handed out. */
  readonly canDouble = computed(() => this.phase() === 'asking' || this.phase() === 'revealed');

  /** One more card ends the game for this player (meaningless when a single card does it). */
  isOnTheEdge(player: GamePlayer): boolean {
    const limit = this.badCardLimit();
    return limit > 1 && player.badCards === limit - 1;
  }

  async start(gameId: number, categoryIds: readonly number[]): Promise<void> {
    this.gameId = gameId;
    this.categoryIds = categoryIds;
    this.phase.set('loading');
    this.error.set(null);

    try {
      const game = await firstValueFrom(this.gamesApi.getDetails(gameId));
      this.game.set(game);
    } catch (error) {
      this.fail(error);
      return;
    }

    if (this.game()?.finished) {
      this.phase.set('finished');
      return;
    }

    await this.nextQuestion();
  }

  /** Draws one of the least seen questions and counts it as shown. */
  async nextQuestion(): Promise<void> {
    this.phase.set('loading');
    this.error.set(null);

    try {
      const drawn = await firstValueFrom(this.questionsApi.getRandom(this.categoryIds));
      // A lost view only makes a repeat a bit likelier; the round can go on.
      await firstValueFrom(this.questionsApi.recordView(drawn.id)).catch(() => undefined);
      this.question.set(drawn);
      this.chosenBadPoints.set(null);
      this.questionNumber.update((n) => n + 1);
      this.phase.set(this.choosesBadPoints() ? 'choosing' : 'asking');
    } catch (error) {
      if (toProblem(error).code === 'Question.NoneAvailable') {
        this.question.set(null);
        this.phase.set('no-questions');
        return;
      }
      this.fail(error);
    }
  }

  /** The starting player set the question's worth; now its text is shown. */
  choose(badPoints: number): void {
    const limit = this.rules()?.badPoints;
    if (this.phase() !== 'choosing' || !limit || !Number.isInteger(badPoints) || badPoints < limit.min || badPoints > limit.max) {
      return;
    }

    this.chosenBadPoints.set(badPoints);
    this.phase.set('asking');
  }

  /** "To je priveľa!" – someone stopped the bidding. */
  reveal(): void {
    if (this.phase() === 'asking') {
      this.phase.set('revealed');
    }
  }

  /** Too easy or nobody wants it: nobody takes a card. */
  async skip(): Promise<void> {
    if (this.phase() === 'asking' || this.phase() === 'revealed') {
      await this.nextQuestion();
    }
  }

  /**
   * Credits the player with a double that held: the next player raised the doubled estimate or wrongly
   * called "To je priveľa!". Worth one bad point off at the end of the game.
   */
  awardDouble(playerId: number): Promise<void> {
    return this.changeDoubles(() => this.gamesApi.awardDouble(this.gameId, playerId));
  }

  /** Takes back a double tapped by mistake; the score may still end up below zero. */
  removeDouble(playerId: number): Promise<void> {
    const player = this.game()?.players.find((p) => p.playerId === playerId);
    if (!player || player.doubles === 0) {
      return this.doubles ?? Promise.resolve();
    }

    return this.changeDoubles(() => this.gamesApi.removeDouble(this.gameId, playerId));
  }

  /** Queues a doubles change behind the ones still being saved, so they reach the server in the order tapped. */
  private changeDoubles(request: () => Observable<GameDetails>): Promise<void> {
    if (!this.canDouble()) {
      return this.doubles ?? Promise.resolve();
    }

    const saved = (this.doubles ?? Promise.resolve()).then(async () => {
      try {
        this.game.set(await firstValueFrom(request()));
      } catch (error) {
        this.error.set(toProblem(error).message);
      }

      if (this.doubles === saved) {
        this.doubles = null;
      }
    });

    this.doubles = saved;
    return saved;
  }

  /**
   * Gives the current question's card to the player once every double tapped before it is saved. The scoreboard updates only once `landing`
   * resolves, so the numbers change when the flying card arrives.
   */
  async award(playerId: number, landing: Promise<void> = Promise.resolve()): Promise<void> {
    const question = this.question();
    if (this.phase() !== 'revealed' || !question) {
      return;
    }

    this.phase.set('awarding');
    if (this.doubles) {
      await this.doubles;
    }

    try {
      const [game] = await Promise.all([
        firstValueFrom(
          this.gamesApi.awardBadCard(
            this.gameId,
            playerId,
            question.id,
            this.choosesBadPoints() ? this.badPoints() : undefined,
          ),
        ),
        landing,
      ]);
      this.game.set(game);
    } catch (error) {
      // e.g. the game was finished from another device: reload the real state instead of guessing.
      const message = toProblem(error).message;
      await this.start(this.gameId, this.categoryIds);
      this.error.set(message);
      return;
    }

    if (this.game()?.finished) {
      this.phase.set('finished');
      return;
    }

    await this.nextQuestion();
  }

  async finish(): Promise<void> {
    try {
      this.game.set(await firstValueFrom(this.gamesApi.finish(this.gameId)));
      this.phase.set('finished');
    } catch (error) {
      this.fail(error);
    }
  }

  retry(): Promise<void> {
    return this.game() ? this.nextQuestion() : this.start(this.gameId, this.categoryIds);
  }

  private fail(error: unknown): void {
    this.error.set(toProblem(error).message);
    this.phase.set('error');
  }
}
