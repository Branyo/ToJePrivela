/** Mirrors the backend DTOs (camelCase JSON). */

export interface Player {
  id: number;
  name: string;
  /** Assigned at random by the server when the player is created; never changes. */
  avatar: string;
}

/**
 * Where a game's bad cards take their worth from: `Question` uses the question's stored bad points, `Chooser` lets the
 * round's starting player set them (1–5) after seeing only the category.
 */
export type BadPointsMode = 'Question' | 'Chooser';

export interface Game {
  id: number;
  started: string | null;
  finished: string | null;
  badCardLimit: number;
  badPointsMode: BadPointsMode;
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
  badPointsMode: BadPointsMode;
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

/** Inclusive bounds of a number or of a text's length. */
export interface Limit {
  min: number;
  max: number;
}

/** The limits the backend enforces (`GET /api/rules`); the client never keeps its own copies. */
export interface GameRules {
  players: Limit;
  badCardLimit: Limit;
  defaultBadCardLimit: number;
  badPoints: Limit;
  playerName: Limit;
  categoryName: Limit;
  maxAiQuestionCount: number;
  /** Every avatar a player can be given, in a fixed order. */
  avatars: string[];
}
