import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpClient } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { NavigationService } from '../../core/navigation/navigation-service';
import { Search } from '../../shared/components/search';
import { Table, TableColumn } from '../../shared/components/table';

interface Option { value: string; text: string; }
interface Row {
  studentCode: string;
  name: string;
  decree: string;
  decreeDescription: string;
  average: string | number;
  promotion: string;
  entrySemester: string;
  ranking: string | number;
  total: string | number;
}
interface RowView extends Row { decreeText: string; place: string; }
interface Report {
  group: string;
  period: string;
  selection: string;
  sortBy: string;
  direction: string;
  rows: Row[];
}

const BASE = '/api/reports/regulation/ranking';
const GROUPS: readonly Option[] = [
  { value: 'ICMA', text: 'Ingeniería Comercial Mención Administración' },
  { value: 'ICME', text: 'Ingeniería Comercial Mención Economía' },
  { value: 'IICG', text: 'Ingeniería en Información y Control de Gestión' },
  { value: 'CA', text: 'Contador Auditor' },
  { value: 'LICADM', text: 'Licenciatura en Administración' },
  { value: 'LICECO', text: 'Licenciatura en Economía' },
  { value: 'LICINF', text: 'Licenciatura en Información' },
];
const SELECTIONS: readonly Option[] = [
  { value: 'PROMOCION', text: 'Ranking por Promoción' },
  { value: 'INGRESO', text: 'Ranking por Período de Ingreso' },
];
const SORTINGS: readonly Option[] = [
  { value: 'RANKING', text: 'Ordenar por Ranking' },
  { value: 'NOMBRE', text: 'Ordenar Por Nombre' },
];
const DIRECTIONS: readonly Option[] = [
  { value: 'ASC', text: 'Ascendente' },
  { value: 'DESC', text: 'Descendente' },
];
const MISSING_PERIOD = 'Debe seleccionar un período.';
const EMPTY_MESSAGE = 'No existen alumnos con los requisitos indicados';
const COLUMNS: readonly TableColumn[] = [
  { key: 'studentCode', name: 'Código', sortable: true },
  { key: 'name', name: 'Nombre', sortable: true },
  { key: 'decreeText', name: 'Decreto', sortable: true, multiline: true },
  { key: 'average', name: 'Promedio', sortable: true },
  { key: 'promotion', name: 'Promoción', sortable: true },
  { key: 'entrySemester', name: 'Semestre de Ingreso', sortable: true },
  { key: 'place', name: 'Lugar', sortable: false },
];

function decreeText(row: Row): string {
  const decree = (row.decree ?? '').trim();
  const description = (row.decreeDescription ?? '').trim();
  if (!description || description === decree) return decree;
  return decree ? `${decree}\n${description}` : description;
}

function place(row: Row): string {
  return `${row.ranking ?? ''} de ${row.total ?? ''}`;
}

@Component({
  selector: 'app-ranking',
  imports: [ReactiveFormsModule, NgSelectModule, Search, Table],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="container-fluid px-0 py-4">
      <div class="page-header">
        <div>
          <h1 class="page-title">
            <i class="bi bi-award" aria-hidden="true"></i>
            Ranking de Alumnos por Licenciatura/Egreso
          </h1>
          <p class="page-subtitle mb-0">Ranking de alumnos de licenciatura y egreso según el grupo,
            el período y el tipo de selección consultados.</p>
        </div>
      </div>

      <div class="form-section mb-3">
        <h2 class="section-title"><i class="bi bi-funnel" aria-hidden="true"></i>Filtros</h2>
        <form [formGroup]="form" (ngSubmit)="submit()">
          <div class="row g-3 align-items-end">
            <div class="col-lg-3 col-md-6">
              <div class="ng-select-container-custom">
                <label class="floating-label" for="ranking-group">Carrera</label>
                <ng-select id="ranking-group" formControlName="group" data-filter="group"
                  [items]="groups" bindLabel="text" bindValue="value" [clearable]="false"
                  class="form-select ng-select-custom" appendTo="body" />
              </div>
            </div>
            <div class="col-lg-3 col-md-6">
              <div class="ng-select-container-custom">
                <label class="floating-label" for="ranking-period">Período</label>
                <ng-select id="ranking-period" formControlName="period" data-filter="period"
                  [items]="periods()" bindLabel="text" bindValue="value" [clearable]="false"
                  class="form-select ng-select-custom" appendTo="body" />
              </div>
            </div>
            <div class="col-lg-3 col-md-6">
              <div class="ng-select-container-custom">
                <label class="floating-label" for="ranking-selection">Tipo de Selección</label>
                <ng-select id="ranking-selection" formControlName="selection" data-filter="selection"
                  [items]="selections" bindLabel="text" bindValue="value" [clearable]="false"
                  class="form-select ng-select-custom" appendTo="body" />
              </div>
            </div>
            <div class="col-lg-3 col-md-6">
              <div class="ng-select-container-custom">
                <label class="floating-label" for="ranking-sort">Ordenar Por</label>
                <ng-select id="ranking-sort" formControlName="sortBy" data-filter="sortBy"
                  [items]="sortings" bindLabel="text" bindValue="value" [clearable]="false"
                  class="form-select ng-select-custom" appendTo="body" />
              </div>
            </div>
            <div class="col-lg-3 col-md-6">
              <div class="ng-select-container-custom">
                <label class="floating-label" for="ranking-direction">Dirección</label>
                <ng-select id="ranking-direction" formControlName="direction" data-filter="direction"
                  [items]="directions" bindLabel="text" bindValue="value" [clearable]="false"
                  class="form-select ng-select-custom" appendTo="body" />
              </div>
            </div>
            <div class="col-auto">
              <button type="submit" class="btn btn-search" data-action="show"
                [disabled]="loadingFilters() || loading()">
                <i class="bi bi-search me-1" aria-hidden="true"></i>Mostrar
              </button>
            </div>
          </div>
        </form>
      </div>

      @if (loadingFilters()) { <p role="status">Cargando períodos…</p> }
      @if (error()) { <div class="alert alert-warning" role="alert">{{ error() }}</div> }

      @if (searched() && !error()) {
        <div class="form-section">
          <h2 class="section-title"><i class="bi bi-table" aria-hidden="true"></i>Resultados</h2>
          <div class="d-flex justify-content-end mb-3">
            <div class="search-box"><app-search (search)="searchTerm.set($event)" /></div>
          </div>
          <app-table [loading]="loading() || !loaded()" aria-label="Ranking de alumnos por licenciatura y egreso"
            [data]="rows()" [columns]="columns" [searchTerm]="searchTerm()"
            [showSummary]="false" [showRowNumber]="false" />
          @if (!loading() && loaded() && rows().length === 0) {
            <p class="fw-semibold mt-3 mb-0" data-empty="ranking">{{ emptyMessage }}</p>
          }
          <p class="fw-semibold mt-3 mb-0">Total: {{ rows().length }}</p>
        </div>
      }
    </div>
  `,
  styles: '.search-box { width: 20rem; max-width: 100%; }',
})
export class Ranking implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly destroyRef = inject(DestroyRef);
  private readonly navigation = inject(NavigationService);
  readonly form = inject(FormBuilder).nonNullable.group({
    group: 'ICMA', period: '', selection: 'PROMOCION', sortBy: 'RANKING', direction: 'ASC',
  });
  protected readonly groups = GROUPS;
  protected readonly selections = SELECTIONS;
  protected readonly sortings = SORTINGS;
  protected readonly directions = DIRECTIONS;
  protected readonly emptyMessage = EMPTY_MESSAGE;
  protected readonly columns = COLUMNS.map(column => column.key === 'studentCode'
    ? { ...column, link: (row: object) => this.navigation.studentProgressUrl((row as Row).studentCode) }
    : column);
  protected readonly periods = signal<Option[]>([]);
  protected readonly rows = signal<RowView[]>([]);
  protected readonly searchTerm = signal('');
  protected readonly loadingFilters = signal(false);
  protected readonly loading = signal(false);
  protected readonly loaded = signal(false);
  protected readonly searched = signal(false);
  protected readonly error = signal('');

  ngOnInit(): void {
    this.loadFilters();
  }

  protected submit(): void {
    if (this.loading()) return;
    const { group, period, selection, sortBy, direction } = this.form.getRawValue();
    this.error.set('');
    if (!period) { this.error.set(MISSING_PERIOD); return; }
    this.searched.set(true);
    this.searchTerm.set('');
    this.loadReport({ group, period, selection, sortBy, direction });
  }

  private loadFilters(): void {
    this.loadingFilters.set(true);
    this.error.set('');
    this.http.get<{ success: boolean; data: { periods: Option[] } }>(BASE + '/filters')
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: result => {
          this.loadingFilters.set(false);
          if (!result.success || !result.data) {
            this.error.set('No se pudieron cargar los períodos.');
            return;
          }
          this.periods.set(result.data.periods);
          const first = result.data.periods[0];
          if (first && !this.form.controls.period.value) {
            this.form.controls.period.setValue(first.value);
          }
        },
        error: () => {
          this.loadingFilters.set(false);
          this.error.set('No se pudieron cargar los períodos.');
        },
      });
  }

  private loadReport(params: Record<string, string>): void {
    this.loading.set(true);
    this.loaded.set(false);
    this.rows.set([]);
    this.http.get<{ success: boolean; data: Report }>(BASE + '/report', { params })
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: result => {
          this.loading.set(false);
          this.loaded.set(true);
          if (!result.success || !result.data) {
            this.error.set('No se pudo cargar el informe.');
            return;
          }
          this.rows.set(result.data.rows.map(row => ({
            ...row, decreeText: decreeText(row), place: place(row),
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
}
