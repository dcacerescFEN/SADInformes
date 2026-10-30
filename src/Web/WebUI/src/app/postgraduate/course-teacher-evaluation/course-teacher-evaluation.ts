import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';

interface Filters { years: string[]; types: string[]; }
const BASE = '/api/reports/postgraduate/course-teacher-evaluation';

@Component({
  selector: 'app-course-teacher-evaluation',
  imports: [ReactiveFormsModule, NgSelectModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="container-fluid px-0 py-4">
      <div class="page-header">
        <div class="d-flex flex-wrap justify-content-between align-items-start gap-3">
          <div>
            <h1 class="page-title"><i class="bi bi-clipboard-check" aria-hidden="true"></i>Evaluación Docente/Curso</h1>
            <p class="page-subtitle mb-0">Exportación de evaluación docente y curso por año y tipo.</p>
          </div>
          <button type="button" class="btn btn-create" data-action="export"
            [disabled]="!ready() || exporting()" (click)="download()">
            <i class="bi bi-file-earmark-excel me-1" aria-hidden="true"></i>
            {{ exporting() ? 'Exportando…' : 'Exportar Excel' }}
          </button>
        </div>
      </div>
      <div class="form-section">
        <form [formGroup]="form">
          <div class="row g-3 align-items-end">
            <div class="col-md-4">
              <div class="ng-select-container-custom">
                <label class="floating-label" for="evaluation-year">Año</label>
                <ng-select id="evaluation-year" formControlName="year" data-filter="year"
                  [items]="options().years" bindLabel="text" bindValue="value" [clearable]="false"
                  class="form-select ng-select-custom" />
              </div>
            </div>
            <div class="col-md-4">
              <div class="ng-select-container-custom">
                <label class="floating-label" for="evaluation-type">Tipo</label>
                <ng-select id="evaluation-type" formControlName="type" data-filter="type"
                  [items]="options().types" bindLabel="text" bindValue="value" [clearable]="false"
                  class="form-select ng-select-custom" />
              </div>
            </div>
          </div>
        </form>
      </div>
      @if (filtersLoading()) { <p role="status">Cargando filtros…</p> }
      @if (error()) { <div class="alert alert-warning" role="alert">{{ error() }}</div> }
      <p class="text-body-secondary">El informe se descarga en Excel; la grilla de respuestas no se muestra en pantalla.</p>
    </div>
  `,
})
export class CourseTeacherEvaluation {
  private readonly http = inject(HttpClient);
  private readonly destroyRef = inject(DestroyRef);
  readonly form = inject(FormBuilder).nonNullable.group({
    year: ['', Validators.required],
    type: ['EX', Validators.required],
  });
  private readonly filters = signal<Filters>({ years: [], types: ['EX', 'FT'] });
  protected readonly options = computed(() => ({
    years: this.filters().years.map(value => ({ value, text: value })),
    types: this.filters().types.map(value => ({ value, text: value === 'EX' ? 'Executive' : 'Full Time' })),
  }));
  protected readonly filtersLoading = signal(false);
  protected readonly exporting = signal(false);
  protected readonly ready = signal(false);
  protected readonly error = signal('');

  constructor() {
    this.destroyRef.onDestroy(() => this.exporting.set(false));
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
            year: result.data.years.includes(current.year) ? current.year : result.data.years[0] ?? '',
            type: result.data.types.includes(current.type) ? current.type : result.data.types[0] ?? 'EX',
          });
          this.ready.set(!this.form.invalid);
        },
        error: () => { this.filtersLoading.set(false); this.error.set('No se pudieron cargar los filtros.'); },
      });
  }

  protected download(): void {
    if (!this.ready() || this.exporting() || this.form.invalid) return;
    this.exporting.set(true);
    this.error.set('');
    this.http.get(BASE + '/download', { params: { ...this.form.getRawValue() }, responseType: 'blob' })
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: blob => {
          this.exporting.set(false);
          if (blob.size === 0) { this.error.set('Sin Datos'); return; }
          const url = URL.createObjectURL(blob);
          const link = document.createElement('a');
          try {
            link.href = url; link.download = 'Evaluacion_Docente_Curso.xlsx';
            document.body.appendChild(link); link.click();
          } finally { link.remove(); URL.revokeObjectURL(url); }
        },
        error: (error: HttpErrorResponse) => {
          this.exporting.set(false);
          this.error.set(error.status === 400 ? 'Sin Datos' : 'No se pudo exportar el informe.');
        },
      });
  }
}
