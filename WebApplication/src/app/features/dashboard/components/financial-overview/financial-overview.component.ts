import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { KpiCardComponent } from '../../../../shared/components/kpi-card/kpi-card.component';
import { FinancialOverview } from '../../models/dashboard.models';

@Component({
  selector: 'app-financial-overview',
  imports: [KpiCardComponent],
  template: ` <section aria-labelledby="capital-title">
    <div class="section-heading">
      <h2 id="capital-title">Capital &amp; Financial Position</h2>
      <span class="text-[11px] text-slate-400">Current position · EUR</span>
    </div>
    <div class="grid grid-cols-2 xl:grid-cols-4 gap-4">
      <app-kpi-card
        label="Available Cash"
        [value]="data().availableCash"
        icon="wallet"
        tone="green"
        [money]="true"
        context="Available for new opportunities"
      />
      <app-kpi-card
        label="Capital Invested"
        [value]="data().capitalInvested"
        icon="landmark"
        tone="blue"
        [money]="true"
        context="Capital currently in vehicles"
      />
      <app-kpi-card
        label="Committed Costs"
        [value]="data().committedCosts"
        icon="receipt-text"
        tone="amber"
        [money]="true"
        context="Planned and agreed costs"
      />
      <app-kpi-card
        label="Inventory Value"
        [value]="data().inventoryValue"
        icon="warehouse"
        [money]="true"
        context="Expected selling value"
      />
    </div>
  </section>`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FinancialOverviewComponent {
  readonly data = input.required<FinancialOverview>();
}
