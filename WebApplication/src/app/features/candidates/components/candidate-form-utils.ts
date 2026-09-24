import { HttpErrorResponse } from '@angular/common/http';
import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export const requiredText: ValidatorFn = (control: AbstractControl): ValidationErrors | null =>
  typeof control.value === 'string' && control.value.trim() ? null : { required: true };
export const integer: ValidatorFn = (control: AbstractControl): ValidationErrors | null =>
  control.value == null || Number.isInteger(control.value) ? null : { integer: true };
export const optionalText = (value: string) => value.trim() || null;

export function apiError(error: unknown, writing = false): string {
  if (!(error instanceof HttpErrorResponse)) return 'errors.unexpected';
  if (error.status === 0 || error.status >= 500) {
    return writing ? 'errors.uncertainWrite' : 'errors.unavailable';
  }
  if (error.status === 404) return 'errors.notFound';
  if (error.status === 409) return 'errors.conflict';
  // The API currently sends English prose rather than stable localization codes.
  // Keep client messages language-neutral until the server exposes such codes.
  return 'errors.validation';
}
