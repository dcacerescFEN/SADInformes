import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { GlossaryEntry } from './teacher-survey-glossary';

/** Glosa estática de preguntas y escala; no se pagina para no ocultar filas. */
@Component({
  selector: 'app-glossary-table',
  template: `
    <div class="table-responsive">
      <table class="table table-sm align-middle app-table mb-0">
        <thead>
          <tr><th scope="col">{{ firstColumn() }}</th><th scope="col">Descripción</th></tr>
        </thead>
        <tbody>
          @for (entry of entries(); track entry.code) {
            <tr><td>{{ entry.code }}</td><td>{{ entry.description }}</td></tr>
          }
        </tbody>
      </table>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GlossaryTable {
  readonly entries = input<readonly GlossaryEntry[]>([]);
  readonly firstColumn = input('Pregunta');
}
