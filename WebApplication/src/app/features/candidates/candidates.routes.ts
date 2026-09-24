import { Routes } from '@angular/router';

export const CANDIDATE_ROUTES: Routes = [
  {
    path: '',
    pathMatch: 'full',
    title: 'ui.candidates',
    loadComponent: () =>
      import('./pages/candidate-list.component').then((m) => m.CandidateListComponent),
  },
  {
    path: 'new',
    title: 'ui.new_candidate_2',
    loadComponent: () =>
      import('./pages/candidate-create.component').then((m) => m.CandidateCreateComponent),
  },
  {
    path: ':id',
    title: 'ui.candidate_overview',
    loadComponent: () =>
      import('./pages/candidate-details.component').then((m) => m.CandidateDetailsComponent),
  },
];
