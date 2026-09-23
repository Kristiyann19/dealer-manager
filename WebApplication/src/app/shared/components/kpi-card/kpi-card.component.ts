import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { LucideDynamicIcon } from '@lucide/angular';
import { MoneyDisplayComponent } from '../money-display/money-display.component';
import { Tone } from '../../models/ui.models';

@Component({
  selector: 'app-kpi-card',
  imports: [LucideDynamicIcon, MoneyDisplayComponent],
  templateUrl: './kpi-card.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class KpiCardComponent {
  readonly label = input.required<string>();
  readonly value = input.required<number>();
  readonly icon = input.required<string>();
  readonly money = input(false);
  readonly tone = input<Tone>('slate');
  readonly context = input('');
  readonly contextTone = input<Tone>('slate');
}
