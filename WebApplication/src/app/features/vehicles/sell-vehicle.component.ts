import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { TranslatePipe } from '@ngx-translate/core';
import { DialogModule } from 'primeng/dialog';
import {
  LocalizedCurrencyPipe,
  LocalizedNumberPipe,
} from '../../shared/pipes/localized-format.pipe';
import { VehicleApiService } from './vehicle-api.service';
import { positiveAmount, validOptionalDate, vehicleError } from './vehicle-form-utils';

@Component({
  selector: 'app-sell-vehicle',
  imports: [
    TranslatePipe,
    ReactiveFormsModule,
    DialogModule,
    LocalizedCurrencyPipe,
    LocalizedNumberPipe,
  ],
  templateUrl: './sell-vehicle.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SellVehicleComponent implements OnInit {
  readonly vehicleId = input.required<number>();
  readonly listingPrice = input.required<number | null>();
  readonly totalInvested = input.required<number>();
  readonly saved = output<void>();
  readonly closed = output<boolean>();
  readonly busy = signal(false);
  readonly error = signal('');
  readonly form = inject(FormBuilder).group({
    actualSalePrice: [null as number | null, [Validators.required, positiveAmount]],
    soldAt: ['', validOptionalDate],
  });
  private readonly price = toSignal(this.form.controls.actualSalePrice.valueChanges, {
    initialValue: null,
  });
  readonly profit = computed(() => {
    const price = this.price();
    return price !== null && Number.isFinite(price) && price > 0
      ? price - this.totalInvested()
      : null;
  });
  readonly roi = computed(() =>
    this.profit() === null
      ? null
      : this.totalInvested() === 0
        ? 0
        : (this.profit()! / this.totalInvested()) * 100,
  );
  private readonly api = inject(VehicleApiService);
  private readonly destroyRef = inject(DestroyRef);
  ngOnInit() {
    this.form.controls.actualSalePrice.setValue(this.listingPrice());
  }
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
      .sellVehicle(this.vehicleId(), {
        actualSalePrice: value.actualSalePrice!,
        ...(value.soldAt ? { soldAt: new Date(value.soldAt + 'T00:00:00').toISOString() } : {}),
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
