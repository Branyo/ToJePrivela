import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { CategoriesApi } from '../../core/api/categories-api';
import { GamesApi } from '../../core/api/games-api';
import {
  DEFAULT_BAD_CARD_LIMIT,
  MAX_BAD_CARD_LIMIT,
  MAX_PLAYERS,
  MIN_BAD_CARD_LIMIT,
  MIN_PLAYERS,
  Player,
  QuestionCategory,
} from '../../core/api/models';
import { PlayersApi } from '../../core/api/players-api';
import { toProblem } from '../../core/api/problem';
import { LanguageService, Message, compareNames } from '../../core/i18n/language';
import { MessagePipe } from '../../core/i18n/message.pipe';
import { QuestionsApi } from '../../core/api/questions-api';
import { PlayerAvatar } from '../../shared/player-avatar';

const NAME_MIN = 2;
const NAME_MAX = 50;
const CATEGORY_NAME_MIN = 2;
const CATEGORY_NAME_MAX = 32;
const QUESTIONS_MIN = 1;
const QUESTIONS_MAX = 200;

@Component({
  selector: 'app-setup',
  imports: [PlayerAvatar, TranslatePipe, MessagePipe],
  templateUrl: './setup.html',
  styleUrl: './setup.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Setup {
  private readonly playersApi = inject(PlayersApi);
  private readonly gamesApi = inject(GamesApi);
  private readonly categoriesApi = inject(CategoriesApi);
  private readonly questionsApi = inject(QuestionsApi);
  private readonly router = inject(Router);
  protected readonly i18n = inject(LanguageService);

  protected readonly minPlayers = MIN_PLAYERS;
  protected readonly maxPlayers = MAX_PLAYERS;
  protected readonly minLimit = MIN_BAD_CARD_LIMIT;
  protected readonly maxLimit = MAX_BAD_CARD_LIMIT;

  protected readonly players = signal<Player[]>([]);
  protected readonly selectedIds = signal<number[]>([]);
  protected readonly newName = signal('');
  protected readonly badCardLimit = signal(DEFAULT_BAD_CARD_LIMIT);

  protected readonly categories = signal<QuestionCategory[]>([]);
  protected readonly questionCounts = signal<ReadonlyMap<number, number>>(new Map());
  protected readonly selectedCategoryIds = signal<number[]>([]);
  protected readonly newCategoryName = signal('');
  protected readonly newCategoryCount = signal(20);
  protected readonly generating = signal(false);
  protected readonly generated = signal<Message | null>(null);

  protected readonly starting = signal(false);
  protected readonly error = signal<Message | null>(null);

  /** Seated by id, the same order the game screens use, so everyone keeps their animal. */
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

  protected readonly isFull = computed(() => this.selectedIds().length >= MAX_PLAYERS);

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
    if (count < MIN_PLAYERS) {
      const missing = MIN_PLAYERS - count;
      return { key: this.i18n.pluralKey('setup.errors.missingPlayers', missing), params: { count: missing } };
    }
    if (this.availableQuestions() === 0) {
      return { key: 'setup.errors.noQuestions' };
    }
    return null;
  });

  constructor() {
    this.loadPlayers();
    this.loadCategories();
  }

  protected addPlayer(): void {
    const name = this.newName().trim();
    if (name.length < NAME_MIN || name.length > NAME_MAX) {
      this.error.set({ key: 'setup.errors.nameLength', params: { min: NAME_MIN, max: NAME_MAX } });
      return;
    }
    if (this.isFull()) {
      this.error.set({ key: 'setup.errors.tableFull', params: { max: MAX_PLAYERS } });
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
    this.badCardLimit.update((limit) => Math.min(MAX_BAD_CARD_LIMIT, Math.max(MIN_BAD_CARD_LIMIT, limit + delta)));
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

  protected createCategory(): void {
    const name = this.newCategoryName().trim();
    const count = this.newCategoryCount();
    if (name.length < CATEGORY_NAME_MIN || name.length > CATEGORY_NAME_MAX) {
      this.error.set({ key: 'setup.errors.categoryNameLength', params: { min: CATEGORY_NAME_MIN, max: CATEGORY_NAME_MAX } });
      return;
    }
    if (!Number.isInteger(count) || count < QUESTIONS_MIN || count > QUESTIONS_MAX) {
      this.error.set({ key: 'setup.errors.questionCount', params: { min: QUESTIONS_MIN, max: QUESTIONS_MAX } });
      return;
    }

    this.error.set(null);
    this.generated.set(null);
    this.generating.set(true);
    this.categoriesApi.create(name, count).subscribe({
      next: (category) => {
        this.generating.set(false);
        this.newCategoryName.set('');
        this.generated.set({
          key: 'setup.generator.done',
          params: { name: category.name, count: category.questionGeneration.created },
        });
        this.selectedCategoryIds.update((ids) => [...ids, category.id]);
        this.loadCategories();
      },
      error: (error) => {
        this.generating.set(false);
        this.error.set(toProblem(error).message);
      },
    });
  }

  protected start(): void {
    if (this.startBlocker() || this.starting()) {
      return;
    }

    this.starting.set(true);
    this.error.set(null);
    this.gamesApi.create(this.selectedIds(), this.badCardLimit()).subscribe({
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

  protected onCategoryNameInput(event: Event): void {
    this.newCategoryName.set((event.target as HTMLInputElement).value);
  }

  protected onCategoryCountInput(event: Event): void {
    this.newCategoryCount.set(Number((event.target as HTMLInputElement).value));
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
      next: (categories) => this.categories.set(categories),
      error: (error) => this.error.set(toProblem(error).message),
    });
    this.questionsApi.getAll().subscribe({
      next: (questions) => {
        const counts = new Map<number, number>();
        for (const question of questions) {
          counts.set(question.categoryId, (counts.get(question.categoryId) ?? 0) + 1);
        }
        this.questionCounts.set(counts);
      },
      error: (error) => this.error.set(toProblem(error).message),
    });
  }
}
