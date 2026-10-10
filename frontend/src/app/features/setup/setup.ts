import { ChangeDetectionStrategy, Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { CategoriesApi } from '../../core/api/categories-api';
import { GamesApi } from '../../core/api/games-api';
import { BadPointsMode, Limit, Player, QuestionCategory } from '../../core/api/models';
import { PlayersApi } from '../../core/api/players-api';
import { AuthStore } from '../../core/auth/auth-store';
import { toProblem } from '../../core/api/problem';
import { LanguageService, Message, compareNames } from '../../core/i18n/language';
import { MessagePipe } from '../../core/i18n/message.pipe';
import { badPointsParams } from '../../core/rules/bad-points-params';
import { GameRulesStore } from '../../core/rules/game-rules-store';
import { PlayerAvatar } from '../../shared/player-avatar';

/** Without the backend's rules nothing is allowed; the screen's own requests report the outage. */
const NOTHING: Limit = { min: 0, max: 0 };

@Component({
  selector: 'app-setup',
  imports: [RouterLink, PlayerAvatar, TranslatePipe, MessagePipe],
  templateUrl: './setup.html',
  styleUrl: './setup.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Setup {
  private readonly playersApi = inject(PlayersApi);
  private readonly gamesApi = inject(GamesApi);
  private readonly categoriesApi = inject(CategoriesApi);
  private readonly router = inject(Router);
  protected readonly i18n = inject(LanguageService);
  private readonly rules = inject(GameRulesStore).rules;
  /** Only admins can add questions, so only they are sent to the settings screen when there are none. */
  protected readonly isAdmin = inject(AuthStore).isAdmin;

  protected readonly playerLimit = computed(() => this.rules()?.players ?? NOTHING);
  protected readonly cardLimit = computed(() => this.rules()?.badCardLimit ?? NOTHING);
  protected readonly playerNameLimit = computed(() => this.rules()?.playerName ?? NOTHING);
  protected readonly badPoints = computed(() => badPointsParams(this.rules()));

  protected readonly players = signal<Player[]>([]);
  protected readonly selectedIds = signal<number[]>([]);
  protected readonly newName = signal('');
  protected readonly badCardLimit = signal(this.rules()?.defaultBadCardLimit ?? 0);
  protected readonly badPointsMode = signal<BadPointsMode>('Question');
  protected readonly badPointsModes: readonly BadPointsMode[] = ['Question', 'Chooser'];

  protected readonly categories = signal<QuestionCategory[]>([]);
  protected readonly questionCounts = signal<ReadonlyMap<number, number>>(new Map());
  protected readonly selectedCategoryIds = signal<number[]>([]);

  protected readonly starting = signal(false);
  protected readonly error = signal<Message | null>(null);

  /** Seated by id, the same order the game screens use. */
  protected readonly selectedPlayers = computed(() => {
    const byId = new Map(this.players().map((p) => [p.id, p]));
    return [...this.selectedIds()].sort((a, b) => a - b).flatMap((id) => byId.get(id) ?? []);
  });

  protected readonly benchPlayers = computed(() => {
    const selected = new Set(this.selectedIds());
    const locale = this.i18n.locale();
    return this.players()
      .filter((p) => !selected.has(p.id))
      .sort((a, b) => compareNames(a.name, b.name, locale));
  });

  protected readonly sortedCategories = computed(() => {
    const locale = this.i18n.locale();
    return [...this.categories()].sort((a, b) => compareNames(a.name, b.name, locale));
  });

  protected readonly limitCards = computed(() => Array.from({ length: this.badCardLimit() }));

  protected readonly isFull = computed(() => this.selectedIds().length >= this.playerLimit().max);

  /** Questions the chosen categories hold; all categories count when none is chosen. */
  protected readonly availableQuestions = computed(() => {
    const counts = this.questionCounts();
    const chosen = this.selectedCategoryIds();
    const ids = chosen.length > 0 ? chosen : [...counts.keys()];
    return ids.reduce((sum, id) => sum + (counts.get(id) ?? 0), 0);
  });

  protected readonly questionsLabel = computed(() => {
    const count = this.availableQuestions();
    return this.i18n.instant(this.i18n.pluralKey('setup.categories.questions', count), {
      count: this.i18n.formatNumber(count),
    });
  });

  protected readonly startBlocker = computed<Message | null>(() => {
    const count = this.selectedIds().length;
    const min = this.playerLimit().min;
    if (count < min) {
      const missing = min - count;
      return { key: this.i18n.pluralKey('setup.errors.missingPlayers', missing), params: { count: missing } };
    }
    if (this.availableQuestions() === 0) {
      return { key: this.isAdmin() ? 'setup.errors.noQuestionsAdmin' : 'setup.errors.noQuestions' };
    }
    return null;
  });

  constructor() {
    this.loadPlayers();
    // Category names arrive in the shown language, so they are fetched again after a switch.
    effect(() => {
      this.i18n.language();
      untracked(() => this.loadCategories());
    });
  }

  protected addPlayer(): void {
    const name = this.newName().trim();
    const { min, max } = this.playerNameLimit();
    if (name.length < min || name.length > max) {
      this.error.set({ key: 'setup.errors.nameLength', params: { min, max } });
      return;
    }
    if (this.isFull()) {
      this.error.set({ key: 'setup.errors.tableFull', params: { max: this.playerLimit().max } });
      return;
    }

    const existing = this.findByName(name);
    if (existing) {
      this.select(existing.id);
      this.newName.set('');
      return;
    }

    this.error.set(null);
    this.playersApi.create(name).subscribe({
      next: (player) => {
        this.players.update((players) => [...players, player]);
        this.select(player.id);
        this.newName.set('');
      },
      error: (error) => {
        const problem = toProblem(error);
        if (problem.status === 409) {
          // Someone else added the name meanwhile: pick up the stored player instead.
          this.loadPlayers(() => {
            const match = this.findByName(name);
            if (match) {
              this.select(match.id);
              this.newName.set('');
            }
          });
          return;
        }
        this.error.set(problem.message);
      },
    });
  }

  protected select(id: number): void {
    if (this.selectedIds().includes(id) || this.isFull()) {
      return;
    }
    this.error.set(null);
    this.selectedIds.update((ids) => [...ids, id]);
  }

  protected unselect(id: number): void {
    this.selectedIds.update((ids) => ids.filter((selected) => selected !== id));
  }

  protected changeLimit(delta: number): void {
    const { min, max } = this.cardLimit();
    this.badCardLimit.update((limit) => Math.min(max, Math.max(min, limit + delta)));
  }

  protected toggleCategory(id: number): void {
    this.selectedCategoryIds.update((ids) => (ids.includes(id) ? ids.filter((c) => c !== id) : [...ids, id]));
  }

  protected isCategorySelected(id: number): boolean {
    return this.selectedCategoryIds().includes(id);
  }

  protected countFor(id: number): number {
    return this.questionCounts().get(id) ?? 0;
  }

  protected start(): void {
    if (this.startBlocker() || this.starting()) {
      return;
    }

    this.starting.set(true);
    this.error.set(null);
    this.gamesApi.create(this.selectedIds(), this.badCardLimit(), this.badPointsMode()).subscribe({
      next: (game) => {
        const categories = this.selectedCategoryIds();
        void this.router.navigate(['/games', game.id], {
          queryParams: categories.length > 0 ? { categories: categories.join(',') } : {},
        });
      },
      error: (error) => {
        this.starting.set(false);
        this.error.set(toProblem(error).message);
      },
    });
  }

  protected onNameInput(event: Event): void {
    this.newName.set((event.target as HTMLInputElement).value);
  }

  private findByName(name: string): Player | undefined {
    const wanted = name.toLocaleLowerCase();
    return this.players().find((p) => p.name.toLocaleLowerCase() === wanted);
  }

  private loadPlayers(then?: () => void): void {
    this.playersApi.getAll().subscribe({
      next: (players) => {
        this.players.set(players);
        then?.();
      },
      error: (error) => this.error.set(toProblem(error).message),
    });
  }

  private loadCategories(): void {
    this.categoriesApi.getAll().subscribe({
      next: (categories) => {
        this.categories.set(categories);
        this.questionCounts.set(new Map(categories.map((category) => [category.id, category.questionCount])));
      },
      error: (error) => this.error.set(toProblem(error).message),
    });
  }
}
