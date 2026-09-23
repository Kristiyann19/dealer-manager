import { Injectable } from '@angular/core';
import { defer, delay, Observable, of } from 'rxjs';
import { DashboardSummary } from '../models/dashboard.models';
import { DashboardService } from './dashboard.service';
import { MOCK_DASHBOARD } from './mock-dashboard.data';

@Injectable()
export class MockDashboardService extends DashboardService {
  override getDashboard(): Observable<DashboardSummary> {
    // Fresh response per subscription, just like a future HttpClient request.
    return defer(() => of(structuredClone(MOCK_DASHBOARD))).pipe(delay(250));
  }
}
