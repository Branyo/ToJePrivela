import { GameRules } from '../api/models';

/** Rules as the backend serves them, for specs. */
export const TEST_RULES: GameRules = {
  players: { min: 2, max: 12 },
  badCardLimit: { min: 2, max: 10 },
  defaultBadCardLimit: 3,
  badPoints: { min: 1, max: 5 },
  playerName: { min: 2, max: 50 },
  maxPlayersPerAccount: 100,
  categoryName: { min: 2, max: 32 },
  maxAiQuestionCount: 200,
  avatars: ['🦊', '🐸', '🐼'],
  loginName: { min: 3, max: 30 },
  password: { min: 8, max: 128 },
};
