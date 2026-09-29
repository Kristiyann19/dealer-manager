import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { BehaviorSubject, catchError, forkJoin, map, of, switchMap } from 'rxjs';
import { TranslatePipe } from '@ngx-translate/core';
import {
  LocalizedCurrencyPipe,
  LocalizedDatePipe,
  LocalizedNumberPipe,
} from '../../shared/pipes/localized-format.pipe';
import { VehicleApiService, VehicleDetails } from './vehicle-api.service';
import { VehicleCostPlanItem, VehicleExpense, VehicleFinancialSummary } from './vehicle.models';
import { EditCostPlanComponent } from './edit-cost-plan.component';
import { AddExpenseComponent } from './add-expense.component';
import { ConfirmPaymentComponent } from './confirm-payment.component';

@Component({
  selector: 'app-vehicle-details',
  imports: [
    RouterLink,
    TranslatePipe,
    LocalizedCurrencyPipe,
    LocalizedDatePipe,
    LocalizedNumberPipe,
    EditCostPlanComponent,
    AddExpenseComponent,
    ConfirmPaymentComponent,
  ],
  templateUrl: './vehicle-details.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VehicleDetailsComponent {
  private readonly api = inject(VehicleApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly reload = new BehaviorSubject<void>(undefined);
  readonly purchased = !!inject(Router).currentNavigation()?.extras.state?.['purchased'];
  readonly vehicle = signal<VehicleDetails | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly notice = signal('');
  readonly plan = signal<VehicleCostPlanItem[]>([]);
  readonly pendingPlan = computed(() => this.plan().filter((item) => item.remainingProjected > 0));
  readonly expenses = signal<VehicleExpense[]>([]);
  readonly editing = signal<VehicleCostPlanItem | null>(null);
  readonly expenseDialog = signal<{ plan: VehicleCostPlanItem | null } | null>(null);
  readonly metrics: { key: keyof VehicleFinancialSummary; label: string }[] = [
    { key: 'totalInvested', label: 'vehicle.totalInvested' },
    { key: 'remainingProjectedCosts', label: 'vehicle.remainingProjectedCosts' },
    { key: 'projectedFinalCost', label: 'vehicle.projectedFinalCost' },
    { key: 'expectedSellingPrice', label: 'vehicle.expectedSellingPrice' },
    { key: 'projectedProfit', label: 'vehicle.projectedProfit' },
  ];
  readonly statusKeys = [
    'status.Purchased',
    'status.Transporting',
    'status.Arrived',
    'status.Inspecting',
    'status.Repairing',
    'status.Preparing',
    'status.Ready for sale',
    'status.Listed',
    'status.Reserved',
    'status.Sold',
  ];
  constructor() {
    this.route.paramMap
      .pipe(
        switchMap((params) =>
          this.reload.pipe(
            switchMap(() => {
              this.loading.set(true);
              this.error.set('');
              this.vehicle.set(null);
              this.editing.set(null);
              this.expenseDialog.set(null);
              this.plan.set([]);
              this.expenses.set([]);
              const id = Number(params.get('id'));
              if (!Number.isInteger(id) || id < 1 || id > 2147483647)
                return of({ data: null, error: 'errors.invalidId' });
              return forkJoin({
                vehicle: this.api.details(id),
                plan: this.api.costPlan(id),
                expenses: this.api.expenses(id),
              }).pipe(
                map((data) => ({ data, error: '' })),
                catchError((error: unknown) =>
                  of({
                    data: null,
                    error:
                      error instanceof HttpErrorResponse && error.status === 404
                        ? 'purchase.vehicleNotFound'
                        : 'finance.errors.unavailable',
                  }),
                ),
              );
            }),
          ),
        ),
        takeUntilDestroyed(),
      )
      .subscribe((result) => {
        this.vehicle.set(result.data?.vehicle ?? null);
        this.plan.set(result.data?.plan ?? []);
        this.expenses.set(result.data?.expenses ?? []);
        this.error.set(result.error);
        this.loading.set(false);
      });
  }
  refresh() {
    this.reload.next();
  }
  openExpense(plan: VehicleCostPlanItem | null = null) {
    if (this.vehicle()?.status === 9) return;
    this.expenseDialog.set({ plan });
  }
  saved(kind: 'plan' | 'expense') {
    this.notice.set(kind === 'plan' ? 'vehicle.planSaved' : 'vehicle.expenseSaved');
    this.refresh();
  }
}
