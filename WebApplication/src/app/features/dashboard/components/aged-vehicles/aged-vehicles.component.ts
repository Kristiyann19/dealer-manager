import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { LocalizedCurrencyPipe, LocalizedNumberPipe } from '../../../../shared/pipes/localized-format.pipe';
import { VehicleStatusComponent } from '../../../vehicles/vehicle-status.component';
import { AgedVehicle } from '../../models/dashboard.models';

@Component({
  selector: 'app-aged-vehicles',
  imports: [RouterLink, TranslatePipe, LocalizedCurrencyPipe, LocalizedNumberPipe, VehicleStatusComponent],
  templateUrl: './aged-vehicles.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AgedVehiclesComponent {
  readonly vehicles = input.required<AgedVehicle[]>();
  readonly query = input('');
  private readonly router = inject(Router);
  protected readonly filtered = computed(() => this.vehicles().filter(v =>
    `${v.make} ${v.model} ${v.id}`.toLowerCase().includes(this.query().trim().toLowerCase())));
  protected open(id: number) { this.router.navigate(['/vehicles', id]); }
}
