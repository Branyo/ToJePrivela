import { ChangeDetectionStrategy, Component, computed, effect, inject, untracked } from '@angular/core';
import { ActivatedRoute, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthStore } from '../../core/auth/auth-store';

interface SettingsTab {
  path: string;
  label: string;
}

const PLAYERS: SettingsTab = { path: 'players', label: 'settings.tabs.players' };
const PASSWORD: SettingsTab = { path: 'password', label: 'settings.tabs.password' };
const ADMIN_TABS: SettingsTab[] = [
  { path: 'categories', label: 'settings.tabs.categories' },
  { path: 'questions', label: 'settings.tabs.questions' },
];

/**
 * The login's settings, one tab per child route: its players for everyone, the shared categories and questions for
 * admins only, and last the login's password for everyone but admins, whose password the server configuration sets
 * (the server refuses to change it). Once the session is checked, a tab the account turns out not to get (admin
 * rights gained or lost) is left for the players tab, which the route guards alone cannot do.
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

  protected readonly tabs = computed(() => (this.auth.isAdmin() ? [PLAYERS, ...ADMIN_TABS] : [PLAYERS, PASSWORD]));

  constructor() {
    const router = inject(Router);
    const route = inject(ActivatedRoute);
    effect(() => {
      // Signing out navigates to the sign-in screen on its own; redirecting here would override it.
      if (!this.auth.isSignedIn()) {
        return;
      }
      const tabs = this.tabs();
      untracked(() => {
        const shown = route.firstChild?.snapshot.routeConfig?.path;
        if (shown && !tabs.some((tab) => tab.path === shown)) {
          void router.navigate(['/settings', PLAYERS.path]);
        }
      });
    });
  }
}
