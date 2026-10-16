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
import { VehicleExpense } from './vehicle.models';
import {
  positiveAmount,
  requiredText,
  validOptionalDate,
  vehicleError,
} from './vehicle-form-utils';

@Component({
  selector: 'app-correct-expense',
  imports: [TranslatePipe, ReactiveFormsModule, DialogModule, LocalizedCurrencyPipe],
  templateUrl: './correct-expense.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CorrectExpenseComponent {
  readonly vehicleId = input.required<number>();
  readonly expense = input.required<VehicleExpense>();
  readonly reverseOnly = input(false);
  readonly saved = output<void>();
  readonly closed = output<boolean>();
  readonly busy = signal(false);
  readonly error = signal('');
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
    reason: ['', [requiredText, Validators.maxLength(1000)]],
  });
  private readonly values = toSignal(this.form.valueChanges, { initialValue: this.form.value });
  readonly balanceChange = computed(
    () => this.expense().amount - (this.reverseOnly() ? 0 : (this.values().amount ?? 0)),
  );
  ngOnInit() {
    const e = this.expense();
    this.form.patchValue({
      category: e.category,
      description: e.description,
      amount: e.amount,
      supplier: e.supplier ?? '',
      documentNumber: e.documentNumber ?? '',
      paidAt: e.paidAt.slice(0, 10),
    });
    if (e.costPlanItemId != null) this.form.controls.category.disable();
    if (this.reverseOnly()) {
      for (const key of [
        'category',
        'description',
        'amount',
        'supplier',
        'documentNumber',
        'paidAt',
      ] as const)
        this.form.controls[key].disable();
    }
  }
  close() {
    if (!this.busy()) this.closed.emit(!!this.error());
  }
  submit() {
    this.form.markAllAsTouched();
    if (this.busy() || this.form.invalid || this.expense().isReversed) return;
    const v = this.form.getRawValue();
    const e = this.expense();
    this.busy.set(true);
    this.error.set('');
    const request = this.reverseOnly()
      ? this.api.reverseExpense(this.vehicleId(), e.id, v.reason!.trim())
      : this.api.correctExpense(this.vehicleId(), e.id, {
          reason: v.reason!.trim(),
          category: v.category!,
          description: v.description!.trim(),
          amount: v.amount!,
          costPlanItemId: e.costPlanItemId,
          capitalAccountId: e.capitalAccountId,
          supplier: v.supplier?.trim() || null,
          documentNumber: v.documentNumber?.trim() || null,
          paidAt:
            v.paidAt && v.paidAt !== e.paidAt.slice(0, 10)
              ? new Date(v.paidAt + 'T00:00:00Z').toISOString()
              : e.paidAt,
        });
    request
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
