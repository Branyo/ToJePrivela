import { DOCUMENT, Injectable, computed, inject, signal } from '@angular/core';
import { InterpolationParameters, TranslateService } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';

export const LANGUAGES = [
  { code: 'sk', locale: 'sk-SK' },
  { code: 'en', locale: 'en-GB' },
] as const;

export type LanguageCode = (typeof LANGUAGES)[number]['code'];

export const DEFAULT_LANGUAGE: LanguageCode = 'sk';

const STORAGE_KEY = 'language';

/**
 * Text to show that is only translated when rendered (see `MessagePipe`), so it follows a language switch.
 * `fallback` is shown when `key` has no translation; `text` is shown as it is (e.g. a backend error without a code).
 */
export type Message = { key: string; params?: InterpolationParameters; fallback?: string } | { text: string };

export function isLanguage(value: unknown): value is LanguageCode {
  return LANGUAGES.some((language) => language.code === value);
}

export function localeOf(language: LanguageCode): string {
  return LANGUAGES.find((l) => l.code === language)!.locale;
}

/** Sorts names by the language's rules, so in Slovak `Č` follows `C` rather than `Z`. */
export function compareNames(a: string, b: string, locale: string = localeOf(DEFAULT_LANGUAGE)): number {
  return a.localeCompare(b, locale);
}

/**
 * Key of the plural form for a count: `common.cards` → `common.cards.few`. The forms are the CLDR categories
 * (`one`, `few`, `many`, `other`), so every language lists the ones it needs and always has `other`.
 */
export function pluralKey(key: string, count: number, locale: string): string {
  return `${key}.${new Intl.PluralRules(locale).select(count)}`;
}

/** The active language: switches ngx-translate, remembers the choice and gives the matching locale. */
@Injectable({ providedIn: 'root' })
export class LanguageService {
  private readonly translate = inject(TranslateService);
  private readonly document = inject(DOCUMENT);

  /** Set once the language's translations are loaded, so templates never show raw keys. */
  readonly language = signal<LanguageCode>(DEFAULT_LANGUAGE);
  readonly locale = computed(() => localeOf(this.language()));
  readonly languages = LANGUAGES.map((l) => l.code);

  /** Loads the remembered language before the app renders. */
  init(): Promise<void> {
    return this.use(this.stored() ?? DEFAULT_LANGUAGE);
  }

  async use(language: LanguageCode): Promise<void> {
    await firstValueFrom(this.translate.use(language));
    this.language.set(language);
    this.document.documentElement.lang = language;
    try {
      localStorage.setItem(STORAGE_KEY, language);
    } catch {
      // Storage can be blocked; the choice then lasts only for this visit.
    }
  }

  /** Translates now; reads the language signal so a `computed` using it follows a switch. */
  instant(key: string, params?: InterpolationParameters): string {
    this.language();
    return this.translate.instant(key, params) as string;
  }

  /** See `pluralKey`; reactive like `instant`. */
  pluralKey(key: string, count: number): string {
    return pluralKey(key, count, this.locale());
  }

  formatNumber(value: number, options?: Intl.NumberFormatOptions): string {
    return value.toLocaleString(this.locale(), options);
  }

  private stored(): LanguageCode | null {
    try {
      const value = localStorage.getItem(STORAGE_KEY);
      return isLanguage(value) ? value : null;
    } catch {
      return null;
    }
  }
}
