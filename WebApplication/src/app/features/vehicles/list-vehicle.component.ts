import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
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
import { LocalizedCurrencyPipe } from '../../shared/pipes/localized-format.pipe';
import { VehicleApiService } from './vehicle-api.service';
import { positiveAmount, validOptionalDate, vehicleError } from './vehicle-form-utils';

@Component({
  selector: 'app-list-vehicle',
  imports: [TranslatePipe, ReactiveFormsModule, DialogModule, LocalizedCurrencyPipe],
  templateUrl: './list-vehicle.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ListVehicleComponent {
  readonly vehicleId = input.required<number>();
  readonly expectedSellingPrice = input.required<number | null>();
  readonly saved = output<void>();
  readonly closed = output<boolean>();
  readonly busy = signal(false);
  readonly error = signal('');
  readonly form = inject(FormBuilder).group({
    listingPrice: [null as number | null, [Validators.required, positiveAmount]],
    listedAt: ['', validOptionalDate],
  });
  private readonly api = inject(VehicleApiService);
  private readonly destroyRef = inject(DestroyRef);
  close() {
    if (!this.busy()) this.closed.emit(!!this.error());
  }
  submit() {
    this.form.markAllAsTouched();
    if (this.busy() || !this.form.valid) return;
    const value = this.form.getRawValue();
    this.busy.set(true);
    this.error.set('');
    this.api
      .listVehicle(this.vehicleId(), {
        listingPrice: value.listingPrice!,
        ...(value.listedAt
          ? { listedAt: new Date(value.listedAt + 'T00:00:00').toISOString() }
          : {}),
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
