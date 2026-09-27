import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { BehaviorSubject, catchError, map, of, switchMap } from 'rxjs';
import { TranslatePipe } from '@ngx-translate/core';
import { LocalizedCurrencyPipe, LocalizedDatePipe } from '../../shared/pipes/localized-format.pipe';
import { VehicleApiService, VehicleDetails } from './vehicle-api.service';

@Component({
  selector: 'app-vehicle-details',
  imports: [RouterLink, TranslatePipe, LocalizedCurrencyPipe, LocalizedDatePipe],
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
              const id = Number(params.get('id'));
              if (!Number.isInteger(id) || id < 1 || id > 2147483647)
                return of({ data: null, error: 'errors.invalidId' });
              return this.api.details(id).pipe(
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
        this.vehicle.set(result.data);
        this.error.set(result.error);
        this.loading.set(false);
      });
  }
  refresh() {
    this.reload.next();
  }
}
