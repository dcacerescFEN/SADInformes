import { ChangeDetectionStrategy, Component, input } from '@angular/core';

export interface LikertRow {
  question: string; description: string;
  alternative1: number; alternative2: number; alternative3: number; alternative4: number; alternative5: number;
  average: number | null; total: number;
}
export interface LikertTotals { alternative1: number; alternative2: number; alternative3: number; alternative4: number; alternative5: number; }
const LABELS = ['Muy en desacuerdo', 'En desacuerdo', 'Ni de acuerdo ni en desacuerdo', 'De acuerdo', 'Muy de acuerdo'];
const decimal = new Intl.NumberFormat('es-CL', { minimumFractionDigits: 1, maximumFractionDigits: 1 });

@Component({
  selector: 'app-likert-matrix',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (rows().length > 0) {
      <div class="table-responsive">
        <table class="table table-hover align-middle app-table" [attr.aria-label]="title()">
          <thead class="table-light"><tr><th>Pregunta</th>@for (label of labels; track label) {<th class="text-center">{{ label }}</th>}<th class="text-center">Promedio</th></tr></thead>
          <tbody>
            @for (row of rows(); track row.question + '|' + row.description) {
              <tr><td>{{ row.description }}</td>
                <td class="text-center">{{ row.alternative1 }}<br><strong>{{ percent(row.alternative1, row.total) }}</strong></td>
                <td class="text-center">{{ row.alternative2 }}<br><strong>{{ percent(row.alternative2, row.total) }}</strong></td>
                <td class="text-center">{{ row.alternative3 }}<br><strong>{{ percent(row.alternative3, row.total) }}</strong></td>
                <td class="text-center">{{ row.alternative4 }}<br><strong>{{ percent(row.alternative4, row.total) }}</strong></td>
                <td class="text-center">{{ row.alternative5 }}<br><strong>{{ percent(row.alternative5, row.total) }}</strong></td>
                <td class="text-center">{{ average(row.average) }}</td>
              </tr>
            }
          </tbody>
          <tfoot class="table-light fw-semibold"><tr><td>Totales</td><td class="text-center">{{ totals().alternative1 }}</td><td class="text-center">{{ totals().alternative2 }}</td><td class="text-center">{{ totals().alternative3 }}</td><td class="text-center">{{ totals().alternative4 }}</td><td class="text-center">{{ totals().alternative5 }}</td><td></td></tr></tfoot>
        </table>
      </div>
    } @else {
      <p class="text-body-secondary" role="status">No existen datos disponibles.</p>
    }
  `,
})
export class LikertMatrix {
  readonly title = input.required<string>();
  readonly rows = input.required<LikertRow[]>();
  readonly totals = input.required<LikertTotals>();
  protected readonly labels = LABELS;
  protected percent(value: number, total: number): string {
    return decimal.format(total === 0 ? 0 : value * 100 / total) + '%';
  }
  protected average(value: number | null): string { return value === null ? '' : decimal.format(value); }
}
