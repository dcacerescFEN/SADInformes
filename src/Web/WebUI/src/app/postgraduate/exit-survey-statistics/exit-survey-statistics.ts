import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpClient } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { Subscription, firstValueFrom } from 'rxjs';
import { LikertMatrix, LikertRow, LikertTotals } from './likert-matrix';
import { MultiScaleMatrix, ScaleData } from './multi-scale-matrix';
import { TeacherRanking, TeacherRankingData } from './teacher-ranking';
import { Insatisfaction, InsatisfactionRow, TeacherDetail } from './insatisfaction';
import { SummaryMatrix, SummaryRow } from './summary-matrix';
import { SurveyChart, ChartData, ChartRow } from './survey-chart';

interface Option { value: string; text: string; }
interface Filters { periods: Option[]; studentTypes: Option[]; modalities: Option[]; }
interface Row { type: string; modality: string | null; sample: number; inProgress: number; answered: number; }
interface Totals { sample: number; inProgress: number; answered: number; }
interface Tab { id: string; label: string; top: TopTabId; }
type TopTabId = 'programa' | 'cursos' | 'docentes' | 'conocimientos' | 'graficos';
interface TopTab { id: TopTabId; label: string; }
const TOP_TABS: readonly TopTab[] = [
  { id: 'programa', label: 'I. Evaluación Programa' },
  { id: 'cursos', label: 'II. Evaluación Cursos' },
  { id: 'docentes', label: 'III. Docentes' },
  { id: 'conocimientos', label: 'IV. Conocimientos y Habilidades Adquiridas' },
  { id: 'graficos', label: 'Gráficos' },
];
const BASE = '/api/reports/postgraduate/exit-survey-statistics';
const LIKERT_TABS = new Set(['malla', 'cuerpo-academico', 'funcionamiento', 'infraestructura', 'servicios', 'vinculacion-medio', 'networking', 'evaluacion-general']);
const RATING_TABS = new Set(['catedras-area', 'habilidades']);
const CATEDRA_LABELS = ['Muy Malo', 'Malo', 'Regular', 'Bueno', 'Muy Bueno'];
const SKILL_LABELS = ['Malo', 'Deficiente', 'Insuficiente', 'Suficiente', 'Bueno', 'Muy Bueno', 'Sobresaliente'];
const LIKERT_LABELS = ['Muy en desacuerdo', 'En desacuerdo', 'Ni de acuerdo ni en desacuerdo', 'De acuerdo', 'Muy de acuerdo'];
const CHART_OPTIONS = [
  { id: 'malla', label: 'Malla Curricular' }, { id: 'cuerpo-academico', label: 'Cuerpo Académico' },
  { id: 'funcionamiento', label: 'Funcionamiento' }, { id: 'infraestructura', label: 'Infraestructura y recursos' },
  { id: 'servicios', label: 'Servicios' }, { id: 'vinculacion-medio', label: 'Vinculación Medio' },
  { id: 'networking', label: 'Networking' }, { id: 'evaluacion-general', label: 'Evaluación general' },
] as const;
interface CatedraRow { code: string; description: string; alternative1: number; alternative2: number; alternative3: number; alternative4: number; alternative5: number; average: number | null; total: number; }
interface CatedraTotals { alternative1: number; alternative2: number; alternative3: number; alternative4: number; alternative5: number; }
interface SkillRow { question: string; description: string; alternative1: number; alternative2: number; alternative3: number; alternative4: number; alternative5: number; alternative6: number; alternative7: number; average: number | null; total: number; }
interface SkillTotals { alternative1: number; alternative2: number; alternative3: number; alternative4: number; alternative5: number; alternative6: number; alternative7: number; }
const TABS: readonly Tab[] = [
  { id: 'malla', label: 'Malla Curricular', top: 'programa' },
  { id: 'cuerpo-academico', label: 'Cuerpo Académico', top: 'programa' },
  { id: 'funcionamiento', label: 'Funcionamiento', top: 'programa' },
  { id: 'infraestructura', label: 'Infraestructura y recursos', top: 'programa' },
  { id: 'servicios', label: 'Servicios', top: 'programa' },
  { id: 'vinculacion-medio', label: 'Vinculación Medio', top: 'programa' },
  { id: 'networking', label: 'Networking', top: 'programa' },
  { id: 'resumen', label: 'Resumen', top: 'programa' },
  { id: 'catedras-area', label: 'Cátedras x Área', top: 'cursos' },
  { id: 'ranking', label: 'Ranking', top: 'docentes' },
  { id: 'insatisfaccion', label: 'Insatisfacción', top: 'docentes' },
  { id: 'habilidades', label: 'Conocimientos y habilidades', top: 'conocimientos' },
  { id: 'evaluacion-general', label: 'Evaluación general', top: 'conocimientos' },
];

@Component({
  selector: 'app-exit-survey-statistics',
  imports: [ReactiveFormsModule, NgSelectModule, LikertMatrix, MultiScaleMatrix, TeacherRanking, Insatisfaction, SummaryMatrix, SurveyChart],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="container-fluid px-0 py-4">
      <div class="page-header">
        <h1 class="page-title"><i class="bi bi-bar-chart-line" aria-hidden="true"></i>Estadísticas Encuesta de Salida</h1>
        <p class="page-subtitle mb-0">Resultados de la encuesta de graduados por período, tipo de alumno y modalidad.</p>
      </div>
      <div class="page-header">
        <form [formGroup]="form">
          <div class="row g-3 align-items-end">
            <div class="col-xl-3 col-md-4">
              <div class="ng-select-container-custom">
                <label class="floating-label" for="statisticsPeriod">Período ingreso</label>
                <ng-select inputId="statisticsPeriod" formControlName="period" [items]="filters().periods"
                  bindLabel="text" bindValue="value" [clearable]="false" class="form-select ng-select-custom" />
              </div>
            </div>
            <div class="col-xl-3 col-md-4">
              <div class="ng-select-container-custom">
                <label class="floating-label" for="statisticsType">Tipo Alumno</label>
                <ng-select inputId="statisticsType" formControlName="studentType" [items]="filters().studentTypes"
                  bindLabel="text" bindValue="value" [clearable]="false" class="form-select ng-select-custom" />
              </div>
            </div>
            <div class="col-xl-3 col-md-4">
              <div class="ng-select-container-custom">
                <label class="floating-label" for="statisticsModality">Modalidad</label>
                <ng-select inputId="statisticsModality" formControlName="modality" [items]="filters().modalities"
                  bindLabel="text" bindValue="value" [clearable]="false" class="form-select ng-select-custom" />
              </div>
            </div>
            <div class="col-xl-auto">
              <button type="button" class="btn btn-search" data-action="refresh" [disabled]="busy()" (click)="loadSample()">
                <i class="bi bi-arrow-clockwise me-1" aria-hidden="true"></i>Actualizar
              </button>
            </div>
          </div>
        </form>
      </div>
      @if (error()) { <div class="alert alert-warning" role="alert">{{ error() }}</div> }
      @if (loading()) { <p role="status">Cargando muestra…</p> }
      @if (rows().length > 0) {
        <div class="form-section">
          <h2 class="h6">Muestra</h2>
          <div class="table-responsive">
            <table class="table table-hover align-middle app-table" aria-label="Muestra encuesta de salida">
              <thead class="table-light"><tr><th>Tipo</th><th>Modalidad</th><th>Muestra</th><th>Incompletas</th><th>Contestadas</th></tr></thead>
              <tbody>
                @for (row of rows(); track row.type + '|' + (row.modality ?? '')) {
                  <tr><td>{{ row.type }}</td><td>{{ row.modality ?? '' }}</td><td>{{ row.sample }}</td><td>{{ row.inProgress }}</td><td>{{ row.answered }}</td></tr>
                }
              </tbody>
              <tfoot class="table-light fw-semibold"><tr><td colspan="2">Totales</td><td>{{ totals().sample }}</td><td>{{ totals().inProgress }}</td><td>{{ totals().answered }}</td></tr></tfoot>
            </table>
          </div>
        </div>
      } @else if (loaded() && !busy()) {
        <p class="text-body-secondary" role="status">Sin Datos</p>
      }
      <div class="app-tabs mb-3" role="tablist" aria-label="Etapas de la encuesta">
        @for (tab of topTabs; track tab.id) {
          <button type="button" class="app-tab" role="tab" [attr.data-top]="tab.id"
            [class.active]="activeTopTab() === tab.id" [attr.aria-selected]="activeTopTab() === tab.id"
            (click)="selectTop(tab.id)">{{ tab.label }}</button>
        }
      </div>
      <div class="form-section mt-3">
        @if (visibleTabs().length > 0) {
          <div class="app-tabs app-tabs--secondary mb-3" role="tablist" [attr.aria-label]="activeTopLabel()">
            @for (tab of visibleTabs(); track tab.id) {
              <button type="button" class="app-tab" role="tab" [attr.data-tab]="tab.id"
                [class.active]="activeTab() === tab.id" [attr.aria-selected]="activeTab() === tab.id"
                (click)="selectTab(tab.id)">{{ tab.label }}</button>
            }
          </div>
        }
        @if (isLikertTab()) {
          <div class="mt-3" role="tabpanel" data-panel>
            @if (likertLoading()) { <p role="status">Cargando sección…</p> }
            @if (likertError()) { <div class="alert alert-warning" role="alert">{{ likertError() }}</div> }
            @if (likertData(); as data) {
              <h3 class="h5">{{ data.title }}</h3>
              <app-likert-matrix [title]="data.title" [rows]="data.rows" [totals]="data.totals" />
            } @else if (!likertLoading() && !likertError()) {
              <p class="text-body-secondary" role="status">No existen datos disponibles.</p>
            }
          </div>
        } @else if (isRatingTab()) {
          <div class="mt-3" role="tabpanel" data-panel>
            @if (ratingLoading()) { <p role="status">Cargando sección…</p> }
            @if (ratingError()) { <div class="alert alert-warning" role="alert">{{ ratingError() }}</div> }
            @if (ratingData(); as data) {
              <h3 class="h5">{{ data.title }}</h3>
              <app-multi-scale-matrix [data]="data" />
            } @else if (!ratingLoading() && !ratingError()) {
              <p class="text-body-secondary" role="status">No existen datos disponibles.</p>
            }
          </div>
        } @else if (isTeachersTab()) {
          <div class="mt-3" role="tabpanel" data-panel>
            @if (teacherLoading()) { <p role="status">Cargando sección…</p> }
            @if (teacherError()) { <div class="alert alert-warning" role="alert">{{ teacherError() }}</div> }
            @if (teacherData(); as data) { <app-teacher-ranking [data]="data" /> }
            @else if (!teacherLoading() && !teacherError()) { <p class="text-body-secondary" role="status">No existen datos disponibles.</p> }
          </div>
        } @else if (isInsatisfactionTab()) {
          <div class="mt-3" role="tabpanel" data-panel>
            @if (insatisfactionLoading()) { <p role="status">Cargando sección…</p> }
            @if (insatisfactionError()) { <div class="alert alert-warning" role="alert">{{ insatisfactionError() }}</div> }
            @if (!insatisfactionLoading() && !insatisfactionError()) {
              <app-insatisfaction [rows]="insatisfactionRows()" [detail]="teacherDetail()"
                [detailLoading]="detailLoading()" [detailError]="detailError()" (showDetail)="loadDetail($event)" />
            }
          </div>
        } @else if (isSummaryTab()) {
          <div class="mt-3" role="tabpanel" data-panel>
            @if (summaryLoading()) { <p role="status">Cargando resumen…</p> }
            @if (summaryError()) { <div class="alert alert-warning" role="alert">{{ summaryError() }}</div> }
            @if (!summaryLoading() && !summaryError()) { <app-summary-matrix [rows]="summaryRows()" /> }
          </div>
        } @else if (isChartsTab()) {
          <div class="mt-3" role="tabpanel" data-panel>
            <div class="app-tabs app-tabs--secondary mb-3" role="tablist" aria-label="Gráficos de la encuesta">
              @for (chart of chartOptions; track chart.id) {
                <button type="button" class="app-tab" [attr.data-chart]="chart.id"
                  [class.active]="selectedChart() === chart.id" [attr.aria-selected]="selectedChart() === chart.id"
                  (click)="selectChart(chart.id)">{{ chart.label }}</button>
              }
            </div>
            @if (chartLoading()) { <p role="status">Cargando gráfico…</p> }
            @if (chartError()) { <div class="alert alert-warning" role="alert">{{ chartError() }}</div> }
            @if (chartData(); as data) { <h3 class="h5">{{ data.title }}</h3><app-survey-chart [data]="data" /> }
          </div>
        }
      </div>
    </div>
  `,
})
export class ExitSurveyStatistics {
  private readonly http = inject(HttpClient);
  private readonly destroyRef = inject(DestroyRef);
  readonly form = inject(FormBuilder).nonNullable.group({ period: '', studentType: '', modality: '' });
  protected readonly topTabs = TOP_TABS;
  readonly activeTopTab = signal<TopTabId>('programa');
  protected readonly visibleTabs = computed(() => TABS.filter(tab => tab.top === this.activeTopTab()));
  protected readonly activeTopLabel = computed(() => TOP_TABS.find(tab => tab.id === this.activeTopTab())?.label ?? '');
  protected readonly filters = signal<Filters>({ periods: [], studentTypes: [], modalities: [] });
  protected readonly rows = signal<Row[]>([]);
  protected readonly totals = signal<Totals>({ sample: 0, inProgress: 0, answered: 0 });
  readonly activeTab = signal('malla');
  protected readonly loading = signal(false);
  private readonly filtering = signal(false);
  protected readonly loaded = signal(false);
  protected readonly error = signal('');
  protected readonly busy = computed(() => this.loading() || this.filtering() || this.likertLoading() || this.ratingLoading() || this.teacherLoading() || this.insatisfactionLoading() || this.summaryLoading() || this.chartLoading());
  protected readonly likertData = signal<{ section: string; title: string; rows: LikertRow[]; totals: LikertTotals } | null>(null);
  protected readonly likertLoading = signal(false);
  protected readonly likertError = signal('');
  protected readonly ratingData = signal<ScaleData | null>(null);
  protected readonly ratingLoading = signal(false);
  protected readonly ratingError = signal('');
  protected readonly teacherData = signal<TeacherRankingData | null>(null);
  protected readonly teacherLoading = signal(false);
  protected readonly teacherError = signal('');
  protected readonly insatisfactionRows = signal<InsatisfactionRow[]>([]);
  protected readonly insatisfactionLoading = signal(false);
  protected readonly insatisfactionError = signal('');
  protected readonly teacherDetail = signal<TeacherDetail | null>(null);
  protected readonly detailLoading = signal(false);
  protected readonly detailError = signal('');
  protected readonly summaryRows = signal<SummaryRow[]>([]);
  protected readonly summaryLoading = signal(false);
  protected readonly summaryError = signal('');
  protected readonly isSummaryTab = computed(() => this.activeTab() === 'resumen');
  protected readonly isChartsTab = computed(() => this.activeTab() === 'graficos');
  protected readonly chartOptions = CHART_OPTIONS;
  protected readonly selectedChart = signal('malla');
  protected readonly chartData = signal<ChartData | null>(null);
  protected readonly chartLoading = signal(false);
  protected readonly chartError = signal('');
  protected readonly isLikertTab = computed(() => LIKERT_TABS.has(this.activeTab()));
  protected readonly isRatingTab = computed(() => RATING_TABS.has(this.activeTab()));
  protected readonly isTeachersTab = computed(() => this.activeTab() === 'ranking');
  protected readonly isInsatisfactionTab = computed(() => this.activeTab() === 'insatisfaccion');
  private filterRevision = 0;
  private sampleRevision = 0;
  private likertRevision = 0;
  private ratingRevision = 0;
  private teacherRevision = 0;
  private insatisfactionRevision = 0;
  private detailRevision = 0;
  private summaryRevision = 0;
  private chartRevision = 0;
  private sampleSubscription?: Subscription;
  private likertSubscription?: Subscription;
  private ratingSubscription?: Subscription;
  private teacherSubscription?: Subscription;
  private insatisfactionSubscription?: Subscription;
  private detailSubscription?: Subscription;
  private summarySubscription?: Subscription;
  private chartSubscription?: Subscription;

  constructor() {
    this.destroyRef.onDestroy(() => { this.sampleSubscription?.unsubscribe(); this.likertSubscription?.unsubscribe(); this.ratingSubscription?.unsubscribe(); this.teacherSubscription?.unsubscribe(); this.insatisfactionSubscription?.unsubscribe(); this.detailSubscription?.unsubscribe(); this.summarySubscription?.unsubscribe(); this.chartSubscription?.unsubscribe(); });
    this.form.controls.studentType.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => void this.reloadForType());
    for (const control of ['period', 'modality'] as const) {
      this.form.controls[control].valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.loadSample());
    }
    void this.initialize();
  }

  private async initialize(): Promise<void> {
    await this.loadFilters();
    this.loadSample();
  }

  private async reloadForType(): Promise<void> {
    this.invalidateSample();
    this.filtering.set(true);
    await this.loadFilters();
    this.filtering.set(false);
    this.loadSample();
  }

  private async loadFilters(): Promise<void> {
    const revision = ++this.filterRevision;
    try {
      const current = this.form.getRawValue();
      const params: Record<string, string> = current.studentType ? { studentType: current.studentType } : {};
      const response = await firstValueFrom(this.http.get<{ success: boolean; data: Filters }>(BASE + '/filters', { params }));
      if (revision !== this.filterRevision) return;
      if (response.success !== true) throw new Error('contract');
      const data = response.data;
      const value = (options: Option[], selected: string) => options.some(option => option.value === selected) ? selected : options[0]?.value ?? '';
      this.filters.set(data);
      this.form.patchValue({
        period: value(data.periods, current.period), studentType: value(data.studentTypes, current.studentType),
        modality: value(data.modalities, current.modality),
      }, { emitEvent: false });
    } catch {
      if (revision === this.filterRevision) { this.filters.set({ periods: [], studentTypes: [], modalities: [] }); this.error.set('No se pudieron cargar los filtros.'); }
    }
  }

  protected loadSample(): void {
    if (this.filtering()) return;
    const selection = this.form.getRawValue();
    if (!selection.period || !selection.studentType || !selection.modality) return;
    this.sampleSubscription?.unsubscribe();
    this.cancelLikert(); this.cancelRating(); this.cancelTeachers(); this.cancelInsatisfaction(); this.cancelDetail(); this.cancelSummary(); this.cancelChart();
    const revision = ++this.sampleRevision;
    this.loading.set(true); this.error.set(''); this.likertData.set(null); this.likertError.set('');
    this.sampleSubscription = this.http.get<{ success: boolean; data: { rows: Row[]; totals: Totals } }>(BASE + '/sample', { params: selection })
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: response => {
          if (revision !== this.sampleRevision) return;
          this.loading.set(false); this.loaded.set(true);
          if (response.success !== true) { this.rows.set([]); this.error.set('No se pudo cargar la muestra.'); return; }
          this.rows.set(response.data.rows); this.totals.set(response.data.totals);
          this.loadActiveSection();
        },
        error: () => {
          if (revision !== this.sampleRevision) return;
          this.loading.set(false); this.loaded.set(true); this.rows.set([]);
          this.error.set('No se pudo cargar la muestra.');
        },
      });
  }

  protected selectTop(id: TopTabId): void {
    this.activeTopTab.set(id);
    if (id === 'graficos') {
      this.activeTab.set('graficos');
      this.cancelLikert(); this.cancelRating(); this.cancelTeachers(); this.cancelInsatisfaction(); this.cancelDetail(); this.cancelSummary(); this.loadChart();
      return;
    }
    const first = TABS.find(tab => tab.top === id);
    if (first) this.selectTab(first.id);
  }

  protected selectTab(id: string): void {
    this.activeTab.set(id);
    if (this.loading() || this.filtering()) return;
    if (LIKERT_TABS.has(id)) { this.cancelRating(); this.cancelTeachers(); this.cancelInsatisfaction(); this.cancelDetail(); this.loadLikert(); }
    else if (RATING_TABS.has(id)) { this.cancelLikert(); this.cancelTeachers(); this.cancelInsatisfaction(); this.cancelDetail(); this.loadRating(); }
    else if (id === 'ranking') { this.cancelLikert(); this.cancelRating(); this.cancelInsatisfaction(); this.cancelDetail(); this.loadTeachers(); }
    else if (id === 'insatisfaccion') { this.cancelLikert(); this.cancelRating(); this.cancelTeachers(); this.cancelDetail(); this.cancelSummary(); this.loadInsatisfaction(); }
    else if (id === 'resumen') { this.cancelLikert(); this.cancelRating(); this.cancelTeachers(); this.cancelInsatisfaction(); this.cancelDetail(); this.cancelChart(); this.loadSummary(); }
    else if (id === 'graficos') { this.cancelLikert(); this.cancelRating(); this.cancelTeachers(); this.cancelInsatisfaction(); this.cancelDetail(); this.cancelSummary(); this.loadChart(); }
    else { this.cancelLikert(); this.cancelRating(); this.cancelTeachers(); this.cancelInsatisfaction(); this.cancelDetail(); this.cancelSummary(); this.cancelChart(); }
  }

  private loadActiveSection(): void {
    if (LIKERT_TABS.has(this.activeTab())) this.loadLikert();
    else if (RATING_TABS.has(this.activeTab())) this.loadRating();
    else if (this.activeTab() === 'ranking') this.loadTeachers();
    else if (this.activeTab() === 'insatisfaccion') this.loadInsatisfaction();
    else if (this.activeTab() === 'resumen') this.loadSummary();
    else if (this.activeTab() === 'graficos') this.loadChart();
  }

  private cancelLikert(): void { this.likertRevision++; this.likertSubscription?.unsubscribe(); this.likertLoading.set(false); this.likertData.set(null); this.likertError.set(''); }
  private cancelRating(): void { this.ratingRevision++; this.ratingSubscription?.unsubscribe(); this.ratingLoading.set(false); this.ratingData.set(null); this.ratingError.set(''); }
  private cancelTeachers(): void { this.teacherRevision++; this.teacherSubscription?.unsubscribe(); this.teacherLoading.set(false); this.teacherData.set(null); this.teacherError.set(''); }
  private cancelInsatisfaction(): void { this.insatisfactionRevision++; this.insatisfactionSubscription?.unsubscribe(); this.insatisfactionLoading.set(false); this.insatisfactionRows.set([]); this.insatisfactionError.set(''); }
  private cancelDetail(): void { this.detailRevision++; this.detailSubscription?.unsubscribe(); this.detailLoading.set(false); this.teacherDetail.set(null); this.detailError.set(''); }
  private cancelSummary(): void { this.summaryRevision++; this.summarySubscription?.unsubscribe(); this.summaryLoading.set(false); this.summaryRows.set([]); this.summaryError.set(''); }
  private cancelChart(): void { this.chartRevision++; this.chartSubscription?.unsubscribe(); this.chartLoading.set(false); this.chartData.set(null); this.chartError.set(''); }

  protected selectChart(id: string): void {
    this.selectedChart.set(id);
    if (this.activeTab() === 'graficos') this.loadChart();
  }

  private loadChart(): void {
    const selection = this.form.getRawValue();
    if (!selection.period || !selection.studentType || !selection.modality) return;
    this.chartSubscription?.unsubscribe();
    const revision = ++this.chartRevision;
    this.chartLoading.set(true); this.chartError.set(''); this.chartData.set(null);
    this.chartSubscription = this.http.get<{ success: boolean; data: { section: string; title: string; rows: LikertRow[] } }>(
      BASE + '/likert', { params: { ...selection, section: this.selectedChart() } })
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: response => {
          if (revision !== this.chartRevision) return;
          this.chartLoading.set(false);
          if (response.success !== true) { this.chartError.set('No se pudo cargar el gráfico.'); return; }
          const rows: ChartRow[] = response.data.rows.map(row => ({ description: row.description,
            counts: [row.alternative1, row.alternative2, row.alternative3, row.alternative4, row.alternative5],
            average: row.average, total: row.total }));
          this.chartData.set({ title: response.data.title, labels: LIKERT_LABELS, rows });
        },
        error: () => {
          if (revision !== this.chartRevision) return;
          this.chartLoading.set(false); this.chartData.set(null); this.chartError.set('No se pudo cargar el gráfico.');
        },
      });
  }

  private loadSummary(): void {
    const selection = this.form.getRawValue();
    if (!selection.period || !selection.studentType || !selection.modality) return;
    this.summarySubscription?.unsubscribe();
    const revision = ++this.summaryRevision;
    this.summaryLoading.set(true); this.summaryError.set(''); this.summaryRows.set([]);
    this.summarySubscription = this.http.get<{ success: boolean; data: { rows: SummaryRow[] } }>(BASE + '/summary', { params: { ...selection } })
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: response => {
          if (revision !== this.summaryRevision) return;
          this.summaryLoading.set(false);
          if (response.success !== true) { this.summaryError.set('No se pudo cargar el resumen.'); return; }
          this.summaryRows.set(response.data.rows);
        },
        error: () => {
          if (revision !== this.summaryRevision) return;
          this.summaryLoading.set(false); this.summaryRows.set([]); this.summaryError.set('No se pudo cargar el resumen.');
        },
      });
  }

  private loadTeachers(): void {
    const selection = this.form.getRawValue();
    if (!selection.period || !selection.studentType || !selection.modality) return;
    this.teacherSubscription?.unsubscribe();
    const revision = ++this.teacherRevision;
    this.teacherLoading.set(true); this.teacherError.set(''); this.teacherData.set(null);
    this.teacherSubscription = this.http.get<{ success: boolean; data: TeacherRankingData }>(BASE + '/teachers', { params: { ...selection } })
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: response => {
          if (revision !== this.teacherRevision) return;
          this.teacherLoading.set(false);
          if (response.success !== true) { this.teacherData.set(null); this.teacherError.set('No se pudo cargar la sección.'); return; }
          this.teacherData.set(response.data);
        },
        error: () => {
          if (revision !== this.teacherRevision) return;
          this.teacherLoading.set(false); this.teacherData.set(null); this.teacherError.set('No se pudo cargar la sección.');
        },
      });
  }

  private loadInsatisfaction(): void {
    const selection = this.form.getRawValue();
    if (!selection.period || !selection.studentType || !selection.modality) return;
    this.insatisfactionSubscription?.unsubscribe(); this.cancelDetail();
    const revision = ++this.insatisfactionRevision;
    this.insatisfactionLoading.set(true); this.insatisfactionError.set(''); this.insatisfactionRows.set([]);
    this.insatisfactionSubscription = this.http.get<{ success: boolean; data: { rows: InsatisfactionRow[] } }>(
      BASE + '/insatisfaction', { params: { ...selection } })
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: response => {
          if (revision !== this.insatisfactionRevision) return;
          this.insatisfactionLoading.set(false);
          if (response.success !== true) { this.insatisfactionError.set('No se pudo cargar la sección.'); return; }
          this.insatisfactionRows.set(response.data.rows);
        },
        error: () => {
          if (revision !== this.insatisfactionRevision) return;
          this.insatisfactionLoading.set(false); this.insatisfactionRows.set([]); this.insatisfactionError.set('No se pudo cargar la sección.');
        },
      });
  }

  protected loadDetail(row: InsatisfactionRow): void {
    if (!row.professorCode) return;
    this.detailSubscription?.unsubscribe();
    const revision = ++this.detailRevision;
    this.detailLoading.set(true); this.detailError.set(''); this.teacherDetail.set(null);
    this.detailSubscription = this.http.get<{ success: boolean; data: TeacherDetail }>(BASE + '/insatisfaction/detail', {
      params: { period: row.period, studentType: row.studentType, modality: row.modality, professor: row.professorCode },
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: response => {
        if (revision !== this.detailRevision) return;
        this.detailLoading.set(false);
        if (response.success !== true) { this.detailError.set('No se pudo cargar el detalle.'); return; }
        this.teacherDetail.set(response.data);
      },
      error: () => {
        if (revision !== this.detailRevision) return;
        this.detailLoading.set(false); this.detailError.set('No se pudo cargar el detalle.');
      },
    });
  }

  private loadRating(): void {
    const section = this.activeTab();
    if (!RATING_TABS.has(section)) return;
    const selection = this.form.getRawValue();
    if (!selection.period || !selection.studentType || !selection.modality) return;
    this.ratingSubscription?.unsubscribe();
    const revision = ++this.ratingRevision;
    this.ratingLoading.set(true); this.ratingError.set(''); this.ratingData.set(null);
    const endpoint = section === 'catedras-area' ? 'catedras-area' : 'skills';
    this.ratingSubscription = this.http.get<{ success: boolean; data: { rows: CatedraRow[] | SkillRow[]; totals: CatedraTotals | SkillTotals } }>(
      BASE + '/' + endpoint, { params: { ...selection } })
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: response => {
          if (revision !== this.ratingRevision) return;
          this.ratingLoading.set(false);
          if (response.success !== true) { this.ratingData.set(null); this.ratingError.set('No se pudo cargar la sección.'); return; }
          this.ratingData.set(this.toScaleData(section, response.data.rows, response.data.totals));
        },
        error: () => {
          if (revision !== this.ratingRevision) return;
          this.ratingLoading.set(false); this.ratingData.set(null); this.ratingError.set('No se pudo cargar la sección.');
        },
      });
  }

  private toScaleData(section: string, rows: CatedraRow[] | SkillRow[], totals: CatedraTotals | SkillTotals): ScaleData {
    if (section === 'catedras-area') {
      const values = totals as CatedraTotals;
      return {
        section, title: 'Cátedras x Área', labels: CATEDRA_LABELS,
        rows: (rows as CatedraRow[]).map(row => ({ key: row.code || row.description, description: row.description,
          alternatives: [row.alternative1, row.alternative2, row.alternative3, row.alternative4, row.alternative5],
          average: row.average, total: row.total })),
        totals: [values.alternative1, values.alternative2, values.alternative3, values.alternative4, values.alternative5],
      };
    }
    const values = totals as SkillTotals;
    return {
      section, title: 'Conocimientos y habilidades', labels: SKILL_LABELS,
      rows: (rows as SkillRow[]).map(row => ({ key: row.question || row.description, description: row.description,
        alternatives: [row.alternative1, row.alternative2, row.alternative3, row.alternative4, row.alternative5, row.alternative6, row.alternative7],
        average: row.average, total: row.total })),
      totals: [values.alternative1, values.alternative2, values.alternative3, values.alternative4, values.alternative5, values.alternative6, values.alternative7],
    };
  }

  private loadLikert(): void {
    const section = this.activeTab();
    if (!LIKERT_TABS.has(section)) return;
    const selection = this.form.getRawValue();
    if (!selection.period || !selection.studentType || !selection.modality) return;
    this.likertSubscription?.unsubscribe();
    const revision = ++this.likertRevision;
    this.likertLoading.set(true); this.likertError.set(''); this.likertData.set(null);
    this.likertSubscription = this.http.get<{ success: boolean; data: { section: string; title: string; rows: LikertRow[]; totals: LikertTotals } }>(
      BASE + '/likert', { params: { ...selection, section } })
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: response => {
          if (revision !== this.likertRevision) return;
          this.likertLoading.set(false);
          if (response.success !== true) { this.likertData.set(null); this.likertError.set('No se pudo cargar la sección.'); return; }
          this.likertData.set(response.data);
        },
        error: () => {
          if (revision !== this.likertRevision) return;
          this.likertLoading.set(false); this.likertData.set(null); this.likertError.set('No se pudo cargar la sección.');
        },
      });
  }
  private invalidateSample(): void {
    this.sampleRevision++; this.sampleSubscription?.unsubscribe(); this.rows.set([]); this.loaded.set(false); this.totals.set({ sample: 0, inProgress: 0, answered: 0 });
  }
}
