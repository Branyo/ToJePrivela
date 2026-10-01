import { ApplicationConfig, inject, provideAppInitializer, provideBrowserGlobalErrorListeners } from '@angular/core';
import { registerLocaleData } from '@angular/common';
import { provideHttpClient, withFetch } from '@angular/common/http';
import localeEnGb from '@angular/common/locales/en-GB';
import localeSk from '@angular/common/locales/sk';
import { TitleStrategy, provideRouter, withComponentInputBinding } from '@angular/router';
import { provideTranslateService } from '@ngx-translate/core';
import { provideTranslateHttpLoader } from '@ngx-translate/http-loader';
import { routes } from './app.routes';
import { DEFAULT_LANGUAGE, LanguageService } from './core/i18n/language';
import { TranslatedTitleStrategy } from './core/i18n/translated-title-strategy';
import { GameRulesStore } from './core/rules/game-rules-store';

// Date and number formats for every supported language; pipes pick one via `LanguageService.locale`.
registerLocaleData(localeSk, 'sk-SK');
registerLocaleData(localeEnGb, 'en-GB');

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideHttpClient(withFetch()),
    provideRouter(routes, withComponentInputBinding()),
    provideTranslateService({
      loader: provideTranslateHttpLoader({ prefix: '/i18n/', suffix: '.json' }),
      fallbackLang: DEFAULT_LANGUAGE,
    }),
    { provide: TitleStrategy, useExisting: TranslatedTitleStrategy },
    // The first screen renders only once its translations and the backend's limits are there.
    provideAppInitializer(() => Promise.all([inject(LanguageService).init(), inject(GameRulesStore).load()])),
  ],
};
