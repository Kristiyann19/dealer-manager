import { LocalizedDatePipe } from '../../../../shared/pipes/localized-format.pipe';
import { TranslatePipe } from '@ngx-translate/core';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';

import { toSignal } from '@angular/core/rxjs-interop';
import { catchError, map, of, startWith, Subject, switchMap } from 'rxjs';
import { LucideDynamicIcon } from '@lucide/angular';
import { DashboardService } from '../../services/dashboard.service';
import { DashboardSummary } from '../../models/dashboard.models';
import { WorkspaceService } from '../../../../core/services/workspace.service';
import { MonthlyOverviewComponent } from '../../components/monthly-overview/monthly-overview.component';
import { FinancialOverviewComponent } from '../../components/financial-overview/financial-overview.component';
import { OperationalPipelineComponent } from '../../components/operational-pipeline/operational-pipeline.component';
import { RecentCandidatesComponent } from '../../components/recent-candidates/recent-candidates.component';
import { ActiveVehiclesComponent } from '../../components/active-vehicles/active-vehicles.component';
import { FinancialChartComponent } from '../../components/financial-chart/financial-chart.component';

type DashboardState =
  { status: 'loading' } | { status: 'error' } | { status: 'ready'; data: DashboardSummary };

@Component({
  selector: 'app-dashboard',
  imports: [
    TranslatePipe,
    LocalizedDatePipe,
    LucideDynamicIcon,
    MonthlyOverviewComponent,
    FinancialOverviewComponent,
    OperationalPipelineComponent,
    RecentCandidatesComponent,
    ActiveVehiclesComponent,
    FinancialChartComponent,
  ],
  templateUrl: './dashboard.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardComponent {
  private readonly service = inject(DashboardService);
  protected readonly workspace = inject(WorkspaceService);
  protected readonly reload = new Subject<void>();
  protected readonly state = toSignal(
    this.reload.pipe(
      startWith(undefined),
      switchMap(() =>
        this.service.getDashboard().pipe(
          map((data): DashboardState => ({ status: 'ready', data })),
          startWith<DashboardState>({ status: 'loading' }),
          catchError(() => of<DashboardState>({ status: 'error' })),
        ),
      ),
    ),
    { initialValue: { status: 'loading' } as DashboardState },
  );
}
