import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideTranslateService, TranslateService } from '@ngx-translate/core';
import { registerLocaleData } from '@angular/common';
import bgLocale from '@angular/common/locales/bg';
import en from '../../../../../assets/i18n/en.json';
import bg from '../../../../../assets/i18n/bg.json';
import { AttentionComponent } from './attention.component';
import {
  DashboardAttentionItem,
  DashboardAttentionType as Type,
  DashboardAttentionSeverity as Severity,
} from '../../models/dashboard.models';

registerLocaleData(bgLocale);
const now = '2026-10-01T12:00:00Z';
const item = (
  type: Type,
  severity: Severity,
  vehicleId: number | null,
  candidateId: number | null,
  days: number,
): DashboardAttentionItem => ({
  type,
  severity,
  vehicleId,
  candidateId,
  days,
  make: 'BMW',
  model: 'X3',
  referenceDate: '2026-09-30T12:00:00Z',
  depositAmount: type === Type.ReservationExpired ? 500 : null,
});
describe('Dashboard attention', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideRouter([]), provideTranslateService()] });
    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('en', en);
    translate.setTranslation('bg', bg);
    translate.use('en').subscribe();
  });
  function create(items: DashboardAttentionItem[] = [], count = items.length) {
    const fixture = TestBed.createComponent(AttentionComponent);
    fixture.componentRef.setInput('items', items);
    fixture.componentRef.setInput('count', count);
    fixture.componentRef.setInput('asOf', now);
    fixture.detectChanges();
    return fixture;
  }
  it('shows the positive empty state without a table or navigation', () => {
    const fixture = create();
    expect(fixture.nativeElement.textContent).toContain(en.dashboard.attention.emptyTitle);
    expect(fixture.nativeElement.querySelector('table, a')).toBeNull();
    TestBed.inject(TranslateService).use('bg').subscribe();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain(bg.dashboard.attention.emptyTitle);
  });
  it('preserves backend priority, links to the correct resource and shows the total count', () => {
    const fixture = create(
      [
        item(Type.ReservationExpired, Severity.Critical, 10, null, 1),
        item(Type.RepairTakingTooLong, Severity.Warning, 20, null, 17),
        item(Type.CandidateWaiting, Severity.Info, null, 30, 8),
        item(Type.CandidateWaiting, Severity.Info, null, 31, 7),
        item(Type.CandidateWaiting, Severity.Info, null, 32, 6),
      ],
      8,
    );
    const rows = fixture.nativeElement.querySelectorAll('li a');
    expect([...rows].slice(0, 3).map((row: any) => row.getAttribute('href'))).toEqual([
      '/vehicles/10',
      '/vehicles/20',
      '/candidates/30',
    ]);
    expect(rows[0].textContent).toContain(en.dashboard.attention.types[0]);
    expect(rows[0].textContent).toContain('500');
    expect(rows[1].textContent).toContain('17 days');
    expect(fixture.nativeElement.textContent).toContain('Showing 5 of 8');
    expect(fixture.nativeElement.querySelector('.bg-red-500')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('.bg-orange-500')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('.bg-blue-500')).not.toBeNull();
  });
  it.each([
    ['2026-10-01T18:00:00Z', 'Expires today'],
    ['2026-10-02T12:00:00Z', 'Expires tomorrow'],
    ['2026-10-03T12:00:00Z', 'Expires in 48 hours'],
  ])('renders expiry %s relative to the server snapshot', (referenceDate, expected) => {
    const fixture = create([
      { ...item(Type.ReservationExpiringSoon, Severity.Warning, 1, null, 0), referenceDate },
    ]);
    expect(fixture.nativeElement.textContent).toContain(expected);
  });
});
