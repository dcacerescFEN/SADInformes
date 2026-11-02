import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Search } from '../../shared/components/search';
import { Table, TableColumn } from '../../shared/components/table';

interface Row {
  studentCode: string; name: string; career: string; degree: string; status: string;
}
const BASE = '/api/reports/postgraduate/licenciatura-pregrado';
const COLUMNS: readonly TableColumn[] = [
  { key: 'studentCode', name: 'Código Alumno', sortable: true },
  { key: 'name', name: 'Nombre', sortable: true },
  { key: 'career', name: 'Nombre Carrera', sortable: true },
  { key: 'degree', name: 'Grado', sortable: true },
  { key: 'status', name: 'Estado', sortable: true },
];

@Component({
  selector: 'app-licenciatura',
  imports: [Search, Table],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="container-fluid px-0 py-4">
      <div class="page-header">
        <div class="d-flex flex-wrap justify-content-between align-items-start gap-3">
          <div>
            <h1 class="page-title"><i class="bi bi-mortarboard" aria-hidden="true"></i>Próximas Licenciaturas Pregrado</h1>
            <p class="page-subtitle mb-0">Alumnos que cumplen los requisitos para obtener la licenciatura.</p>
          </div>
          <div class="d-flex flex-wrap gap-2">
            <button type="button" class="btn btn-search" data-action="search"
              [disabled]="loading()" (click)="load()">
              <i class="bi bi-search me-1" aria-hidden="true"></i>
              {{ loading() ? 'Buscando…' : 'Buscar' }}
            </button>
            <button type="button" class="btn btn-create" data-action="export"
              [disabled]="rows().length === 0 || exporting()" (click)="download()">
              <i class="bi bi-file-earmark-excel me-1" aria-hidden="true"></i>
              {{ exporting() ? 'Exportando…' : 'Exportar' }}
            </button>
          </div>
        </div>
      </div>
      @if (error()) { <div class="alert alert-warning" role="alert">{{ error() }}</div> }
      @if (rows().length > 0) {
        <div class="form-section">
          <div class="d-flex justify-content-end mb-3">
            <div class="search-box"><app-search (search)="searchTerm.set($event)" /></div>
          </div>
          <app-table aria-label="Próximas licenciaturas Pregrado" [data]="rows()" [columns]="columns"
            [searchTerm]="searchTerm()" [showSummary]="false" />
          <p class="fw-semibold mt-3 mb-0">Total: {{ rows().length }}</p>
        </div>
      } @else if (loaded() && !error()) {
        <p class="text-body-secondary" role="status">Sin Datos</p>
      }
    </div>
  `,
  styles: '.search-box { width: 20rem; max-width: 100%; }',
})
export class Licenciatura {
  private readonly http = inject(HttpClient);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly columns = COLUMNS;
  protected readonly rows = signal<Row[]>([]);
  protected readonly searchTerm = signal('');
  protected readonly loading = signal(false);
  protected readonly exporting = signal(false);
  protected readonly loaded = signal(false);
  protected readonly error = signal('');

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

  protected download(): void {
    if (this.rows().length === 0 || this.exporting()) return;
    this.exporting.set(true);
    this.error.set('');
    this.http.get(BASE + '/download', { responseType: 'blob' })
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: blob => {
          this.exporting.set(false);
          if (blob.size === 0) { this.error.set('Sin Datos'); return; }
          const url = URL.createObjectURL(blob);
          const link = document.createElement('a');
          try {
            link.href = url;
            link.download = 'ProximasLicenciatura.xlsx';
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
