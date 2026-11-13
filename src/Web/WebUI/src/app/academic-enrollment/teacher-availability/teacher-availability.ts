import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpClient } from '@angular/common/http';
import { Search } from '../../shared/components/search';
import { Table, TableColumn } from '../../shared/components/table';

interface Row {
  code: string;
  course: string;
  teacherCode: string;
  teacher: string;
  necessary: string;
  given: string;
  availability: string;
  createdAt: string;
  state: string;
  mail: string;
  welcomeEmail: string;
  observations: string;
}
interface Report { period: string; periodName: string; rows: Row[]; pending: number; ok: number; }

const BASE = '/api/reports/academic-enrollment/teacher-availability';
const COLUMNS: readonly TableColumn[] = [
  { key: 'code', name: 'Código', sortable: true, multiline: true },
  { key: 'course', name: 'Cátedra', sortable: true, multiline: true },
  { key: 'teacher', name: 'Profesor', sortable: true },
  { key: 'necessary', name: 'Necesarias', sortable: true },
  { key: 'given', name: 'Dadas', sortable: true },
  { key: 'availability', name: 'Disp.', sortable: true },
  { key: 'state', name: 'Estado', sortable: true },
  { key: 'observations', name: 'Observaciones', sortable: true, multiline: true },
];

@Component({
  selector: 'app-teacher-availability',
  imports: [Search, Table],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="container-fluid px-0 py-4">
      <div class="page-header">
        <div class="d-flex flex-wrap justify-content-between align-items-start gap-3">
          <div>
            <h1 class="page-title">
              <i class="bi bi-calendar-check" aria-hidden="true"></i>
              Disponibilidad Horaria
            </h1>
            <p class="page-subtitle mb-0">
              Disponibilidad registrada por los profesores
              @if (report()?.periodName) { · {{ report()!.periodName }} }.
            </p>
          </div>
          <button type="button" class="btn btn-search" data-action="refresh"
            [disabled]="loading()" (click)="load()">
            <i class="bi bi-arrow-clockwise me-1" aria-hidden="true"></i>
            {{ loading() ? 'Cargando…' : 'Actualizar' }}
          </button>
        </div>
      </div>

      @if (report(); as data) {
        <div class="row g-3 mb-3">
          <div class="col-sm-6 col-lg-3">
            <div class="form-section h-100 py-3">
              <div class="text-body-secondary small">Pendientes</div>
              <div class="fs-3 fw-semibold">{{ data.pending }}</div>
            </div>
          </div>
          <div class="col-sm-6 col-lg-3">
            <div class="form-section h-100 py-3">
              <div class="text-body-secondary small">Ok</div>
              <div class="fs-3 fw-semibold">{{ data.ok }}</div>
            </div>
          </div>
        </div>
      }

      @if (error()) { <div class="alert alert-warning" role="alert">{{ error() }}</div> }

      @if (!error()) {
        <div class="form-section">
          <h2 class="section-title"><i class="bi bi-table" aria-hidden="true"></i>Resultados</h2>
          <div class="d-flex justify-content-end mb-3">
            <div class="search-box"><app-search (search)="searchTerm.set($event)" /></div>
          </div>
          <app-table [loading]="loading()" aria-label="Disponibilidad horaria"
            [data]="report()?.rows ?? []" [columns]="columns" [searchTerm]="searchTerm()"
            [showSummary]="false" [showRowNumber]="false" />
          <p class="fw-semibold mt-3 mb-0">Total: {{ report()?.rows?.length ?? 0 }}</p>
        </div>
      }
    </div>
  `,
  styles: '.search-box { width: 20rem; max-width: 100%; }',
})
export class TeacherAvailability {
  private readonly http = inject(HttpClient);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly columns = COLUMNS;
  protected readonly report = signal<Report | null>(null);
  protected readonly searchTerm = signal('');
  protected readonly loading = signal(false);
  protected readonly error = signal('');

  constructor() { this.load(); }

  protected load(): void {
    if (this.loading()) return;
    this.loading.set(true);
    this.error.set('');
    this.searchTerm.set('');
    this.http.get<{ success: boolean; data: Report }>(BASE + '/report')
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: result => {
          this.loading.set(false);
          if (!result.success) { this.error.set('No se pudo cargar la disponibilidad.'); return; }
          this.report.set(result.data);
        },
        error: () => {
          this.loading.set(false);
          this.error.set('No se pudo cargar la disponibilidad.');
        },
      });
  }
}
