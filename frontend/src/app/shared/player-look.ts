import { Avatar } from '../core/api/models';

/** Behind an animal the server's pool (`GameRules.avatars`) does not list, or before the pool has loaded. */
export const UNKNOWN_AVATAR_COLOR = '#c9c9d4';

/** Shown for a player deleted since; not one of the animals a player can be given. */
export const UNKNOWN_PLAYER_AVATAR = '👤';

export interface PlayerLook {
  animal: string;
  color: string;
}

/** The background colour is the one the server keeps with the animal. */
export function playerLook(avatar: string, pool: readonly Avatar[]): PlayerLook {
  const color = pool.find((entry) => entry.emoji === avatar)?.color ?? UNKNOWN_AVATAR_COLOR;
  return { animal: avatar, color };
}
