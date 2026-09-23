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
        title: 'Dashboard | AutoCapital',
        loadComponent: () =>
          import('./features/dashboard/pages/dashboard/dashboard.component').then(
            (module) => module.DashboardComponent,
          ),
        providers: [{ provide: DashboardService, useClass: MockDashboardService }],
      },
      {
        path: 'candidates',
        title: 'Candidates | AutoCapital',
        loadComponent: placeholder,
        data: { title: 'Candidates', icon: 'clipboard-list' },
      },
      {
        path: 'vehicles',
        title: 'Vehicles | AutoCapital',
        loadComponent: placeholder,
        data: { title: 'Vehicles', icon: 'car-front' },
      },
      {
        path: 'sales',
        title: 'Sales | AutoCapital',
        loadComponent: placeholder,
        data: { title: 'Sales', icon: 'handshake' },
      },
      {
        path: 'customers',
        title: 'Customers | AutoCapital',
        loadComponent: placeholder,
        data: { title: 'Customers', icon: 'users' },
      },
      {
        path: 'finances',
        title: 'Finances | AutoCapital',
        loadComponent: placeholder,
        data: { title: 'Finances', icon: 'wallet' },
      },
      {
        path: 'reports',
        title: 'Reports | AutoCapital',
        loadComponent: placeholder,
        data: { title: 'Reports', icon: 'chart-no-axes-combined' },
      },
      {
        path: 'tasks',
        title: 'Tasks | AutoCapital',
        loadComponent: placeholder,
        data: { title: 'Tasks', icon: 'list-checks' },
      },
      {
        path: 'settings',
        title: 'Settings | AutoCapital',
        loadComponent: placeholder,
        data: { title: 'Settings', icon: 'settings' },
      },
      { path: '**', redirectTo: 'dashboard' },
    ],
  },
];
