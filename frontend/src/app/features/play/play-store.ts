import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { GamesApi } from '../../core/api/games-api';
import { GameDetails, GamePlayer, Question } from '../../core/api/models';
import { toProblem } from '../../core/api/problem';
import { Message } from '../../core/i18n/language';
import { QuestionsApi } from '../../core/api/questions-api';
import { seatPlayers } from '../../shared/ranking';

export type PlayPhase =
  | 'loading'
  | 'asking'
  | 'revealed'
  | 'awarding'
  | 'no-questions'
  | 'finished'
  | 'error';

/** State of one running game screen: the scoreboard, the current question and where the round is. */
@Injectable()
export class PlayStore {
  private readonly gamesApi = inject(GamesApi);
  private readonly questionsApi = inject(QuestionsApi);

  private gameId = 0;
  private categoryIds: readonly number[] = [];

  readonly game = signal<GameDetails | null>(null);
  readonly question = signal<Question | null>(null);
  readonly phase = signal<PlayPhase>('loading');
  readonly error = signal<Message | null>(null);
  readonly questionNumber = signal(0);

  readonly seats = computed(() => seatPlayers(this.game()?.players ?? []));
  readonly badCardLimit = computed(() => this.game()?.badCardLimit ?? 0);
  readonly cardSlots = computed(() => Array.from({ length: this.badCardLimit() }));

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
      this.questionNumber.update((n) => n + 1);
      this.phase.set('asking');
    } catch (error) {
      if (toProblem(error).code === 'Question.NoneAvailable') {
        this.question.set(null);
        this.phase.set('no-questions');
        return;
      }
      this.fail(error);
    }
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
   * Gives the current question's card to the player. The scoreboard updates only once `landing`
   * resolves, so the numbers change when the flying card arrives.
   */
  async award(playerId: number, landing: Promise<void> = Promise.resolve()): Promise<void> {
    const question = this.question();
    if (this.phase() !== 'revealed' || !question) {
      return;
    }

    this.phase.set('awarding');

    try {
      const [game] = await Promise.all([
        firstValueFrom(this.gamesApi.awardBadCard(this.gameId, playerId, question.id)),
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
