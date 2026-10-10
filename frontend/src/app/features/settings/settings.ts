import { ChangeDetectionStrategy, Component, computed, effect, inject, untracked } from '@angular/core';
import { ActivatedRoute, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthStore } from '../../core/auth/auth-store';

interface SettingsTab {
  path: string;
  label: string;
}

const PLAYERS: SettingsTab = { path: 'players', label: 'settings.tabs.players' };
const ADMIN_TABS: SettingsTab[] = [
  { path: 'categories', label: 'settings.tabs.categories' },
  { path: 'questions', label: 'settings.tabs.questions' },
];

/**
 * The login's settings, one tab per child route: its players for everyone, the shared categories and questions for
 * admins only. A login with just one tab gets no tab bar. An account that turns out not to be an admin (any more)
 * once the session is checked leaves an admin tab it already shows, which `adminGuard` alone cannot do.
 */
@Component({
  selector: 'app-settings',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, TranslatePipe],
  templateUrl: './settings.html',
  styleUrl: './settings.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Settings {
  protected readonly auth = inject(AuthStore);

  protected readonly tabs = computed(() => (this.auth.isAdmin() ? [PLAYERS, ...ADMIN_TABS] : [PLAYERS]));

  constructor() {
    const router = inject(Router);
    const route = inject(ActivatedRoute);
    effect(() => {
      if (this.auth.isAdmin()) {
        return;
      }
      untracked(() => {
        const shown = route.firstChild?.snapshot.routeConfig?.path;
        if (ADMIN_TABS.some((tab) => tab.path === shown)) {
          void router.navigate(['/settings', PLAYERS.path]);
        }
      });
    });
  }
}
