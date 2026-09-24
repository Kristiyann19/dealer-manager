import { TranslatePipe } from '@ngx-translate/core';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { AbstractControl } from '@angular/forms';

@Component({
  imports: [TranslatePipe],
  selector: 'app-field-error',
  template: `@if (control().invalid && control().touched) {
    <small class="field-error" role="alert">{{ message() || ('form.invalid' | translate) }}</small>
  }`,
  // FormControl changes its touched/errors state without changing the input reference.
  changeDetection: ChangeDetectionStrategy.Default,
})
export class FieldErrorComponent {
  readonly control = input.required<AbstractControl>();
  readonly message = input('');
}
