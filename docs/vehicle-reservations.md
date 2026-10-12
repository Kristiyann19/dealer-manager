# Vehicle Reservation + Deposit

## Model and schema

`VehicleReservation` records customer name/contact, positive deposit, reservation/expiry dates, notes, status (Active/Completed/Cancelled), deposit/refund movement IDs, creation/completion/cancellation timestamps and separate cancellation notes. Integer IDs; no Customer entity. Vehicle history is retained, and expiry is informational.

Migration `20261012111047_AddVehicleReservations` adds the table and a partial unique index on VehicleId WHERE Status = 0. Financial links restrict deletion. VehicleSale gains ReservationId and an optional FinancialTransactionId: no final movement exists when the deposit covers the entire sale. Existing sales and movements are preserved.

## Actions

- POST `/api/vehicles/{id}/reservations`: Listed + active listing, no active reservation; customer required, 0 < deposit <= listing price; an active EUR account belonging to the dealership. ReservedAt defaults to TimeProvider UTC. Creates an incoming VehicleDeposit (type 7), reservation and Listed → Reserved history. Listing stays active.
- GET `/api/vehicles/{id}/reservations/active`: active DTO or 204; missing/foreign vehicle is 404.
- GET `/api/vehicles/{id}/reservations`: all historical records, ReservedAt DESC then Id DESC.
- POST `/api/vehicles/{id}/reservations/{reservationId}/cancel`: explicit nullable-required `refundDeposit` boolean, optional cancelledAt/notes. Active only. If refunded, create outgoing DepositRefund (type 8) from the original deposit account; sufficient funds are required. If retained, create no movement. Mark Cancelled and transition Reserved → Listed. The existing listing remains active.
- POST `/api/vehicles/{id}/sale`: Listed uses the existing full-price payment. Reserved requires an active reservation and price >= its deposit. Record only price − active deposit; skip the movement at zero. Complete that reservation, create VehicleSale with full SalePrice, deactivate the listing and transition directly Reserved → Sold. By default a reserved sale uses its deposit account for the final payment; existing explicit same-tenant EUR account selection remains available at the API.

All mutations use the existing serializable unit of work, global tenant query filters and tenant write guards. Foreign vehicles/accounts/reservations return 404. Double submissions cannot create a second active reservation or sale. A failure at either save rolls back the complete operation. Generic status changes cannot reserve vehicles.

Cancelled/retained deposits never become sales or reduce a later customer's final payment. Capital includes all In minus Out movements; TotalInvested still includes purchase + actual expenses only. RealizedProfit and dashboard sales revenue use VehicleSale.SalePrice, never the final payment amount.

Example: initial cash 20,000; deposit +500 → 20,500; full sale price 12,000; final payment +11,500 → 32,000. Investment 10,000 means profit 2,000. Refund cancellation returns cash to 20,000; retained cancellation leaves 20,500.

## UI and verification

Listed details offer Reserve and Sell. Reservation dialog includes customer/contact, deposit, optional expiry/notes, account selector and a listing-price-based remaining preview. Reserved details show customer, deposit and dates plus cancellation/sale actions. Cancellation requires refund/retain selection and previews the original account balance. Sale preview subtracts the active deposit while profit uses the full price. Successful mutations close dialogs, show translated toasts and re-fetch existing vehicle/financial/history GETs; invalid/busy forms prevent duplicate submission.

BG/EN translations include new transaction types and all reservation labels/errors. Existing inventory Reserved status remains unchanged. No scheduler, payment gateway, customer module, partial deposits or accounting rules.

Backend integration tests cover cash, profit, boundary amounts, zero final payment, active listing, historical reservations, retained deposits, dedicated statuses, foreign resources with real tenant sessions, database uniqueness and rollback at each save. Angular tests cover forms, live previews, refund account choice, duplicate submit, server conflicts and details refresh. Migration was applied to the local DealerManager PostgreSQL database without resetting it.
