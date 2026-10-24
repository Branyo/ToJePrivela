import { Routes, UrlMatchResult, UrlSegment } from '@angular/router';
import { adminGuard, notAdminGuard, signedInGuard, signedOutGuard } from './core/auth/auth-guards';

/**
 * `/statistics` and `/statistics/:playerId` as one route: moving between them reuses the page (its roster and scroll
 * position) and only the bound `playerId` input changes. Two sibling routes would rebuild it on every pick.
 */
export function statisticsMatcher(segments: UrlSegment[]): UrlMatchResult | null {
  if (segments[0]?.path !== 'statistics' || segments.length > 2) {
    return null;
  }

  return { consumed: segments, posParams: segments.length === 2 ? { playerId: segments[1] } : {} };
}

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
        // One tab per child; the categories and questions tabs are for admins only.
        children: [
          { path: '', pathMatch: 'full', redirectTo: 'players' },
          {
            path: 'players',
            loadComponent: () => import('./features/settings/players-section').then((m) => m.PlayersSection),
            title: 'titles.players',
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
          {
            path: 'accounts',
            canActivate: [adminGuard],
            loadComponent: () => import('./features/accounts/accounts').then((m) => m.Accounts),
            title: 'titles.accounts',
          },
          {
            path: 'password',
            canActivate: [notAdminGuard],
            loadComponent: () => import('./features/settings/password-section').then((m) => m.PasswordSection),
            title: 'titles.password',
          },
        ],
      },
      {
        matcher: statisticsMatcher,
        loadComponent: () => import('./features/statistics/statistics').then((m) => m.Statistics),
        title: 'titles.statistics',
      },
      {
        path: 'history',
        loadComponent: () => import('./features/history/history').then((m) => m.History),
        title: 'titles.history',
      },
      {
        // One login as an admin sees it, read only: the login's own screens, reading its data (`AdminAccountData`).
        path: 'accounts/:accountId',
        canActivate: [adminGuard],
        loadComponent: () => import('./features/accounts/account-view').then((m) => m.AccountView),
        children: [
          { path: '', pathMatch: 'full', redirectTo: 'players' },
          {
            path: 'players',
            loadComponent: () => import('./features/accounts/account-players').then((m) => m.AccountPlayers),
            title: 'titles.players',
          },
          {
            path: 'history',
            loadComponent: () => import('./features/history/history').then((m) => m.History),
            title: 'titles.history',
          },
          {
            matcher: statisticsMatcher,
            loadComponent: () => import('./features/statistics/statistics').then((m) => m.Statistics),
            title: 'titles.statistics',
          },
          {
            path: 'games/:id',
            loadComponent: () => import('./features/summary/summary').then((m) => m.Summary),
            title: 'titles.summary',
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
