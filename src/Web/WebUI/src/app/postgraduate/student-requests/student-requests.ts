import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpClient } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { Subscription } from 'rxjs';

interface Option { value: string; text: string; }
interface Filters { studentTypes: Option[]; modalities: string[]; cohorts: string[]; states: string[]; }
const BASE = '/api/reports/postgraduate/student-requests';

@Component({
  selector: 'app-student-requests',
  imports: [ReactiveFormsModule, NgSelectModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="container-fluid px-0 py-4">
      <div class="page-header">
        <h1 class="page-title"><i class="bi bi-file-earmark-text" aria-hidden="true"></i>Solicitudes alumno</h1>
        <p class="page-subtitle mb-0">Informe de solicitudes de alumnos por programa, modalidad, ingreso y estado.</p>
      </div>
      <div class="page-header">
        <form [formGroup]="form" (ngSubmit)="download()">
          <div class="row g-3 align-items-end">
            @for (field of fields; track field.control) {
              <div class="col-xl-3 col-md-6">
                <div class="ng-select-container-custom">
                  <label class="floating-label" [for]="'student-requests-' + field.control">{{ field.label }}</label>
                  <ng-select [labelForId]="'student-requests-' + field.control" [formControlName]="field.control"
                    [attr.data-filter]="field.control" [items]="options()[field.control]" bindLabel="text" bindValue="value"
                    [clearable]="false" class="form-select ng-select-custom" />
                </div>
              </div>
            }
            <div class="col-auto">
              <button type="submit" class="btn btn-search" [disabled]="loading() || downloading() || !ready()">
                <i class="bi bi-file-earmark-excel me-1" aria-hidden="true"></i>{{ downloading() ? 'Descargando…' : 'Descargar Excel' }}
              </button>
              @if (error()) { <button type="button" class="btn btn-outline-secondary ms-2" (click)="ready() ? download() : loadFilters()">Reintentar</button> }
            </div>
          </div>
        </form>
      </div>
      @if (loading()) { <p role="status">Cargando filtros…</p> }
      @if (error()) { <div class="alert alert-warning" role="alert">{{ error() }}</div> }
    </div>
  `,
})
export class StudentRequests {
  private readonly http = inject(HttpClient);
  private readonly destroyRef = inject(DestroyRef);
  readonly form = inject(FormBuilder).nonNullable.group({ studentType: 'Todos', modality: '', cohort: 'Todos', state: 'Todos' });
  protected readonly fields = [
    { control: 'studentType', label: 'Tipo alumno' }, { control: 'modality', label: 'Modalidad' },
    { control: 'cohort', label: 'Año de ingreso' }, { control: 'state', label: 'Estado' },
  ] as const;
  private readonly filters = signal<Filters>({ studentTypes: [], modalities: [], cohorts: [], states: [] });
  protected readonly options = computed(() => {
    const f = this.filters();
    const option = (value: string): Option => ({ value, text: value });
    return { studentType: f.studentTypes, modality: [{ value: '', text: 'Todas' }, ...f.modalities.map(option)],
      cohort: f.cohorts.map(option), state: f.states.map(option) };
  });
  protected readonly loading = signal(false);
  protected readonly ready = signal(false);
  protected readonly error = signal('');
  private revision = 0;
  protected readonly downloading = signal(false);
  private downloadSubscription?: Subscription;

  constructor() {
    this.form.controls.studentType.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => {
      this.form.controls.modality.setValue('', { emitEvent: false });
      this.loadFilters();
    });
    this.form.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => this.cancelDownload());
    this.destroyRef.onDestroy(() => this.cancelDownload());
    this.loadFilters();
  }

  protected loadFilters(): void {
    const revision = ++this.revision;
    this.loading.set(true); this.ready.set(false); this.error.set('');
    this.http.get<{ success: boolean; data: Filters }>(BASE + '/filters', {
      params: { studentType: this.form.controls.studentType.value },
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: result => {
        if (revision !== this.revision) return;
        this.loading.set(false);
        if (!result.success) { this.error.set('No se pudieron cargar los filtros.'); return; }
        this.filters.set(result.data); this.ready.set(true);
      },
      error: () => {
        if (revision !== this.revision) return;
        this.loading.set(false); this.error.set('No se pudieron cargar los filtros.');
      },
    });
  }

  protected download(): void {
    if (!this.ready() || this.loading() || this.downloading()) return;
    const snapshot = { ...this.form.getRawValue() };
    this.downloading.set(true);
    this.error.set('');
    // The server selects Excel by default; no client report name or arbitrary format.
    this.downloadSubscription = this.http.get(BASE, { params: snapshot, responseType: 'blob' })
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: blob => {
          this.downloading.set(false);
          if (blob.size === 0) { this.error.set('No se pudo descargar el informe.'); return; }
          const url = URL.createObjectURL(blob);
          const link = document.createElement('a');
          try {
            link.href = url;
            link.download = 'SolicitudesAlumnoPostgrado.xlsx';
            document.body.appendChild(link);
            link.click();
          } finally {
            link.remove();
            URL.revokeObjectURL(url);
          }
        },
        error: () => {
          this.downloading.set(false);
          this.error.set('No se pudo descargar el informe.');
        },
      });
  }

  private cancelDownload(): void {
    this.downloadSubscription?.unsubscribe();
    this.downloadSubscription = undefined;
    this.downloading.set(false);
  }
}
