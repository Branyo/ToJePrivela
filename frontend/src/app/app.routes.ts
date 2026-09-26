import { Routes } from '@angular/router';

/** Titles are translation keys, see `TranslatedTitleStrategy`. */
export const routes: Routes = [
  { path: '', loadComponent: () => import('./features/home/home').then((m) => m.Home) },
  { path: 'new', loadComponent: () => import('./features/setup/setup').then((m) => m.Setup), title: 'titles.setup' },
  { path: 'games/:id', loadComponent: () => import('./features/play/play').then((m) => m.Play), title: 'titles.play' },
  {
    path: 'games/:id/summary',
    loadComponent: () => import('./features/summary/summary').then((m) => m.Summary),
    title: 'titles.summary',
  },
  { path: '**', redirectTo: '' },
];
