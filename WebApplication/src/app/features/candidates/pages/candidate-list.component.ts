import {
  LocalizedDatePipe,
  LocalizedNumberPipe,
  LocalizedCurrencyPipe,
} from '../../../shared/pipes/localized-format.pipe';
import { TranslatePipe } from '@ngx-translate/core';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';

import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
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
import { CandidateApiService } from '../services/candidate-api.service';
import { CandidateListResult } from '../models/candidate.models';
import { CandidateStatusComponent } from '../components/candidate-status.component';
import { apiError } from '../components/candidate-form-utils';

type ListState =
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'ready'; data: CandidateListResult };
@Component({
  selector: 'app-candidate-list',
  imports: [
    TranslatePipe,
    RouterLink,
    ReactiveFormsModule,
    LocalizedCurrencyPipe,
    LocalizedDatePipe,
    LocalizedNumberPipe,
    CandidateStatusComponent,
  ],
  templateUrl: './candidate-list.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CandidateListComponent {
  private readonly api = inject(CandidateApiService);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly search = new FormControl('', { nonNullable: true });
  protected readonly query = signal('');
  protected readonly offset = signal(0);
  protected readonly limit = 15;
  private readonly reload = new BehaviorSubject<void>(undefined);
  protected readonly state = toSignal(
    this.reload.pipe(
      switchMap(() =>
        this.api.list(this.query(), this.offset(), this.limit).pipe(
          map((data) => ({ status: 'ready', data }) as ListState),
          catchError((error) => of<ListState>({ status: 'error', message: apiError(error) })),
          startWith<ListState>({ status: 'loading' }),
        ),
      ),
    ),
    { initialValue: { status: 'loading' } as ListState },
  );

  constructor() {
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
        this.refresh();
      });
  }
  protected refresh() {
    this.reload.next();
  }
  protected page(direction: number) {
    this.offset.update((value) => Math.max(0, value + direction * this.limit));
    this.refresh();
  }
  protected end(total: number) {
    return Math.min(this.offset() + this.limit, total);
  }
}
