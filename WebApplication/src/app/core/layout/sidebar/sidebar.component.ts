import { TranslatePipe } from '@ngx-translate/core';
import { ChangeDetectionStrategy, Component, inject, output } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { LucideDynamicIcon } from '@lucide/angular';
import { WorkspaceService } from '../../services/workspace.service';

@Component({
  selector: 'app-sidebar',
  imports: [TranslatePipe, RouterLink, RouterLinkActive, LucideDynamicIcon],
  templateUrl: './sidebar.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SidebarComponent {
  protected readonly workspace = inject(WorkspaceService);
  readonly navigate = output<void>();
  protected readonly links = [
    { label: 'ui.dashboard', path: '/dashboard', icon: 'layout-dashboard' },
    { label: 'ui.candidates', path: '/candidates', icon: 'clipboard-list' },
    { label: 'nav.vehicles', path: '/vehicles', icon: 'car-front' },
    { label: 'nav.sales', path: '/sales', icon: 'handshake' },
    { label: 'nav.customers', path: '/customers', icon: 'users' },
    { label: 'nav.finances', path: '/finances', icon: 'wallet' },
    { label: 'nav.reports', path: '/reports', icon: 'chart-no-axes-combined' },
    { label: 'nav.tasks', path: '/tasks', icon: 'list-checks' },
  ];
}
