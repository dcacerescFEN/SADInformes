import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpClient } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { firstValueFrom } from 'rxjs';
import { Table, TableColumn } from '../../shared/components/table';

interface Option { value: string; text: string; }
interface Filters { periodTypes: Option[]; periods: Option[]; programs: Option[]; modalities: string[]; promotions: Option[]; }
interface Envelope<T> { success: boolean; message: string | null; data: T; }
interface Row { period: string; periodLabel: string; course: string; name: string; type: string; units: number | null; teachers: string; classes: string; }
type FilterKey = 'periodType' | 'program' | 'modality' | 'promotion' | 'period';
const BASE = '/api/reports/postgraduate/teaching-schedule';
const EMPTY_FILTERS: Filters = { periodTypes: [], periods: [], programs: [], modalities: [], promotions: [] };
const FIELDS: readonly { control: FilterKey; label: string; icon: string }[] = [
  { control: 'periodType', label: 'Tipo de período', icon: 'bi-calendar-range' },
  { control: 'program', label: 'Programa', icon: 'bi-mortarboard' },
  { control: 'modality', label: 'Modalidad', icon: 'bi-grid-3x3-gap-fill' },
  { control: 'promotion', label: 'Promoción', icon: 'bi-people-fill' },
  { control: 'period', label: 'Período', icon: 'bi-calendar3' },
];
const COLUMNS: readonly TableColumn[] = [
  { key: 'periodLabel', name: 'PeriodoLargo', sortable: true },
  { key: 'course', name: 'Curso', sortable: true },
  { key: 'name', name: 'Cátedra', sortable: true },
  { key: 'type', name: 'Tipo', sortable: true },
  { key: 'units', name: 'Ud', sortable: true },
  { key: 'teachers', name: 'Profesores', sortable: true },
  { key: 'classes', name: 'Clases', sortable: true },
];

@Component({
  selector: 'app-teaching-schedule',
  imports: [ReactiveFormsModule, NgSelectModule, Table],
  template: `
    <div class="container-fluid px-0 py-4">
      <div class="page-header">
        <div class="d-flex flex-wrap justify-content-between align-items-start gap-3">
          <div>
            <h1 class="page-title">
              <i class="bi bi-calendar-week" aria-hidden="true"></i>
              Programación Docente Postgrado X Programa y Modalidad
            </h1>
            <p class="page-subtitle mb-0">Cursos, profesores y clases por tipo de período, programa, modalidad y promoción.</p>
          </div>
          <button class="btn btn-create" type="button" data-action="export" [disabled]="rows().length === 0" (click)="exportCsv()">
            <i class="bi bi-filetype-csv me-2" aria-hidden="true"></i>Exportar CSV
          </button>
        </div>
      </div>
      <div class="page-header">
        <form [formGroup]="form" (ngSubmit)="show()">
          <div class="row g-3 align-items-end">
            @for (field of fields; track field.control) {
              <div class="col-xl-2 col-lg-4 col-md-6">
                <div class="ng-select-container-custom">
                  <label class="floating-label" [for]="'schedule-' + field.control">
                    <i [class]="'bi ' + field.icon + ' me-1'" aria-hidden="true"></i>{{ field.label }}
                  </label>
                  <ng-select [labelForId]="'schedule-' + field.control" [formControlName]="field.control" [attr.data-filter]="field.control"
                    [items]="filterOptions()[field.control]" bindLabel="text" bindValue="value" [clearable]="false"
                    placeholder="Seleccione" class="form-select ng-select-custom" />
                </div>
              </div>
            }
            <div class="col-xl-auto col-lg-12">
              <button class="btn btn-search" type="submit" data-action="show" [disabled]="loading() || filtersLoading() || !filtersReady()">
                <i class="bi bi-search me-1" aria-hidden="true"></i>Mostrar
              </button>
            </div>
          </div>
        </form>
      </div>
      @if (error()) { <div class="alert alert-warning" role="alert">{{ error() }}</div> }
      @if (notice()) { <p class="text-body-secondary" role="status">{{ notice() }}</p> }
      @if (loading()) { <p role="status">Cargando…</p> }
      @if (rows().length > 0) {
        <div class="form-section">
          <app-table aria-label="Programación Docente Postgrado X Programa y Modalidad" [data]="rows()" [columns]="columns"
            [showSummary]="false" [showRowNumber]="false" />
          <p class="fw-semibold mt-3 mb-0">Total:{{ rows().length }}</p>
        </div>
      }
    </div>
  `,
  styles: ':host { display: block; }',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TeachingSchedule {
  private readonly http = inject(HttpClient);
  private readonly builder = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly fields = FIELDS;
  protected readonly columns = COLUMNS;
  readonly form = this.builder.nonNullable.group({
    periodType: 'PERÍODO', program: '', modality: { value: '', disabled: true },
    promotion: '', period: { value: '', disabled: true },
  });
  protected readonly filters = signal<Filters>(EMPTY_FILTERS);
  protected readonly filterOptions = computed<Record<FilterKey, Option[]>>(() => {
    const filters = this.filters();
    return { periodType: filters.periodTypes, program: filters.programs, promotion: filters.promotions,
      period: filters.periods, modality: filters.modalities.map(value => ({ value, text: value })) };
  });
  protected readonly rows = signal<Row[]>([]);
  protected readonly error = signal('');
  protected readonly notice = signal('');
  protected readonly loading = signal(false);
  protected readonly filtersLoading = signal(false);
  protected readonly filtersReady = signal(false);
  private filterRevision = 0;
  private reportRevision = 0;

  constructor() {
    this.destroyRef.onDestroy(() => { ++this.filterRevision; ++this.reportRevision; });
    this.form.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.discardReport());
    for (const control of [this.form.controls.periodType, this.form.controls.program])
      control.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => { void this.loadFilters(); });
    void this.loadFilters(true);
  }

  private discardReport(): void {
    ++this.reportRevision;
    this.rows.set([]);
    this.error.set('');
    this.notice.set('');
    this.loading.set(false);
  }

  private async loadFilters(applyDefaults = false): Promise<void> {
    const revision = ++this.filterRevision;
    const selected = this.form.getRawValue();
    this.discardReport();
    this.filtersLoading.set(true);
    this.filtersReady.set(false);
    this.form.controls.modality.disable({ emitEvent: false });
    this.form.controls.period.disable({ emitEvent: false });
    const params: Record<string, string> = { tipoPeriodo: selected.periodType };
    if (selected.program) params['tipoAlumno'] = selected.program;
    try {
      const response = await firstValueFrom(this.http.get<Envelope<Filters>>(BASE + '/filters', { params })
        .pipe(takeUntilDestroyed(this.destroyRef)));
      if (revision !== this.filterRevision) return;
      if (response.success !== true) throw new Error('Invalid response envelope');
      const filters = response.data;
      this.filters.set(filters);
      const period = filters.periods.some(option => option.value === selected.period) ? selected.period : filters.periods[0]?.value ?? '';
      this.form.controls.period.setValue(period, { emitEvent: false });
      const currentPromotion = this.form.controls.promotion.value;
      const promotion = filters.promotions.some(option => option.value === currentPromotion)
        ? currentPromotion : filters.promotions[0]?.value ?? '';
      this.form.controls.promotion.setValue(promotion, { emitEvent: false });
      if (applyDefaults && !selected.program && filters.programs.length > 0) {
        this.form.controls.program.setValue(filters.programs[0].value, { emitEvent: false });
        await this.loadFilters();
        return;
      }
      const modality = filters.modalities.includes(selected.modality) ? selected.modality : filters.modalities[0] ?? '';
      this.form.controls.modality.setValue(modality, { emitEvent: false });
      if (selected.program && filters.modalities.length > 0) this.form.controls.modality.enable({ emitEvent: false });
      if (filters.periods.length > 0) this.form.controls.period.enable({ emitEvent: false });
      this.filtersReady.set(true);
    } catch {
      if (revision !== this.filterRevision) return;
      this.filters.set(EMPTY_FILTERS);
      this.error.set('No se pudieron cargar los filtros del informe.');
    } finally {
      if (revision === this.filterRevision) this.filtersLoading.set(false);
    }
  }

  protected async show(): Promise<void> {
    if (this.loading()) return;
    this.discardReport();
    const selected = this.form.getRawValue();
    if (!this.filtersReady() || !selected.period || !selected.program || !selected.periodType) {
      this.error.set('Seleccione tipo de período, período y programa para consultar.');
      return;
    }
    const revision = this.reportRevision;
    this.loading.set(true);
    const params = { tipoPeriodo: selected.periodType, periodo: selected.period, tipoAlumno: selected.program,
      modalidad: selected.modality, promocion: selected.promotion };
    try {
      const response = await firstValueFrom(this.http.get<Envelope<{ rows: Row[]; total: number }>>(BASE, { params })
        .pipe(takeUntilDestroyed(this.destroyRef)));
      if (revision !== this.reportRevision) return;
      if (response.success !== true) throw new Error('Invalid response envelope');
      this.rows.set(response.data.rows);
      if (response.data.rows.length === 0) this.notice.set('No se encontraron cursos');
    } catch {
      if (revision !== this.reportRevision) return;
      this.rows.set([]);
      this.error.set('No se pudo cargar el informe.');
    } finally {
      if (revision === this.reportRevision) this.loading.set(false);
    }
  }

  protected exportCsv(): void {
    const rows = this.rows();
    if (rows.length === 0) return;
    const cell = (value: unknown) => {
      let text = value === null || value === undefined ? '' : String(value);
      if (/^\s*[=+\-@]|^[\t\r\n]/.test(text)) text = "'" + text;
      return /[";\r\n]/.test(text) ? '"' + text.replaceAll('"', '""') + '"' : text;
    };
    const lines = [COLUMNS.map(column => column.name).join(';'),
      ...rows.map(row => COLUMNS.map(column => cell(row[column.key as keyof Row])).join(';'))];
    const blob = new Blob(['\uFEFF' + lines.join('\r\n')], { type: 'text/csv;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = 'CursosProfesores.csv';
    link.click();
    URL.revokeObjectURL(url);
  }
}
