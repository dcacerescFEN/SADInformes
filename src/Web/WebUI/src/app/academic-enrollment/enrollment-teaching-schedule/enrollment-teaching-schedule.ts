import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { Search } from '../../shared/components/search';
import { Table, TableColumn } from '../../shared/components/table';

interface Option { value: string; text: string; }
interface Row {
  course: string;
  courseName: string;
  teachers: string;
  classes: string;
  assistantships: string;
  room: string;
  assistantRoom: string;
  vacancies: string;
  enrolled: string;
  area: string;
  teacherId: string;
}
interface Selection { school: string; period: string; area: string; }

const BASE = '/api/reports/academic-enrollment/enrollment-teaching-schedule';
const SCHOOLS: readonly Option[] = [
  { value: 'PREGRADO', text: 'Pregrado y Postgrado Full Time' },
  { value: 'POSTGRADO', text: 'Postgrado Executive y Full Time' },
];
const COLUMNS: readonly TableColumn[] = [
  { key: 'course', name: 'Curso', sortable: true },
  { key: 'courseName', name: 'Cátedra', sortable: true },
  { key: 'teachers', name: 'Profesor(es)', sortable: true, multiline: true },
  { key: 'classes', name: 'Clases', sortable: true, multiline: true },
  { key: 'assistantships', name: 'Ayudantías', sortable: true, multiline: true },
  { key: 'room', name: 'Sala', sortable: true, multiline: true },
  { key: 'assistantRoom', name: 'Sala Ayudantías', sortable: true, multiline: true },
  { key: 'vacancies', name: 'Vacantes', sortable: true },
  { key: 'enrolled', name: 'Inscritos', sortable: true },
  { key: 'area', name: 'Área', sortable: true },
  { key: 'teacherId', name: 'Rut Profesor', sortable: true, multiline: true },
];

@Component({
  selector: 'app-enrollment-teaching-schedule',
  imports: [ReactiveFormsModule, NgSelectModule, Search, Table],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="container-fluid px-0 py-4">
      <div class="page-header">
        <div class="d-flex flex-wrap justify-content-between align-items-start gap-3">
          <div>
            <h1 class="page-title">
              <i class="bi bi-calendar3" aria-hidden="true"></i>
              Programación Docente
            </h1>
            <p class="page-subtitle mb-0">
              Programación docente resumida del proceso de inscripción
              @if (periodLabel()) { · {{ periodLabel() }} }.
            </p>
          </div>
          <button type="button" class="btn btn-create" data-action="export"
            [disabled]="rows().length === 0 || exporting() || !shownSelection()" (click)="download()">
            <i class="bi bi-file-earmark-excel me-1" aria-hidden="true"></i>
            {{ exporting() ? 'Descargando…' : 'Exportar Excel' }}
          </button>
        </div>
      </div>

      <div class="form-section mb-3">
        <h2 class="section-title"><i class="bi bi-funnel" aria-hidden="true"></i>Filtros</h2>
        <form [formGroup]="form" (ngSubmit)="loadReport()">
          <div class="row g-3 align-items-end">
            <div class="col-xl-3 col-md-6">
              <div class="ng-select-container-custom">
                <label class="floating-label" for="schedule-school">Escuela</label>
                <ng-select id="schedule-school" formControlName="school" data-filter="school"
                  [items]="schools" bindLabel="text" bindValue="value" [clearable]="false"
                  class="form-select ng-select-custom" />
              </div>
            </div>
            <div class="col-xl-3 col-md-6">
              <div class="ng-select-container-custom">
                <label class="floating-label" for="schedule-period">Período</label>
                <ng-select id="schedule-period" formControlName="period" data-filter="period"
                  [items]="periods()" bindLabel="text" bindValue="value" [clearable]="false"
                  class="form-select ng-select-custom" />
              </div>
            </div>
            <div class="col-xl-4 col-md-6">
              <div class="ng-select-container-custom">
                <label class="floating-label" for="schedule-area">Área</label>
                <ng-select id="schedule-area" formControlName="area" data-filter="area"
                  [items]="areas()" bindLabel="text" bindValue="value" [clearable]="false"
                  class="form-select ng-select-custom" />
              </div>
            </div>
            <div class="col-auto">
              <button type="submit" class="btn btn-search" data-action="refresh"
                [disabled]="form.invalid || loadingFilters() || loading()">
                <i class="bi bi-search me-1" aria-hidden="true"></i>Consultar
              </button>
            </div>
          </div>
        </form>
      </div>

      @if (loadingFilters()) { <p role="status">Cargando filtros…</p> }
      @if (error()) { <div class="alert alert-warning" role="alert">{{ error() }}</div> }

      @if (!error()) {
        <div class="form-section">
          <h2 class="section-title"><i class="bi bi-table" aria-hidden="true"></i>Resultados</h2>
          <div class="d-flex justify-content-end mb-3">
            <div class="search-box"><app-search (search)="searchTerm.set($event)" /></div>
          </div>
          <app-table [loading]="loading() || !loaded()" aria-label="Programación docente"
            [data]="rows()" [columns]="columns" [searchTerm]="searchTerm()"
            [showSummary]="false" [showRowNumber]="false" />
          <p class="fw-semibold mt-3 mb-0">Total: {{ rows().length }}</p>
        </div>
      }
    </div>
  `,
  styles: '.search-box { width: 20rem; max-width: 100%; }',
})
export class EnrollmentTeachingSchedule {
  private readonly http = inject(HttpClient);
  private readonly destroyRef = inject(DestroyRef);
  readonly form = inject(FormBuilder).nonNullable.group({
    school: ['PREGRADO', Validators.required],
    period: ['', Validators.required],
    area: ['', Validators.required],
  });
  protected readonly schools = SCHOOLS;
  protected readonly columns = COLUMNS;
  protected readonly periods = signal<Option[]>([]);
  protected readonly areas = signal<Option[]>([]);
  protected readonly rows = signal<Row[]>([]);
  protected readonly searchTerm = signal('');
  protected readonly loadingFilters = signal(false);
  protected readonly loading = signal(false);
  protected readonly loaded = signal(false);
  protected readonly exporting = signal(false);
  protected readonly error = signal('');
  protected readonly shownSelection = signal<Selection | null>(null);
  private revision = 0;

  protected periodLabel(): string {
    return this.periods().find(option => option.value === this.form.controls.period.value)?.text ?? '';
  }

  constructor() {
    this.form.controls.school.valueChanges.pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.loadFilters());
    this.form.controls.period.valueChanges.pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.loadReport());
    this.form.controls.area.valueChanges.pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.loadReport());
    this.loadFilters();
  }

  protected loadFilters(): void {
    const revision = ++this.revision;
    this.loadingFilters.set(true);
    this.loading.set(false);
    this.error.set('');
    this.rows.set([]);
    this.loaded.set(false);
    this.shownSelection.set(null);
    this.http.get<{ success: boolean; data: { periods: Option[]; areas: Option[] } }>(BASE + '/filters', {
      params: { school: this.form.controls.school.value },
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: result => {
        if (revision !== this.revision) return;
        this.loadingFilters.set(false);
        if (!result.success) { this.error.set('No se pudieron cargar los filtros.'); return; }
        this.periods.set(result.data.periods);
        this.areas.set(result.data.areas);
        this.form.patchValue({
          period: result.data.periods[0]?.value ?? '',
          area: result.data.areas.some(area => area.value === '%') ? '%' : result.data.areas[0]?.value ?? '',
        }, { emitEvent: false });
        if (!this.form.controls.period.value || !this.form.controls.area.value) {
          this.error.set('No hay períodos o áreas disponibles para el informe.');
          return;
        }
        this.loadReport();
      },
      error: () => {
        if (revision !== this.revision) return;
        this.loadingFilters.set(false);
        this.error.set('No se pudieron cargar los filtros.');
      },
    });
  }

  protected loadReport(): void {
    if (this.loadingFilters() || this.form.invalid) return;
    const selection: Selection = { ...this.form.getRawValue() };
    if (!selection.period || !selection.area) return;
    const revision = ++this.revision;
    this.loading.set(true);
    this.rows.set([]);
    this.loaded.set(false);
    this.error.set('');
    this.searchTerm.set('');
    this.shownSelection.set(null);
    this.http.get<{ success: boolean; data: { rows: Row[]; total: number } }>(BASE + '/report', {
      params: { ...selection },
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: result => {
        if (revision !== this.revision) return;
        this.loading.set(false);
        this.loaded.set(true);
        if (!result.success) { this.error.set('No se pudo cargar el informe.'); return; }
        this.rows.set(result.data.rows);
        this.shownSelection.set(selection);
      },
      error: () => {
        if (revision !== this.revision) return;
        this.loading.set(false);
        this.loaded.set(true);
        this.rows.set([]);
        this.error.set('No se pudo cargar el informe.');
      },
    });
  }

  protected download(): void {
    const selection = this.shownSelection();
    if (!selection || this.rows().length === 0 || this.exporting()) return;
    this.exporting.set(true);
    this.error.set('');
    this.http.get(BASE + '/export', { params: { ...selection }, responseType: 'blob' })
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: blob => {
          this.exporting.set(false);
          if (blob.size === 0) { this.error.set('Sin datos.'); return; }
          const url = URL.createObjectURL(blob);
          const link = document.createElement('a');
          try {
            link.href = url;
            link.download = 'ProgramacionDocente.xlsx';
            document.body.appendChild(link);
            link.click();
          } finally {
            link.remove();
            URL.revokeObjectURL(url);
          }
        },
        error: (error: HttpErrorResponse) => {
          this.exporting.set(false);
          this.error.set(error.status === 400 ? 'Sin datos.' : 'No se pudo exportar el informe.');
        },
      });
  }
}
