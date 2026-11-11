import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpClient } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { Search } from '../../shared/components/search';
import { Table, TableColumn } from '../../shared/components/table';

interface Option { value: string; text: string; }
interface ProcessOption extends Option { start: string; end: string; }
interface Row {
  studentCode: string;
  name: string;
  studentType: string;
  courseCode: string;
  courseName: string;
  fromSection: string;
  toSection: string;
  result: string;
}

const BASE = '/api/reports/academic-enrollment/permutation-results-by-student';
const COLUMNS: readonly TableColumn[] = [
  { key: 'studentCode', name: 'Código', sortable: true },
  { key: 'name', name: 'Nombre', sortable: true },
  { key: 'studentType', name: 'Tipo Alumno', sortable: true },
  { key: 'courseCode', name: 'Cátedra', sortable: true },
  { key: 'courseName', name: 'Nom.Cátedra', sortable: true },
  { key: 'fromSection', name: 'Desde', sortable: true },
  { key: 'toSection', name: 'Hacia', sortable: true },
  { key: 'result', name: 'Resultado', sortable: true },
];

@Component({
  selector: 'app-permutation-results-by-student',
  imports: [ReactiveFormsModule, NgSelectModule, Search, Table],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="container-fluid px-0 py-4">
      <div class="page-header">
        <div class="d-flex flex-wrap justify-content-between align-items-start gap-3">
          <div>
            <h1 class="page-title">
              <i class="bi bi-arrow-left-right" aria-hidden="true"></i>
              Resultados Procesamiento Masivo Permuta
            </h1>
            <p class="page-subtitle mb-0">
              Resultados por alumno del procesamiento de permuta
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
            <div class="col-lg-3 col-md-6">
              <div class="ng-select-container-custom">
                <label class="floating-label" for="permutation-period">Período</label>
                <ng-select id="permutation-period" formControlName="period" data-filter="period"
                  [items]="periods()" bindLabel="text" bindValue="value" [clearable]="false"
                  class="form-select ng-select-custom" />
              </div>
            </div>
            <div class="col-lg-3 col-md-6">
              <div class="ng-select-container-custom">
                <label class="floating-label" for="permutation-process">Rango</label>
                <ng-select id="permutation-process" formControlName="process" data-filter="process"
                  [items]="processes()" bindLabel="text" bindValue="value" [clearable]="false"
                  [loading]="loadingProcesses()" class="form-select ng-select-custom" />
              </div>
            </div>
            <div class="col-lg-3 col-md-6">
              <div class="form-floating">
                <input id="permutation-from" type="datetime-local" class="form-control"
                  formControlName="from" placeholder="Desde" required />
                <label for="permutation-from">Desde</label>
              </div>
            </div>
            <div class="col-lg-3 col-md-6">
              <div class="form-floating">
                <input id="permutation-to" type="datetime-local" class="form-control"
                  formControlName="to" placeholder="Hasta" required />
                <label for="permutation-to">Hasta</label>
              </div>
            </div>
          </div>
          <p class="text-body-secondary mt-3 mb-0">
            El rango selecciona las fechas del proceso; puede ajustarlas antes de actualizar.
          </p>
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
          <app-table [loading]="loading() || !loaded()" aria-label="Resultados de permuta por alumno" [data]="rows()"
            [columns]="columns" [searchTerm]="searchTerm()" [showSummary]="false" [showRowNumber]="false" />
          <p class="fw-semibold mt-3 mb-0">Total: {{ rows().length }}</p>
        </div>
      }
    </div>
  `,
  styles: '.search-box { width: 20rem; max-width: 100%; }',
})
export class PermutationResultsByStudent {
  private readonly http = inject(HttpClient);
  private readonly destroyRef = inject(DestroyRef);
  readonly form = inject(FormBuilder).nonNullable.group({
    period: '', process: '', from: '', to: '',
  });
  protected readonly columns = COLUMNS;
  protected readonly periods = signal<Option[]>([]);
  protected readonly processes = signal<ProcessOption[]>([]);
  protected readonly rows = signal<Row[]>([]);
  protected readonly searchTerm = signal('');
  protected readonly loadingFilters = signal(false);
  protected readonly loadingProcesses = signal(false);
  protected readonly loading = signal(false);
  protected readonly loaded = signal(false);
  protected readonly error = signal('');
  private processRevision = 0;

  protected periodLabel(): string {
    return this.periods().find(option => option.value === this.form.controls.period.value)?.text ?? '';
  }

  constructor() {
    this.form.controls.period.valueChanges.pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(period => {
        this.rows.set([]);
        this.loaded.set(false);
        this.form.controls.process.setValue('', { emitEvent: false });
        if (period) this.loadProcesses(period);
      });

    this.form.controls.process.valueChanges.pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(processId => {
        const process = this.processes().find(option => option.value === processId);
        if (!process) return;
        this.form.patchValue({ from: process.start, to: process.end }, { emitEvent: false });
        this.loadReport();
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
            this.error.set('No hay períodos activos para permuta.');
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

  protected loadProcesses(period: string): void {
    const revision = ++this.processRevision;
    this.loadingProcesses.set(true);
    this.processes.set([]);
    this.error.set('');
    this.http.get<{ success: boolean; data: { processes: ProcessOption[] } }>(BASE + '/processes', {
      params: { period },
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: result => {
        if (revision !== this.processRevision) return;
        this.loadingProcesses.set(false);
        if (!result.success) { this.error.set('No se pudieron cargar los rangos.'); return; }
        this.processes.set(result.data.processes);
        if (result.data.processes.length === 0) {
          this.error.set('No hay rangos de proceso para el período seleccionado.');
          return;
        }
        this.form.controls.process.setValue(result.data.processes[0].value);
      },
      error: () => {
        if (revision !== this.processRevision) return;
        this.loadingProcesses.set(false);
        this.error.set('No se pudieron cargar los rangos.');
      },
    });
  }

  protected loadReport(): void {
    if (this.loading()) return;
    const { period, from, to } = this.form.getRawValue();
    if (!period || !from || !to) return;
    this.loading.set(true);
    this.error.set('');
    this.searchTerm.set('');
    this.http.get<{ success: boolean; data: { rows: Row[]; total: number } }>(BASE + '/report', {
      params: { period, from, to },
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
