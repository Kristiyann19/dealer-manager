import {
  LocalizedNumberPipe,
  LocalizedCurrencyPipe,
  LocalizedDatePipe,
} from '../../../../shared/pipes/localized-format.pipe';
import { TranslatePipe } from '@ngx-translate/core';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CANDIDATE_STATUS_LABELS } from '../../../candidates/models/candidate.models';
import { CandidateSummary } from '../../models/dashboard.models';

@Component({
  selector: 'app-recent-candidates',
  imports: [
    TranslatePipe,
    LocalizedNumberPipe,
    LocalizedCurrencyPipe,
    LocalizedDatePipe,
    RouterLink,
  ],
  templateUrl: './recent-candidates.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecentCandidatesComponent {
  readonly candidates = input.required<CandidateSummary[]>();
  readonly query = input('');
  protected readonly statusLabels = CANDIDATE_STATUS_LABELS;
  protected readonly filtered = computed(() => {
    const query = this.query().trim().toLowerCase();
    return this.candidates().filter((c) =>
      `${c.make} ${c.model} ${c.id}`.toLowerCase().includes(query),
    );
  });
}
