import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { LocalizedNumberPipe } from '../pipes/localized-format.pipe';

@Component({
  selector: 'app-pagination',
  imports: [TranslatePipe, LocalizedNumberPipe],
  template: `
    <div class="flex flex-col items-end gap-2">
        <nav class="pagination-control" [attr.aria-label]="'common.pagination' | translate">
          <button type="button" [attr.aria-label]="'ui.previous' | translate" [disabled]="offset() === 0" (click)="select(offset() / pageSize())"><span aria-hidden="true">&lt;</span></button>
          @for (number of pages(); track $index) {
            @if (number === null) {
              <span class="pagination-gap" aria-hidden="true">…</span>
            } @else {
              <button type="button" [class.is-current]="number === offset() / pageSize() + 1"
                [attr.aria-current]="number === offset() / pageSize() + 1 ? 'page' : null"
                [attr.aria-label]="'common.pageNumber' | translate: { number: number }"
                (click)="select(number)">{{ number | localizedNumber }}</button>
            }
          }
          <button type="button" [attr.aria-label]="'ui.next' | translate" [disabled]="offset() + pageSize() >= total()" (click)="select(offset() / pageSize() + 2)"><span aria-hidden="true">&gt;</span></button>
        </nav>
      <span class="text-xs text-slate-500" role="status"><ng-content /></span>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PaginationComponent {
  readonly offset = input.required<number>();
  readonly pageSize = input.required<number>();
  readonly total = input.required<number>();
  readonly pageChange = output<number>();
  protected readonly pages = computed(() => {
    const count = Math.max(1, Math.ceil(this.total() / this.pageSize()));
    const current = this.offset() / this.pageSize() + 1;
    const visible = new Set([1, count]);
    const start = Math.max(1, Math.min(current - 1, count - 2));
    if (count <= 7) {
      for (let page = 1; page <= count; page++) visible.add(page);
    } else {
      for (let page = start; page <= Math.min(count, start + 2); page++) visible.add(page);
    }
    const result: (number | null)[] = [];
    let previous = 0;
    for (const page of [...visible].sort((a, b) => a - b)) {
      if (page - previous > 1) result.push(null);
      result.push(page);
      previous = page;
    }
    return result;
  });
  protected select(page: number) {
    const offset = (page - 1) * this.pageSize();
    if (offset < 0 || offset >= this.total() || offset === this.offset()) return;
    this.pageChange.emit(page);
  }
}
