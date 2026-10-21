import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { CandidateDetails } from '../models/candidate.models';
import { CandidateApiService } from '../services/candidate-api.service';
import {
  LocalizedCurrencyPipe,
  LocalizedDatePipe,
} from '../../../shared/pipes/localized-format.pipe';
import { AuctionFieldsComponent } from './auction-fields.component';
import { createAuctionForm, fillAuctionForm, auctionRequest } from './auction-form';
import { apiError } from './candidate-form-utils';

@Component({
  selector: 'app-candidate-auction',
  imports: [
    ReactiveFormsModule,
    TranslatePipe,
    AuctionFieldsComponent,
    LocalizedCurrencyPipe,
    LocalizedDatePipe,
  ],
  templateUrl: './candidate-auction.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CandidateAuctionComponent {
  readonly candidate = input.required<CandidateDetails>();
  readonly saved = output<void>();
  private readonly api = inject(CandidateApiService);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly form = createAuctionForm(inject(FormBuilder));
  protected readonly editing = signal(false);
  protected readonly busy = signal(false);
  protected readonly error = signal('');
  protected edit() {
    const details = this.candidate().auctionDetails;
    if (!details || this.busy()) return;
    fillAuctionForm(this.form, details);
    this.error.set('');
    this.editing.set(true);
  }
  protected submit() {
    if (this.busy()) return;
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    this.busy.set(true);
    this.error.set('');
    this.api
      .updateAuctionDetails(this.candidate().id, auctionRequest(this.form))
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.busy.set(false)),
      )
      .subscribe({
        next: () => {
          this.editing.set(false);
          this.saved.emit();
        },
        error: (error) => this.error.set(apiError(error, true)),
      });
  }
}
