import { TranslatePipe } from '@ngx-translate/core';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TagModule } from 'primeng/tag';
import { CANDIDATE_STATUS_LABELS, CandidateStatus } from '../models/candidate.models';

@Component({
  selector: 'app-candidate-status',
  imports: [TagModule, TranslatePipe],
  template: `<p-tag
    [value]="labels[status()] ?? 'common.unknown' | translate"
    [severity]="severity[status()] ?? 'secondary'"
  />`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CandidateStatusComponent {
  readonly status = input.required<CandidateStatus>();
  protected readonly labels = CANDIDATE_STATUS_LABELS;
  protected readonly severity = { 0: 'info', 1: 'success', 2: 'danger', 3: 'secondary' } as const;
}
