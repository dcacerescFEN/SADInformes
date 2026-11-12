import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { Search } from '../../shared/components/search';
import { Table, TableColumn } from '../../shared/components/table';

interface Option { value: string; text: string; }
interface Row {
  course1: string;
  courseName1: string;
  course2: string;
  courseName2: string;
  icLimit: string;
  iicgLimit: string;
  otherLimit: string;
  totalLimit: string;
}

const BASE = '/api/reports/academic-enrollment/course-crossing';
const COLUMNS: readonly TableColumn[] = [
  { key: 'course1', name: 'Curso 1', sortable: true },
  { key: 'courseName1', name: 'Cátedra 1', sortable: true, multiline: true },
  { key: 'course2', name: 'Curso 2', sortable: true },
  { key: 'courseName2', name: 'Cátedra 2', sortable: true, multiline: true },
  { key: 'icLimit', name: 'Topes IC', sortable: true },
  { key: 'iicgLimit', name: 'Topes IICG', sortable: true },
  { key: 'otherLimit', name: 'Topes Otros', sortable: true },
  { key: 'totalLimit', name: 'Topes Total', sortable: true },
];

@Component({
  selector: 'app-course-crossing',
  imports: [ReactiveFormsModule, NgSelectModule, Search, Table],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="container-fluid px-0 py-4">
      <div class="page-header">
        <div class="d-flex flex-wrap justify-content-between align-items-start gap-3">
          <div>
            <h1 class="page-title">
              <i class="bi bi-intersect" aria-hidden="true"></i>
              Cruce de Cursos
            </h1>
            <p class="page-subtitle mb-0">
              Alumnos inscritos simultáneamente en pares de cursos
              @if (periodLabel()) { · {{ periodLabel() }} }.
            </p>
          </div>
          <button type="button" class="btn btn-create" data-action="export"
            [disabled]="rows().length === 0 || exporting() || !shownPeriod()" (click)="download()">
            <i class="bi bi-file-earmark-excel me-1" aria-hidden="true"></i>
            {{ exporting() ? 'Descargando…' : 'Exportar Excel' }}
          </button>
        </div>
      </div>

      <div class="form-section mb-3">
        <h2 class="section-title"><i class="bi bi-funnel" aria-hidden="true"></i>Filtros</h2>
        <form [formGroup]="form" (ngSubmit)="loadReport()">
          <div class="row g-3 align-items-end">
            <div class="col-lg-4 col-md-6">
              <div class="ng-select-container-custom">
                <label class="floating-label" for="crossing-period">Período</label>
                <ng-select id="crossing-period" formControlName="period" data-filter="period"
                  [items]="periods()" bindLabel="text" bindValue="value" [clearable]="false"
                  class="form-select ng-select-custom" />
              </div>
            </div>
          </div>
        </form>
      </div>

      @if (loadingFilters()) { <p role="status">Cargando períodos…</p> }
      @if (error()) { <div class="alert alert-warning" role="alert">{{ error() }}</div> }

      @if (!error()) {
        <div class="form-section">
          <h2 class="section-title"><i class="bi bi-table" aria-hidden="true"></i>Resultados</h2>
          <div class="d-flex justify-content-end mb-3">
            <div class="search-box"><app-search (search)="searchTerm.set($event)" /></div>
          </div>
          <app-table [loading]="loading() || !loaded()" aria-label="Cruce de cursos"
            [data]="rows()" [columns]="columns" [searchTerm]="searchTerm()"
            [showSummary]="false" [showRowNumber]="false" />
          <p class="fw-semibold mt-3 mb-0">Total: {{ rows().length }}</p>
        </div>
      }
    </div>
  `,
  styles: '.search-box { width: 20rem; max-width: 100%; }',
})
export class CourseCrossing {
  private readonly http = inject(HttpClient);
  private readonly destroyRef = inject(DestroyRef);
  readonly form = inject(FormBuilder).nonNullable.group({ period: '' });
  protected readonly columns = COLUMNS;
  protected readonly periods = signal<Option[]>([]);
  protected readonly rows = signal<Row[]>([]);
  protected readonly searchTerm = signal('');
  protected readonly loadingFilters = signal(false);
  protected readonly loading = signal(false);
  protected readonly loaded = signal(false);
  protected readonly exporting = signal(false);
  protected readonly error = signal('');
  protected readonly shownPeriod = signal('');
  private revision = 0;

  constructor() {
    this.form.controls.period.valueChanges.pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.loadReport());
    this.loadFilters();
  }

  protected periodLabel(): string {
    return this.periods().find(option => option.value === this.form.controls.period.value)?.text ?? '';
  }

  protected loadFilters(): void {
    const revision = ++this.revision;
    this.loadingFilters.set(true);
    this.loading.set(false);
    this.error.set('');
    this.rows.set([]);
    this.loaded.set(false);
    this.shownPeriod.set('');
    this.http.get<{ success: boolean; data: { periods: Option[] } }>(BASE + '/filters')
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: result => {
          if (revision !== this.revision) return;
          this.loadingFilters.set(false);
          if (!result.success) { this.error.set('No se pudieron cargar los períodos.'); return; }
          this.periods.set(result.data.periods);
          if (result.data.periods.length === 0) {
            this.error.set('No hay períodos activos para el informe.');
            return;
          }
          this.form.controls.period.setValue(result.data.periods[0].value, { emitEvent: false });
          this.loadReport();
        },
        error: () => {
          if (revision !== this.revision) return;
          this.loadingFilters.set(false);
          this.error.set('No se pudieron cargar los períodos.');
        },
      });
  }

  protected loadReport(): void {
    if (this.loadingFilters() || this.form.invalid) return;
    const period = this.form.controls.period.value;
    if (!period) return;
    const revision = ++this.revision;
    this.loading.set(true);
    this.rows.set([]);
    this.loaded.set(false);
    this.error.set('');
    this.searchTerm.set('');
    this.shownPeriod.set('');
    this.http.get<{ success: boolean; data: { rows: Row[]; total: number } }>(BASE + '/report', {
      params: { period },
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: result => {
        if (revision !== this.revision) return;
        this.loading.set(false);
        this.loaded.set(true);
        if (!result.success) { this.error.set('No se pudo cargar el informe.'); return; }
        this.rows.set(result.data.rows);
        this.shownPeriod.set(period);
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
    const period = this.shownPeriod();
    if (!period || this.rows().length === 0 || this.exporting()) return;
    this.exporting.set(true);
    this.error.set('');
    this.http.get(BASE + '/export', { params: { period }, responseType: 'blob' })
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: blob => {
          this.exporting.set(false);
          if (blob.size === 0) { this.error.set('Sin datos.'); return; }
          const url = URL.createObjectURL(blob);
          const link = document.createElement('a');
          try {
            link.href = url;
            link.download = 'CruceCursos.xlsx';
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
