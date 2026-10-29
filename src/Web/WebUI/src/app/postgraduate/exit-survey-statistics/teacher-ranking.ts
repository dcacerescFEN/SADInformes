import { ChangeDetectionStrategy, Component, input } from '@angular/core';

export interface TeacherRow { professor: string; frequency: number; }
export interface TeacherRankingData { theoretical: TeacherRow[]; practical: TeacherRow[]; formative: TeacherRow[]; }
const GROUPS = [
  { key: 'theoretical', label: '(1) Sus conocimientos teóricos' },
  { key: 'practical', label: '(2) Su experiencia práctico-profesional' },
  { key: 'formative', label: '(3) Su capacidad formativo-pedagógica' },
] as const;

@Component({
  selector: 'app-teacher-ranking',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <p class="text-body-secondary">Indique los 3 mejores profesores que ha tenido, considerando cada factor. Un mismo profesor puede aparecer en más de un grupo.</p>
    <div class="row g-3">
      @for (group of groups; track group.key) {
        <div class="col-xl-4 col-md-6" [attr.data-teacher-group]="group.key">
          <h3 class="h6">{{ group.label }}</h3>
          @if (data()[group.key].length > 0) {
            <table class="table table-sm align-middle app-table" [attr.aria-label]="group.label">
              <thead class="table-light"><tr><th>Profesor</th><th class="text-center">Frecuencia</th></tr></thead>
              <tbody>@for (row of data()[group.key]; track row.professor) {<tr><td>{{ row.professor }}</td><td class="text-center">{{ row.frequency }}</td></tr>}</tbody>
            </table>
          } @else {
            <p class="text-body-secondary" role="status">Sin datos.</p>
          }
        </div>
      }
    </div>
  `,
})
export class TeacherRanking {
  readonly data = input.required<TeacherRankingData>();
  protected readonly groups = GROUPS;
}
