import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { AuctionForm } from './auction-form';
import { FieldErrorComponent } from './field-error.component';

@Component({
  selector: 'app-auction-fields',
  imports: [ReactiveFormsModule, TranslatePipe, FieldErrorComponent],
  templateUrl: './auction-fields.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AuctionFieldsComponent {
  readonly form = input.required<AuctionForm>();
}
