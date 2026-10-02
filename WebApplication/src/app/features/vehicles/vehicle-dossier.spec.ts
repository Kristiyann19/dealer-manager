import { TestBed } from '@angular/core/testing';
import { registerLocaleData } from '@angular/common';
import bgLocale from '@angular/common/locales/bg';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { provideTranslateService, TranslateService } from '@ngx-translate/core';
import en from '../../../assets/i18n/en.json';
import bg from '../../../assets/i18n/bg.json';
import { VehicleDossierEditComponent } from './vehicle-dossier-edit.component';
import { VehicleDossierComponent } from './vehicle-dossier.component';
import { VehicleDetails, VehicleStatus } from './vehicle.models';
import { CandidateCreateComponent } from '../candidates/pages/candidate-create.component';

registerLocaleData(bgLocale);
const details = {
  id: 5,
  make: 'BMW',
  model: '320d',
  vin: 'ORIGINAL',
  registrationNumber: 'PB1234AB',
  firstRegistration: '2018-06-01',
  mileage: 140000,
  status: VehicleStatus.Sold,
  color: null,
  notes: 'Archived details',
} as VehicleDetails;
describe('Vehicle dossier', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        provideTranslateService(),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap({ id: '5' }) } },
        },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('en', en);
    translate.setTranslation('bg', bg);
    translate.use('en').subscribe();
  });
  afterEach(() => http.verify());
  it('shows only VIN and mileage in one compact row and hides registration fields', () => {
    const fixture = TestBed.createComponent(VehicleDossierComponent);
    fixture.componentRef.setInput('vehicle', details);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('dt').length).toBe(2);
    expect(fixture.nativeElement.querySelector('dl').classList.contains('grid-cols-2')).toBe(true);
    expect(fixture.nativeElement.textContent).not.toContain(en.vehicle.dossier.registrationNumber);
    expect(fixture.nativeElement.textContent).not.toContain(en.vehicle.dossier.firstRegistration);
    expect(fixture.nativeElement.textContent).toContain(en.vehicle.dossier.title);
    expect(fixture.nativeElement.querySelector('a').getAttribute('href')).toBe('/vehicles/5/edit');
    fixture.nativeElement.querySelector('button').click();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('dt').length).toBe(16);
    expect(fixture.nativeElement.textContent).toContain('Archived details');
    TestBed.inject(TranslateService).use('bg').subscribe();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain(bg.vehicle.dossier.title);
  });
  it('validates positive measurements, permits clearing optional data and prevents double submit', () => {
    const fixture = TestBed.createComponent(VehicleDossierEditComponent);
    const page = fixture.componentInstance;
    http.expectOne('/api/vehicles/5').flush(details);
    fixture.detectChanges();
    page.form.controls['powerHp'].setValue(-1);
    fixture.detectChanges();
    expect(page.form.invalid).toBe(true);
    expect(fixture.nativeElement.querySelector('button[type=submit]').disabled).toBe(true);
    page.save();
    http.expectNone('/api/vehicles/5/details');
    const power = fixture.nativeElement.querySelector('#powerHp');
    power.value = '136';
    power.dispatchEvent(new Event('input'));
    page.form.controls['vin'].setValue(' new-vin ');
    page.form.controls['color'].setValue('   ');
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    page.save();
    page.save();
    const request = http.expectOne('/api/vehicles/5/details');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body.vin).toBe('new-vin');
    expect(request.request.body.color).toBeNull();
    expect(request.request.body.powerHp).toBe(136);
    expect(request.request.body.registrationNumber).toBe('PB1234AB');
    expect(request.request.body.firstRegistration).toBe('2018-06-01');
    expect(fixture.nativeElement.querySelector('#registrationNumber')).toBeNull();
    expect(fixture.nativeElement.querySelector('#firstRegistration')).toBeNull();
    expect(request.request.body).not.toHaveProperty('status');
    expect(request.request.body).not.toHaveProperty('dealershipId');
    request.flush({ ...details, vin: 'NEW-VIN', powerHp: 136 });
    expect(navigate).toHaveBeenCalledWith(['/vehicles', 5], { state: { dossierSaved: true } });
    expect(page.busy()).toBe(false);
  });
  it('keeps entered values on server validation failure', () => {
    const fixture = TestBed.createComponent(VehicleDossierEditComponent);
    const page = fixture.componentInstance;
    http.expectOne('/api/vehicles/5').flush(details);
    page.form.controls['notes'].setValue('Keep me');
    page.save();
    http.expectOne('/api/vehicles/5/details').flush({}, { status: 400, statusText: 'Bad Request' });
    expect(page.form.controls['notes'].value).toBe('Keep me');
    expect(page.error()).not.toBe('');
    expect(page.busy()).toBe(false);
  });
  it('does not show an editable form for a foreign or missing vehicle', () => {
    const fixture = TestBed.createComponent(VehicleDossierEditComponent);
    http.expectOne('/api/vehicles/5').flush({}, { status: 404, statusText: 'Not Found' });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('form')).toBeNull();
    expect(fixture.componentInstance.error()).toBe('vehicle.errors.notFound');
  });
  it('includes normalized optional VIN in the compact candidate create request', () => {
    const fixture = TestBed.createComponent(CandidateCreateComponent);
    fixture.detectChanges();
    for (const [id, value] of Object.entries({
      make: 'BMW',
      model: '320d',
      vin: ' abc123 ',
      'asking-price': '3000',
    })) {
      const input = fixture.nativeElement.querySelector('#' + id);
      input.value = value;
      input.dispatchEvent(new Event('input'));
    }
    vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    fixture.nativeElement.querySelector('form').dispatchEvent(new Event('submit'));
    const request = http.expectOne('/api/candidates');
    expect(request.request.body.vin).toBe('ABC123');
    request.flush({ id: 1 });
  });
  it('uses translated enum selects, numeric request values and optional clearing', () => {
    const fixture = TestBed.createComponent(VehicleDossierEditComponent);
    http.expectOne('/api/vehicles/5').flush({ ...details, fuelType: 1 });
    fixture.detectChanges();
    const select: HTMLSelectElement = fixture.nativeElement.querySelector('#fuelType');
    expect(select.tagName).toBe('SELECT');
    expect(select.textContent).toContain('Diesel');
    expect(fixture.nativeElement.querySelectorAll('select').length).toBe(5);
    select.selectedIndex = 1;
    select.dispatchEvent(new Event('change'));
    expect(fixture.componentInstance.form.controls['fuelType'].value).toBe(0);
    TestBed.inject(TranslateService).use('bg').subscribe();
    fixture.detectChanges();
    expect(select.textContent).toContain('Бензин');
    vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    fixture.componentInstance.save();
    const request = http.expectOne('/api/vehicles/5/details');
    expect(request.request.body.fuelType).toBe(0);
    request.flush(details);
    fixture.componentInstance.form.controls['fuelType'].setValue(999);
    expect(fixture.componentInstance.form.invalid).toBe(true);
    fixture.componentInstance.form.controls['fuelType'].setValue(null);
    expect(fixture.componentInstance.form.valid).toBe(true);
  });
});
