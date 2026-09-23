# Candidate evaluation API

Candidate represents a pre-purchase opportunity. This feature only creates candidates, creates versioned estimates, reads their analysis, and approves/rejects them.

## Structure

- `DealerManager.Application/IService/Candidate`: service/calculator contracts and feature-specific not-found/conflict exceptions.
- `DealerManager.Application/Dtos/Candidate`: create requests and list/details/estimate read models.
- `DealerManager.Application/FilterDtos/Candidate/CandidateFilterDto.cs`: existing pagination plus optional make/model/VIN text search.
- `DealerManager.Infrastructure/Service/Candidate/CandidateService.cs`: orchestration and explicit DTO mapping.
- `DealerManager.Infrastructure/Service/Candidate/CandidateFinancialCalculator.cs`: decimal financial calculations.
- `DealerManager.Infrastructure/Repository/UnitOfWork.cs`: saving changes and serializable transactions on the existing DbContext.
- `DealerManager.WebAPI/Controllers/CandidatesController.cs`: thin HTTP endpoints.
- `DealerManager.WebAPI/ExceptionHandling/CandidateExceptionHandler.cs`: ProblemDetails for validation, not-found and conflict errors.
- `DealerManager.Tests/Candidate`: business and HTTP integration tests using an isolated SQLite in-memory database.

No additional repository, ORM, validation or mapping library is introduced. The existing `BaseRepository` is registered as an open generic and injected twice for Candidate and CandidateEstimate. Its `GetById`, `GetAll`, `GetListByProperties`, `GetQueryByProperties`, `Create`, and `SetEntryModified` methods are reused unchanged. Candidate details include estimates and items through its existing include callback. Lists load the latest estimates for the current page in one batch.

## Endpoints

| Method | Path | Operation |
| --- | --- | --- |
| POST | `/api/candidates` | CreateCandidate |
| GET | `/api/candidates` | GetCandidates |
| GET | `/api/candidates/{id}` | GetCandidateDetails |
| POST | `/api/candidates/estimates` | CreateCandidateEstimate |
| GET | `/api/candidates/{id}/estimates` | GetEstimateHistory |
| POST | `/api/candidates/{id}/approve` | ApproveCandidate |
| POST | `/api/candidates/{id}/reject` | RejectCandidate |

Create candidate example:

```json
{
  "make": "BMW",
  "model": "X1 2.0d",
  "year": 2013,
  "mileage": 142000,
  "expectedSellingPrice": 7000,
  "notes": "Inspect the fuel system"
}
```

The backend assigns Id, UnderReview status and CreatedAt. Requests cannot assign system timestamps or status. Make/Model are required; Year must be 1886 through the current UTC year plus one. Mileage and selling price must be non-negative. Optional empty strings are normalized to null.

Create estimate example (replace candidateId with the created candidate's ID):

```json
{
  "candidateId": 1,
  "expectedSellingPrice": 7000,
  "notes": "Updated transport and repair quotes",
  "items": [
    { "category": 0, "description": "Purchase", "estimatedAmount": 3000 },
    { "category": 1, "description": "Transport", "estimatedAmount": 660 },
    { "category": 2, "description": "Repair", "estimatedAmount": 1200 }
  ]
}
```

Enums retain the existing numeric JSON representation. At least one item is required. Each item needs a valid CostCategory, a description, and a non-negative amount. Zero-cost items are allowed. Version, CreatedAt, item IDs and IsDecisionSnapshot are assigned by the backend.

Reject with an optional reason:

```json
{ "reason": "Repair risk is too high" }
```

An empty object `{}` rejects without a reason. Approval does not require a body.

## Versioning and financial analysis

Within one serializable transaction, the service verifies the candidate, reads the highest estimate version, and inserts a new estimate with `max + 1` (or 1 initially), including its items. Existing estimates and decision snapshots are never updated. All current estimate writers must use this transaction path. No schema change or migration is needed for this feature.

PostgreSQL serialization failures/deadlocks roll back the entire operation and return HTTP 409. The caller can retry the complete request; there is no automatic retry. This prevents two conflicting requests from committing the same new version through this service. This behavior follows [PostgreSQL transaction isolation](https://www.postgresql.org/docs/current/transaction-iso.html).

The calculator returns:

- EstimatedTotalCost = sum of item amounts.
- ExpectedProfit = the estimate's selling price minus its total cost.
- ExpectedRoi = profit / cost * 100, or 0 when cost is zero.

Decimal results are not rounded or persisted. An absent estimate produces null calculated fields, distinct from an estimate with zero cost. History is newest version first; each entry includes its own items, selling price and financial analysis. Details also return LatestEstimate. List results contain Items and TotalCount, with Limit=30 and Offset=0 by default; Limit accepts 1–500, or GetAllData=true bypasses paging.

## Explicit assumptions

- A new estimate updates Candidate.ExpectedSellingPrice to its current expected price while retaining the historical price in every prior estimate.
- Only UnderReview/Approved candidates accept new estimates. Adding an estimate does not automatically reset an Approved candidate's status.
- Approval requires an estimate and rejects Rejected/Purchased candidates. It never selects a decision snapshot.
- Repeated approval/rejection is idempotent. Repeated rejection preserves the first RejectedAt and reason.
- A rejection reason is appended to Candidate.Notes, preserving existing notes.
- Validation uses DataAnnotations at the API boundary and in service methods, with the dynamic year and aggregate arithmetic checks in the service/calculator.
- There was no pre-existing error/result convention; expected errors use HTTP ProblemDetails (400/404/409). Unexpected exceptions use the standard ASP.NET Core handler.

No Vehicle, FinancialTransaction, capital operation, purchase action, photo storage, authentication or Angular integration is implemented.

## Verification

```sh
dotnet build DealerManager.slnx
dotnet test DealerManager.Tests/DealerManager.Tests.csproj
```

Tests exercise the real BaseRepository and UnitOfWork with SQLite, plus the real API host with a test-only DbContext replacement. They do not read or alter the configured PostgreSQL database. Serialization-error mapping is tested using a simulated PostgreSQL error; simultaneous transactions against a live PostgreSQL server have not been exercised locally.
