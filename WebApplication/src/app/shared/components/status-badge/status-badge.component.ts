import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TagModule } from 'primeng/tag';
import { DisplayStatus } from '../../models/ui.models';

const SEVERITIES = {
  'Under Review': 'info',
  Approved: 'success',
  'Pending Inspection': 'warn',
  Repairing: 'warn',
  Transporting: 'info',
  'Ready For Sale': 'success',
} as const satisfies Record<DisplayStatus, 'info' | 'success' | 'warn'>;

@Component({
  selector: 'app-status-badge',
  imports: [TagModule],
  template: `<p-tag [value]="status()" [severity]="severity()" />`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatusBadgeComponent {
  readonly status = input.required<DisplayStatus>();
  protected readonly severity = computed(() => SEVERITIES[this.status()]);
}
