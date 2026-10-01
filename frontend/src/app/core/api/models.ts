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
  /** Ended without a result because one of its players was deleted. */
  cancelled: boolean;
  /** In seat order; `null` for a player deleted since. */
  playerIds: (number | null)[];
}

/** `playerId`, `name` and `avatar` are `null` for a player deleted since: an unknown player whose score stays. */
export interface GamePlayer {
  playerId: number | null;
  name: string | null;
  avatar: string | null;
  badPoints: number;
  badCards: number;
  /** Doubles that held; each takes one bad point off `finalBadPoints`. */
  doubles: number;
  /** `badPoints` minus one per double: this is what decides the loser. */
  finalBadPoints: number;
  /** 1-based, decided by the server; players who stand equal share it. */
  rank: number;
  /** The server's verdict: most final bad points, ties broken by cards; several on a full tie, nobody without cards. */
  isLoser: boolean;
}

/** A player who still exists; every player of a running game is one. */
export type KnownGamePlayer = GamePlayer & { playerId: number; name: string; avatar: string };

export function isKnownPlayer(player: GamePlayer): player is KnownGamePlayer {
  return player.playerId !== null;
}

export interface GameDetails {
  id: number;
  started: string | null;
  finished: string | null;
  badCardLimit: number;
  badPointsMode: BadPointsMode;
  /** Ended without a result (and without a loser) because one of its players was deleted. */
  cancelled: boolean;
  players: GamePlayer[];
}

export interface QuestionCategory {
  id: number;
  name: string;
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
export const MIN_BAD_POINTS = 1;
export const MAX_BAD_POINTS = 5;
