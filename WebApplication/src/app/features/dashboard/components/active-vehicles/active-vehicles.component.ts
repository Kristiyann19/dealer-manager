import { VEHICLE_STATUSES } from '../../../vehicles/vehicle.models';
import { LocalizedCurrencyPipe } from '../../../../shared/pipes/localized-format.pipe';
import { TranslatePipe } from '@ngx-translate/core';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ActiveVehicleSummary } from '../../models/dashboard.models';

@Component({
  selector: 'app-active-vehicles',
  imports: [TranslatePipe, RouterLink, LocalizedCurrencyPipe],
  templateUrl: './active-vehicles.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ActiveVehiclesComponent {
  protected readonly statusLabels = Object.fromEntries(
    VEHICLE_STATUSES.map((s) => [s.value, s.label]),
  );
  readonly vehicles = input.required<ActiveVehicleSummary[]>();
  readonly query = input('');
  protected readonly filtered = computed(() => {
    const query = this.query().trim().toLowerCase();
    return this.vehicles().filter(
      (vehicle) =>
        !query || `${vehicle.make} ${vehicle.model} ${vehicle.id}`.toLowerCase().includes(query),
    );
  });
}
