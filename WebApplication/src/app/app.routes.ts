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
        path: 'vehicles',
        title: 'nav.vehicles',
        loadComponent: placeholder,
        data: { title: 'nav.vehicles', icon: 'car-front' },
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
        loadComponent: placeholder,
        data: { title: 'nav.finances', icon: 'wallet' },
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
        loadComponent: placeholder,
        data: { title: 'nav.settings', icon: 'settings' },
      },
      { path: '**', redirectTo: 'dashboard' },
    ],
  },
];
