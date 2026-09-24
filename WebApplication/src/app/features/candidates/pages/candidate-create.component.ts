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
  imports: [TranslatePipe, ReactiveFormsModule, RouterLink, FieldErrorComponent],
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
  protected readonly form = this.fb.group({
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
    expectedSellingPrice: this.fb.control<number | null>(null, [
      Validators.required,
      Validators.min(0),
    ]),
    vin: this.fb.nonNullable.control(''),
    source: this.fb.nonNullable.control(''),
    location: this.fb.nonNullable.control(''),
    notes: this.fb.nonNullable.control(''),
  });
  protected readonly fields = this.form.controls;

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
        expectedSellingPrice: value.expectedSellingPrice!,
        vin: optionalText(value.vin),
        source: optionalText(value.source),
        location: optionalText(value.location),
        notes: optionalText(value.notes),
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
