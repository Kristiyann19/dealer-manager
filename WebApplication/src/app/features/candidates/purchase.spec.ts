import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { of } from 'rxjs';
import { provideTranslateService, TranslateService } from '@ngx-translate/core';
import en from '../../../assets/i18n/en.json';
import { PurchaseDialogComponent } from './components/purchase-dialog.component';
import { CandidateDetailsComponent } from './pages/candidate-details.component';
import { CandidateDetails, CandidateStatus, CostCategory } from './models/candidate.models';
import { VehicleDetailsComponent } from '../vehicles/vehicle-details.component';

const candidate: CandidateDetails = {
  id: 1,
  vehicleId: null,
  make: 'BMW',
  model: '320d',
  year: 2018,
  mileage: null,
  status: CandidateStatus.Approved,
  askingPrice: 6500,
  expectedSellingPrice: 10000,
  createdAt: '2026-09-25T00:00:00Z',
  vin: null,
  source: null,
  location: null,
  notes: null,
  rejectedAt: null,
  purchasedAt: null,
  estimatedTotalCost: 8100,
  expectedProfit: 1900,
  expectedRoi: 23.46,
  estimateHistory: [],
  latestEstimate: {
    id: 2,
    candidateId: 1,
    version: 2,
    expectedSellingPrice: 10000,
    notes: null,
    createdAt: '2026-09-25T00:00:00Z',
    isDecisionSnapshot: false,
    items: [
      { id: 1, category: CostCategory.Purchase, description: 'Purchase', estimatedAmount: 6500 },
    ],
    financialAnalysis: { estimatedTotalCost: 8100, expectedProfit: 1900, expectedRoi: 23.46 },
  },
};
const account = { id: 1, name: 'Main', currency: 'EUR', isActive: true, currentBalance: 20000 };
const result = {
  candidateId: 1,
  vehicleId: 7,
  capitalAccountId: 1,
  actualPurchasePrice: 6300,
  purchaseDate: '2026-09-25T00:00:00Z',
  previousBalance: 20000,
  remainingBalance: 13700,
};
describe('Purchase frontend', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideTranslateService(),
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { paramMap: of(convertToParamMap({ id: '1' })) } },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('en', en);
    translate.use('en').subscribe();
  });
  afterEach(() => http.verify());
  function dialog(value = candidate) {
    const fixture = TestBed.createComponent(PurchaseDialogComponent);
    fixture.componentRef.setInput('candidate', value);
    fixture.detectChanges();
    http
      .expectOne('/api/capital-accounts')
      .flush([
        account,
        { ...account, id: 2, currency: 'USD' },
        { ...account, id: 3, isActive: false },
      ]);
    fixture.detectChanges();
    return fixture;
  }
  it('uses only active EUR accounts and previews the actual price without editing the forecast', () => {
    const fixture = dialog();
    const page = fixture.componentInstance;
    expect(page.accounts()).toHaveLength(1);
    expect(page.form.value.actualPurchasePrice).toBe(6500);
    page.form.patchValue({ actualPurchasePrice: 6300 });
    fixture.detectChanges();
    expect(page.remaining()).toBe(13700);
    expect(page.forecastPrice()).toBe(6500);
  });
  it('blocks overspending, zero prices and missing year before sending a request', () => {
    const fixture = dialog();
    const page = fixture.componentInstance;
    page.form.patchValue({ actualPurchasePrice: 25000 });
    fixture.detectChanges();
    page.submit();
    expect(page.insufficient()).toBe(true);
    expect(fixture.nativeElement.textContent).toContain('Insufficient available capital.');
    page.form.patchValue({ actualPurchasePrice: 0 });
    fixture.detectChanges();
    page.submit();
    page.form.patchValue({ actualPurchasePrice: 6300 });
    fixture.componentRef.setInput('candidate', { ...candidate, year: null });
    fixture.detectChanges();
    page.submit();
    http.expectNone('/api/candidates/1/purchase');
  });
  it('sends only purchase inputs, prevents duplicate submits and emits the server result', () => {
    const fixture = dialog();
    const page = fixture.componentInstance;
    const saved = vi.fn();
    page.purchased.subscribe(saved);
    page.form.patchValue({ actualPurchasePrice: 6300 });
    fixture.detectChanges();
    page.submit();
    page.submit();
    const request = http.expectOne('/api/candidates/1/purchase');
    expect(request.request.body).toEqual({ capitalAccountId: 1, actualPurchasePrice: 6300 });
    request.flush(result);
    expect(saved).toHaveBeenCalledWith(result);
  });
  it('keeps the dialog and price on a backend capital conflict', () => {
    const fixture = dialog();
    const page = fixture.componentInstance;
    page.submit();
    http
      .expectOne('/api/candidates/1/purchase')
      .flush({ code: 'insufficientCapital' }, { status: 409, statusText: 'Conflict' });
    fixture.detectChanges();
    expect(page.error()).toBe('purchase.errors.insufficientCapital');
    expect(page.busy()).toBe(false);
    expect(page.form.value.actualPurchasePrice).toBe(6500);
  });
  it.each([CandidateStatus.UnderReview, CandidateStatus.Rejected, CandidateStatus.Purchased])(
    'hides purchase for status %s',
    (status) => {
      const fixture = TestBed.createComponent(CandidateDetailsComponent);
      http
        .expectOne('/api/candidates/1')
        .flush({
          ...candidate,
          status,
          vehicleId: status === CandidateStatus.Purchased ? 7 : null,
        });
      fixture.detectChanges();
      expect(fixture.nativeElement.textContent).not.toContain('Confirm purchase');
      if (status === CandidateStatus.Purchased)
        expect(fixture.nativeElement.querySelector('a[href="/vehicles/7"]')).toBeTruthy();
    },
  );
  it('opens the approved candidate purchase dialog and navigates to the created vehicle', () => {
    const fixture = TestBed.createComponent(CandidateDetailsComponent);
    http.expectOne('/api/candidates/1').flush(candidate);
    fixture.detectChanges();
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    const buttons = Array.from(
      fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>,
    );
    buttons.find((b) => b.textContent?.trim() === 'Confirm purchase')!.click();
    fixture.detectChanges();
    http.expectOne('/api/capital-accounts').flush([account]);
    fixture.detectChanges();
    fixture.nativeElement
      .querySelector('form')
      .dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    fixture.detectChanges();
    http.expectOne('/api/candidates/1/purchase').flush(result);
    fixture.detectChanges();
    expect(navigate).toHaveBeenCalledWith(['/vehicles', 7], { state: { purchased: true } });
  });
  it('loads vehicle details from API on a direct visit', () => {
    const fixture = TestBed.createComponent(VehicleDetailsComponent);
    http
      .expectOne('/api/vehicles/1')
      .flush({
        id: 1,
        make: 'BMW',
        model: '320d',
        year: 2018,
        status: 0,
        sourceCandidateId: 1,
        purchaseDate: result.purchaseDate,
        actualPurchasePrice: 6300,
      });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('BMW 320d');
    expect(fixture.nativeElement.querySelector('a[href="/candidates/1"]')).toBeTruthy();
  });
});
