import {
  LocalizedDatePipe,
  LocalizedNumberPipe,
  LocalizedCurrencyPipe,
} from '../../../shared/pipes/localized-format.pipe';
import { TranslatePipe } from '@ngx-translate/core';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  signal,
} from '@angular/core';

import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  BehaviorSubject,
  catchError,
  distinctUntilChanged,
  finalize,
  map,
  of,
  switchMap,
} from 'rxjs';
import { CandidateApiService } from '../services/candidate-api.service';
import {
  CandidateDetails,
  CandidateEstimate,
  CandidateStatus,
  COST_CATEGORIES,
} from '../models/candidate.models';
import { CandidateStatusComponent } from '../components/candidate-status.component';
import { EstimateFormComponent } from '../components/estimate-form.component';
import { apiError, optionalText } from '../components/candidate-form-utils';

@Component({
  selector: 'app-candidate-details',
  imports: [
    TranslatePipe,
    RouterLink,
    ReactiveFormsModule,
    LocalizedCurrencyPipe,
    LocalizedDatePipe,
    LocalizedNumberPipe,
    CandidateStatusComponent,
    EstimateFormComponent,
  ],
  templateUrl: './candidate-details.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CandidateDetailsComponent {
  private readonly api = inject(CandidateApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  private readonly reload = new BehaviorSubject<void>(undefined);
  protected readonly candidate = signal<CandidateDetails | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal('');
  protected readonly actionError = signal('');
  protected readonly notice = signal('');
  protected readonly savedVersion = signal(0);
  protected readonly busy = signal(false);
  protected readonly showEstimate = signal(false);
  protected readonly decision = signal<'approve' | 'reject' | null>(null);
  protected readonly reason = new FormControl('', { nonNullable: true });
  protected readonly selectedVersion = signal<number | null>(null);
  protected readonly selectedEstimate = computed(() => {
    const candidate = this.candidate();
    return (
      candidate?.estimateHistory.find((estimate) => estimate.id === this.selectedVersion()) ??
      candidate?.latestEstimate ??
      null
    );
  });
  protected readonly statuses = CandidateStatus;
  protected readonly categories = COST_CATEGORIES;
  protected readonly canEvaluate = computed(() => {
    const status = this.candidate()?.status;
    return status === CandidateStatus.UnderReview || status === CandidateStatus.Approved;
  });

  constructor() {
    this.route.paramMap
      .pipe(
        map((params) => Number(params.get('id'))),
        distinctUntilChanged(),
        switchMap((id) => {
          this.showEstimate.set(false);
          this.decision.set(null);
          this.selectedVersion.set(null);
          this.notice.set('');
          this.actionError.set('');
          return this.reload.pipe(
            switchMap(() => {
              this.loading.set(true);
              this.error.set('');
              this.candidate.set(null);
              if (!Number.isInteger(id) || id < 1 || id > 2147483647)
                return of({ data: null, error: 'errors.invalidId' });
              return this.api.details(id).pipe(
                map((data) => ({ data, error: '' })),
                catchError((error) => of({ data: null, error: apiError(error) })),
              );
            }),
          );
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((result) => {
        this.candidate.set(result.data);
        this.error.set(result.error);
        this.loading.set(false);
      });
  }
  protected refresh() {
    if (this.busy()) return;
    this.actionError.set('');
    this.decision.set(null);
    this.showEstimate.set(false);
    this.reload.next();
  }
  protected openEstimate() {
    this.decision.set(null);
    this.actionError.set('');
    this.notice.set('');
    this.showEstimate.set(true);
  }
  protected estimateSaved(estimate: CandidateEstimate) {
    this.selectedVersion.set(estimate.id);
    this.savedVersion.set(estimate.version);
    this.notice.set('candidate.versionSaved');
    this.refresh();
  }
  protected openDecision(decision: 'approve' | 'reject') {
    this.actionError.set('');
    this.notice.set('');
    this.reason.setValue('');
    this.decision.set(decision);
  }
  protected confirmDecision() {
    const candidate = this.candidate();
    const decision = this.decision();
    if (!candidate || !decision || this.busy() || !this.canEvaluate()) return;
    this.busy.set(true);
    this.actionError.set('');
    const request =
      decision === 'approve'
        ? this.api.approve(candidate.id)
        : this.api.reject(candidate.id, optionalText(this.reason.value));
    request
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.busy.set(false)),
      )
      .subscribe({
        next: (updated) => {
          // Ignore a response if navigation has already moved to a different candidate.
          if (this.candidate()?.id !== candidate.id) return;
          this.candidate.set(updated);
          this.decision.set(null);
          this.notice.set(decision === 'approve' ? 'candidate.approved' : 'candidate.rejected');
        },
        error: (error) => {
          if (this.candidate()?.id === candidate.id) this.actionError.set(apiError(error, true));
        },
      });
  }
}
