import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpClient } from '@angular/common/http';
import { Search } from '../../shared/components/search';
import { Table, TableColumn } from '../../shared/components/table';

interface Row {
  studentCode: string;
  name: string;
  academicStatus: string;
  studentType: string;
}

const BASE = '/api/reports/academic-enrollment/students-without-priority';
const COLUMNS: readonly TableColumn[] = [
  { key: 'studentCode', name: 'Código', sortable: true },
  { key: 'name', name: 'Nombre', sortable: true },
  { key: 'academicStatus', name: 'Situación', sortable: true },
  { key: 'studentType', name: 'Tipo Alumno', sortable: true },
];

@Component({
  selector: 'app-students-without-priority',
  imports: [Search, Table],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="container-fluid px-0 py-4">
      <div class="page-header">
        <div class="d-flex flex-wrap justify-content-between align-items-start gap-3">
          <div>
            <h1 class="page-title">
              <i class="bi bi-person-exclamation" aria-hidden="true"></i>
              Alumnos que pueden postular y no tienen prioridad
            </h1>
            <p class="page-subtitle mb-0">
              Alumnos de IC, IICG y CA que pueden postular y tienen prioridad 0.
            </p>
          </div>
          <button type="button" class="btn btn-search" data-action="refresh"
            [disabled]="loading()" (click)="load()">
            <i class="bi bi-arrow-clockwise me-1" aria-hidden="true"></i>
            {{ loading() ? 'Cargando…' : 'Actualizar' }}
          </button>
        </div>
      </div>

      @if (error()) {
        <div class="alert alert-warning" role="alert">{{ error() }}</div>
      }

      @if (!error()) {
        <div class="form-section">
          <h2 class="section-title"><i class="bi bi-table" aria-hidden="true"></i>Resultados</h2>
          <div class="d-flex justify-content-end mb-3">
            <div class="search-box"><app-search (search)="searchTerm.set($event)" /></div>
          </div>
          <app-table [loading]="loading() || !loaded()" aria-label="Alumnos que pueden postular y no tienen prioridad"
            [data]="rows()" [columns]="columns" [searchTerm]="searchTerm()" [showSummary]="false" [showRowNumber]="false" />
          <p class="fw-semibold mt-3 mb-0">Total: {{ rows().length }}</p>
        </div>
      }
    </div>
  `,
  styles: '.search-box { width: 20rem; max-width: 100%; }',
})
export class StudentsWithoutPriority {
  private readonly http = inject(HttpClient);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly columns = COLUMNS;
  protected readonly rows = signal<Row[]>([]);
  protected readonly searchTerm = signal('');
  protected readonly loading = signal(false);
  protected readonly loaded = signal(false);
  protected readonly error = signal('');

  constructor() {
    this.load();
  }

  protected load(): void {
    if (this.loading()) return;
    this.loading.set(true);
    this.error.set('');
    this.searchTerm.set('');
    this.http.get<{ success: boolean; data: { rows: Row[]; total: number } }>(BASE + '/report')
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: result => {
          this.loading.set(false);
          this.loaded.set(true);
          if (!result.success) {
            this.rows.set([]);
            this.error.set('No se pudo cargar el informe.');
            return;
          }
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
