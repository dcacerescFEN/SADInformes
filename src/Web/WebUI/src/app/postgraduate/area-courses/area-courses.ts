import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { Subscription } from 'rxjs';
import { Search } from '../../shared/components/search';

interface Option { value: string; text: string; }
interface Filters { years: string[]; }
interface Selection { year1: string; year2: string; }
interface PageItem { label: string; page: number | null; }
interface Row {
  id: string; description: string; area: string; period: string; courseCode: string;
  section: string; credits: string; teacherName: string; personCode: string; teacherType: string;
  start: string; end: string; program: string; modality: string; promotion: string;
  courseName: string; courseType: string; scheduleType: string; aqpq: string;
  teachingCategory: string; confirmed: string; dedicatedHours: string; totalStudents: string;
  postgraduateStudents: string; undergraduateStudents: string; teachingEvaluationAverage: string;
  courseObservations: string; observationUser: string;
}
const BASE = '/api/reports/postgraduate/area-courses';
const PAGE_SIZE = 10;

@Component({
  selector: 'app-area-courses',
  imports: [ReactiveFormsModule, NgSelectModule, Search],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="container-fluid px-0 py-4">
      <div class="page-header">
        <div class="d-flex flex-wrap justify-content-between align-items-start gap-3">
          <div>
            <h1 class="page-title"><i class="bi bi-diagram-3" aria-hidden="true"></i>Reporte Cátedras Áreas</h1>
            <p class="page-subtitle mb-0">Cátedras y áreas para dos años.</p>
          </div>
          <button type="button" class="btn btn-create" data-action="export"
            [disabled]="rows().length === 0 || exporting() || !shownSelection()" (click)="download()">
            <i class="bi bi-file-earmark-excel me-1" aria-hidden="true"></i>
            {{ exporting() ? 'Descargando…' : 'Descargar Excel' }}
          </button>
        </div>
      </div>
      <div class="page-header">
        <form [formGroup]="form" (ngSubmit)="show()">
          <div class="row g-3 align-items-end">
            @for (field of fields; track field.control) {
              <div class="col-xl-3 col-md-6">
                <div class="ng-select-container-custom">
                  <label class="floating-label" [for]="'area-' + field.control">{{ field.label }}</label>
                  <ng-select [labelForId]="'area-' + field.control" [formControlName]="field.control"
                    [attr.data-filter]="field.control" [items]="options()[field.control]"
                    bindLabel="text" bindValue="value" [clearable]="false" class="form-select ng-select-custom" />
                </div>
              </div>
            }
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
            <table class="table table-hover align-middle app-table" aria-label="Reporte de áreas y cátedras">
              <thead class="table-light">
                <tr>
                  <th>ID</th><th>Area_Catedra</th><th>Area</th><th>Periodo</th><th>Cod_Catedra</th>
                  <th>Sección</th><th>Creditos</th><th>Nombre_Profesor</th><th>Cod_Persona</th>
                  <th>Tipo_Profesor</th><th>Inicio_Catedra</th><th>Termino_Catedra</th><th>Tipo_Alumno</th>
                  <th>Modalidad</th><th>Promocion</th><th>Nombre_Catedra</th><th>TipoCatedra</th>
                  <th>Tipo_Horario</th><th>AQPQ</th><th>Categorización_Docente</th><th>Confirmado</th>
                  <th>HorasDedicadas</th><th>Alumnos_Totales</th><th>Alumnos_Postgrado</th>
                  <th>Alumnos_Pregrado</th><th>Promedio_Ev_Docente</th><th>Observaciones Curso</th>
                  <th>Usuario_Observ</th>
                </tr>
              </thead>
              <tbody>
                @for (row of pagedRows(); track row.id) {
                  <tr>
                    <td>{{ row.id }}</td><td>{{ row.description }}</td><td>{{ row.area }}</td>
                    <td>{{ row.period }}</td><td>{{ row.courseCode }}</td><td>{{ row.section }}</td>
                    <td>{{ row.credits }}</td><td>{{ row.teacherName }}</td><td>{{ row.personCode }}</td>
                    <td>{{ row.teacherType }}</td><td>{{ row.start }}</td><td>{{ row.end }}</td>
                    <td>{{ row.program }}</td><td>{{ row.modality }}</td><td>{{ row.promotion }}</td>
                    <td>{{ row.courseName }}</td><td>{{ row.courseType }}</td><td>{{ row.scheduleType }}</td>
                    <td>{{ row.aqpq }}</td><td>{{ row.teachingCategory }}</td><td>{{ row.confirmed }}</td>
                    <td>{{ row.dedicatedHours }}</td><td>{{ row.totalStudents }}</td>
                    <td>{{ row.postgraduateStudents }}</td><td>{{ row.undergraduateStudents }}</td>
                    <td>{{ row.teachingEvaluationAverage }}</td><td>{{ row.courseObservations }}</td>
                    <td>{{ row.observationUser }}</td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
          <div class="d-flex flex-wrap justify-content-between align-items-center gap-2">
            <small class="text-body-secondary">Mostrando {{ firstItem() }}–{{ lastItem() }} de {{ filteredRows().length }}</small>
            @if (totalPages() > 1) {
              <nav aria-label="Paginación del reporte de áreas">
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
export class AreaCourses {
  private readonly http = inject(HttpClient);
  private readonly destroyRef = inject(DestroyRef);
  readonly form = inject(FormBuilder).nonNullable.group({
    year1: ['', Validators.required],
    year2: ['', Validators.required],
  });
  protected readonly fields = [
    { control: 'year1', label: 'Año' },
    { control: 'year2', label: 'Otro Año' },
  ] as const;
  private readonly filters = signal<Filters>({ years: [] });
  protected readonly options = computed(() => {
    const option = (value: string): Option => ({ value, text: value });
    return { year1: this.filters().years.map(option), year2: this.filters().years.map(option) };
  });
  protected readonly rows = signal<Row[]>([]);
  protected readonly searchTerm = signal('');
  protected readonly page = signal(1);
  protected readonly filteredRows = computed(() => {
    const term = this.searchTerm().trim().toLowerCase();
    if (!term) return this.rows();
    return this.rows().filter(row => [
      row.id, row.description, row.area, row.period, row.courseCode, row.section, row.credits,
      row.teacherName, row.personCode, row.teacherType, row.start, row.end, row.program,
      row.modality, row.promotion, row.courseName, row.courseType, row.scheduleType, row.aqpq,
      row.teachingCategory, row.confirmed, row.dedicatedHours, row.totalStudents,
      row.postgraduateStudents, row.undergraduateStudents, row.teachingEvaluationAverage,
      row.courseObservations, row.observationUser,
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
  protected readonly shownSelection = signal<Selection | null>(null);
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
            year1: result.data.years.includes(current.year1) ? current.year1 : result.data.years[0] ?? '',
            year2: result.data.years.includes(current.year2) ? current.year2 : result.data.years[0] ?? '',
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
    const selection = { ...this.form.getRawValue() };
    this.loading.set(true);
    this.reportSubscription = this.http.get<{ success: boolean; data: { rows: Row[]; total: number } }>(
      BASE, { params: selection }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: result => {
          if (revision !== this.reportRevision) return;
          this.loading.set(false);
          if (!result.success) { this.error.set('No se pudo cargar el reporte.'); return; }
          this.rows.set(result.data.rows);
          this.shownSelection.set(selection);
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
    const selection = this.shownSelection();
    if (!selection || this.rows().length === 0 || this.exporting()) return;
    const revision = this.reportRevision;
    this.exporting.set(true);
    this.error.set('');
    this.exportSubscription = this.http.get(BASE + '/download',
      { params: { ...selection }, responseType: 'blob' })
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: blob => {
          if (revision !== this.reportRevision) return;
          this.exporting.set(false);
          if (blob.size === 0) { this.error.set('Sin Datos'); return; }
          const url = URL.createObjectURL(blob);
          const link = document.createElement('a');
          try {
            link.href = url;
            link.download = 'AlumnosPregradoPostgrado.xlsx';
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
    this.shownSelection.set(null);
    this.loading.set(false);
    this.exporting.set(false);
    this.error.set('');
    this.notice.set('');
  }
}
