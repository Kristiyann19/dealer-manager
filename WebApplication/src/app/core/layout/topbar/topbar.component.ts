import { ChangeDetectionStrategy, Component, inject, output, signal } from '@angular/core';
import { LucideDynamicIcon } from '@lucide/angular';
import { DialogModule } from 'primeng/dialog';
import { TooltipModule } from 'primeng/tooltip';
import { WorkspaceService } from '../../services/workspace.service';

@Component({
  selector: 'app-topbar',
  imports: [LucideDynamicIcon, DialogModule, TooltipModule],
  templateUrl: './topbar.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TopbarComponent {
  protected readonly workspace = inject(WorkspaceService);
  readonly toggleMenu = output<void>();
  protected readonly dialog = signal<'candidate' | 'notifications' | 'profile' | null>(null);
}
