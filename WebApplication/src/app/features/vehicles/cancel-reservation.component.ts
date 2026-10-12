import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, computed, inject, input, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { TranslatePipe } from '@ngx-translate/core';
import { DialogModule } from 'primeng/dialog';
import { LocalizedCurrencyPipe } from '../../shared/pipes/localized-format.pipe';
import { CapitalAccountApiService } from '../finance/services/capital-account-api.service';
import { VehicleApiService } from './vehicle-api.service';
import { VehicleReservation } from './reservation.models';
import { vehicleError } from './vehicle-form-utils';
@Component({ selector: 'app-cancel-reservation', imports: [ReactiveFormsModule, TranslatePipe, DialogModule, LocalizedCurrencyPipe], templateUrl: './cancel-reservation.component.html', changeDetection: ChangeDetectionStrategy.OnPush })
export class CancelReservationComponent implements OnInit {
  readonly reservation = input.required<VehicleReservation>();
  readonly saved = output<void>(); readonly closed = output<boolean>();
  readonly busy = signal(false); readonly loading = signal(true); readonly error = signal('');
  readonly balance = signal<number | null>(null);
  readonly form = inject(FormBuilder).group({ refund: [null as boolean | null, Validators.required], notes: ['', Validators.maxLength(2000)] });
  readonly refund = toSignal(this.form.controls.refund.valueChanges, { initialValue: null });
  readonly insufficient = computed(() => this.refund() === true && this.balance() !== null && this.balance()! < this.reservation().depositAmount);
  private readonly api = inject(VehicleApiService); private readonly finance = inject(CapitalAccountApiService); private readonly destroyRef = inject(DestroyRef);
  ngOnInit() {
    this.finance.getCapitalAccountDetails(this.reservation().capitalAccountId).pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.loading.set(false))).subscribe({
      next: account => this.balance.set(account.currentBalance), error: error => this.error.set(vehicleError(error)),
    });
  }
  close() { if (!this.busy()) this.closed.emit(!!this.error()); }
  submit() {
    if (this.busy() || this.loading() || this.form.invalid || this.insufficient() || this.balance() === null) return;
    this.busy.set(true); this.error.set(''); const r = this.reservation();
    this.api.cancelReservation(r.vehicleId, r.reservationId, this.form.controls.refund.value!, this.form.controls.notes.value?.trim() || null)
      .pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.busy.set(false))).subscribe({ next: () => this.saved.emit(), error: error => this.error.set(vehicleError(error, true)) });
  }
}
