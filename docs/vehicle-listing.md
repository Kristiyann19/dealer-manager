# Vehicle Listing

Listing is a dedicated business action. It does not create a sale, expense, capital movement or realized profit.

## Files

- Domain: `DealerManager.Domain/Entities/Vehicle/VehicleListing.cs`; `Vehicle.cs` adds the initialized collection.
- Persistence: `DealerManagerDbContext.cs`, migration `20260929130240_AddVehicleListing` and its designer/model snapshot. The new table has an int PK, restricted vehicle FK, positive-price check and filtered unique index allowing at most one active listing per vehicle.
- Application: `ListVehicleRequest`, `VehicleListingDto`, `VehicleDetailsDto.CurrentListing`, `VehicleListItemDto.ListingPrice`, `IVehicleService`.
- Backend: `VehicleListingService.cs` is part of the existing partial VehicleService, using IBaseRepository, serializable IUnitOfWork and TimeProvider. `VehicleService.cs`, `VehicleOperationsService.cs` and `VehiclesController.cs` integrate reads and routes. No new repository or service registration.
- Frontend: `list-vehicle.component.ts/html`, Vehicle details/list templates, details component, API service, models and error mapping; matching BG/EN translations. Existing global form/panel/button styles are reused.
- Tests: `VehicleListingTests.cs`, inventory query-count assertion, Angular `vehicle-operations.spec.ts` and existing detail fixtures.

## API and transaction

`POST /api/vehicles/{id}/listing` accepts `{ "listingPrice": 6200, "listedAt": "2026-09-29T00:00:00Z" }`. ListedAt is optional and defaults to the current UTC time. Supplied dates normalize to UTC. CreatedAt and history ChangedAt always use the current UTC clock.

Vehicle must exist, be ReadyForSale, have no active listing, and the price must be positive. One transaction creates the active listing, changes status to Listed and appends ReadyForSale → Listed history with null ChangedByUserId. A failed save rolls everything back. Serializable isolation and a unique active-listing index protect concurrent requests. Conflicts return 409; invalid input 400; missing vehicle 404.

The response is 201 with a Location header and VehicleId, ListingId, ListingPrice, ListedAt, Status. `GET /api/vehicles/{id}/listing` returns the active listing (200), 204 when the vehicle has none, or 404 for an unknown vehicle. Generic status changes still cannot set Listed.

ExpectedSellingPrice continues to come from the immutable candidate decision snapshot. ListingPrice lives only in VehicleListing. No actual sale price is added. Details includes CurrentListing and inventory adds nullable ListingPrice through one batched page query (seven queries for a populated inventory page, no N+1).

## UI

Only ReadyForSale displays the primary List vehicle button. The dialog shows the read-only expected price, a required positive listing price and optional date. An omitted date uses server time; a selected date uses local midnight converted to UTC, consistent with existing date forms. Submit stays disabled while invalid or busy. Failed requests preserve the draft; closing after a write error reconciles with the server.

On success the modal closes, a translated success toast appears and the existing forkJoin refresh reloads details, finances, plans, expenses, payment account and status history. The listing panel shows price, date and **potential profit at listing price = ListingPrice − TotalInvested**, with explicit text that this is not realized profit and excludes future costs. The original expected price and forecast remain visible. Inventory shows listing price beneath expected price without another column.

## Verification and remaining scope

Verification passed: 129 backend tests, 82 Angular tests, 509 matching translation keys, backend solution build and Angular production build. Angular retains the existing initial-bundle budget and unused Topbar RouterLink warnings. EF reports no pending model changes after the migration.

Backend tests cover allowed/disallowed statuses, invalid/missing price/date, repeat/active listing, uniqueness, inactive history, UTC timestamps, atomic rollback, GET/details/inventory reads, unchanged capital and transaction count, and distinct forecast/listing values.

The routed Angular integration test opens the modal on ReadyForSale, enters 6200 against expected 6000 and invested 5210, confirms, verifies Listed/history/separate prices/potential profit 990/success message/unchanged displayed capital, then returns to inventory. HTTP tests use isolated SQLite; no live vehicle is listed as test data.

The additive migration has been applied to the local DealerManager database. Other environments need `dotnet ef database update --project DealerManager.Infrastructure --startup-project DealerManager.WebAPI` before running the new build.

SellVehicle remains separate: actual sale price, customer, incoming payment, VehicleSale and realized profit are not implemented. Reservation, listing price edits/history, revenue and dashboard integration are also outside this feature.
