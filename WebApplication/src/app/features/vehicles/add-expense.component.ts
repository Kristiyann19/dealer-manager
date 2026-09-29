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
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { TranslatePipe } from '@ngx-translate/core';
import { DialogModule } from 'primeng/dialog';
import { LocalizedCurrencyPipe } from '../../shared/pipes/localized-format.pipe';
import { COST_CATEGORIES, CostCategory } from '../candidates/models/candidate.models';
import { VehicleApiService } from './vehicle-api.service';
import { VehiclePaymentAccount } from './vehicle.models';
import {
  positiveAmount,
  requiredText,
  validOptionalDate,
  vehicleError,
} from './vehicle-form-utils';

@Component({
  selector: 'app-add-vehicle-expense',
  imports: [TranslatePipe, ReactiveFormsModule, DialogModule, LocalizedCurrencyPipe],
  templateUrl: './add-expense.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AddExpenseComponent {
  readonly vehicleId = input.required<number>();
  readonly saved = output<void>();
  readonly closed = output<void>();
  readonly busy = signal(false);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly loadError = signal('');
  readonly account = signal<VehiclePaymentAccount | null>(null);
  readonly categories = COST_CATEGORIES;
  private readonly api = inject(VehicleApiService);
  private readonly destroyRef = inject(DestroyRef);
  readonly form = inject(FormBuilder).group({
    category: [CostCategory.Other, Validators.required],
    description: ['', requiredText],
    amount: [null as number | null, positiveAmount],
    supplier: [''],
    documentNumber: [''],
    paidAt: ['', validOptionalDate],
  });
  private readonly values = toSignal(this.form.valueChanges, { initialValue: this.form.value });
  readonly remaining = computed(
    () => (this.account()?.currentBalance ?? 0) - (this.values().amount ?? 0),
  );
  readonly insufficient = computed(() => !!this.account() && this.remaining() < 0);
  ngOnInit() {
    this.loadAccount();
  }
  loadAccount() {
    if (this.busy()) return;
    this.loading.set(true);
    this.loadError.set('');
    this.account.set(null);
    this.api
      .paymentAccount(this.vehicleId())
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.loading.set(false)),
      )
      .subscribe({
        next: (account) => this.account.set(account),
        error: (error) => this.loadError.set(vehicleError(error)),
      });
  }
  close() {
    if (!this.busy()) this.closed.emit();
  }
  submit() {
    this.form.markAllAsTouched();
    if (
      this.busy() ||
      this.loading() ||
      this.loadError() ||
      this.form.invalid ||
      this.insufficient() ||
      !this.account()
    )
      return;
    const value = this.form.getRawValue();
    this.busy.set(true);
    this.error.set('');
    this.api
      .addExpense(this.vehicleId(), {
        costPlanItemId: null,
        category: value.category!,
        description: value.description!.trim(),
        amount: value.amount!,
        supplier: value.supplier?.trim() || null,
        documentNumber: value.documentNumber?.trim() || null,
        ...(value.paidAt ? { paidAt: new Date(value.paidAt + 'T00:00:00').toISOString() } : {}),
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
