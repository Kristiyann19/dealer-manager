import { ChangeDetectionStrategy, Component, computed, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { LocalizedDatePipe, LocalizedNumberPipe } from '../../shared/pipes/localized-format.pipe';
import { VehicleDetails } from './vehicle.models';
import { DOSSIER_GROUPS, DossierField } from './vehicle-dossier.models';
@Component({
  selector: 'app-vehicle-dossier',
  imports: [RouterLink, TranslatePipe, LocalizedDatePipe, LocalizedNumberPipe],
  templateUrl: './vehicle-dossier.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VehicleDossierComponent {
  readonly vehicle = input.required<VehicleDetails>();
  readonly expanded = signal(false);
  readonly groups = DOSSIER_GROUPS;
  optionLabel(field: DossierField) {
    return (
      field.options?.find((o) => o.value === this.vehicle()[field.key])?.label ??
      'common.notProvided'
    );
  }
  readonly compact = computed<DossierField[]>(() =>
    ['vin', 'mileage'].map((key) =>
      DOSSIER_GROUPS.flatMap((g) => g.fields).find((f) => f.key === key)!,
    ),
  );
  readonly incomplete = computed(
    () =>
      this.groups
        .flatMap((g) => g.fields)
        .filter((f) => this.vehicle()[f.key] != null && this.vehicle()[f.key] !== '').length <
      this.groups.flatMap((g) => g.fields).length / 2,
  );
  readonly visibleGroups = computed(() =>
    this.expanded() ? this.groups : [{ title: '', fields: this.compact() }],
  );
}
