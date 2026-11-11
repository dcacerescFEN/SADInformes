import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { Search } from '../../shared/components/search';
import { Table, TableColumn } from '../../shared/components/table';

interface Option { value: string; text: string; }
interface Row {
  id: string; type: string; course: string; from: string; to: string; period: string;
  courseCode: string; section: string; program: string; modality: string; promotion: string;
  room: string; roomCapacity: string; enrolledStudents: string; roomType: string; hybridRoom: string;
  entryDate: string; entryUser: string; suspended: string; suspensionDate: string;
  suspensionUser: string; observations: string;
}
const BASE = '/api/reports/postgraduate/course-activities';
const COLUMNS: readonly TableColumn[] = [
  { key: 'id', name: 'Id', sortable: true },
  { key: 'type', name: 'Tipo', sortable: true },
  { key: 'course', name: 'Catedra', sortable: true },
  { key: 'from', name: 'Desde' },
  { key: 'to', name: 'Hasta' },
  { key: 'period', name: 'Periodo', sortable: true },
  { key: 'courseCode', name: 'Cod_Catedra', sortable: true },
  { key: 'section', name: 'Cod_Seccion', sortable: true },
  { key: 'program', name: 'Programa', sortable: true },
  { key: 'modality', name: 'Modalidad', sortable: true },
  { key: 'promotion', name: 'Promocion', sortable: true },
  { key: 'room', name: 'Sala', sortable: true },
  { key: 'roomCapacity', name: 'Capacidad_Sala' },
  { key: 'enrolledStudents', name: 'Estudiantes_Inscritos' },
  { key: 'roomType', name: 'Tipo_Sala', sortable: true },
  { key: 'hybridRoom', name: 'Es_Sala_Hibrida', sortable: true },
  { key: 'entryDate', name: 'Fecha_Ingreso' },
  { key: 'entryUser', name: 'UsuarioIngreso', sortable: true },
  { key: 'suspended', name: 'Suspendida', sortable: true },
  { key: 'suspensionDate', name: 'Fecha_Suspención' },
  { key: 'suspensionUser', name: 'Usuario_Suspención', sortable: true },
  { key: 'observations', name: 'Observaciones', sortable: true },
];

@Component({
  selector: 'app-course-activities',
  imports: [ReactiveFormsModule, NgSelectModule, Search, Table],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="container-fluid px-0 py-4">
      <div class="page-header">
        <h1 class="page-title"><i class="bi bi-calendar3" aria-hidden="true"></i>Actividades Cursos Postgrado</h1>
        <p class="page-subtitle mb-0">Actividades, salas, inscritos y suspensiones por año.</p>
      </div>
      <div class="form-section mb-3">
        <h2 class="section-title"><i class="bi bi-funnel" aria-hidden="true"></i>Filtros</h2>
        <form [formGroup]="form" (ngSubmit)="loadReport()">
          <div class="row g-3 align-items-end">
            <div class="col-md-4">
              <div class="ng-select-container-custom">
                <label class="floating-label" for="activities-year">Año</label>
                <ng-select id="activities-year" formControlName="year" data-filter="year"
                  [items]="years()" bindLabel="text" bindValue="value" [clearable]="false"
                  class="form-select ng-select-custom" />
              </div>
            </div>
            <div class="col-auto d-flex gap-2">
              <button type="submit" class="btn btn-search" data-action="show"
                [disabled]="!selectedYear() || loading() || filtersLoading()">
                <i class="bi bi-search me-1" aria-hidden="true"></i>
                {{ loading() ? 'Cargando…' : 'Mostrar' }}
              </button>
              <button type="button" class="btn btn-create" data-action="export"
                [disabled]="rows().length === 0 || exporting()" (click)="download()">
                <i class="bi bi-file-earmark-excel me-1" aria-hidden="true"></i>
                {{ exporting() ? 'Exportando…' : 'Exportar' }}
              </button>
            </div>
          </div>
        </form>
      </div>
      @if (filtersLoading()) { <p role="status">Cargando filtros…</p> }
      @if (error()) { <div class="alert alert-warning" role="alert">{{ error() }}</div> }
      @if ((loaded() || loading()) && !error()) {
        <div class="form-section">
          <h2 class="section-title"><i class="bi bi-table" aria-hidden="true"></i>Resultados</h2>
          <div class="d-flex justify-content-end mb-3">
            <div class="search-box"><app-search (search)="searchTerm.set($event)" /></div>
          </div>
          <app-table [loading]="loading() || !loaded()" aria-label="Actividades de cursos de Postgrado" [data]="rows()" [columns]="columns"
            [searchTerm]="searchTerm()" [showSummary]="false" [showRowNumber]="false" />
          <p class="fw-semibold mt-3 mb-0">Total: {{ rows().length }}</p>
        </div>
      }
    </div>
  `,
  styles: '.search-box { width: 20rem; max-width: 100%; }',
})
export class CourseActivities {
  private readonly http = inject(HttpClient);
  private readonly destroyRef = inject(DestroyRef);
  readonly form = inject(FormBuilder).nonNullable.group({ year: '' });
  protected readonly columns = COLUMNS;
  protected readonly years = signal<Option[]>([]);
  protected readonly rows = signal<Row[]>([]);
  protected readonly searchTerm = signal('');
  protected readonly filtersLoading = signal(false);
  protected readonly loading = signal(false);
  protected readonly exporting = signal(false);
  protected readonly loaded = signal(false);
  protected readonly error = signal('');

  constructor() {
    this.form.controls.year.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => {
      this.rows.set([]);
      this.searchTerm.set('');
      this.loaded.set(false);
      this.error.set('');
    });
    this.loadFilters();
  }

  protected loadFilters(): void {
    this.filtersLoading.set(true);
    this.error.set('');
    this.http.get<{ success: boolean; data: { years: Option[] } }>(BASE + '/filters')
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: result => {
          this.filtersLoading.set(false);
          if (!result.success) { this.error.set('No se pudieron cargar los años.'); return; }
          this.years.set(result.data.years);
          this.form.patchValue({ year: result.data.years[0]?.value ?? '' }, { emitEvent: false });
          if (this.selectedYear()) this.loadReport();
        },
        error: () => { this.filtersLoading.set(false); this.error.set('No se pudieron cargar los años.'); },
      });
  }

  protected loadReport(): void {
    if (!this.selectedYear() || this.loading()) return;
    this.loading.set(true);
    this.error.set('');
    this.searchTerm.set('');
    this.http.get<{ success: boolean; data: { year: string; rows: Row[]; total: number } }>(
      BASE + '/report', { params: { year: this.selectedYear() } })
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: result => {
          this.loading.set(false);
          this.loaded.set(true);
          if (!result.success) { this.rows.set([]); this.error.set('No se pudo cargar el informe.'); return; }
          this.rows.set(result.data.rows);
        },
        error: () => {
          this.loading.set(false);
          this.loaded.set(true);
          this.rows.set([]);
          this.error.set('No se pudo cargar el informe.');
        },
      });
  }

  protected selectedYear(): string {
    return this.form.controls.year.value;
  }

  protected download(): void {
    if (this.rows().length === 0 || this.exporting()) return;
    this.exporting.set(true);
    this.error.set('');
    this.http.get(BASE + '/download', { params: { year: this.selectedYear() }, responseType: 'blob' })
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: blob => {
          this.exporting.set(false);
          if (blob.size === 0) { this.error.set('Sin Datos'); return; }
          const url = URL.createObjectURL(blob);
          const link = document.createElement('a');
          try {
            link.href = url;
            link.download = `Reporte Actividades Cursos Postgrado - ${this.selectedYear()}.xlsx`;
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
