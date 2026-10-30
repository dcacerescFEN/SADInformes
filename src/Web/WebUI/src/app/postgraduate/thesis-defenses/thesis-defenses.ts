import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { Subscription } from 'rxjs';
import { Search } from '../../shared/components/search';

interface Option { value: string; text: string; }
interface Filters { years: string[]; studentTypes: string[]; modalities: string[]; }
interface Selection { defenseYear: string; studentType: string; modality: string; entryYear: string; }
interface PageItem { label: string; page: number | null; }
interface ReportTab { id: 'coordination' | 'treasury'; label: string; }
interface CoordinationRow {
  studentCode: string; studentName: string; program: string; modality: string; entryYear: string;
  examDate: string; period: string; courseCode: string; section: string; grade: string;
  examStatus: string; thesisTopic: string; guide: string; coGuide: string; informant1: string;
  informant2: string; informant3: string; costCenter: string; defenseYear: string;
  resolutionDate: string;
}
interface TreasuryRow {
  period: string; courseCode: string; section: string; professorCode: string;
  professorName: string; type: string; studentCode: string; grade: string; studentName: string;
  program: string; modality: string; entryYear: string; thesisTopic: string; examDate: string;
  examStatus: string; costCenter: string; resolutionDate: string;
}
interface Report { coordination: CoordinationRow[]; treasury: TreasuryRow[]; coordinationTotal: number; treasuryTotal: number; }
const BASE = '/api/reports/postgraduate/thesis-defenses';
const TODOS = 'Todos';
const PAGE_SIZE = 10;
const REPORT_TABS: readonly ReportTab[] = [
  { id: 'coordination', label: 'Coordinación' },
  { id: 'treasury', label: 'Tesorería' },
];

function pageWindow(totalPages: number, currentPage: number): PageItem[] {
  const pages = new Set<number>([1, totalPages]);
  for (let page = Math.max(1, currentPage - 2); page <= Math.min(totalPages, currentPage + 2); page++) {
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
}

@Component({
  selector: 'app-thesis-defenses',
  imports: [ReactiveFormsModule, NgSelectModule, Search],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="container-fluid px-0 py-4">
      <div class="page-header">
        <div class="d-flex flex-wrap justify-content-between align-items-start gap-3">
          <div>
            <h1 class="page-title"><i class="bi bi-journal-check" aria-hidden="true"></i>Reporte Defensas de Tesis Postgrado</h1>
            <p class="page-subtitle mb-0">Comisiones de defensa en formato Coordinación y Tesorería.</p>
          </div>
          <div class="d-flex flex-wrap gap-2">
            <button type="button" class="btn btn-create" data-action="export-coordination"
              [disabled]="coordination().length === 0 || exporting()" (click)="download('coordination')">
              <i class="bi bi-file-earmark-excel me-1" aria-hidden="true"></i>Exportar Coordinación
            </button>
            <button type="button" class="btn btn-create" data-action="export-treasury"
              [disabled]="treasury().length === 0 || exporting()" (click)="download('treasury')">
              <i class="bi bi-file-earmark-excel me-1" aria-hidden="true"></i>Exportar Tesorería
            </button>
          </div>
        </div>
      </div>
      <div class="page-header">
        <form [formGroup]="form" (ngSubmit)="show()">
          <div class="row g-3 align-items-end">
            @for (field of fields; track field.control) {
              <div class="col-xl-3 col-md-6">
                <div class="ng-select-container-custom">
                  <label class="floating-label" [for]="'defense-' + field.control">{{ field.label }}</label>
                  <ng-select [labelForId]="'defense-' + field.control" [formControlName]="field.control"
                    [attr.data-filter]="field.control" [items]="options()[field.control]"
                    bindLabel="text" bindValue="value" [clearable]="false" class="form-select ng-select-custom" />
                </div>
              </div>
            }
            <div class="col-auto">
              <button type="submit" class="btn btn-search" data-action="show"
                [disabled]="!ready() || filtersLoading() || loading() || form.invalid">
                <i class="bi bi-search me-1" aria-hidden="true"></i>Buscar
              </button>
              @if (!ready() && error()) {
                <button type="button" class="btn btn-outline-secondary ms-2" (click)="loadFilters()">Reintentar filtros</button>
              }
            </div>
          </div>
        </form>
      </div>
      @if (filtersLoading()) { <p role="status">Cargando filtros…</p> }
      @if (loading()) { <p role="status">Cargando defensas…</p> }
      @if (error()) { <div class="alert alert-warning" role="alert">{{ error() }}</div> }
      @if (notice()) { <p class="text-body-secondary" role="status">{{ notice() }}</p> }
      <div class="app-tabs mb-3" role="tablist" aria-label="Reportes de defensas">
        @for (tab of reportTabs; track tab.id) {
          <button type="button" class="app-tab" role="tab" [attr.data-report-tab]="tab.id"
            [class.active]="activeTab() === tab.id" [attr.aria-selected]="activeTab() === tab.id"
            (click)="selectTab(tab.id)">{{ tab.label }}</button>
        }
      </div>
      @if (coordination().length > 0) {
        @if (activeTab() === 'coordination') {
        <div class="form-section">
          <h2 class="h5 mb-3">Reporte Comisiones formato Coordinación</h2>
          <div class="d-flex justify-content-end mb-3"><div class="search-box"><app-search (search)="searchCoordination($event)" /></div></div>
          @if (filteredCoordination().length > 0) {
          <div class="table-responsive">
            <table class="table table-hover align-middle app-table" aria-label="Reporte de comisiones formato Coordinación">
              <thead class="table-light"><tr>
                <th>Cod_Alumno</th><th>Nombre</th><th>Programa</th><th>Modalidad</th><th>Año Ingreso</th>
                <th>Fecha Examen</th><th>Periodo</th><th>Cod_Catedra</th><th>Cod_Seccion</th><th>Nota</th>
                <th>Estado_Examen</th><th>Tema/Nombre Tésis</th><th>GUIA</th><th>COGUIA</th>
                <th>INFORMANTE1</th><th>INFORMANTE2</th><th>INFORMANTE3</th><th>Centro Costo</th>
                <th>Año Defensa</th><th>Fecha Resolución</th>
              </tr></thead>
              <tbody>@for (row of pagedCoordination(); track row.studentCode + row.courseCode) {
                <tr>
                  <td>{{ row.studentCode }}</td><td>{{ row.studentName }}</td><td>{{ row.program }}</td>
                  <td>{{ row.modality }}</td><td>{{ row.entryYear }}</td><td>{{ row.examDate }}</td>
                  <td>{{ row.period }}</td><td>{{ row.courseCode }}</td><td>{{ row.section }}</td>
                  <td>{{ row.grade }}</td><td>{{ row.examStatus }}</td><td>{{ row.thesisTopic }}</td>
                  <td>{{ row.guide }}</td><td>{{ row.coGuide }}</td><td>{{ row.informant1 }}</td>
                  <td>{{ row.informant2 }}</td><td>{{ row.informant3 }}</td><td>{{ row.costCenter }}</td>
                  <td>{{ row.defenseYear }}</td><td>{{ row.resolutionDate }}</td>
                </tr>
              }</tbody>
            </table>
          </div>
          <div class="d-flex flex-wrap justify-content-between align-items-center gap-2">
            <small class="text-body-secondary">Mostrando {{ coordinationFirst() }}–{{ coordinationLast() }} de {{ filteredCoordination().length }}</small>
            @if (coordinationTotalPages() > 1) {
              <nav aria-label="Paginación de Coordinación">
                <ul class="pagination pagination-sm mb-0">
                  <li class="page-item" [class.disabled]="coordinationCurrentPage() === 1"><button type="button" class="page-link" (click)="setCoordinationPage(coordinationCurrentPage() - 1)">Anterior</button></li>
                  @for (item of coordinationPageItems(); track $index) {
                    @if (item.page === null) {
                      <li class="page-item disabled"><span class="page-link" aria-hidden="true">{{ item.label }}</span></li>
                    } @else {
                      <li class="page-item" [class.active]="coordinationCurrentPage() === item.page"><button type="button" class="page-link" (click)="setCoordinationPage(item.page)">{{ item.label }}</button></li>
                    }
                  }
                  <li class="page-item" [class.disabled]="coordinationCurrentPage() === coordinationTotalPages()"><button type="button" class="page-link" (click)="setCoordinationPage(coordinationCurrentPage() + 1)">Siguiente</button></li>
                </ul>
              </nav>
            }
          </div>
          } @else {
            <p class="text-body-secondary" role="status">No se encontraron registros.</p>
          }
          <p class="fw-semibold mt-4 mb-0">Total Coordinación: {{ coordination().length }}</p>
        </div>
        }
      }
      @if (treasury().length > 0) {
        @if (activeTab() === 'treasury') {
        <div class="form-section">
          <h2 class="h5 mb-3">Reporte Comisiones formato Tesorería</h2>
          <div class="d-flex justify-content-end mb-3"><div class="search-box"><app-search (search)="searchTreasury($event)" /></div></div>
          @if (filteredTreasury().length > 0) {
          <div class="table-responsive">
            <table class="table table-hover align-middle app-table" aria-label="Reporte de comisiones formato Tesorería">
              <thead class="table-light"><tr>
                <th>Periodo</th><th>Cod_Catedra</th><th>Cod_Seccion</th><th>Cod_Profesor</th>
                <th>Nombre Profesor</th><th>Tipo</th><th>Cod_Alumno</th><th>Nota</th><th>Estudiante</th>
                <th>Programa</th><th>Modalidad</th><th>Año Ingreso</th><th>Tema/Nombre Tésis</th>
                <th>Fecha_Examen</th><th>Estado_Examen</th><th>Centro Costo</th><th>Fecha Resolución</th>
              </tr></thead>
              <tbody>@for (row of pagedTreasury(); track row.professorCode + row.studentCode + row.period) {
                <tr>
                  <td>{{ row.period }}</td><td>{{ row.courseCode }}</td><td>{{ row.section }}</td>
                  <td>{{ row.professorCode }}</td><td>{{ row.professorName }}</td><td>{{ row.type }}</td>
                  <td>{{ row.studentCode }}</td><td>{{ row.grade }}</td><td>{{ row.studentName }}</td>
                  <td>{{ row.program }}</td><td>{{ row.modality }}</td><td>{{ row.entryYear }}</td>
                  <td>{{ row.thesisTopic }}</td><td>{{ row.examDate }}</td><td>{{ row.examStatus }}</td>
                  <td>{{ row.costCenter }}</td><td>{{ row.resolutionDate }}</td>
                </tr>
              }</tbody>
            </table>
          </div>
          <div class="d-flex flex-wrap justify-content-between align-items-center gap-2">
            <small class="text-body-secondary">Mostrando {{ treasuryFirst() }}–{{ treasuryLast() }} de {{ filteredTreasury().length }}</small>
            @if (treasuryTotalPages() > 1) {
              <nav aria-label="Paginación de Tesorería">
                <ul class="pagination pagination-sm mb-0">
                  <li class="page-item" [class.disabled]="treasuryCurrentPage() === 1"><button type="button" class="page-link" (click)="setTreasuryPage(treasuryCurrentPage() - 1)">Anterior</button></li>
                  @for (item of treasuryPageItems(); track $index) {
                    @if (item.page === null) {
                      <li class="page-item disabled"><span class="page-link" aria-hidden="true">{{ item.label }}</span></li>
                    } @else {
                      <li class="page-item" [class.active]="treasuryCurrentPage() === item.page"><button type="button" class="page-link" (click)="setTreasuryPage(item.page)">{{ item.label }}</button></li>
                    }
                  }
                  <li class="page-item" [class.disabled]="treasuryCurrentPage() === treasuryTotalPages()"><button type="button" class="page-link" (click)="setTreasuryPage(treasuryCurrentPage() + 1)">Siguiente</button></li>
                </ul>
              </nav>
            }
          </div>
          } @else {
            <p class="text-body-secondary" role="status">No se encontraron registros.</p>
          }
          <p class="fw-semibold mt-4 mb-0">Total Tesorería: {{ treasury().length }}</p>
        </div>
        }
      }
    </div>
  `,
})
export class ThesisDefenses {
  private readonly http = inject(HttpClient);
  private readonly destroyRef = inject(DestroyRef);
  readonly form = inject(FormBuilder).nonNullable.group({
    defenseYear: [TODOS, Validators.required],
    studentType: [TODOS, Validators.required],
    modality: [TODOS, Validators.required],
    entryYear: [TODOS, Validators.required],
  });
  protected readonly fields = [
    { control: 'defenseYear', label: 'Año Defensa' },
    { control: 'studentType', label: 'Tipo Alumno' },
    { control: 'modality', label: 'Modalidad' },
    { control: 'entryYear', label: 'Año Ingreso' },
  ] as const;
  private readonly filters = signal<Filters>({ years: [], studentTypes: [], modalities: [] });
  protected readonly options = computed(() => {
    const option = (value: string): Option => ({ value, text: value });
    return {
      defenseYear: [TODOS, ...this.filters().years].map(option),
      studentType: this.filters().studentTypes.map(option),
      modality: this.filters().modalities.map(option),
      entryYear: [TODOS, ...this.filters().years].map(option),
    };
  });
  protected readonly coordination = signal<CoordinationRow[]>([]);
  protected readonly treasury = signal<TreasuryRow[]>([]);
  protected readonly reportTabs = REPORT_TABS;
  protected readonly activeTab = signal<ReportTab['id']>('coordination');
  protected readonly coordinationSearch = signal('');
  protected readonly treasurySearch = signal('');
  protected readonly coordinationPage = signal(1);
  protected readonly treasuryPage = signal(1);
  protected readonly filteredCoordination = computed(() => {
    const term = this.coordinationSearch().trim().toLowerCase();
    if (!term) return this.coordination();
    return this.coordination().filter(row => [
      row.studentCode, row.studentName, row.program, row.modality, row.entryYear, row.examDate,
      row.period, row.courseCode, row.section, row.grade, row.examStatus, row.thesisTopic,
      row.guide, row.coGuide, row.informant1, row.informant2, row.informant3, row.costCenter,
      row.defenseYear, row.resolutionDate,
    ].join(' ').toLowerCase().includes(term));
  });
  protected readonly filteredTreasury = computed(() => {
    const term = this.treasurySearch().trim().toLowerCase();
    if (!term) return this.treasury();
    return this.treasury().filter(row => [
      row.period, row.courseCode, row.section, row.professorCode, row.professorName, row.type,
      row.studentCode, row.grade, row.studentName, row.program, row.modality, row.entryYear,
      row.thesisTopic, row.examDate, row.examStatus, row.costCenter, row.resolutionDate,
    ].join(' ').toLowerCase().includes(term));
  });
  protected readonly coordinationTotalPages = computed(() =>
    Math.max(1, Math.ceil(this.filteredCoordination().length / PAGE_SIZE)));
  protected readonly treasuryTotalPages = computed(() =>
    Math.max(1, Math.ceil(this.filteredTreasury().length / PAGE_SIZE)));
  protected readonly coordinationCurrentPage = computed(() =>
    Math.min(this.coordinationPage(), this.coordinationTotalPages()));
  protected readonly treasuryCurrentPage = computed(() =>
    Math.min(this.treasuryPage(), this.treasuryTotalPages()));
  protected readonly pagedCoordination = computed(() => {
    const start = (this.coordinationCurrentPage() - 1) * PAGE_SIZE;
    return this.filteredCoordination().slice(start, start + PAGE_SIZE);
  });
  protected readonly pagedTreasury = computed(() => {
    const start = (this.treasuryCurrentPage() - 1) * PAGE_SIZE;
    return this.filteredTreasury().slice(start, start + PAGE_SIZE);
  });
  protected readonly coordinationPageItems = computed(() =>
    pageWindow(this.coordinationTotalPages(), this.coordinationCurrentPage()));
  protected readonly treasuryPageItems = computed(() =>
    pageWindow(this.treasuryTotalPages(), this.treasuryCurrentPage()));
  protected readonly coordinationFirst = computed(() => this.filteredCoordination().length === 0
    ? 0 : (this.coordinationCurrentPage() - 1) * PAGE_SIZE + 1);
  protected readonly coordinationLast = computed(() =>
    Math.min(this.coordinationCurrentPage() * PAGE_SIZE, this.filteredCoordination().length));
  protected readonly treasuryFirst = computed(() => this.filteredTreasury().length === 0
    ? 0 : (this.treasuryCurrentPage() - 1) * PAGE_SIZE + 1);
  protected readonly treasuryLast = computed(() =>
    Math.min(this.treasuryCurrentPage() * PAGE_SIZE, this.filteredTreasury().length));
  protected readonly filtersLoading = signal(false);
  protected readonly loading = signal(false);
  protected readonly exporting = signal(false);
  protected readonly ready = signal(false);
  protected readonly error = signal('');
  protected readonly notice = signal('');
  private readonly shownSelection = signal<Selection | null>(null);
  private reportRevision = 0;
  private reportSubscription?: Subscription;
  private exportSubscription?: Subscription;

  constructor() {
    this.form.controls.studentType.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => {
      this.form.controls.modality.setValue(TODOS, { emitEvent: false });
      this.loadFilters();
    });
    this.form.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => this.invalidate());
    this.destroyRef.onDestroy(() => this.invalidate());
    this.loadFilters();
  }

  protected loadFilters(): void {
    this.filtersLoading.set(true);
    this.ready.set(false);
    this.error.set('');
    const type = this.form.controls.studentType.value;
    this.http.get<{ success: boolean; data: Filters }>(BASE + '/filters', {
      params: { studentType: type },
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: result => {
        this.filtersLoading.set(false);
        if (!result.success) { this.error.set('No se pudieron cargar los filtros.'); return; }
        const current = this.form.getRawValue();
        this.filters.set(result.data);
        this.form.patchValue({
          defenseYear: result.data.years.includes(current.defenseYear) ? current.defenseYear : TODOS,
          studentType: result.data.studentTypes.includes(current.studentType) ? current.studentType : TODOS,
          modality: result.data.modalities.includes(current.modality) ? current.modality : TODOS,
          entryYear: result.data.years.includes(current.entryYear) ? current.entryYear : TODOS,
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
    this.reportSubscription = this.http.get<{ success: boolean; data: Report }>(
      BASE, { params: selection }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: result => {
          if (revision !== this.reportRevision) return;
          this.loading.set(false);
          if (!result.success) { this.error.set('No se pudo cargar el reporte.'); return; }
          this.coordination.set(result.data.coordination);
          this.treasury.set(result.data.treasury);
          this.shownSelection.set(selection);
          if (result.data.coordinationTotal + result.data.treasuryTotal === 0) this.notice.set('Sin Datos');
        },
        error: () => {
          if (revision !== this.reportRevision) return;
          this.loading.set(false);
          this.error.set('No se pudo cargar el reporte.');
        },
      });
  }

  protected selectTab(tab: ReportTab['id']): void {
    this.activeTab.set(tab);
  }

  protected searchCoordination(term: string): void {
    this.coordinationSearch.set(term);
    this.coordinationPage.set(1);
  }

  protected searchTreasury(term: string): void {
    this.treasurySearch.set(term);
    this.treasuryPage.set(1);
  }

  protected setCoordinationPage(page: number): void {
    this.coordinationPage.set(Math.min(Math.max(page, 1), this.coordinationTotalPages()));
  }

  protected setTreasuryPage(page: number): void {
    this.treasuryPage.set(Math.min(Math.max(page, 1), this.treasuryTotalPages()));
  }

  protected download(kind: 'coordination' | 'treasury'): void {
    const selection = this.shownSelection();
    if (!selection || this.exporting()) return;
    const revision = this.reportRevision;
    this.exporting.set(true);
    this.error.set('');
    this.exportSubscription = this.http.get(BASE + '/download/' + kind,
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
            link.download = kind === 'coordination'
              ? 'ReporteComisionesCoordinacion.xlsx' : 'ReporteComisionesTesoreria.xlsx';
            document.body.appendChild(link); link.click();
          } finally { link.remove(); URL.revokeObjectURL(url); }
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
    this.coordination.set([]);
    this.treasury.set([]);
    this.coordinationSearch.set('');
    this.treasurySearch.set('');
    this.coordinationPage.set(1);
    this.treasuryPage.set(1);
    this.shownSelection.set(null);
    this.loading.set(false);
    this.exporting.set(false);
    this.error.set('');
    this.notice.set('');
  }
}
