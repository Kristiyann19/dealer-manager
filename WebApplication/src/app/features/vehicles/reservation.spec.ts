import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideTranslateService, TranslateService } from '@ngx-translate/core';
import en from '../../../assets/i18n/en.json';
import { ReserveVehicleComponent } from './reserve-vehicle.component';
import { CancelReservationComponent } from './cancel-reservation.component';
import { SellVehicleComponent } from './sell-vehicle.component';
import { VehicleReservation } from './reservation.models';
import { VehicleStatus } from './vehicle.models';
const reservation: VehicleReservation = {
  reservationId: 4, vehicleId: 1, customerName: 'Ivan Ivanov', customerPhone: null, customerEmail: null,
  depositAmount: 500, reservedAt: '2026-10-12T10:00:00Z', expiresAt: null, status: 0,
  vehicleStatus: VehicleStatus.Reserved, notes: null, cancellationNotes: null, createdAt: '2026-10-12T10:00:00Z',
  completedAt: null, cancelledAt: null, capitalAccountId: 2, depositTransactionId: 3, refundTransactionId: null,
};
describe('Vehicle reservation dialogs', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting(), provideTranslateService(), provideRouter([])] });
    http = TestBed.inject(HttpTestingController);
    const translate = TestBed.inject(TranslateService); translate.setTranslation('en', en); translate.use('en').subscribe();
  });
  afterEach(() => http.verify());
  it('validates deposit and customer, previews remaining price and prevents duplicate reservations', () => {
    const fixture = TestBed.createComponent(ReserveVehicleComponent);
    fixture.componentRef.setInput('vehicleId', 1); fixture.componentRef.setInput('vehicleName', 'BMW 320d'); fixture.componentRef.setInput('listingPrice', 12500); fixture.detectChanges();
    const c = fixture.componentInstance;
    http.expectOne('/api/capital-accounts').flush([{ id: 2, name: 'Cash', currency: 'EUR', isActive: true, currentBalance: 20000 }]);
    expect(c.form.invalid).toBe(true);
    c.form.patchValue({ customerName: '   ', depositAmount: 500 }); expect(c.form.invalid).toBe(true);
    c.form.patchValue({ customerName: 'Ivan Ivanov', depositAmount: 12501 }); expect(c.form.invalid).toBe(true);
    c.form.controls.depositAmount.setValue(500); expect(c.form.valid).toBe(true); expect(c.remaining()).toBe(12000);
    c.submit(); c.submit();
    const request = http.expectOne('/api/vehicles/1/reservations');
    expect(request.request.body).toEqual({ customerName: 'Ivan Ivanov', customerPhone: null, customerEmail: null, depositAmount: 500, capitalAccountId: 2, expiresAt: null, notes: null });
    request.flush({}, { status: 409, statusText: 'Conflict' }); expect(c.busy()).toBe(false); expect(c.form.controls.depositAmount.value).toBe(500);
    fixture.destroy();
  });
  it('requires an explicit refund decision and always uses the original account', () => {
    const fixture = TestBed.createComponent(CancelReservationComponent); fixture.componentRef.setInput('reservation', reservation); fixture.detectChanges();
    const c = fixture.componentInstance;
    http.expectOne('/api/capital-accounts/2').flush({ currentBalance: 20500 });
    expect(c.form.invalid).toBe(true); c.form.controls.refund.setValue(true); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('20,000');
    c.submit(); c.submit(); const request = http.expectOne('/api/vehicles/1/reservations/4/cancel');
    expect(request.request.body).toEqual({ refundDeposit: true, notes: null }); request.flush(reservation); fixture.destroy();
  });
  it('retains a deposit without a refund and prevents refunds with insufficient capital', () => {
    const fixture = TestBed.createComponent(CancelReservationComponent); fixture.componentRef.setInput('reservation', reservation); fixture.detectChanges();
    const c = fixture.componentInstance; http.expectOne('/api/capital-accounts/2').flush({ currentBalance: 100 });
    c.form.controls.refund.setValue(true); expect(c.insufficient()).toBe(true); c.submit(); http.expectNone('/api/vehicles/1/reservations/4/cancel');
    c.form.controls.refund.setValue(false); fixture.detectChanges(); expect(c.form.valid).toBe(true); expect(fixture.nativeElement.textContent).toContain('Capital remains unchanged');
    c.submit(); const request = http.expectOne('/api/vehicles/1/reservations/4/cancel'); expect(request.request.body.refundDeposit).toBe(false); request.flush(reservation); fixture.destroy();
  });
  it('uses full price for profit, subtracts deposit for payment and allows exact deposit coverage', () => {
    const fixture = TestBed.createComponent(SellVehicleComponent);
    fixture.componentRef.setInput('vehicleId', 1); fixture.componentRef.setInput('listingPrice', 12500); fixture.componentRef.setInput('totalInvested', 10000); fixture.componentRef.setInput('depositAmount', 500); fixture.detectChanges();
    const c = fixture.componentInstance;
    c.form.controls.actualSalePrice.setValue(12000); expect(c.profit()).toBe(2000); expect(c.remaining()).toBe(11500);
    c.form.controls.actualSalePrice.setValue(499); expect(c.form.invalid).toBe(true); c.submit(); http.expectNone('/api/vehicles/1/sale');
    c.form.controls.actualSalePrice.setValue(500); expect(c.form.valid).toBe(true); expect(c.remaining()).toBe(0);
    c.submit(); c.submit(); const request = http.expectOne('/api/vehicles/1/sale'); expect(request.request.body.actualSalePrice).toBe(500); request.flush({}); fixture.destroy();
  });
});
