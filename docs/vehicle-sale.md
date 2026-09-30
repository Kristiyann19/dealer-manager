# SellVehicle

## Changed files

- Application: `SellVehicleRequest.cs`, `VehicleSaleResultDto.cs`, `IVehicleService.cs`, financial summary and detail DTOs.
- Infrastructure: `VehicleSaleService.cs` extends the existing partial VehicleService; `VehicleService.cs` and `VehicleOperationsService.cs` extend details and batched inventory reads.
- API: `VehiclesController.cs` adds `POST /api/vehicles/{id}/sale`.
- Angular: `sell-vehicle.component.ts/html`, Vehicle API service/models/error mapping, detail and inventory components/templates, BG/EN JSON.
- Tests: `VehicleSaleTests.cs`, existing inventory query-count assertion and Angular Vehicle tests.

No domain/schema changes or migrations. Existing VehicleSale, unique VehicleId index, VehicleListing, FinancialTransaction, IBaseRepository, serializable IUnitOfWork and TimeProvider are reused.

## Business action

Request: `{ "actualSalePrice": 6800, "soldAt": "2026-09-30T12:00:00Z" }`. SoldAt and CapitalAccountId are optional. A missing date uses current UTC; supplied dates normalize to UTC. Without an account ID, the unique outgoing VehiclePurchase account is used. An explicitly selected account must exist; either account must be active and use EUR, consistent with the current vehicle money model. No account selector or multi-account workflow is added to the UI.

Only a Listed vehicle with an active listing and no prior VehicleSale may be sold. ActualSalePrice must be positive. All work runs inside one serializable transaction:

1. Validate the vehicle, listing, amount and account.
2. Calculate TotalInvested from actual purchase transactions plus actual expenses.
3. Create one incoming VehicleSale FinancialTransaction for the complete ActualSalePrice.
4. Save to obtain its FK; create VehicleSale referencing that movement with null CustomerId/Notes.
5. Deactivate (retain) the listing, set Sold, append Listed → Sold history with ChangedAt = SoldAt.
6. Save and commit. Failure at either save rolls back every write, including the first financial movement.

Repeat/concurrent sales are protected by validation, serializable isolation and the existing unique sale index. Generic status changes still cannot set Sold. HTTP returns 201 with VehicleId, SaleId, ActualSalePrice, SoldAt, account ID, updated derived account balance, TotalInvested, RealizedProfit, RealizedROI and Status; invalid input is 400, missing vehicle 404 and business conflicts 409.

ExpectedSellingPrice stays in the decision estimate; ListingPrice stays in the retained listing; ActualSalePrice is VehicleSale.SalePrice. Profit is never a separate financial transaction. RealizedProfit = SalePrice − TotalInvested; RealizedROI = RealizedProfit / TotalInvested × 100, or zero for zero investment. These are read-model calculations, not database columns. Loss is allowed.

## Reads and UI

Details adds sale price/date/derived realized metrics, latest listing price/date, and the sale account's current balance. CurrentListing becomes null after sale. Inventory batches sale data and latest listing prices with eight queries per populated page (no per-vehicle requests). All/Sold filters retain sold vehicles.

Only Listed vehicles show Sell vehicle. The dialog defaults to listing price and offers an editable positive actual price, optional date and live profit/loss/ROI preview. Save is disabled while invalid or busy. A failed write preserves the draft; closing after an error reconciles server state. No financial write is retried automatically.

After success the modal closes, a translated toast appears and the existing GET refresh updates details, status history, financial sections and current capital. Sold details use actual/realized primary metrics, preserve the listing and sale dates and original forecast as history, and hide pending cost-plan actions. Inventory remains six columns, showing actual price/profit for sold rows and clearly labelled forecasts otherwise. No manual refresh/browser reload.

## Verification and next feature

Backend tests cover the 6469 invested / 7000 listing / 6800 sale example: capital 7081 → 13881, profit 331, ROI ≈ 5.12%. They also cover loss, optional dates, zero investment, explicit account, disallowed states, missing listing, invalid accounts, duplicate sale and rollback after each save. Tests run in isolated SQLite databases; no real vehicle is sold as test data.

Angular integration tests cover status-specific actions, default price, edits/live preview, loss, validation, duplicate submit, errors, sale success, modal closure, history/realized metrics/capital refresh and return to inventory. Dashboard integration remains next: real KPI reads and charts can now derive revenue, invested capital and realized profit from these records. Customers, reservations, deposits, installments, invoices, reports and listing price history remain outside scope.
