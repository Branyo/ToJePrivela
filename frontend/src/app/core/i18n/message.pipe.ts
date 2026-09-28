import { Pipe, PipeTransform, inject } from '@angular/core';
import { LanguageService, Message } from './language';

/** Renders a `Message` in the active language, re-rendering when the language changes. */
@Pipe({ name: 'message', pure: false })
export class MessagePipe implements PipeTransform {
  private readonly language = inject(LanguageService);

  transform(message: Message | null | undefined): string {
    if (!message) {
      return '';
    }
    if ('text' in message) {
      return message.text;
    }
    const text = this.language.instant(message.key, message.params);
    return text === message.key && message.fallback ? message.fallback : text;
  }
}
