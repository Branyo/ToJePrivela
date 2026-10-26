import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { DatePipe } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { AccountOverview } from '../../core/api/models';
import { LanguageService } from '../../core/i18n/language';

/** When a login last signed in (or that it never has) and when it was created, as the logins list and a login show it. */
@Component({
  selector: 'app-account-dates',
  imports: [DatePipe, TranslatePipe],
  template: `
    <span>
      @if (account().lastSignedInAt; as at) {
        {{ 'accounts.lastSignedIn' | translate: { at: (at | date: ('accounts.dateTime' | translate) : undefined : i18n.locale()) } }}
      } @else {
        {{ 'accounts.neverSignedIn' | translate }}
      }
    </span>
    <span>
      {{ 'accounts.created' | translate: { at: (account().createdAt | date: ('accounts.dateTime' | translate) : undefined : i18n.locale()) } }}
    </span>
  `,
  styles: `
    :host {
      display: flex;
      flex-wrap: wrap;
      column-gap: 16px;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AccountDates {
  protected readonly i18n = inject(LanguageService);

  readonly account = input.required<AccountOverview>();
}
