import { ChangeDetectionStrategy, Component, inject, output } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { LucideDynamicIcon } from '@lucide/angular';
import { WorkspaceService } from '../../services/workspace.service';

@Component({
  selector: 'app-sidebar',
  imports: [RouterLink, RouterLinkActive, LucideDynamicIcon],
  templateUrl: './sidebar.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SidebarComponent {
  protected readonly workspace = inject(WorkspaceService);
  readonly navigate = output<void>();
  protected readonly links = [
    { label: 'Dashboard', path: '/dashboard', icon: 'layout-dashboard' },
    { label: 'Candidates', path: '/candidates', icon: 'clipboard-list' },
    { label: 'Vehicles', path: '/vehicles', icon: 'car-front' },
    { label: 'Sales', path: '/sales', icon: 'handshake' },
    { label: 'Customers', path: '/customers', icon: 'users' },
    { label: 'Finances', path: '/finances', icon: 'wallet' },
    { label: 'Reports', path: '/reports', icon: 'chart-no-axes-combined' },
    { label: 'Tasks', path: '/tasks', icon: 'list-checks' },
  ];
}
