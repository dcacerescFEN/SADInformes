import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { Search } from '../../shared/components/search';
import { Table, TableColumn } from '../../shared/components/table';

interface Option { value: string; text: string; }
interface Filters { periods: Option[]; studentTypes: Option[]; modalities: Option[]; courses: Option[]; }
interface Advances { advances: Option[]; }
interface Row {
  advance: string; title: string; createdAt: string; dueAt: string; teacher: string; course: string;
  student: string; group: string; deliveredAt: string; grade: string; evaluatedAt: string;
  evaluationObservation: string;
}
type FilterControl = 'period' | 'studentType' | 'modality' | 'course' | 'advance';
const BASE = '/api/reports/postgraduate/afe-report';
const FIELDS = [
  { control: 'period', label: 'Período' },
  { control: 'studentType', label: 'Tipo Alumno' },
  { control: 'modality', label: 'Modalidad' },
  { control: 'course', label: 'Cursos' },
  { control: 'advance', label: 'Número Avance' },
] as const;
const COLUMNS: readonly TableColumn[] = [
  { key: 'advance', name: 'Avance Afe', sortable: true },
  { key: 'title', name: 'Nombre Avance', sortable: true },
  { key: 'createdAt', name: 'Fecha Creación' },
  { key: 'dueAt', name: 'Fecha Entrega Programada' },
  { key: 'teacher', name: 'Nombre Profesor', sortable: true },
  { key: 'course', name: 'Catedra' },
  { key: 'student', name: 'Nombre Alumno', sortable: true },
  { key: 'group', name: 'Grupo' },
  { key: 'deliveredAt', name: 'Fecha Entrega Real' },
  { key: 'grade', name: 'Evaluación' },
  { key: 'evaluatedAt', name: 'Fecha Evaluación' },
  { key: 'evaluationObservation', name: 'Obs. Evaluación' },
];

@Component({
  selector: 'app-afe-report',
  imports: [ReactiveFormsModule, NgSelectModule, Search, Table],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="container-fluid px-0 py-4">
      <div class="page-header">
        <div class="d-flex flex-wrap justify-content-between align-items-start gap-3">
          <div>
            <h1 class="page-title"><i class="bi bi-clipboard-check" aria-hidden="true"></i>Reporte Avances AFE</h1>
            <p class="page-subtitle mb-0">Avances, entregas, evaluaciones y observaciones por curso AFE.</p>
          </div>
          <button type="button" class="btn btn-create" data-action="export"
            [disabled]="rows().length === 0 || exporting()" (click)="download()">
            <i class="bi bi-file-earmark-excel me-1" aria-hidden="true"></i>
            {{ exporting() ? 'Exportando…' : 'Exportar Excel' }}
          </button>
        </div>
      </div>
      <div class="form-section mb-3">
        <h2 class="section-title"><i class="bi bi-funnel" aria-hidden="true"></i>Filtros</h2>
        <form [formGroup]="form">
          <div class="row g-3 align-items-end">
            @for (field of fields; track field.control) {
              <div class="col-lg-4 col-md-6" [class.col-xl-4]="field.control === 'studentType'" [class.col-xl-2]="field.control !== 'studentType'">
                <div class="ng-select-container-custom">
                  <label class="floating-label" [for]="'afe-' + field.control">{{ field.label }}</label>
                  <ng-select [id]="'afe-' + field.control" [formControlName]="field.control"
                    [attr.data-filter]="field.control" [items]="options()[field.control]"
                    bindLabel="text" bindValue="value" [clearable]="false"
                    class="form-select ng-select-custom" />
                </div>
              </div>
            }
            <div class="col-12 d-flex justify-content-end">
              <button type="button" class="btn btn-search" data-action="show"
                [disabled]="!canSearch() || loading()" (click)="show()">
                <i class="bi bi-search me-1" aria-hidden="true"></i>
                {{ loading() ? 'Cargando…' : 'Buscar' }}
              </button>
            </div>
          </div>
        </form>
      </div>
      @if (filtersLoading()) { <p role="status">Cargando filtros…</p> }
      @if (advancesLoading()) { <p role="status">Cargando avances…</p> }
      @if (loading()) { <p role="status">Cargando reporte…</p> }
      @if (error()) { <div class="alert alert-warning" role="alert">{{ error() }}</div> }
      @if (notice()) { <p class="text-body-secondary" role="status">{{ notice() }}</p> }
      @if ((loaded() || loading()) && !error()) {
        <div class="form-section">
          <h2 class="section-title"><i class="bi bi-table" aria-hidden="true"></i>Resultados</h2>
          <div class="d-flex justify-content-end mb-3">
            <div class="search-box"><app-search (search)="searchTerm.set($event)" /></div>
          </div>
          <app-table [loading]="loading() || !loaded()" aria-label="Reporte de avances AFE" [data]="rows()" [columns]="columns"
            [searchTerm]="searchTerm()" [showSummary]="false" [showRowNumber]="false" />
          <p class="fw-semibold mt-3 mb-0">Total: {{ rows().length }}</p>
        </div>
      }
    </div>
  `,
  styles: '.search-box { width: 20rem; max-width: 100%; }',
})
export class AfeReport {
  private readonly http = inject(HttpClient);
  private readonly destroyRef = inject(DestroyRef);
  readonly form = inject(FormBuilder).nonNullable.group({
    period: ['', Validators.required],
    studentType: ['', Validators.required],
    modality: ['', Validators.required],
    course: ['', Validators.required],
    advance: ['', Validators.required],
  });
  protected readonly fields = FIELDS;
  protected readonly columns = COLUMNS;
  protected readonly filters = signal<Filters>({ periods: [], studentTypes: [], modalities: [], courses: [] });
  protected readonly advances = signal<Option[]>([]);
  protected readonly options = computed<Record<FilterControl, Option[]>>(() => ({
    period: this.filters().periods,
    studentType: this.filters().studentTypes,
    modality: this.filters().modalities,
    course: this.filters().courses,
    advance: this.advances(),
  }));
  protected readonly rows = signal<Row[]>([]);
  protected readonly searchTerm = signal('');
  protected readonly filtersLoading = signal(false);
  protected readonly advancesLoading = signal(false);
  protected readonly loading = signal(false);
  protected readonly loaded = signal(false);
  protected readonly exporting = signal(false);
  protected readonly error = signal('');
  protected readonly notice = signal('');

  constructor() {
    for (const control of ['period', 'studentType', 'modality'] as const) {
      this.form.controls[control].valueChanges.pipe(takeUntilDestroyed()).subscribe(() => {
        this.invalidate();
        this.loadFilters();
      });
    }
    this.form.controls.course.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => {
      this.invalidate();
      this.loadAdvances();
    });
    this.form.controls.advance.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => this.invalidate());
    this.loadFilters();
  }

  protected canSearch(): boolean {
    return this.form.valid && !this.filtersLoading() && !this.advancesLoading();
  }

  protected loadFilters(): void {
    this.filtersLoading.set(true);
    this.error.set('');
    const current = this.form.getRawValue();
    this.http.get<{ success: boolean; data: Filters }>(BASE + '/filters', {
      params: {
        ...(current.period ? { period: current.period } : {}),
        ...(current.studentType ? { studentType: current.studentType } : {}),
        ...(current.modality ? { modality: current.modality } : {}),
      },
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: result => {
        this.filtersLoading.set(false);
        if (!result.success) { this.error.set('No se pudieron cargar los filtros.'); return; }
        this.filters.set(result.data);
        const selected = this.pick(result.data.periods, current.period);
        const type = this.pick(result.data.studentTypes, current.studentType);
        const modality = this.pick(result.data.modalities, current.modality);
        const course = this.pick(result.data.courses, current.course);
        this.form.patchValue({ period: selected, studentType: type, modality, course }, { emitEvent: false });
        if (course) this.loadAdvances(); else { this.advances.set([]); this.form.patchValue({ advance: '' }, { emitEvent: false }); }
      },
      error: () => { this.filtersLoading.set(false); this.error.set('No se pudieron cargar los filtros.'); },
    });
  }

  protected loadAdvances(): void {
    const period = this.form.controls.period.value;
    const course = this.form.controls.course.value;
    if (!period || !course) { this.advances.set([]); this.form.patchValue({ advance: '' }, { emitEvent: false }); return; }
    this.advancesLoading.set(true);
    this.error.set('');
    this.http.get<{ success: boolean; data: Advances }>(BASE + '/advances',
      { params: { period, course } }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: result => {
          this.advancesLoading.set(false);
          if (!result.success) { this.error.set('No se pudieron cargar los avances.'); return; }
          this.advances.set(result.data.advances);
          const selected = result.data.advances.some(row => row.value === this.form.controls.advance.value)
            ? this.form.controls.advance.value : result.data.advances[0]?.value ?? '';
          this.form.patchValue({ advance: selected }, { emitEvent: false });
          this.notice.set(result.data.advances.length === 0 ? 'No se han encontrado avances' : '');
        },
        error: () => { this.advancesLoading.set(false); this.error.set('No se pudieron cargar los avances.'); },
      });
  }

  protected show(): void {
    if (!this.canSearch() || this.loading()) return;
    this.loading.set(true);
    this.error.set('');
    this.notice.set('');
    this.searchTerm.set('');
    this.http.get<{ success: boolean; data: { rows: Row[]; total: number } }>(BASE + '/report',
      { params: { ...this.form.getRawValue() } }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: result => {
          this.loading.set(false);
          this.loaded.set(true);
          if (!result.success) { this.rows.set([]); this.error.set('No se pudo cargar el reporte.'); return; }
          this.rows.set(result.data.rows);
        },
        error: () => { this.loading.set(false); this.loaded.set(true); this.rows.set([]); this.error.set('No se pudo cargar el reporte.'); },
      });
  }

  protected download(): void {
    if (this.rows().length === 0 || this.exporting()) return;
    this.exporting.set(true);
    this.error.set('');
    this.http.get(BASE + '/download', { params: { ...this.form.getRawValue() }, responseType: 'blob' })
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: blob => {
          this.exporting.set(false);
          if (blob.size === 0) { this.error.set('Sin Datos'); return; }
          const url = URL.createObjectURL(blob);
          const link = document.createElement('a');
          try {
            link.href = url;
            link.download = 'Reporte nivel curso AFE - coordinacion.xlsx';
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
    this.notice.set('');
    this.error.set('');
    this.searchTerm.set('');
  }
}
