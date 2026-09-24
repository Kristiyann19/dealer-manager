import { TranslatePipe } from '@ngx-translate/core';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { KpiCardComponent } from '../../../../shared/components/kpi-card/kpi-card.component';
import { FinancialOverview } from '../../models/dashboard.models';

@Component({
  selector: 'app-financial-overview',
  imports: [TranslatePipe, KpiCardComponent],
  template: ` <section aria-labelledby="capital-title">
    <div class="section-heading">
      <h2 id="capital-title">{{ 'ui.capital_financial_position' | translate }}</h2>
      <span class="text-[11px] text-slate-400">{{ 'ui.current_position_eur' | translate }}</span>
    </div>
    <div class="grid grid-cols-2 xl:grid-cols-4 gap-4">
      <app-kpi-card
        [label]="'ui.available_cash' | translate"
        [value]="data().availableCash"
        icon="wallet"
        tone="green"
        [money]="true"
        [context]="'ui.available_for_new_opportunities' | translate"
      />
      <app-kpi-card
        [label]="'ui.capital_invested' | translate"
        [value]="data().capitalInvested"
        icon="landmark"
        tone="blue"
        [money]="true"
        [context]="'ui.capital_currently_in_vehicles' | translate"
      />
      <app-kpi-card
        [label]="'ui.committed_costs' | translate"
        [value]="data().committedCosts"
        icon="receipt-text"
        tone="amber"
        [money]="true"
        [context]="'ui.planned_and_agreed_costs' | translate"
      />
      <app-kpi-card
        [label]="'ui.inventory_value' | translate"
        [value]="data().inventoryValue"
        icon="warehouse"
        [money]="true"
        [context]="'ui.expected_selling_value' | translate"
      />
    </div>
  </section>`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FinancialOverviewComponent {
  readonly data = input.required<FinancialOverview>();
}
