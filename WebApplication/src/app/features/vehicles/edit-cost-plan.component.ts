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
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { TranslatePipe } from '@ngx-translate/core';
import { DialogModule } from 'primeng/dialog';
import { VehicleApiService } from './vehicle-api.service';
import { VehicleCostPlanItem } from './vehicle.models';
import { nonnegativeAmount, requiredText, vehicleError } from './vehicle-form-utils';

@Component({
  selector: 'app-edit-cost-plan',
  imports: [TranslatePipe, ReactiveFormsModule, DialogModule],
  templateUrl: './edit-cost-plan.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EditCostPlanComponent {
  readonly vehicleId = input.required<number>();
  readonly item = input.required<VehicleCostPlanItem>();
  readonly saved = output<void>();
  readonly closed = output<void>();
  readonly busy = signal(false);
  readonly error = signal('');
  private readonly api = inject(VehicleApiService);
  private readonly destroyRef = inject(DestroyRef);
  readonly form = inject(FormBuilder).group({
    currentEstimatedAmount: [null as number | null, nonnegativeAmount],
    committedAmount: [null as number | null, nonnegativeAmount],
    description: ['', requiredText],
    isCancelled: [false],
  });
  ngOnInit() {
    this.form.patchValue(this.item());
  }
  close() {
    if (!this.busy()) this.closed.emit();
  }
  submit() {
    this.form.markAllAsTouched();
    if (this.busy() || this.form.invalid) return;
    const value = this.form.getRawValue();
    this.busy.set(true);
    this.error.set('');
    this.api
      .updateCostPlan(this.vehicleId(), this.item().id, {
        currentEstimatedAmount: value.currentEstimatedAmount,
        committedAmount: value.committedAmount,
        description: value.description!.trim(),
        isCancelled: value.isCancelled ?? false,
      })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.busy.set(false)),
      )
      .subscribe({
        next: () => this.saved.emit(),
        error: (error) => this.error.set(vehicleError(error, true)),
      });
  }
}
