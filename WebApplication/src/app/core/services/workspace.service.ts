import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class WorkspaceService {
  readonly searchQuery = signal('');
}
