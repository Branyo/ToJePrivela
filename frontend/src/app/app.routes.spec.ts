import { Component, input } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { statisticsMatcher } from './app.routes';

@Component({ selector: 'app-stub', template: '' })
class Stub {
  readonly playerId = input<string | undefined>();
}

describe('statisticsMatcher', () => {
  async function harness(): Promise<RouterTestingHarness> {
    TestBed.configureTestingModule({
      providers: [provideRouter([{ matcher: statisticsMatcher, component: Stub }], withComponentInputBinding())],
    });
    return RouterTestingHarness.create();
  }

  it('keeps the page while moving between the roster and a player', async () => {
    const router = await harness();

    const roster = await router.navigateByUrl('/statistics', Stub);
    expect(roster.playerId()).toBeUndefined();

    const picked = await router.navigateByUrl('/statistics/5', Stub);
    expect(picked).toBe(roster);
    expect(picked.playerId()).toBe('5');

    const back = await router.navigateByUrl('/statistics', Stub);
    expect(back).toBe(roster);
    expect(back.playerId()).toBeUndefined();
  });

  it('matches nothing else', async () => {
    const router = await harness();

    await expect(router.navigateByUrl('/statistics/5/more')).rejects.toThrow();
    await expect(router.navigateByUrl('/stats')).rejects.toThrow();
  });
});
