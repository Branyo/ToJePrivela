import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthStore } from './core/auth/auth-store';
import { ProviderSdks } from './core/auth/provider-sdks';
import { LanguageService } from './core/i18n/language';

@Component({
  imports: [RouterOutlet, RouterLink, RouterLinkActive, TranslatePipe],
  selector: 'app-root',
  styleUrl: './app.scss',
  templateUrl: './app.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {
  protected readonly i18n = inject(LanguageService);
  protected readonly auth = inject(AuthStore);
  private readonly sdks = inject(ProviderSdks);
  private readonly router = inject(Router);

  protected signOut(): void {
    this.auth.signOut();
    this.sdks.forgetGoogleChoice();
    void this.router.navigate(['/sign-in']);
  }
}
