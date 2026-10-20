import { ChangeDetectionStrategy, Component, input, model, output, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

let nextId = 0;

/**
 * Questions as JSON in the AI generator's format: a file to pick or text to paste. A picked file's content goes into
 * the text box, where the admin can still look at it before importing.
 */
@Component({
  selector: 'app-json-questions-input',
  imports: [TranslatePipe],
  templateUrl: './json-questions-input.html',
  styleUrl: './json-questions-input.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class JsonQuestionsInput {
  readonly label = input.required<string>();
  readonly disabled = input(false);
  readonly value = model('');
  /** The picked file could not be read; the text stays as it was. */
  readonly unreadable = output<void>();

  protected readonly id = `json-questions-${nextId++}`;
  protected readonly fileName = signal<string | null>(null);

  protected async onFile(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) {
      return;
    }
    try {
      this.value.set(await file.text());
      this.fileName.set(file.name);
    } catch {
      this.unreadable.emit();
    }
    input.value = '';
  }

  protected onInput(event: Event): void {
    this.value.set((event.target as HTMLTextAreaElement).value);
    this.fileName.set(null);
  }
}
