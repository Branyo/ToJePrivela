import { Avatar } from '../core/api/models';
import { UNKNOWN_AVATAR_COLOR, playerLook } from './player-look';

const POOL: Avatar[] = [
  { emoji: '🦊', color: '#3ddc97' },
  { emoji: '🐸', color: '#ff9f1c' },
  { emoji: '🐼', color: '#4cc9f0' },
];

describe('playerLook', () => {
  it('shows the assigned animal', () => {
    expect(playerLook('🐼', POOL).animal).toBe('🐼');
  });

  it('uses the colour the server keeps with the animal', () => {
    expect(playerLook('🦊', POOL).color).toBe('#3ddc97');
    expect(playerLook('🐸', POOL).color).toBe('#ff9f1c');
  });

  it('follows the animal, not its place in the pool', () => {
    expect(playerLook('🐼', [...POOL].reverse()).color).toBe('#4cc9f0');
  });

  it('falls back to a neutral colour for an animal the pool does not list', () => {
    expect(playerLook('🐲', POOL).color).toBe(UNKNOWN_AVATAR_COLOR);
    expect(playerLook('🐲', []).color).toBe(UNKNOWN_AVATAR_COLOR);
  });
});
