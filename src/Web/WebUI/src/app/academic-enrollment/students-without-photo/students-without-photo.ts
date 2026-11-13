import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Search } from '../../shared/components/search';
import { DynamicTable } from '../../shared/components/dynamic-table';
import { TableColumn } from '../../shared/components/table';
import { NotificationService } from '../../core/services/notification-service';
import { EmailDispatchService } from './email-dispatch-service';

interface Row {
  name: string;
  rut: string;
  studentCode: string;
  studentType: string;
  mobile: string;
  mailFen: string;
  personalMail: string;
}

const BASE = '/api/reports/academic-enrollment/students-without-photo';
const PAGE_SIZE = 10;
const COLUMNS: readonly TableColumn[] = [
  { key: 'name', name: 'Nombre', sortable: true },
  { key: 'rut', name: 'Rut', sortable: true },
  { key: 'studentCode', name: 'Cód. Alumno', sortable: true },
  { key: 'studentType', name: 'Tipo Alumno', sortable: true },
  { key: 'mobile', name: 'Celular', sortable: true },
  { key: 'mailFen', name: 'Mail Fen', sortable: true },
  { key: 'personalMail', name: 'Mail Personal', sortable: true },
];

@Component({
  selector: 'app-students-without-photo',
  imports: [Search, DynamicTable],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="container-fluid px-0 py-4">
      <div class="page-header">
        <div class="d-flex flex-wrap justify-content-between align-items-start gap-3">
          <div>
            <h1 class="page-title">
              <i class="bi bi-person-badge" aria-hidden="true"></i>
              Alumnos sin fotos
            </h1>
            <p class="page-subtitle mb-0">
              Alumnos de Ingeniería Comercial, IICG y Contador Auditor sin fotografía registrada.
              La búsqueda y la paginación se resuelven en el servidor.
            </p>
          </div>
          <div class="d-flex flex-wrap gap-2">
            <button type="button" class="btn btn-search" data-action="refresh"
              [disabled]="loading()" (click)="reload()">
              <i class="bi bi-arrow-clockwise me-1" aria-hidden="true"></i>
              {{ loading() ? 'Cargando…' : 'Actualizar' }}
            </button>
            <button type="button" class="btn btn-search" data-action="email"
              [disabled]="loading() || sending() || email.active()" (click)="sendEmail()">
              <i class="bi bi-envelope me-1" aria-hidden="true"></i>
              {{ sending() ? 'Enviando…' : 'Enviar correo' }}
            </button>
            <button type="button" class="btn btn-create" data-action="export"
              [disabled]="rows().length === 0 || exporting()" (click)="download()">
              <i class="bi bi-file-earmark-excel me-1" aria-hidden="true"></i>
              {{ exporting() ? 'Descargando…' : 'Exportar Excel' }}
            </button>
          </div>
        </div>
      </div>

      @if (error()) { <div class="alert alert-warning" role="alert">{{ error() }}</div> }

      @if (!error()) {
        <div class="form-section">
          <h2 class="section-title"><i class="bi bi-table" aria-hidden="true"></i>Resultados</h2>
          <div class="d-flex justify-content-end mb-3">
            <div class="search-box"><app-search (search)="onSearch($event)" /></div>
          </div>
          <app-dynamic-table
            mode="server"
            [loading]="loading() || !loaded()"
            [data]="rows()"
            [columns]="columns"
            [page]="page()"
            [pageSize]="pageSize"
            [hasMore]="hasMore()"
            [total]="null"
            [showRowNumber]="false"
            [showSummary]="true"
            aria-label="Alumnos sin fotos"
            (pageChange)="changePage($event)" />
          <p class="fw-semibold mt-3 mb-0">Página {{ page() }}@if (hasMore()) { · quedan más resultados }</p>
        </div>
      }
    </div>
  `,
  styles: '.search-box { width: 20rem; max-width: 100%; }',
})
export class StudentsWithoutPhoto {
  private readonly http = inject(HttpClient);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly email = inject(EmailDispatchService);
  private readonly notification = inject(NotificationService);
  protected readonly columns = COLUMNS;
  protected readonly pageSize = PAGE_SIZE;
  protected readonly rows = signal<Row[]>([]);
  protected readonly loading = signal(false);
  protected readonly loaded = signal(false);
  protected readonly exporting = signal(false);
  protected readonly sending = signal(false);
  protected readonly error = signal('');
  protected readonly page = signal(1);
  protected readonly hasMore = signal(false);
  private searchTerm = '';
  private revision = 0;
  private searchTimer?: ReturnType<typeof setTimeout>;

  constructor() {
    this.load(1);
    this.destroyRef.onDestroy(() => {
      if (this.searchTimer) clearTimeout(this.searchTimer);
    });
  }

  protected onSearch(term: string): void {
    this.searchTerm = term.trim();
    if (this.searchTimer) clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => {
      this.page.set(1);
      this.load(1);
    }, 300);
  }

  protected changePage(target: number): void {
    if (target < 1 || target === this.page() || (target > this.page() && !this.hasMore())) return;
    this.page.set(target);
    this.load(target);
  }

  protected reload(): void {
    this.load(this.page());
  }

  private load(page: number): void {
    const revision = ++this.revision;
    this.loading.set(true);
    this.error.set('');
    this.http.get<{
      success: boolean;
      data: { rows: Row[]; page: number; hasMore: boolean };
    }>(BASE + '/report', { params: { page, pageSize: PAGE_SIZE, search: this.searchTerm } })
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: result => {
          if (revision !== this.revision) return;
          this.loading.set(false);
          this.loaded.set(true);
          if (!result.success) { this.rows.set([]); this.error.set('No se pudo cargar el informe.'); return; }
          this.rows.set(result.data.rows);
          this.hasMore.set(result.data.hasMore);
        },
        error: () => {
          if (revision !== this.revision) return;
          this.loading.set(false);
          this.loaded.set(true);
          this.rows.set([]);
          this.hasMore.set(false);
          this.error.set('No se pudo cargar el informe.');
        },
      });
  }

  protected sendEmail(): void {
    if (this.sending() || this.email.active()) return;
    this.sending.set(true);
    this.email.availability().subscribe({
      next: availability => {
        this.sending.set(false);
        if (!availability.success) {
          void this.notification.alert('Envío no disponible', 'El envío de correo está desactivado.');
          return;
        }
        if (!availability.data.enabled) {
          void this.notification.alert('Envío desactivado', 'El envío de correo está desactivado.');
          return;
        }
        if (!availability.data.smtpConfigured) {
          void this.notification.alert('Envío no disponible', 'La configuración SMTP no está disponible.');
          return;
        }
        void this.notification.confirmHtml(
          'Enviar correo',
          '<p>Se cargará la lista de alumnos sin foto y luego se enviarán los correos.</p>'
            + '<p class="mb-0">El envío continuará en segundo plano y podrás minimizar el progreso.</p>',
          'Aceptar y enviar',
          'Cancelar',
        ).then(confirmation => {
          if (!confirmation.isConfirmed) return;
          this.sending.set(true);
          this.email.start().subscribe({
            next: () => this.sending.set(false),
            error: (error: HttpErrorResponse) => {
              this.sending.set(false);
              const title = error.error?.title;
              void this.notification.alert(
                'No se pudo iniciar el envío',
                typeof title === 'string' && title.length > 0
                  ? title
                  : 'El envío de correo no está disponible.',
              );
            },
          });
        });
      },
      error: () => {
        this.sending.set(false);
        void this.notification.alert('Envío no disponible', 'El envío de correo está desactivado.');
      },
    });
  }

  protected download(): void {
    if (this.rows().length === 0 || this.exporting()) return;
    this.exporting.set(true);
    this.error.set('');
    this.http.get(BASE + '/export', { responseType: 'blob' })
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: blob => {
          this.exporting.set(false);
          if (blob.size === 0) { this.error.set('Sin datos.'); return; }
          const url = URL.createObjectURL(blob);
          const link = document.createElement('a');
          try {
            link.href = url;
            link.download = 'SinFotos.xlsx';
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
