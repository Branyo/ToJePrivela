import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideTranslateService } from '@ngx-translate/core';
import { Player } from '../../core/api/models';
import { GameRulesStore } from '../../core/rules/game-rules-store';
import { TEST_RULES } from '../../core/rules/testing';
import { PlayersSection } from './players-section';

const PLAYERS: Player[] = [
  { id: 2, name: 'Duri', avatar: '🐼' },
  { id: 1, name: 'Ana', avatar: '🦊' },
];

describe('PlayersSection', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [PlayersSection],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([]), provideTranslateService()],
    });
    http = TestBed.inject(HttpTestingController);
    TestBed.inject(GameRulesStore).rules.set(TEST_RULES);
  });

  afterEach(() => http.verify());

  async function render() {
    const fixture = TestBed.createComponent(PlayersSection);
    fixture.detectChanges();
    http.expectOne('/api/players').flush(PLAYERS);
    await fixture.whenStable();
    const page = fixture.nativeElement as HTMLElement;
    const names = () => [...page.querySelectorAll('.row__name')].map((name) => name.textContent?.trim());
    return { fixture, page, names };
  }

  it("lists the login's players by name", async () => {
    const { names } = await render();

    expect(names()).toEqual(['Ana', 'Duri']);
  });

  it('adds a player', async () => {
    const { fixture, page, names } = await render();

    const input = page.querySelector<HTMLInputElement>('.add-row input')!;
    input.value = 'Brano';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    page.querySelector<HTMLButtonElement>('.add-row button')!.click();
    const request = http.expectOne({ method: 'POST', url: '/api/players' });
    expect(request.request.body).toEqual({ name: 'Brano' });
    request.flush({ id: 3, name: 'Brano', avatar: '🐸' });
    await fixture.whenStable();

    expect(names()).toEqual(['Ana', 'Brano', 'Duri']);
  });

  it('deletes a player only after a second tap', async () => {
    const { fixture, page, names } = await render();

    page.querySelector<HTMLButtonElement>('.row__remove')!.click();
    await fixture.whenStable();
    http.expectNone({ method: 'DELETE' });
    expect(page.querySelector('.row--confirming .row__name')?.textContent).toContain('Ana');

    page.querySelector<HTMLButtonElement>('.row--confirming .btn--danger')!.click();
    http.expectOne({ method: 'DELETE', url: '/api/players/1' }).flush(null);
    await fixture.whenStable();

    expect(names()).toEqual(['Duri']);
  });

  it('can keep a player after all', async () => {
    const { fixture, page } = await render();

    page.querySelector<HTMLButtonElement>('.row__remove')!.click();
    await fixture.whenStable();
    page.querySelector<HTMLButtonElement>('.row--confirming .btn--ghost')!.click();
    await fixture.whenStable();

    http.expectNone({ method: 'DELETE' });
    expect(page.querySelector('.row--confirming')).toBeNull();
  });

  it('shows how many players the login has out of the most it may keep', async () => {
    const { page } = await render();

    expect(page.querySelector('.count')?.textContent).toContain('players.count');
    expect(page.querySelector('.full')).toBeNull();
  });

  it('stops adding once the login keeps the most players it may', async () => {
    TestBed.inject(GameRulesStore).rules.set({ ...TEST_RULES, maxPlayersPerAccount: PLAYERS.length });
    const { page } = await render();

    expect(page.querySelector<HTMLInputElement>('.add-row input')!.disabled).toBe(true);
    expect(page.querySelector<HTMLButtonElement>('.add-row button')!.disabled).toBe(true);
    expect(page.querySelector('.full')).not.toBeNull();
    expect(page.querySelector('.count--full')).not.toBeNull();
  });

  it('says the rules are missing instead of calling an empty list full', async () => {
    TestBed.inject(GameRulesStore).rules.set(null);
    const fixture = TestBed.createComponent(PlayersSection);
    fixture.detectChanges();
    http.expectOne('/api/rules').error(new ProgressEvent('error'));
    http.expectOne('/api/players').flush(PLAYERS);
    await fixture.whenStable();
    const page = fixture.nativeElement as HTMLElement;

    expect(page.querySelector('.full')).toBeNull();
    expect(page.querySelector('.count')?.textContent?.trim()).toBe(String(PLAYERS.length));
    const input = page.querySelector<HTMLInputElement>('.add-row input')!;
    expect(input.disabled).toBe(false);
    expect(input.hasAttribute('maxlength')).toBe(false);

    input.value = 'Brano';
    input.dispatchEvent(new Event('input'));
    await fixture.whenStable();
    page.querySelector<HTMLButtonElement>('.add-row button')!.click();
    http.expectOne('/api/rules').error(new ProgressEvent('error'));
    await new Promise((resolve) => setTimeout(resolve));
    await fixture.whenStable();

    expect(page.querySelector('.error-banner')?.textContent).toContain('errors.rulesUnavailable');
    http.expectNone('/api/players');
  });
});
