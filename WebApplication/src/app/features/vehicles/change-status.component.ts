import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { TranslatePipe } from '@ngx-translate/core';
import { DialogModule } from 'primeng/dialog';
import { VehicleStatusComponent } from './vehicle-status.component';
import { VehicleApiService } from './vehicle-api.service';
import { VEHICLE_STATUSES, VehicleStatus } from './vehicle.models';
import { vehicleError } from './vehicle-form-utils';
@Component({
  selector: 'app-change-vehicle-status',
  imports: [TranslatePipe, ReactiveFormsModule, DialogModule, VehicleStatusComponent],
  templateUrl: './change-status.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ChangeStatusComponent {
  readonly vehicleId = input.required<number>();
  readonly currentStatus = input.required<VehicleStatus>();
  readonly saved = output<void>();
  readonly closed = output<boolean>();
  readonly busy = signal(false);
  readonly error = signal('');
  readonly options = computed(() =>
    VEHICLE_STATUSES.filter(
      (s) => s.value <= VehicleStatus.ReadyForSale && s.value !== this.currentStatus(),
    ),
  );
  readonly form = inject(FormBuilder).group({
    status: [null as VehicleStatus | null, Validators.required],
    notes: [''],
  });
  private readonly api = inject(VehicleApiService);
  private readonly destroyRef = inject(DestroyRef);
  close() {
    if (!this.busy()) this.closed.emit(!!this.error());
  }
  submit() {
    this.form.markAllAsTouched();
    const value = this.form.getRawValue();
    if (
      this.busy() ||
      !this.form.valid ||
      this.currentStatus() >= VehicleStatus.Listed ||
      !this.options().some((s) => s.value === value.status)
    )
      return;
    this.busy.set(true);
    this.error.set('');
    this.api
      .changeStatus(this.vehicleId(), value.status!, value.notes?.trim() || null)
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
