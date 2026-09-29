# Vehicle status and inventory

No domain/schema changes or migrations. Existing int IDs, IBaseRepository, serializable IUnitOfWork and TimeProvider are reused.

## Backend

- `DealerManager.Application/Dtos/Vehicle/ChangeVehicleStatusRequest.cs`: required status and optional notes. This request accepts enum names or numeric values; existing response enums remain numeric.
- `VehicleStatusHistoryDto.cs`: immutable history response with Id, FromStatus, ToStatus, ChangedAt and Notes.
- `VehicleListResultDto.cs`: paginated inventory (`items`, `totalCount`) and flat list financial fields.
- `DealerManager.Application/FilterDtos/Vehicle/VehicleFilterDto.cs`: existing TextFilter/Offset/Limit/GetAllData pattern, with an optional Status filter.
- `IVehicleService`, `VehiclesController` and the partial `VehicleService` implementation in `VehicleOperationsService.cs` expose the new operations. `VehicleService.cs` shares its financial calculation between list and details.

`POST /api/vehicles/{id}/status` accepts, for example, `{ "status": "Repairing", "notes": "In workshop" }`. The server loads the tracked vehicle, validates the requested status and writes both Vehicle.Status and VehicleStatusHistory in one transaction/save. ChangedAt is UTC from TimeProvider; ChangedByUserId is null. Blank notes become null. No financial transactions are created.

All transitions between Purchased, Transporting, Arrived, Inspecting, Repairing, Preparing and ReadyForSale are permitted, including skipped or backward operational steps. A no-op transition is rejected. Listed/Reserved/Sold cannot be targets or sources of this generic action. Unknown enum values and missing status return 400, business conflicts 409, unknown vehicle 404. Serializable transaction handling protects concurrent changes and rolls back status/history together.

`GET /api/vehicles/{id}/status-history` returns only that vehicle's history ordered by ChangedAt descending and Id descending. History is neither edited nor deleted. Vehicle details still carries current Status without embedding history.

`GET /api/vehicles?TextFilter=BMW&Status=4&Offset=0&Limit=15` searches make, model and VIN case-insensitively. Filters are combined. Inventory sorts by Id descending and returns a filtered total count. Offset must be nonnegative; Limit is 1–500 unless GetAllData is used, matching Candidates.

For a populated inventory page there are six SQL queries regardless of vehicle count: page, count, purchase sums, expense sums, decision snapshot sale forecasts and plans with aggregated payments. Shared CalculateFinancialSummary and VehicleCostPlanItemDto.RemainingProjected preserve existing formulas, commitment precedence, cancelled costs, historical payments and missing-snapshot null values. There are no per-vehicle detail requests or stored balances.

## Frontend

`/vehicles` replaces the placeholder with a compact, horizontally scrollable table, debounced search, status filter and server pagination. VehicleStatusComponent renders translated moderate-color badges shared by inventory, detail header and history. Filters reset paging; switchMap cancels obsolete requests. Opening a vehicle and returning to inventory fetches current backend state.

Vehicle details keeps the purchase date beside its status and projected ROI among the main financial metrics. Change status opens a PrimeNG dialog with other operational statuses and optional notes. Save is disabled until valid and during the request. Listed/Reserved/Sold have no generic status action. On success the modal closes, a localized message appears and the shared forkJoin load refreshes details/financials, plans, expenses, capital and status history. No manual refresh button or browser reload is used. GET retry is offered only after a read error; POSTs never retry automatically.

History is collapsed by default and renders localized timestamps, old/new badges and notes. No history controls can mutate it. All application text is in matching BG/EN JSON keys.

## Validation

Backend HTTP tests cover status changes, UTC/user fields, no-ops, missing vehicles, reserved business statuses, undefined/missing enums, stage skipping, chronological/tie ordering and atomic rollback. Inventory tests cover financial parity with details, search, filters, pagination, empty results, missing forecasts and bounded query count.

Angular tests cover translated inventory, pagination/filter cancellation, loading/errors, modal validation, duplicate submits and locked states. A routed frontend integration scenario opens inventory, opens a Purchased vehicle, changes it to Repairing, verifies closed modal/current badge/history/success message and returns to inventory with updated state. HTTP writes run against isolated SQLite integration databases; browser inspection against the live API does not change real vehicle history.

Listing is intentionally deferred: ListVehicle, listing price/history, reservation, sale, customers, revenue and realized profit require dedicated later business actions. Dashboard data remains unchanged.
