import { registerLocaleData } from '@angular/common';
import bgLocale from '@angular/common/locales/bg';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideTranslateService, TranslateService } from '@ngx-translate/core';
import en from '../../../assets/i18n/en.json';
import bg from '../../../assets/i18n/bg.json';
import { FinancesComponent } from './pages/finances.component';
import {
  CapitalAccount,
  FinancialTransaction,
  TransactionDirection,
  TransactionType,
} from './models/finance.models';

registerLocaleData(bgLocale);
const base = '/api/capital-accounts';
const account: CapitalAccount = {
  id: 1,
  name: 'Main',
  currency: 'EUR',
  isActive: true,
  currentBalance: 1000,
};
const transaction: FinancialTransaction = {
  id: 10,
  capitalAccountId: 1,
  type: TransactionType.CapitalContribution,
  direction: TransactionDirection.In,
  amount: 2000,
  description: 'Deposit',
  vehicleId: null,
  occurredAt: '2026-09-25T12:00:00Z',
  createdAt: '2026-09-25T12:00:00Z',
};

describe('Finance API workflows', () => {
  let http: HttpTestingController;
  let fixture: ComponentFixture<FinancesComponent>;
  let page: FinancesComponent;
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideTranslateService()],
    });
    http = TestBed.inject(HttpTestingController);
    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('en', en);
    translate.setTranslation('bg', bg);
    translate.use('en').subscribe();
    fixture = TestBed.createComponent(FinancesComponent);
    page = fixture.componentInstance;
  });
  afterEach(() => http.verify());
  function details(value = account, transactions: FinancialTransaction[] = []) {
    http.expectOne(`${base}/${value.id}`).flush({ ...value, latestTransactions: [] });
    http.expectOne(`${base}/${value.id}/transactions`).flush(transactions);
    fixture.detectChanges();
  }
  function load(accounts = [account]) {
    http.expectOne(base).flush(accounts);
    if (accounts.length) details(accounts[0]);
    else fixture.detectChanges();
  }

  it('groups backend balances by currency and excludes inactive accounts', () => {
    http
      .expectOne(base)
      .flush([
        account,
        { ...account, id: 2, currentBalance: 500 },
        { ...account, id: 3, currency: 'USD', currentBalance: 700 },
        { ...account, id: 4, isActive: false, currentBalance: 9999 },
      ]);
    details(account, [transaction]);
    expect(page.totals()).toEqual([
      { currency: 'EUR', balance: 1500 },
      { currency: 'USD', balance: 700 },
    ]);
    expect(page.transactions()[0].amount).toBe(2000);
  });
  it('shows empty states and reacts to language changes', () => {
    load([]);
    expect(fixture.nativeElement.textContent).toContain('No capital accounts yet.');
    TestBed.inject(TranslateService).use('bg').subscribe();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain(
      'Все още няма създадена капиталова сметка.',
    );
  });
  it('cancels stale account requests on a new selection', () => {
    load();
    page.selectAccount(2);
    const staleDetails = http.expectOne(`${base}/2`);
    const staleHistory = http.expectOne(`${base}/2/transactions`);
    page.selectAccount(1);
    expect(staleDetails.cancelled).toBe(true);
    expect(staleHistory.cancelled).toBe(true);
    details();
    expect(page.details()?.id).toBe(1);
  });
  it('validates create, trims fields, prevents duplicate saves and selects the created account', () => {
    load([]);
    page.openDialog('create');
    page.createForm.setValue({ name: '  ', currency: 'EUR' });
    page.createAccount();
    http.expectNone(base);
    page.createForm.setValue({ name: ' New ', currency: ' eur ' });
    page.createAccount();
    page.createAccount();
    const request = http.expectOne(base);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ name: 'New', currency: 'EUR' });
    page.closeDialog();
    expect(page.dialog()).toBe('create');
    const created = { ...account, id: 2, name: 'New', currentBalance: 0 };
    request.flush(created);
    expect(page.dialog()).toBeNull();
    expect(page.loading()).toBe(true);
    expect(page.details()).toBeNull();
    http.expectOne(base).flush([account, created]);
    details(created);
    expect(page.selectedId()).toBe(2);
    expect(page.success()).toBe('finance.accountCreated');
  });
  it('rejects nonpositive contributions and refreshes balances and full history after saving', () => {
    load();
    page.openDialog('contribution');
    page.contributionForm.patchValue({ amount: 0, description: ' Deposit ' });
    page.addCapital();
    http.expectNone(`${base}/1/contributions`);
    page.contributionForm.patchValue({ amount: 25 });
    page.addCapital();
    page.addCapital();
    const request = http.expectOne(`${base}/1/contributions`);
    expect(request.request.body).toEqual({ amount: 25, description: 'Deposit' });
    request.flush({ ...transaction, amount: 25 });
    expect(page.loading()).toBe(true);
    expect(page.transactions()).toEqual([]);
    const refreshed = { ...account, currentBalance: 1025 };
    http.expectOne(base).flush([refreshed]);
    details(refreshed, [transaction]);
    expect(page.details()?.currentBalance).toBe(1025);
    expect(page.totals()[0].balance).toBe(1025);
    expect(page.transactions()).toHaveLength(1);
    expect(fixture.nativeElement.textContent).toContain(en.finance.capitalAdded);
    expect(Array.from(fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>)
      .some(button => button.textContent?.trim() === 'Refresh')).toBe(false);
  });
  it('sends an optional date as an ISO timestamp and preserves the draft on failure', () => {
    load();
    page.openDialog('contribution');
    page.contributionForm.patchValue({
      amount: 25,
      description: 'Deposit',
      occurredAt: '2026-09-25',
    });
    page.addCapital();
    const request = http.expectOne(`${base}/1/contributions`);
    expect(request.request.body.occurredAt).toBe(new Date('2026-09-25T00:00:00').toISOString());
    request.flush({}, { status: 500, statusText: 'Error' });
    expect(page.busy()).toBe(false);
    expect(page.dialog()).toBe('contribution');
    expect(page.contributionForm.controls.amount.value).toBe(25);
    expect(page.saveError()).toBe('finance.errors.uncertain');
  });
  it('renders signs and translated types without changing positive amounts', () => {
    http.expectOne(base).flush([account]);
    const outgoing = {
      ...transaction,
      id: 11,
      amount: 630,
      direction: TransactionDirection.Out,
      type: TransactionType.VehiclePurchase,
      vehicleId: 7,
    };
    details(account, [transaction, outgoing]);
    const rows = fixture.nativeElement.querySelectorAll(
      'tbody tr',
    ) as NodeListOf<HTMLTableRowElement>;
    expect(rows[0].cells[4].textContent).toContain('+');
    expect(rows[1].cells[4].textContent).toContain('−');
    expect(rows[1].textContent).toContain('Vehicle purchase');
    expect(rows[1].textContent).toContain('Vehicle #7');
    expect(page.transactions()[1].amount).toBe(630);
  });
  it('recovers from list and history errors without showing stale history', () => {
    http.expectOne(base).flush({}, { status: 503, statusText: 'Unavailable' });
    expect(page.error()).toBeTruthy();
    page.refreshAccounts();
    http.expectOne(base).flush([account]);
    http.expectOne(`${base}/1`).flush({ ...account, latestTransactions: [] });
    http.expectOne(`${base}/1/transactions`).flush({}, { status: 500, statusText: 'Error' });
    expect(page.detailError()).toBeTruthy();
    expect(page.transactions()).toEqual([]);
    page.selectAccount(1);
    details();
    expect(page.detailError()).toBe('');
    expect(fixture.nativeElement.textContent).toContain('No transactions in this account yet.');
  });
});
