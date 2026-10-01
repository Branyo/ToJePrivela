import { playerLook } from './player-look';

const POOL = ['🦊', '🐸', '🐼'];

describe('playerLook', () => {
  it('shows the assigned animal', () => {
    expect(playerLook('🐼', POOL).animal).toBe('🐼');
  });

  it('gives the same animal the same colour every time', () => {
    expect(playerLook('🦊', POOL).color).toBe(playerLook('🦊', POOL).color);
    expect(playerLook('🦊', POOL).color).not.toBe(playerLook('🐸', POOL).color);
  });

  it('colours by the place in the server pool, not by the animal', () => {
    expect(playerLook('🐸', ['🐸']).color).toBe(playerLook('🦊', ['🦊']).color);
  });

  it('still colours an animal the pool does not list', () => {
    expect(playerLook('🐲', POOL).color).toMatch(/^#[0-9a-f]{6}$/);
    expect(playerLook('🐲', []).color).toMatch(/^#[0-9a-f]{6}$/);
  });
});
