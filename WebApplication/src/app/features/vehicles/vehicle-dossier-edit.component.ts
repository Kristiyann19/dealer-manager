import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { TranslatePipe } from '@ngx-translate/core';
import { VehicleApiService } from './vehicle-api.service';
import { DOSSIER_GROUPS, VehicleDossier } from './vehicle-dossier.models';
import { validOptionalDate, vehicleError } from './vehicle-form-utils';
import { integer } from '../candidates/components/candidate-form-utils';
@Component({
  imports: [ReactiveFormsModule, RouterLink, TranslatePipe],
  templateUrl: './vehicle-dossier-edit.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VehicleDossierEditComponent {
  private readonly api = inject(VehicleApiService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  readonly id = Number(inject(ActivatedRoute).snapshot.paramMap.get('id'));
  readonly groups = DOSSIER_GROUPS;
  private retainedRegistration: Pick<VehicleDossier, 'registrationNumber' | 'firstRegistration'> = {
    registrationNumber: null,
    firstRegistration: null,
  };
  readonly loading = signal(true);
  readonly busy = signal(false);
  readonly error = signal('');
  readonly loaded = signal(false);
  readonly form = new FormGroup(
    Object.fromEntries(
      this.groups
        .flatMap((g) => g.fields)
        .map((f) => [
          f.key,
          new FormControl<string | number | null>(
            null,
            f.type === 'select'
              ? [
                  (control) =>
                    control.value == null || f.options!.some((o) => o.value === control.value)
                      ? null
                      : { enum: true },
                ]
              : f.type === 'number'
                ? [integer, Validators.min(f.min!), Validators.max(f.max!)]
                : f.type === 'date'
                  ? [validOptionalDate]
                  : [Validators.maxLength(f.maxLength!)],
          ),
        ]),
    ),
  );
  constructor() {
    if (!Number.isInteger(this.id) || this.id <= 0) {
      this.error.set('errors.invalidId');
      this.loading.set(false);
      return;
    }
    this.api
      .details(this.id)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.loading.set(false)),
      )
      .subscribe({
        next: (vehicle) => {
          this.retainedRegistration = {
            registrationNumber: vehicle.registrationNumber ?? null,
            firstRegistration: vehicle.firstRegistration ?? null,
          };
          for (const field of this.groups.flatMap((g) => g.fields))
            this.form.controls[field.key].setValue(vehicle[field.key] ?? null);
          this.loaded.set(true);
        },
        error: (error) => this.error.set(vehicleError(error)),
      });
  }
  save() {
    if (this.busy() || !this.loaded()) return;
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    this.busy.set(true);
    this.error.set('');
    const request = {
      ...this.retainedRegistration,
      ...Object.fromEntries(
        Object.entries(this.form.getRawValue()).map(([key, value]) => [
          key,
          typeof value === 'string' ? value.trim() || null : value,
        ]),
      ),
    } as unknown as VehicleDossier;
    this.api
      .updateDossier(this.id, request)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.busy.set(false)),
      )
      .subscribe({
        next: () =>
          void this.router.navigate(['/vehicles', this.id], { state: { dossierSaved: true } }),
        error: (error) => this.error.set(vehicleError(error, true)),
      });
  }
}
