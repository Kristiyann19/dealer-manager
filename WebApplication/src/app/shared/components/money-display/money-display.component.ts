import { LocalizedCurrencyPipe } from '../../pipes/localized-format.pipe';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-money-display',
  imports: [LocalizedCurrencyPipe],
  template: `<span class="tabular-nums whitespace-nowrap">{{
    amount() | localizedCurrency: 'EUR' : 'symbol' : '1.0-0'
  }}</span>`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MoneyDisplayComponent {
  readonly amount = input.required<number>();
}
