# AutoCapital frontend

## Candidate module and API connection

The `/candidates` module uses the real WebAPI: server-side search and pagination, candidate creation, details, estimate versions, approval and rejection. The dashboard still uses sample data.

Start the server from the solution root with `dotnet run --project DealerManager.WebAPI --launch-profile http`, then run `pnpm start --port 4201` from `WebApplication`. Open `/candidates` on the frontend. The existing PostgreSQL database must be configured and available.

`proxy.conf.cjs` forwards `/api/**` to `http://localhost:5296`. To use another local API address, set `DEALER_API_URL` before starting Angular. **Restart `ng serve` after adding or changing proxy configuration**; hot reload does not apply it. A `404` on candidate creation, or HTML returned from `/api/candidates`, can mean an older dev server is still running without the proxy.

Production hosting must reverse-proxy `/api` to WebAPI as well as serve Angular routes. `src/app/configuration/api.config.ts` contains the injectable API base URL.

Run `pnpm test` for the Candidate HTTP/form tests and `pnpm build` for production compilation. Candidate estimates and financial summaries come from the API; no client estimates change capital. Purchasing, editing, deletion and photo uploads are not offered by the current API.

Angular 22 standalone dealership dashboard using PrimeNG 22, Tailwind CSS 4, `@lucide/angular`, Chart.js 4 and ng2-charts 10. Angular strict TypeScript and template checking are enabled.

## Run locally

Use Node.js 24.19.0 and pnpm 11.19.0. From `WebApplication`:

```sh
pnpm install --frozen-lockfile
pnpm start
```

Open http://localhost:4200/dashboard. If port 4200 is occupied, run `pnpm start --port 4201`.

```sh
pnpm build
```

Production files are emitted to `dist/dealer-manager-client/browser`. Hosting must fall back to `index.html` for client routes.

## Structure

- `core/layout`: lazy-loaded app shell, responsive sidebar and topbar.
- `core/services/workspace.service.ts`: shared dashboard search and temporary display profile; not authentication.
- `shared/components`: KPI card, currency display, status badge and a common placeholder page.
- `shared/models`: display status types, independent of future API enums.
- `features/dashboard/pages/dashboard`: loads data and handles loading, error and retry states.
- `features/dashboard/components`: monthly overview, financial position, operational pipeline, recent candidates, active vehicles and mixed bar/line chart.
- `features/dashboard/models/dashboard.models.ts`: strongly typed dashboard response contract.
- `features/dashboard/services`: service contract, mock implementation and the single mock data source.
- `app.routes.ts`: all nine routes, lazy components and the dashboard service provider.
- `app.config.ts`: PrimeNG theme, selected Lucide icons and router setup.
- `src/styles.css` / `.postcssrc.json`: global design styles and Tailwind configuration.

## Mock data and future API integration

The dependency chain is:

```text
DashboardComponent -> DashboardService -> MockDashboardService -> mock-dashboard.data.ts
```

`DashboardService.getDashboard()` returns `Observable<DashboardSummary>`. The page uses `toSignal`; visual components receive typed inputs and never import mock data. Each mock subscription receives a fresh clone with a short asynchronous delay. Summary totals cover the whole dealership; the recent/active lists are subsets.

When the ASP.NET Core API is ready:

1. Create `DashboardApiService extends DashboardService` in `features/dashboard/services`.
2. Implement `getDashboard()` using `HttpClient`, mapping the API response to `DashboardSummary` where needed.
3. Replace `useClass: MockDashboardService` with `useClass: DashboardApiService` in the `/dashboard` route provider in `app.routes.ts`.
4. Register `provideHttpClient()` in `app.config.ts` and configure the real API URL.

No dashboard component needs to change. Totals, profit, ROI and progress are supplied in the read model; they are not calculated in HTML. `Pending Inspection` is a prototype display status and will need mapping to the eventual server workflow. All IDs are integers.

## Prototype interactions

- Search filters dashboard candidates and active vehicles by model/VIN; candidates also accept their numeric ID.
- PrimeNG status selection filters recent candidates. Review opens a read-only dialog.
- The chart switches between three and six months and includes an accessible data table.
- Refresh reloads the data through the service contract.
- All sidebar routes work; modules other than Dashboard and Candidates display a placeholder.
- New Candidate opens the working creation form. Notifications and profile show informational dialogs.

Candidates are connected to the backend. Dashboard integration, authentication, transactions and uploads remain future work. Estimated values do not move capital.

## PrimeNG license

PrimeNG 22 and its theme package require a valid PrimeUI license key. No key is included, no license is activated, and the library's license notice is left intact. Configure your eligible Community or Commercial license through the official PrimeNG provider setup before using it beyond this prototype.

- https://primeng.dev/installation
- https://primeui.dev/licenses/community

The current build and UI work, with the vendor's missing-license notice.

## Verification

Production compilation runs strict TypeScript/template checks. Browser checks cover dashboard rendering, all sidebar routes, model search, combined status filtering, empty states, Review, chart period selection and mobile navigation. The only expected runtime warning is the missing PrimeUI license.
