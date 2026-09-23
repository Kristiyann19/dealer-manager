import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class WorkspaceService {
  readonly searchQuery = signal('');
  // Temporary display identity, not an authenticated user.
  readonly user = { name: 'Boris Marinov', role: 'Admin / Manager', initials: 'BM' };
}
