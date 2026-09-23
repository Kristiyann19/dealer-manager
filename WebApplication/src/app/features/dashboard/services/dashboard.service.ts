import { Observable } from 'rxjs';
import { DashboardSummary } from '../models/dashboard.models';

// DI contract: replace its provider with DashboardApiService when the API is ready.
export abstract class DashboardService {
  abstract getDashboard(): Observable<DashboardSummary>;
}
