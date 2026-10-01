# Dashboard integration

`GET /api/dashboard` is a read-only aggregate endpoint. No migrations, statistics tables, persisted balances or calculated statistics were added. The server uses `TimeProvider.GetUtcNow()`; all monthly ranges are UTC `[monthStart, nextMonthStart)`.

## Backend files

- `DealerManager.Application/Dtos/Dashboard/DashboardDto.cs`: aggregate response and operational, financial, pipeline, vehicle and month DTOs.
- `DealerManager.Application/IService/Dashboard/IDashboardService.cs`: service contract.
- `DealerManager.Infrastructure/Service/Dashboard/DashboardService.cs`: database aggregation, bounded previews and six calendar months.
- `DealerManager.WebAPI/Controllers/DashboardController.cs`: GET endpoint, cancellation propagated.
- `DealerManager.Infrastructure/Service/Finance/FinancialQueries.cs`: shared SQL balance and vehicle investment queries, now used by existing finance/vehicle services as well.
- `DealerManager.Application/Dtos/Vehicle/CostPlanCalculation.cs`: one remaining-cost expression used both by DTO calculations and SQL aggregation.
- `DealerManager.Tests/Dashboard/`: aggregate and full HTTP lifecycle tests.

Service registration lives in `InternalServicesExtensions`.

## Exact definitions

| Metric | Definition |
| --- | --- |
| Cars in stock | Count of Vehicle records whose Status is not Sold, including Reserved. |
| Cars in repair | Count whose Status is Repairing; expenses do not determine status. |
| Sold this month | Count of VehicleSale records whose SoldAt falls in the current UTC month. |
| Profit this month | Sum of actual SalePrice minus lifetime TotalInvested for those sales, including losses. |
| Total invested | VehiclePurchase OUT transaction amounts plus actual VehicleExpense amounts. Expense transactions are not added again. |
| Available cash | Sum of calculated In minus Out balances of active EUR accounts. Other currencies and inactive accounts are excluded. |
| Capital invested | TotalInvested summed over unsold vehicles only. |
| Upcoming projected costs | Unsold vehicles' noncancelled plans: max((CommittedAmount ?? CurrentEstimatedAmount ?? 0) - ActualPaid, 0). Cancelled/fully paid/overpaid plans contribute zero. |
| Net worth at cost | AvailableCash + CapitalInvested. No forecast or listing values. |
| Pipeline candidates | Candidates not Purchased or Rejected. Vehicle stages count their exact statuses. |
| Recent candidates | Latest five active candidates, CreatedAt descending then Id descending; highest estimate Version then Id supplies forecast figures. Missing estimates produce null, not fake zero profit. CandidateFinancialCalculator calculates profit and ROI. |
| Active vehicles | Latest five unsold vehicles, CreatedAt descending then Id descending. ExpectedSellingPrice is the decision snapshot price, matching existing Vehicle financials. Active listing price is displayed separately. ProjectedFinalCost = TotalInvested + RemainingProjectedCosts; ProjectedProfit = ExpectedSellingPrice - ProjectedFinalCost. |
| Chart invested | EUR VehiclePurchase / VehicleExpense OUT transactions by OccurredAt within the month, including spending on vehicles since sold. |
| Chart sales revenue | Actual VehicleSale.SalePrice by SoldAt within the month. |
| Chart realized profit | Actual sale price minus lifetime TotalInvested for those sales. |

The chart includes the current month and five preceding calendar months, zero-filled. Vehicle purchase, expense and sale workflows already enforce EUR; no currency conversion is performed. Future-dated records are assigned to their actual calendar bucket, not the current day.

## Queries and consistency

There are **21 SELECT statements** for a populated dashboard (some providers can skip empty-ID queries):

- 1 vehicle status grouping, 1 active candidate count;
- 3 scalar financial aggregates;
- 1 recent candidate projection, 3 bounded active vehicle batch queries;
- 12 chart aggregates (two per calendar month, for six months).

This fixed count does not grow with inventory size. Full inventory entities are never loaded for totals. SQL performs sums/counts and correlated projections. Only five candidate rows, five vehicle rows and the selected vehicles' cost-plan amount projections are materialized. No call to GetVehicleDetails in a loop, lazy loading or per-vehicle database query occurs. The month loop has six fixed buckets, not N entities. Requests use the normal database read isolation; a concurrent mutation can land between separate aggregates, and navigating to the dashboard fetches a fresh response.

## Angular

DashboardService uses HttpClient and the existing API_BASE_URL. The route no longer provides MockDashboardService; both mock service/data files were removed. No client business metric calculations or permanent refresh button remain. Each component creation requests the aggregate endpoint; errors offer Retry, which returns to loading state. Requests are cleaned up through toSignal. First row is operational/monthly; second row is finance. Negative profit is supported. All amounts retain cents.

Removed sample date/workspace badge, fake growth/overdue values, mock specifications, preview dialogs and decorative spending percentages. Candidate and vehicle Review actions navigate to their real detail routes. Empty lists, null forecasts, absent listings and six zero months have explicit presentation. Labels and new help text are translated in Bulgarian and English; existing theme support remains.

## Verification

- Full backend suite: 162 passing tests at implementation time.
- Angular suite: 92 passing tests; production build and translation validation pass.
- Backend Release build passes. Angular still reports the pre-existing bundle budget and unused Topbar RouterLink warnings.
- Relational API tests verify statuses, month boundaries, previous/next-month exclusion, positive/negative actual profit, active EUR balances, sold exclusion, partial/cancelled plan costs, latest estimates, ordering, six months and fixed query count.
- The lifecycle test exercises real endpoints: contribution 20,000; purchase 6,000; paid repair 1,000; list 11,000; actual sale 9,000 (forecast 15,000). Before sale: cash +13,000, invested +7,000, net worth +20,000. After sale: cash +22,000, invested back to baseline, realized profit +2,000 and sold count +1.
- That lifecycle also passed against the configured PostgreSQL database with existing records. The optional `DEALER_DASHBOARD_POSTGRES` connection-string environment variable selects this verification. An outer repeatable-read transaction encloses the HTTP workflow and always rolls back; no schema changes or test records persist. PostgreSQL sequences can advance even on rollback. Test-only UnitOfWork enlists in the outer transaction; production transaction behavior is covered by the normal tests.
- The live API and Angular dashboard were checked with real database values, and the vehicle Review route was verified.

On this restricted Windows runner, use `Logging__EventLog__LogLevel__Default=None` for test/local server processes to avoid permission errors from the OS EventLog logger. This is a process environment override, not an application logging configuration change.
