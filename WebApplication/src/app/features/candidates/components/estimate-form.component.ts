import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import {
  LocalizedCurrencyPipe,
  LocalizedNumberPipe,
} from '../../../shared/pipes/localized-format.pipe';
import { computed } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  inject,
  input,
  OnInit,
  output,
  signal,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import {
  CandidateAcquisitionType,
  CandidateDetails,
  CandidateEstimate,
  COST_CATEGORIES,
  CostCategory,
  CreateEstimateItemRequest,
} from '../models/candidate.models';
import { CandidateApiService } from '../services/candidate-api.service';
import { FieldErrorComponent } from './field-error.component';
import { apiError } from './candidate-form-utils';

@Component({
  selector: 'app-estimate-form',
  imports: [
    TranslatePipe,
    ReactiveFormsModule,
    FieldErrorComponent,
    LocalizedCurrencyPipe,
    LocalizedNumberPipe,
  ],
  templateUrl: './estimate-form.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EstimateFormComponent implements OnInit {
  readonly candidate = input.required<CandidateDetails>();
  readonly saved = output<CandidateEstimate>();
  readonly cancelled = output<boolean>();
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(CandidateApiService);
  private readonly translate = inject(TranslateService);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly categories = COST_CATEGORIES;
  protected readonly additionalCategories = COST_CATEGORIES.filter(
    (category) => category.value !== CostCategory.Purchase,
  );
  protected readonly busy = signal(false);
  protected readonly error = signal('');
  protected readonly form = this.fb.group({
    expectedSellingPrice: this.fb.control<number | null>(null, [
      Validators.required,
      Validators.min(0),
    ]),
    items: this.fb.array([this.item()], Validators.minLength(1)),
  });
  protected get items() {
    return this.form.controls.items;
  }
  private readonly changes = toSignal(this.form.valueChanges);
  protected readonly totalCost = computed(() => {
    this.changes();
    const amounts = this.form.getRawValue().items.map((item) => item.estimatedAmount);
    return amounts.some((value) => value === null || !Number.isFinite(value) || value < 0)
      ? null
      : amounts.reduce<number>((sum, value) => sum + value!, 0);
  });
  protected readonly profit = computed(() => {
    this.changes();
    const price = this.form.controls.expectedSellingPrice.value;
    const cost = this.totalCost();
    return price === null || !Number.isFinite(price) || price < 0 || cost === null
      ? null
      : price - cost;
  });
  protected readonly margin = computed(() => {
    const profit = this.profit();
    const price = this.form.controls.expectedSellingPrice.value;
    return profit === null || !price ? null : (profit / price) * 100;
  });

  ngOnInit() {
    const candidate = this.candidate();
    this.form.controls.expectedSellingPrice.setValue(
      candidate.latestEstimate?.expectedSellingPrice ?? null,
    );
    this.items.at(0).patchValue({
      category: CostCategory.Purchase,
      description: this.translate.instant('cost.0'),
      estimatedAmount: candidate.acquisitionType === CandidateAcquisitionType.Auction
        ? candidate.latestEstimate?.items.find(item => item.category === CostCategory.Purchase)?.estimatedAmount ?? null
        : candidate.askingPrice,
    });
    this.items.at(0).controls.category.disable();
    this.items.at(0).controls.description.disable();
  }
  private item(value?: CreateEstimateItemRequest) {
    return this.fb.group({
      category: this.fb.nonNullable.control(value?.category ?? CostCategory.Other, [
        Validators.required,
      ]),
      description: this.fb.nonNullable.control(value?.description ?? ''),
      estimatedAmount: this.fb.control<number | null>(value?.estimatedAmount ?? null, [
        Validators.required,
        Validators.min(0),
      ]),
    });
  }
  protected addItem() {
    this.items.push(this.item());
  }
  protected isPurchaseRow(index: number) {
    return index === 0;
  }
  protected removeItem(index: number) {
    if (index > 0 && !this.busy()) {
      this.items.removeAt(index);
    }
  }
  protected copyLatest() {
    const latest = this.candidate().latestEstimate;
    if (!latest || this.busy()) return;
    // Preserve the negotiated purchase price in this draft when copying other costs.
    while (this.items.length > 1) this.items.removeAt(this.items.length - 1);
    latest.items
      .filter((item) => item.category !== CostCategory.Purchase)
      .forEach((item) => this.items.push(this.item(item)));
    this.form.patchValue({ expectedSellingPrice: latest.expectedSellingPrice });
  }
  protected submit() {
    if (this.busy()) return;
    this.form.markAllAsTouched();
    if (!this.form.valid) {
      return;
    }
    const value = this.form.getRawValue();
    this.busy.set(true);
    this.error.set('');
    this.api
      .createEstimate({
        candidateId: this.candidate().id,
        expectedSellingPrice: value.expectedSellingPrice!,
        notes: null,
        items: value.items.map((item) => ({
          category: item.category,
          description: item.description.trim(),
          estimatedAmount: item.estimatedAmount!,
        })),
      })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.busy.set(false)),
      )
      .subscribe({
        next: (estimate) => this.saved.emit(estimate),
        error: (error) => this.error.set(apiError(error, true)),
      });
  }
}
