import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { LucideDynamicIcon } from '@lucide/angular';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';

@Component({
  selector: 'app-module-placeholder',
  imports: [RouterLink, LucideDynamicIcon],
  template: ` <div class="page-heading">
      <div>
        <p class="eyebrow">WORKSPACE</p>
        <h1>{{ routeData()['title'] }}</h1>
      </div>
      <span class="demo-pill">Coming soon</span>
    </div>
    <section class="panel flex min-h-[420px] flex-col items-center justify-center px-6 text-center">
      <div class="rounded-2xl bg-blue-50 p-5 text-blue-600">
        <svg [lucideIcon]="routeData()['icon']" [size]="30" aria-hidden="true"></svg>
      </div>
      <h2 class="mt-6 text-xl font-semibold">{{ routeData()['title'] }}</h2>
      <p class="mt-2 text-sm text-slate-500">This module will be implemented later.</p>
      <a routerLink="/dashboard" class="secondary-button mt-7"
        ><svg lucideIcon="arrow-left" [size]="15" aria-hidden="true"></svg>Back to dashboard</a
      >
    </section>`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ModulePlaceholderComponent {
  protected readonly routeData = toSignal(
    inject(ActivatedRoute).data.pipe(
      map((data) => ({
        title: typeof data['title'] === 'string' ? data['title'] : 'Module',
        icon: typeof data['icon'] === 'string' ? data['icon'] : 'layout-dashboard',
      })),
    ),
    { initialValue: { title: 'Module', icon: 'layout-dashboard' } },
  );
}
