import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpClient } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { Subscription, firstValueFrom } from 'rxjs';
import { NavigationService } from '../../core/navigation/navigation-service';
import { NotificationService } from '../../core/services/notification-service';

interface Option { value: string; text: string; }
interface Filters { studentTypes: Option[]; modalities: Option[]; years: string[]; }
interface Row {
  studentType: string; modality: string; personCode: string; name: string; studentCode: string;
  entrySemester: string; exitDate: string; degreeDate: string; enrolled: boolean; progress: string | null;
}
const BASE = '/api/reports/postgraduate/exit-survey-enrollment';
const DEFAULT_PHOTO = 'https://fotos.fen.uchile.cl/perfilNN.jpg';

@Component({
  selector: 'app-exit-survey-enrollment',
  imports: [ReactiveFormsModule, NgSelectModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="container-fluid px-0 py-4">
      <div class="page-header">
        <div class="d-flex flex-wrap justify-content-between align-items-start gap-3">
          <div>
            <h1 class="page-title"><i class="bi bi-clipboard-check" aria-hidden="true"></i>Inscripción Encuesta Salida</h1>
            <p class="page-subtitle mb-0">Selección e inscripción de alumnos en la encuesta de salida.</p>
          </div>
          <button type="button" class="btn btn-create" data-action="enroll"
            [disabled]="!canEnroll() || selected().size === 0 || busy()" (click)="enroll()">
            <i class="bi bi-person-plus me-2" aria-hidden="true"></i>{{ saving() ? 'Inscribiendo…' : 'Inscribir' }}
          </button>
        </div>
      </div>
      <div class="page-header">
        <form [formGroup]="form">
          <div class="row g-3 align-items-end">
            <div class="col-xl-3 col-md-6">
              <div class="ng-select-container-custom">
                <label class="floating-label" for="exitSurveyType">Tipo Alumno</label>
                <ng-select inputId="exitSurveyType" formControlName="studentType" [items]="filters().studentTypes"
                  bindLabel="text" bindValue="value" [clearable]="false" class="form-select ng-select-custom" />
              </div>
            </div>
            <div class="col-xl-3 col-md-6">
              <div class="ng-select-container-custom">
                <label class="floating-label" for="exitSurveyModality">Modalidad</label>
                <ng-select inputId="exitSurveyModality" formControlName="modality" [items]="filters().modalities"
                  bindLabel="text" bindValue="value" [clearable]="false" class="form-select ng-select-custom" />
              </div>
            </div>
            <div class="col-xl-3 col-md-6">
              <div class="ng-select-container-custom">
                <label class="floating-label" for="exitSurveyYear">Año Egreso/Titulación</label>
                <ng-select inputId="exitSurveyYear" formControlName="year" [items]="years()"
                  [clearable]="false" class="form-select ng-select-custom" />
              </div>
            </div>
            <div class="col-xl-auto">
              <button type="button" class="btn btn-search" data-action="refresh" [disabled]="busy()" (click)="loadRows()">
                <i class="bi bi-arrow-clockwise me-1" aria-hidden="true"></i>Actualizar
              </button>
            </div>
          </div>
        </form>
      </div>
      @if (error()) { <div class="alert alert-warning" role="alert">{{ error() }}</div> }
      @if (notice()) { <div class="alert alert-success" role="status">{{ notice() }}</div> }
      @if (busy()) { <p role="status">{{ saving() ? 'Inscribiendo…' : 'Cargando…' }}</p> }
      @if (rows().length > 0) {
        <div class="form-section">
          <div class="table-responsive">
            <table class="table table-hover align-middle app-table" aria-label="Inscripción encuesta de salida">
              <thead class="table-light"><tr>
                <th>Tipo Alumno</th><th>Modalidad</th><th>Foto</th><th>Rut</th><th>Nombre</th><th>Cod. Alumno</th>
                <th>Semestre de Ingreso</th><th>Fecha Egreso</th><th>Fecha Titulación</th><th>Inscrito</th>
                <th><label><input type="checkbox" class="form-check-input me-1" data-action="select-all"
                  [checked]="allSelected()" [disabled]="!canEnroll() || busy() || eligibleCount() === 0"
                  (change)="selectAll($any($event.target).checked)" />Inscribir</label></th><th>Estado</th>
              </tr></thead>
              <tbody>
                @for (row of rows(); track row.studentCode) {
                  <tr>
                    <td>{{ row.studentType }}</td><td>{{ row.modality }}</td>
                    <td><img [src]="photo(row.personCode)" (error)="photoFailed($event)" [alt]="'Foto de ' + row.name"
                      width="35" height="47" loading="lazy" /></td>
                    <td>{{ row.personCode }}</td><td class="fw-semibold">{{ row.name }}</td>
                    <td>
                      @if (studentUrl(row.studentCode); as url) {
                        <a class="student-file-link" [href]="url" target="_blank" rel="noopener noreferrer">{{ row.studentCode }}</a>
                      } @else { {{ row.studentCode }} }
                    </td>
                    <td>{{ row.entrySemester }}</td><td>{{ row.exitDate }}</td><td>{{ row.degreeDate }}</td>
                    <td>{{ row.enrolled ? 'SI' : 'NO' }}</td>
                    <td class="text-center"><input type="checkbox" class="form-check-input" [attr.data-student]="row.studentCode"
                      [attr.aria-label]="'Inscribir ' + row.studentCode"
                      [disabled]="!canEnroll() || row.enrolled || busy()"
                      [checked]="selected().has(row.studentCode)" (change)="toggle(row)" /></td>
                    <td>{{ status(row) }}</td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
          <p class="fw-semibold mt-3 mb-0">Total: {{ rows().length }} · Seleccionados: {{ selected().size }}</p>
        </div>
      } @else if (loaded() && !busy()) {
        <p class="text-body-secondary" role="status">Sin Datos</p>
      }
    </div>
  `,
})
export class ExitSurveyEnrollment {
  private readonly http = inject(HttpClient);
  private readonly navigation = inject(NavigationService);
  private readonly notifications = inject(NotificationService);
  private readonly destroyRef = inject(DestroyRef);
  readonly form = inject(FormBuilder).nonNullable.group({ studentType: 'Todos', modality: 'Todos', year: '' });
  protected readonly filters = signal<Filters>({ studentTypes: [], modalities: [], years: [] });
  protected readonly years = computed(() => this.filters().years);
  protected readonly rows = signal<Row[]>([]);
  readonly selected = signal<Set<string>>(new Set());
  protected readonly canEnroll = signal(false);
  protected readonly error = signal('');
  protected readonly notice = signal('');
  protected readonly loading = signal(false);
  private readonly filtering = signal(false);
  protected readonly saving = signal(false);
  protected readonly loaded = signal(false);
  protected readonly busy = computed(() => this.loading() || this.filtering() || this.saving());
  protected readonly eligibleCount = computed(() => this.canEnroll() ? this.rows().filter(row => !row.enrolled).length : 0);
  protected readonly allSelected = computed(() => this.eligibleCount() > 0 && this.selected().size === this.eligibleCount());
  private filterRevision = 0;
  private listRevision = 0;
  private listSubscription?: Subscription;
  private initialized = false;

  constructor() {
    this.destroyRef.onDestroy(() => this.listSubscription?.unsubscribe());
    void this.initialize();
  }

  private async initialize(): Promise<void> {
    await this.loadFilters();
    await this.loadRows();
    if (this.initialized) return;
    this.initialized = true;
    this.form.controls.studentType.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      void this.reloadAfterTypeChange();
    });
    for (const control of ['modality', 'year'] as const) {
      this.form.controls[control].valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => void this.loadRows());
    }
  }

  private async reloadAfterTypeChange(): Promise<void> {
    this.invalidate(); this.filtering.set(true);
    await this.loadFilters();
    this.filtering.set(false);
    if (this.initialized && !this.error()) await this.loadRows();
  }

  private async loadFilters(): Promise<void> {
    const revision = ++this.filterRevision;
    this.error.set('');
    try {
      const current = this.form.getRawValue();
      const params: Record<string, string> = current.studentType ? { studentType: current.studentType } : {};
      const response = await firstValueFrom(this.http.get<{ success: boolean; data: Filters }>(BASE + '/filters', { params }));
      if (revision !== this.filterRevision) return;
      if (response.success !== true) throw new Error('contract');
      const filters = response.data;
      const option = (value: string, options: Option[]) =>
        options.some(item => item.value === value) ? value : options[0]?.value ?? '';
      this.filters.set(filters);
      this.form.patchValue({
        studentType: option(current.studentType, filters.studentTypes),
        modality: option(current.modality, filters.modalities),
        year: filters.years.includes(current.year) ? current.year : filters.years[0] ?? '',
      }, { emitEvent: false });
    } catch {
      if (revision === this.filterRevision) { this.filters.set({ studentTypes: [], modalities: [], years: [] }); this.error.set('No se pudieron cargar los filtros.'); }
    }
  }

  protected loadRows(): void {
    if (this.busy()) return;
    const selection = this.form.getRawValue();
    if (!selection.studentType || !selection.modality || !selection.year) {
      this.invalidate(); this.error.set('Seleccione tipo, modalidad y año.'); return;
    }
    this.listSubscription?.unsubscribe();
    this.listRevision++;
    const revision = this.listRevision;
    this.loading.set(true); this.error.set(''); this.selected.set(new Set());
    this.listSubscription = this.http.get<{ success: boolean; data: { rows: Row[]; total: number; canEnroll: boolean } }>(
      BASE, { params: selection })
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: response => {
          if (revision !== this.listRevision) return;
          this.loading.set(false); this.loaded.set(true);
          if (response.success !== true) { this.rows.set([]); this.error.set('No se pudo cargar el listado.'); return; }
          this.rows.set(response.data.rows); this.canEnroll.set(response.data.canEnroll);
        },
        error: () => {
          if (revision !== this.listRevision) return;
          this.loading.set(false); this.loaded.set(true); this.rows.set([]); this.canEnroll.set(false);
          this.error.set('No se pudo cargar el listado.');
        },
      });
  }

  protected toggle(row: Row): void {
    if (!this.canEnroll() || row.enrolled || this.busy()) return;
    const selected = new Set(this.selected());
    if (selected.has(row.studentCode)) selected.delete(row.studentCode); else selected.add(row.studentCode);
    this.selected.set(selected);
  }

  protected selectAll(checked: boolean): void {
    if (!this.canEnroll() || this.busy()) return;
    this.selected.set(new Set(checked ? this.rows().filter(row => !row.enrolled).map(row => row.studentCode) : []));
  }

  protected async enroll(): Promise<void> {
    if (!this.canEnroll() || this.busy() || this.selected().size === 0) return;
    const selection = { ...this.form.getRawValue() };
    const codes = [...this.selected()];
    const revision = this.listRevision;
    this.saving.set(true);
    this.form.disable({ emitEvent: false });
    try {
      const confirmation = await this.notifications.confirm('Confirmar inscripción',
        '¿Desea inscribir a los alumnos seleccionados en la encuesta de salida?', 'Sí, inscribir', 'Cancelar');
      if (!confirmation.isConfirmed || this.destroyRef.destroyed || revision !== this.listRevision || !this.canEnroll()) return;
      const response = await firstValueFrom(this.http.post<{ success: boolean; data: { enrolled: number; skipped: number } }>(
        BASE, { ...selection, studentCodes: codes, confirmed: true }));
      if (revision !== this.listRevision) return;
      if (response.success !== true) throw new Error('contract');
      this.selected.set(new Set());
      this.notice.set('Alumnos Inscritos: ' + response.data.enrolled
        + (response.data.skipped > 0 ? '. Ya inscritos: ' + response.data.skipped : ''));
      this.form.enable({ emitEvent: false });
      this.saving.set(false);
      this.loadRows();
    } catch {
      if (revision === this.listRevision) {
        this.selected.set(new Set());
        this.form.enable({ emitEvent: false });
        this.saving.set(false);
        this.loadRows();
        this.error.set('No se pudo confirmar la inscripción. Actualice el listado antes de reintentar.');
      }
    } finally {
      if (revision === this.listRevision) {
        this.form.enable({ emitEvent: false });
        this.saving.set(false);
      }
    }
  }

  protected status(row: Row): string {
    if (!row.enrolled) return row.progress ?? '';
    return !row.progress || row.progress.trim().toUpperCase() === 'NULL' ? 'Sin contestar' : row.progress;
  }

  protected photo(personCode: string): string {
    return personCode ? 'https://fotos.fen.uchile.cl/' + encodeURIComponent(personCode) + '.jpg' : DEFAULT_PHOTO;
  }

  protected photoFailed(event: Event): void {
    const image = event.target as HTMLImageElement;
    if (image.src !== DEFAULT_PHOTO) image.src = DEFAULT_PHOTO;
  }

  protected studentUrl(code: string): string | null {
    const base = this.navigation.links().find(link => link.key === 'student')?.url;
    if (!base || !code || code === '.' || code === '..') return null;
    const url = new URL(base);
    url.pathname = url.pathname.replace(/\/$/, '') + '/students/detail/' + encodeURIComponent(code);
    url.search = ''; url.hash = ''; return url.href;
  }

  private invalidate(): void {
    this.listRevision++;
    this.listSubscription?.unsubscribe();
    this.rows.set([]); this.selected.set(new Set()); this.loaded.set(false); this.notice.set('');
  }
}
