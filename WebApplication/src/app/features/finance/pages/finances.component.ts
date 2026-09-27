import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  inject,
  signal,
} from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule, ValidatorFn, Validators } from '@angular/forms';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { BehaviorSubject, catchError, finalize, forkJoin, map, of, switchMap } from 'rxjs';
import { TranslatePipe } from '@ngx-translate/core';
import { DialogModule } from 'primeng/dialog';
import {
  LocalizedCurrencyPipe,
  LocalizedDatePipe,
} from '../../../shared/pipes/localized-format.pipe';
import { CapitalAccountApiService } from '../services/capital-account-api.service';
import {
  CapitalAccount,
  CapitalAccountDetails,
  FinancialTransaction,
  TRANSACTION_TYPE_KEYS,
  TransactionDirection,
} from '../models/finance.models';

const requiredText: ValidatorFn = (control) =>
  typeof control.value === 'string' && control.value.trim() ? null : { required: true };
const positiveAmount: ValidatorFn = (control) =>
  typeof control.value === 'number' && Number.isFinite(control.value) && control.value > 0
    ? null
    : { positive: true };
const validDate: ValidatorFn = (control) =>
  !control.value || Number.isFinite(new Date(control.value + 'T00:00:00').getTime())
    ? null
    : { date: true };

@Component({
  selector: 'app-finances',
  imports: [
    ReactiveFormsModule,
    TranslatePipe,
    DialogModule,
    LocalizedCurrencyPipe,
    LocalizedDatePipe,
  ],
  templateUrl: './finances.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FinancesComponent {
  private readonly api = inject(CapitalAccountApiService);
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);
  private readonly reload = new BehaviorSubject<number | null>(null);
  private readonly selection = new BehaviorSubject<number | null>(null);
  readonly accounts = signal<CapitalAccount[]>([]);
  readonly selectedId = signal<number | null>(null);
  readonly details = signal<CapitalAccountDetails | null>(null);
  readonly transactions = signal<FinancialTransaction[]>([]);
  readonly loading = signal(true);
  readonly detailLoading = signal(false);
  readonly error = signal('');
  readonly detailError = signal('');
  readonly saveError = signal('');
  readonly success = signal('');
  readonly busy = signal(false);
  readonly dialog = signal<'create' | 'contribution' | null>(null);
  readonly typeKeys = TRANSACTION_TYPE_KEYS;
  readonly direction = TransactionDirection;
  readonly createForm = this.fb.nonNullable.group({
    name: ['', requiredText],
    currency: ['EUR', requiredText],
  });
  readonly contributionForm = this.fb.group({
    accountId: [null as number | null, Validators.required],
    amount: [null as number | null, positiveAmount],
    description: ['', requiredText],
    occurredAt: ['', validDate],
  });
  private readonly contributionAccountId = toSignal(
    this.contributionForm.controls.accountId.valueChanges,
    { initialValue: null },
  );
  readonly contributionAccount = computed(() =>
    this.accounts().find((a) => a.id === this.contributionAccountId()),
  );
  readonly totals = computed(() => {
    const totals = new Map<string, number>();
    for (const account of this.accounts())
      totals.set(account.currency, (totals.get(account.currency) ?? 0) + account.currentBalance);
    return Array.from(totals, ([currency, balance]) => ({ currency, balance }));
  });

  constructor() {
    this.selection
      .pipe(
        switchMap((id) => {
          this.details.set(null);
          this.transactions.set([]);
          this.detailError.set('');
          this.detailLoading.set(id !== null);
          return id === null
            ? of(null)
            : forkJoin({
                details: this.api.getCapitalAccountDetails(id),
                transactions: this.api.getTransactions(id),
              }).pipe(
                map((data) => ({ data, error: '' })),
                catchError((error) => of({ data: null, error: this.errorKey(error) })),
              );
        }),
        takeUntilDestroyed(),
      )
      .subscribe((result) => {
        this.detailLoading.set(false);
        if (result?.data) {
          this.details.set(result.data.details);
          this.transactions.set(result.data.transactions);
        }
        this.detailError.set(result?.error ?? '');
      });
    this.reload
      .pipe(
        switchMap((preferredId) => {
          this.loading.set(true);
          this.error.set('');
          this.selection.next(null);
          return this.api.getCapitalAccounts().pipe(
            map((accounts) => ({ accounts, preferredId, error: '' })),
            catchError((error) =>
              of({ accounts: [] as CapitalAccount[], preferredId, error: this.errorKey(error) }),
            ),
          );
        }),
        takeUntilDestroyed(),
      )
      .subscribe((result) => {
        this.loading.set(false);
        this.error.set(result.error);
        if (result.error) return;
        this.accounts.set(result.accounts.filter((account) => account.isActive));
        const preferred = result.preferredId ?? this.selectedId();
        this.selectAccount(
          this.accounts().find((a) => a.id === preferred)?.id ?? this.accounts()[0]?.id ?? null,
        );
      });
  }
  refreshAccounts() {
    this.reload.next(this.selectedId());
  }
  selectAccount(id: number | null) {
    this.selectedId.set(id);
    this.selection.next(id);
  }
  openDialog(mode: 'create' | 'contribution') {
    this.saveError.set('');
    this.success.set('');
    this.createForm.reset({ name: '', currency: 'EUR' });
    this.contributionForm.reset({
      accountId: this.selectedId() ?? this.accounts()[0]?.id ?? null,
      amount: null,
      description: '',
      occurredAt: '',
    });
    this.dialog.set(mode);
  }
  closeDialog() {
    if (!this.busy()) this.dialog.set(null);
  }
  createAccount() {
    if (this.busy()) return;
    this.createForm.markAllAsTouched();
    if (this.createForm.invalid) return;
    const value = this.createForm.getRawValue();
    this.busy.set(true);
    this.saveError.set('');
    this.api
      .createCapitalAccount({
        name: value.name.trim(),
        currency: value.currency.trim().toUpperCase(),
      })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.busy.set(false)),
      )
      .subscribe({
        next: (account) => this.saved('finance.accountCreated', account.id),
        error: (error) => this.saveError.set(this.errorKey(error, true)),
      });
  }
  addCapital() {
    if (this.busy()) return;
    this.contributionForm.markAllAsTouched();
    const value = this.contributionForm.getRawValue();
    if (this.contributionForm.invalid || value.accountId === null || value.amount === null) return;
    if (!this.accounts().some((a) => a.id === value.accountId)) {
      this.saveError.set('finance.errors.account');
      return;
    }
    this.busy.set(true);
    this.saveError.set('');
    this.api
      .addCapital(value.accountId, {
        amount: value.amount,
        description: value.description!.trim(),
        ...(value.occurredAt
          ? { occurredAt: new Date(value.occurredAt + 'T00:00:00').toISOString() }
          : {}),
      })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.busy.set(false)),
      )
      .subscribe({
        next: () => this.saved('finance.capitalAdded', value.accountId!),
        error: (error) => this.saveError.set(this.errorKey(error, true)),
      });
  }
  private saved(message: string, id: number) {
    this.dialog.set(null);
    this.success.set(message);
    this.reload.next(id);
  }
  private errorKey(error: unknown, writing = false) {
    if (!(error instanceof HttpErrorResponse) || error.status === 0 || error.status >= 500)
      return writing ? 'finance.errors.uncertain' : 'finance.errors.unavailable';
    if (error.status === 404) return 'finance.errors.notFound';
    if (error.status === 409) return 'finance.errors.inactive';
    return 'finance.errors.validation';
  }
}
