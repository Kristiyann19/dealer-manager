import { Routes } from '@angular/router';
import { DashboardService } from './features/dashboard/services/dashboard.service';
import { MockDashboardService } from './features/dashboard/services/mock-dashboard.service';

const placeholder = () =>
  import('./shared/components/module-placeholder/module-placeholder.component').then(
    (module) => module.ModulePlaceholderComponent,
  );

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./core/layout/app-shell/app-shell.component').then(
        (module) => module.AppShellComponent,
      ),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        title: 'ui.dashboard',
        loadComponent: () =>
          import('./features/dashboard/pages/dashboard/dashboard.component').then(
            (module) => module.DashboardComponent,
          ),
        providers: [{ provide: DashboardService, useClass: MockDashboardService }],
      },
      {
        path: 'candidates',
        loadChildren: () =>
          import('./features/candidates/candidates.routes').then((m) => m.CANDIDATE_ROUTES),
      },
      {
        path: 'vehicles/:id',
        title: 'nav.vehicles',
        loadComponent: () =>
          import('./features/vehicles/vehicle-details.component').then(
            (m) => m.VehicleDetailsComponent,
          ),
      },
      {
        path: 'vehicles',
        title: 'nav.vehicles',
        loadComponent: () =>
          import('./features/vehicles/vehicle-list.component').then((m) => m.VehicleListComponent),
      },
      {
        path: 'sales',
        title: 'nav.sales',
        loadComponent: placeholder,
        data: { title: 'nav.sales', icon: 'handshake' },
      },
      {
        path: 'customers',
        title: 'nav.customers',
        loadComponent: placeholder,
        data: { title: 'nav.customers', icon: 'users' },
      },
      {
        path: 'finances',
        title: 'nav.finances',
        loadComponent: () =>
          import('./features/finance/pages/finances.component').then((m) => m.FinancesComponent),
      },
      {
        path: 'reports',
        title: 'nav.reports',
        loadComponent: placeholder,
        data: { title: 'nav.reports', icon: 'chart-no-axes-combined' },
      },
      {
        path: 'tasks',
        title: 'nav.tasks',
        loadComponent: placeholder,
        data: { title: 'nav.tasks', icon: 'list-checks' },
      },
      {
        path: 'settings',
        title: 'nav.settings',
        loadComponent: () =>
          import('./features/settings/settings.component').then((m) => m.SettingsComponent),
      },
      { path: '**', redirectTo: 'dashboard' },
    ],
  },
];
