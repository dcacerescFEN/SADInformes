import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

interface Option { value: string; text: string; }
interface Filters {
  periods: Option[];
  studentTypes: Option[];
  modalities: string[];
}
interface Envelope<T> { success: boolean; message: string | null; data: T; }
interface Row {
  period: string; course: string; name: string; teacherId: string; teacherName: string; assistants: string;
  a1: number | null; a2: number | null; a3: number | null; a4: number | null; aComments: string | null;
  b1: number | null; b2: number | null; b3: number | null; b4: number | null; b5: number | null; b6: number | null;
  b7: number | null; b8: number | null; b9: number | null; b10: number | null; b11: number | null; b12: number | null;
  b13: number | null; bGrade: number | null; bComments: string | null;
}

const BASE = '/api/reports/postgraduate/teacher-survey';
const COLUMNS: readonly { key: keyof Row; label: string }[] = [
  { key: 'course', label: 'Curso' }, { key: 'name', label: 'Cátedra' }, { key: 'period', label: 'Período' },
  { key: 'teacherId', label: 'Rut Profesor' }, { key: 'teacherName', label: 'Nombre Profesor' },
  { key: 'assistants', label: 'Ayudantes' },
  { key: 'a1', label: 'A1' }, { key: 'a2', label: 'A2' }, { key: 'a3', label: 'A3' }, { key: 'a4', label: 'A4' },
  { key: 'aComments', label: 'A_Comentarios' },
  { key: 'b1', label: 'B1' }, { key: 'b2', label: 'B2' }, { key: 'b3', label: 'B3' }, { key: 'b4', label: 'B4' },
  { key: 'b5', label: 'B5' }, { key: 'b6', label: 'B6' }, { key: 'b7', label: 'B7' }, { key: 'b8', label: 'B8' },
  { key: 'b9', label: 'B9' }, { key: 'b10', label: 'B10' }, { key: 'b11', label: 'B11' }, { key: 'b12', label: 'B12' },
  { key: 'b13', label: 'B13' }, { key: 'bGrade', label: 'B_Nota' }, { key: 'bComments', label: 'B_Comentarios' },
];

@Component({
  selector: 'app-teacher-survey',
  template: `
    <section class="report" aria-labelledby="survey-title">
      <p class="text-body-secondary mb-2">Postgrado</p>
      <h1 id="survey-title">Encuesta docente por curso</h1>
      <p class="text-body-secondary">
        Evaluación general del curso (A) y del profesor (B) según período, tipo de alumno y modalidad.
      </p>

      <form class="row g-2 align-items-end mb-3" (submit)="$event.preventDefault(); show()">
        <div class="col-auto">
          <label class="form-label" for="survey-period">Período</label>
          <select id="survey-period" class="form-select" data-filter="periodo" [value]="period()"
            (change)="period.set(value($event))">
            <option value="">Seleccione período</option>
            @for (option of filters().periods; track option.value) {
              <option [value]="option.value">{{ option.text }}</option>
            }
          </select>
        </div>
        <div class="col-auto">
          <label class="form-label" for="survey-type">Tipo alumno</label>
          <select id="survey-type" class="form-select" data-filter="tipoAlumno" [value]="studentType()"
            (change)="changeStudentType(value($event))">
            <option value="">Seleccione tipo</option>
            @for (option of filters().studentTypes; track option.value) {
              <option [value]="option.value">{{ option.text }}</option>
            }
          </select>
        </div>
        <div class="col-auto">
          <label class="form-label" for="survey-modality">Modalidad</label>
          <select id="survey-modality" class="form-select" data-filter="modalidad" [value]="modality()"
            (change)="modality.set(value($event))">
            <option value="">Seleccione modalidad</option>
            @for (option of filters().modalities; track option) {
              <option [value]="option">{{ option }}</option>
            }
          </select>
        </div>
        <div class="col-auto">
          <button class="btn btn-primary" type="submit" data-action="show" [disabled]="loading()">Mostrar</button>
        </div>
        <div class="col-auto">
          <button class="btn btn-outline-secondary" type="button" data-action="export"
            [disabled]="rows().length === 0" (click)="exportCsv()">Exportar CSV</button>
        </div>
      </form>

      @if (error()) { <div class="alert alert-warning" role="alert">{{ error() }}</div> }
      @if (notice()) { <p class="text-body-secondary" role="status">{{ notice() }}</p> }
      @if (loading()) { <p role="status">Cargando…</p> }

      @if (rows().length > 0) {
        <div class="table-responsive">
          <table class="table table-sm table-striped align-middle">
            <caption class="visually-hidden">Encuesta docente por curso</caption>
            <thead>
              <tr>@for (column of columns; track column.key) { <th scope="col">{{ column.label }}</th> }</tr>
            </thead>
            <tbody>
              @for (item of rows(); track item.course + item.teacherId) {
                <tr>@for (column of columns; track column.key) {
                  <td>{{ item[column.key] ?? '' }}</td>
                }</tr>
              }
            </tbody>
          </table>
        </div>
        <p class="fw-semibold">Total:{{ rows().length }}</p>
      }
    </section>
  `,
  styles: `
    :host { display: block; }
    .report { max-width: 100%; }
    table { font-size: 0.8rem; }
    th, td { white-space: nowrap; }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TeacherSurvey {
  private readonly http = inject(HttpClient);
  protected readonly columns = COLUMNS;
  protected readonly filters = signal<Filters>({ periods: [], studentTypes: [], modalities: [] });
  protected readonly period = signal('');
  protected readonly studentType = signal('');
  protected readonly modality = signal('');
  protected readonly rows = signal<Row[]>([]);
  protected readonly error = signal('');
  protected readonly notice = signal('');
  protected readonly loading = signal(false);
  protected readonly selected = computed(() => ({
    period: this.period(), studentType: this.studentType(), modality: this.modality(),
  }));

  constructor() { void this.loadFilters(); }

  protected value(event: Event): string { return (event.target as HTMLSelectElement).value; }

  protected async changeStudentType(value: string): Promise<void> {
    this.studentType.set(value);
    await this.loadFilters();
  }

  private async loadFilters(): Promise<void> {
    const query = this.studentType() ? '?tipoAlumno=' + encodeURIComponent(this.studentType()) : '';
    try {
      const response = await firstValueFrom(this.http.get<Envelope<Filters>>(BASE + '/filters' + query));
      if (response.success !== true) throw new Error('contrato');
      this.filters.set(response.data);
      if (!response.data.modalities.includes(this.modality())) this.modality.set('');
    } catch {
      this.filters.set({ periods: [], studentTypes: [], modalities: [] });
      this.error.set('No se pudieron cargar los filtros del informe.');
    }
  }

  protected async show(): Promise<void> {
    this.error.set('');
    this.notice.set('');
    const { period, studentType, modality } = this.selected();
    if (!period || !studentType || !modality) {
      this.rows.set([]);
      this.error.set('Seleccione período, tipo de alumno y modalidad para consultar.');
      return;
    }
    const query = `?periodo=${encodeURIComponent(period)}&tipoAlumno=${encodeURIComponent(studentType)}`
      + `&modalidad=${encodeURIComponent(modality)}`;
    this.loading.set(true);
    try {
      const response = await firstValueFrom(this.http.get<Envelope<{ rows: Row[]; total: number }>>(BASE + query));
      if (response.success !== true) throw new Error('contrato');
      this.rows.set(response.data.rows);
      if (response.data.rows.length === 0) this.notice.set('Sin resultados para los filtros seleccionados.');
    } catch {
      this.rows.set([]);
      this.error.set('No se pudo cargar el informe.');
    } finally {
      this.loading.set(false);
    }
  }

  protected exportCsv(): void {
    const rows = this.rows();
    if (rows.length === 0) return;
    const cell = (value: unknown) => {
      const text = value === null || value === undefined ? '' : String(value);
      return /[";\r\n]/.test(text) ? '"' + text.replaceAll('"', '""') + '"' : text;
    };
    const lines = [
      COLUMNS.map(column => column.label).join(';'),
      ...rows.map(row => COLUMNS.map(column => cell(row[column.key])).join(';')),
    ];
    const blob = new Blob(['\uFEFF' + lines.join('\r\n')], { type: 'text/csv;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = 'EncuestaDocenteXCurso.csv';
    link.click();
    URL.revokeObjectURL(url);
  }
}
