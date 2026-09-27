# PurchaseCandidate

`POST /api/candidates/{id}/purchase` accepts `capitalAccountId`, positive `actualPurchasePrice`, and optional `purchaseDate`. The response contains candidate/vehicle/account IDs, actual price, UTC purchase date, previous balance and remaining balance. `GET /api/vehicles/{id}` supports direct visits to the minimal vehicle page; Candidate Details now includes `vehicleId`.

The service uses the existing repositories and UnitOfWork. No entity or database schema changes are required. A candidate must be Approved, have an estimate and a valid year, and have neither an existing vehicle nor an existing decision snapshot. Payment requires an active EUR account with sufficient funds. Candidate forecasts currently use EUR; no conversion is performed.

Within one transaction, the latest estimate (Version, then Id) becomes the decision snapshot, the candidate becomes Purchased, the vehicle and non-Purchase cost-plan items are created, and an Out / VehiclePurchase transaction records the positive actual price. The first SaveChanges obtains the vehicle ID; the second saves the payment, both before the same commit. Any failure rolls everything back. AskingPrice, estimated purchase amounts, previous versions and expected selling prices remain unchanged. Purchased candidates cannot add estimates, approve or reject through the existing API.

`ICapitalAccountService.GetBalance` reuses the existing SQL aggregate projection without loading history. Both balance checks and the response's remaining balance execute inside the transaction. The existing UnitOfWork uses Serializable isolation: concurrent spending that cannot be serialized is aborted and mapped to HTTP 409. There is no automatic purchase retry. Concurrent PostgreSQL load testing was not performed; SQLite integration tests validate transaction atomicity, not PostgreSQL serialization scheduling.

Business conflicts include a stable ProblemDetails `code` for translated frontend errors. Unknown resources return 404, malformed request values return 400, and business/concurrency conflicts return 409.

## UI verification

1. In Finances, create an EUR account and add EUR 20,000.
2. Create a candidate with a valid year. Add an estimate: Purchase 6,500, Transport 1,000, Repair 500, Detailing 100. Approve it.
3. Open Confirm purchase, enter 6,300 and select the account. The preview shows 13,700. The forecast remains 6,500.
4. Confirm: the vehicle page shows a success message, actual price and source candidate link. It also loads correctly on refresh/direct visit.
5. Open the original candidate: Purchased status, historical snapshot and Open vehicle link are displayed; purchase is unavailable.
6. Open Finances: the account balance is 13,700 and history includes Out 6,300 with the vehicle reference.

Candidates without a year are deliberately blocked; no fake default year or new candidate-edit workflow was introduced. The date is optional; when supplied from the UI it represents local midnight and is normalized to UTC by the backend. The UI filters payment accounts to active EUR accounts and disables confirmation for insufficient funds; server checks remain authoritative.

The automated HTTP integration flow runs these financial writes only in an isolated SQLite database. It verifies the 20,000 / 6,300 / 13,700 example, unchanged historical prices, one snapshot, non-Purchase plan items, repeated purchase rejection, invalid state/account/currency/year/funds, and rollback after the payment was saved but before commit. Angular tests cover modal/API wiring, preview, validations, duplicate submissions, conflicts, visibility, navigation and direct vehicle loading. No purchase test records are inserted into the user's live database.
