import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';

interface Question { code: string; description: string; }
const BASE = '/api/reports/postgraduate/entry-survey-statistics';

@Component({
  selector: 'app-entry-survey-statistics',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="container-fluid px-0 py-4">
      <div class="page-header">
        <div class="d-flex flex-wrap justify-content-between align-items-start gap-3">
          <div>
            <h1 class="page-title"><i class="bi bi-clipboard-data" aria-hidden="true"></i>Estadísticas Encuesta Entrada</h1>
            <p class="page-subtitle mb-0">Diccionario de preguntas y respuestas de la encuesta de entrada de Postgrado.</p>
          </div>
          <button type="button" class="btn btn-create" data-action="export" [disabled]="exporting()" (click)="download()">
            <i class="bi bi-file-earmark-excel me-1" aria-hidden="true"></i>
            {{ exporting() ? 'Descargando…' : 'Descargar Excel' }}
          </button>
        </div>
      </div>
      @if (error()) { <div class="alert alert-warning" role="alert">{{ error() }}</div> }
      <div class="form-section">
        <div class="table-responsive">
          <table class="table table-hover align-middle app-table" aria-label="Diccionario de preguntas de la encuesta de entrada">
            <thead class="table-light"><tr><th>Código</th><th>Descripción</th></tr></thead>
            <tbody>@for (question of questions; track question.code) {
              <tr><td>{{ question.code }}</td><td>{{ question.description }}</td></tr>
            }</tbody>
          </table>
        </div>
      </div>
    </div>
  `,
})
export class EntrySurveyStatistics {
  private readonly http = inject(HttpClient);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly questions: Question[] = [
    { code: 'Q1_1', description: 'Indique las 5 razones principales por las cuales decidió cursar un programa de postgrado y ordénelas de mayor a menor importancia (siendo 1 la más importante y 5 la menos importante para usted).' },
    { code: 'Q2_1', description: 'Visión internacional que entrega el programa' },
    { code: 'Q2_2', description: 'Contenido atractivo del programa' },
    { code: 'Q2_3', description: 'Equilibrio teórico-práctico de los conocimientos entregados' },
    { code: 'Q2_4', description: 'Cuerpo académico' },
    { code: 'Q2_5', description: 'Prestigio de la universidad' },
    { code: 'Q2_6', description: 'Precio del programa' },
    { code: 'Q2_7', description: 'Distancia trabajo-sede de clases' },
    { code: 'Q2_8', description: 'Infraestructura moderna' },
    { code: 'Q3_1', description: 'El horario de clases del programa es adecuado' },
    { code: 'Q3_2', description: 'El precio del programa es apropiado' },
    { code: 'Q3_3', description: 'El contenido del programa es atractivo' },
    { code: 'Q3_4', description: 'El programa entrega un equilibrio teórico-práctico adecuado' },
    { code: 'Q3_5', description: 'La Escuela posee un sólido cuerpo académico' },
    { code: 'Q3_6', description: 'La sede de clases está bien ubicada' },
    { code: 'Q3_7', description: 'La sede es moderna' },
    { code: 'Q4_1', description: 'A continuación indique qué universidad era su segunda opción para cursar un programa de postgrado e indique su grado de acuerdo con las siguientes afirmaciones' },
    { code: 'Q4_2', description: 'El horario de clases del programa es adecuado' },
    { code: 'Q4_3', description: 'El precio del programa es apropiado' },
    { code: 'Q4_4', description: 'El contenido del programa es atractivo' },
    { code: 'Q4_5', description: 'El programa entrega un equilibrio teórico-práctico adecuado' },
    { code: 'Q4_6', description: 'La Escuela posee un sólido cuerpo académico' },
    { code: 'Q4_7', description: 'La sede de clases está bien ubicada' },
    { code: 'Q4_8', description: 'La sede es moderna' },
    { code: 'Q5', description: 'Por favor indique ¿Cómo se enteró del programa?' },
    { code: 'Q5_otro', description: 'Otra opción' },
    { code: 'Q6', description: '¿Cuál es su situación laboral actual?' },
    { code: 'Q7', description: 'Por favor indique nombre de la empresa donde trabaja (si se encuentra trabajando).' },
    { code: 'Q8', description: '¿En qué sector se encuentra trabajando actualmente?' },
    { code: 'Q9', description: '¿Cuál es el rubro de la empresa en la cual se desempeña?' },
    { code: 'Q9_otro', description: 'Otra opción' },
    { code: 'Q10', description: 'Indique el tamaño de la empresa donde trabaja según el número de trabajadores que esta posee' },
    { code: 'Q11', description: '¿Cuál es su cargo actual?' },
    { code: 'Q11_otro', description: 'Otra opción' },
    { code: 'Q12', description: 'Acorde al siguiente organigrama ¿En qué nivel se encuentra su cargo actual con respecto al cargo principal de la compañía? (si la empresa es multinacional, considerar solo Chile como base de medición)' },
    { code: 'Q13', description: '¿A qué rango corresponde su ingreso bruto (ingreso imponible) mensual actual?' },
    { code: 'Q14', description: 'Rango de Edad' },
    { code: 'Q15', description: 'Por favor indique la región en la que habita' },
    { code: 'Q16', description: 'Por favor indique su ciudad de residencia actual' },
    { code: 'Q17', description: 'Indique su comuna de residencia actual' },
  ];
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
            link.download = 'EncuestaEntradaPostgrado.xlsx';
            document.body.appendChild(link); link.click();
          } finally { link.remove(); URL.revokeObjectURL(url); }
        },
        error: (error: HttpErrorResponse) => {
          this.exporting.set(false);
          this.error.set(error.status === 400 ? 'Sin Datos' : 'No se pudo descargar el informe.');
        },
      });
  }
}
