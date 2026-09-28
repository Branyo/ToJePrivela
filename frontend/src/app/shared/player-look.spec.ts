import { playerLook } from './player-look';

describe('playerLook', () => {
  it('shows the assigned animal', () => {
    expect(playerLook('🐼').animal).toBe('🐼');
  });

  it('gives the same animal the same colour every time', () => {
    expect(playerLook('🦊').color).toBe(playerLook('🦊').color);
    expect(playerLook('🦊').color).not.toBe(playerLook('🐸').color);
  });

  it('still colours an animal the pool does not list', () => {
    expect(playerLook('🐲').color).toMatch(/^#[0-9a-f]{6}$/);
  });
});
