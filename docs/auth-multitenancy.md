# Authentication and dealership isolation (backend v1)

ASP.NET Core Identity uses the existing `DealerManagerDbContext` and EF stores with integer keys. `ApplicationUser : IdentityUser<int>` belongs to one `Dealership` (Id, Name, CreatedAt, Users). The only role is `Owner`, seeded by the migration. No employee management or Angular authentication UI is included.

## HTTP contract

| Method | Endpoint | Result |
| --- | --- | --- |
| GET | `/api/auth/csrf` | Anonymous bootstrap; stores antiforgery cookies and returns `{ token }` |
| POST | `/api/auth/register` | `{ email, password, confirmPassword, dealershipName }`; 201 and signed-in cookie |
| POST | `/api/auth/login` | `{ email, password }`; 200 and signed-in cookie, or generic 401 |
| POST | `/api/auth/logout` | Authenticated; clears authentication cookie, 204 |
| GET | `/api/auth/me` | Authenticated; `{ id, email, dealership: { id, name }, roles }` |

All unsafe HTTP methods require `X-XSRF-TOKEN`, including registration, login and logout. First GET `/api/auth/csrf` using the same cookie jar, then pass its token in the header. GET it again after registration/login/logout because antiforgery tokens are identity-bound. The readable `XSRF-TOKEN` cookie supports Angular's standard same-origin XSRF integration. Missing/invalid tokens produce 400. Authentication cookie and antiforgery session cookie remain HttpOnly.

Register validates inputs and creates the dealership, Identity user and Owner assignment in one transaction using the existing UnitOfWork. Failure rolls back the dealership; sign-in happens after commit. Email uniqueness is case-insensitive through Identity normalization and a unique database index. Duplicate email returns 409. Passwords use Identity hashing and the default complexity requirements with a minimum length of 12. Five unsuccessful password attempts lock the account for 15 minutes. Passwords, hashes and security stamps are never included in response DTOs.

`DealerManager.Auth` is HttpOnly, SameSite=Lax, with an eight-hour sliding authentication ticket and a session cookie (no persistent remember-me). Secure is mandatory outside Development/Testing; locally it follows HTTPS. API challenges return 401 and forbidden responses 403 without HTML redirects. All business controllers require authenticated Owner membership and a positive dealership ID. Me/logout require authentication only. Security stamp, roles and dealership membership are refreshed from Identity on every authenticated request.

Production must use HTTPS and a durable, protected ASP.NET Core Data Protection key ring; replicas must share the key ring/application configuration. No custom cryptography or token storage is used. No permissive CORS configuration was introduced. Prefer serving Angular and API through the same origin/proxy. Swagger UI was not added; API clients can use the cookie jar + CSRF contract above. Relevant framework guidance: [ASP.NET Core antiforgery](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0).

## Tenant ownership and queries

`ICurrentUserContext` reads UserId and DealershipId from the authenticated Identity principal. The dealership claim is created server-side from ApplicationUser; body/query/header tenant IDs never determine ownership.

Candidate, Vehicle, CapitalAccount and FinancialTransaction have required `DealershipId`. Child entities use their Candidate or Vehicle relationship, including estimate items, photos, cost plans, expenses, status history, listings and sales. They do not duplicate the tenant column.

EF global query filters are the single read-filtering strategy, including direct child queries, lists, details and Dashboard aggregates. A context without an authenticated tenant sees no business records. Identity users deliberately have no tenant query filter: anonymous login must find an account. There is no public user/dealership CRUD API.

SaveChanges stamps new root records with the current dealership and validates modified/deleted records and business foreign keys using filtered queries. This also protects detached update/delete operations and reassignment of child foreign keys. Composite database foreign keys reinforce same-dealership Vehicle → Candidate and FinancialTransaction → CapitalAccount/Vehicle relationships. A supplied foreign capital account cannot fund purchases, expenses, planned payments or sales. Foreign resource lookups return the same 404 as absent resources; existing business validation of incompatible payment-account selections retains its conflict response.

Dashboard reads the filtered DbSets, so cash, investments, stock, profit, monthly series and lists include only the current dealership. Existing business calculations and lifecycle rules are unchanged. Do not bypass filters with IgnoreQueryFilters, raw SQL or bulk writes in future application features; these bypass the normal query/save safeguards and need an explicit security review.

## Migration and existing development data

Migration: `20261001093412_AddIdentityAndDealershipTenancy`.

It creates Identity/Dealership tables, seeds Owner, adds required tenant columns, updates foreign keys and indexes, and preserves existing business IDs/records. Existing roots are assigned to the reserved **`Legacy — unassigned` dealership with ID -1**, with no users. This is an isolated holding dealership, not ownership granted to any registering user. The temporary column defaults used during migration are removed. New registrations create positive dealership IDs and cannot see legacy records. The negative ID is reserved for this purpose.

**The migration has not been applied to the working application database. No reset was performed.** Before applying it, back up that database and review the generated SQL. Stop the old API during the deployment, then apply the migration and restart the new backend:

```powershell
dotnet ef database update --project DealerManager.Infrastructure --startup-project DealerManager.WebAPI
```

Use a .NET 10-compatible EF tool. This command is an operator step, not an application startup action. For a disposable development database, creating a separate empty database and applying migrations is another option; deleting the current database requires an explicit decision.

To retain and expose legacy records, first register the intended owner, then explicitly identify which records belong to that dealership. Transfer them through a reviewed one-off data migration/transaction that handles all four root tables and their composite foreign keys together. Do not assign everything automatically to the first registered user, and do not transfer each root table independently under immediate composite constraints. Historical child records remain linked through their existing parents. Restoring the backup is preferable to blindly running Down after new authentication data has been created.

Until the migration and new API deployment happen, an already-running older server continues using its older behavior. After deployment, the existing Angular client needs the authentication phase below before protected requests can succeed.

## Verification

The suite includes the 162 existing business regression cases, 14 real-cookie authentication cases, two multi-scenario tenant-isolation cases, and one optional PostgreSQL migration case (179 total).

Authentication tests cover atomic registration/rollback, duplicate email, Owner assignment, cookie flags, successful/invalid login, logout, me, anonymous 401, role revocation 403 and missing CSRF tokens. Tenant tests use two actual registrations and separate cookies to verify own/foreign Candidate and Vehicle access, estimates, account histories/contributions, purchase accounts, expenses, planned payments, listing/sale, lists, dashboard totals, forged body tenant IDs, detached writes and anonymous database access.

```powershell
dotnet build DealerManager.slnx --no-restore
dotnet test DealerManager.Tests --no-restore
```

Set `DEALER_TENANCY_POSTGRES` to an accessible PostgreSQL connection string to enable the migration test. It creates a unique schema inside a transaction, builds the previous schema, inserts representative legacy records, applies the new migration there, checks preservation/Owner/defaults/composite-FK rejection and rolls everything back. It does not migrate the application's existing schema. Without that variable, this one test is explicitly skipped.

The older `DEALER_DASHBOARD_POSTGRES` lifecycle mode now requires an already migrated test database. It uses a test-only authenticated Owner and an outer rollback transaction; never use it as a production test runner. The ordinary business fixtures use test authentication; the new auth/isolation tests use the actual Identity cookie handler. Ephemeral Data Protection is confined to tests.

## Next Angular phase

Add Login/Register pages, initial CSRF bootstrap and post-login token renewal, current-user state from `/me`, logout, route guards, 401/403 handling, and translated profile/dealership information. Use cookies and same-origin API requests; do not store authentication tokens in localStorage. Backend authorization remains authoritative regardless of route guards.
