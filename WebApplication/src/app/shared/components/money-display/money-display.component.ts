import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CurrencyPipe } from '@angular/common';

@Component({
  selector: 'app-money-display',
  imports: [CurrencyPipe],
  template: `<span class="tabular-nums whitespace-nowrap">{{
    amount() | currency: 'EUR' : 'symbol' : '1.0-0'
  }}</span>`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MoneyDisplayComponent {
  readonly amount = input.required<number>();
}
