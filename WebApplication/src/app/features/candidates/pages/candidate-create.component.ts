import { AuctionFieldsComponent } from '../components/auction-fields.component';
import { createAuctionForm, auctionRequest } from '../components/auction-form';
import { CandidateAcquisitionType } from '../models/candidate.models';
import { TranslatePipe } from '@ngx-translate/core';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { CandidateApiService } from '../services/candidate-api.service';
import { FieldErrorComponent } from '../components/field-error.component';
import { apiError, integer, optionalText, requiredText } from '../components/candidate-form-utils';

@Component({
  selector: 'app-candidate-create',
  imports: [
    AuctionFieldsComponent,
    TranslatePipe,
    ReactiveFormsModule,
    RouterLink,
    FieldErrorComponent,
  ],
  templateUrl: './candidate-create.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CandidateCreateComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(CandidateApiService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly maxYear = new Date().getUTCFullYear() + 1;
  protected readonly busy = signal(false);
  protected readonly error = signal('');
  protected readonly acquisitionTypes = CandidateAcquisitionType;
  protected readonly form = this.fb.group({
    acquisitionType: this.fb.nonNullable.control(CandidateAcquisitionType.DirectPurchase),
    auctionDetails: createAuctionForm(this.fb),
    make: this.fb.nonNullable.control('', requiredText),
    model: this.fb.nonNullable.control('', requiredText),
    year: this.fb.control<number | null>(null, [
      integer,
      Validators.min(1886),
      Validators.max(this.maxYear),
    ]),
    mileage: this.fb.control<number | null>(null, [
      integer,
      Validators.min(0),
      Validators.max(2147483647),
    ]),
    askingPrice: this.fb.control<number | null>(null, [Validators.required, Validators.min(0)]),
    vin: this.fb.nonNullable.control('', Validators.maxLength(32)),
    notes: this.fb.nonNullable.control(''),
  });
  protected readonly fields = this.form.controls;

  constructor() {
    this.fields.auctionDetails.disable();
    this.fields.acquisitionType.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((type) => {
        if (type === CandidateAcquisitionType.Auction) {
          this.fields.askingPrice.disable();
          this.fields.auctionDetails.enable();
        } else {
          this.fields.askingPrice.enable();
          this.fields.auctionDetails.disable();
        }
      });
  }

  protected submit() {
    if (this.busy()) return;
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    const value = this.form.getRawValue();
    this.busy.set(true);
    this.error.set('');
    this.api
      .create({
        ...value,
        make: value.make.trim(),
        model: value.model.trim(),
        askingPrice:
          value.acquisitionType === CandidateAcquisitionType.Auction ? null : value.askingPrice!,
        auctionDetails:
          value.acquisitionType === CandidateAcquisitionType.Auction
            ? auctionRequest(this.fields.auctionDetails)
            : null,
        notes: optionalText(value.notes),
        vin: optionalText(value.vin)?.toUpperCase() ?? null,
      })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.busy.set(false)),
      )
      .subscribe({
        next: (candidate) => {
          void this.router.navigate(['/candidates', candidate.id]);
        },
        error: (error) => this.error.set(apiError(error, true)),
      });
  }
}
