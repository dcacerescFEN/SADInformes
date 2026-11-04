import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpClient } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { Table, TableColumn } from '../../shared/components/table';

interface Option { value: string; text: string; }
interface Filters { periods: Option[]; courseSections: Option[]; }
interface Sample {
  courseCode: string; section: string; period: string; enrolled: number | null;
  answered: number | null; percentage: number | null; missing: number | null;
}
interface Question {
  question: string; alternative1: number; alternative2: number; alternative3: number;
  alternative4: number; alternative5: number; total: number; average: number | null;
}
interface Category { id: number; description: string; questions: Question[]; }
interface Summary { sample: Sample | null; categories: Category[]; observations: string[]; }
interface ReportTab {
  id: string;
  label: string;
  columns: readonly TableColumn[];
  rows: readonly Record<string, string | number | null>[];
}
const BASE = '/api/reports/postgraduate/iss-statistics';
const SAMPLE_COLUMNS: readonly TableColumn[] = [
  { key: 'courseCode', name: 'Cátedra' },
  { key: 'section', name: 'Sección' },
  { key: 'period', name: 'Período' },
  { key: 'enrolled', name: 'Inscritos' },
  { key: 'answered', name: 'Contestadas' },
  { key: 'percentage', name: 'Porcentaje %' },
  { key: 'missing', name: 'Faltantes' },
];
const CATEGORY_COLUMNS: readonly TableColumn[] = [
  { key: 'question', name: 'Pregunta' },
  { key: 'alternative1', name: 'Muy mala' },
  { key: 'alternative2', name: 'Mala' },
  { key: 'alternative3', name: 'Regular' },
  { key: 'alternative4', name: 'Buena' },
  { key: 'alternative5', name: 'Excelente' },
  { key: 'average', name: 'Promedio' },
];
const OBSERVATION_COLUMNS: readonly TableColumn[] = [{ key: 'observation', name: 'Observación' }];

@Component({
  selector: 'app-iss-statistics',
  imports: [ReactiveFormsModule, NgSelectModule, Table],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="container-fluid px-0 py-4">
      <div class="page-header">
        <h1 class="page-title"><i class="bi bi-bar-chart-line" aria-hidden="true"></i>Estadísticas ISS</h1>
        <p class="page-subtitle mb-0">Muestra, categorías de preguntas y observaciones de la evaluación ISS de Postgrado.</p>
      </div>
      <div class="form-section mb-3">
        <h2 class="section-title"><i class="bi bi-funnel" aria-hidden="true"></i>Filtros</h2>
        <form [formGroup]="form">
          <div class="row g-3 align-items-end">
            <div class="col-md-4">
              <div class="ng-select-container-custom">
                <label class="floating-label" for="iss-period">Período ingreso</label>
                <ng-select id="iss-period" formControlName="period" data-filter="period"
                  [items]="options().periods" bindLabel="text" bindValue="value" [clearable]="false"
                  class="form-select ng-select-custom" />
              </div>
            </div>
            <div class="col-md-5">
              <div class="ng-select-container-custom">
                <label class="floating-label" for="iss-course-section">Cátedra / Sección</label>
                <ng-select id="iss-course-section" formControlName="courseSection" data-filter="courseSection"
                  [items]="options().courseSections" bindLabel="text" bindValue="value" [clearable]="false"
                  class="form-select ng-select-custom" />
              </div>
            </div>
            <div class="col-md-3">
              <button type="button" class="btn btn-search" data-action="show"
                [disabled]="!ready() || summaryLoading()" (click)="show()">
                <i class="bi bi-search me-1" aria-hidden="true"></i>
                {{ summaryLoading() ? 'Cargando…' : 'Mostrar' }}
              </button>
            </div>
          </div>
        </form>
      </div>
      @if (filtersLoading()) { <p role="status">Cargando filtros…</p> }
      @if (error()) { <div class="alert alert-warning" role="alert">{{ error() }}</div> }
      @if (!hasLoaded() && !summaryLoading() && !error()) {
        <p class="text-body-secondary">Seleccione período y cátedra/sección, luego presione Mostrar.</p>
      }
      @if (tabs().length > 0) {
        <div class="app-tabs mb-3" role="tablist" aria-label="Secciones de estadísticas ISS">
          @for (tab of tabs(); track tab.id) {
            <button type="button" class="app-tab" role="tab" [attr.data-tab]="tab.id"
              [class.active]="activeTab() === tab.id" [attr.aria-selected]="activeTab() === tab.id"
              (click)="activeTab.set(tab.id)">{{ tab.label }}</button>
          }
        </div>
        @if (currentTab(); as tab) {
          <div class="form-section" [attr.data-section]="tab.id">
            <h2 class="section-title"><i class="bi bi-table" aria-hidden="true"></i>{{ tab.label }}</h2>
            <app-table [attr.aria-label]="tab.label" [data]="tab.rows" [columns]="tab.columns"
              [showSummary]="false" [showRowNumber]="false" />
          </div>
        }
      }
      @if (hasLoaded() && !summaryLoading() && !error() && tabs().length === 0) {
        <div class="form-section"><p class="text-body-secondary" role="status">Sin Datos</p></div>
      }
    </div>
  `,
})
export class IssStatistics {
  private readonly http = inject(HttpClient);
  private readonly destroyRef = inject(DestroyRef);
  readonly form = inject(FormBuilder).nonNullable.group({
    period: ['', Validators.required],
    courseSection: ['', Validators.required],
  });
  protected readonly options = signal<Filters>({ periods: [], courseSections: [] });
  protected readonly filtersLoading = signal(false);
  protected readonly summaryLoading = signal(false);
  protected readonly hasLoaded = signal(false);
  protected readonly error = signal('');
  protected readonly summary = signal<Summary | null>(null);
  protected readonly activeTab = signal('sample');
  protected readonly tabs = computed<ReportTab[]>(() => {
    const report = this.summary();
    if (!report) return [];
    const tabs: ReportTab[] = [{
      id: 'sample',
      label: 'Muestra',
      columns: SAMPLE_COLUMNS,
      rows: report.sample ? [{
        courseCode: report.sample.courseCode,
        section: report.sample.section,
        period: report.sample.period,
        enrolled: report.sample.enrolled,
        answered: report.sample.answered,
        percentage: this.percentValue(report.sample.percentage),
        missing: report.sample.missing,
      }] : [],
    }];
    for (const category of report.categories) {
      tabs.push({
        id: `category-${category.id}`,
        label: category.description,
        columns: CATEGORY_COLUMNS,
        rows: category.questions.map(question => ({
          question: question.question,
          alternative1: this.count(question.alternative1, question.total),
          alternative2: this.count(question.alternative2, question.total),
          alternative3: this.count(question.alternative3, question.total),
          alternative4: this.count(question.alternative4, question.total),
          alternative5: this.count(question.alternative5, question.total),
          average: this.average(question.average),
        })),
      });
    }
    tabs.push({
      id: 'observations',
      label: 'Observaciones',
      columns: OBSERVATION_COLUMNS,
      rows: report.observations.map(observation => ({ observation })),
    });
    return tabs.filter(tab => tab.rows.length > 0);
  });
  protected readonly currentTab = computed(() =>
    this.tabs().find(tab => tab.id === this.activeTab()) ?? this.tabs()[0] ?? null);

  constructor() {
    this.form.controls.period.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => {
      this.clearReport();
      this.loadFilters(this.form.controls.period.value);
    });
    this.loadFilters();
  }

  protected ready(): boolean {
    return this.form.valid && !this.filtersLoading();
  }

  protected loadFilters(period?: string): void {
    this.filtersLoading.set(true);
    this.error.set('');
    const selected = period || this.form.controls.period.value;
    this.http.get<{ success: boolean; data: Filters }>(BASE + '/filters',
      { params: selected ? { period: selected } : {} })
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: result => {
          this.filtersLoading.set(false);
          if (!result.success) { this.error.set('No se pudieron cargar los filtros.'); return; }
          this.options.set(result.data);
          const current = this.form.getRawValue();
          const periodValue = result.data.periods.some(row => row.value === selected)
            ? selected : result.data.periods[0]?.value ?? '';
          const section = result.data.courseSections.some(row => row.value === current.courseSection)
            ? current.courseSection : result.data.courseSections[0]?.value ?? '';
          this.form.patchValue({ period: periodValue, courseSection: section }, { emitEvent: false });
        },
        error: () => { this.filtersLoading.set(false); this.error.set('No se pudieron cargar los filtros.'); },
      });
  }

  protected show(): void {
    if (!this.ready() || this.summaryLoading()) return;
    this.summaryLoading.set(true);
    this.hasLoaded.set(false);
    this.error.set('');
    this.http.get<{ success: boolean; data: Summary }>(BASE + '/summary',
      { params: { ...this.form.getRawValue() } })
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: result => {
          this.summaryLoading.set(false);
          this.hasLoaded.set(true);
          if (!result.success) { this.summary.set(null); this.error.set('No se pudo cargar el informe.'); return; }
          this.summary.set(result.data);
          this.activeTab.set('sample');
        },
        error: () => {
          this.summaryLoading.set(false);
          this.hasLoaded.set(true);
          this.summary.set(null);
          this.error.set('No se pudo cargar el informe.');
        },
      });
  }

  protected percent(value: number, total: number): string {
    return total > 0 ? ((value * 100) / total).toFixed(1) + ' %' : '0.0 %';
  }

  protected count(value: number, total: number): string {
    return `${value} / ${this.percent(value, total)}`;
  }

  protected percentValue(value: number | null): string {
    return value == null ? '' : value.toFixed(1) + ' %';
  }

  protected average(value: number | null): string {
    return value == null ? '' : value.toFixed(1);
  }

  private clearReport(): void {
    this.summary.set(null);
    this.hasLoaded.set(false);
    this.error.set('');
    this.activeTab.set('sample');
  }
}
