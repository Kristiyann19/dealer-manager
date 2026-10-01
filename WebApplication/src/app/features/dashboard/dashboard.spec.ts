import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideTranslateService, TranslateService } from '@ngx-translate/core';
import en from '../../../assets/i18n/en.json';
import { DashboardComponent } from './pages/dashboard/dashboard.component';
import { DashboardService } from './services/dashboard.service';
import { DashboardSummary } from './models/dashboard.models';
import { CandidateStatus } from '../candidates/models/candidate.models';
import { VehicleStatus } from '../vehicles/vehicle.models';
import { RecentCandidatesComponent } from './components/recent-candidates/recent-candidates.component';
import { ActiveVehiclesComponent } from './components/active-vehicles/active-vehicles.component';

const empty: DashboardSummary = {
  asOf: '2026-10-01T00:00:00Z',
  monthly: { carsInStock: 0, carsInRepair: 0, soldThisMonth: 0, profitThisMonth: 0 },
  financial: {
    availableCash: 0,
    capitalInvested: 0,
    upcomingProjectedCosts: 0,
    netWorthAtCost: 0,
    currency: 'EUR',
  },
  pipeline: { candidates: 0, transporting: 0, repairing: 0, readyForSale: 0, listed: 0 },
  candidates: [],
  activeVehicles: [],
  monthlyFinancials: [],
};

describe('Dashboard integration', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideTranslateService(),
        provideRouter([]),
      ],
    });
    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('en', en);
    translate.use('en').subscribe();
  });
  afterEach(() => TestBed.inject(HttpTestingController).verify());

  it('requests one aggregate endpoint with no cache on subsequent visits', () => {
    const service = TestBed.inject(DashboardService);
    const http = TestBed.inject(HttpTestingController);
    let value: DashboardSummary | undefined;
    service.getDashboard().subscribe((v) => (value = v));
    const first = http.expectOne('/api/dashboard');
    expect(first.request.method).toBe('GET');
    first.flush(empty);
    // A second visit must make a new request.
    service.getDashboard().subscribe((v) => (value = v));
    http
      .expectOne('/api/dashboard')
      .flush({ ...empty, monthly: { ...empty.monthly, profitThisMonth: -169 } });
    expect(value?.monthly.profitThisMonth).toBe(-169);
  });

  it('starts loading, shows failure, retries and exposes the real response', () => {
    const fixture = TestBed.createComponent(DashboardComponent);
    const page = fixture.componentInstance as unknown as {
      state: () => { status: string; data?: DashboardSummary };
      reload: { next: () => void };
    };
    const http = TestBed.inject(HttpTestingController);
    expect(page.state().status).toBe('loading');
    http.expectOne('/api/dashboard').flush({}, { status: 500, statusText: 'Failed' });
    expect(page.state().status).toBe('error');
    page.reload.next();
    expect(page.state().status).toBe('loading');
    http.expectOne('/api/dashboard').flush(empty);
    expect(page.state()).toEqual({ status: 'ready', data: empty });
    fixture.destroy();
    const nextVisit = TestBed.createComponent(DashboardComponent);
    http.expectOne('/api/dashboard').flush(empty);
    nextVisit.destroy();
  });

  it('renders absent estimates distinctly from zero and links to candidate details', () => {
    const fixture = TestBed.createComponent(RecentCandidatesComponent);
    fixture.componentRef.setInput('candidates', [
      {
        id: 42,
        make: 'BMW',
        model: '320d',
        year: null,
        mileage: null,
        status: CandidateStatus.UnderReview,
        createdAt: empty.asOf,
        estimatedTotalCost: null,
        expectedSellingPrice: null,
        expectedProfit: null,
        expectedRoi: null,
      },
    ]);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No estimate');
    expect(fixture.nativeElement.querySelector('a[href="/candidates/42"]')).not.toBeNull();
    fixture.componentRef.setInput('candidates', []);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No active candidates');
  });

  it('shows actual investment, missing listings and a real vehicle detail link', () => {
    const fixture = TestBed.createComponent(ActiveVehiclesComponent);
    fixture.componentRef.setInput('vehicles', [
      {
        id: 17,
        make: 'BMW',
        model: '320d',
        year: 2020,
        status: VehicleStatus.Repairing,
        totalInvested: 7000,
        remainingProjectedCosts: 500,
        projectedFinalCost: 7500,
        expectedSellingPrice: 7000,
        listingPrice: null,
        projectedProfit: -500,
      },
    ]);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No active listing');
    expect(fixture.nativeElement.querySelector('a[href="/vehicles/17"]')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('.text-red-700').textContent).toContain('500');
  });
});
