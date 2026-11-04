import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DecimalPipe } from '@angular/common';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { NavigationService } from '../../core/navigation/navigation-service';
import { NotificationService } from '../../core/services/notification-service';
import { Search } from '../../shared/components/search';
import { Table, TableColumn } from '../../shared/components/table';

interface Option { value: string; text: string; }
interface Filters { studentTypes: Option[]; modalities: Option[]; years: Option[]; paymentStatuses: Option[]; }
interface Row {
  position: number; photoUrl: string; name: string; rut: string; studentCode: string; entry: string;
  situation: string; average: number | null; paid: boolean; selected: boolean; originalPaid: boolean;
}
interface Report { amount: number | null; rows: Omit<Row, 'selected' | 'originalPaid'>[]; total: number; canManage: boolean; }
const BASE = '/api/reports/postgraduate/basic-right-payment';
const COLUMNS: readonly TableColumn[] = [
  { key: 'position', name: 'Posición', sortable: true },
  { key: 'photoUrl', name: 'Foto' },
  { key: 'name', name: 'Nombre', sortable: true },
  { key: 'rut', name: 'Rut', sortable: true },
  { key: 'studentCode', name: 'Código Alumno', sortable: true },
  { key: 'entry', name: 'Semestre de Ingreso', sortable: true },
  { key: 'situation', name: 'Situación', sortable: true },
  { key: 'average', name: 'Promedio', sortable: true },
];

@Component({
  selector: 'app-basic-right-payment',
  imports: [DecimalPipe, ReactiveFormsModule, NgSelectModule, Search, Table],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="container-fluid px-0 py-4">
      <div class="page-header">
        <div class="d-flex flex-wrap justify-content-between align-items-start gap-3">
          <div>
            <h1 class="page-title"><i class="bi bi-cash-coin" aria-hidden="true"></i>Pago Derecho Básico Anual</h1>
            <p class="page-subtitle mb-0">Alumnos, promedio y registro anual del pago de derecho básico.</p>
          </div>
          <div class="d-flex flex-wrap gap-2">
            <button type="button" class="btn btn-export-excel" data-action="export"
              [disabled]="rows().length === 0 || exporting()" (click)="download()">
              <i class="bi bi-file-earmark-excel me-1" aria-hidden="true"></i>Exportar
            </button>
            @if (canManage()) {
              <button type="button" class="btn btn-create" data-action="save"
                [disabled]="dirtyRows().length === 0 || amount() === null || saving()" (click)="save()">
                <i class="bi bi-save me-1" aria-hidden="true"></i>
                {{ saving() ? 'Guardando…' : 'Guardar' }}
              </button>
            }
          </div>
        </div>
      </div>
      <div class="form-section mb-3">
        <h2 class="section-title"><i class="bi bi-funnel" aria-hidden="true"></i>Filtros</h2>
        <form [formGroup]="form">
          <div class="row g-3 align-items-end">
            <div class="col-xl-4 col-lg-4 col-md-6">
              <div class="ng-select-container-custom">
                <label class="floating-label" for="basic-right-type">Tipo Alumno</label>
                <ng-select id="basic-right-type" formControlName="studentType" data-filter="studentType"
                  [items]="options().studentType" bindLabel="text" bindValue="value" [clearable]="false"
                  class="form-select ng-select-custom" />
              </div>
            </div>
            <div class="col-xl-2 col-lg-4 col-md-6">
              <div class="ng-select-container-custom">
                <label class="floating-label" for="basic-right-modality">Modalidad</label>
                <ng-select id="basic-right-modality" formControlName="modality" data-filter="modality"
                  [items]="options().modality" bindLabel="text" bindValue="value" [clearable]="false"
                  class="form-select ng-select-custom" />
              </div>
            </div>
            <div class="col-xl-1 col-lg-4 col-md-6">
              <div class="ng-select-container-custom">
                <label class="floating-label" for="basic-right-entry">Año Ingreso</label>
                <ng-select id="basic-right-entry" formControlName="entryYear" data-filter="entryYear"
                  [items]="options().year" bindLabel="text" bindValue="value" [clearable]="false"
                  class="form-select ng-select-custom" />
              </div>
            </div>
            <div class="col-xl-1 col-lg-4 col-md-6">
              <div class="ng-select-container-custom">
                <label class="floating-label" for="basic-right-payment-year">Año del Pago</label>
                <ng-select id="basic-right-payment-year" formControlName="paymentYear" data-filter="paymentYear"
                  [items]="options().year" bindLabel="text" bindValue="value" [clearable]="false"
                  class="form-select ng-select-custom" />
              </div>
            </div>
            <div class="col-xl-2 col-lg-4 col-md-6">
              <div class="ng-select-container-custom">
                <label class="floating-label" for="basic-right-status">Situación Pago</label>
                <ng-select id="basic-right-status" formControlName="paymentStatus" data-filter="paymentStatus"
                  [items]="options().paymentStatus" bindLabel="text" bindValue="value" [clearable]="false"
                  class="form-select ng-select-custom" />
              </div>
            </div>
            <div class="col-xl-2 col-lg-4 col-md-6">
              <button type="button" class="btn btn-search" data-action="show"
                [disabled]="!ready() || loading()" (click)="loadReport()">
                <i class="bi bi-search me-1" aria-hidden="true"></i>Mostrar
              </button>
            </div>
          </div>
        </form>
      </div>
      @if (filtersLoading()) { <p role="status">Cargando filtros…</p> }
      @if (loading()) { <p role="status">Cargando pagos…</p> }
      @if (error()) { <div class="alert alert-warning" role="alert">{{ error() }}</div> }
      @if (notice()) { <p class="text-body-secondary" role="status">{{ notice() }}</p> }
      @if (loaded()) {
        <div class="form-section">
          <h2 class="section-title"><i class="bi bi-table" aria-hidden="true"></i>Resultados</h2>
          <div class="d-flex flex-wrap justify-content-between align-items-center gap-3 mb-3">
            <p class="fw-semibold mb-0">Monto {{ form.controls.paymentYear.value }}:
              {{ amount() === null ? 'Sin Monto' : '$ ' + (amount() | number:'1.0-0') }}</p>
            @if (rows().length > 0) {
              <label class="form-check mb-0">
                <input class="form-check-input" type="checkbox" data-action="toggle-all"
                  [checked]="allPaid()" (change)="toggleAll($any($event.target).checked)" />
                Marcar todos los pagos
              </label>
              <div class="search-box"><app-search (search)="searchTerm.set($event)" /></div>
            }
          </div>
          <app-table aria-label="Pago derecho básico anual" [data]="rows()" [columns]="columns"
            [searchTerm]="searchTerm()" [showSummary]="false" [showRowNumber]="false"
            [selectionEnabled]="true" selectionLabel="Pago" (selectionChange)="togglePayment($event)" />
          <p class="fw-semibold mt-3 mb-0">Total: {{ rows().length }}</p>
        </div>
      }
    </div>
  `,
  styles: '.search-box { width: 20rem; max-width: 100%; }',
})
export class BasicRightPayment {
  private readonly http = inject(HttpClient);
  private readonly destroyRef = inject(DestroyRef);
  private readonly notifications = inject(NotificationService);
  private readonly navigation = inject(NavigationService);
  readonly form = inject(FormBuilder).nonNullable.group({
    studentType: '', modality: '', entryYear: '', paymentYear: '', paymentStatus: 'Todos',
  });
  protected readonly columns = COLUMNS.map(column => column.key === 'studentCode'
    ? { ...column, link: (row: object) => this.studentLink(row) }
    : column);
  protected readonly filters = signal<Filters>({ studentTypes: [], modalities: [], years: [], paymentStatuses: [] });
  protected readonly options = computed(() => {
    const option = (value: string): Option => ({ value, text: value });
    return {
      studentType: this.filters().studentTypes,
      modality: this.filters().modalities,
      year: this.filters().years,
      paymentStatus: this.filters().paymentStatuses,
    };
  });
  protected readonly rows = signal<Row[]>([]);
  protected readonly amount = signal<number | null>(null);
  protected readonly canManage = signal(false);
  protected readonly filtersLoading = signal(false);
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly exporting = signal(false);
  protected readonly loaded = signal(false);
  protected readonly error = signal('');
  protected readonly notice = signal('');
  protected readonly searchTerm = signal('');
  protected readonly dirtyRows = computed(() => this.rows().filter(row => row.paid !== row.originalPaid));
  protected readonly allPaid = computed(() => this.rows().length > 0 && this.rows().every(row => row.paid));

  constructor() {
    this.form.controls.studentType.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => {
      this.invalidate();
      this.loadFilters();
    });
    for (const control of ['modality', 'entryYear', 'paymentYear', 'paymentStatus'] as const) {
      this.form.controls[control].valueChanges.pipe(takeUntilDestroyed()).subscribe(() => this.invalidate());
    }
    this.loadFilters();
  }

  private studentLink(row: object): string | null {
    const base = this.navigation.links().find(link => link.key === 'student')?.url;
    const code = (row as Row).studentCode?.trim();
    if (!base || !code) return null;
    const url = new URL(base);
    url.pathname = url.pathname.replace(/\/$/, '') + '/students/detail/' + encodeURIComponent(code);
    url.search = '';
    url.hash = '';
    return url.href;
  }

  protected ready(): boolean {
    return !this.filtersLoading() && Object.values(this.form.getRawValue()).every(Boolean);
  }

  protected loadFilters(): void {
    this.filtersLoading.set(true);
    this.error.set('');
    const current = this.form.getRawValue();
    this.http.get<{ success: boolean; data: Filters }>(BASE + '/filters',
      { params: current.studentType ? { studentType: current.studentType } : {} })
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: result => {
          this.filtersLoading.set(false);
          if (!result.success) { this.error.set('No se pudieron cargar los filtros.'); return; }
          this.filters.set(result.data);
          this.form.patchValue({
            studentType: this.pick(result.data.studentTypes, current.studentType),
            modality: this.pick(result.data.modalities, current.modality),
            entryYear: this.pick(result.data.years, current.entryYear),
            paymentYear: this.pick(result.data.years, current.paymentYear),
            paymentStatus: this.pick(result.data.paymentStatuses, current.paymentStatus),
          }, { emitEvent: false });
        },
        error: () => { this.filtersLoading.set(false); this.error.set('No se pudieron cargar los filtros.'); },
      });
  }

  protected loadReport(keepNotice = false): void {
    if (!this.ready() || this.loading()) return;
    this.loading.set(true);
    this.error.set('');
    if (!keepNotice) this.notice.set('');
    this.searchTerm.set('');
    this.http.get<{ success: boolean; data: Report }>(BASE + '/report', { params: this.form.getRawValue() })
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: result => {
          this.loading.set(false);
          this.loaded.set(true);
          if (!result.success) { this.rows.set([]); this.error.set('No se pudo cargar el informe.'); return; }
          this.amount.set(result.data.amount);
          this.canManage.set(result.data.canManage);
          this.rows.set(result.data.rows.map(row => ({
            ...row, paid: row.paid, selected: row.paid, originalPaid: row.paid,
          })));
        },
        error: () => {
          this.loading.set(false);
          this.loaded.set(true);
          this.rows.set([]);
          this.error.set('No se pudo cargar el informe.');
        },
      });
  }

  protected togglePayment(row: object): void {
    const target = row as Row;
    this.rows.update(rows => rows.map(item => item.studentCode === target.studentCode
      ? { ...item, paid: !item.paid, selected: !item.paid } : item));
  }

  protected toggleAll(checked: boolean): void {
    this.rows.update(rows => rows.map(row => ({ ...row, paid: checked, selected: checked })));
  }

  protected async save(): Promise<void> {
    if (!this.canManage() || this.dirtyRows().length === 0 || this.amount() === null || this.saving()) return;
    const result = await this.notifications.confirm('¿Guardar cambios de pago?',
      `Se actualizarán ${this.dirtyRows().length} registro(s) del año ${this.form.controls.paymentYear.value}.`,
      'Sí, guardar', 'Cancelar');
    if (!result.isConfirmed) return;
    this.saving.set(true);
    this.error.set('');
    this.http.put<{ success: boolean; data: { saved: number } }>(BASE + '/payments', {
      paymentYear: this.form.controls.paymentYear.value,
      rows: this.dirtyRows().map(row => ({ studentCode: row.studentCode, paid: row.paid })),
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: response => {
        this.saving.set(false);
        if (!response.success) { this.error.set('No se pudieron guardar los pagos.'); return; }
        this.notice.set(`Pagos guardados: ${response.data.saved}.`);
        this.loadReport(true);
      },
      error: () => { this.saving.set(false); this.error.set('No se pudieron guardar los pagos.'); },
    });
  }

  protected download(): void {
    if (this.rows().length === 0 || this.exporting()) return;
    this.exporting.set(true);
    this.error.set('');
    this.http.get(BASE + '/download', { params: this.form.getRawValue(), responseType: 'blob' })
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: blob => {
          this.exporting.set(false);
          if (blob.size === 0) { this.error.set('Sin Datos'); return; }
          const url = URL.createObjectURL(blob);
          const link = document.createElement('a');
          try {
            link.href = url; link.download = 'PagoDerechoBasicoAnual.xlsx';
            document.body.appendChild(link); link.click();
          } finally { link.remove(); URL.revokeObjectURL(url); }
        },
        error: (error: HttpErrorResponse) => {
          this.exporting.set(false);
          this.error.set(error.status === 400 ? 'Sin Datos' : 'No se pudo exportar el informe.');
        },
      });
  }

  private pick(options: Option[], value: string): string {
    return options.some(row => row.value === value) ? value : options[0]?.value ?? '';
  }

  private invalidate(): void {
    this.rows.set([]);
    this.amount.set(null);
    this.loaded.set(false);
    this.notice.set('');
    this.error.set('');
    this.searchTerm.set('');
  }
}
