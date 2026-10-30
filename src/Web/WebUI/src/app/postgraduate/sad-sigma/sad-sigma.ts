import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { Subscription } from 'rxjs';
import { Search } from '../../shared/components/search';

interface Option { value: string; text: string; }
interface Filters { years: string[]; }
interface PageItem { label: string; page: number | null; }
interface Row {
  id: string; cod: string; description: string; area: string; period: string; courseCode: string;
  section: string; teacherName: string; personCode: string; aqpq: string; dedicatedHours: string;
  start: string; end: string; promotion: string; teacherType: string; courseName: string;
  courseType: string; program: string; modality: string; confirmed: string; scheduleType: string;
  totalStudents: string; postgraduateStudents: string; undergraduateStudents: string;
  teachingEvaluationAverage: string; existsInVista: string; scenario1Projected: string;
  scenario1Actual: string; scenario2Projected: string; scenario2Actual: string;
  scenario3Projected: string; scenario3Actual: string; existsInSigma: string;
  createdInSigma: string; confirmationDate: string; confirmationUser: string;
  courseObservations: string; observationUser: string;
}
const BASE = '/api/reports/postgraduate/sad-sigma';
const PAGE_SIZE = 10;

@Component({
  selector: 'app-sad-sigma',
  imports: [ReactiveFormsModule, NgSelectModule, Search],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="container-fluid px-0 py-4">
      <div class="page-header">
        <div class="d-flex flex-wrap justify-content-between align-items-start gap-3">
          <div>
            <h1 class="page-title"><i class="bi bi-arrow-left-right" aria-hidden="true"></i>Reporte Vista Sad Sigma</h1>
            <p class="page-subtitle mb-0">Cátedras, pagos y estado en Sigma por año.</p>
          </div>
          <button type="button" class="btn btn-create" data-action="export"
            [disabled]="rows().length === 0 || exporting() || !shownYear()" (click)="download()">
            <i class="bi bi-file-earmark-excel me-1" aria-hidden="true"></i>
            {{ exporting() ? 'Descargando…' : 'Descargar Excel' }}
          </button>
        </div>
      </div>
      <div class="page-header">
        <form [formGroup]="form" (ngSubmit)="show()">
          <div class="row g-3 align-items-end">
            <div class="col-xl-3 col-md-6">
              <div class="ng-select-container-custom">
                <label class="floating-label" for="sigma-year">Año</label>
                <ng-select labelForId="sigma-year" formControlName="year" data-filter="year"
                  [items]="options().year" bindLabel="text" bindValue="value" [clearable]="false"
                  class="form-select ng-select-custom" />
              </div>
            </div>
            <div class="col-auto">
              <button type="submit" class="btn btn-search" data-action="show"
                [disabled]="!ready() || filtersLoading() || loading() || form.invalid">
                <i class="bi bi-search me-1" aria-hidden="true"></i>Mostrar
              </button>
              @if (!ready() && error()) {
                <button type="button" class="btn btn-outline-secondary ms-2" (click)="loadFilters()">Reintentar filtros</button>
              }
            </div>
          </div>
        </form>
      </div>
      @if (filtersLoading()) { <p role="status">Cargando filtros…</p> }
      @if (loading()) { <p role="status">Cargando reporte…</p> }
      @if (error()) { <div class="alert alert-warning" role="alert">{{ error() }}</div> }
      @if (notice()) { <p class="text-body-secondary" role="status">{{ notice() }}</p> }
      @if (rows().length > 0) {
        <div class="form-section">
          <div class="d-flex justify-content-end mb-3"><div class="search-box"><app-search (search)="applySearch($event)" /></div></div>
          @if (filteredRows().length > 0) {
          <div class="table-responsive">
            <table class="table table-hover align-middle app-table" aria-label="Reporte SAD Sigma">
              <thead class="table-light">
                <tr>
                  <th>ID</th><th>COD</th><th>descripcion</th><th>area</th><th>periodo</th>
                  <th>cod_catedra</th><th>cod_seccion</th><th>Nombre_Profesor</th><th>Cod_Persona</th>
                  <th>AQPQ</th><th>HorasDedicadas</th><th>Inicio</th><th>Termino</th><th>Promocion</th>
                  <th>Tipo_Profesor</th><th>nom_catedra</th><th>TipoCatedra</th><th>Programa</th>
                  <th>Modalidad</th><th>Confirmado</th><th>Tipo_Horario</th><th>Alumnos_Totales</th>
                  <th>Alumnos_Postgrado</th><th>Alumnos_Pregrado</th><th>Promedio_Ev_Docente</th>
                  <th>Existe_En_Vista</th><th>Escenario1Proyectado</th><th>Escenario1Real</th>
                  <th>Escenario2Proyectado</th><th>Escenario2Real</th><th>Escenario3Proyectado</th>
                  <th>Escenario3Real</th><th>Existe_En_Sigma</th><th>Fecha_Confirmacion</th>
                  <th>Usuario_Confirmacion</th><th>Observaciones_Curso</th><th>Usuario_Observ</th>
                  <th>Creado_En_Sigma</th>
                </tr>
              </thead>
              <tbody>
                @for (row of pagedRows(); track row.id) {
                  <tr>
                    <td>{{ row.id }}</td><td>{{ row.cod }}</td><td>{{ row.description }}</td>
                    <td>{{ row.area }}</td><td>{{ row.period }}</td><td>{{ row.courseCode }}</td>
                    <td>{{ row.section }}</td><td>{{ row.teacherName }}</td><td>{{ row.personCode }}</td>
                    <td>{{ row.aqpq }}</td><td>{{ row.dedicatedHours }}</td><td>{{ row.start }}</td>
                    <td>{{ row.end }}</td><td>{{ row.promotion }}</td><td>{{ row.teacherType }}</td>
                    <td>{{ row.courseName }}</td><td>{{ row.courseType }}</td><td>{{ row.program }}</td>
                    <td>{{ row.modality }}</td><td>{{ row.confirmed }}</td><td>{{ row.scheduleType }}</td>
                    <td>{{ row.totalStudents }}</td><td>{{ row.postgraduateStudents }}</td>
                    <td>{{ row.undergraduateStudents }}</td><td>{{ row.teachingEvaluationAverage }}</td>
                    <td>{{ row.existsInVista }}</td><td>{{ row.scenario1Projected }}</td>
                    <td>{{ row.scenario1Actual }}</td><td>{{ row.scenario2Projected }}</td>
                    <td>{{ row.scenario2Actual }}</td><td>{{ row.scenario3Projected }}</td>
                    <td>{{ row.scenario3Actual }}</td><td>{{ row.existsInSigma }}</td>
                    <td>{{ row.confirmationDate }}</td><td>{{ row.confirmationUser }}</td>
                    <td>{{ row.courseObservations }}</td><td>{{ row.observationUser }}</td>
                    <td>{{ row.createdInSigma }}</td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
          <div class="d-flex flex-wrap justify-content-between align-items-center gap-2">
            <small class="text-body-secondary">Mostrando {{ firstItem() }}–{{ lastItem() }} de {{ filteredRows().length }}</small>
            @if (totalPages() > 1) {
              <nav aria-label="Paginación del reporte SAD Sigma">
                <ul class="pagination pagination-sm mb-0">
                  <li class="page-item" [class.disabled]="currentPage() === 1"><button type="button" class="page-link" (click)="setPage(currentPage() - 1)">Anterior</button></li>
                  @for (item of pageItems(); track $index) {
                    @if (item.page === null) {
                      <li class="page-item disabled"><span class="page-link" aria-hidden="true">{{ item.label }}</span></li>
                    } @else {
                      <li class="page-item" [class.active]="currentPage() === item.page"><button type="button" class="page-link" (click)="setPage(item.page)">{{ item.label }}</button></li>
                    }
                  }
                  <li class="page-item" [class.disabled]="currentPage() === totalPages()"><button type="button" class="page-link" (click)="setPage(currentPage() + 1)">Siguiente</button></li>
                </ul>
              </nav>
            }
          </div>
          } @else {
            <p class="text-body-secondary" role="status">No se encontraron registros.</p>
          }
          <p class="fw-semibold mt-3 mb-0">Total: {{ rows().length }}</p>
        </div>
      }
    </div>
  `,
  styles: `.search-box { width: 20rem; max-width: 100%; }`,
})
export class SadSigma {
  private readonly http = inject(HttpClient);
  private readonly destroyRef = inject(DestroyRef);
  readonly form = inject(FormBuilder).nonNullable.group({
    year: ['', Validators.required],
  });
  private readonly filters = signal<Filters>({ years: [] });
  protected readonly options = computed(() => {
    const option = (value: string): Option => ({ value, text: value });
    return { year: this.filters().years.map(option) };
  });
  protected readonly rows = signal<Row[]>([]);
  protected readonly searchTerm = signal('');
  protected readonly page = signal(1);
  protected readonly filteredRows = computed(() => {
    const term = this.searchTerm().trim().toLowerCase();
    if (!term) return this.rows();
    return this.rows().filter(row => [
      row.id, row.cod, row.description, row.area, row.period, row.courseCode, row.section,
      row.teacherName, row.personCode, row.aqpq, row.dedicatedHours, row.start, row.end,
      row.promotion, row.teacherType, row.courseName, row.courseType, row.program, row.modality,
      row.confirmed, row.scheduleType, row.totalStudents, row.postgraduateStudents,
      row.undergraduateStudents, row.teachingEvaluationAverage, row.existsInVista,
      row.scenario1Projected, row.scenario1Actual, row.scenario2Projected, row.scenario2Actual,
      row.scenario3Projected, row.scenario3Actual, row.existsInSigma, row.createdInSigma,
      row.confirmationDate, row.confirmationUser, row.courseObservations, row.observationUser,
    ].join(' ').toLowerCase().includes(term));
  });
  protected readonly totalPages = computed(() =>
    Math.max(1, Math.ceil(this.filteredRows().length / PAGE_SIZE)));
  protected readonly currentPage = computed(() => Math.min(this.page(), this.totalPages()));
  protected readonly pagedRows = computed(() => {
    const start = (this.currentPage() - 1) * PAGE_SIZE;
    return this.filteredRows().slice(start, start + PAGE_SIZE);
  });
  protected readonly pageItems = computed<PageItem[]>(() => {
    const total = this.totalPages();
    const current = this.currentPage();
    const pages = new Set<number>([1, total]);
    for (let page = Math.max(1, current - 2); page <= Math.min(total, current + 2); page++) {
      pages.add(page);
    }
    const sorted = [...pages].sort((left, right) => left - right);
    const items: PageItem[] = [];
    for (const page of sorted) {
      const previous = items.at(-1);
      if (previous?.page !== null && previous?.page !== undefined && page - previous.page > 1) {
        items.push({ label: '...', page: null });
      }
      items.push({ label: String(page), page });
    }
    return items;
  });
  protected readonly firstItem = computed(() => this.filteredRows().length === 0
    ? 0 : (this.currentPage() - 1) * PAGE_SIZE + 1);
  protected readonly lastItem = computed(() =>
    Math.min(this.currentPage() * PAGE_SIZE, this.filteredRows().length));
  protected readonly filtersLoading = signal(false);
  protected readonly loading = signal(false);
  protected readonly exporting = signal(false);
  protected readonly ready = signal(false);
  protected readonly error = signal('');
  protected readonly notice = signal('');
  protected readonly shownYear = signal('');
  private reportRevision = 0;
  private reportSubscription?: Subscription;
  private exportSubscription?: Subscription;

  constructor() {
    this.form.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => this.invalidate());
    this.destroyRef.onDestroy(() => this.invalidate());
    this.loadFilters();
  }

  protected loadFilters(): void {
    this.filtersLoading.set(true);
    this.ready.set(false);
    this.error.set('');
    this.http.get<{ success: boolean; data: Filters }>(BASE + '/filters')
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: result => {
          this.filtersLoading.set(false);
          if (!result.success) { this.error.set('No se pudieron cargar los filtros.'); return; }
          const current = this.form.getRawValue();
          this.filters.set(result.data);
          this.form.patchValue({
            year: result.data.years.includes(current.year) ? current.year : result.data.years[0] ?? '',
          }, { emitEvent: false });
          this.ready.set(!this.form.invalid);
        },
        error: () => {
          this.filtersLoading.set(false);
          this.error.set('No se pudieron cargar los filtros.');
        },
      });
  }

  protected show(): void {
    if (!this.ready() || this.filtersLoading() || this.loading() || this.form.invalid) return;
    this.invalidate();
    const revision = this.reportRevision;
    const year = this.form.getRawValue().year;
    this.loading.set(true);
    this.reportSubscription = this.http.get<{ success: boolean; data: { rows: Row[]; total: number } }>(
      BASE, { params: { year } }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: result => {
          if (revision !== this.reportRevision) return;
          this.loading.set(false);
          if (!result.success) { this.error.set('No se pudo cargar el reporte.'); return; }
          this.rows.set(result.data.rows);
          this.shownYear.set(year);
          if (result.data.rows.length === 0) this.notice.set('Sin Datos');
        },
        error: () => {
          if (revision !== this.reportRevision) return;
          this.loading.set(false);
          this.error.set('No se pudo cargar el reporte.');
        },
      });
  }

  protected applySearch(term: string): void {
    this.searchTerm.set(term);
    this.page.set(1);
  }

  protected setPage(page: number): void {
    this.page.set(Math.min(Math.max(page, 1), this.totalPages()));
  }

  protected download(): void {
    const year = this.shownYear();
    if (!year || this.rows().length === 0 || this.exporting()) return;
    const revision = this.reportRevision;
    this.exporting.set(true);
    this.error.set('');
    this.exportSubscription = this.http.get(BASE + '/download',
      { params: { year }, responseType: 'blob' })
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: blob => {
          if (revision !== this.reportRevision) return;
          this.exporting.set(false);
          if (blob.size === 0) { this.error.set('Sin Datos'); return; }
          const url = URL.createObjectURL(blob);
          const link = document.createElement('a');
          try {
            link.href = url;
            link.download = 'ReporteVistaSadSigma.xlsx';
            document.body.appendChild(link);
            link.click();
          } finally {
            link.remove();
            URL.revokeObjectURL(url);
          }
        },
        error: (error: HttpErrorResponse) => {
          if (revision !== this.reportRevision) return;
          this.exporting.set(false);
          this.error.set(error.status === 400 ? 'Sin Datos' : 'No se pudo descargar el reporte.');
        },
      });
  }

  private invalidate(): void {
    ++this.reportRevision;
    this.reportSubscription?.unsubscribe();
    this.exportSubscription?.unsubscribe();
    this.rows.set([]);
    this.searchTerm.set('');
    this.page.set(1);
    this.shownYear.set('');
    this.loading.set(false);
    this.exporting.set(false);
    this.error.set('');
    this.notice.set('');
  }
}
