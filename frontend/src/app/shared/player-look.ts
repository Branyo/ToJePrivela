/** Every seat gets its own animal and colour; the donkey is reserved for bad cards. */
const ANIMALS = ['🦊', '🐸', '🐼', '🐯', '🐙', '🦄', '🐨', '🐧', '🦁', '🐵', '🐰', '🦉'];
const COLORS = [
  '#ff9f1c', '#3ddc97', '#4cc9f0', '#f15bb5', '#9b5de5', '#ff5d5d',
  '#00bbf9', '#fee440', '#80ed99', '#ffafcc', '#c77dff', '#f4a261',
];

export interface PlayerLook {
  animal: string;
  color: string;
}

export function playerLook(seat: number): PlayerLook {
  const index = ((seat % ANIMALS.length) + ANIMALS.length) % ANIMALS.length;
  return { animal: ANIMALS[index], color: COLORS[index] };
}
