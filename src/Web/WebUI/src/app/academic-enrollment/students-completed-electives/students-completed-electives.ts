import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpClient } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { Search } from '../../shared/components/search';
import { Table, TableColumn } from '../../shared/components/table';

interface Option { value: string; text: string; }
interface Row {
  studentCode: string;
  name: string;
  course: string;
  courseName: string;
  decree: string;
  courseType: string;
}

const BASE = '/api/reports/academic-enrollment/students-completed-electives';
const COLUMNS: readonly TableColumn[] = [
  { key: 'studentCode', name: 'Código', sortable: true },
  { key: 'name', name: 'Nombre', sortable: true },
  { key: 'course', name: 'Curso', sortable: true },
  { key: 'courseName', name: 'Cátedra', sortable: true },
  { key: 'decree', name: 'Decreto', sortable: true },
  { key: 'courseType', name: 'Tipo Cátedra', sortable: true },
];

@Component({
  selector: 'app-students-completed-electives',
  imports: [ReactiveFormsModule, NgSelectModule, Search, Table],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="container-fluid px-0 py-4">
      <div class="page-header">
        <div class="d-flex flex-wrap justify-content-between align-items-start gap-3">
          <div>
            <h1 class="page-title">
              <i class="bi bi-check2-circle" aria-hidden="true"></i>
              Alumnos que completaron electivos
            </h1>
            <p class="page-subtitle mb-0">
              Alumnos con cursos electivos completados e inscritos en el semestre
              @if (periodLabel()) { · {{ periodLabel() }} }.
            </p>
          </div>
          <button type="button" class="btn btn-search" data-action="refresh"
            [disabled]="form.invalid || loading()" (click)="loadReport()">
            <i class="bi bi-arrow-clockwise me-1" aria-hidden="true"></i>
            {{ loading() ? 'Cargando…' : 'Actualizar' }}
          </button>
        </div>
      </div>

      <div class="form-section mb-3">
        <h2 class="section-title"><i class="bi bi-funnel" aria-hidden="true"></i>Filtros</h2>
        <form [formGroup]="form" (ngSubmit)="loadReport()">
          <div class="row g-3 align-items-end">
            <div class="col-lg-4 col-md-6">
              <div class="ng-select-container-custom">
                <label class="floating-label" for="electives-period">Período</label>
                <ng-select id="electives-period" formControlName="period" data-filter="period"
                  [items]="periods()" bindLabel="text" bindValue="value" [clearable]="false"
                  class="form-select ng-select-custom" />
              </div>
            </div>
          </div>
        </form>
      </div>

      @if (loadingFilters()) { <p role="status">Cargando períodos…</p> }
      @if (error()) { <div class="alert alert-warning" role="alert">{{ error() }}</div> }

      @if (loaded() && !error()) {
        <div class="form-section">
          <h2 class="section-title"><i class="bi bi-table" aria-hidden="true"></i>Resultados</h2>
          <div class="d-flex justify-content-end mb-3">
            <div class="search-box"><app-search (search)="searchTerm.set($event)" /></div>
          </div>
          <app-table aria-label="Alumnos que completaron electivos" [data]="rows()"
            [columns]="columns" [searchTerm]="searchTerm()" [showSummary]="false" [showRowNumber]="false" />
          <p class="fw-semibold mt-3 mb-0">Total: {{ rows().length }}</p>
        </div>
      }
    </div>
  `,
  styles: '.search-box { width: 20rem; max-width: 100%; }',
})
export class StudentsCompletedElectives {
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
  protected readonly error = signal('');

  protected periodLabel(): string {
    return this.periods().find(option => option.value === this.form.controls.period.value)?.text ?? '';
  }

  constructor() {
    this.form.controls.period.valueChanges.pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(period => {
        this.rows.set([]);
        this.loaded.set(false);
        if (period) this.loadReport();
      });
    this.loadFilters();
  }

  protected loadFilters(): void {
    this.loadingFilters.set(true);
    this.error.set('');
    this.http.get<{ success: boolean; data: { periods: Option[] } }>(BASE + '/filters')
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: result => {
          this.loadingFilters.set(false);
          if (!result.success) { this.error.set('No se pudieron cargar los períodos.'); return; }
          this.periods.set(result.data.periods);
          if (result.data.periods.length === 0) {
            this.error.set('No hay períodos activos para el informe.');
            return;
          }
          this.form.controls.period.setValue(result.data.periods[0].value);
        },
        error: () => {
          this.loadingFilters.set(false);
          this.error.set('No se pudieron cargar los períodos.');
        },
      });
  }

  protected loadReport(): void {
    if (this.loading()) return;
    const period = this.form.controls.period.value;
    if (!period) return;
    this.loading.set(true);
    this.error.set('');
    this.searchTerm.set('');
    this.http.get<{ success: boolean; data: { rows: Row[]; total: number } }>(BASE + '/report', {
      params: { period },
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
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
}
