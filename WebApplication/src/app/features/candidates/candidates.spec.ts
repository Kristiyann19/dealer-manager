import { HttpErrorResponse, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { BehaviorSubject } from 'rxjs';
import { CandidateCreateComponent } from './pages/candidate-create.component';
import { CandidateDetailsComponent } from './pages/candidate-details.component';
import { CandidateListComponent } from './pages/candidate-list.component';
import { EstimateFormComponent } from './components/estimate-form.component';
import {
  CandidateDetails,
  CandidateEstimate,
  CandidateStatus,
  CostCategory,
} from './models/candidate.models';
import { apiError } from './components/candidate-form-utils';
import { provideTranslateService, TranslateService } from '@ngx-translate/core';
import en from '../../../assets/i18n/en.json';
import bg from '../../../assets/i18n/bg.json';
import { registerLocaleData } from '@angular/common';
import bgLocale from '@angular/common/locales/bg';

registerLocaleData(bgLocale);

const estimate: CandidateEstimate = {
  id: 7,
  candidateId: 12,
  version: 1,
  expectedSellingPrice: 7000,
  notes: 'Original estimate',
  createdAt: '2026-09-23T10:00:00Z',
  isDecisionSnapshot: false,
  items: [
    { id: 9, category: CostCategory.Purchase, description: 'Purchase', estimatedAmount: 3000 },
  ],
  financialAnalysis: { estimatedTotalCost: 3000, expectedProfit: 4000, expectedRoi: 133.33 },
};
const candidate: CandidateDetails = {
  id: 12,
  make: 'BMW',
  model: '320d',
  year: null,
  mileage: 0,
  status: CandidateStatus.UnderReview,
  expectedSellingPrice: 7000,
  createdAt: '2026-09-23T10:00:00Z',
  estimatedTotalCost: null,
  expectedProfit: null,
  expectedRoi: null,
  vin: null,
  source: null,
  location: null,
  notes: null,
  rejectedAt: null,
  purchasedAt: null,
  estimateHistory: [],
  latestEstimate: null,
};
function input(fixture: ComponentFixture<unknown>, selector: string, value: string) {
  const element: HTMLInputElement = fixture.nativeElement.querySelector(selector);
  element.value = value;
  element.dispatchEvent(new Event('input'));
  fixture.detectChanges();
}
function button(fixture: ComponentFixture<unknown>, text: string): HTMLButtonElement {
  return Array.from(
    fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>,
  ).find((element) => element.textContent?.trim() === text)!;
}
function submit(fixture: ComponentFixture<unknown>) {
  fixture.nativeElement
    .querySelector('form')
    .dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
  fixture.detectChanges();
}

describe('Candidate module HTTP workflows', () => {
  let http: HttpTestingController;
  let params: BehaviorSubject<ReturnType<typeof convertToParamMap>>;
  beforeEach(() => {
    params = new BehaviorSubject(convertToParamMap({ id: '12' }));
    TestBed.configureTestingModule({
      providers: [
        provideTranslateService(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
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

  it('does not submit an empty form or whitespace-only make', () => {
    const fixture = TestBed.createComponent(CandidateCreateComponent);
    fixture.detectChanges();
    submit(fixture);
    expect(fixture.nativeElement.textContent).toContain('Make is required.');
    input(fixture, '#make', '   ');
    input(fixture, '#model', '320d');
    input(fixture, '#selling-price', '0');
    submit(fixture);
    http.expectNone('/api/candidates');
  });
  it('sends integer/nullable fields, preserves zero, prevents duplicate submissions and navigates to the saved candidate', () => {
    const fixture = TestBed.createComponent(CandidateCreateComponent);
    fixture.detectChanges();
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    input(fixture, '#make', ' BMW ');
    input(fixture, '#model', '320d');
    input(fixture, '#selling-price', '0');
    input(fixture, '#mileage', '0');
    submit(fixture);
    submit(fixture);
    const request = http.expectOne('/api/candidates');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      make: 'BMW',
      model: '320d',
      year: null,
      mileage: 0,
      expectedSellingPrice: 0,
      vin: null,
      source: null,
      location: null,
      notes: null,
    });
    request.flush(candidate);
    expect(navigate).toHaveBeenCalledWith(['/candidates', 12]);
  });
  it('keeps the create draft and displays server validation errors', () => {
    const fixture = TestBed.createComponent(CandidateCreateComponent);
    fixture.detectChanges();
    input(fixture, '#make', 'BMW');
    input(fixture, '#model', '320d');
    input(fixture, '#selling-price', '7000');
    submit(fixture);
    http
      .expectOne('/api/candidates')
      .flush(
        { errors: { Year: ['Year is out of range.'] } },
        { status: 400, statusText: 'Bad Request' },
      );
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain(en.errors.validation);
    expect(fixture.nativeElement.querySelector('#make').value).toBe('BMW');
    expect(button(fixture, 'Create candidate').disabled).toBe(false);
  });
  it('rejects fractional years and negative mileage before sending', () => {
    const fixture = TestBed.createComponent(CandidateCreateComponent);
    fixture.detectChanges();
    input(fixture, '#make', 'BMW');
    input(fixture, '#model', '320d');
    input(fixture, '#selling-price', '7000');
    input(fixture, '#year', '2020.5');
    input(fixture, '#mileage', '-1');
    submit(fixture);
    http.expectNone('/api/candidates');
  });
  it('saves estimates with numeric categories and without editable versions or snapshot flags', () => {
    const fixture = TestBed.createComponent(EstimateFormComponent);
    fixture.componentRef.setInput('candidate', { ...candidate, latestEstimate: estimate });
    fixture.detectChanges();
    const saved = vi.fn();
    fixture.componentInstance.saved.subscribe(saved);
    button(fixture, 'Replace draft items with latest estimate').click();
    fixture.detectChanges();
    input(fixture, '#amount-0', '3100.25');
    submit(fixture);
    submit(fixture);
    const request = http.expectOne('/api/candidates/estimates');
    expect(request.request.body).toEqual({
      candidateId: 12,
      expectedSellingPrice: 7000,
      notes: null,
      items: [{ category: 0, description: 'Purchase', estimatedAmount: 3100.25 }],
    });
    expect(estimate.items[0].estimatedAmount).toBe(3000);
    request.flush({ ...estimate, version: 2 });
    expect(saved).toHaveBeenCalledOnce();
  });
  it('requires at least one valid estimate item and permits a zero amount', () => {
    const fixture = TestBed.createComponent(EstimateFormComponent);
    fixture.componentRef.setInput('candidate', candidate);
    fixture.detectChanges();
    submit(fixture);
    http.expectNone('/api/candidates/estimates');
    expect(button(fixture, 'Remove').disabled).toBe(true);
    input(fixture, '#description-0', 'No charge');
    input(fixture, '#amount-0', '0');
    submit(fixture);
    const request = http.expectOne('/api/candidates/estimates');
    expect(request.request.body.items[0].estimatedAmount).toBe(0);
    request.flush(estimate);
  });
  it('requires an estimate before approval', () => {
    const fixture = TestBed.createComponent(CandidateDetailsComponent);
    fixture.detectChanges();
    http.expectOne('/api/candidates/12').flush(candidate);
    fixture.detectChanges();
    expect(button(fixture, 'Approve candidate').disabled).toBe(true);
  });
  it('confirms approval through the API and uses the returned status', () => {
    const fixture = TestBed.createComponent(CandidateDetailsComponent);
    fixture.detectChanges();
    http
      .expectOne('/api/candidates/12')
      .flush({ ...candidate, latestEstimate: estimate, estimateHistory: [estimate] });
    fixture.detectChanges();
    button(fixture, 'Approve candidate').click();
    fixture.detectChanges();
    button(fixture, 'Confirm approval').click();
    fixture.detectChanges();
    const request = http.expectOne('/api/candidates/12/approve');
    expect(request.request.method).toBe('POST');
    request.flush({
      ...candidate,
      status: CandidateStatus.Approved,
      latestEstimate: estimate,
      estimateHistory: [estimate],
    });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Candidate approved.');
    expect(button(fixture, 'Approve candidate')).toBeUndefined();
  });
  it('retains a rejected candidate and removes evaluation actions after saving the reason', () => {
    const fixture = TestBed.createComponent(CandidateDetailsComponent);
    fixture.detectChanges();
    http.expectOne('/api/candidates/12').flush(candidate);
    fixture.detectChanges();
    button(fixture, 'Reject').click();
    fixture.detectChanges();
    input(fixture, '#rejection-reason', ' Too expensive ');
    button(fixture, 'Confirm rejection').click();
    const request = http.expectOne('/api/candidates/12/reject');
    expect(request.request.body).toEqual({ reason: 'Too expensive' });
    request.flush({
      ...candidate,
      status: CandidateStatus.Rejected,
      rejectedAt: candidate.createdAt,
      notes: 'Rejection reason: Too expensive',
    });
    fixture.detectChanges();
    expect(button(fixture, '+ New estimate')).toBeUndefined();
    expect(button(fixture, 'Reject')).toBeUndefined();
    expect(fixture.nativeElement.textContent).toContain('Too expensive');
  });
  it('shows a conflict instead of optimistically changing status', () => {
    const fixture = TestBed.createComponent(CandidateDetailsComponent);
    fixture.detectChanges();
    http.expectOne('/api/candidates/12').flush(candidate);
    fixture.detectChanges();
    button(fixture, 'Reject').click();
    fixture.detectChanges();
    button(fixture, 'Confirm rejection').click();
    http
      .expectOne('/api/candidates/12/reject')
      .flush({ detail: 'Candidate was purchased.' }, { status: 409, statusText: 'Conflict' });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain(
      'Refresh the candidate before trying again.',
    );
    expect(fixture.nativeElement.textContent).toContain('Under Review');
  });
  it('handles missing candidates and route ID changes', () => {
    const fixture = TestBed.createComponent(CandidateDetailsComponent);
    fixture.detectChanges();
    http.expectOne('/api/candidates/12').flush({}, { status: 404, statusText: 'Not Found' });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('could not be found');
    params.next(convertToParamMap({ id: '13' }));
    http.expectOne('/api/candidates/13').flush({ ...candidate, id: 13 });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('CANDIDATE #13');
  });
  it('paginates on the server and distinguishes missing estimates from zero', () => {
    const fixture = TestBed.createComponent(CandidateListComponent);
    fixture.detectChanges();
    const request = http.expectOne((req) => req.url === '/api/candidates');
    expect(request.request.params.get('Limit')).toBe('15');
    expect(request.request.params.get('Offset')).toBe('0');
    request.flush({
      items: [
        candidate,
        { ...candidate, id: 13, estimatedTotalCost: 0, expectedProfit: 0, expectedRoi: 0 },
      ],
      totalCount: 16,
    });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No estimate');
    expect(fixture.nativeElement.textContent).toContain('€0.00');
    button(fixture, 'Next').click();
    const page = http.expectOne((req) => req.url === '/api/candidates');
    expect(page.request.params.get('Offset')).toBe('15');
    page.flush({ items: [{ ...candidate, id: 14 }], totalCount: 16 });
    fixture.detectChanges();
    expect(button(fixture, 'Next').disabled).toBe(true);
  });
  it('searches remotely after debounce and cancels an obsolete request', async () => {
    const fixture = TestBed.createComponent(CandidateListComponent);
    fixture.detectChanges();
    const original = http.expectOne((req) => req.url === '/api/candidates');
    input(fixture, 'input[type="search"]', 'BMW');
    await new Promise((resolve) => setTimeout(resolve, 400));
    expect(original.cancelled).toBe(true);
    const request = http.expectOne((req) => req.params.get('TextFilter') === 'BMW');
    expect(request.request.params.get('Offset')).toBe('0');
    request.flush({ items: [], totalCount: 0 });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No matching candidates');
  });
  it('does not present failed loads as an empty list', () => {
    const fixture = TestBed.createComponent(CandidateListComponent);
    fixture.detectChanges();
    http
      .expectOne((req) => req.url === '/api/candidates')
      .flush({}, { status: 503, statusText: 'Unavailable' });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Cannot reach the server');
    expect(fixture.nativeElement.textContent).not.toContain('No candidates yet');
  });
  it('warns about uncertain write outcomes rather than automatically retrying a POST', () => {
    expect(apiError(new HttpErrorResponse({ status: 0 }), true)).toBe('errors.uncertainWrite');
  });
  it('switches labels and existing validation errors without losing a candidate draft', () => {
    const fixture = TestBed.createComponent(CandidateCreateComponent);
    fixture.detectChanges();
    input(fixture, '#model', '320d');
    submit(fixture);
    const translate = TestBed.inject(TranslateService);
    translate.use('bg').subscribe();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain(bg.ui.make_is_required);
    expect(button(fixture, bg.candidate.create)).toBeDefined();
    expect(fixture.nativeElement.querySelector('#model').value).toBe('320d');
    translate.use('en').subscribe();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain(en.ui.make_is_required);
    expect(fixture.nativeElement.querySelector('#model').value).toBe('320d');
    http.expectNone('/api/candidates');
  });
  it('translates a displayed API error when the language changes', () => {
    const fixture = TestBed.createComponent(CandidateListComponent);
    fixture.detectChanges();
    http
      .expectOne((req) => req.url === '/api/candidates')
      .flush({}, { status: 503, statusText: 'Unavailable' });
    TestBed.inject(TranslateService).use('bg').subscribe();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain(bg.errors.unavailable);
  });
});
