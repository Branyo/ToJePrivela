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

export type IdentityProvider = 'Google' | 'Facebook';

/** A login: one identity at Google or Facebook. Players and games belong to it. */
export interface Account {
  id: number;
  provider: IdentityProvider;
  displayName: string;
  email: string | null;
  /** Admins manage the shared questions and categories, AI generation included. */
  isAdmin: boolean;
}

/** A provider the backend is configured for, with the public id its browser SDK is initialised with. */
export interface SignInProvider {
  provider: IdentityProvider;
  clientId: string;
}

export interface SignedIn {
  /** Sent as `Authorization: Bearer …` with every request until `expiresAt`. */
  accessToken: string;
  expiresAt: string;
  account: Account;
}

export interface GeneratedAiQuestions {
  summary: QuestionGenerationSummary;
  questions: Question[];
}

export interface DeletedAiQuestions {
  deleted: number;
}
