import { ChangeDetectionStrategy, Component, input } from '@angular/core';

export interface ScaleRow { key: string; description: string; alternatives: number[]; average: number | null; total: number; }
export interface ScaleData { section: string; title: string; labels: string[]; rows: ScaleRow[]; totals: number[]; }
const decimal = new Intl.NumberFormat('es-CL', { minimumFractionDigits: 1, maximumFractionDigits: 1 });

@Component({
  selector: 'app-multi-scale-matrix',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (data().rows.length > 0) {
      <div class="table-responsive">
        <table class="table table-hover align-middle app-table" [attr.aria-label]="data().title">
          <thead class="table-light"><tr><th>Pregunta</th>@for (label of data().labels; track label) {<th class="text-center">{{ label }}</th>}<th class="text-center">Promedio</th></tr></thead>
          <tbody>
            @for (row of data().rows; track row.key) {
              <tr><td>{{ row.description }}</td>
                @for (value of row.alternatives; track $index) {
                  <td class="text-center">{{ value }}<br><strong>{{ percent(value, row.total) }}</strong></td>
                }
                <td class="text-center">{{ average(row.average) }}</td>
              </tr>
            }
          </tbody>
          <tfoot class="table-light fw-semibold"><tr><td>Totales</td>@for (value of data().totals; track $index) {<td class="text-center">{{ value }}</td>}<td></td></tr></tfoot>
        </table>
      </div>
    } @else {
      <p class="text-body-secondary" role="status">No existen datos disponibles.</p>
    }
  `,
})
export class MultiScaleMatrix {
  readonly data = input.required<ScaleData>();
  protected percent(value: number, total: number): string {
    return decimal.format(total === 0 ? 0 : value * 100 / total) + '%';
  }
  protected average(value: number | null): string { return value === null ? '' : decimal.format(value); }
}
