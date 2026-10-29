import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { Subscription } from 'rxjs';

interface Option { value: string; text: string; }
interface Filters { modalities: Option[]; years: string[]; }
const BASE = '/api/reports/postgraduate/enrolled-students';

@Component({
  selector: 'app-enrolled-students',
  imports: [ReactiveFormsModule, NgSelectModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="container-fluid px-0 py-4">
      <div class="page-header">
        <div class="d-flex flex-wrap justify-content-between align-items-start gap-3">
          <div>
            <h1 class="page-title"><i class="bi bi-people" aria-hidden="true"></i>Alumnos Matriculados MBA</h1>
            <p class="page-subtitle mb-0">Alumnos matriculados por año de ingreso y modalidad.</p>
          </div>
          <button type="button" class="btn btn-create" data-action="export"
            [disabled]="!ready() || exporting()" (click)="download()">
            <i class="bi bi-file-earmark-excel me-1" aria-hidden="true"></i>
            {{ exporting() ? 'Exportando…' : 'Exportar Excel' }}
          </button>
        </div>
      </div>
      <div class="page-header">
        <form [formGroup]="form" (ngSubmit)="download()">
          <div class="row g-3 align-items-end">
            @for (field of fields; track field.control) {
              <div class="col-xl-3 col-md-6">
                <div class="ng-select-container-custom">
                  <label class="floating-label" [for]="'enrolled-' + field.control">{{ field.label }}</label>
                  <ng-select [labelForId]="'enrolled-' + field.control" [formControlName]="field.control"
                    [attr.data-filter]="field.control" [items]="options()[field.control]"
                    bindLabel="text" bindValue="value" [clearable]="false" class="form-select ng-select-custom" />
                </div>
              </div>
            }
          </div>
        </form>
      </div>
      @if (filtersLoading()) { <p role="status">Cargando filtros…</p> }
      @if (error()) { <div class="alert alert-warning" role="alert">{{ error() }}</div> }
      @if (!ready() && error()) {
        <button type="button" class="btn btn-outline-secondary" (click)="loadFilters()">Reintentar filtros</button>
      }
      <p class="text-body-secondary">El informe se descarga en Excel; la grilla de datos personales no se muestra en pantalla.</p>
    </div>
  `,
})
export class EnrolledStudents {
  private readonly http = inject(HttpClient);
  private readonly destroyRef = inject(DestroyRef);
  readonly form = inject(FormBuilder).nonNullable.group({
    modality: ['', Validators.required],
    year: ['', Validators.required],
  });
  protected readonly fields = [
    { control: 'modality', label: 'Modalidad' },
    { control: 'year', label: 'Año' },
  ] as const;
  private readonly filters = signal<Filters>({ modalities: [], years: [] });
  protected readonly options = computed(() => {
    const option = (value: string): Option => ({ value, text: value });
    return { modality: this.filters().modalities, year: this.filters().years.map(option) };
  });
  protected readonly filtersLoading = signal(false);
  protected readonly exporting = signal(false);
  protected readonly ready = signal(false);
  protected readonly error = signal('');
  private downloadSubscription?: Subscription;

  constructor() {
    this.form.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => {
      this.error.set('');
    });
    this.destroyRef.onDestroy(() => this.downloadSubscription?.unsubscribe());
    this.loadFilters();
  }

  protected loadFilters(): void {
    this.filtersLoading.set(true);
    this.ready.set(false);
    this.error.set('');
    this.http.get<{ success: boolean; data: Filters }>(BASE + '/filters')
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: result => {
          this.filtersLoading.set(false);
          if (!result.success) { this.error.set('No se pudieron cargar los filtros.'); return; }
          const current = this.form.getRawValue();
          this.filters.set(result.data);
          this.form.patchValue({
            modality: result.data.modalities.some(row => row.value === current.modality)
              ? current.modality : result.data.modalities[0]?.value ?? '',
            year: result.data.years.includes(current.year)
              ? current.year : result.data.years[0] ?? '',
          }, { emitEvent: false });
          this.ready.set(!this.form.invalid);
        },
        error: () => {
          this.filtersLoading.set(false);
          this.error.set('No se pudieron cargar los filtros.');
        },
      });
  }

  protected download(): void {
    if (!this.ready() || this.form.invalid || this.exporting()) return;
    this.error.set('');
    this.exporting.set(true);
    const selection = { ...this.form.getRawValue() };
    this.downloadSubscription = this.http.get(BASE + '/download',
      { params: selection, responseType: 'blob' })
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: blob => {
          this.exporting.set(false);
          if (blob.size === 0) { this.error.set('Sin Datos'); return; }
          const url = URL.createObjectURL(blob);
          const link = document.createElement('a');
          try {
            link.href = url;
            link.download = 'AlumnosMatriculados.xlsx';
            document.body.appendChild(link);
            link.click();
          } finally {
            link.remove();
            URL.revokeObjectURL(url);
          }
        },
        error: (error: HttpErrorResponse) => {
          this.exporting.set(false);
          this.error.set(error.status === 400 ? 'Sin Datos' : 'No se pudo exportar el informe.');
        },
      });
  }
}
