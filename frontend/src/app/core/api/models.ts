/** Mirrors the backend DTOs (camelCase JSON). */

export interface Player {
  id: number;
  name: string;
  /** Assigned at random by the server when the player is created; never changes. */
  avatar: string;
}

export interface Game {
  id: number;
  started: string | null;
  finished: string | null;
  badCardLimit: number;
  playerIds: number[];
}

export interface GamePlayer {
  playerId: number;
  name: string;
  avatar: string;
  badPoints: number;
  badCards: number;
  /** Doubles that held; each takes one bad point off `finalBadPoints`. */
  doubles: number;
  /** `badPoints` minus one per double: this is what decides the loser. */
  finalBadPoints: number;
}

export interface GameDetails {
  id: number;
  started: string | null;
  finished: string | null;
  badCardLimit: number;
  players: GamePlayer[];
}

export interface QuestionCategory {
  id: number;
  name: string;
  addedByPlayer: Player | null;
}

export interface QuestionGenerationSummary {
  requested: number;
  created: number;
  discarded: number;
}

export interface CreatedQuestionCategory extends QuestionCategory {
  questionGeneration: QuestionGenerationSummary;
}

export interface Question {
  id: number;
  text: string;
  answer: string;
  categoryId: number;
  categoryName: string;
  badPoints: number;
  source: 'Manual' | 'Ai';
  createdAt: string;
  viewCount: number;
  lastViewedAt: string | null;
}

export const MIN_PLAYERS = 2;
export const MAX_PLAYERS = 12;
export const MIN_BAD_CARD_LIMIT = 2;
export const MAX_BAD_CARD_LIMIT = 10;
export const DEFAULT_BAD_CARD_LIMIT = 3;
