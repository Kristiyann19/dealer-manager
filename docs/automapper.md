# AutoMapper

AutoMapper 14.0.0 is registered once by `ConfigureAutoMapper` in
`DealerManager.WebAPI/Extensions/InternalServicesExtensions.cs`, called from `Program.cs`.
Profiles are discovered from the Infrastructure assembly.

- `CandidateProfile`: candidate responses, estimates and ordered estimate items.
- `FinanceProfile`: account responses and transaction projections.
- `VehicleProfile`: vehicle responses, dossier updates, listings, status history,
  cost plans and expenses.

CandidateService, CapitalAccountService and VehicleService inject `IMapper`.
Read-only queries use `ProjectTo` after filtering/ordering, before materialization,
so EF still selects the DTO fields in SQL. Tenant filters remain on repository queries.

Financial calculations, balances, lifecycle transitions, transaction creation and
snapshot selection stay in services. Calculated DTO fields are explicitly ignored
by entity maps and supplied by services. Dashboard aggregate queries and Identity
operations remain explicit rather than loading entity graphs just for mapping.
Do not add broad `ReverseMap` mappings for request DTOs. The dossier update map
explicitly protects IDs, tenant ownership, lifecycle fields and navigations.

AutoMapper 14 was selected for its MIT license. NuGet reports NU1903 for
CVE-2026-32933 / GHSA-rvv3-g6hj-g44x (uncontrolled recursion). Profiles are bounded
with `MaxDepth(4)` and do not map recursive entity relationships. This limits the
current mapping surface but is not an upstream package fix. The advisory remains
enabled; patched releases start at 15.1.1 and 16.1.1 and have different licensing.

`DealerManager.Tests/Mapping/AutoMapperTests.cs` validates every discovered profile,
protected dossier fields and preservation of calculated estimate data. Run the
backend regression suite when changing profiles, including query projection tests.
