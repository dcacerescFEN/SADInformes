import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpClient } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { Subscription } from 'rxjs';
import { NavigationService } from '../../core/navigation/navigation-service';

interface Option { value: string; text: string; }
interface Filters { studentTypes: Option[]; modalities: string[]; years: string[]; }
interface Selection { studentType: string; modality: string; cohort: string; year: string; }
interface Row {
  position: number; name: string; rut: string; studentCode: string; entrySemester: string;
  situation: string; weightedAverage: string; percentile: string;
}
const BASE = '/api/reports/postgraduate/weighted-ranking';
const DEFAULT_PHOTO = 'https://fotos.fen.uchile.cl/perfilNN.jpg';

@Component({
  selector: 'app-weighted-ranking',
  imports: [ReactiveFormsModule, NgSelectModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="container-fluid px-0 py-4">
      <div class="page-header">
        <div class="d-flex flex-wrap justify-content-between align-items-start gap-3">
          <div>
            <h1 class="page-title"><i class="bi bi-award" aria-hidden="true"></i>Ranking Notas Ponderado</h1>
            <p class="page-subtitle mb-0">Ranking de alumnos graduados según sus notas ponderadas.</p>
          </div>
          <div class="d-flex gap-2">
            <button type="button" class="btn btn-create" data-action="export"
              [disabled]="rows().length === 0 || exporting()" (click)="download()">
              <i class="bi bi-file-earmark-excel me-1" aria-hidden="true"></i>{{ exporting() ? 'Descargando…' : 'Descargar Excel' }}
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
                  <label class="floating-label" [for]="'weighted-' + field.control">{{ field.label }}</label>
                  <ng-select [labelForId]="'weighted-' + field.control" [formControlName]="field.control"
                    [attr.data-filter]="field.control" [items]="options()[field.control]"
                    bindLabel="text" bindValue="value" [clearable]="false" class="form-select ng-select-custom" />
                </div>
              </div>
            }
            <div class="col-auto">
              <button type="submit" class="btn btn-search"
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
      @if (loading()) { <p role="status">Cargando ranking…</p> }
      @if (error()) { <div class="alert alert-warning" role="alert">{{ error() }}</div> }
      @if (notice()) { <p class="text-body-secondary" role="status">{{ notice() }}</p> }
      @if (rows().length > 0) {
        <div class="form-section">
          <div class="table-responsive">
            <table class="table table-hover align-middle app-table" aria-label="Ranking notas ponderado">
              <thead class="table-light">
                <tr>
                  <th>Posición</th><th>Foto</th><th>Nombre</th><th>Rut</th><th>Código Alumno</th>
                  <th>Semestre de Ingreso</th><th>Situación</th><th>Promedio Ponderado</th><th>Percentil</th>
                </tr>
              </thead>
              <tbody>
                @for (row of rows(); track row.position + '-' + row.studentCode) {
                  <tr>
                    <td>{{ row.position }}</td>
                    <td>
                      <a [href]="photoUrl(row.rut)" target="_blank" rel="noopener noreferrer"
                        [attr.aria-label]="'Ver foto de ' + row.name">
                        <img [src]="photoUrl(row.rut)" [alt]="'Foto de ' + row.name" width="35" height="47"
                          style="object-fit:cover" loading="lazy" (error)="photoFallback($event)" />
                      </a>
                    </td>
                    <td class="fw-semibold">{{ row.name }}</td>
                    <td>{{ row.rut }}</td>
                    <td>
                      @if (studentUrl(row.studentCode); as url) {
                        <a class="student-file-link" [href]="url" target="_blank" rel="noopener noreferrer">{{ row.studentCode }}</a>
                      } @else { {{ row.studentCode }} }
                    </td>
                    <td>{{ row.entrySemester }}</td>
                    <td>{{ row.situation }}</td>
                    <td>{{ row.weightedAverage }}</td>
                    <td>{{ row.percentile }}</td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
          <p class="fw-semibold mt-3 mb-0">Total: {{ rows().length }}</p>
        </div>
      }
    </div>
  `,
})
export class WeightedRanking {
  private readonly http = inject(HttpClient);
  private readonly navigation = inject(NavigationService);
  private readonly destroyRef = inject(DestroyRef);
  readonly form = inject(FormBuilder).nonNullable.group({
    studentType: ['', Validators.required], modality: ['', Validators.required],
    cohort: ['', Validators.required], year: ['', Validators.required],
  });
  protected readonly fields = [
    { control: 'studentType', label: 'Tipo Alumno' }, { control: 'modality', label: 'Modalidad' },
    { control: 'cohort', label: 'Año Ingreso' }, { control: 'year', label: 'Año del Cuadro de Honor' },
  ] as const;
  private readonly filters = signal<Filters>({ studentTypes: [], modalities: [], years: [] });
  protected readonly options = computed(() => {
    const f = this.filters();
    const option = (value: string): Option => ({ value, text: value });
    return {
      studentType: f.studentTypes, modality: f.modalities.map(option),
      cohort: f.years.map(option), year: f.years.map(option),
    };
  });
  protected readonly rows = signal<Row[]>([]);
  protected readonly filtersLoading = signal(false);
  protected readonly loading = signal(false);
  protected readonly exporting = signal(false);
  protected readonly ready = signal(false);
  protected readonly error = signal('');
  protected readonly notice = signal('');
  private filterRevision = 0;
  private reportRevision = 0;
  private reportSubscription?: Subscription;
  private exportSubscription?: Subscription;
  private shownSelection?: Selection;

  constructor() {
    this.form.controls.studentType.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => {
      this.form.controls.modality.setValue('', { emitEvent: false });
      this.loadFilters();
    });
    this.form.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => this.invalidate());
    this.destroyRef.onDestroy(() => this.invalidate());
    this.loadFilters();
  }

  protected loadFilters(): void {
    const revision = ++this.filterRevision;
    const selection = this.form.getRawValue();
    this.invalidate();
    this.filtersLoading.set(true);
    this.ready.set(false);
    const params: Record<string, string> = selection.studentType
      ? { studentType: selection.studentType } : {};
    this.http.get<{ success: boolean; data: Filters }>(BASE + '/filters', { params })
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: result => {
          if (revision !== this.filterRevision) return;
          this.filtersLoading.set(false);
          if (!result.success) { this.error.set('No se pudieron cargar los filtros.'); return; }
          const f = result.data;
          const current = this.form.getRawValue();
          this.filters.set(f);
          this.form.patchValue({
            studentType: f.studentTypes.some(row => row.value === current.studentType)
              ? current.studentType : f.studentTypes[0]?.value ?? '',
            modality: f.modalities.includes(current.modality) ? current.modality : f.modalities[0] ?? '',
            cohort: f.years.includes(current.cohort) ? current.cohort : f.years[0] ?? '',
            year: f.years.includes(current.year) ? current.year : f.years[0] ?? '',
          }, { emitEvent: false });
          this.ready.set(!this.form.invalid);
        },
        error: () => {
          if (revision !== this.filterRevision) return;
          this.filtersLoading.set(false);
          this.error.set('No se pudieron cargar los filtros.');
        },
      });
  }

  protected show(): void {
    if (!this.ready() || this.filtersLoading() || this.loading() || this.form.invalid) return;
    this.invalidate();
    const revision = this.reportRevision;
    const snapshot = { ...this.form.getRawValue() };
    this.loading.set(true);
    this.reportSubscription = this.http.get<{ success: boolean; data: { rows: Row[]; total: number } }>(
      BASE, { params: snapshot }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: result => {
          if (revision !== this.reportRevision) return;
          this.loading.set(false);
          if (!result.success) { this.error.set('No se pudo cargar el ranking.'); return; }
          this.rows.set(result.data.rows);
          this.shownSelection = snapshot;
          if (result.data.rows.length === 0) this.notice.set('Sin Datos');
        },
        error: () => {
          if (revision !== this.reportRevision) return;
          this.loading.set(false);
          this.error.set('No se pudo cargar el ranking.');
        },
      });
  }

  protected download(): void {
    if (!this.shownSelection || this.rows().length === 0 || this.exporting()) return;
    const revision = this.reportRevision;
    this.exporting.set(true);
    this.error.set('');
    this.exportSubscription = this.http.get(BASE + '/download',
      { params: { ...this.shownSelection }, responseType: 'blob' })
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: blob => {
          if (revision !== this.reportRevision) return;
          this.exporting.set(false);
          if (blob.size === 0) { this.error.set('No se pudo descargar el ranking.'); return; }
          const url = URL.createObjectURL(blob);
          const link = document.createElement('a');
          try {
            link.href = url; link.download = 'Ranking_Programas_Graduados.xlsx';
            document.body.appendChild(link); link.click();
          } finally { link.remove(); URL.revokeObjectURL(url); }
        },
        error: () => {
          if (revision !== this.reportRevision) return;
          this.exporting.set(false);
          this.error.set('No se pudo descargar el ranking.');
        },
      });
  }

  protected photoUrl(rut: string): string {
    return rut ? 'https://fotos.fen.uchile.cl/' + encodeURIComponent(rut) + '.jpg' : DEFAULT_PHOTO;
  }

  protected photoFallback(event: Event): void {
    const image = event.target as HTMLImageElement;
    if (image.dataset['fallback']) return;
    image.dataset['fallback'] = 'true';
    image.src = DEFAULT_PHOTO;
  }

  protected studentUrl(code: string): string | null {
    const base = this.navigation.links().find(link => link.key === 'student')?.url;
    if (!base || !code || code === '.' || code === '..') return null;
    const url = new URL(base);
    url.pathname = url.pathname.replace(/\/$/, '') + '/students/detail/' + encodeURIComponent(code);
    url.search = '';
    url.hash = '';
    return url.href;
  }

  private invalidate(): void {
    ++this.reportRevision;
    this.reportSubscription?.unsubscribe();
    this.exportSubscription?.unsubscribe();
    this.rows.set([]);
    this.shownSelection = undefined;
    this.loading.set(false);
    this.exporting.set(false);
    this.error.set('');
    this.notice.set('');
  }
}
