import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpClient } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { NgSelectModule } from '@ng-select/ng-select';
import { NavigationService } from '../../core/navigation/navigation-service';
import { AuthService } from '../../core/auth/auth-service';
import { NotificationService } from '../../core/services/notification-service';
import { Search } from '../../shared/components/search';
import { Table, TableAction, TableColumn } from '../../shared/components/table';

interface Option { value: string; text: string; }
interface SourceRow {
  personCode: string; studentCode: string; name: string; requestCode: string; rawRequestCode: string; date: string;
  type: string; situation: string; comment: string; firstRequest: string; lastRequest: string;
}
interface Row extends SourceRow { photoUrl: string; }
interface Report { career: string; status: string; type: string; rows: SourceRow[]; }

const BASE = '/api/reports/requests/by-status';
const DEFAULT_PHOTO = 'https://fotos.fen.uchile.cl/perfilNN.jpg';

@Component({
  selector: 'app-requests-by-status',
  imports: [ReactiveFormsModule, NgSelectModule, Search, Table],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="container-fluid px-0 py-4">
      <div class="page-header">
        <div class="d-flex flex-wrap justify-content-between align-items-start gap-3">
          <div>
            <h1 class="page-title">
              <i class="bi bi-inbox" aria-hidden="true"></i>
              {{ title }}
            </h1>
            <p class="page-subtitle mb-0">
              @if (isPending) { Solicitudes pendientes de tramitación por carrera. }
              @else { Solicitudes pendientes de impresión por carrera. }
              @if (range()) { · Rango {{ range() }} }.
            </p>
          </div>
          @if (showPrint()) {
            <div class="d-flex flex-wrap gap-2">
              <button type="button" class="btn btn-create" data-action="confirm-print"
                [disabled]="confirming() || rows().length === 0" (click)="confirmPrint()">
                <i class="bi bi-check2-square me-1" aria-hidden="true"></i>
                {{ confirming() ? 'Confirmando…' : 'Confirmar impresión' }}
              </button>
              @if (rows().length > 0) {
                <button type="button" class="btn btn-primary" data-action="print-detail"
                  (click)="openPrintDetail()">
                  <i class="bi bi-printer me-1" aria-hidden="true"></i>
                  Imprimir detalle
                </button>
              }
            </div>
          }
        </div>
      </div>

      <div class="form-section mb-3">
        <h2 class="section-title"><i class="bi bi-funnel" aria-hidden="true"></i>Filtros</h2>
        <form [formGroup]="form">
          <div class="row g-3">
            <div class="col-lg-4 col-md-6">
              <div class="ng-select-container-custom">
                <label class="floating-label" for="status-career">Carrera</label>
                <ng-select id="status-career" formControlName="career" data-filter="career"
                  [items]="careers" bindLabel="text" bindValue="value" [clearable]="false"
                  [searchable]="true" appendTo="body" class="form-select ng-select-custom" />
              </div>
            </div>
            @if (showType) {
              <div class="col-lg-5 col-md-6">
                <div class="ng-select-container-custom">
                  <label class="floating-label" for="status-type">Tipo de solicitud</label>
                  <ng-select id="status-type" formControlName="type" data-filter="type"
                    [items]="types()" bindLabel="text" bindValue="value" [clearable]="false"
                    [searchable]="true" appendTo="body" class="form-select ng-select-custom" />
                </div>
              </div>
            }
          </div>
        </form>
      </div>

      @if (error()) { <div class="alert alert-warning" role="alert">{{ error() }}</div> }

      @if (!error()) {
        <div class="form-section">
          <h2 class="section-title"><i class="bi bi-table" aria-hidden="true"></i>Resultados</h2>
          <div class="d-flex justify-content-end mb-3">
            <div class="search-box"><app-search (search)="searchTerm.set($event)" /></div>
          </div>
          <app-table [loading]="loading() || !loaded()" aria-label="Solicitudes no impresas"
            [data]="rows()" [columns]="columns()" [searchTerm]="searchTerm()"
            [rowId]="rowId" (actionEvent)="handleAction($event)"
            [showSummary]="false" [showRowNumber]="false" />
          <p class="fw-semibold mt-3 mb-0">Total: {{ rows().length }}</p>
        </div>
      }
    </div>
  `,
  styles: '.search-box { width: 20rem; max-width: 100%; }',
})
export class RequestsByStatus {
  private readonly http = inject(HttpClient);
  private readonly destroyRef = inject(DestroyRef);
  private readonly navigation = inject(NavigationService);
  private readonly auth = inject(AuthService);
  private readonly notification = inject(NotificationService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly requestType = (() => {
    const value = this.route.snapshot.data['requestType'];
    return value === '65' || value === 'OTROS' || value === '%' ? value : '67';
  })();
  protected readonly title = this.requestType === '65'
    ? 'Solicitudes no impresas · Homologación de Seminario X Electivos Magister'
    : this.requestType === 'OTROS'
      ? 'Solicitudes no impresas · Otros Tipos'
      : this.requestType === '%'
        ? 'Solicitudes pendientes'
        : 'Solicitudes no impresas · Reconocimiento de Cátedras Aprobadas en Postgrado';
  protected readonly isPending = this.requestType === '%';
  private readonly requestStatus = this.isPending ? 'PENDIENTE' : 'NO IMPRESA';
  protected readonly showType = this.requestType === 'OTROS' || this.isPending;
  protected readonly showPrint = computed(() => !this.isPending
    && this.auth.hasPermission('REPORT_PRINT_REQUESTS'));
  protected readonly confirming = signal(false);
  protected readonly showCommentEdit = computed(() =>
    this.auth.hasPermission('REPORT_EDIT_REQUEST_VALIDATION'));
  protected readonly editingRequest = signal<string | null>(null);
  protected readonly draftComment = signal('');
  protected readonly savingComment = signal(false);
  protected readonly rowId = (row: object) => (row as Row).rawRequestCode;
  readonly form = inject(FormBuilder).nonNullable.group({
    career: 'IC', type: this.isPending ? '%' : 'OTROS',
  });
  protected readonly careers: readonly Option[] = [
    { value: 'IC', text: 'Ingeniería Comercial' },
    { value: 'IICG', text: 'Ingeniería en Información y Control de Gestión' },
    { value: 'CA', text: 'Contador Auditor' },
  ];
  protected readonly types = signal<Option[]>([]);
  protected readonly rows = signal<Row[]>([]);
  protected readonly searchTerm = signal('');
  protected readonly loading = signal(false);
  protected readonly loaded = signal(false);
  protected readonly error = signal('');
  protected readonly range = signal('');
  protected readonly columns = computed<readonly TableColumn[]>(() => {
    const columns: TableColumn[] = [
      { key: 'date', name: 'Fecha', sortable: true },
      { key: 'requestCode', name: 'Solicitud', sortable: true },
      { key: 'type', name: 'Tipo', sortable: true, multiline: true },
      { key: 'name', name: 'Alumno', sortable: true,
        link: (row) => this.navigation.studentRequestsUrl((row as Row).studentCode) },
      { key: 'photoUrl', name: 'Foto', sortable: false },
      { key: 'situation', name: 'Situación Académica', sortable: true, multiline: true },
      { key: 'comment', name: 'Comentario Validación', sortable: true, multiline: true,
        editText: {
          editing: (row) => this.editingRequest() === (row as Row).rawRequestCode,
          value: () => this.draftComment(),
          maxLength: 400,
          input: (_row, value) => this.draftComment.set(value),
        } },
    ];
    if (this.showCommentEdit()) {
      const editing = (row: object) => this.editingRequest() === (row as Row).rawRequestCode;
      columns.push({
        key: 'commentAction', name: 'Acciones',
        additionalActions: [
          {
            icon: 'bi-pencil', label: 'Editar comentario', actionKey: 'edit-comment',
            iconOnly: true, cssClass: 'btn btn-sm btn-outline-primary',
            visible: (row) => !editing(row),
          },
          {
            icon: 'bi-save', label: 'Guardar comentario', actionKey: 'save-comment',
            iconOnly: true, cssClass: 'btn btn-sm btn-outline-success',
            visible: editing,
          },
          {
            icon: 'bi-x-circle', label: 'Cancelar edición', actionKey: 'cancel-comment',
            iconOnly: true, cssClass: 'btn btn-sm btn-outline-secondary',
            visible: editing,
          },
        ],
      });
    }
    return columns;
  });
  private revision = 0;

  constructor() {
    if (this.showType) {
      this.form.controls.career.valueChanges.pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe(() => this.loadTypes());
      this.form.controls.type.valueChanges.pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe(() => this.loadReport());
      this.loadTypes();
    } else {
      this.form.controls.career.valueChanges.pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe(() => this.loadReport());
      this.loadReport();
    }
  }

  protected loadTypes(): void {
    const revision = ++this.revision;
    this.loading.set(true);
    this.loaded.set(false);
    this.rows.set([]);
    this.editingRequest.set(null);
    this.range.set('');
    this.error.set('');
    const career = this.form.controls.career.value;
    this.http.get<{ success: boolean; data: { types: Option[] } }>(BASE + '/types', {
      params: {
        career, status: this.requestStatus,
        typeFilter: this.isPending ? '%' : 'OTROS',
      },
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: result => {
        if (revision !== this.revision) return;
        this.loading.set(false);
        if (!result.success) { this.error.set('No se pudieron cargar los tipos.'); return; }
        const defaultType = this.isPending ? '%' : 'OTROS';
        const types = result.data.types.some(option => option.value === defaultType)
          ? result.data.types
          : [{ value: defaultType, text: 'TODOS' }, ...result.data.types];
        this.types.set(types);
        this.form.patchValue({ type: defaultType }, { emitEvent: false });
        this.loadReport();
      },
      error: () => {
        if (revision !== this.revision) return;
        this.loading.set(false);
        this.error.set('No se pudieron cargar los tipos.');
      },
    });
  }

  protected openPrintDetail(): void {
    const value = this.form.getRawValue();
    void this.router.navigate(['/requests/print-detail'], {
      queryParams: {
        career: value.career,
        status: this.requestStatus,
        type: this.showType ? value.type : this.requestType,
      },
    });
  }

  protected confirmPrint(): void {
    const codes = this.rows().map(row => row.rawRequestCode).filter(code => code.length > 0);
    if (codes.length === 0 || this.confirming()) return;
    void this.notification.confirm(
      'Confirmar impresión',
      `Se marcarán ${codes.length} solicitudes como impresas. ¿Continuar?`,
      'Confirmar', 'Cancelar')
      .then(result => {
        if (!result.isConfirmed) return;
        this.confirming.set(true);
        this.http.post<{ success: boolean; data: number }>(BASE + '/confirm-print', {
          requestCodes: codes,
        }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
          next: result => {
            this.confirming.set(false);
            if (!result.success) { this.error.set('No se pudo confirmar la impresión.'); return; }
            this.notification.showSuccess('Impresión confirmada',
              `Se marcaron ${result.data} solicitudes como impresas.`);
            this.loadReport();
          },
          error: () => {
            this.confirming.set(false);
            this.error.set('No se pudo confirmar la impresión.');
          },
        });
      });
  }

  protected handleAction(event: { id: string | number | null | undefined; actionKey: string }): void {
    const row = this.rows().find(item => item.rawRequestCode === String(event.id ?? ''));
    if (!row) return;
    if (event.actionKey === 'edit-comment') this.startCommentEdit(row);
    if (event.actionKey === 'save-comment') this.saveComment(row);
    if (event.actionKey === 'cancel-comment') this.cancelComment();
  }

  protected startCommentEdit(row: Row): void {
    this.editingRequest.set(row.rawRequestCode);
    this.draftComment.set(row.comment);
  }

  protected cancelComment(): void {
    this.editingRequest.set(null);
    this.draftComment.set('');
  }

  protected saveComment(row: Row): void {
    if (this.savingComment()) return;
    this.savingComment.set(true);
    this.http.put<void>(BASE + '/comment', {
      requestCode: row.rawRequestCode, comment: this.draftComment(),
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.savingComment.set(false);
        this.rows.update(rows => rows.map(item => item.rawRequestCode === row.rawRequestCode
          ? { ...item, comment: this.draftComment().trim() } : item));
        this.editingRequest.set(null);
        this.notification.showSuccess('Comentario guardado', 'El comentario fue actualizado.');
      },
      error: () => {
        this.savingComment.set(false);
        this.error.set('No se pudo guardar el comentario.');
      },
    });
  }

  protected loadReport(): void {
    const value = this.form.getRawValue();
    const revision = ++this.revision;
    this.loading.set(true);
    this.loaded.set(false);
    this.rows.set([]);
    this.range.set('');
    this.error.set('');
    this.searchTerm.set('');
    this.http.get<{ success: boolean; data: Report }>(BASE + '/report', {
      params: {
        career: value.career, status: this.requestStatus,
        type: this.showType ? value.type : this.requestType,
      },
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: result => {
        if (revision !== this.revision) return;
        this.loading.set(false);
        this.loaded.set(true);
        if (!result.success) { this.error.set('No se pudo cargar el informe.'); return; }
        this.rows.set(result.data.rows.map(row => ({
          ...row,
          photoUrl: row.personCode
            ? `https://fotos.fen.uchile.cl/${encodeURIComponent(row.personCode)}.jpg`
            : DEFAULT_PHOTO,
        })));
        const first = result.data.rows[0]?.firstRequest ?? '';
        const last = result.data.rows[0]?.lastRequest ?? '';
        this.range.set(first && last ? `${first} a ${last}` : '');
      },
      error: () => {
        if (revision !== this.revision) return;
        this.loading.set(false);
        this.loaded.set(true);
        this.rows.set([]);
        this.error.set('No se pudo cargar el informe.');
      },
    });
  }
}
