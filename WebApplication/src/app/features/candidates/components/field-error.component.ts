import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { AbstractControl } from '@angular/forms';

@Component({
  selector: 'app-field-error',
  template: `@if (control().invalid && control().touched) {
    <small class="field-error" role="alert">{{ message() }}</small>
  }`,
  // FormControl changes its touched/errors state without changing the input reference.
  changeDetection: ChangeDetectionStrategy.Default,
})
export class FieldErrorComponent {
  readonly control = input.required<AbstractControl>();
  readonly message = input('Enter a valid value.');
}
