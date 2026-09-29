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
import { HttpErrorResponse } from '@angular/common/http';
import { AbstractControl, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { TranslatePipe } from '@ngx-translate/core';
import { DialogModule } from 'primeng/dialog';
import { RouterLink } from '@angular/router';
import { LocalizedCurrencyPipe } from '../../../shared/pipes/localized-format.pipe';
import { CapitalAccountApiService } from '../../finance/services/capital-account-api.service';
import { CapitalAccount } from '../../finance/models/finance.models';
import { CandidateApiService } from '../services/candidate-api.service';
import { CandidateDetails, CostCategory } from '../models/candidate.models';
import { PurchaseCandidateResult } from '../models/purchase.models';

@Component({
  selector: 'app-purchase-dialog',
  imports: [ReactiveFormsModule, TranslatePipe, DialogModule, LocalizedCurrencyPipe, RouterLink],
  templateUrl: './purchase-dialog.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PurchaseDialogComponent {
  readonly candidate = input.required<CandidateDetails>();
  readonly closed = output<boolean>();
  readonly purchased = output<PurchaseCandidateResult>();
  private readonly accountsApi = inject(CapitalAccountApiService);
  private readonly api = inject(CandidateApiService);
  private readonly destroyRef = inject(DestroyRef);
  readonly accounts = signal<CapitalAccount[]>([]);
  readonly loading = signal(true);
  readonly busy = signal(false);
  readonly error = signal('');
  readonly loadError = signal('');
  readonly form = inject(FormBuilder).group({
    capitalAccountId: [null as number | null, Validators.required],
    actualPurchasePrice: [
      null as number | null,
      [
        Validators.required,
        (control: AbstractControl) =>
          typeof control.value === 'number' && Number.isFinite(control.value) && control.value > 0
            ? null
            : { positive: true },
      ],
    ],
    purchaseDate: [
      '',
      (control: AbstractControl) =>
        !control.value || Number.isFinite(new Date(control.value + 'T00:00:00').getTime())
          ? null
          : { date: true },
    ],
  });
  private readonly values = toSignal(this.form.valueChanges, { initialValue: this.form.value });
  readonly account = computed(() =>
    this.accounts().find((a) => a.id === this.values().capitalAccountId),
  );
  readonly forecastPrice = computed(
    () =>
      this.candidate().latestEstimate?.items.find((i) => i.category === CostCategory.Purchase)
        ?.estimatedAmount ?? null,
  );
  readonly remaining = computed(
    () => (this.account()?.currentBalance ?? 0) - (this.values().actualPurchasePrice ?? 0),
  );
  readonly insufficient = computed(() => !!this.account() && this.remaining() < 0);
  readonly missingYear = computed(() => !this.candidate().year);
  ngOnInit() {
    this.form.patchValue({ actualPurchasePrice: this.forecastPrice() });
    this.loadAccounts();
  }
  loadAccounts() {
    if (this.busy()) return;
    this.loading.set(true);
    this.loadError.set('');
    this.accountsApi
      .getCapitalAccounts()
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.loading.set(false)),
      )
      .subscribe({
        next: (accounts) => {
          this.accounts.set(
            accounts.filter((a) => a.isActive && a.currency.trim().toUpperCase() === 'EUR'),
          );
          this.form.patchValue({
            capitalAccountId: this.accounts().some((a) => a.id === this.form.value.capitalAccountId)
              ? this.form.value.capitalAccountId
              : (this.accounts()[0]?.id ?? null),
          });
        },
        error: () => this.loadError.set('finance.errors.unavailable'),
      });
  }
  close() {
    if (!this.busy()) this.closed.emit(!!this.error());
  }
  submit() {
    this.form.markAllAsTouched();
    if (
      this.busy() ||
      this.loading() ||
      this.loadError() ||
      this.form.invalid ||
      !this.account() ||
      this.insufficient() ||
      this.missingYear()
    )
      return;
    const value = this.form.getRawValue();
    this.busy.set(true);
    this.error.set('');
    this.api
      .purchase(this.candidate().id, {
        capitalAccountId: value.capitalAccountId!,
        actualPurchasePrice: value.actualPurchasePrice!,
        ...(value.purchaseDate
          ? { purchaseDate: new Date(value.purchaseDate + 'T00:00:00').toISOString() }
          : {}),
      })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.busy.set(false)),
      )
      .subscribe({
        next: (result) => this.purchased.emit(result),
        error: (error: unknown) => {
          const codes = [
            'status',
            'alreadyPurchased',
            'estimate',
            'snapshot',
            'year',
            'inactive',
            'currency',
            'insufficientCapital',
          ];
          if (error instanceof HttpErrorResponse) {
            const body: unknown = error.error;
            const code = body && typeof body === 'object' && 'code' in body ? body.code : null;
            if (typeof code === 'string' && codes.includes(code)) {
              this.error.set('purchase.errors.' + code);
              return;
            }
            if (error.status === 409) {
              this.error.set('purchase.errors.conflict');
              return;
            }
            if (error.status === 404) {
              this.error.set('purchase.errors.notFound');
              return;
            }
            if (error.status === 400) {
              this.error.set('finance.errors.validation');
              return;
            }
          }
          this.error.set('purchase.errors.uncertain');
        },
      });
  }
}
