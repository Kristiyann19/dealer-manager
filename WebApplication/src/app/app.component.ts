import { Location } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Router, RouterOutlet } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from './core/services/auth.service';
@Component({
  imports: [RouterOutlet, TranslatePipe],
  selector: 'app-root',
  templateUrl: './app.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppComponent {
  readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly location = inject(Location);
  constructor() {
    this.auth.initialize().subscribe();
  }
  retry() {
    this.auth.initialize().subscribe((ready) => {
      if (ready) void this.router.navigateByUrl(this.location.path() || '/dashboard');
    });
  }
}
