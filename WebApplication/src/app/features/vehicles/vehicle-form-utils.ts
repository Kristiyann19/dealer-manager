import { HttpErrorResponse } from '@angular/common/http';
import { ValidatorFn } from '@angular/forms';

export const nonnegativeAmount: ValidatorFn = (control) =>
  control.value === null ||
  (typeof control.value === 'number' && Number.isFinite(control.value) && control.value >= 0)
    ? null
    : { amount: true };
export const positiveAmount: ValidatorFn = (control) =>
  typeof control.value === 'number' && Number.isFinite(control.value) && control.value > 0
    ? null
    : { amount: true };
export const requiredText: ValidatorFn = (control) =>
  typeof control.value === 'string' && control.value.trim() ? null : { required: true };
export const validOptionalDate: ValidatorFn = (control) =>
  !control.value || Number.isFinite(new Date(control.value + 'T00:00:00').getTime())
    ? null
    : { date: true };
export function vehicleError(error: unknown, writing = false): string {
  if (!(error instanceof HttpErrorResponse) || error.status === 0 || error.status >= 500)
    return writing ? 'vehicle.errors.uncertain' : 'finance.errors.unavailable';
  if (error.status === 404) return 'vehicle.errors.notFound';
  if (error.status === 409) {
    const body: unknown = error.error;
    const code = body && typeof body === 'object' && 'code' in body ? body.code : null;
    if (
      typeof code === 'string' &&
      [
        'planMismatch',
        'sold',
        'inactive',
        'currency',
        'insufficientCapital',
        'purchaseAccountMissing',
        'purchaseAccountMismatch',
        'confirmationRequired',
        'paymentChanged',
        'cancelledPlan',
        'invalidPlanAmount',
        'alreadyPaid',
        'partialPayment',
        'statusLocked',
        'operationalStatusOnly',
        'sameStatus',
      ].includes(code)
    )
      return 'vehicle.errors.' + code;
    return 'vehicle.errors.conflict';
  }
  return 'finance.errors.validation';
}
