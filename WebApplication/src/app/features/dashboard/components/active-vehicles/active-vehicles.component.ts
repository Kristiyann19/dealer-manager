import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideDynamicIcon } from '@lucide/angular';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge.component';
import { MoneyDisplayComponent } from '../../../../shared/components/money-display/money-display.component';
import { ActiveVehicleSummary } from '../../models/dashboard.models';

@Component({
  selector: 'app-active-vehicles',
  imports: [RouterLink, LucideDynamicIcon, StatusBadgeComponent, MoneyDisplayComponent],
  templateUrl: './active-vehicles.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ActiveVehiclesComponent {
  readonly vehicles = input.required<ActiveVehicleSummary[]>();
  readonly query = input('');
  protected readonly filtered = computed(() => {
    const query = this.query().trim().toLowerCase();
    return this.vehicles().filter(
      (vehicle) =>
        !query ||
        vehicle.vehicle.toLowerCase().includes(query) ||
        vehicle.vin.toLowerCase().includes(query),
    );
  });
}
