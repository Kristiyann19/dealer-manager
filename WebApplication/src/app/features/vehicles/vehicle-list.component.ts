import { PaginationComponent } from '../../shared/components/pagination.component';
import { SellVehicleComponent } from './sell-vehicle.component';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import {
  BehaviorSubject,
  catchError,
  debounceTime,
  distinctUntilChanged,
  map,
  of,
  startWith,
  switchMap,
} from 'rxjs';
import { TranslatePipe } from '@ngx-translate/core';
import { LocalizedCurrencyPipe } from '../../shared/pipes/localized-format.pipe';
import { VehicleApiService } from './vehicle-api.service';
import {
  VEHICLE_STATUSES,
  VehicleListResult,
  VehicleStatus,
  VehicleListItem,
} from './vehicle.models';
import { VehicleStatusComponent } from './vehicle-status.component';
import { vehicleError } from './vehicle-form-utils';
type ListState =
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'ready'; data: VehicleListResult };
@Component({
  selector: 'app-vehicle-list',
  imports: [
    SellVehicleComponent,
    ToastModule,
    PaginationComponent,
    TranslatePipe,
    ReactiveFormsModule,
    RouterLink,
    LocalizedCurrencyPipe,
    VehicleStatusComponent,
  ],
  templateUrl: './vehicle-list.component.html',
  providers: [MessageService],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VehicleListComponent {
  private readonly messages = inject(MessageService);
  readonly saleVehicle = signal<VehicleListItem | null>(null);
  readonly listedStatus = VehicleStatus.Listed;
  saleSaved() {
    this.saleVehicle.set(null);
    const current = this.state();
    if (this.statusFilter.value === VehicleStatus.Listed && current.status === 'ready' && current.data.items.length === 1 && this.offset() > 0) {
      this.offset.update(value => Math.max(0, value - this.limit));
    }
    this.messages.clear();
    this.messages.add({ severity: 'success', detail: 'vehicle.sale.success', life: 5000 });
    this.retry();
  }
  private readonly api = inject(VehicleApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  private readonly reload = new BehaviorSubject<void>(undefined);
  readonly search = new FormControl('', { nonNullable: true });
  readonly statusFilter = new FormControl<VehicleStatus | null>(this.routeStatus(this.route.snapshot.queryParamMap.get('status')));
  readonly statuses = VEHICLE_STATUSES;
  readonly query = signal('');
  readonly offset = signal(0);
  readonly limit = 15;
  readonly columns = [
    { key: 'totalInvested', label: 'vehicle.totalInvested' },
    { key: 'expectedSellingPrice', label: 'vehicle.sale.inventoryPrice' },
    { key: 'projectedProfit', label: 'vehicle.sale.inventoryProfit' },
  ] as const;
  readonly state = toSignal(
    this.reload.pipe(
      switchMap(() =>
        this.api.list(this.query(), this.statusFilter.value, this.offset(), this.limit).pipe(
          map((data) => ({ status: 'ready', data }) as ListState),
          catchError((error) => of<ListState>({ status: 'error', message: vehicleError(error) })),
          startWith<ListState>({ status: 'loading' }),
        ),
      ),
    ),
    { initialValue: { status: 'loading' } as ListState },
  );
  constructor() {
    this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(params => {
      const status = this.routeStatus(params.get('status'));
      if (status !== this.statusFilter.value) {
        this.statusFilter.setValue(status, { emitEvent: false });
        this.offset.set(0);
        this.reload.next();
      }
    });
    this.search.valueChanges
      .pipe(
        map((value) => value.trim()),
        debounceTime(300),
        distinctUntilChanged(),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((value) => {
        this.query.set(value);
        this.offset.set(0);
        this.reload.next();
      });
    this.statusFilter.valueChanges
      .pipe(distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.offset.set(0);
        this.reload.next();
      });
  }
  amount(
    vehicle: VehicleListItem,
    key: 'totalInvested' | 'expectedSellingPrice' | 'projectedProfit',
  ) {
    if (vehicle.status === VehicleStatus.Sold) {
      if (key === 'expectedSellingPrice') return vehicle.actualSalePrice;
      if (key === 'projectedProfit') return vehicle.realizedProfit;
    }
    return vehicle[key];
  }
  private routeStatus(value: string | null): VehicleStatus | null {
    if (value === null || value.trim() === '') return null;
    const status = Number(value);
    return VEHICLE_STATUSES.some(option => option.value === status) ? status : null;
  }
  retry() {
    this.reload.next();
  }
  page(direction: number) {
    this.offset.update((value) => Math.max(0, value + direction * this.limit));
    this.reload.next();
  }
  goToPage(number: number) {
    this.offset.set((number - 1) * this.limit);
    this.reload.next();
  }
  end(total: number) {
    return Math.min(this.offset() + this.limit, total);
  }
}
