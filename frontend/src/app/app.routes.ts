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
        path: 'players',
        loadComponent: () => import('./features/players/players').then((m) => m.Players),
        title: 'titles.players',
      },
      {
        path: 'admin',
        canActivate: [adminGuard],
        loadComponent: () => import('./features/admin/admin').then((m) => m.Admin),
        title: 'titles.admin',
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
