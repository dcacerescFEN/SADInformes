import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpClient } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { firstValueFrom } from 'rxjs';
import { Table, TableColumn } from '../../shared/components/table';

interface Option { value: string; text: string; }
interface Filters { years: Option[]; departments: Option[]; }
interface Envelope<T> { success: boolean; message: string | null; data: T; }
interface Row { professorCode: string; name: string; evaluatedCourses: number | null; average: string; classification: string; }
type FilterKey = 'year' | 'department';
const BASE = '/api/reports/postgraduate/teacher-courses';
const EMPTY_FILTERS: Filters = { years: [], departments: [] };
const FIELDS: readonly { control: FilterKey; label: string; icon: string }[] = [
  { control: 'year', label: 'Período', icon: 'bi-calendar3' },
  { control: 'department', label: 'Departamento/Escuela', icon: 'bi-building' },
];
const COLUMNS: readonly TableColumn[] = [
  { key: 'professorCode', name: 'Cod_Profesor', sortable: true },
  { key: 'name', name: 'Nombre', sortable: true },
  { key: 'evaluatedCourses', name: '#CatedrasEvaluadas', sortable: true },
  { key: 'average', name: 'Promedio Ev. Docente Profesor', sortable: false },
  { key: 'classification', name: 'Tipo AQ_PQ_O', sortable: true },
];

@Component({
  selector: 'app-teacher-courses',
  imports: [ReactiveFormsModule, NgSelectModule, Table],
  template: `
    <div class="container-fluid px-0 py-4">
      <div class="page-header">
        <div class="d-flex flex-wrap justify-content-between align-items-start gap-3">
          <div>
            <h1 class="page-title">
              <i class="bi bi-journal-check" aria-hidden="true"></i>
              Cursos Profesores Postgrado
            </h1>
            <p class="page-subtitle mb-0">Cursos y evaluación de profesores por período y departamento.</p>
          </div>
          <button class="btn btn-create" type="button" data-action="export" [disabled]="rows().length === 0" (click)="exportCsv()">
            <i class="bi bi-filetype-csv me-2" aria-hidden="true"></i>Exportar CSV
          </button>
        </div>
      </div>
      <div class="page-header">
        <form [formGroup]="form" (ngSubmit)="show()">
          <div class="row g-3 align-items-end">
            @for (field of fields; track field.control) {
              <div class="col-xl-3 col-lg-4 col-md-6">
                <div class="ng-select-container-custom">
                  <label class="floating-label" [for]="'teacher-courses-' + field.control">
                    <i [class]="'bi ' + field.icon + ' me-1'" aria-hidden="true"></i>{{ field.label }}
                  </label>
                  <ng-select [labelForId]="'teacher-courses-' + field.control" [formControlName]="field.control"
                    [attr.data-filter]="field.control" [items]="filterOptions()[field.control]" bindLabel="text"
                    bindValue="value" [clearable]="false" placeholder="Seleccione" class="form-select ng-select-custom" />
                </div>
              </div>
            }
            <div class="col-xl-auto col-lg-12">
              <button class="btn btn-search" type="submit" data-action="show" [disabled]="loading() || filtersLoading() || !filtersReady()">
                <i class="bi bi-search me-1" aria-hidden="true"></i>Mostrar
              </button>
            </div>
          </div>
        </form>
      </div>
      @if (error()) { <div class="alert alert-warning" role="alert">{{ error() }}</div> }
      @if (notice()) { <p class="text-body-secondary" role="status">{{ notice() }}</p> }
      @if (loading()) { <p role="status">Cargando…</p> }
      @if (rows().length > 0) {
        <div class="form-section">
          <app-table aria-label="Cursos Profesores Postgrado" [data]="rows()" [columns]="columns"
            [showSummary]="false" [showRowNumber]="false" />
          <p class="fw-semibold mt-3 mb-0">Total:{{ rows().length }}</p>
        </div>
      }
    </div>
  `,
  styles: ':host { display: block; }',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TeacherCourses {
  private readonly http = inject(HttpClient);
  private readonly builder = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly fields = FIELDS;
  protected readonly columns = COLUMNS;
  readonly form = this.builder.nonNullable.group({
    year: { value: '', disabled: true }, department: { value: '', disabled: true },
  });
  protected readonly filters = signal<Filters>(EMPTY_FILTERS);
  protected readonly filterOptions = computed<Record<FilterKey, Option[]>>(() => {
    const filters = this.filters();
    return { year: filters.years, department: filters.departments };
  });
  protected readonly rows = signal<Row[]>([]);
  protected readonly error = signal('');
  protected readonly notice = signal('');
  protected readonly loading = signal(false);
  protected readonly filtersLoading = signal(false);
  protected readonly filtersReady = signal(false);
  private filterRevision = 0;
  private reportRevision = 0;

  constructor() {
    this.destroyRef.onDestroy(() => { ++this.filterRevision; ++this.reportRevision; });
    this.form.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.discardReport());
    void this.loadFilters();
  }

  private discardReport(): void {
    ++this.reportRevision;
    this.rows.set([]);
    this.error.set('');
    this.notice.set('');
    this.loading.set(false);
  }

  private async loadFilters(): Promise<void> {
    const revision = ++this.filterRevision;
    const selected = this.form.getRawValue();
    this.discardReport();
    this.filtersLoading.set(true);
    this.filtersReady.set(false);
    this.form.controls.year.disable({ emitEvent: false });
    this.form.controls.department.disable({ emitEvent: false });
    try {
      const response = await firstValueFrom(this.http.get<Envelope<Filters>>(BASE + '/filters')
        .pipe(takeUntilDestroyed(this.destroyRef)));
      if (revision !== this.filterRevision) return;
      if (response.success !== true) throw new Error('Invalid response envelope');
      this.filters.set(response.data);
      const year = response.data.years.some(option => option.value === selected.year)
        ? selected.year : response.data.years[0]?.value ?? '';
      const department = response.data.departments.some(option => option.value === selected.department)
        ? selected.department : response.data.departments[0]?.value ?? '';
      this.form.controls.year.setValue(year, { emitEvent: false });
      this.form.controls.department.setValue(department, { emitEvent: false });
      if (response.data.years.length > 0) this.form.controls.year.enable({ emitEvent: false });
      if (response.data.departments.length > 0) this.form.controls.department.enable({ emitEvent: false });
      this.filtersReady.set(true);
    } catch {
      if (revision !== this.filterRevision) return;
      this.filters.set(EMPTY_FILTERS);
      this.error.set('No se pudieron cargar los filtros del informe.');
    } finally {
      if (revision === this.filterRevision) this.filtersLoading.set(false);
    }
  }

  protected async show(): Promise<void> {
    if (this.loading()) return;
    this.discardReport();
    const selected = this.form.getRawValue();
    if (!this.filtersReady() || !selected.year || !selected.department) {
      this.error.set('Seleccione período y departamento para consultar.');
      return;
    }
    const revision = this.reportRevision;
    this.loading.set(true);
    const params = { periodo: selected.year, departamento: selected.department };
    try {
      const response = await firstValueFrom(this.http.get<Envelope<{ rows: Row[]; total: number }>>(BASE, { params })
        .pipe(takeUntilDestroyed(this.destroyRef)));
      if (revision !== this.reportRevision) return;
      if (response.success !== true) throw new Error('Invalid response envelope');
      this.rows.set(response.data.rows);
      if (response.data.rows.length === 0) this.notice.set('No se encontraron evaluaciones');
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
    link.download = 'CursosProfesores_Postgrado.csv';
    link.click();
    URL.revokeObjectURL(url);
  }
}
