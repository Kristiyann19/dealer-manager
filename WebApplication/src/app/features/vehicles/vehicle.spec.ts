import { registerLocaleData } from '@angular/common';
import bgLocale from '@angular/common/locales/bg';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { provideTranslateService, TranslateService } from '@ngx-translate/core';
import { BehaviorSubject } from 'rxjs';
import en from '../../../assets/i18n/en.json';
import bg from '../../../assets/i18n/bg.json';
import { CostCategory } from '../candidates/models/candidate.models';
import { VehicleDetailsComponent } from './vehicle-details.component';
import { EditCostPlanComponent } from './edit-cost-plan.component';
import { AddExpenseComponent } from './add-expense.component';
import { ConfirmPaymentComponent } from './confirm-payment.component';
import { VehicleCostPlanItem, VehicleDetails, VehicleExpense } from './vehicle.models';

registerLocaleData(bgLocale);
const plan: VehicleCostPlanItem = {
  id: 4,
  category: CostCategory.Transport,
  description: 'Transport',
  currentEstimatedAmount: 1000,
  committedAmount: null,
  actualPaid: 0,
  remainingProjected: 1000,
  isCancelled: false,
};
const details: VehicleDetails = {
  id: 1,
  make: 'BMW',
  model: '320d',
  year: 2018,
  mileage: 50000,
  vin: 'VIN',
  status: 0,
  sourceCandidateId: 9,
  purchaseDate: '2026-09-25T00:00:00Z',
  actualPurchasePrice: 6300,
  actualExpenses: 0,
  totalInvested: 6300,
  remainingProjectedCosts: 1600,
  projectedFinalCost: 7900,
  expectedSellingPrice: 12000,
  projectedProfit: 4100,
  projectedROI: 51.9,
  originalForecast: {
    estimateId: 1,
    version: 1,
    items: [
      { id: 1, category: CostCategory.Purchase, description: 'Purchase', estimatedAmount: 6500 },
    ],
    originalEstimatedTotal: 8100,
    originalExpectedSellingPrice: 12000,
    originalExpectedProfit: 3900,
    originalExpectedROI: 48.15,
  },
};
const expense: VehicleExpense = {
  id: 1,
  category: CostCategory.Transport,
  description: 'Transport Italy',
  amount: 950,
  supplier: 'Carrier',
  documentNumber: 'INV-1',
  paidAt: '2026-09-27T00:00:00Z',
  costPlanItemId: 4,
  costPlanDescription: 'Transport',
  financialTransactionId: 3,
  capitalAccountId: 1,
  capitalAccountName: 'Main',
  currency: 'EUR',
};
const account = { capitalAccountId: 1, currency: 'EUR', currentBalance: 13700 };
const preview = { ...account, amount: 670, currentBalance: 5000, category: CostCategory.Transport };
function button(fixture: ComponentFixture<unknown>, label: string) {
  return Array.from(
    fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>,
  ).find((b) => b.textContent?.trim() === label)!;
}
describe('Vehicle finances', () => {
  let http: HttpTestingController;
  let params: BehaviorSubject<ReturnType<typeof convertToParamMap>>;
  beforeEach(() => {
    params = new BehaviorSubject(convertToParamMap({ id: '1' }));
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        provideTranslateService(),
        { provide: ActivatedRoute, useValue: { paramMap: params } },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('en', en);
    translate.setTranslation('bg', bg);
    translate.use('en').subscribe();
  });
  afterEach(() => http.verify());
  function load(
    value = details,
    items = [plan],
    history: VehicleExpense[] = [],
    balance = account.currentBalance,
  ) {
    http.expectOne('/api/vehicles/1').flush(value);
    http.expectOne('/api/vehicles/1/cost-plan').flush(items);
    http.expectOne('/api/vehicles/1/expenses').flush(history);
    http.expectOne('/api/vehicles/1/status-history').flush([]);
    http
      .expectOne('/api/vehicles/1/payment-account')
      .flush({ ...account, currentBalance: balance });
  }
  it('displays server summary, read-only forecast and expense history in both languages', () => {
    const fixture = TestBed.createComponent(VehicleDetailsComponent);
    load(details, [plan], [expense]);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Original forecast');
    expect(fixture.nativeElement.textContent).toContain('51.90%');
    expect(fixture.nativeElement.textContent).toContain('Transport Italy');
    expect(fixture.nativeElement.querySelector('#expenses-title').textContent).toContain(
      'Payment history',
    );
    expect(fixture.nativeElement.querySelector('#original-forecast').open).toBe(false);
    expect(
      fixture.nativeElement.querySelectorAll('#finance-summary-title + dl > div'),
    ).toHaveLength(5);
    expect(fixture.nativeElement.querySelector('button[aria-pressed]')).toBeNull();
    TestBed.inject(TranslateService).use('bg').subscribe();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('История на плащанията');
  });
  it('updates only the plan and reloads server summary and history after saving', () => {
    const fixture = TestBed.createComponent(VehicleDetailsComponent);
    load();
    fixture.detectChanges();
    button(fixture, 'Edit estimate').click();
    fixture.detectChanges();
    const input: HTMLInputElement = fixture.nativeElement.querySelector('#plan-estimate');
    input.value = '950';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    fixture.nativeElement
      .querySelector('form')
      .dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    const request = http.expectOne('/api/vehicles/1/cost-plan/4');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body.currentEstimatedAmount).toBe(950);
    expect(request.request.body.committedAmount).toBeNull();
    request.flush({ ...plan, committedAmount: 950 });
    load(
      { ...details, remainingProjectedCosts: 1550, projectedFinalCost: 7850, projectedROI: 52.87 },
      [{ ...plan, committedAmount: 950, remainingProjected: 950 }],
    );
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('52.87%');
    http.expectNone('/api/vehicles/1/expenses', 'no expense POST');
    expect(fixture.nativeElement.querySelector('form')).toBeNull();
  });
  it('rejects negative plans, accepts zero commitment and prevents duplicate submissions', () => {
    const fixture = TestBed.createComponent(EditCostPlanComponent);
    fixture.componentRef.setInput('vehicleId', 1);
    fixture.componentRef.setInput('item', plan);
    fixture.detectChanges();
    const page = fixture.componentInstance;
    page.form.patchValue({ currentEstimatedAmount: -1 });
    page.submit();
    http.expectNone('/api/vehicles/1/cost-plan/4');
    page.form.patchValue({
      currentEstimatedAmount: null,
      committedAmount: 0,
      description: ' Agreed ',
    });
    page.submit();
    page.submit();
    const request = http.expectOne('/api/vehicles/1/cost-plan/4');
    expect(request.request.body).toEqual({
      currentEstimatedAmount: null,
      committedAmount: 0,
      description: 'Agreed',
      isCancelled: false,
    });
    request.flush({ ...plan, committedAmount: 0 });
  });
  function expenseDialog() {
    const fixture = TestBed.createComponent(AddExpenseComponent);
    fixture.componentRef.setInput('vehicleId', 1);
    fixture.detectChanges();
    http.expectOne('/api/vehicles/1/payment-account').flush(account);
    fixture.detectChanges();
    return fixture;
  }
  function confirmationDialog(balance = 5000) {
    const fixture = TestBed.createComponent(ConfirmPaymentComponent);
    fixture.componentRef.setInput('vehicleId', 1);
    fixture.componentRef.setInput('itemId', 4);
    fixture.detectChanges();
    http
      .expectOne('/api/vehicles/1/cost-plan/4/payment-preview')
      .flush({ ...preview, currentBalance: balance });
    fixture.detectChanges();
    return fixture;
  }
  it('shows a confirmation without editable fields and prevents duplicate submissions', () => {
    const fixture = confirmationDialog();
    expect(fixture.nativeElement.querySelector('input, select, textarea, form')).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('€670.00');
    expect(fixture.nativeElement.textContent).toContain('€5,000.00');
    expect(fixture.nativeElement.textContent).toContain('€4,330.00');
    fixture.componentInstance.confirm();
    fixture.componentInstance.confirm();
    const request = http.expectOne('/api/vehicles/1/cost-plan/4/confirm-payment');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ expectedAmount: 670, expectedCapitalAccountId: 1 });
    request.flush({ ...expense, amount: 670 });
  });
  it('blocks confirmation with insufficient funds and translates the warning', () => {
    const fixture = confirmationDialog(600);
    expect(button(fixture, 'Confirm payment').disabled).toBe(true);
    fixture.componentInstance.confirm();
    http.expectNone('/api/vehicles/1/cost-plan/4/confirm-payment');
    TestBed.inject(TranslateService).use('bg').subscribe();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Потвърждаване на плащане');
    expect(fixture.nativeElement.textContent).toContain(bg.vehicle.errors.insufficientCapital);
  });
  it('requires a fresh preview after a conflict and never retries payment automatically', () => {
    const fixture = confirmationDialog();
    fixture.componentInstance.confirm();
    http
      .expectOne('/api/vehicles/1/cost-plan/4/confirm-payment')
      .flush({ code: 'paymentChanged' }, { status: 409, statusText: 'Conflict' });
    fixture.detectChanges();
    expect(button(fixture, 'Confirm payment').disabled).toBe(true);
    fixture.componentInstance.confirm();
    http.expectNone('/api/vehicles/1/cost-plan/4/confirm-payment');
    http
      .expectOne('/api/vehicles/1/cost-plan/4/payment-preview')
      .flush({ ...preview, amount: 700 });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('€700.00');
    expect(button(fixture, 'Confirm payment').disabled).toBe(false);
    expect(fixture.nativeElement.textContent).toContain(en.vehicle.errors.paymentChanged);
    expect(button(fixture, 'Refresh')).toBeUndefined();
  });
  it('shows missing purchase account errors without offering arbitrary accounts', () => {
    const fixture = TestBed.createComponent(AddExpenseComponent);
    fixture.componentRef.setInput('vehicleId', 1);
    fixture.detectChanges();
    http
      .expectOne('/api/vehicles/1/payment-account')
      .flush({ code: 'purchaseAccountMissing' }, { status: 409, statusText: 'Conflict' });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain(en.vehicle.errors.purchaseAccountMissing);
    expect(fixture.nativeElement.querySelector('form')).toBeNull();
  });
  it('supports an unexpected expense and refuses zero, blank and over-budget inputs', () => {
    const fixture = expenseDialog();
    const page = fixture.componentInstance;
    page.form.patchValue({ amount: 0, description: ' ' });
    page.submit();
    http.expectNone('/api/vehicles/1/expenses');
    page.form.patchValue({ amount: 14000, description: 'Battery' });
    fixture.detectChanges();
    page.submit();
    expect(page.insufficient()).toBe(true);
    http.expectNone('/api/vehicles/1/expenses');
    page.form.patchValue({ amount: 180, paidAt: '2026-09-27' });
    fixture.detectChanges();
    page.submit();
    const request = http.expectOne('/api/vehicles/1/expenses');
    expect(request.request.body.costPlanItemId).toBeNull();
    expect(request.request.body.paidAt).toBe(new Date('2026-09-27T00:00:00').toISOString());
    request.flush(expense);
  });
  it('keeps expense input on a backend conflict and never retries the POST automatically', () => {
    const fixture = expenseDialog();
    const page = fixture.componentInstance;
    page.form.patchValue({ amount: 950, description: 'Transport' });
    fixture.detectChanges();
    page.submit();
    http
      .expectOne('/api/vehicles/1/expenses')
      .flush({ code: 'insufficientCapital' }, { status: 409, statusText: 'Conflict' });
    fixture.detectChanges();
    expect(page.error()).toBe('vehicle.errors.insufficientCapital');
    expect(page.form.value.amount).toBe(950);
    expect(page.busy()).toBe(false);
  });
  it('refreshes all vehicle data after an expense and requests fresh account balances on reopening', () => {
    const fixture = TestBed.createComponent(VehicleDetailsComponent);
    load();
    fixture.detectChanges();
    button(fixture, 'Add unexpected expense').click();
    fixture.detectChanges();
    http.expectOne('/api/vehicles/1/payment-account').flush(account);
    fixture.detectChanges();
    const set = (selector: string, value: string) => {
      const field: HTMLInputElement = fixture.nativeElement.querySelector(selector);
      field.value = value;
      field.dispatchEvent(new Event('input'));
    };
    set('#expense-description', 'Transport');
    set('#expense-amount', '950');
    fixture.detectChanges();
    fixture.nativeElement
      .querySelector('form')
      .dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    http.expectOne('/api/vehicles/1/expenses').flush(expense);
    load(
      { ...details, actualExpenses: 950, totalInvested: 7250 },
      [{ ...plan, actualPaid: 950, remainingProjected: 0 }],
      [expense],
    );
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('€7,250.00');
    button(fixture, 'Add unexpected expense').click();
    fixture.detectChanges();
    http.expectOne('/api/vehicles/1/payment-account').flush({ ...account, currentBalance: 12750 });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('€12,750.00');
  });
  it('handles empty plans, missing forecast and sold vehicles', () => {
    const fixture = TestBed.createComponent(VehicleDetailsComponent);
    load(
      {
        ...details,
        status: 9,
        originalForecast: null,
        expectedSellingPrice: null,
        projectedProfit: null,
      },
      [],
    );
    fixture.detectChanges();
    expect(button(fixture, 'Add unexpected expense').disabled).toBe(true);
    expect(fixture.nativeElement.textContent).toContain('No decision snapshot');
    expect(fixture.nativeElement.textContent).toContain('No upcoming expenses');
  });
  it('keeps a successful payment notice if reloading fails and retries only GET requests', () => {
    const fixture = TestBed.createComponent(VehicleDetailsComponent);
    load();
    fixture.componentInstance.saved('expense');
    expect(fixture.componentInstance.loading()).toBe(true);
    expect(fixture.componentInstance.vehicle()).toBeNull();
    expect(fixture.componentInstance.paymentAccount()).toBeNull();
    const requests = http.match((request) => request.url.startsWith('/api/vehicles/1'));
    expect(requests.every((request) => request.request.method === 'GET')).toBe(true);
    requests
      .find((request) => request.request.url === '/api/vehicles/1')!
      .flush({}, { status: 503, statusText: 'Unavailable' });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain(en.vehicle.expenseSaved);
    button(fixture, en.finance.retry).click();
    load(
      { ...details, actualExpenses: 670 },
      [{ ...plan, actualPaid: 670, remainingProjected: 0 }],
      [{ ...expense, amount: 670 }],
      4330,
    );
    fixture.detectChanges();
    expect(fixture.componentInstance.paymentAccount()?.currentBalance).toBe(4330);
    expect(fixture.componentInstance.expenses()).toHaveLength(1);
  });
  it('still loads vehicle history when the purchase account is unavailable', () => {
    const fixture = TestBed.createComponent(VehicleDetailsComponent);
    http.expectOne('/api/vehicles/1').flush(details);
    http.expectOne('/api/vehicles/1/cost-plan').flush([plan]);
    http.expectOne('/api/vehicles/1/expenses').flush([expense]);
    http.expectOne('/api/vehicles/1/status-history').flush([]);
    http
      .expectOne('/api/vehicles/1/payment-account')
      .flush({ code: 'purchaseAccountMissing' }, { status: 409, statusText: 'Conflict' });
    fixture.detectChanges();
    expect(fixture.componentInstance.paymentAccount()).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Transport Italy');
    expect(fixture.nativeElement.textContent).not.toContain(en.vehicle.purchaseAccountCapital);
  });
  it('cancels obsolete loads when navigating between vehicles', () => {
    TestBed.createComponent(VehicleDetailsComponent);
    const requests = http.match((request) => request.url.startsWith('/api/vehicles/1'));
    params.next(convertToParamMap({ id: '2' }));
    expect(requests.every((request) => request.cancelled)).toBe(true);
    http.expectOne('/api/vehicles/2').flush({ ...details, id: 2 });
    http.expectOne('/api/vehicles/2/cost-plan').flush([]);
    http.expectOne('/api/vehicles/2/expenses').flush([]);
    http.expectOne('/api/vehicles/2/status-history').flush([]);
    http.expectOne('/api/vehicles/2/payment-account').flush(account);
  });
  it('confirms transport, removes it from pending, refreshes history and totals, and reloads capital', () => {
    const fixture = TestBed.createComponent(VehicleDetailsComponent);
    load(details, [{ ...plan, currentEstimatedAmount: 670, remainingProjected: 670 }]);
    fixture.detectChanges();
    button(fixture, 'Add payment').click();
    fixture.detectChanges();
    http.expectOne('/api/vehicles/1/cost-plan/4/payment-preview').flush(preview);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('app-add-vehicle-expense')).toBeNull();
    button(fixture, 'Confirm payment').click();
    http
      .expectOne('/api/vehicles/1/cost-plan/4/confirm-payment')
      .flush({ ...expense, amount: 670 });
    load(
      { ...details, actualExpenses: 670, totalInvested: 6970, remainingProjectedCosts: 0 },
      [{ ...plan, currentEstimatedAmount: 670, actualPaid: 670, remainingProjected: 0 }],
      [{ ...expense, amount: 670 }],
      4330,
    );
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('article')).toBeNull();
    expect(fixture.nativeElement.querySelector('tbody').textContent).toContain('€670.00');
    expect(fixture.nativeElement.textContent).toContain('€6,970.00');
    expect(fixture.componentInstance.plan()).toHaveLength(1);
    expect(fixture.componentInstance.paymentAccount()?.currentBalance).toBe(4330);
    expect(fixture.nativeElement.textContent).toContain('€4,330.00');
    expect(button(fixture, 'Refresh')).toBeUndefined();
    button(fixture, 'Add unexpected expense').click();
    fixture.detectChanges();
    http.expectOne('/api/vehicles/1/payment-account').flush({ ...account, currentBalance: 4330 });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('€4,330.00');
    expect(fixture.nativeElement.querySelector('#expense-account')).toBeNull();
  });
  it('keeps advanced payment inputs available but collapsed and unexpected payments unlinked', () => {
    const fixture = expenseDialog();
    const page = fixture.componentInstance;
    expect(fixture.nativeElement.querySelector('#expense-category')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('#expense-plan')).toBeNull();
    const options: HTMLDetailsElement = fixture.nativeElement.querySelector('details');
    expect(options.open).toBe(false);
    options.querySelector('summary')!.click();
    expect(options.open).toBe(true);
    page.form.patchValue({
      amount: 180,
      description: 'Battery',
      supplier: 'Supplier',
      documentNumber: 'INV',
      paidAt: '2026-09-29',
    });
    fixture.detectChanges();
    page.submit();
    const request = http.expectOne('/api/vehicles/1/expenses');
    expect(request.request.body.costPlanItemId).toBeNull();
    expect(request.request.body.supplier).toBe('Supplier');
    expect(request.request.body.documentNumber).toBe('INV');
    expect(request.request.body.paidAt).toBe(new Date('2026-09-29T00:00:00').toISOString());
    request.flush(expense);
  });
  it('preserves existing commitments when editing the visible estimate', () => {
    const fixture = TestBed.createComponent(EditCostPlanComponent);
    fixture.componentRef.setInput('vehicleId', 1);
    fixture.componentRef.setInput('item', { ...plan, committedAmount: 950 });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('details').open).toBe(false);
    fixture.componentInstance.form.patchValue({ currentEstimatedAmount: 1100 });
    fixture.componentInstance.submit();
    const request = http.expectOne('/api/vehicles/1/cost-plan/4');
    expect(request.request.body).toEqual({
      currentEstimatedAmount: 1100,
      committedAmount: 950,
      description: 'Transport',
      isCancelled: false,
    });
    request.flush(plan);
  });
  it('shows the effective forecast without changing server remaining amounts', () => {
    const fixture = TestBed.createComponent(VehicleDetailsComponent);
    load(details, [{ ...plan, committedAmount: 950, actualPaid: 300, remainingProjected: 650 }]);
    fixture.detectChanges();
    const row: HTMLElement = fixture.nativeElement.querySelector('article');
    expect(row.textContent).toContain('€950.00');
    expect(row.textContent).toContain('€650.00');
    expect(row.textContent).not.toContain('Committed');
    expect(row.querySelectorAll('dl > div')).toHaveLength(3);
    const original: HTMLDetailsElement = fixture.nativeElement.querySelector('#original-forecast');
    expect(original.open).toBe(false);
    original.querySelector('summary')!.click();
    expect(original.open).toBe(true);
    expect(original.textContent).toContain('€6,500.00');
  });
});
