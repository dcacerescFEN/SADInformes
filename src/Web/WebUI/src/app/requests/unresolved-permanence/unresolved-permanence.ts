import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpClient } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { Search } from '../../shared/components/search';
import { Table, TableColumn } from '../../shared/components/table';

interface Option { value: string; text: string; }
interface Filters { careers: Option[]; }
interface PreviousRequest { period: string; type: string; resolution: string; authority: string; approved: boolean; }
interface CurrentRequest { period: string; type: string; }
interface EliminationCause { text: string; severity: string; }
interface SourceRow {
  personCode: string; studentCode: string; name: string; situation: string;
  entryData: string; credit: string; performance: string; semesters: string;
  failedCore: string; enrolledUnits: string; previousRequests: PreviousRequest[];
  currentRequests: CurrentRequest[]; currentRequestsText: string;
  eliminationCauses: EliminationCause[];
}
interface Row extends SourceRow { photoUrl: string; observations: string; restrictions: string; }
interface Report { period: string; career: string; rows: SourceRow[]; }

const BASE = '/api/reports/requests/unresolved-permanence';
const DEFAULT_PHOTO = 'https://fotos.fen.uchile.cl/perfilNN.jpg';

@Component({
  selector: 'app-unresolved-permanence',
  imports: [ReactiveFormsModule, NgSelectModule, Search, Table],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="container-fluid px-0 py-4">
      <div class="page-header">
        <div>
          <h1 class="page-title">
            <i class="bi bi-person-exclamation" aria-hidden="true"></i>
            Alumnos con Solicitudes de Permanencia No Resueltas
          </h1>
          <p class="page-subtitle mb-0">Alumnos con solicitudes de permanencia pendientes por carrera
            @if (resolvedPeriod()) { · {{ resolvedPeriod() }} }.</p>
        </div>
      </div>

      <div class="form-section mb-3">
        <h2 class="section-title"><i class="bi bi-funnel" aria-hidden="true"></i>Filtros</h2>
        <form [formGroup]="form">
          <div class="row g-3">
            <div class="col-lg-4 col-md-6">
              <div class="ng-select-container-custom">
                <label class="floating-label" for="permanence-career">Carrera</label>
                <ng-select id="permanence-career" formControlName="career" data-filter="career"
                  [items]="careers()" bindLabel="text" bindValue="value" [clearable]="false"
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
          <app-table [loading]="loading() || !loaded()" aria-label="Permanencia no resuelta"
            [data]="rows()" [columns]="columns" [searchTerm]="searchTerm()"
            [showSummary]="false" [showRowNumber]="false" />
          <p class="fw-semibold mt-3 mb-0">Total: {{ rows().length }}</p>
        </div>
      }
    </div>
  `,
  styles: '.search-box { width: 20rem; max-width: 100%; }',
})
export class UnresolvedPermanence {
  private readonly http = inject(HttpClient);
  private readonly destroyRef = inject(DestroyRef);
  readonly form = inject(FormBuilder).nonNullable.group({ career: 'IC' });
  protected readonly careers = signal<Option[]>([]);
  protected readonly rows = signal<Row[]>([]);
  protected readonly searchTerm = signal('');
  protected readonly loadingFilters = signal(false);
  protected readonly loading = signal(false);
  protected readonly loaded = signal(false);
  protected readonly error = signal('');
  protected readonly resolvedPeriod = signal('');
  protected readonly columns: readonly TableColumn[] = [
    { key: 'name', name: 'Nombre', sortable: true, multiline: true,
      avatar: (row) => (row as Row).photoUrl },
    { key: 'entryData', name: 'Datos Ingreso', sortable: false, multiline: true },
    { key: 'credit', name: '% Crédito', sortable: false },
    { key: 'performance', name: 'Indicadores Rendimiento', sortable: false, multiline: true },
    { key: 'semesters', name: 'Semestres', sortable: false, multiline: true },
    { key: 'failedCore', name: 'Cátedras Core Reprobadas', sortable: false, multiline: true },
    { key: 'enrolledUnits', name: 'UD Inscritas', sortable: false, multiline: true },
    { key: 'previousRequests', name: 'Solicitudes Permanencia Anteriores', sortable: false,
      icons: (row) => (row as Row).previousRequests.map(item => ({
        icon: item.approved ? 'bi-check-circle-fill' : 'bi-x-circle-fill',
        cssClass: item.approved ? 'table-icon-success' : 'table-icon-danger',
        label: `${item.period}: ${item.resolution} ${item.authority}\n${item.type}`,
      })) },
    { key: 'currentRequestsText', name: 'Solicitudes Permanencia Actuales', sortable: false, multiline: true },
    { key: 'eliminationCauses', name: 'Causales de Eliminación', sortable: false,
      icons: (row) => (row as Row).eliminationCauses.map(item => ({
        icon: 'bi-circle-fill',
        cssClass: item.severity === 'warning' ? 'table-icon-warning' : 'table-icon-danger',
        label: item.text,
      })) },
    { key: 'observations', name: 'Observaciones', sortable: false, multiline: true },
    { key: 'restrictions', name: 'Restricciones', sortable: false, multiline: true },
  ];
  private revision = 0;

  constructor() {
    this.form.controls.career.valueChanges.pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.loadReport());
    this.loadFilters();
  }

  protected loadFilters(): void {
    const revision = ++this.revision;
    this.loadingFilters.set(true);
    this.error.set('');
    this.http.get<{ success: boolean; data: Filters }>(BASE + '/filters')
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: result => {
          if (revision !== this.revision) return;
          this.loadingFilters.set(false);
          if (!result.success) { this.error.set('No se pudieron cargar los filtros.'); return; }
          this.careers.set(result.data.careers);
          this.form.patchValue({
            career: result.data.careers[0]?.value ?? 'IC',
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

    const revision = ++this.revision;
    this.loading.set(true);
    this.loaded.set(false);
    this.rows.set([]);
    this.error.set('');
    this.searchTerm.set('');
    this.http.get<{ success: boolean; data: Report }>(BASE + '/report', {
      params: { career: value.career },
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: result => {
        if (revision !== this.revision) return;
        this.loading.set(false);
        this.loaded.set(true);
        if (!result.success) { this.error.set('No se pudo cargar el informe.'); return; }
        this.resolvedPeriod.set(result.data.period);
        this.rows.set(result.data.rows.map(row => ({
          ...row,
          name: `${row.name}\n${row.situation}`,
          currentRequestsText: row.currentRequests.map(item => `${item.period}\n${item.type}`).join('\n'),
          observations: '', restrictions: '',
          photoUrl: row.personCode
            ? `https://fotos.fen.uchile.cl/${encodeURIComponent(row.personCode)}.jpg`
            : DEFAULT_PHOTO,
        })));
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

