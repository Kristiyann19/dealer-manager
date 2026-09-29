import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { TagModule } from 'primeng/tag';
import { VEHICLE_STATUSES, VehicleStatus } from './vehicle.models';
@Component({
  selector: 'app-vehicle-status',
  imports: [TranslatePipe, TagModule],
  template: `<p-tag
    [value]="statuses[status()]?.label ?? 'common.unknown' | translate"
    [severity]="tones[status()] ?? 'secondary'"
  />`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VehicleStatusComponent {
  readonly status = input.required<VehicleStatus>();
  readonly statuses = VEHICLE_STATUSES;
  readonly tones = {
    0: 'secondary',
    1: 'info',
    2: 'info',
    3: 'warn',
    4: 'warn',
    5: 'info',
    6: 'success',
    7: 'info',
    8: 'warn',
    9: 'secondary',
  } as const;
}
