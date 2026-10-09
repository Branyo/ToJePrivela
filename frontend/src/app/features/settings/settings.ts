import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
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
 * admins only. A login with just one tab gets no tab bar.
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
}
