import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideDynamicIcon } from '@lucide/angular';
import { PipelineSummary } from '../../models/dashboard.models';

@Component({
  selector: 'app-operational-pipeline',
  imports: [RouterLink, LucideDynamicIcon],
  template: ` <section class="panel px-5 py-4" aria-labelledby="pipeline-title">
    <div class="section-heading !mb-4">
      <h2 id="pipeline-title">Operational Pipeline</h2>
      <span class="text-[11px] text-slate-400 hidden sm:block">From opportunity to sale</span>
    </div>
    <div class="pipeline-grid">
      @for (stage of stages(); track stage.label; let last = $last) {
        <a [routerLink]="stage.route" class="pipeline-stage group" [attr.data-tone]="stage.tone">
          <span class="metric-icon"
            ><svg [lucideIcon]="stage.icon" [size]="17" aria-hidden="true"></svg
          ></span>
          <div class="flex-1">
            <p class="text-[11px] text-slate-500">{{ stage.label }}</p>
            <p class="mt-1 text-xl font-semibold text-slate-800 tabular-nums">{{ stage.count }}</p>
          </div>
          @if (!last) {
            <svg
              lucideIcon="chevron-right"
              [size]="14"
              class="text-slate-300"
              aria-hidden="true"
            ></svg>
          }
        </a>
      }
    </div>
  </section>`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OperationalPipelineComponent {
  readonly data = input.required<PipelineSummary>();
  protected readonly stages = computed(() => [
    {
      label: 'Candidates',
      count: this.data().candidates,
      icon: 'clipboard-list',
      tone: 'slate',
      route: '/candidates',
    },
    {
      label: 'Transporting',
      count: this.data().transporting,
      icon: 'truck',
      tone: 'blue',
      route: '/vehicles',
    },
    {
      label: 'Repairing',
      count: this.data().repairing,
      icon: 'wrench',
      tone: 'amber',
      route: '/vehicles',
    },
    {
      label: 'Ready for Sale',
      count: this.data().readyForSale,
      icon: 'circle-check',
      tone: 'green',
      route: '/vehicles',
    },
    { label: 'Listed', count: this.data().listed, icon: 'tag', tone: 'blue', route: '/vehicles' },
  ]);
}
