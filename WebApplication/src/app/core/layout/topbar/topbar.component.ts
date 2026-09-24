import { ChangeDetectionStrategy, Component, inject, output, signal } from '@angular/core';
import { LucideDynamicIcon } from '@lucide/angular';
import { DialogModule } from 'primeng/dialog';
import { TooltipModule } from 'primeng/tooltip';
import { WorkspaceService } from '../../services/workspace.service';
import { NavigationEnd, Router, RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { filter, map, startWith } from 'rxjs';

@Component({
  selector: 'app-topbar',
  imports: [LucideDynamicIcon, DialogModule, TooltipModule, RouterLink],
  templateUrl: './topbar.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TopbarComponent {
  private readonly router = inject(Router);
  protected readonly isDashboard = toSignal(
    this.router.events.pipe(
      filter((event) => event instanceof NavigationEnd),
      map(() => this.router.url.startsWith('/dashboard')),
      startWith(this.router.url.startsWith('/dashboard')),
    ),
  );
  protected readonly workspace = inject(WorkspaceService);
  readonly toggleMenu = output<void>();
  protected readonly dialog = signal<'notifications' | 'profile' | null>(null);
}
