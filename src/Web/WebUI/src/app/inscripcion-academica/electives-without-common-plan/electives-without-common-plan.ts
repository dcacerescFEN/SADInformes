import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpClient } from '@angular/common/http';
import { Search } from '../../shared/components/search';
import { Table, TableColumn } from '../../shared/components/table';

interface Row {
  decree: string;
  courseCode: string;
  courseName: string;
}

const BASE = '/api/reports/academic-enrollment/electives-without-common-plan';
const COLUMNS: readonly TableColumn[] = [
  { key: 'decree', name: 'Decreto', sortable: true },
  { key: 'courseCode', name: 'Código', sortable: true },
  { key: 'courseName', name: 'Nombre', sortable: true },
];

@Component({
  selector: 'app-electives-without-common-plan',
  imports: [Search, Table],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="container-fluid px-0 py-4">
      <div class="page-header">
        <div class="d-flex flex-wrap justify-content-between align-items-start gap-3">
          <div>
            <h1 class="page-title">
              <i class="bi bi-journal-check" aria-hidden="true"></i>
              Electivos de mención que no están en el plan común
            </h1>
            <p class="page-subtitle mb-0">
              Los electivos en esta situación provocan problemas cuando un alumno de Plan Común
              los toma por estar eligiendo mención, y luego vuelve a Plan Común, por haber
              eliminado las obligatorias de mención.
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

      @if (loaded() && !error()) {
        <div class="form-section">
          <h2 class="section-title"><i class="bi bi-table" aria-hidden="true"></i>Resultados</h2>
          <div class="d-flex justify-content-end mb-3">
            <div class="search-box"><app-search (search)="searchTerm.set($event)" /></div>
          </div>
          <app-table aria-label="Electivos de mención que no están en el plan común"
            [data]="rows()" [columns]="columns" [searchTerm]="searchTerm()" [showSummary]="false" [showRowNumber]="false" />
          <p class="fw-semibold mt-3 mb-0">Total: {{ rows().length }}</p>
        </div>
      }
    </div>
  `,
  styles: '.search-box { width: 20rem; max-width: 100%; }',
})
export class ElectivesWithoutCommonPlan {
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
