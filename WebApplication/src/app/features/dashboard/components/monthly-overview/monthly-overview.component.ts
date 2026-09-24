import { TranslatePipe } from '@ngx-translate/core';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { KpiCardComponent } from '../../../../shared/components/kpi-card/kpi-card.component';
import { MonthlyOverview } from '../../models/dashboard.models';

@Component({
  selector: 'app-monthly-overview',
  imports: [TranslatePipe, KpiCardComponent],
  template: ` <section aria-labelledby="monthly-title">
    <div class="section-heading">
      <h2 id="monthly-title">{{ 'ui.monthly_overview' | translate }}</h2>
      <span class="text-xs text-slate-400">{{ period() | translate }}</span>
    </div>
    <div class="grid grid-cols-2 xl:grid-cols-4 gap-4">
      <app-kpi-card
        [label]="'ui.cars_in_stock' | translate"
        [value]="data().carsInStock"
        icon="car-front"
        tone="blue"
        [context]="'dashboard.stockChange' | translate: { value: data().stockChange }"
        contextTone="green"
      />
      <app-kpi-card
        [label]="'ui.cars_in_repair' | translate"
        [value]="data().carsInRepair"
        icon="wrench"
        tone="amber"
        [context]="'dashboard.overdue' | translate: { value: data().overdueRepairs }"
        contextTone="amber"
      />
      <app-kpi-card
        [label]="'ui.sold_this_month' | translate"
        [value]="data().soldThisMonth"
        icon="circle-check"
        tone="green"
        [context]="'dashboard.percentChange' | translate: { value: data().salesGrowthPercent }"
        contextTone="green"
      />
      <app-kpi-card
        [label]="'ui.profit_this_month' | translate"
        [value]="data().profitThisMonth"
        icon="trending-up"
        tone="green"
        [money]="true"
        [context]="'dashboard.percentChange' | translate: { value: data().profitGrowthPercent }"
        contextTone="green"
      />
    </div>
  </section>`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MonthlyOverviewComponent {
  readonly data = input.required<MonthlyOverview>();
  readonly period = input.required<string>();
}
