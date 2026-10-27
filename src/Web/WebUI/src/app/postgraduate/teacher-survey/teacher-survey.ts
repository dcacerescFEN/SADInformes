import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { firstValueFrom } from 'rxjs';
import { Table, TableColumn } from '../../shared/components/table';
import { ANSWER_SCALE, COURSE_QUESTIONS, TEACHER_QUESTIONS } from './teacher-survey-glossary';
import { GlossaryTable } from './glossary-table';

interface Option { value: string; text: string; }
interface Filters { periods: Option[]; studentTypes: Option[]; modalities: string[]; }
interface Envelope<T> { success: boolean; message: string | null; data: T; }
interface Row {
  period: string; course: string; name: string; teacherId: string; teacherName: string; assistants: string;
  a1: number | null; a2: number | null; a3: number | null; a4: number | null; aComments: string | null;
  b1: number | null; b2: number | null; b3: number | null; b4: number | null; b5: number | null; b6: number | null;
  b7: number | null; b8: number | null; b9: number | null; b10: number | null; b11: number | null; b12: number | null;
  b13: number | null; bGrade: number | null; bComments: string | null;
}

const BASE = '/api/reports/postgraduate/teacher-survey';
const COLUMNS: readonly TableColumn[] = [
  { key: 'course', name: 'Curso', sortable: true },
  { key: 'name', name: 'Cátedra', sortable: true },
  { key: 'period', name: 'Período', sortable: true },
  { key: 'teacherId', name: 'Rut Profesor' },
  { key: 'teacherName', name: 'Nombre Profesor', sortable: true },
  { key: 'assistants', name: 'Ayudantes' },
  { key: 'a1', name: 'A1' }, { key: 'a2', name: 'A2' }, { key: 'a3', name: 'A3' }, { key: 'a4', name: 'A4' },
  { key: 'aComments', name: 'A_Comentarios', multiline: true },
  { key: 'b1', name: 'B1' }, { key: 'b2', name: 'B2' }, { key: 'b3', name: 'B3' }, { key: 'b4', name: 'B4' },
  { key: 'b5', name: 'B5' }, { key: 'b6', name: 'B6' }, { key: 'b7', name: 'B7' }, { key: 'b8', name: 'B8' },
  { key: 'b9', name: 'B9' }, { key: 'b10', name: 'B10' }, { key: 'b11', name: 'B11' }, { key: 'b12', name: 'B12' },
  { key: 'b13', name: 'B13' }, { key: 'bGrade', name: 'B_Nota' },
  { key: 'bComments', name: 'B_Comentarios', multiline: true },
];

@Component({
  selector: 'app-teacher-survey',
  imports: [ReactiveFormsModule, NgSelectModule, Table, GlossaryTable],
  template: `
    <div class="container-fluid px-0 py-4">
      <div class="page-header">
        <div class="d-flex flex-wrap justify-content-between align-items-start gap-3">
          <div>
            <h1 class="page-title">
              <i class="bi bi-clipboard-data" aria-hidden="true"></i>
              Encuesta docente por curso
            </h1>
            <p class="page-subtitle mb-0">
              Evaluación general del curso (A) y del profesor (B) según período, tipo de alumno y modalidad.
            </p>
          </div>
          <button class="btn btn-create" type="button" data-action="export"
            [disabled]="rows().length === 0" (click)="exportCsv()">
            <i class="bi bi-filetype-csv me-2" aria-hidden="true"></i>Exportar CSV
          </button>
        </div>
      </div>

      <div class="page-header">
        <form [formGroup]="form" (ngSubmit)="show()">
          <div class="row g-3 align-items-end">
            <div class="col-xl-3 col-lg-4 col-md-6">
              <div class="ng-select-container-custom">
                <label class="floating-label" for="surveyPeriod">
                  <i class="bi bi-calendar3 me-1"></i>Período
                </label>
                <ng-select inputId="surveyPeriod" formControlName="period" data-filter="periodo"
                  [items]="filters().periods" bindLabel="text" bindValue="value"
                  placeholder="Seleccione período" class="form-select ng-select-custom" />
              </div>
            </div>
            <div class="col-xl-3 col-lg-4 col-md-6">
              <div class="ng-select-container-custom">
                <label class="floating-label" for="surveyStudentType">
                  <i class="bi bi-people-fill me-1"></i>Tipo alumno
                </label>
                <ng-select inputId="surveyStudentType" formControlName="studentType" data-filter="tipoAlumno"
                  [items]="filters().studentTypes" bindLabel="text" bindValue="value"
                  placeholder="Seleccione tipo" class="form-select ng-select-custom" />
              </div>
            </div>
            <div class="col-xl-3 col-lg-4 col-md-6">
              <div class="ng-select-container-custom">
                <label class="floating-label" for="surveyModality">
                  <i class="bi bi-grid-3x3-gap-fill me-1"></i>Modalidad
                </label>
                <ng-select inputId="surveyModality" formControlName="modality" data-filter="modalidad"
                  [items]="filters().modalities" bindLabel="item" bindValue="item" [clearable]="false"
                  placeholder="Seleccione modalidad" class="form-select ng-select-custom" />
              </div>
            </div>
            <div class="col-xl-auto col-lg-12">
              <div class="d-flex gap-2">
                <button class="btn btn-search" type="submit" data-action="show" [disabled]="loading()">
                  <i class="bi bi-search me-1" aria-hidden="true"></i>Mostrar
                </button>
              </div>
            </div>
          </div>
        </form>
      </div>

      @if (error()) { <div class="alert alert-warning" role="alert">{{ error() }}</div> }
      @if (notice()) { <p class="text-body-secondary" role="status">{{ notice() }}</p> }
      @if (loading()) { <p role="status">Cargando…</p> }

      @if (rows().length > 0) {
        <div class="form-section">
          <app-table aria-label="Encuesta docente por curso" [data]="rows()" [columns]="columns"
            [showSummary]="false" [showRowNumber]="false" />
          <p class="fw-semibold mt-3 mb-0">Total:{{ rows().length }}</p>
        </div>
      }

      <div class="form-section mt-3">
        <h2 class="h6">Glosa</h2>
        <h3 class="h6 text-body-secondary">Evaluación general del curso</h3>
        <app-glossary-table [entries]="courseQuestions" data-glossary="curso" />
        <h3 class="h6 text-body-secondary mt-3">Evaluación del profesor</h3>
        <app-glossary-table [entries]="teacherQuestions" data-glossary="profesor" />
        <h3 class="h6 text-body-secondary mt-3">Respuestas</h3>
        <app-glossary-table [entries]="answerScale" firstColumn="Número" data-glossary="respuestas" />
      </div>
    </div>
  `,
  styles: ':host { display: block; }',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TeacherSurvey {
  private readonly http = inject(HttpClient);
  private readonly formBuilder = inject(FormBuilder);
  protected readonly columns = COLUMNS;
  protected readonly courseQuestions = COURSE_QUESTIONS;
  protected readonly teacherQuestions = TEACHER_QUESTIONS;
  protected readonly answerScale = ANSWER_SCALE;
  readonly form = this.formBuilder.nonNullable.group({ period: '', studentType: '', modality: '' });
  protected readonly filters = signal<Filters>({ periods: [], studentTypes: [], modalities: [] });
  protected readonly rows = signal<Row[]>([]);
  protected readonly error = signal('');
  protected readonly notice = signal('');
  protected readonly loading = signal(false);

  constructor() {
    void this.loadFilters(true);
    this.form.controls.studentType.valueChanges.subscribe(value => {
      // La modalidad depende del tipo: se refleja en la señal antes de recargar.
      this.selectedType.set(value ?? '');
      void this.loadFilters();
    });
  }

  /** `applyDefaults` solo en la primera carga: el SAD llega con valores elegidos. */
  private async loadFilters(applyDefaults = false): Promise<void> {
    const studentType = this.form.controls.studentType.value;
    this.selectedType.set(studentType ?? '');
    const query = studentType ? '?tipoAlumno=' + encodeURIComponent(studentType) : '';
    try {
      const response = await firstValueFrom(this.http.get<Envelope<Filters>>(BASE + '/filters' + query));
      if (response.success !== true) throw new Error('contrato');
      this.filters.set(response.data);
      // El SAD llega con valores preseleccionados: primer período, primer tipo y
      // la primera modalidad disponible para el tipo elegido.
      const { period, modality } = this.form.getRawValue();
      if (applyDefaults && !period && response.data.periods.length > 0)
        this.form.controls.period.setValue(response.data.periods[0].value);
      if (applyDefaults && !this.form.controls.studentType.value && response.data.studentTypes.length > 0) {
        this.form.controls.studentType.setValue(response.data.studentTypes[0].value, { emitEvent: false });
        this.selectedType.set(response.data.studentTypes[0].value);
        // La primera carga sin tipo trae todas las modalidades; se recargan acotadas
        // al tipo por defecto, como en el SAD.
        await this.loadFilters();
        return;
      }
      if (!response.data.modalities.includes(modality))
        this.form.controls.modality.setValue(response.data.modalities[0] ?? '');
    } catch {
      this.filters.set({ periods: [], studentTypes: [], modalities: [] });
      this.error.set('No se pudieron cargar los filtros del informe.');
    }
  }

  private readonly selectedType = signal('');
  protected readonly modalityDisabled = computed(() =>
    !this.selectedType() || this.filters().modalities.length === 0);

  // Con Reactive Forms el estado se controla en el propio FormControl.
  private readonly syncModalityState = effect(() => {
    const disabled = this.modalityDisabled();
    const control = this.form.controls.modality;
    if (disabled && control.enabled) control.disable({ emitEvent: false });
    if (!disabled && control.disabled) control.enable({ emitEvent: false });
  });

  protected async show(): Promise<void> {
    this.error.set('');
    this.notice.set('');
    const { period, studentType, modality } = this.form.getRawValue();
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
      COLUMNS.map(column => column.name).join(';'),
      ...rows.map(row => COLUMNS.map(column => cell(row[column.key as keyof Row])).join(';')),
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
