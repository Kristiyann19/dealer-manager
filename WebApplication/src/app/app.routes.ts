import { authGuard, guestGuard } from './auth-guards/auth.guard';
import { Routes } from '@angular/router';

const placeholder = () =>
  import('./shared/components/module-placeholder/module-placeholder.component').then(
    (module) => module.ModulePlaceholderComponent,
  );

export const routes: Routes = [
  {
    path: 'login',
    title: 'auth.login.title',
    canActivate: [guestGuard],
    loadComponent: () =>
      import('./features/auth/auth-page.component').then((m) => m.AuthPageComponent),
  },
  {
    path: 'register',
    title: 'auth.register.title',
    canActivate: [guestGuard],
    data: { register: true },
    loadComponent: () =>
      import('./features/auth/auth-page.component').then((m) => m.AuthPageComponent),
  },
  {
    path: '',
    canActivate: [authGuard],
    canActivateChild: [authGuard],
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
      { path: 'finance', redirectTo: 'finances', pathMatch: 'full' },
      { path: '**', redirectTo: 'dashboard' },
    ],
  },
];
