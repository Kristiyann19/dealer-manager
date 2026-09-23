import { ChangeDetectionStrategy, Component, computed, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { SelectModule } from 'primeng/select';
import { BaseChartDirective, provideCharts } from 'ng2-charts';
import {
  BarController,
  BarElement,
  CategoryScale,
  ChartData,
  ChartOptions,
  Legend,
  LinearScale,
  LineController,
  LineElement,
  PointElement,
  Tooltip,
} from 'chart.js';
import { MonthlyFinancialData } from '../../models/dashboard.models';

@Component({
  selector: 'app-financial-chart',
  imports: [BaseChartDirective, SelectModule, FormsModule],
  providers: [
    provideCharts({
      registerables: [
        BarController,
        BarElement,
        CategoryScale,
        LinearScale,
        LineController,
        LineElement,
        PointElement,
        Tooltip,
        Legend,
      ],
    }),
  ],
  templateUrl: './financial-chart.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FinancialChartComponent {
  readonly data = input.required<MonthlyFinancialData[]>();
  protected readonly months = signal(6);
  protected readonly periods = [
    { label: 'Last 6 months', value: 6 },
    { label: 'Last 3 months', value: 3 },
  ];
  protected readonly visibleData = computed(() => this.data().slice(-this.months()));
  protected readonly chartData = computed<ChartData<'bar' | 'line'>>(() => ({
    labels: this.visibleData().map((month) => month.month),
    datasets: [
      {
        type: 'bar',
        label: 'Capital Invested',
        data: this.visibleData().map((month) => month.capitalInvested),
        backgroundColor: '#b9d3fb',
        borderRadius: 3,
        maxBarThickness: 23,
        order: 2,
      },
      {
        type: 'bar',
        label: 'Sales Revenue',
        data: this.visibleData().map((month) => month.salesRevenue),
        backgroundColor: '#3976df',
        borderRadius: 3,
        maxBarThickness: 23,
        order: 2,
      },
      {
        type: 'line',
        label: 'Net Profit',
        data: this.visibleData().map((month) => month.netProfit),
        borderColor: '#159b73',
        backgroundColor: '#159b73',
        tension: 0.35,
        borderWidth: 2,
        pointRadius: 3,
        pointBackgroundColor: '#fff',
        order: 1,
      },
    ],
  }));
  protected readonly options: ChartOptions<'bar' | 'line'> = {
    responsive: true,
    maintainAspectRatio: false,
    interaction: { mode: 'index', intersect: false },
    plugins: {
      legend: { display: false },
      tooltip: {
        padding: 12,
        backgroundColor: '#17253d',
        callbacks: {
          label: (context) =>
            context.dataset.label +
            ': ' +
            new Intl.NumberFormat('en-IE', {
              style: 'currency',
              currency: 'EUR',
              maximumFractionDigits: 0,
            }).format(context.parsed.y ?? 0),
        },
      },
    },
    scales: {
      x: {
        grid: { display: false },
        border: { display: false },
        ticks: { color: '#8290a5', font: { size: 11 } },
      },
      y: {
        beginAtZero: true,
        border: { display: false },
        grid: { color: '#edf1f6' },
        ticks: {
          color: '#8290a5',
          font: { size: 10 },
          callback: (value) => '€' + Number(value) / 1000 + 'k',
        },
      },
    },
  };
}
