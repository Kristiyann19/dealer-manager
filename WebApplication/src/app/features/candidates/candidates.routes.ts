import { Routes } from '@angular/router';

export const CANDIDATE_ROUTES: Routes = [
  {
    path: '',
    pathMatch: 'full',
    title: 'Candidates | AutoCapital',
    loadComponent: () =>
      import('./pages/candidate-list.component').then((m) => m.CandidateListComponent),
  },
  {
    path: 'new',
    title: 'New candidate | AutoCapital',
    loadComponent: () =>
      import('./pages/candidate-create.component').then((m) => m.CandidateCreateComponent),
  },
  {
    path: ':id',
    title: 'Candidate details | AutoCapital',
    loadComponent: () =>
      import('./pages/candidate-details.component').then((m) => m.CandidateDetailsComponent),
  },
];
