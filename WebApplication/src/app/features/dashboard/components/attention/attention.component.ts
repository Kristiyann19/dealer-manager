import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { LucideDynamicIcon, LucideCircleCheck, provideLucideIcons } from '@lucide/angular';
import {
  LocalizedCurrencyPipe,
  LocalizedNumberPipe,
} from '../../../../shared/pipes/localized-format.pipe';
import { DashboardAttentionItem, DashboardAttentionType } from '../../models/dashboard.models';

@Component({
  selector: 'app-dashboard-attention',
  providers: [provideLucideIcons(LucideCircleCheck)],
  imports: [
    RouterLink,
    TranslatePipe,
    LucideDynamicIcon,
    LocalizedCurrencyPipe,
    LocalizedNumberPipe,
  ],
  templateUrl: './attention.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'block min-w-0' },
})
export class AttentionComponent {
  readonly items = input.required<DashboardAttentionItem[]>();
  readonly count = input.required<number>();
  readonly asOf = input.required<string>();
  detail(item: DashboardAttentionItem): { key: string; params: { days?: number; hours?: number } } {
    const prefix = 'dashboard.attention.';
    if (item.type === DashboardAttentionType.ReservationExpiringSoon) {
      const now = new Date(this.asOf());
      const expiry = new Date(item.referenceDate);
      const date = (d: Date) => Date.UTC(d.getUTCFullYear(), d.getUTCMonth(), d.getUTCDate());
      const days = (date(expiry) - date(now)) / 86400000;
      return days === 0
        ? { key: prefix + 'today', params: {} }
        : days === 1
          ? { key: prefix + 'tomorrow', params: {} }
          : {
              key: prefix + 'inHours',
              params: {
                hours: Math.max(0, Math.ceil((expiry.getTime() - now.getTime()) / 3600000)),
              },
            };
    }
    if (item.type === DashboardAttentionType.ReservationExpired && item.days === 0)
      return { key: prefix + 'expiredRecently', params: {} };
    if (item.type === DashboardAttentionType.ReservationExpired && item.days === 1)
      return { key: prefix + 'expiredYesterday', params: {} };
    return { key: prefix + 'details.' + item.type, params: { days: item.days } };
  }
}
