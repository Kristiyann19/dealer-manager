import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
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
  CandidateDetails,
  CandidateEstimate,
  COST_CATEGORIES,
  CostCategory,
  CreateEstimateItemRequest,
} from '../models/candidate.models';
import { CandidateApiService } from '../services/candidate-api.service';
import { FieldErrorComponent } from './field-error.component';
import { apiError, optionalText, requiredText } from './candidate-form-utils';

@Component({
  selector: 'app-estimate-form',
  imports: [TranslatePipe, ReactiveFormsModule, FieldErrorComponent],
  templateUrl: './estimate-form.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EstimateFormComponent implements OnInit {
  readonly candidate = input.required<CandidateDetails>();
  readonly saved = output<CandidateEstimate>();
  readonly cancelled = output<void>();
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(CandidateApiService);
  private readonly translate = inject(TranslateService);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly categories = COST_CATEGORIES;
  protected readonly additionalCategories = COST_CATEGORIES.filter(
    (category) => category.value !== CostCategory.Purchase,
  );
  protected readonly hasFixedPurchase = computed(() => this.candidate().askingPrice !== null);
  protected readonly busy = signal(false);
  protected readonly error = signal('');
  protected readonly form = this.fb.group({
    expectedSellingPrice: this.fb.control<number | null>(null, [
      Validators.required,
      Validators.min(0),
    ]),
    notes: this.fb.nonNullable.control(''),
    items: this.fb.array([this.item()], Validators.minLength(1)),
  });
  protected get items() {
    return this.form.controls.items;
  }

  ngOnInit() {
    const candidate = this.candidate();
    this.form.controls.expectedSellingPrice.setValue(
      candidate.latestEstimate?.expectedSellingPrice ?? null,
    );
    this.items.at(0).patchValue({
      category: CostCategory.Purchase,
      description: this.translate.instant('cost.0'),
      estimatedAmount: candidate.askingPrice,
    });
    if (this.hasFixedPurchase()) this.items.at(0).disable();
  }
  private item(value?: CreateEstimateItemRequest) {
    return this.fb.group({
      category: this.fb.nonNullable.control(value?.category ?? CostCategory.Other, [
        Validators.required,
      ]),
      description: this.fb.nonNullable.control(value?.description ?? '', requiredText),
      estimatedAmount: this.fb.control<number | null>(value?.estimatedAmount ?? null, [
        Validators.required,
        Validators.min(0),
      ]),
    });
  }
  protected addItem() {
    this.items.push(this.item());
  }
  protected isFixedPurchase(index: number) {
    return index === 0 && this.hasFixedPurchase();
  }
  protected removeItem(index: number) {
    if (this.items.length > 1 && !this.isFixedPurchase(index)) this.items.removeAt(index);
  }
  protected copyLatest() {
    const latest = this.candidate().latestEstimate;
    if (!latest || this.busy()) return;
    // Copy into a new draft; the original quote remains the single purchase cost.
    if (this.hasFixedPurchase()) {
      while (this.items.length > 1) this.items.removeAt(this.items.length - 1);
      latest.items
        .filter((item) => item.category !== CostCategory.Purchase)
        .forEach((item) => this.items.push(this.item(item)));
    } else {
      this.items.clear();
      latest.items.forEach((item) => this.items.push(this.item(item)));
      if (!this.items.length) this.items.push(this.item());
    }
    this.form.patchValue({ expectedSellingPrice: latest.expectedSellingPrice, notes: '' });
  }
  protected submit() {
    if (this.busy()) return;
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    const value = this.form.getRawValue();
    this.busy.set(true);
    this.error.set('');
    this.api
      .createEstimate({
        candidateId: this.candidate().id,
        expectedSellingPrice: value.expectedSellingPrice!,
        notes: optionalText(value.notes),
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
