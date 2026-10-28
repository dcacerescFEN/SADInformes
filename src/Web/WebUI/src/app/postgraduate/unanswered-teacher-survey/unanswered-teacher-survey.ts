import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpClient } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { firstValueFrom } from 'rxjs';
import { Table, TableColumn } from '../../shared/components/table';

interface Option { value: string; text: string; }
interface Filters { periods: Option[]; studentTypes: Option[]; modalities: string[]; }
interface Envelope<T> { success: boolean; message: string | null; data: T; }
interface Row {
  period: string; course: string; name: string;
  studentCode: string; studentName: string;
}

const BASE = '/api/reports/postgraduate/unanswered-teacher-survey';
const COLUMNS: readonly TableColumn[] = [
  { key: 'course', name: 'Curso', sortable: true },
  { key: 'name', name: 'Cátedra', sortable: true },
  { key: 'studentCode', name: 'Código Alumno', sortable: true },
  { key: 'studentName', name: 'Nombre Alumno', sortable: true },
  { key: 'period', name: 'Periodo', sortable: true },
];

@Component({
  selector: 'app-unanswered-teacher-survey',
  imports: [ReactiveFormsModule, NgSelectModule, Table],
  template: `
    <div class="container-fluid px-0 py-4">
      <div class="page-header">
        <div class="d-flex flex-wrap justify-content-between align-items-start gap-3">
          <div>
            <h1 class="page-title">
              <i class="bi bi-journal-bookmark" aria-hidden="true"></i>
              Alumnos sin contestar encuesta docente
            </h1>
            <p class="page-subtitle mb-0">
              Alumnos sin evaluaciones de curso ni de docentes registradas para el período, tipo y modalidad seleccionados.
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
                <label class="floating-label" for="unansweredPeriod">
                  <i class="bi bi-calendar3 me-1"></i>Período
                </label>
                <ng-select inputId="unansweredPeriod" formControlName="period" data-filter="periodo"
                  [items]="filters().periods" bindLabel="text" bindValue="value"
                  placeholder="Seleccione período" class="form-select ng-select-custom" />
              </div>
            </div>
            <div class="col-xl-3 col-lg-4 col-md-6">
              <div class="ng-select-container-custom">
                <label class="floating-label" for="unansweredStudentType">
                  <i class="bi bi-people-fill me-1"></i>Tipo alumno
                </label>
                <ng-select inputId="unansweredStudentType" formControlName="studentType" data-filter="tipoAlumno"
                  [items]="filters().studentTypes" bindLabel="text" bindValue="value"
                  placeholder="Seleccione tipo" class="form-select ng-select-custom" />
              </div>
            </div>
            <div class="col-xl-3 col-lg-4 col-md-6">
              <div class="ng-select-container-custom">
                <label class="floating-label" for="unansweredModality">
                  <i class="bi bi-grid-3x3-gap-fill me-1"></i>Modalidad
                </label>
                <ng-select inputId="unansweredModality" formControlName="modality" data-filter="modalidad"
                  [items]="filters().modalities" [clearable]="false"
                  placeholder="Seleccione modalidad" class="form-select ng-select-custom" />
              </div>
            </div>
            <div class="col-xl-auto col-lg-12">
              <div class="d-flex gap-2">
                <button class="btn btn-search" type="submit" data-action="show" [disabled]="loading() || filtersLoading()">
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
          <app-table aria-label="Alumnos sin contestar encuesta docente" [data]="rows()" [columns]="columns"
            [showSummary]="false" [showRowNumber]="false" />
          <p class="fw-semibold mt-3 mb-0">Total:{{ rows().length }}</p>
        </div>
      }

    </div>
  `,
  styles: ':host { display: block; }',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UnansweredTeacherSurvey {
  private readonly http = inject(HttpClient);
  private readonly formBuilder = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly columns = COLUMNS;
  readonly form = this.formBuilder.nonNullable.group({
    period: '', studentType: '', modality: { value: '', disabled: true },
  });
  protected readonly filters = signal<Filters>({ periods: [], studentTypes: [], modalities: [] });
  protected readonly rows = signal<Row[]>([]);
  protected readonly error = signal('');
  protected readonly notice = signal('');
  protected readonly loading = signal(false);
  protected readonly filtersLoading = signal(false);
  private filterRevision = 0;
  private reportRevision = 0;

  constructor() {
    this.destroyRef.onDestroy(() => {
      ++this.filterRevision;
      ++this.reportRevision;
    });
    this.form.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.discardReport());
    this.form.controls.studentType.valueChanges.pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => { void this.loadFilters(); });
    void this.loadFilters(true);
  }

  private discardReport(): void {
    ++this.reportRevision;
    this.rows.set([]);
    this.error.set('');
    this.notice.set('');
    this.loading.set(false);
  }

  private async loadFilters(applyDefaults = false): Promise<void> {
    const revision = ++this.filterRevision;
    const studentType = this.form.controls.studentType.value;
    this.discardReport();
    this.form.controls.modality.setValue('', { emitEvent: false });
    this.form.controls.modality.disable({ emitEvent: false });
    this.filters.update(filters => ({ ...filters, modalities: [] }));
    this.filtersLoading.set(true);
    const query = studentType ? '?tipoAlumno=' + encodeURIComponent(studentType) : '';
    try {
      const response = await firstValueFrom(this.http.get<Envelope<Filters>>(BASE + '/filters' + query)
        .pipe(takeUntilDestroyed(this.destroyRef)));
      if (revision !== this.filterRevision) return;
      if (response.success !== true) throw new Error('Invalid response envelope');
      this.filters.set(response.data);
      const period = this.form.controls.period.value;
      if (!response.data.periods.some(option => option.value === period))
        this.form.controls.period.setValue(response.data.periods[0]?.value ?? '', { emitEvent: false });
      if (applyDefaults && !studentType && response.data.studentTypes.length > 0) {
        this.form.controls.studentType.setValue(response.data.studentTypes[0].value, { emitEvent: false });
        await this.loadFilters();
        return;
      }
      if (studentType && response.data.modalities.length > 0) {
        this.form.controls.modality.setValue(response.data.modalities[0], { emitEvent: false });
        this.form.controls.modality.enable({ emitEvent: false });
      }
    } catch {
      if (revision !== this.filterRevision) return;
      this.filters.set({ periods: [], studentTypes: [], modalities: [] });
      this.error.set('No se pudieron cargar los filtros del informe.');
    } finally {
      if (revision === this.filterRevision) this.filtersLoading.set(false);
    }
  }

  protected async show(): Promise<void> {
    if (this.loading()) return;
    this.discardReport();
    const { period, studentType, modality } = this.form.getRawValue();
    if (!period || !studentType || !modality || this.filtersLoading()) {
      this.error.set('Seleccione período, tipo de alumno y modalidad para consultar.');
      return;
    }
    const revision = this.reportRevision;
    const query = `?periodo=${encodeURIComponent(period)}&tipoAlumno=${encodeURIComponent(studentType)}`
      + `&modalidad=${encodeURIComponent(modality)}`;
    this.loading.set(true);
    try {
      const response = await firstValueFrom(this.http.get<Envelope<{ rows: Row[]; total: number }>>(BASE + query)
        .pipe(takeUntilDestroyed(this.destroyRef)));
      if (revision !== this.reportRevision) return;
      if (response.success !== true) throw new Error('Invalid response envelope');
      this.rows.set(response.data.rows);
      if (response.data.rows.length === 0) this.notice.set('Sin resultados para los filtros seleccionados.');
    } catch {
      if (revision !== this.reportRevision) return;
      this.rows.set([]);
      this.error.set('No se pudo cargar el informe.');
    } finally {
      if (revision === this.reportRevision) this.loading.set(false);
    }
  }

  protected exportCsv(): void {
    const rows = this.rows();
    if (rows.length === 0) return;
    const cell = (value: unknown) => {
      let text = value === null || value === undefined ? '' : String(value);
      // Spreadsheet formulas remain inert even when prefixed by whitespace.
      if (/^\s*[=+\-@]|^[\t\r\n]/.test(text)) text = "'" + text;
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
    link.download = 'AlumnosSinContestarEncuesta.csv';
    link.click();
    URL.revokeObjectURL(url);
  }
}
