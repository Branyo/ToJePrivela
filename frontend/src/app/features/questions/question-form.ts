import { ChangeDetectionStrategy, Component, computed, inject, input, linkedSignal, output, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { Question, QuestionDraft } from '../../core/api/models';
import { Message } from '../../core/i18n/language';
import { MessagePipe } from '../../core/i18n/message.pipe';
import { GameRulesStore } from '../../core/rules/game-rules-store';

/**
 * Both texts, the answer and the bad points of a manual question: empty for a new one, filled in for an edit. Checks
 * what the backend's rules allow before it hands the draft over; the answer's format is left to the backend.
 */
@Component({
  selector: 'app-question-form',
  imports: [TranslatePipe, MessagePipe],
  templateUrl: './question-form.html',
  styleUrl: './question-form.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class QuestionForm {
  private readonly rulesStore = inject(GameRulesStore);

  /** `null` for a new question. */
  readonly question = input<Question | null>(null);
  /** Names the category of a new question in its title. */
  readonly categoryName = input('');
  readonly busy = input(false);
  /** Why the backend refused the last draft. */
  readonly problem = input<Message | null>(null);

  readonly saved = output<QuestionDraft>();
  readonly cancelled = output<void>();

  /** `null` while the backend's rules are missing; the inputs then take anything and saving says so. */
  protected readonly textLimit = computed(() => this.rulesStore.rules()?.questionText ?? null);
  protected readonly badPointOptions = computed(() => {
    const limit = this.rulesStore.rules()?.badPoints;
    return limit ? Array.from({ length: limit.max - limit.min + 1 }, (_, index) => limit.min + index) : [];
  });

  /**
   * The question the fields start from. A reload hands over a new object for the same question, which must not
   * throw away what was typed, so only another id counts as a change.
   */
  private readonly original = computed(() => this.question(), { equal: (a, b) => a?.id === b?.id });

  protected readonly textSk = linkedSignal(() => this.original()?.textSk ?? '');
  protected readonly textEn = linkedSignal(() => this.original()?.textEn ?? '');
  protected readonly answer = linkedSignal(() => this.original()?.answer ?? '');
  protected readonly badPoints = linkedSignal<number | null>(() => this.original()?.badPoints ?? null);

  private readonly invalid = signal<Message | null>(null);
  protected readonly error = computed(() => this.invalid() ?? this.problem());

  /** Any other worth than the current one, so the dice always visibly changes something. */
  protected roll(): void {
    const options = this.badPointOptions().filter((points) => points !== this.badPoints());
    if (options.length > 0) {
      this.badPoints.set(options[Math.floor(Math.random() * options.length)]);
    }
  }

  protected submit(): void {
    const rules = this.rulesStore.rules();
    if (!rules) {
      this.invalid.set({ key: 'errors.rulesUnavailable' });
      return;
    }
    const textSk = this.textSk().trim();
    const textEn = this.textEn().trim();
    const answer = this.answer().trim();
    const badPoints = this.badPoints();
    const { min, max } = rules.questionText;
    const outOfLimit = (text: string) => text.length < min || text.length > max;

    if (outOfLimit(textSk)) {
      this.invalid.set({ key: 'questionAdmin.form.errors.textSk', params: { min, max } });
    } else if (outOfLimit(textEn)) {
      this.invalid.set({ key: 'questionAdmin.form.errors.textEn', params: { min, max } });
    } else if (answer.length === 0) {
      this.invalid.set({ key: 'questionAdmin.form.errors.answer' });
    } else if (badPoints === null) {
      this.invalid.set({ key: 'questionAdmin.form.errors.badPoints', params: { ...rules.badPoints } });
    } else {
      this.invalid.set(null);
      this.saved.emit({ textSk, textEn, answer, badPoints });
    }
  }

  protected onInput(field: 'textSk' | 'textEn' | 'answer', event: Event): void {
    this[field].set((event.target as HTMLInputElement | HTMLTextAreaElement).value);
  }
}
