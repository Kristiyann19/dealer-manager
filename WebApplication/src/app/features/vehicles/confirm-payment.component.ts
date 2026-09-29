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
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { TranslatePipe } from '@ngx-translate/core';
import { DialogModule } from 'primeng/dialog';
import { LocalizedCurrencyPipe } from '../../shared/pipes/localized-format.pipe';
import { VehicleApiService } from './vehicle-api.service';
import { VehiclePaymentPreview } from './vehicle.models';
import { vehicleError } from './vehicle-form-utils';

@Component({
  selector: 'app-confirm-vehicle-payment',
  imports: [TranslatePipe, DialogModule, LocalizedCurrencyPipe],
  templateUrl: './confirm-payment.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ConfirmPaymentComponent {
  readonly vehicleId = input.required<number>();
  readonly itemId = input.required<number>();
  readonly saved = output<void>();
  readonly closed = output<void>();
  readonly preview = signal<VehiclePaymentPreview | null>(null);
  readonly loading = signal(true);
  readonly busy = signal(false);
  readonly error = signal('');
  readonly remaining = computed(
    () => (this.preview()?.currentBalance ?? 0) - (this.preview()?.amount ?? 0),
  );
  private readonly api = inject(VehicleApiService);
  private readonly destroyRef = inject(DestroyRef);

  ngOnInit() {
    this.load();
  }
  load() {
    if (this.busy()) return;
    this.preview.set(null);
    this.loading.set(true);
    this.error.set('');
    this.api
      .paymentPreview(this.vehicleId(), this.itemId())
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.loading.set(false)),
      )
      .subscribe({
        next: (preview) => this.preview.set(preview),
        error: (error) => this.error.set(vehicleError(error)),
      });
  }
  close() {
    if (!this.busy()) this.closed.emit();
  }
  confirm() {
    const preview = this.preview();
    if (!preview || this.busy() || this.loading() || this.remaining() < 0 || this.error()) return;
    this.busy.set(true);
    this.api
      .confirmPayment(this.vehicleId(), this.itemId(), preview)
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
