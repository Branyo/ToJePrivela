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
  /** In the language the request asked for (`Accept-Language`), ready to show. */
  name: string;
  nameSk: string;
  nameEn: string;
  /** Every question in the category. */
  questionCount: number;
  /** Those of them the AI wrote; an edited one counts as manual. */
  aiQuestionCount: number;
}

export interface QuestionGenerationSummary {
  requested: number;
  created: number;
  discarded: number;
}

export interface CreatedQuestionCategory extends Omit<QuestionCategory, 'questionCount' | 'aiQuestionCount'> {
  questionGeneration: QuestionGenerationSummary;
}

export interface Question {
  id: number;
  /** In the language the request asked for (`Accept-Language`), ready to show; Slovak while an English one is missing. */
  text: string;
  textSk: string;
  /** `null` for a question stored before texts became bilingual. */
  textEn: string | null;
  answer: string;
  categoryId: number;
  /** In the language the request asked for (`Accept-Language`), ready to show. */
  categoryName: string;
  badPoints: number;
  source: 'Manual' | 'Ai';
  createdAt: string;
  viewCount: number;
  lastViewedAt: string | null;
}

/** What an admin writes for a manual question, when adding one or editing any. */
export interface QuestionDraft {
  textSk: string;
  textEn: string;
  answer: string;
  badPoints: number;
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
  /** How many players one login may keep. */
  maxPlayersPerAccount: number;
  categoryName: Limit;
  maxAiQuestionCount: number;
  /** Every avatar a player can be given, in a fixed order. */
  avatars: string[];
  /** Length of a new login's name. */
  loginName: Limit;
  /** Length of a new login's password. */
  password: Limit;
  /** Length of a question's text, in either language. */
  questionText: Limit;
}

/** A login: a name and a password. Players and games belong to it. */
export interface Account {
  id: number;
  name: string;
  /** Admins manage the shared questions and categories, AI generation included. */
  isAdmin: boolean;
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
