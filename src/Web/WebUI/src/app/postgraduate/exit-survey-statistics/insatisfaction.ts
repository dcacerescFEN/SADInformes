import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

export interface InsatisfactionRow { professor: string; count: number; professorCode: string; period: string; studentType: string; modality: string; }
export interface TeacherDetail { professor: string; motives: string[]; }

@Component({
  selector: 'app-insatisfaction',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <p class="text-body-secondary">¿Tuvo profesores que lo dejaron insatisfecho? Indique el profesor para revisar los motivos.</p>
    @if (rows().length > 0) {
      <table class="table table-hover align-middle app-table" aria-label="Profesores con insatisfacción">
        <thead class="table-light"><tr><th>Profesor</th><th class="text-center">Cantidad</th><th>Detalle</th></tr></thead>
        <tbody>@for (row of rows(); track row.professorCode) {
          <tr><td>{{ row.professor }}</td><td class="text-center">{{ row.count }}</td>
            <td><button type="button" class="btn btn-sm btn-outline-primary" [attr.data-detail]="row.professorCode" (click)="showDetail.emit(row)">Ver motivos</button></td></tr>
        }</tbody>
      </table>
    } @else {
      <p class="text-body-secondary" role="status">No existen datos disponibles.</p>
    }
    @if (detailLoading()) { <p role="status">Cargando motivos…</p> }
    @if (detailError()) { <div class="alert alert-warning" role="alert">{{ detailError() }}</div> }
    @if (detail(); as value) {
      <div class="form-section mt-3" data-teacher-detail>
        <h3 class="h6">Motivos de {{ value.professor }}</h3>
        @if (value.motives.length > 0) {
          <ul>@for (motive of value.motives; track $index) {<li>{{ motive }}</li>}</ul>
        } @else {
          <p class="text-body-secondary" role="status">Sin motivos registrados.</p>
        }
      </div>
    }
  `,
})
export class Insatisfaction {
  readonly rows = input.required<InsatisfactionRow[]>();
  readonly detail = input<TeacherDetail | null>(null);
  readonly detailLoading = input(false);
  readonly detailError = input('');
  readonly showDetail = output<InsatisfactionRow>();
}
