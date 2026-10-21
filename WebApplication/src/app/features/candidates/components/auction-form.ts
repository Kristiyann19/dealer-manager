import { AbstractControl, FormBuilder, Validators } from '@angular/forms';
import { CandidateAuctionDetails } from '../models/candidate.models';
import { requiredText, optionalText } from './candidate-form-utils';

function httpUrl(control: AbstractControl) {
  if (!control.value?.trim()) return null;
  try {
    return ['http:', 'https:'].includes(new URL(control.value.trim()).protocol)
      ? null
      : { url: true };
  } catch {
    return { url: true };
  }
}
function money(control: AbstractControl) {
  const value = control.value;
  return value === null ||
    (Number.isFinite(value) && Math.abs(value * 100 - Math.round(value * 100)) < 0.00001)
    ? null
    : { money: true };
}
export function createAuctionForm(fb: FormBuilder) {
  return fb.group({
    platform: fb.nonNullable.control('', [requiredText, Validators.maxLength(120)]),
    lotNumber: fb.nonNullable.control('', [requiredText, Validators.maxLength(100)]),
    listingUrl: fb.nonNullable.control('', [httpUrl, Validators.maxLength(2000)]),
    country: fb.nonNullable.control('', Validators.maxLength(100)),
    endsAt: fb.nonNullable.control(''),
    observedBid: fb.control<number | null>(null, [
      Validators.min(0),
      Validators.max(9999999999999),
      money,
    ]),
    maxBid: fb.control<number | null>(null, [
      Validators.min(0.01),
      Validators.max(9999999999999),
      money,
    ]),
  });
}
export type AuctionForm = ReturnType<typeof createAuctionForm>;
export function auctionRequest(form: AuctionForm): CandidateAuctionDetails {
  const value = form.getRawValue();
  return {
    ...value,
    platform: value.platform.trim(),
    lotNumber: value.lotNumber.trim(),
    listingUrl: optionalText(value.listingUrl),
    country: optionalText(value.country),
    endsAt: value.endsAt ? new Date(value.endsAt).toISOString() : null,
  };
}
export function fillAuctionForm(form: AuctionForm, value: CandidateAuctionDetails) {
  // datetime-local displays the operator's local time; the API receives a UTC instant.
  const date = value.endsAt ? new Date(value.endsAt) : null;
  const local = date
    ? new Date(date.getTime() - date.getTimezoneOffset() * 60000).toISOString().slice(0, 16)
    : '';
  form.reset({
    ...value,
    listingUrl: value.listingUrl ?? '',
    country: value.country ?? '',
    endsAt: local,
  });
}
