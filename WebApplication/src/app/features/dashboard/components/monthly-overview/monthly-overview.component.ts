import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { KpiCardComponent } from '../../../../shared/components/kpi-card/kpi-card.component';
import { MonthlyOverview } from '../../models/dashboard.models';

@Component({
  selector: 'app-monthly-overview',
  imports: [KpiCardComponent],
  template: ` <section aria-labelledby="monthly-title">
    <div class="section-heading">
      <h2 id="monthly-title">Monthly Overview</h2>
      <span class="text-xs text-slate-400">{{ period() }}</span>
    </div>
    <div class="grid grid-cols-2 xl:grid-cols-4 gap-4">
      <app-kpi-card
        label="Cars in Stock"
        [value]="data().carsInStock"
        icon="car-front"
        tone="blue"
        [context]="'+' + data().stockChange + ' vs last month'"
        contextTone="green"
      />
      <app-kpi-card
        label="Cars in Repair"
        [value]="data().carsInRepair"
        icon="wrench"
        tone="amber"
        [context]="data().overdueRepairs + ' overdue'"
        contextTone="amber"
      />
      <app-kpi-card
        label="Sold This Month"
        [value]="data().soldThisMonth"
        icon="circle-check"
        tone="green"
        [context]="'+' + data().salesGrowthPercent + '% vs last month'"
        contextTone="green"
      />
      <app-kpi-card
        label="Profit This Month"
        [value]="data().profitThisMonth"
        icon="trending-up"
        tone="green"
        [money]="true"
        [context]="'+' + data().profitGrowthPercent + '% vs last month'"
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
