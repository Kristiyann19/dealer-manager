import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, computed, inject, input, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { TranslatePipe } from '@ngx-translate/core';
import { DialogModule } from 'primeng/dialog';
import { LocalizedCurrencyPipe } from '../../shared/pipes/localized-format.pipe';
import { CapitalAccountApiService } from '../finance/services/capital-account-api.service';
import { CapitalAccount } from '../finance/models/finance.models';
import { VehicleApiService } from './vehicle-api.service';
import { positiveAmount, requiredText, vehicleError } from './vehicle-form-utils';
@Component({
  selector: 'app-reserve-vehicle',
  imports: [ReactiveFormsModule, TranslatePipe, DialogModule, LocalizedCurrencyPipe],
  templateUrl: './reserve-vehicle.component.html', changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReserveVehicleComponent implements OnInit {
  readonly vehicleId = input.required<number>();
  readonly vehicleName = input.required<string>();
  readonly listingPrice = input.required<number>();
  readonly saved = output<void>(); readonly closed = output<boolean>();
  readonly busy = signal(false); readonly loading = signal(true); readonly error = signal('');
  readonly accounts = signal<CapitalAccount[]>([]);
  private readonly api = inject(VehicleApiService);
  private readonly finance = inject(CapitalAccountApiService);
  private readonly destroyRef = inject(DestroyRef);
  readonly form = inject(FormBuilder).group({
    customerName: ['', [requiredText, Validators.maxLength(200)]],
    customerPhone: ['', Validators.maxLength(50)], customerEmail: ['', [Validators.email, Validators.maxLength(254)]],
    depositAmount: [null as number | null, [Validators.required, positiveAmount]],
    capitalAccountId: [null as number | null, Validators.required],
    expiresAt: [''], notes: ['', Validators.maxLength(2000)],
  });
  private readonly amount = toSignal(this.form.controls.depositAmount.valueChanges, { initialValue: null });
  readonly remaining = computed(() => this.listingPrice() - (this.amount() ?? 0));
  ngOnInit() {
    this.form.controls.depositAmount.addValidators(Validators.max(this.listingPrice()));
    this.form.controls.expiresAt.addValidators(control => !control.value || (Number.isFinite(Date.parse(control.value)) && Date.parse(control.value) >= Date.now()) ? null : { expiry: true });
    this.finance.getCapitalAccounts().pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.loading.set(false))).subscribe({
      next: accounts => {
        this.accounts.set(accounts.filter(a => a.isActive && a.currency.toUpperCase() === 'EUR'));
        if (this.accounts().length === 1) this.form.controls.capitalAccountId.setValue(this.accounts()[0].id);
      }, error: error => this.error.set(vehicleError(error)),
    });
  }
  close() { if (!this.busy()) this.closed.emit(!!this.error()); }
  submit() {
    this.form.controls.expiresAt.updateValueAndValidity();
    this.form.markAllAsTouched();
    if (this.busy() || this.loading() || this.form.invalid) return;
    const value = this.form.getRawValue();
    this.busy.set(true); this.error.set('');
    this.api.reserve(this.vehicleId(), {
      customerName: value.customerName!.trim(), customerPhone: value.customerPhone?.trim() || null,
      customerEmail: value.customerEmail?.trim() || null, depositAmount: value.depositAmount!, capitalAccountId: value.capitalAccountId!,
      expiresAt: value.expiresAt ? new Date(value.expiresAt).toISOString() : null, notes: value.notes?.trim() || null,
    }).pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.busy.set(false))).subscribe({
      next: () => this.saved.emit(), error: error => this.error.set(vehicleError(error, true)),
    });
  }
}
