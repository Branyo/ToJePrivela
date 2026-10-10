import { Routes } from '@angular/router';
import { adminGuard, signedInGuard, signedOutGuard } from './core/auth/auth-guards';

/** Titles are translation keys, see `TranslatedTitleStrategy`. Everything but signing in needs a signed-in account. */
export const routes: Routes = [
  {
    path: 'sign-in',
    canActivate: [signedOutGuard],
    loadComponent: () => import('./features/sign-in/sign-in').then((m) => m.SignIn),
    title: 'titles.signIn',
  },
  {
    path: '',
    canActivateChild: [signedInGuard],
    children: [
      { path: '', loadComponent: () => import('./features/home/home').then((m) => m.Home) },
      { path: 'new', loadComponent: () => import('./features/setup/setup').then((m) => m.Setup), title: 'titles.setup' },
      {
        path: 'settings',
        loadComponent: () => import('./features/settings/settings').then((m) => m.Settings),
        // One tab per child; the tabs past players and password are for admins only.
        children: [
          { path: '', pathMatch: 'full', redirectTo: 'players' },
          {
            path: 'players',
            loadComponent: () => import('./features/settings/players-section').then((m) => m.PlayersSection),
            title: 'titles.players',
          },
          {
            path: 'password',
            loadComponent: () => import('./features/settings/password-section').then((m) => m.PasswordSection),
            title: 'titles.password',
          },
          {
            path: 'categories',
            canActivate: [adminGuard],
            loadComponent: () => import('./features/settings/ai-questions-section').then((m) => m.AiQuestionsSection),
            title: 'titles.categories',
          },
          {
            path: 'questions',
            canActivate: [adminGuard],
            loadComponent: () => import('./features/questions/questions').then((m) => m.Questions),
            title: 'titles.questions',
          },
        ],
      },
      { path: 'games/:id', loadComponent: () => import('./features/play/play').then((m) => m.Play), title: 'titles.play' },
      {
        path: 'games/:id/summary',
        loadComponent: () => import('./features/summary/summary').then((m) => m.Summary),
        title: 'titles.summary',
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
