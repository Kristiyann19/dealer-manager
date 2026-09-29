# Vehicle financials and expenses

No domain entities or database schema changed. The existing repositories, UnitOfWork, TimeProvider and Finance balance query are reused.

## API

- `GET /api/vehicles/{id}`: vehicle information, financial summary fields and optional original decision forecast.
- `GET /api/vehicles/{id}/financial-summary`: calculated vehicle summary.
- `GET /api/vehicles/{id}/cost-plan`: current plan with actual paid and remaining projected values.
- `PUT /api/vehicles/{id}/cost-plan/{itemId}`: replaces current estimate, commitment and cancellation flag; a null description preserves the existing description.
- `GET /api/vehicles/{id}/expenses`: payments ordered by PaidAt descending, then Id descending. Includes account name/currency and linked plan description for display.
- `GET /api/vehicles/{id}/payment-account`: resolves the unique purchase account and its current server-calculated balance.
- `GET /api/vehicles/{id}/cost-plan/{itemId}/payment-preview`: returns the eligible plan category, current target amount, purchase account ID, currency and current balance.
- `POST /api/vehicles/{id}/cost-plan/{itemId}/confirm-payment`: confirms the entire current target atomically. Body: `{ "expectedAmount": 670, "expectedCapitalAccountId": 1 }`. These are preview guards only: amount/account are derived again inside the transaction. A changed preview returns `409 paymentChanged`.
- `POST /api/vehicles/{id}/expenses`: records an unexpected actual payment with a null plan ID, using the purchase account automatically. Required fields are category, description, amount. Optional fields: supplier, documentNumber, paidAt. The legacy optional capitalAccountId must match the resolved purchase account; a non-null costPlanItemId is rejected with `confirmationRequired`.

Plan amounts may be null or zero but cannot be negative. Zero commitment takes precedence over an estimate. Cancelled items have zero remaining projected cost; actual payments remain included in invested capital. Confirmation requires a positive target and an active, unpaid plan belonging to the vehicle. Fully paid items return `alreadyPaid`; historical partial payments return `partialPayment` rather than charging the target again. No plan or historical payment is deleted.

Both payment flows require a vehicle that is not Sold, an active EUR purchase account and sufficient balance. The account comes from VehiclePurchase / Out transactions for that vehicle; missing or ambiguous accounts return `purchaseAccountMissing`, never a fallback account. No account selection or currency conversion is implemented. Description is trimmed; supplier and document number are trimmed to null when blank. Confirmation copies category and description from the plan and uses TimeProvider UTC. Unexpected expenses can supply PaidAt (normalized to UTC); otherwise it defaults to TimeProvider UTC.

The account balance check and both saves are inside the existing Serializable transaction. The first save obtains the financial transaction ID; the second records the linked expense. Failure after either save rolls back both records. No automatic retry is used for a write whose outcome is uncertain. PostgreSQL serialization conflicts remain HTTP 409 through the existing handler; no new locking infrastructure was added.

## Calculations

- `ProjectedTarget = CommittedAmount ?? CurrentEstimatedAmount ?? 0`
- `ActualPaid = SUM(linked VehicleExpense.Amount)`
- `RemainingProjected = max(ProjectedTarget - ActualPaid, 0)` for active plan items; cancelled items return zero.
- `ActualPurchasePrice = SUM(VehiclePurchase / Out transaction amounts)`
- `ActualExpenses = SUM(all vehicle expenses, including unplanned and cancelled-plan payments)`
- `TotalInvested = ActualPurchasePrice + ActualExpenses`
- `RemainingProjectedCosts = SUM(active plan RemainingProjected)`
- `ProjectedFinalCost = TotalInvested + RemainingProjectedCosts`
- `ExpectedSellingPrice` comes from the Candidate decision snapshot.
- `ProjectedProfit = ExpectedSellingPrice - ProjectedFinalCost`
- `ProjectedROI = ProjectedProfit / ProjectedFinalCost * 100`, or zero for a zero final cost.

If no snapshot exists, expected selling price and profit are null; ROI is null unless final cost is zero. No calculated values are stored. Original forecast items and totals use the immutable decision snapshot and existing Candidate calculator. Queries aggregate payments in SQL, with no per-plan database round trips.

An estimate of 500 with 300 actually paid leaves 200 projected, so projected cost remains 500. Paying 650 leaves zero projected, so cost becomes 650. An unexpected payment increases actual cost without reducing unrelated planned amounts.

## UI

`/vehicles/{id}` is a single page: five main financial values, upcoming expenses, payment history, then a collapsed original forecast. Purchase price and ROI are secondary. Upcoming expenses show only server-provided `remainingProjected > 0` items; paid/cancelled items remain in backend history. Each row shows the effective forecast, actual paid amount and remaining projection.

Add payment opens a confirmation-only dialog: category, amount, deduction explanation, available capital, balance after payment, Cancel and Confirm. It has no editable fields. A fresh server preview is fetched each time; insufficient funds disable confirmation. Failed writes are never retried automatically and require refreshing the preview before another attempt. Successful writes close the dialog and reload details/summary, plan and history. Opening the next payment fetches a fresh balance.

Edit estimate affects planning only. Existing commitment/cancellation remain under More options. Commitment takes precedence over the estimate; the editor explains that precedence. To change an amount, edit its current target before confirmation.

Unexpected expense remains a separate form with category, description and amount; date/supplier/document number are optional under More options. There is no account dropdown. It resolves the purchase account and displays a fresh balance preview. All text is in BG/EN JSON and formatting uses shared localized pipes and global styles.

## Verification

Automated HTTP integration tests cover forecast 670 EUR with available capital 5,000 EUR: preview, confirmation, balance 4,330 EUR, retained plan with ActualPaid 670 and RemainingProjected 0, new payment history, refreshed totals and identical original Candidate decision snapshot. Tests cover re-confirmation, insufficient funds, stale/tampered previews, missing/ambiguous purchase accounts, plan ownership, cancellation, invalid amounts, sold vehicles and rollback after either save. Legacy partial payment/overrun calculations are verified using seeded historical records; the new API does not create partial payments.

Angular tests cover confirmation without inputs, duplicate submissions, insufficient funds, refreshed previews after conflicts, missing purchase accounts, pending item disappearance, new history/totals, refreshed balance, unexpected expenses, translations and existing estimate editing.

Validation: 95 backend tests, 58 Angular tests, matching BG/EN translation keys and backend/Angular builds passed. Angular retains its existing initial bundle budget warning (about 652 kB against 500 kB). Browser verification is read-only/cancel-only against the live API; financial write scenarios run in isolated integration tests, not the user's database.
