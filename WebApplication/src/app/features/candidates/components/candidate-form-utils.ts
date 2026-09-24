import { HttpErrorResponse } from '@angular/common/http';
import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export const requiredText: ValidatorFn = (control: AbstractControl): ValidationErrors | null =>
  typeof control.value === 'string' && control.value.trim() ? null : { required: true };
export const integer: ValidatorFn = (control: AbstractControl): ValidationErrors | null =>
  control.value == null || Number.isInteger(control.value) ? null : { integer: true };
export const optionalText = (value: string) => value.trim() || null;

export function apiError(error: unknown, writing = false): string {
  if (!(error instanceof HttpErrorResponse)) return 'Something went wrong. Please try again.';
  if (error.status === 0 || error.status >= 500) {
    return writing
      ? 'The server did not confirm the result. Check the candidate before submitting again to avoid creating a duplicate.'
      : 'Cannot reach the server. Check that the WebAPI and database are running, then try again.';
  }
  if (error.status === 404)
    return 'This candidate could not be found. It may no longer be available.';
  if (error.status === 409)
    return `${error.error?.detail || 'The candidate has changed.'} Refresh the candidate before trying again.`;
  const errors: unknown = error.error?.errors;
  if (errors && typeof errors === 'object') {
    const messages = Object.values(errors)
      .flat()
      .filter((value): value is string => typeof value === 'string');
    if (messages.length) return messages.join(' ');
  }
  return (
    error.error?.detail || 'The request could not be completed. Check the values and try again.'
  );
}
