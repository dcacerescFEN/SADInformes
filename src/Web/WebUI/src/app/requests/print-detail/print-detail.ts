import { Location } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';
import { ReportViewer } from '../../shared/components/report-viewer/report-viewer';
import { Search } from '../../shared/components/search';
import { Table, TableColumn } from '../../shared/components/table';

interface SourceRow {
  personCode: string;
  studentCode: string;
  name: string;
  requestCode: string;
  rawRequestCode: string;
  date: string;
  type: string;
  situation: string;
  comment: string;
  firstRequest: string;
  lastRequest: string;
}
interface Report { career: string; status: string; type: string; rows: SourceRow[]; }

const BASE = '/api/reports/requests/by-status';

@Component({
  selector: 'app-request-print-detail',
  imports: [ReportViewer, Search, Table],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="container-fluid px-0 py-4">
      <div class="page-header">
        <div class="d-flex flex-wrap justify-content-between align-items-start gap-3">
          <div>
            <h1 class="page-title">
              <i class="bi bi-printer" aria-hidden="true"></i>
              Imprimir detalle de solicitudes
            </h1>
            <p class="page-subtitle mb-0">
              Carrera {{ career }} · Estado {{ status }} · Tipo {{ type }}.
              Seleccione una solicitud de la lista para ver su reporte.
            </p>
          </div>
          <button type="button" class="btn btn-clear" data-action="back" (click)="goBack()">
            <i class="bi bi-arrow-left me-1" aria-hidden="true"></i>Volver
          </button>
        </div>
      </div>

      @if (error()) { <div class="alert alert-warning" role="alert">{{ error() }}</div> }

      <div class="print-detail">
        <section class="print-detail__viewer" aria-label="Detalle de la solicitud seleccionada">
          @if (selected(); as row) {
            <app-report-viewer [requestCode]="row.rawRequestCode" />
          } @else if (loading()) {
            <div class="print-detail__empty" role="status">
              <span class="spinner-border text-primary" aria-hidden="true"></span>
              <p class="mb-0">Cargando solicitudes...</p>
            </div>
          } @else if (!rows().length && !error()) {
            <div class="print-detail__empty">
              <i class="bi bi-inbox" aria-hidden="true"></i>
              <p class="mb-0">No hay solicitudes para imprimir.</p>
            </div>
          }
        </section>

        <aside class="print-detail__list">
          <h2 class="section-title"><i class="bi bi-list-ul" aria-hidden="true"></i>Solicitudes imprimibles</h2>
          <div class="search-box mb-3"><app-search (search)="searchTerm.set($event)" /></div>
          <app-table [loading]="loading()" aria-label="Solicitudes imprimibles"
            [data]="rows()" [columns]="columns" [searchTerm]="searchTerm()" [rowId]="rowId"
            [enableRowClick]="true" [showSummary]="false" [showRowNumber]="false"
            (rowClick)="selectRow($event)" />
        </aside>
      </div>
    </div>
  `,
  styles: `
    .print-detail {
      display: grid;
      grid-template-columns: minmax(0, 1.7fr) minmax(20rem, 1fr);
      gap: 1rem;
      align-items: start;
    }
    .print-detail__viewer,
    .print-detail__list {
      min-width: 0;
    }
    .print-detail__list {
      padding: 1.25rem;
      background: var(--layout-surface-strong);
      border: 1px solid var(--layout-border-color);
      border-radius: var(--card-radius);
      box-shadow: var(--card-shadow);
    }
    .print-detail__empty {
      display: grid;
      place-items: center;
      gap: 0.75rem;
      min-height: 24rem;
      padding: 2rem;
      text-align: center;
      color: var(--layout-muted-text);
      background: var(--layout-surface-strong);
      border: 1px solid var(--layout-border-color);
      border-radius: var(--card-radius);
    }
    .print-detail__empty i {
      font-size: 2rem;
    }
    .search-box {
      width: 100%;
    }
    @media (max-width: 991.98px) {
      .print-detail {
        grid-template-columns: minmax(0, 1fr);
      }
    }
  `,
})
export class RequestPrintDetail {
  private readonly http = inject(HttpClient);
  private readonly destroyRef = inject(DestroyRef);
  private readonly location = inject(Location);
  private readonly queryParams = inject(ActivatedRoute).snapshot.queryParamMap;

  protected readonly career = this.readParam('career', 'IC');
  protected readonly status = this.readParam('status', 'NO IMPRESA');
  protected readonly type = this.readParam('type', '67');

  protected readonly rows = signal<SourceRow[]>([]);
  protected readonly selected = signal<SourceRow | null>(null);
  protected readonly searchTerm = signal('');
  protected readonly loading = signal(true);
  protected readonly error = signal('');
  protected readonly columns: readonly TableColumn[] = [
    { key: 'requestCode', name: 'Solicitud', sortable: true },
    { key: 'name', name: 'Alumno', sortable: true },
    { key: 'type', name: 'Tipo', sortable: true, multiline: true },
  ];
  protected readonly rowId = (row: object) => (row as SourceRow).rawRequestCode;

  constructor() {
    this.loadReport();
  }

  protected selectRow(row: object): void {
    this.selected.set(row as SourceRow);
  }

  protected goBack(): void {
    this.location.back();
  }

  private loadReport(): void {
    this.loading.set(true);
    this.error.set('');
    this.http.get<{ success: boolean; data: Report }>(BASE + '/report', {
      params: { career: this.career, status: this.status, type: this.type },
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: result => {
        this.loading.set(false);
        if (!result.success || !result.data) {
          this.error.set('No se pudo cargar la lista de solicitudes.');
          return;
        }
        const rows = result.data.rows.map(row => ({ ...row }));
        this.rows.set(rows);
        this.selected.set(rows[0] ?? null);
      },
      error: () => {
        this.loading.set(false);
        this.rows.set([]);
        this.selected.set(null);
        this.error.set('No se pudo cargar la lista de solicitudes.');
      },
    });
  }

  private readParam(name: string, fallback: string): string {
    return this.queryParams.get(name)?.trim() || fallback;
  }
}
