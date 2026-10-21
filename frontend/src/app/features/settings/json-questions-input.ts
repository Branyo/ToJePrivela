import { ChangeDetectionStrategy, Component, input, model, output, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

let nextId = 0;

/** Mirrors the backend's `QuestionImporter.MaxJsonLength`: the most characters one import may send. */
export const MAX_JSON_LENGTH = 2_000_000;
/** A UTF-8 character takes up to 4 bytes, so a larger file cannot fit; it is refused before it is read. */
const MAX_FILE_BYTES = MAX_JSON_LENGTH * 4;

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
  /** The picked file is longer than one import may be (the limit, in characters); the text stays as it was. */
  readonly tooLarge = output<number>();

  protected readonly id = `json-questions-${nextId++}`;
  protected readonly fileName = signal<string | null>(null);

  protected async onFile(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) {
      return;
    }
    input.value = '';
    if (file.size > MAX_FILE_BYTES) {
      this.tooLarge.emit(MAX_JSON_LENGTH);
      return;
    }
    try {
      const text = await file.text();
      if (text.length > MAX_JSON_LENGTH) {
        this.tooLarge.emit(MAX_JSON_LENGTH);
        return;
      }
      this.value.set(text);
      this.fileName.set(file.name);
    } catch {
      this.unreadable.emit();
    }
  }

  protected onInput(event: Event): void {
    this.value.set((event.target as HTMLTextAreaElement).value);
    this.fileName.set(null);
  }
}
