# Vehicle expense corrections

The payment history in vehicle details exposes Correct and Reverse for effective expenses, including sold vehicles. Reversed entries remain visible with reason, timestamp, actor ID, reversal transaction ID and optional replacement expense ID. The original amount and metadata are retained. Purchase transactions, contributions, deposits and sales are outside this feature.

## Endpoints

- POST /api/vehicles/{id}/expenses/{expenseId}/reverse — required reason (1–1000 characters after trimming).
- POST /api/vehicles/{id}/expenses/{expenseId}/correct — required reason, positive amount, category and description; optional supplier, document number and payment date. Account and plan cannot be moved; the category of a planned expense stays unchanged.

Both endpoints return the original expense DTO with the new audit properties. GET expenses includes effective and reversed records. No DELETE is used. The replacement can subsequently be corrected again, preserving the chain.

## Ledger and dates

Reverse creates a positive In/VehicleExpense transaction in the original account at TimeProvider.GetUtcNow(). Use for wrong/duplicate expenses or fully returned payments. It does not issue a bank refund: it records the money movement in the application's ledger. Partial refunds have no separate action in this version.

Correct atomically reverses the original payment and creates a new Out/VehicleExpense transaction plus replacement expense. The correction reversal retains the original movement's OccurredAt for reporting; replacement PaidAt defaults to the original expense date (or the requested new date). CreatedAt/ReversedAt always record the current audit time. Insufficient funds rolls back the entire change, including the reversal. A correction requires the original account to be active; reversal may restore money to a closed account without reopening it.

Serializable transactions, an IsReversed concurrency token, unique reversal/replacement indexes and an audit check constraint protect against duplicate reversal and incomplete history. Tenant filters and write guards apply throughout. Original payment amount/type/direction/vehicle are validated before a correction.

## Calculations

All actual expense and cost-plan paid totals exclude reversed expenses, including list/detail, dashboard, aged inventory, projected costs, sale profit and ROI. Account balance still sums all immutable movements. Monthly spending nets incoming expense reversals against outgoing purchases/expenses. Reducing a planned payment reopens the unpaid remainder; confirmation pays only that remainder. Cancelling a payment does not cancel its plan.

Angular reloads the details, plans, expenses, status history and available payment account via the existing forkJoin flow after success. The modal disables duplicate submission, retains input on failure and reconciles data when closed after an error. BG/EN translations are included.

## Schema and verification

Migration: 20261016104607_AddVehicleExpenseCorrections. Adds audit fields and relationships to VehicleExpenses; existing entries remain effective. Applied to the local DealerManager PostgreSQL database during implementation. Apply through the normal migration process on other environments; do not roll back after recording corrections, as that would discard their classification while retaining ledger movements.

Tests cover reversal, repeat rejection, amount increases/decreases, partial remaining payment, financial summaries, sold-vehicle profit, reason/amount validation, insufficient capital, rollback at all five save points and cross-tenant requests. Frontend tests cover validation, disabled planned category, duplicate-submit protection, error preservation, history and automatic refresh.
