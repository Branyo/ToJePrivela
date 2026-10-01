import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { GameRulesStore } from './game-rules-store';
import { TEST_RULES } from './testing';

describe('GameRulesStore', () => {
  let store: GameRulesStore;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    store = TestBed.inject(GameRulesStore);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('keeps the rules the backend serves', async () => {
    const loading = store.load();
    http.expectOne('/api/rules').flush(TEST_RULES);
    await loading;

    expect(store.rules()).toEqual(TEST_RULES);
  });

  it('lets the app start without them when the backend is down', async () => {
    const loading = store.load();
    http.expectOne('/api/rules').error(new ProgressEvent('error'));
    await loading;

    expect(store.rules()).toBeNull();
  });
});
