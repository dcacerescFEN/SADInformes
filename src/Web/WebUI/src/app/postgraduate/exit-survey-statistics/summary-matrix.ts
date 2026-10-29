import { ChangeDetectionStrategy, Component, input } from '@angular/core';

export interface SummaryRow { section: string; title: string; alternative1: number; alternative2: number; alternative3: number; alternative4: number; alternative5: number; total: number; dimension: number | null; }
const decimal = new Intl.NumberFormat('es-CL', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const LABELS = ['(1) Muy en desacuerdo', '(2) En desacuerdo', '(3) Ni de acuerdo ni en desacuerdo', '(4) De acuerdo', '(5) Muy de acuerdo'];

@Component({
  selector: 'app-summary-matrix',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (rows().some(row => row.total > 0)) {
      <div class="table-responsive">
        <table class="table table-hover align-middle app-table" aria-label="Resumen de dimensiones">
          <thead class="table-light"><tr><th>Dimensiones</th>@for (label of labels; track label) {<th class="text-center">{{ label }}</th>}<th class="text-center">Evaluación Dimensión</th></tr></thead>
          <tbody>@for (row of rows(); track row.section) {
            <tr><td>{{ row.title }}</td>
              <td class="text-center">{{ percent(row.alternative1, row.total) }}</td><td class="text-center">{{ percent(row.alternative2, row.total) }}</td>
              <td class="text-center">{{ percent(row.alternative3, row.total) }}</td><td class="text-center">{{ percent(row.alternative4, row.total) }}</td>
              <td class="text-center">{{ percent(row.alternative5, row.total) }}</td><td class="text-center">{{ dimension(row.dimension) }}</td></tr>
          }</tbody>
        </table>
      </div>
    } @else {
      <p class="text-body-secondary" role="status">No existen datos disponibles.</p>
    }
  `,
})
export class SummaryMatrix {
  readonly rows = input.required<SummaryRow[]>();
  protected readonly labels = LABELS;
  protected percent(value: number, total: number): string { return total === 0 ? '' : decimal.format(value * 100 / total) + '%'; }
  protected dimension(value: number | null): string { return value === null ? '' : decimal.format(value); }
}
