import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpClient } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { Subscription, firstValueFrom } from 'rxjs';

interface Option { value: string; text: string; }
interface Filters { periods: Option[]; studentTypes: Option[]; modalities: Option[]; }
interface Row { type: string; modality: string | null; sample: number; inProgress: number; answered: number; }
interface Totals { sample: number; inProgress: number; answered: number; }
interface Tab { id: string; label: string; group: string; }
const BASE = '/api/reports/postgraduate/exit-survey-statistics';
const TABS: readonly Tab[] = [
  { id: 'malla', label: 'Malla Curricular', group: 'Parte I' },
  { id: 'cuerpo-academico', label: 'Cuerpo Académico', group: 'Parte I' },
  { id: 'funcionamiento', label: 'Funcionamiento', group: 'Parte I' },
  { id: 'infraestructura', label: 'Infraestructura y recursos', group: 'Parte I' },
  { id: 'servicios', label: 'Servicios', group: 'Parte I' },
  { id: 'vinculacion-medio', label: 'Vinculación Medio', group: 'Parte I' },
  { id: 'networking', label: 'Networking', group: 'Parte I' },
  { id: 'resumen', label: 'Resumen', group: 'Parte I' },
  { id: 'catedras-area', label: 'Cátedras x Área', group: 'Parte II' },
  { id: 'ranking', label: 'Ranking', group: 'Parte III' },
  { id: 'insatisfaccion', label: 'Insatisfacción', group: 'Parte III' },
  { id: 'habilidades', label: 'Conocimientos y habilidades', group: 'Parte IV' },
  { id: 'evaluacion-general', label: 'Evaluación general', group: 'Parte IV' },
  { id: 'graficos', label: 'Gráficos', group: 'Gráficos' },
];

@Component({
  selector: 'app-exit-survey-statistics',
  imports: [ReactiveFormsModule, NgSelectModule],
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
      <div class="form-section mt-3">
        <h2 class="h6">Secciones</h2>
        <div class="d-flex flex-wrap gap-2" role="tablist" aria-label="Secciones de la encuesta">
          @for (tab of tabs; track tab.id) {
            <button type="button" class="btn btn-sm" role="tab" [attr.data-tab]="tab.id"
              [class.btn-primary]="activeTab() === tab.id" [class.btn-outline-primary]="activeTab() !== tab.id"
              [attr.aria-selected]="activeTab() === tab.id" (click)="selectTab(tab.id)">{{ tab.label }}</button>
          }
        </div>
        <div class="alert alert-info mt-3" role="tabpanel" data-panel>
          <strong>{{ activeLabel() }}</strong>: sección pendiente de migración.
        </div>
      </div>
    </div>
  `,
})
export class ExitSurveyStatistics {
  private readonly http = inject(HttpClient);
  private readonly destroyRef = inject(DestroyRef);
  readonly form = inject(FormBuilder).nonNullable.group({ period: '', studentType: '', modality: '' });
  protected readonly tabs = TABS;
  protected readonly filters = signal<Filters>({ periods: [], studentTypes: [], modalities: [] });
  protected readonly rows = signal<Row[]>([]);
  protected readonly totals = signal<Totals>({ sample: 0, inProgress: 0, answered: 0 });
  readonly activeTab = signal('malla');
  protected readonly activeLabel = computed(() => TABS.find(tab => tab.id === this.activeTab())?.label ?? '');
  protected readonly loading = signal(false);
  private readonly filtering = signal(false);
  protected readonly loaded = signal(false);
  protected readonly error = signal('');
  protected readonly busy = computed(() => this.loading() || this.filtering());
  private filterRevision = 0;
  private sampleRevision = 0;
  private sampleSubscription?: Subscription;

  constructor() {
    this.destroyRef.onDestroy(() => this.sampleSubscription?.unsubscribe());
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
    const revision = ++this.sampleRevision;
    this.loading.set(true); this.error.set('');
    this.sampleSubscription = this.http.get<{ success: boolean; data: { rows: Row[]; totals: Totals } }>(BASE + '/sample', { params: selection })
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: response => {
          if (revision !== this.sampleRevision) return;
          this.loading.set(false); this.loaded.set(true);
          if (response.success !== true) { this.rows.set([]); this.error.set('No se pudo cargar la muestra.'); return; }
          this.rows.set(response.data.rows); this.totals.set(response.data.totals);
        },
        error: () => {
          if (revision !== this.sampleRevision) return;
          this.loading.set(false); this.loaded.set(true); this.rows.set([]);
          this.error.set('No se pudo cargar la muestra.');
        },
      });
  }

  protected selectTab(id: string): void { this.activeTab.set(id); }
  private invalidateSample(): void {
    this.sampleRevision++; this.sampleSubscription?.unsubscribe(); this.rows.set([]); this.loaded.set(false); this.totals.set({ sample: 0, inProgress: 0, answered: 0 });
  }
}
