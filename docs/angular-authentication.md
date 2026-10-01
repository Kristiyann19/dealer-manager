# Angular authentication V1

## Files and state

New frontend files:

- `src/app/core/models/auth.models.ts`: exact backend DTOs, integer IDs; no invented profile fields.
- `src/app/core/services/auth.service.ts`: in-memory current user, initialization, CSRF, login/register/logout and centralized notices.
- `src/app/core/services/auth.service.spec.ts`: authentication, routing, XSRF and profile tests.
- `src/app/auth-guards/auth.guard.ts`: authenticated and guest guards.
- `src/app/interceptors/auth.interceptor.ts`: API 401/403 handling.
- `src/app/features/auth/auth-page.component.ts` and `.html`: shared reactive Login/Register page.

App config enables standard Angular XSRF and the auth error interceptor. Root initialization displays a translated loading state until CSRF → me completes. Network/server failure displays a retry action instead of pretending the user is anonymous. Guards share the same in-flight initialization. There is no storage of auth data in localStorage/sessionStorage, no Bearer token and no tenant ID added to business requests.

Login/register run CSRF → POST → fresh CSRF → me → dashboard. Registration already signs in on the backend. Logout runs CSRF → POST → fresh CSRF → clear user → login. If logout succeeded but token renewal fails, the user is still cleared. An already-expired logout session is handled as signed out. The browser owns the HttpOnly authentication cookie; Angular's built-in XSRF handling reads only XSRF-TOKEN and adds X-XSRF-TOKEN to unsafe relative requests.

On browser refresh, me restores the cookie session. A bootstrap me 401 means anonymous. A protected API 401 clears state and redirects once; concurrent failures are completed without repeated component errors. A 403 displays the translated permission notice, preserves the session and propagates the error so component loading states settle. A CSRF 400 remains an ordinary error with no automatic mutation retry. Login 401 and registration 409 have specific translated messages; registration 400 can display the backend validation detail.

`/login` and `/register` are outside the sidebar/topbar shell and use guest guards. The shell and all child business routes are guarded. Existing `/finances` remains; `/finance` redirects to it. Sidebar and profile dialog use the shared user's email, dealership name and translated Owner role; logout is in the profile dialog. No component independently fetches me. Shared styles support light/dark mode and both languages. Existing business services and the `/api` development proxy remain unchanged.

## Local backend prerequisite

No backend source files, database records or migrations were changed/applied by this frontend task. The running backend on port 5296 returned **404 for `/api/auth/csrf`** during verification, so it was still serving the pre-auth version. Full browser end-to-end verification requires deploying the already-created backend migration and restarting the API.

Back up the database and follow `auth-multitenancy.md` for legacy ownership. Stop the old API instance. From the repository root, using an EF CLI compatible with .NET 10:

```powershell
dotnet ef database update --project DealerManager.Infrastructure --startup-project DealerManager.WebAPI
dotnet run --project DealerManager.WebAPI --launch-profile http
```

If the globally installed EF tool is still version 8, update it to version 10 first (`dotnet tool update --global dotnet-ef --version 10.0.12`). Do not reset the database. Existing legacy data is isolated and is not automatically assigned to the first registration.

In a separate terminal:

```powershell
cd WebApplication
pnpm start --port 4201
```

Use `http://localhost:4201` throughout; the existing proxy forwards `/api` to `http://localhost:5296`. Do not mix localhost and 127.0.0.1 for cookies. Production must reverse-proxy `/api` under the frontend origin and use HTTPS.

## Manual verification with two dealerships

1. Open an incognito browser at `http://localhost:4201/dashboard`: expect `/login`, without the application sidebar. Loading should precede routing without a login/dashboard flash.
2. Open `/register`. Register dealership **BM Auto**, email **boris@example.com**, and your own valid password (at least 12 characters, upper/lowercase, digit, special character). Expect dashboard immediately; profile/sidebar show BM Auto and the email. New business data and balances are empty/zero.
3. Create a Candidate. Confirm it appears; refresh the browser and confirm the session and record remain. Visit `/login` while signed in: expect dashboard.
4. Open the avatar/profile dialog and select Sign out. Expect `/login`. Navigate to `/vehicles`: expect `/login` again.
5. Sign in as Boris: the same candidate and dealership are shown.
6. Sign out. Register **Ivan Auto**, **ivan@example.com**, with a separate valid password. Expect an empty dashboard, no BM Auto candidates, vehicles, accounts or financial totals. Try Boris's candidate URL: expect the normal not-found result.
7. Sign out and sign in as Boris: BM Auto records remain visible. Verify BG/EN switching, invalid credentials, password confirmation validation and disabled submit during a pending request.

## Checks

Run in WebApplication: `pnpm check:i18n`, `pnpm test`, `pnpm build`. HTTP tests mock API responses; they do not replace the real browser/two-account scenario above. The production build currently emits an initial-bundle budget warning (about 654 kB versus the 500 kB warning threshold); it succeeds without TypeScript/template errors. No new packages or auth state-management framework were added.
