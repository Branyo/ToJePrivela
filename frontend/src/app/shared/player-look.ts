/**
 * The server gives every player an animal from this pool (`PlayerAvatars` in the domain) when they are created; the colour
 * follows the animal. The donkey is reserved for bad cards.
 */
const ANIMALS = [
  '🦊', '🐸', '🐼', '🐯', '🐙', '🦄', '🐨', '🐧', '🦁', '🐵', '🐰', '🦉',
  '🐶', '🐱', '🐻', '🐷', '🐮', '🐔', '🦋', '🐢', '🦀', '🐳', '🦒', '🦔',
];
const COLORS = [
  '#ff9f1c', '#3ddc97', '#4cc9f0', '#f15bb5', '#9b5de5', '#ff5d5d',
  '#00bbf9', '#fee440', '#80ed99', '#ffafcc', '#c77dff', '#f4a261',
];

/** Shown for a player deleted since; not one of the animals a player can be given. */
export const UNKNOWN_PLAYER_AVATAR = '👤';

export interface PlayerLook {
  animal: string;
  color: string;
}

export function playerLook(avatar: string): PlayerLook {
  const known = ANIMALS.indexOf(avatar);
  // An animal the pool no longer lists still gets a stable colour.
  const index = known >= 0 ? known : [...avatar].reduce((sum, char) => sum + (char.codePointAt(0) ?? 0), 0);
  return { animal: avatar, color: COLORS[index % COLORS.length] };
}
