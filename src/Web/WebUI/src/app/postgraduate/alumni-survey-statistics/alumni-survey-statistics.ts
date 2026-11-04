import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Search } from '../../shared/components/search';
import { Table, TableColumn } from '../../shared/components/table';

interface Question { code: string; description: string; }
const BASE = '/api/reports/postgraduate/alumni-survey-statistics';
const COLUMNS: readonly TableColumn[] = [
  { key: 'code', name: 'Cod. Pregunta', sortable: true },
  { key: 'description', name: 'Descripción' },
];
const QUESTIONS: readonly Question[] = [
  { code: 'P1', description: 'Situación actual de la carrera laboral o académica' },
  { code: 'P1_1', description: 'Si está empleado: Nombre de Empresa' },
  { code: 'P1_2', description: 'Si está empleado: Cargo' },
  { code: 'P1_3', description: 'Si está empleado: Ciudad' },
  { code: 'P1_4', description: 'Si está empleado: País' },
  { code: 'P2', description: '¿Tenía trabajo el trabajo actual antes de graduarse del programa?' },
  { code: 'P2_1', description: '¿El actual sueldo es más alto que el sueldo que recibía antes de graduarse?' },
  { code: 'P2_2', description: '¿El actual trabajo es en la misma compañía/organización o es una compañía/organización diferente?' },
  { code: 'P2_3', description: '¿Cómo describirías tu actual cargo comparado con el cargo previo?' },
  { code: 'P2_4', description: '¿Cómo describirías tu actual sueldo en comparación con tu posición previa?' },
  { code: 'P3_1', description: 'Con respecto a su emprendimiento, Nombre empresa:' },
  { code: 'P3_2', description: 'Con respecto a su emprendimiento, Año Fundación:' },
  { code: 'P3_3', description: 'Con respecto a su emprendimiento, Rubro:' },
  { code: 'P3_4', description: 'Con respecto a su emprendimiento, Ventas Anuales:' },
  { code: 'P4', description: 'Indique el grado en que la preparación académica en la Escuela le dio una ventaja en comparación con otros trabajadores/compañeros que tienen un postgrado de otras instituciones:' },
  { code: 'P5', description: 'Indique el grado en el que el postgrado que cursó y su experiencia en la Escuela contribuyeron a la preparación para su cargo actual:' },
  { code: 'P6', description: 'Indique el grado en el que el postgrado y su experiencia en la FEN contribuyeron en tu avance a una posición de nivel más alto dentro de la compañía/organización o en otra compañía/organización:' },
  { code: 'P7', description: 'Si está actualmente matriculado o ha terminado estudios de postgrado adicionales, indique el grado en el postgrado y su experiencia en la FEN contribuyeron en la preparación para estos estudios:' },
  { code: 'P8_1', description: 'Si está realizando estudios de postgrado adicionales, complete lo siguiente, Nombre de Grado:' },
  { code: 'P8_2', description: 'Si está realizando estudios de postgrado adicionales, complete lo siguiente, Institución:' },
  { code: 'P9', description: 'Si está desempleado, por favor indique el motivo principal:' },
  { code: 'P10', description: '¿Ha publicado su Tesis o AFE o una parte de ella?' },
  { code: 'P10_1', description: 'Con respecto a su publicación: Nombre de Revista' },
  { code: 'P10_2', description: 'Con respecto a su publicación: Conferencia' },
  { code: 'P10_3', description: 'Con respecto a su publicación: Volumen' },
  { code: 'P10_4', description: 'Con respecto a su publicación: Año' },
  { code: 'P11', description: '¿Recibe información de las actividades y servicios que ofrece ALUMNI?' },
  { code: 'P12', description: '¿Participa en estas?' },
  { code: 'P13', description: '¿Considera que estas son un aporte para los graduados?' },
  { code: 'P14', description: 'Si está empleado, ¿en cuál de los siguientes tramos se encuentra su sueldo bruto mensual?' },
];

@Component({
  selector: 'app-alumni-survey-statistics',
  imports: [Search, Table],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="container-fluid px-0 py-4">
      <div class="page-header">
        <div class="d-flex flex-wrap justify-content-between align-items-start gap-3">
          <div>
            <h1 class="page-title"><i class="bi bi-mortarboard" aria-hidden="true"></i>Estadísticas Encuesta Alumni</h1>
            <p class="page-subtitle mb-0">Resultados de la encuesta Alumni de Postgrado.</p>
          </div>
          <button type="button" class="btn btn-create" data-action="export"
            [disabled]="exporting()" (click)="download()">
            <i class="bi bi-file-earmark-excel me-1" aria-hidden="true"></i>
            {{ exporting() ? 'Exportando…' : 'Exportar Excel' }}
          </button>
        </div>
      </div>
      @if (error()) { <div class="alert alert-warning" role="alert">{{ error() }}</div> }
      <div class="form-section">
        <div class="d-flex justify-content-between align-items-center mb-3">
          <h2 class="section-title mb-0"><i class="bi bi-book" aria-hidden="true"></i>Diccionario de preguntas</h2>
          <div class="search-box"><app-search (search)="searchTerm.set($event)" /></div>
        </div>
        <app-table aria-label="Diccionario de preguntas de la encuesta Alumni"
          [data]="questions" [columns]="columns" [searchTerm]="searchTerm()"
          [showSummary]="false" [showRowNumber]="false" />
      </div>
      <p class="text-body-secondary mb-0">Las respuestas se descargan en Excel; no se muestran en pantalla.</p>
    </div>
  `,
  styles: '.search-box { width: 20rem; max-width: 100%; }',
})
export class AlumniSurveyStatistics {
  private readonly http = inject(HttpClient);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly questions = QUESTIONS;
  protected readonly columns = COLUMNS;
  protected readonly searchTerm = signal('');
  protected readonly exporting = signal(false);
  protected readonly error = signal('');

  protected download(): void {
    if (this.exporting()) return;
    this.exporting.set(true);
    this.error.set('');
    this.http.get(BASE + '/download', { responseType: 'blob' })
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: blob => {
          this.exporting.set(false);
          if (blob.size === 0) { this.error.set('Sin Datos'); return; }
          const url = URL.createObjectURL(blob);
          const link = document.createElement('a');
          try {
            link.href = url;
            link.download = 'EncuestaAlumniPostgrado.xlsx';
            document.body.appendChild(link); link.click();
          } finally { link.remove(); URL.revokeObjectURL(url); }
        },
        error: (error: HttpErrorResponse) => {
          this.exporting.set(false);
          this.error.set(error.status === 400 ? 'Sin Datos' : 'No se pudo exportar el informe.');
        },
      });
  }
}
