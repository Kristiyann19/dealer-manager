import { VehicleDossierComponent } from './vehicle-dossier.component';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { BehaviorSubject, catchError, forkJoin, map, of, switchMap } from 'rxjs';
import { TranslatePipe } from '@ngx-translate/core';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import {
  LocalizedCurrencyPipe,
  LocalizedDatePipe,
  LocalizedNumberPipe,
} from '../../shared/pipes/localized-format.pipe';
import { VehicleApiService, VehicleDetails } from './vehicle-api.service';
import {
  VehicleCostPlanItem,
  VehicleExpense,
  VehicleFinancialSummary,
  VehiclePaymentAccount,
  VehicleStatusHistory,
  VehicleStatus,
} from './vehicle.models';
import { EditCostPlanComponent } from './edit-cost-plan.component';
import { AddExpenseComponent } from './add-expense.component';
import { VehicleStatusComponent } from './vehicle-status.component';
import { ChangeStatusComponent } from './change-status.component';
import { SellVehicleComponent } from './sell-vehicle.component';
import { ListVehicleComponent } from './list-vehicle.component';
import { ConfirmPaymentComponent } from './confirm-payment.component';

@Component({
  selector: 'app-vehicle-details',
  imports: [
    VehicleDossierComponent,
    TranslatePipe,
    ToastModule,
    RouterLink,
    VehicleStatusComponent,
    ChangeStatusComponent,
    ListVehicleComponent,
    SellVehicleComponent,
    LocalizedCurrencyPipe,
    LocalizedDatePipe,
    LocalizedNumberPipe,
    EditCostPlanComponent,
    AddExpenseComponent,
    ConfirmPaymentComponent,
  ],
  templateUrl: './vehicle-details.component.html',
  providers: [MessageService],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VehicleDetailsComponent {
  private readonly api = inject(VehicleApiService);
  private readonly messages = inject(MessageService);
  private readonly route = inject(ActivatedRoute);
  private readonly reload = new BehaviorSubject<void>(undefined);
  readonly purchased = !!inject(Router).currentNavigation()?.extras.state?.['purchased'];
  readonly vehicle = signal<VehicleDetails | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly notice = signal(
    inject(Router).currentNavigation()?.extras.state?.['dossierSaved']
      ? 'vehicle.dossier.saved'
      : '',
  );
  readonly plan = signal<VehicleCostPlanItem[]>([]);
  readonly pendingPlan = computed(() => this.plan().filter((item) => item.remainingProjected > 0));
  readonly expenses = signal<VehicleExpense[]>([]);
  readonly paymentAccount = signal<VehiclePaymentAccount | null>(null);
  readonly editing = signal<VehicleCostPlanItem | null>(null);
  readonly expenseDialog = signal<{ plan: VehicleCostPlanItem | null } | null>(null);
  readonly statusHistory = signal<VehicleStatusHistory[]>([]);
  readonly changingStatus = signal(false);
  readonly listingDialog = signal(false);
  readonly saleDialog = signal(false);
  readonly canSell = computed(() => this.vehicle()?.status === VehicleStatus.Listed);
  readonly canList = computed(() => this.vehicle()?.status === VehicleStatus.ReadyForSale);
  readonly canChangeStatus = computed(
    () => this.vehicle() !== null && this.vehicle()!.status < VehicleStatus.Listed,
  );
  readonly metrics: { key: keyof VehicleFinancialSummary; label: string }[] = [
    { key: 'totalInvested', label: 'vehicle.totalInvested' },
    { key: 'remainingProjectedCosts', label: 'vehicle.remainingProjectedCosts' },
    { key: 'projectedROI', label: 'vehicle.projectedROI' },
    { key: 'expectedSellingPrice', label: 'vehicle.expectedSellingPrice' },
    { key: 'projectedProfit', label: 'vehicle.projectedProfit' },
  ];
  readonly soldMetrics: { key: keyof VehicleFinancialSummary; label: string }[] = [
    { key: 'totalInvested', label: 'vehicle.totalInvested' },
    { key: 'actualSalePrice', label: 'vehicle.sale.price' },
    { key: 'realizedProfit', label: 'vehicle.sale.profit' },
    { key: 'realizedROI', label: 'vehicle.sale.roi' },
  ];
  readonly displayMetrics = computed(() =>
    this.vehicle()?.status === VehicleStatus.Sold ? this.soldMetrics : this.metrics,
  );
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
              this.statusHistory.set([]);
              this.changingStatus.set(false);
              this.listingDialog.set(false);
              this.saleDialog.set(false);
              this.expenses.set([]);
              this.paymentAccount.set(null);
              const id = Number(params.get('id'));
              if (!Number.isInteger(id) || id < 1 || id > 2147483647)
                return of({ data: null, error: 'errors.invalidId' });
              return forkJoin({
                vehicle: this.api.details(id),
                plan: this.api.costPlan(id),
                expenses: this.api.expenses(id),
                history: this.api.statusHistory(id),
                paymentAccount: this.api.paymentAccount(id).pipe(catchError(() => of(null))),
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
        this.statusHistory.set(result.data?.history ?? []);
        this.expenses.set(result.data?.expenses ?? []);
        this.paymentAccount.set(result.data?.paymentAccount ?? null);
        this.error.set(result.error);
        this.loading.set(false);
      });
  }
  refreshFinancialData() {
    this.reload.next();
  }
  openExpense(plan: VehicleCostPlanItem | null = null) {
    if (this.vehicle()?.status === 9) return;
    this.expenseDialog.set({ plan });
  }
  saved(kind: 'plan' | 'expense' | 'status' | 'listing' | 'sale') {
    if (kind === 'listing' || kind === 'sale') {
      this.messages.add({
        severity: 'success',
        detail: kind === 'sale' ? 'vehicle.sale.success' : 'vehicle.listing.success',
        life: 5000,
      });
    }
    this.notice.set(
      kind === 'sale'
        ? 'vehicle.sale.success'
        : kind === 'listing'
          ? 'vehicle.listing.success'
          : kind === 'status'
            ? 'vehicle.statusSaved'
            : kind === 'plan'
              ? 'vehicle.planSaved'
              : 'vehicle.expenseSaved',
    );
    this.refreshFinancialData();
  }
  paymentClosed(needsReconciliation: boolean) {
    this.expenseDialog.set(null);
    if (needsReconciliation) this.refreshFinancialData();
  }
}
