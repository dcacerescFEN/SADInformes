import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpClient } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { NavigationService } from '../../core/navigation/navigation-service';
import { Search } from '../../shared/components/search';
import { Table, TableColumn } from '../../shared/components/table';

interface Option { value: string; text: string; }
interface Filters { periods: string[]; careers: Option[]; types: Option[]; states: Option[]; }
interface Row {
  name: string; studentCode: string; requestCode: string; date: string; type: string;
  requested: string; administrativeProcessing: string; result: string; processDate: string;
}
interface Report { period: string; career: string; type: string; status: string; rows: Row[]; }

const BASE = '/api/reports/requests/automatic-processing';
const COLUMNS: readonly TableColumn[] = [
  { key: 'name', name: 'Nombre', sortable: true },
  { key: 'studentCode', name: 'Código', sortable: true },
  { key: 'requestCode', name: 'N° Solicitud', sortable: true },
  { key: 'date', name: 'Fecha', sortable: true },
  { key: 'type', name: 'Tipo', sortable: true, multiline: true },
  { key: 'requested', name: 'Solicitado', sortable: true, multiline: true },
  { key: 'administrativeProcessing', name: 'Tram. Admin.', sortable: true },
  { key: 'result', name: 'Resultado', sortable: true, multiline: true },
  { key: 'processDate', name: 'Fecha Proceso', sortable: true },
];

@Component({
  selector: 'app-automatic-processing-requests',
  imports: [ReactiveFormsModule, NgSelectModule, Search, Table],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="container-fluid px-0 py-4">
      <div class="page-header">
        <div class="d-flex flex-wrap justify-content-between align-items-start gap-3">
          <div>
            <h1 class="page-title">
              <i class="bi bi-inbox" aria-hidden="true"></i>
              Solicitudes procesadas automáticamente
            </h1>
            <p class="page-subtitle mb-0">Resultado del procesamiento automático por carrera y período.</p>
          </div>
        </div>
      </div>

      <div class="form-section mb-3">
        <h2 class="section-title"><i class="bi bi-funnel" aria-hidden="true"></i>Filtros</h2>
        <form [formGroup]="form">
          <div class="row g-3">
            <div class="col-lg-3 col-md-6">
              <div class="ng-select-container-custom">
                <label class="floating-label" for="automatic-period">Período</label>
                <ng-select id="automatic-period" formControlName="period" data-filter="period"
                  [items]="periods()" bindLabel="text" bindValue="value" [clearable]="false"
                  [searchable]="true" appendTo="body" class="form-select ng-select-custom" />
              </div>
            </div>
            <div class="col-lg-3 col-md-6">
              <div class="ng-select-container-custom">
                <label class="floating-label" for="automatic-career">Carrera</label>
                <ng-select id="automatic-career" formControlName="career" data-filter="career"
                  [items]="careers()" bindLabel="text" bindValue="value" [clearable]="false"
                  [searchable]="true" appendTo="body" class="form-select ng-select-custom" />
              </div>
            </div>
            <div class="col-lg-3 col-md-6">
              <div class="ng-select-container-custom">
                <label class="floating-label" for="automatic-type">Tipo</label>
                <ng-select id="automatic-type" formControlName="type" data-filter="type"
                  [items]="types()" bindLabel="text" bindValue="value" [clearable]="false"
                  [searchable]="true" appendTo="body" class="form-select ng-select-custom" />
              </div>
            </div>
            <div class="col-lg-3 col-md-6">
              <div class="ng-select-container-custom">
                <label class="floating-label" for="automatic-status">Resultado</label>
                <ng-select id="automatic-status" formControlName="status" data-filter="status"
                  [items]="states()" bindLabel="text" bindValue="value" [clearable]="false"
                  [searchable]="true" appendTo="body" class="form-select ng-select-custom" />
              </div>
            </div>
          </div>
        </form>
      </div>

      @if (loadingFilters()) { <p role="status">Cargando filtros…</p> }
      @if (error()) { <div class="alert alert-warning" role="alert">{{ error() }}</div> }

      @if (!error()) {
        <div class="form-section">
          <h2 class="section-title"><i class="bi bi-table" aria-hidden="true"></i>Resultados</h2>
          <div class="d-flex justify-content-end mb-3">
            <div class="search-box"><app-search (search)="searchTerm.set($event)" /></div>
          </div>
          <app-table [loading]="loading() || !loaded()" aria-label="Solicitudes procesadas automáticamente"
            [data]="rows()" [columns]="columns" [searchTerm]="searchTerm()"
            [showSummary]="false" [showRowNumber]="false" />
          <p class="fw-semibold mt-3 mb-0">Total: {{ rows().length }}</p>
        </div>
      }
    </div>
  `,
  styles: '.search-box { width: 20rem; max-width: 100%; }',
})
export class AutomaticProcessingRequests {
  private readonly http = inject(HttpClient);
  private readonly destroyRef = inject(DestroyRef);
  readonly form = inject(FormBuilder).nonNullable.group({
    period: '', career: '', type: '', status: '',
  });
  private readonly navigation = inject(NavigationService);
  protected readonly columns = COLUMNS.map(column => column.key === 'studentCode'
    ? { ...column, link: (row: object) => this.navigation.studentRequestsUrl((row as Row).studentCode) }
    : column);
  protected readonly periods = signal<Option[]>([]);
  protected readonly careers = signal<Option[]>([]);
  protected readonly types = signal<Option[]>([]);
  protected readonly states = signal<Option[]>([]);
  protected readonly rows = signal<Row[]>([]);
  protected readonly searchTerm = signal('');
  protected readonly loadingFilters = signal(false);
  protected readonly loading = signal(false);
  protected readonly loaded = signal(false);
  protected readonly error = signal('');
  private revision = 0;

  constructor() {
    this.form.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.loadReport());
    this.loadFilters();
  }

  protected loadFilters(): void {
    const revision = ++this.revision;
    this.loadingFilters.set(true);
    this.loading.set(false);
    this.loaded.set(false);
    this.rows.set([]);
    this.error.set('');
    this.http.get<{ success: boolean; data: Filters }>(BASE + '/filters')
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: result => {
          if (revision !== this.revision) return;
          this.loadingFilters.set(false);
          if (!result.success) { this.error.set('No se pudieron cargar los filtros.'); return; }
          const periods = result.data.periods.map(value => ({ value, text: value }));
          this.periods.set(periods);
          this.careers.set(result.data.careers);
          this.types.set(result.data.types);
          this.states.set(result.data.states);
          if (periods.length === 0) { this.error.set('No hay períodos de solicitudes disponibles.'); return; }
          this.form.patchValue({
            period: periods[0].value,
            career: result.data.careers[0]?.value ?? '',
            type: result.data.types[0]?.value ?? '',
            status: result.data.states[0]?.value ?? '',
          }, { emitEvent: false });
          this.loadReport();
        },
        error: () => {
          if (revision !== this.revision) return;
          this.loadingFilters.set(false);
          this.error.set('No se pudieron cargar los filtros.');
        },
      });
  }

  protected loadReport(): void {
    if (this.loadingFilters()) return;
    const value = this.form.getRawValue();
    if (!value.period) return;
    const revision = ++this.revision;
    this.loading.set(true);
    this.loaded.set(false);
    this.rows.set([]);
    this.error.set('');
    this.searchTerm.set('');
    this.http.get<{ success: boolean; data: Report }>(BASE + '/report', {
      params: { period: value.period, career: value.career, type: value.type, status: value.status },
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: result => {
        if (revision !== this.revision) return;
        this.loading.set(false);
        this.loaded.set(true);
        if (!result.success) { this.error.set('No se pudo cargar el informe.'); return; }
        this.rows.set(result.data.rows);
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
