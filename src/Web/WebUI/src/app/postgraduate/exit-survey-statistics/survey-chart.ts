import { ChangeDetectionStrategy, Component, input } from '@angular/core';

export interface ChartRow { description: string; counts: number[]; average: number | null; total: number; }
export interface ChartData { title: string; labels: string[]; rows: ChartRow[]; }
const COLORS = ['#2287BA', '#C03535', '#83C469', '#956B9B', '#3FB3BD'];
const decimal = new Intl.NumberFormat('es-CL', { minimumFractionDigits: 1, maximumFractionDigits: 1 });

@Component({
  selector: 'app-survey-chart',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (data().rows.length > 0) {
      <div class="survey-chart" role="img" [attr.aria-label]="'Gráfico ' + data().title">
        <div class="survey-chart__legend">
          @for (label of data().labels; track label; let i = $index) {
            <span><i [style.background]="colors[i]"></i>{{ label }}</span>
          }
        </div>
        @for (row of data().rows; track $index; let rowIndex = $index) {
          <div class="survey-chart__row">
            <span class="survey-chart__label">{{ rowIndex + 1 }}. {{ row.description }}</span>
            <div class="survey-chart__bar">
              @for (count of row.counts; track $index; let i = $index) {
                <span class="survey-chart__segment" [style.width.%]="percent(count, row.total)"
                  [style.background]="colors[i]" [attr.title]="data().labels[i] + ': ' + count + ' (' + decimal.format(percent(count, row.total)) + '%)'"></span>
              }
            </div>
            <span class="survey-chart__dimension">{{ row.average === null ? '' : decimal.format(row.average) }}</span>
          </div>
        }
      </div>
    } @else {
      <p class="text-body-secondary" role="status">No existen datos disponibles.</p>
    }
  `,
  styles: `
    .survey-chart { display: grid; gap: .55rem; overflow-x: auto; padding: .25rem 0; }
    .survey-chart__legend { display: flex; flex-wrap: wrap; gap: .75rem; font-size: .78rem; color: var(--layout-muted-text); }
    .survey-chart__legend i { display: inline-block; width: .8rem; height: .8rem; margin-right: .3rem; border-radius: .2rem; vertical-align: middle; }
    .survey-chart__row { display: grid; grid-template-columns: minmax(12rem, 24rem) minmax(16rem, 1fr) 3.5rem; align-items: center; gap: .75rem; }
    .survey-chart__label { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; font-size: .85rem; }
    .survey-chart__bar { display: flex; height: 1.5rem; overflow: hidden; border-radius: .35rem; background: var(--layout-background); }
    .survey-chart__segment { min-width: 0; }
    .survey-chart__dimension { text-align: right; font-variant-numeric: tabular-nums; font-weight: 600; }
    @media (max-width: 767.98px) { .survey-chart__row { grid-template-columns: minmax(9rem, 12rem) minmax(10rem, 1fr) 3rem; gap: .4rem; } }
  `,
})
export class SurveyChart {
  readonly data = input.required<ChartData>();
  protected readonly colors = COLORS;
  protected percent(value: number, total: number): number { return total === 0 ? 0 : value * 100 / total; }
  protected decimal = decimal;
}
