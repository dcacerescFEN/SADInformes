import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Search } from '../../shared/components/search';
import { Table, TableColumn } from '../../shared/components/table';

interface ReportRow {
  id: string; date: string; cause: string; studentCode: string; studentType: string;
  modality: string; academicSituation: string; reason: string; name: string;
}
const BASE = '/api/reports/postgraduate/nce';
const COLUMNS: readonly TableColumn[] = [
  { key: 'id', name: 'ID', sortable: true },
  { key: 'date', name: 'Fecha', sortable: true },
  { key: 'cause', name: 'Causal', sortable: true },
  { key: 'studentCode', name: 'Código Alumno', sortable: true },
  { key: 'studentType', name: 'Tipo Alumno', sortable: true },
  { key: 'modality', name: 'Modalidad', sortable: true },
  { key: 'academicSituation', name: 'Situación Académica', sortable: true },
  { key: 'reason', name: 'Motivo', sortable: true },
  { key: 'name', name: 'Nombre', sortable: true },
];

@Component({
  selector: 'app-nce',
  imports: [Search, Table],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="container-fluid px-0 py-4">
      <div class="page-header">
        <div class="d-flex flex-wrap justify-content-between align-items-start gap-3">
          <div>
            <h1 class="page-title"><i class="bi bi-bell" aria-hidden="true"></i>Informe Notificaciones Causal de Eliminación</h1>
            <p class="page-subtitle mb-0">Notificaciones NCE de Postgrado, en solo lectura.</p>
          </div>
          @if (loaded() && !error()) {
            <div class="d-flex flex-wrap gap-2">
              <button type="button" class="btn btn-export-excel" data-action="export-excel"
                [disabled]="exporting()" (click)="downloadExcel()">
                <i class="bi bi-file-earmark-excel me-1" aria-hidden="true"></i>Excel
              </button>
              <button type="button" class="btn btn-create" data-action="export-csv" (click)="downloadCsv()">
                <i class="bi bi-filetype-csv me-1" aria-hidden="true"></i>CSV
              </button>
              <button type="button" class="btn btn-export-pdf" data-action="export-pdf"
                [disabled]="exportingPdf()" (click)="downloadPdf()">
                <i class="bi bi-file-earmark-pdf me-1" aria-hidden="true"></i>
                {{ exportingPdf() ? 'Generando…' : 'PDF' }}
              </button>
            </div>
          }
        </div>
      </div>
      @if (loading()) { <p role="status">Cargando informe…</p> }
      @if (error()) { <div class="alert alert-warning" role="alert">{{ error() }}</div> }
      @if (loaded() && !error()) {
        <div class="form-section">
          <h2 class="section-title"><i class="bi bi-table" aria-hidden="true"></i>Resultados</h2>
          <div class="d-flex justify-content-end mb-3">
            <div class="search-box"><app-search (search)="searchTerm.set($event)" /></div>
          </div>
          <app-table aria-label="Informe de notificaciones NCE" [data]="rows()" [columns]="columns"
            [searchTerm]="searchTerm()" [showSummary]="false" />
        </div>
      }
    </div>
  `,
  styles: '.search-box { width: 20rem; max-width: 100%; }',
})
export class Nce {
  private readonly http = inject(HttpClient);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly rows = signal<ReportRow[]>([]);
  protected readonly columns = COLUMNS;
  protected readonly loading = signal(false);
  protected readonly loaded = signal(false);
  protected readonly exporting = signal(false);
  protected readonly exportingPdf = signal(false);
  protected readonly error = signal('');
  protected readonly searchTerm = signal('');

  constructor() {
    this.loadReport();
  }

  protected downloadExcel(): void {
    if (this.exporting()) return;
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
            link.href = url; link.download = 'Informe Notificaciones Causal de Eliminación.xlsx';
            document.body.appendChild(link); link.click();
          } finally { link.remove(); URL.revokeObjectURL(url); }
        },
        error: (error: HttpErrorResponse) => {
          this.exporting.set(false);
          this.error.set(error.status === 400 ? 'Sin Datos' : 'No se pudo exportar el informe.');
        },
      });
  }

  protected downloadCsv(): void {
    const headers = ['Fila', 'ID', 'Fecha', 'Causal', 'Código Alumno', 'Tipo Alumno',
      'Modalidad', 'Situación Académica', 'Motivo', 'Nombre'];
    const lines = [headers.map(value => this.csv(value)).join(';')];
    this.rows().forEach((row, index) => lines.push([
      String(index + 1), row.id, row.date, row.cause, row.studentCode, row.studentType,
      row.modality, row.academicSituation, row.reason, row.name,
    ].map(value => this.csv(value)).join(';')));
    const blob = new Blob(['\uFEFF' + lines.join('\r\n')], { type: 'text/csv;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    try {
      link.href = url; link.download = 'Informe Notificaciones Causal de Eliminación.csv';
      document.body.appendChild(link); link.click();
    } finally { link.remove(); URL.revokeObjectURL(url); }
  }

  protected downloadPdf(): void {
    if (this.exportingPdf()) return;
    this.exportingPdf.set(true);
    this.error.set('');
    this.http.get(BASE + '/download/pdf', { responseType: 'blob' })
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: blob => {
          this.exportingPdf.set(false);
          if (blob.size === 0) { this.error.set('Sin Datos'); return; }
          const url = URL.createObjectURL(blob);
          const link = document.createElement('a');
          try {
            link.href = url;
            link.download = 'Informe Notificaciones Causal de Eliminación.pdf';
            document.body.appendChild(link); link.click();
          } finally { link.remove(); URL.revokeObjectURL(url); }
        },
        error: (error: HttpErrorResponse) => {
          this.exportingPdf.set(false);
          this.error.set(error.status === 400 ? 'Sin Datos' : 'No se pudo generar el PDF.');
        },
      });
  }

  private loadReport(): void {
    this.loading.set(true);
    this.error.set('');
    this.http.get<{ success: boolean; data: { rows: ReportRow[]; total: number } }>(BASE + '/report')
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: result => {
          this.loading.set(false);
          this.loaded.set(true);
          if (!result.success) { this.error.set('No se pudo cargar el informe.'); return; }
          this.rows.set(result.data.rows);
        },
        error: () => {
          this.loading.set(false);
          this.loaded.set(true);
          this.error.set('No se pudo cargar el informe.');
        },
      });
  }

  private csv(value: string): string {
    const safe = /^[=+\-@]/.test(value) ? `'${value}` : value;
    return `"${safe.replaceAll('"', '""')}"`;
  }
}
