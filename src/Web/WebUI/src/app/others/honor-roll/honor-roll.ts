import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpClient } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { NavigationService } from '../../core/navigation/navigation-service';
import { Search } from '../../shared/components/search';
import { Table, TableColumn } from '../../shared/components/table';

interface Option { value: string; text: string; }
interface Row {
  position: number;
  name: string;
  rut: string;
  studentCode: string;
  entrySemester: string;
  situation: string;
  average: string | number;
  percentile: string;
}
interface RowView extends Row { photoUrl: string; }
interface Filters { careers: Option[]; years: Option[]; }
interface Report { career: string; entryYear: string; honorYear: string; rows: Row[]; }

const BASE = '/api/reports/others/honor-roll';
const DEFAULT_PHOTO = 'https://fotos.fen.uchile.cl/perfilNN.jpg';
const MISSING_ENTRY_YEAR = 'Debe seleccionar el año de ingreso.';
const MISSING_HONOR_YEAR = 'Debe seleccionar el año del cuadro.';
const EMPTY_MESSAGE = 'Sin Datos';
const COLUMNS: readonly TableColumn[] = [
  { key: 'position', name: 'Posición', sortable: true },
  { key: 'photoUrl', name: 'Foto', sortable: false },
  { key: 'name', name: 'Nombre', sortable: true },
  { key: 'rut', name: 'Rut', sortable: true },
  { key: 'studentCode', name: 'Código Alumno', sortable: true },
  { key: 'entrySemester', name: 'Semestre de Ingreso', sortable: true },
  { key: 'situation', name: 'Situación', sortable: true },
  { key: 'average', name: 'Promedio', sortable: true },
  { key: 'percentile', name: 'Percentil', sortable: true },
];

function photoUrl(rut: string): string {
  const value = (rut ?? '').trim();
  return value ? `https://fotos.fen.uchile.cl/${encodeURIComponent(value)}.jpg` : DEFAULT_PHOTO;
}

@Component({
  selector: 'app-honor-roll',
  imports: [ReactiveFormsModule, NgSelectModule, Search, Table],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="container-fluid px-0 py-4">
      <div class="page-header">
        <div>
          <h1 class="page-title">
            <i class="bi bi-trophy" aria-hidden="true"></i>
            Cuadro de Honor
          </h1>
          <p class="page-subtitle mb-0">Alumnos destacados por carrera y año de ingreso, considerando
            las notas hasta el período consultado.</p>
        </div>
      </div>

      <div class="form-section mb-3">
        <h2 class="section-title"><i class="bi bi-funnel" aria-hidden="true"></i>Filtros</h2>
        <form [formGroup]="form" (ngSubmit)="submit()">
          <div class="row g-3 align-items-end">
            <div class="col-lg-3 col-md-6">
              <div class="ng-select-container-custom">
                <label class="floating-label" for="honor-roll-career">Carrera</label>
                <ng-select id="honor-roll-career" formControlName="career" data-filter="career"
                  [items]="careers()" bindLabel="text" bindValue="value" [clearable]="false"
                  class="form-select ng-select-custom" appendTo="body" />
              </div>
            </div>
            <div class="col-lg-3 col-md-6">
              <div class="ng-select-container-custom">
                <label class="floating-label" for="honor-roll-entry-year">Año de Ingreso</label>
                <ng-select id="honor-roll-entry-year" formControlName="entryYear" data-filter="entryYear"
                  [items]="years()" bindLabel="text" bindValue="value" [clearable]="false"
                  class="form-select ng-select-custom" appendTo="body" />
              </div>
            </div>
            <div class="col-lg-3 col-md-6">
              <div class="ng-select-container-custom">
                <label class="floating-label" for="honor-roll-honor-year">Año del Cuadro</label>
                <ng-select id="honor-roll-honor-year" formControlName="honorYear" data-filter="honorYear"
                  [items]="years()" bindLabel="text" bindValue="value" [clearable]="false"
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

      @if (loadingFilters()) { <p role="status">Cargando filtros…</p> }
      @if (error()) { <div class="alert alert-warning" role="alert">{{ error() }}</div> }

      @if (searched() && !error()) {
        <div class="form-section">
          @if (heading()) {
            <h2 class="section-title" data-heading="honor-roll">
              <i class="bi bi-award" aria-hidden="true"></i>{{ heading() }}
            </h2>
          }
          <div class="d-flex justify-content-end mb-3">
            <div class="search-box"><app-search (search)="searchTerm.set($event)" /></div>
          </div>
          <app-table [loading]="loading() || !loaded()" aria-label="Cuadro de Honor"
            [data]="rows()" [columns]="columns" [searchTerm]="searchTerm()"
            [showSummary]="false" [showRowNumber]="false" />
          @if (!loading() && loaded() && rows().length === 0) {
            <p class="fw-semibold mt-3 mb-0" data-empty="honor-roll">{{ emptyMessage }}</p>
          }
          <p class="fw-semibold mt-3 mb-0">Total: {{ rows().length }}</p>
        </div>
      }
    </div>
  `,
  styles: '.search-box { width: 20rem; max-width: 100%; }',
})
export class HonorRoll implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly destroyRef = inject(DestroyRef);
  private readonly navigation = inject(NavigationService);
  readonly form = inject(FormBuilder).nonNullable.group({
    career: '', entryYear: '', honorYear: '',
  });
  protected readonly emptyMessage = EMPTY_MESSAGE;
  protected readonly columns = COLUMNS.map(column => column.key === 'studentCode'
    ? { ...column, link: (row: object) => this.navigation.studentGradesUrl((row as Row).studentCode) }
    : column);
  protected readonly careers = signal<Option[]>([]);
  protected readonly years = signal<Option[]>([]);
  protected readonly report = signal<Report | null>(null);
  protected readonly heading = computed(() => {
    const report = this.report();
    if (!report) return '';
    const career = this.careers().find(option => option.value === report.career)?.text ?? report.career;
    return `Cuadro de Honor ${career} Año de Ingreso ${report.entryYear} `
      + `considerando las notas hasta el período ${report.honorYear}`;
  });
  protected readonly rows = computed<RowView[]>(() =>
    (this.report()?.rows ?? []).map(row => ({ ...row, photoUrl: photoUrl(row.rut) })));
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
    if (this.loading() || this.loadingFilters()) return;
    const { career, entryYear, honorYear } = this.form.getRawValue();
    this.error.set('');
    if (!entryYear) { this.error.set(MISSING_ENTRY_YEAR); return; }
    if (!honorYear) { this.error.set(MISSING_HONOR_YEAR); return; }
    this.searched.set(true);
    this.searchTerm.set('');
    this.loadReport({ career, entryYear, honorYear });
  }

  private loadFilters(): void {
    this.loadingFilters.set(true);
    this.error.set('');
    this.http.get<{ success: boolean; data: Filters }>(BASE + '/filters')
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: result => {
          this.loadingFilters.set(false);
          if (!result.success || !result.data) {
            this.error.set('No se pudieron cargar los filtros.');
            return;
          }
          const career = result.data.careers[0]?.value ?? '';
          const year = result.data.years[0]?.value ?? '';
          this.careers.set(result.data.careers);
          this.years.set(result.data.years);
          this.form.setValue({ career, entryYear: year, honorYear: year });
        },
        error: () => {
          this.loadingFilters.set(false);
          this.error.set('No se pudieron cargar los filtros.');
        },
      });
  }

  private loadReport(params: Record<string, string>): void {
    this.loading.set(true);
    this.loaded.set(false);
    this.report.set(null);
    this.http.get<{ success: boolean; data: Report }>(BASE + '/report', { params })
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: result => {
          this.loading.set(false);
          this.loaded.set(true);
          if (!result.success || !result.data) {
            this.error.set('No se pudo cargar el informe.');
            return;
          }
          this.report.set(result.data);
        },
        error: () => {
          this.loading.set(false);
          this.loaded.set(true);
          this.report.set(null);
          this.error.set('No se pudo cargar el informe.');
        },
      });
  }
}
