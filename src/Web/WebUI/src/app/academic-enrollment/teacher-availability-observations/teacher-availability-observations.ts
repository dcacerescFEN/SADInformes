import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpClient } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { Search } from '../../shared/components/search';
import { Table, TableColumn } from '../../shared/components/table';

interface Option { value: string; text: string; }
interface Row { teacherCode: string; teacher: string; observations: string; }
interface Report { period: string; periods: Option[]; rows: Row[]; }

const BASE = '/api/reports/academic-enrollment/teacher-availability';
const COLUMNS: readonly TableColumn[] = [
  { key: 'teacherCode', name: 'Cod Profesor', sortable: true },
  { key: 'teacher', name: 'Nombre', sortable: true },
  { key: 'observations', name: 'Observaciones', sortable: true, multiline: true },
];

@Component({
  selector: 'app-teacher-availability-observations',
  imports: [ReactiveFormsModule, NgSelectModule, Search, Table],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="container-fluid px-0 py-4">
      <div class="page-header">
        <div>
          <h1 class="page-title">
            <i class="bi bi-journal-text" aria-hidden="true"></i>
            Disponibilidad Observaciones
          </h1>
          <p class="page-subtitle mb-0">Observaciones de disponibilidad por profesor y período.</p>
        </div>
      </div>

      <div class="form-section mb-3">
        <h2 class="section-title"><i class="bi bi-funnel" aria-hidden="true"></i>Filtros</h2>
        <form [formGroup]="form">
          <div class="row g-3 align-items-end">
            <div class="col-lg-4 col-md-6">
              <div class="ng-select-container-custom">
                <label class="floating-label" for="availability-observations-period">Período</label>
                <ng-select id="availability-observations-period" formControlName="period"
                  data-filter="period" [items]="periods()" bindLabel="text" bindValue="value"
                  [clearable]="false" class="form-select ng-select-custom" />
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
          <app-table [loading]="loading()" aria-label="Disponibilidad observaciones"
            [data]="rows()" [columns]="columns" [searchTerm]="searchTerm()"
            [showSummary]="false" [showRowNumber]="false" />
          <p class="fw-semibold mt-3 mb-0">Total: {{ rows().length }}</p>
        </div>
      }
    </div>
  `,
  styles: '.search-box { width: 20rem; max-width: 100%; }',
})
export class TeacherAvailabilityObservations {
  private readonly http = inject(HttpClient);
  private readonly destroyRef = inject(DestroyRef);
  readonly form = inject(FormBuilder).nonNullable.group({ period: '' });
  protected readonly columns = COLUMNS;
  protected readonly periods = signal<Option[]>([]);
  protected readonly rows = signal<Row[]>([]);
  protected readonly searchTerm = signal('');
  protected readonly loadingFilters = signal(false);
  protected readonly loading = signal(false);
  protected readonly error = signal('');
  private revision = 0;

  constructor() {
    this.form.controls.period.valueChanges.pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(period => { if (period) this.load(period); });
    this.load();
  }

  private load(period?: string): void {
    const revision = ++this.revision;
    this.loadingFilters.set(true);
    this.loading.set(true);
    this.error.set('');
    this.http.get<{ success: boolean; data: Report }>(BASE + '/observations', {
      params: period ? { period } : {},
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: result => {
        if (revision !== this.revision) return;
        this.loadingFilters.set(false);
        this.loading.set(false);
        if (!result.success) { this.error.set('No se pudieron cargar las observaciones.'); return; }
        this.periods.set(result.data.periods);
        this.rows.set(result.data.rows);
        this.form.controls.period.setValue(result.data.period, { emitEvent: false });
      },
      error: () => {
        if (revision !== this.revision) return;
        this.loadingFilters.set(false);
        this.loading.set(false);
        this.error.set('No se pudieron cargar las observaciones.');
      },
    });
  }
}
