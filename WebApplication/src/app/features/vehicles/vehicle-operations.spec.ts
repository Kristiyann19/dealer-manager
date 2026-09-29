import { provideHttpClient } from '@angular/common/http';
import { registerLocaleData } from '@angular/common';
import bgLocale from '@angular/common/locales/bg';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { provideTranslateService, TranslateService } from '@ngx-translate/core';
import en from '../../../assets/i18n/en.json';
import bg from '../../../assets/i18n/bg.json';
import { VehicleListComponent } from './vehicle-list.component';
import { VehicleDetailsComponent } from './vehicle-details.component';
import { ChangeStatusComponent } from './change-status.component';
import { VehicleDetails, VehicleStatus, VehicleStatusHistory } from './vehicle.models';

registerLocaleData(bgLocale);

const vehicle: VehicleDetails = {
  id: 1,
  make: 'BMW',
  model: '320d',
  year: 2018,
  mileage: 90000,
  vin: 'VIN',
  status: VehicleStatus.Purchased,
  sourceCandidateId: 1,
  purchaseDate: '2026-09-29T10:00:00Z',
  originalForecast: null,
  actualPurchasePrice: 5000,
  actualExpenses: 210,
  totalInvested: 5210,
  remainingProjectedCosts: 0,
  projectedFinalCost: 5210,
  expectedSellingPrice: 6000,
  projectedProfit: 790,
  projectedROI: 15.16,
};
const change: VehicleStatusHistory = {
  id: 1,
  fromStatus: VehicleStatus.Purchased,
  toStatus: VehicleStatus.Repairing,
  changedAt: '2026-09-29T12:00:00Z',
  notes: 'In workshop',
};

describe('Vehicle inventory and operational status', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideTranslateService(),
        provideRouter([
          { path: 'vehicles', component: VehicleListComponent },
          { path: 'vehicles/:id', component: VehicleDetailsComponent },
        ]),
      ],
    });
    http = TestBed.inject(HttpTestingController);
    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('en', en);
    translate.setTranslation('bg', bg);
    translate.use('en').subscribe();
  });
  afterEach(() => http.verify());
  function list(items = [vehicle], totalCount = items.length) {
    const request = http.expectOne((r) => r.url === '/api/vehicles');
    request.flush({ items, totalCount });
    return request;
  }
  function details(value = vehicle, history: VehicleStatusHistory[] = []) {
    http.expectOne('/api/vehicles/1').flush(value);
    http.expectOne('/api/vehicles/1/cost-plan').flush([]);
    http.expectOne('/api/vehicles/1/expenses').flush([]);
    http
      .expectOne('/api/vehicles/1/payment-account')
      .flush({ capitalAccountId: 1, currency: 'EUR', currentBalance: 5000 });
    http.expectOne('/api/vehicles/1/status-history').flush(history);
  }
  it('renders compact inventory with translated status, finances and detail links', () => {
    const fixture = TestBed.createComponent(VehicleListComponent);
    const request = list();
    fixture.detectChanges();
    expect(request.request.params.get('Limit')).toBe('15');
    expect(fixture.nativeElement.querySelectorAll('tbody tr')).toHaveLength(1);
    expect(fixture.nativeElement.querySelectorAll('tbody td')).toHaveLength(9);
    expect(fixture.nativeElement.textContent).toContain('€5,210.00');
    expect(fixture.nativeElement.textContent).toContain('Purchased');
    expect(fixture.nativeElement.querySelector('a[href="/vehicles/1"]')).toBeTruthy();
    expect(fixture.nativeElement.textContent).not.toContain('Refresh');
    TestBed.inject(TranslateService).use('bg').subscribe();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Закупен');
  });
  it('filters remotely, resets pagination and cancels stale requests', async () => {
    const fixture = TestBed.createComponent(VehicleListComponent);
    list([vehicle], 30);
    fixture.detectChanges();
    fixture.componentInstance.page(1);
    const stale = http.expectOne((r) => r.url === '/api/vehicles');
    expect(stale.request.params.get('Offset')).toBe('15');
    fixture.componentInstance.statusFilter.setValue(VehicleStatus.Purchased);
    expect(stale.cancelled).toBe(true);
    const filtered = list();
    expect(filtered.request.params.get('Status')).toBe('0');
    expect(filtered.request.params.get('Offset')).toBe('0');
    fixture.componentInstance.search.setValue('  BMW  ');
    await new Promise((resolve) => setTimeout(resolve, 400));
    const search = list();
    expect(search.request.params.get('TextFilter')).toBe('BMW');
    fixture.componentInstance.statusFilter.setValue(null);
    expect(list().request.params.has('Status')).toBe(false);
  });
  it('handles empty inventory, unknown financial values and retry after a read failure', () => {
    const fixture = TestBed.createComponent(VehicleListComponent);
    http
      .expectOne((r) => r.url === '/api/vehicles')
      .flush({}, { status: 503, statusText: 'Unavailable' });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain(en.finance.errors.unavailable);
    fixture.componentInstance.retry();
    list([]);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain(en.vehicle.emptyInventory);
    fixture.componentInstance.statusFilter.setValue(VehicleStatus.Preparing);
    list([
      {
        ...vehicle,
        expectedSellingPrice: null,
        projectedProfit: null,
        projectedROI: null,
        mileage: null,
      },
    ]);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain(en.common.notProvided);
  });
  it('offers only other operational statuses, requires a selection and prevents double submit', () => {
    const fixture = TestBed.createComponent(ChangeStatusComponent);
    fixture.componentRef.setInput('vehicleId', 1);
    fixture.componentRef.setInput('currentStatus', VehicleStatus.Purchased);
    fixture.detectChanges();
    expect(fixture.componentInstance.options().map((s) => s.value)).toEqual([1, 2, 3, 4, 5, 6]);
    expect(fixture.nativeElement.querySelector('button[type="submit"]').disabled).toBe(true);
    fixture.componentInstance.form.setValue({
      status: VehicleStatus.Repairing,
      notes: ' In workshop ',
    });
    fixture.componentInstance.submit();
    fixture.componentInstance.submit();
    const request = http.expectOne('/api/vehicles/1/status');
    expect(request.request.body).toEqual({ status: 4, notes: 'In workshop' });
    request.flush({ code: 'sameStatus' }, { status: 409, statusText: 'Conflict' });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain(en.vehicle.errors.sameStatus);
    expect(fixture.componentInstance.form.value.notes).toBe(' In workshop ');
  });
  it.each([VehicleStatus.Listed, VehicleStatus.Reserved, VehicleStatus.Sold])(
    'hides generic status action for status %s',
    async (status) => {
      const harness = await RouterTestingHarness.create('/vehicles/1');
      details({ ...vehicle, status });
      harness.detectChanges();
      const page = harness.routeDebugElement!.componentInstance as VehicleDetailsComponent;
      expect(page.canChangeStatus()).toBe(false);
      expect(
        Array.from(harness.routeNativeElement!.querySelectorAll('button')).some(
          (b) => b.textContent?.trim() === 'Change status',
        ),
      ).toBe(false);
    },
  );
  it('navigates inventory to details, changes status, reloads history and shows the updated inventory on return', async () => {
    const harness = await RouterTestingHarness.create('/vehicles');
    list();
    harness.detectChanges();
    expect(harness.routeNativeElement!.textContent).toContain('Purchased');
    const page = await harness.navigateByUrl('/vehicles/1', VehicleDetailsComponent);
    details();
    harness.detectChanges();
    page.changingStatus.set(true);
    harness.detectChanges();
    const modal = harness.routeDebugElement!.query(By.directive(ChangeStatusComponent))
      .componentInstance as ChangeStatusComponent;
    modal.form.setValue({ status: VehicleStatus.Repairing, notes: 'In workshop' });
    modal.submit();
    http.expectOne('/api/vehicles/1/status').flush(change);
    expect(page.loading()).toBe(true);
    expect(page.changingStatus()).toBe(false);
    details({ ...vehicle, status: VehicleStatus.Repairing }, [change]);
    harness.detectChanges();
    expect(harness.routeNativeElement!.querySelector('header')!.textContent).toContain('Repairing');
    expect(harness.routeNativeElement!.textContent).toContain(en.vehicle.statusSaved);
    const history = harness.routeNativeElement!.querySelector(
      '#status-history',
    ) as HTMLDetailsElement;
    expect(history.open).toBe(false);
    expect(history.textContent).toContain('Purchased');
    expect(history.textContent).toContain('Repairing');
    expect(history.textContent).toContain('In workshop');
    expect(harness.routeNativeElement!.querySelector('app-change-vehicle-status')).toBeNull();
    await harness.navigateByUrl('/vehicles', VehicleListComponent);
    list([{ ...vehicle, status: VehicleStatus.Repairing }]);
    harness.detectChanges();
    expect(harness.routeNativeElement!.querySelector('tbody')!.textContent).toContain('Repairing');
    expect(harness.routeNativeElement!.textContent).not.toContain('Refresh');
  });
});
