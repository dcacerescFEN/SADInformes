import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute } from '@angular/router';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { Search } from '../../shared/components/search';
import { Table, TableColumn } from '../../shared/components/table';

interface Option { value: string; text: string; }
interface Row {
  course: string;
  courseCode: string;
  courseName: string;
  type: string;
  credits: string;
  mention: string;
  requirements: string;
  teachers: string;
  schedule: string;
}

const BASE = '/api/reports/academic-enrollment/teaching-schedule-decreto';
const COLUMNS: readonly TableColumn[] = [
  { key: 'course', name: 'Curso', sortable: true },
  { key: 'courseCode', name: 'Cátedra', sortable: true },
  { key: 'courseName', name: 'Nombre', sortable: true, multiline: true },
  { key: 'type', name: 'Tipo', sortable: true },
  { key: 'credits', name: 'UD', sortable: true },
  { key: 'mention', name: 'Mención', sortable: true },
  { key: 'requirements', name: 'Requisitos', sortable: true, multiline: true },
  { key: 'teachers', name: 'Profesor(es)', sortable: true, multiline: true },
  { key: 'schedule', name: 'Clases', sortable: true, multiline: true },
];

@Component({
  selector: 'app-teaching-schedule-decreto',
  imports: [ReactiveFormsModule, NgSelectModule, Search, Table],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="container-fluid px-0 py-4">
      <div class="page-header">
        <div class="d-flex flex-wrap justify-content-between align-items-start gap-3">
          <div>
            <h1 class="page-title">
              <i class="bi bi-calendar3" aria-hidden="true"></i>
              {{ title() }}
            </h1>
            <p class="page-subtitle mb-0">
              Programación docente por decreto
              @if (periodLabel()) { · {{ periodLabel() }} }.
            </p>
          </div>
          <button type="button" class="btn btn-search" data-action="refresh"
            [disabled]="form.invalid || loadingFilters() || loading()" (click)="loadReport()">
            <i class="bi bi-arrow-clockwise me-1" aria-hidden="true"></i>
            {{ loading() ? 'Cargando…' : 'Actualizar' }}
          </button>
        </div>
      </div>

      <div class="form-section mb-3">
        <h2 class="section-title"><i class="bi bi-funnel" aria-hidden="true"></i>Filtros</h2>
        <form [formGroup]="form" (ngSubmit)="loadReport()">
          <div class="row g-3 align-items-end">
            <div class="col-lg-4 col-md-6">
              <div class="ng-select-container-custom">
                <label class="floating-label" for="decreto-period">Período</label>
                <ng-select id="decreto-period" formControlName="period" data-filter="period"
                  [items]="periods()" bindLabel="text" bindValue="value" [clearable]="false"
                  class="form-select ng-select-custom" />
              </div>
            </div>
          </div>
        </form>
      </div>

      @if (loadingFilters()) { <p role="status">Cargando períodos…</p> }
      @if (error()) { <div class="alert alert-warning" role="alert">{{ error() }}</div> }

      @if (!error()) {
        <div class="form-section">
          <h2 class="section-title"><i class="bi bi-table" aria-hidden="true"></i>Resultados</h2>
          <div class="d-flex justify-content-end mb-3">
            <div class="search-box"><app-search (search)="searchTerm.set($event)" /></div>
          </div>
          <app-table [loading]="loading() || !loaded()" aria-label="Programación docente por decreto"
            [data]="rows()" [columns]="columns" [searchTerm]="searchTerm()"
            [showSummary]="false" [showRowNumber]="false" />
          <p class="fw-semibold mt-3 mb-0">Total: {{ rows().length }}</p>
        </div>
      }
    </div>
  `,
  styles: '.search-box { width: 20rem; max-width: 100%; }',
})
export class TeachingScheduleDecreto {
  private readonly http = inject(HttpClient);
  private readonly destroyRef = inject(DestroyRef);
  private readonly route = inject(ActivatedRoute);
  private readonly slug = (this.route.snapshot.data['report'] as { slug?: string } | undefined)?.slug ?? '';
  readonly form = inject(FormBuilder).nonNullable.group({ period: '' });
  protected readonly columns = COLUMNS;
  protected readonly title = signal('Programación Docente');
  protected readonly periods = signal<Option[]>([]);
  protected readonly rows = signal<Row[]>([]);
  protected readonly searchTerm = signal('');
  protected readonly loadingFilters = signal(false);
  protected readonly loading = signal(false);
  protected readonly loaded = signal(false);
  protected readonly error = signal('');
  private revision = 0;

  constructor() {
    this.form.controls.period.valueChanges.pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.loadReport());
    this.loadFilters();
  }

  protected periodLabel(): string {
    return this.periods().find(option => option.value === this.form.controls.period.value)?.text ?? '';
  }

  protected loadFilters(): void {
    const revision = ++this.revision;
    this.loadingFilters.set(true);
    this.loading.set(false);
    this.error.set('');
    this.rows.set([]);
    this.loaded.set(false);
    this.http.get<{ success: boolean; data: { title: string; periods: Option[] } }>(BASE + '/filters', {
      params: { slug: this.slug },
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: result => {
        if (revision !== this.revision) return;
        this.loadingFilters.set(false);
        if (!result.success) { this.error.set('No se pudieron cargar los filtros.'); return; }
        this.title.set(result.data.title);
        this.periods.set(result.data.periods);
        this.form.controls.period.setValue(result.data.periods[0]?.value ?? '', { emitEvent: false });
        if (!this.form.controls.period.value) {
          this.error.set('No hay períodos disponibles para el informe.');
          return;
        }
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
    if (this.loadingFilters() || this.form.invalid) return;
    const period = this.form.controls.period.value;
    if (!period) return;
    const revision = ++this.revision;
    this.loading.set(true);
    this.rows.set([]);
    this.loaded.set(false);
    this.error.set('');
    this.searchTerm.set('');
    this.http.get<{ success: boolean; data: { title: string; rows: Row[]; total: number } }>(BASE + '/report', {
      params: { slug: this.slug, period },
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: result => {
        if (revision !== this.revision) return;
        this.loading.set(false);
        this.loaded.set(true);
        if (!result.success) { this.error.set('No se pudo cargar el informe.'); return; }
        this.title.set(result.data.title);
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
