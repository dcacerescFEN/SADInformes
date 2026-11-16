import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { Search } from '../../shared/components/search';
import { Table, TableAction, TableColumn } from '../../shared/components/table';
import { AuthService } from '../../core/auth/auth-service';
import { NotificationService } from '../../core/services/notification-service';
import { TeacherAvailabilityEmailDispatchService } from './teacher-availability-email-dispatch-service';

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
const EMAIL_ACTION: TableAction = {
  icon: 'bi-envelope', label: 'Enviar correo', actionKey: 'email', iconOnly: true,
  ariaLabel: () => 'Enviar correo',
  cssClass: 'btn btn-sm btn-outline-success',
};
const EDIT_ACTION: TableAction = {
  icon: 'bi-pencil-square', label: 'Editar disponibilidad', actionKey: 'edit', iconOnly: true,
  ariaLabel: () => 'Editar disponibilidad',
  cssClass: 'btn btn-sm btn-outline-primary',
};
const DELETE_ACTION: TableAction = {
  icon: 'bi-trash', label: 'Eliminar disponibilidad', actionKey: 'delete', iconOnly: true,
  ariaLabel: () => 'Eliminar disponibilidad',
  cssClass: 'btn btn-sm btn-outline-danger',
};
const COLUMNS: readonly TableColumn[] = [
  { key: 'code', name: 'Código', sortable: true, multiline: true },
  { key: 'course', name: 'Cátedra', sortable: true, multiline: true },
  { key: 'teacher', name: 'Profesor', sortable: true },
  { key: 'necessary', name: 'Necesarias', sortable: true },
  { key: 'given', name: 'Dadas', sortable: true },
  { key: 'availability', name: 'Disp.', sortable: true, multiline: true },
  { key: 'state', name: 'Estado', sortable: true },
  {
    key: 'observations', name: 'Observaciones', sortable: true,
    popover: (row) => String((row as Row).observations ?? '').trim() || 'Sin observaciones',
    popoverTitle: 'Observaciones',
    popoverIcon: 'bi-journal-text',
    popoverClass: 'audit-popover observation-popover',
    actionVisible: (row) => Boolean(String((row as Row).observations ?? '').trim()),
  },
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
          <div class="d-flex flex-wrap gap-2">
            @if (showEmail()) {
              <button type="button" class="btn btn-availability-all" data-action="email-all"
                [disabled]="emailing() || email.active() || !report()?.rows?.length" (click)="sendAll()">
                <i class="bi bi-envelope-paper me-1" aria-hidden="true"></i>Enviar a todos
              </button>
              <button type="button" class="btn btn-availability-welcome" data-action="email-welcome-pending"
                [disabled]="emailing() || email.active() || !report()?.rows?.length" (click)="sendWelcomePending()">
                <i class="bi bi-envelope-exclamation me-1" aria-hidden="true"></i>Bienvenida pendientes
              </button>
              <button type="button" class="btn btn-availability-pending" data-action="email-pending"
                [disabled]="emailing() || email.active() || !report()?.rows?.length" (click)="sendPending()">
                <i class="bi bi-clock-history me-1" aria-hidden="true"></i>Rezagados
              </button>
            }
            @if (showAdmin()) {
              <button type="button" class="btn btn-clear" data-action="administration"
                (click)="openAdministration()">
                <i class="bi bi-gear me-1" aria-hidden="true"></i>Administración
              </button>
            }
            <button type="button" class="btn btn-search" data-action="refresh"
              [disabled]="loading() || emailing()" (click)="load()">
              <i class="bi bi-arrow-clockwise me-1" aria-hidden="true"></i>
              {{ loading() ? 'Cargando…' : 'Actualizar' }}
            </button>
          </div>
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
            [data]="report()?.rows ?? []" [columns]="columns()" [searchTerm]="searchTerm()"
            [showSummary]="false" [showRowNumber]="false"
            (actionEvent)="handleAction($event)" />
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
  private readonly auth = inject(AuthService);
  private readonly notification = inject(NotificationService);
  private readonly router = inject(Router);
  protected readonly email = inject(TeacherAvailabilityEmailDispatchService);
  protected readonly rowActions = computed<readonly TableAction[]>(() => {
    const actions: TableAction[] = [];
    if (this.showEmail()) actions.push(EMAIL_ACTION);
    if (this.showAdmin()) actions.push(EDIT_ACTION);
    if (this.showDelete()) actions.push(DELETE_ACTION);
    return actions;
  });
  protected readonly columns = computed<readonly TableColumn[]>(() =>
    this.rowActions().length
      ? [...COLUMNS, { key: 'rowActions', name: 'Acciones', additionalActions: this.rowActions() }]
      : COLUMNS);
  protected readonly report = signal<Report | null>(null);
  protected readonly searchTerm = signal('');
  protected readonly loading = signal(false);
  protected readonly emailing = signal(false);
  protected readonly error = signal('');
  protected readonly showEmail = computed(() => this.auth.hasPermission('REPORT_SEND_AVAILABILITY_EMAIL'));
  protected readonly showAdmin = computed(() => this.auth.hasPermission('REPORT_EDIT_AVAILABILITY'));
  protected readonly showDelete = this.showAdmin;

  constructor() { this.load(); }

  protected openAdministration(): void {
    void this.router.navigate(['/academic-enrollment/teacher-availability/settings']);
  }

  protected handleAction(event: { id: string | number | null | undefined; actionKey: string }): void {
    const row = this.report()?.rows.find(item => item.code === String(event.id ?? ''));
    if (!row) return;
    if (event.actionKey === 'email') this.send('single', row.teacherCode);
    if (event.actionKey === 'edit') this.edit(row);
    if (event.actionKey === 'delete') this.delete(row);
  }

  private edit(row: Row): void {
    if (!this.report()) return;
    void this.router.navigate(
      ['/academic-enrollment/teacher-availability', row.teacherCode, 'edit'],
      { queryParams: { period: this.report()!.period } });
  }

  private delete(row: Row): void {
    if (!this.report()) return;
    void this.notification.confirm(
      'Eliminar disponibilidad',
      `Se eliminarán las disponibilidades de ${row.teacher} para el período ${this.report()!.periodName}. ¿Continuar?`,
      'Eliminar', 'Cancelar')
      .then(result => {
        if (!result.isConfirmed) return;
        this.http.delete<void>(BASE + '/report/' + encodeURIComponent(row.teacherCode), {
          params: { period: this.report()!.period },
        }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
          next: () => {
            this.notification.showSuccess('Disponibilidad eliminada', 'Las disponibilidades fueron eliminadas.');
            this.load();
          },
          error: (error: HttpErrorResponse) => {
            const title = error.error?.title;
            void this.notification.alert('Eliminación no completada',
              typeof title === 'string' && title.length > 0 ? title : 'No se pudo eliminar la disponibilidad.');
          },
        });
      });
  }

  protected sendAll(): void { this.send('all'); }
  protected sendWelcomePending(): void { this.send('welcome-pending'); }
  protected sendPending(): void { this.send('pending'); }

  private send(mode: string, teacherCode?: string): void {
    if (this.emailing() || !this.report()) return;
    const message = mode === 'single'
      ? '¿Enviar el correo de disponibilidad a este profesor?'
      : mode === 'all'
        ? 'Se enviará la bienvenida a todos los profesores del período y se marcará CorreoBienvenida.'
        : mode === 'welcome-pending'
          ? 'Se enviará la bienvenida a los profesores con CorreoBienvenida pendiente.'
          : 'Se enviará el recordatorio a todos los profesores en estado PENDIENTE.';
    void this.notification.confirm('Enviar correos', message, 'Enviar', 'Cancelar')
      .then(confirmation => {
        if (!confirmation.isConfirmed) return;
        this.emailing.set(true);
        this.email.start(this.report()!.period, mode, teacherCode)
          .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
            next: () => this.emailing.set(false),
            error: (error: HttpErrorResponse) => {
              this.emailing.set(false);
              const title = error.error?.title;
              void this.notification.alert('Envío no completado',
                typeof title === 'string' && title.length > 0 ? title : 'No se pudo iniciar el envío.');
            },
          });
      });
  }

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
