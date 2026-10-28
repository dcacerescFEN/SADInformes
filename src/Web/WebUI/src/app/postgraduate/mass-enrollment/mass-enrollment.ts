import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpClient, HttpParams } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { firstValueFrom } from 'rxjs';
import { NotificationService } from '../../core/services/notification-service';

interface Option { value: string; text: string; }
interface Selection { studentType: string; modality: string; cohort: string; period: string; course: string; }
interface Filters { studentTypes: Option[]; modalities: string[]; cohorts: string[]; periods: Option[]; courses: Option[]; }
interface Row {
  studentCode: string; name: string; rut: string; entrySemester: string; entryType: string;
  situation: string; reason: string; decree: string; enrolled: boolean;
}
interface Envelope<T> { success: boolean; data: T; }
const BASE = '/api/reports/postgraduate/mass-enrollment';
const EMPTY: Filters = { studentTypes: [], modalities: [], cohorts: [], periods: [], courses: [] };
const DEFAULT_PHOTO = 'https://fotos.fen.uchile.cl/perfilNN.jpg';

@Component({
  selector: 'app-mass-enrollment',
  imports: [ReactiveFormsModule, NgSelectModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="container-fluid px-0 py-4">
      <div class="page-header">
        <div class="d-flex flex-wrap justify-content-between align-items-start gap-3">
          <div>
            <h1 class="page-title"><i class="bi bi-person-check" aria-hidden="true"></i> Inscripción masiva</h1>
            <p class="page-subtitle mb-0">Inscripción administrativa por tipo de alumno, modalidad, cohorte, período y curso.</p>
          </div>
          <button class="btn btn-create" type="button" data-action="enroll"
            [disabled]="selected().size === 0 || busy()" (click)="enroll()">
            <i class="bi bi-person-plus me-2" aria-hidden="true"></i>Inscribir
          </button>
        </div>
      </div>
      <div class="page-header">
        <form [formGroup]="form" (ngSubmit)="show()">
          <div class="row g-3 align-items-end">
            <div class="col-xl-3 col-lg-4 col-md-6">
              <div class="ng-select-container-custom">
                <label class="floating-label" for="massType">Tipo alumno</label>
                <ng-select inputId="massType" formControlName="studentType" [items]="filters().studentTypes"
                  bindLabel="text" bindValue="value" [clearable]="false" class="form-select ng-select-custom"
                  placeholder="Seleccione tipo" />
              </div>
            </div>
            <div class="col-xl-3 col-lg-4 col-md-6">
              <div class="ng-select-container-custom">
                <label class="floating-label" for="massModality">Modalidad</label>
                <ng-select inputId="massModality" formControlName="modality" [items]="filters().modalities"
                  [clearable]="false" class="form-select ng-select-custom" placeholder="Seleccione modalidad" />
              </div>
            </div>
            <div class="col-xl-3 col-lg-4 col-md-6">
              <div class="ng-select-container-custom">
                <label class="floating-label" for="massCohort">Cohorte alumnos</label>
                <ng-select inputId="massCohort" formControlName="cohort" [items]="filters().cohorts"
                  [clearable]="false" class="form-select ng-select-custom" placeholder="Seleccione cohorte" />
              </div>
            </div>
            <div class="col-xl-3 col-lg-4 col-md-6">
              <div class="ng-select-container-custom">
                <label class="floating-label" for="massPeriod">Período</label>
                <ng-select inputId="massPeriod" formControlName="period" [items]="filters().periods"
                  bindLabel="text" bindValue="value" [clearable]="false" class="form-select ng-select-custom"
                  placeholder="Seleccione período" />
              </div>
            </div>
            <div class="col-xl-9 col-lg-8">
              <div class="ng-select-container-custom">
                <label class="floating-label" for="massCourse">Curso / sección</label>
                <ng-select inputId="massCourse" formControlName="course" [items]="filters().courses"
                  bindLabel="text" bindValue="value" [clearable]="false" class="form-select ng-select-custom"
                  placeholder="Seleccione curso" />
              </div>
            </div>
            <div class="col-xl-auto col-lg-4">
              <button class="btn btn-search" type="submit" data-action="show" [disabled]="busy()">
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
            <table class="table table-hover align-middle app-table" aria-label="Alumnos para inscripción masiva">
              <thead class="table-light"><tr>
                <th>Foto</th><th>Nombre</th><th>Código Alumno</th><th>Rut</th><th>Semestre de ingreso</th>
                <th>Tipo ingreso</th><th>Situación</th><th>Motivo</th><th>Inscrito</th>
                <th><label><input type="checkbox" class="form-check-input me-1" data-action="select-all"
                  [checked]="allSelected()" [disabled]="busy() || eligibleCount() === 0"
                  (change)="selectAll($any($event.target).checked)" />Inscribir</label></th><th>Decreto</th>
              </tr></thead>
              <tbody>
                @for (row of rows(); track row.studentCode) {
                  <tr>
                    <td><img [src]="photo(row.rut)" (error)="photoFailed($event)" alt="Foto alumno"
                      width="35" height="47" loading="lazy" /></td>
                    <td>{{ row.name }}</td><td>{{ row.studentCode }}</td><td>{{ row.rut }}</td>
                    <td>{{ row.entrySemester }}</td><td>{{ row.entryType }}</td><td>{{ row.situation }}</td>
                    <td>{{ row.reason }}</td><td>{{ row.enrolled ? 'SI' : 'NO' }}</td>
                    <td><input type="checkbox" class="form-check-input"
                      [attr.aria-label]="'Inscribir ' + row.studentCode" [disabled]="row.enrolled || busy()"
                      [checked]="selected().has(row.studentCode)" (change)="toggle(row)" /></td>
                    <td>{{ row.decree }}</td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
          <p class="fw-semibold mt-3 mb-0">Total: {{ rows().length }} · Seleccionados: {{ selected().size }}</p>
        </div>
      }
    </div>
  `,
})
export class MassEnrollment {
  private readonly http = inject(HttpClient);
  private readonly notifications = inject(NotificationService);
  private readonly destroyRef = inject(DestroyRef);
  readonly form = inject(FormBuilder).nonNullable.group({
    studentType: '', modality: '', cohort: '', period: '', course: '',
  });
  protected readonly filters = signal<Filters>(EMPTY);
  protected readonly rows = signal<Row[]>([]);
  protected readonly selected = signal<Set<string>>(new Set());
  protected readonly error = signal('');
  protected readonly notice = signal('');
  protected readonly saving = signal(false);
  private readonly filtering = signal(false);
  private readonly loading = signal(false);
  protected readonly busy = computed(() => this.saving() || this.filtering() || this.loading());
  protected readonly eligibleCount = computed(() => this.rows().filter(row => !row.enrolled).length);
  protected readonly allSelected = computed(() => this.eligibleCount() > 0 && this.selected().size === this.eligibleCount());
  private filterRevision = 0;
  private candidateRevision = 0;
  private loadedSelection: Selection | null = null;

  constructor() {
    for (const name of ['studentType', 'modality', 'period'] as const) {
      this.form.controls[name].valueChanges.pipe(takeUntilDestroyed()).subscribe(() => {
        if (name === 'studentType') this.form.patchValue({ modality: '', period: '', course: '' }, { emitEvent: false });
        else this.form.controls.course.setValue('', { emitEvent: false });
        void this.loadFilters();
      });
    }
    for (const name of ['cohort', 'course'] as const) {
      this.form.controls[name].valueChanges.pipe(takeUntilDestroyed()).subscribe(() => this.invalidate());
    }
    void this.loadFilters();
  }

  private invalidate(preserveNotice = false): void {
    this.candidateRevision++;
    this.loadedSelection = null;
    this.rows.set([]);
    this.selected.set(new Set());
    if (!preserveNotice) this.notice.set('');
  }

  private params(selection: Selection): HttpParams {
    return new HttpParams({ fromObject: selection as unknown as Record<string, string> });
  }

  private async loadFilters(preserveNotice = false, preserveCourse = false): Promise<boolean> {
    const revision = ++this.filterRevision;
    this.invalidate(preserveNotice);
    this.filtering.set(true);
    this.error.set('');
    try {
      const response = await firstValueFrom(this.http.get<Envelope<Filters>>(BASE + '/filters',
        { params: this.params(this.form.getRawValue()) }));
      if (revision !== this.filterRevision) return false;
      if (response.success !== true) throw new Error('contract');
      const filters = response.data;
      this.filters.set(filters);
      const current = this.form.getRawValue();
      const option = (value: string, options: Option[]) => options.some(item => item.value === value) ? value : options[0]?.value ?? '';
      const text = (value: string, options: string[]) => options.includes(value) ? value : options[0] ?? '';
      this.form.setValue({
        studentType: option(current.studentType, filters.studentTypes), modality: text(current.modality, filters.modalities),
        cohort: text(current.cohort, filters.cohorts), period: option(current.period, filters.periods),
        course: preserveCourse && !filters.courses.some(item => item.value === current.course)
          ? '' : option(current.course, filters.courses),
      }, { emitEvent: false });
      return true;
    } catch {
      if (revision === this.filterRevision) {
        this.filters.set(EMPTY);
        if (preserveCourse) this.form.controls.course.setValue('', { emitEvent: false });
        this.error.set(preserveNotice
          ? 'La inscripción fue confirmada, pero no se pudieron actualizar los filtros. Actualice antes de consultar.'
          : 'No se pudieron cargar los filtros de inscripción.');
      }
      return false;
    } finally { if (revision === this.filterRevision) this.filtering.set(false); }
  }

  protected async show(): Promise<void> {
    if (this.busy()) return;
    this.notice.set('');
    if (!this.form.controls.course.value && !await this.loadFilters()) return;
    await this.loadRows();
  }

  private async loadRows(): Promise<void> {
    const selection = this.form.getRawValue();
    this.selected.set(new Set());
    this.loadedSelection = null;
    this.error.set('');
    if (Object.values(selection).some(value => !value)) {
      this.rows.set([]);
      this.error.set('Seleccione tipo, modalidad, cohorte, período y curso.');
      return;
    }
    const revision = ++this.candidateRevision;
    this.loading.set(true);
    try {
      const response = await firstValueFrom(this.http.get<Envelope<{ rows: Row[]; total: number }>>(BASE,
        { params: this.params(selection) }));
      if (revision !== this.candidateRevision) return;
      if (response.success !== true) throw new Error('contract');
      this.rows.set(response.data.rows);
      this.loadedSelection = selection;
      if (response.data.rows.length === 0) this.error.set('Sin datos para los filtros seleccionados.');
    } catch {
      if (revision === this.candidateRevision) {
        this.rows.set([]);
        this.error.set('No se pudo cargar el listado de alumnos.');
      }
    } finally { this.loading.set(false); }
  }

  protected toggle(row: Row): void {
    if (row.enrolled || this.busy()) return;
    const selected = new Set(this.selected());
    if (selected.has(row.studentCode)) selected.delete(row.studentCode); else selected.add(row.studentCode);
    this.selected.set(selected);
  }

  protected selectAll(checked: boolean): void {
    if (this.busy()) return;
    this.selected.set(new Set(checked ? this.rows().filter(row => !row.enrolled).map(row => row.studentCode) : []));
  }

  protected async enroll(): Promise<void> {
    if (this.busy() || !this.loadedSelection || this.selected().size === 0) return;
    const selection = this.loadedSelection;
    const studentCodes = [...this.selected()];
    const revision = this.candidateRevision;
    // Freeze the consent target before awaiting the asynchronous dialog.
    this.saving.set(true);
    this.form.disable({ emitEvent: false });
    try {
      const confirmation = await this.notifications.confirm('Confirmar inscripción',
        'Desea inscribir a los alumnos seleccionados?', 'Sí, inscribir', 'Cancelar');
      if (!confirmation.isConfirmed || this.destroyRef.destroyed || revision !== this.candidateRevision) return;
      this.error.set('');
      this.notice.set('');
      const response = await firstValueFrom(this.http.post<Envelope<{ enrolled: number }>>(BASE, {
        selection, studentCodes, confirmed: true,
      }));
      if (response.success !== true) throw new Error('contract');
      const success = 'Alumnos inscritos: ' + response.data.enrolled;
      this.notice.set(success);
      // A confirmed enrollment can consume the last vacancy and remove the course.
      // Refresh catalogues first; never query stale filters or switch course silently.
      if (await this.loadFilters(true, true)) {
        const current = this.form.getRawValue();
        if (Object.keys(selection).every(key => selection[key as keyof Selection] === current[key as keyof Selection]))
          await this.loadRows();
        else {
          this.form.controls.course.setValue('', { emitEvent: false });
          this.notice.set(success + '. El curso ya no está disponible para inscripción. Actualice los filtros para continuar.');
        }
      }
    } catch {
      this.invalidate();
      this.error.set('No se pudo confirmar la inscripción. Actualice el listado antes de reintentar.');
    } finally {
      this.saving.set(false);
      this.form.enable({ emitEvent: false });
    }
  }

  protected photo(rut: string): string {
    return rut ? 'https://fotos.fen.uchile.cl/' + encodeURIComponent(rut) + '.jpg' : DEFAULT_PHOTO;
  }

  protected photoFailed(event: Event): void {
    const image = event.target as HTMLImageElement;
    if (image.src !== DEFAULT_PHOTO) image.src = DEFAULT_PHOTO;
  }
}
