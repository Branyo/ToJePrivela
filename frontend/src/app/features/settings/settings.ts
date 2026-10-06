import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthStore } from '../../core/auth/auth-store';
import { AiQuestionsSection } from './ai-questions-section';
import { PlayersSection } from './players-section';

/** The login's settings: its players for everyone, and the shared AI questions for admins only. */
@Component({
  selector: 'app-settings',
  imports: [PlayersSection, AiQuestionsSection, TranslatePipe],
  templateUrl: './settings.html',
  styleUrl: './settings.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Settings {
  protected readonly auth = inject(AuthStore);
}
