import { LocalizedNumberPipe } from '../../../../shared/pipes/localized-format.pipe';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';

import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TableModule } from 'primeng/table';
import { SelectModule } from 'primeng/select';
import { DialogModule } from 'primeng/dialog';
import { LucideDynamicIcon } from '@lucide/angular';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge.component';
import { MoneyDisplayComponent } from '../../../../shared/components/money-display/money-display.component';
import { CandidateSummary } from '../../models/dashboard.models';
import { CandidateStatus } from '../../../../shared/models/ui.models';

@Component({
  selector: 'app-recent-candidates',
  imports: [
    TranslatePipe,
    TableModule,
    SelectModule,
    DialogModule,
    FormsModule,
    LocalizedNumberPipe,
    RouterLink,
    LucideDynamicIcon,
    StatusBadgeComponent,
    MoneyDisplayComponent,
  ],
  templateUrl: './recent-candidates.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecentCandidatesComponent {
  private readonly translate = inject(TranslateService);
  readonly candidates = input.required<CandidateSummary[]>();
  readonly query = input('');
  protected readonly selectedStatus = signal<CandidateStatus | null>(null);
  protected readonly selectedCandidate = signal<CandidateSummary | null>(null);
  protected readonly statuses = computed(() => {
    this.translate.currentLang();
    return [
      { label: this.translate.instant('dashboard.allStatuses'), value: null },
      ...(['Under Review', 'Approved', 'Pending Inspection'] as const).map((value) => ({
        label: this.translate.instant('status.' + value),
        value,
      })),
    ];
  });
  protected readonly filtered = computed(() => {
    const query = this.query().trim().toLowerCase();
    return this.candidates().filter(
      (candidate) =>
        (!this.selectedStatus() || candidate.status === this.selectedStatus()) &&
        (!query ||
          [candidate.vehicle, candidate.vin, candidate.id.toString()].some((value) =>
            value.toLowerCase().includes(query),
          )),
    );
  });
  // PrimeNG table templates expose row context loosely; restore its declared model.
  protected row(value: CandidateSummary): CandidateSummary {
    return value;
  }
}
