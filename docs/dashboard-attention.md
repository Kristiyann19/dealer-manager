# Dashboard: needs attention

Replaces the active-vehicles UI beside the unchanged monthly financial chart. Other dashboard data and existing API fields remain compatible.

## Contract

GET /api/dashboard adds `attentionCount` (all matching situations) and `attentionItems` (maximum five). Each response-only DTO carries numeric `type` and `severity`, vehicle/candidate ID, make/model, reference date, elapsed whole days, and optional deposit amount. Angular translates titles/details from the type; the API returns neither localized UI text nor routes.

Severity: Critical=0, Warning=1, Info=2. Types in numeric order:

| Type | Severity | Rule |
| --- | --- | --- |
| ReservationExpired (0) | Critical | Active reservation, expiry strictly before now |
| ReservationExpiringSoon (1) | Warning | Active reservation, expiry from now through now + 48 hours inclusive |
| CandidateWaiting (2) | Info | UnderReview for at least 5 days |
| RepairTakingTooLong (3) | Warning | Current Repairing entry at least 14 days ago |
| TransportTakingTooLong (4) | Warning | Current Transporting entry at least 10 days ago |
| ReadyNotListed (5) | Info | Current ReadyForSale entry at least 3 days ago, no active listing |
| ReservedWithoutReservation (6) | Critical | Reserved, no active reservation |
| ListedWithoutListing (7) | Critical | Listed, no active listing |

Sold vehicles never participate. Expired-but-active reservations still count as active for consistency checks; they produce the expiry alert only. No stale-stock alerts are duplicated here.

## Time and queries

`TimeProvider.GetUtcNow()` is captured once in DashboardService and passed to the query helper, also returned as `asOf`. Duration thresholds use exact elapsed 24-hour days. The UI uses that snapshot (not browser time) for expiry details, with UTC calendar boundaries for today/tomorrow.

Three tenant-filtered, read-only queries retrieve reservation, candidate and relevant vehicle scalars. The vehicle projection uses correlated EXISTS and a single latest matching status transition (ChangedAt DESC, Id DESC, ignoring same-status records). Complete histories are not loaded; there are no per-vehicle queries. Missing history does not fall back to CreatedAt. Only defensive inconsistency items may use `asOf` as their reference when history is absent.

Sort by severity ascending (Critical first), reference date ascending, type, vehicle ID, candidate ID. Older expiry/longer waits and nearer upcoming expiry sort first within their severity. Count before taking five. Nothing is saved, no jobs/entities/tables/migrations.

## UI

Compact clickable rows with severity dots, neutral total badge, deposit amount for reservation alerts and translated detail. Links are built client-side to /vehicles/{id} or /candidates/{id}. Empty state says All clear / Всичко е наред. A truncated list says Showing 5 of N. Responsive dashboard columns and financial chart are preserved. Global search continues to filter aged vehicles; attention counts are never misleadingly filtered against only five results.

## Verification

Backend integration tests cover thresholds and boundaries, inactive/no-expiry reservations, excluded candidate statuses, sold cars, repeated status entries, missing history, active listings, consistency alerts, deterministic priority/top-five counts and tenant isolation with a fixed clock. Bounded query count remains constant (25 total dashboard SELECTs).

Frontend tests cover empty state and language changes, row order/navigation, deposit and count, severity dots and today/tomorrow/48-hour details relative to the server snapshot.
