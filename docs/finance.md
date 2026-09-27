# Capital accounts API

The first Finance backend uses the existing CapitalAccount and FinancialTransaction entities, IBaseRepository and IUnitOfWork. No schema or migration changes are required. All identifiers are integers.

## Endpoints

| Method | Path | Result |
| --- | --- | --- |
| POST | `/api/capital-accounts` | 201, new active account with zero balance |
| GET | `/api/capital-accounts` | Active accounts and calculated balances |
| GET | `/api/capital-accounts/{id}` | Account, balance and latest 20 transactions |
| POST | `/api/capital-accounts/{id}/contributions` | 201, new incoming capital contribution |
| GET | `/api/capital-accounts/{id}/transactions` | Full transaction history |

Create an account:

```json
{ "name": "Main capital", "currency": "EUR" }
```

Add capital with `POST /api/capital-accounts/1/contributions`:

```json
{ "amount": 20000, "description": "Initial capital contribution" }
```

`occurredAt` is optional. The service normalizes a supplied timestamp to UTC, defaults it to TimeProvider.GetUtcNow(), and always sets createdAt from TimeProvider. Name, currency and description are required and trimmed. Amount must be strictly positive; no rounding or currency conversion is applied.

The route supplies CapitalAccountId. An optional capitalAccountId in the body must match the route; direct service callers must set it. Clients cannot choose transaction type, direction, vehicle or createdAt. Contributions always have CapitalContribution / In with no VehicleId.

## Balances and history

Balance = SUM(In.Amount) - SUM(Out.Amount). Both sums run in the database, with empty sums treated as zero. The account list uses one SQL query with correlated aggregates, so there are no per-account round trips or transactions loaded into memory for summation. Balances are per account and currency, are not cached and are only DTO properties. Details calculate the balance from all transactions, independently of the latest-20 limit. Details use one account/balance query and one limited history query; they do not promise a shared snapshot if contributions arrive between those queries.

Both history endpoints sort by OccurredAt descending, then Id descending. Full history is currently unpaginated as requested. Inactive accounts remain readable but are excluded from the active list; contributions to them return 409. Missing accounts return 404 and invalid requests return 400 using the existing ProblemDetails handler.

AddCapital checks the account and creates one FinancialTransaction in an existing UnitOfWork transaction. It does not update the CapitalAccount row. Each successful POST is a new contribution; there is no idempotency-key contract yet, so do not automatically retry uncertain write outcomes.

## Future purchase integration

PurchaseCandidate can later create the Vehicle and a positive FinancialTransaction with Type=VehiclePurchase, Direction=Out and the selected CapitalAccountId/VehicleId in the same UnitOfWork transaction. The current balance query will then include the outgoing movement automatically. PurchaseCandidate should not call AddCapital, which specifically creates incoming contributions. Candidate estimates remain predictions and never create capital movements.

## Verification

### Angular Finance page

Open `/finances` using the existing sidebar link. Create an account with a name and currency (EUR is the default), select it, then use Add capital with a positive amount and description. The optional date is interpreted at local midnight and sent as an ISO timestamp; an empty date lets the server choose the current time.

The Angular service uses `API_BASE_URL` and the five capital-account endpoints above. After account creation or a contribution, the page reloads the active account list, selects the affected account, then requests its details and full transaction history. Summary values sum server-provided currentBalance values separately per currency; history is never used to reconstruct balances. Incoming amounts show a green plus, outgoing amounts a red minus, while the response amount stays positive.

Use Refresh to reload balances and history. Failed writes retain the form; uncertain network/server failures ask the user to check refreshed history before retrying because the API has no idempotency contract. Forms prevent duplicate submissions while a save is pending. All labels and states use the existing BG/EN translation mechanism.

From `WebApplication`, run `pnpm check:i18n`, `pnpm test`, and `pnpm build`. Finance tests cover currency grouping, account selection cancellation, creation, contributions, validation, duplicate submission prevention, backend refreshes, errors, translations and transaction signs. Real local API verification used read-only requests through the Angular proxy and the empty-account UI; no test capital was inserted into the user's database.

`dotnet build DealerManager.slnx` and `dotnet test DealerManager.Tests/DealerManager.Tests.csproj`.

Finance integration tests use the real service/repositories/API with isolated SQLite databases. Only the test model converts transaction timestamps to UTC ticks because SQLite cannot order DateTimeOffset directly; production PostgreSQL mappings remain unchanged. Tests cover validation, initial and decimal balances, outgoing transactions, account isolation, one-query account listing, latest-20 ordering and full-balance calculation, timestamps, inactive and unknown accounts, and server-owned transaction fields.
