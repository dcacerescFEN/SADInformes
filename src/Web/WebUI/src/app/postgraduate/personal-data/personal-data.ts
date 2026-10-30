import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { Subscription } from 'rxjs';

interface Option { value: string; text: string; }
interface Filters { studentTypes: Option[]; modalities: string[]; years: string[]; }
const BASE = '/api/reports/postgraduate/personal-data';

@Component({
  selector: 'app-personal-data',
  imports: [ReactiveFormsModule, NgSelectModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="container-fluid px-0 py-4">
      <div class="page-header">
        <div class="d-flex flex-wrap justify-content-between align-items-start gap-3">
          <div>
            <h1 class="page-title"><i class="bi bi-person-vcard" aria-hidden="true"></i>Datos Personales</h1>
            <p class="page-subtitle mb-0">Datos personales de alumnos de Postgrado por tipo, modalidad e ingreso.</p>
          </div>
          <button type="button" class="btn btn-create" data-action="export"
            [disabled]="!ready() || exporting()" (click)="download()">
            <i class="bi bi-file-earmark-excel me-1" aria-hidden="true"></i>
            {{ exporting() ? 'Exportando…' : 'Exportar Excel' }}
          </button>
        </div>
      </div>
      <div class="page-header">
        <form [formGroup]="form">
          <div class="row g-3 align-items-end">
            <div class="col-md-4">
              <div class="ng-select-container-custom">
                <label class="floating-label" for="personal-data-type">Tipo Alumno</label>
                <ng-select id="personal-data-type" formControlName="studentType" data-filter="studentType"
                  [items]="options().studentType" bindLabel="text" bindValue="value" [clearable]="false"
                  class="form-select ng-select-custom" />
              </div>
            </div>
            <div class="col-md-4">
              <div class="ng-select-container-custom">
                <label class="floating-label" for="personal-data-modality">Modalidad</label>
                <ng-select id="personal-data-modality" formControlName="modality" data-filter="modality"
                  [items]="options().modality" bindLabel="text" bindValue="value" [clearable]="false"
                  class="form-select ng-select-custom" />
              </div>
            </div>
            <div class="col-md-4">
              <div class="ng-select-container-custom">
                <label class="floating-label" for="personal-data-year">Año Ingreso</label>
                <ng-select id="personal-data-year" formControlName="year" data-filter="year"
                  [items]="options().year" bindLabel="text" bindValue="value" [clearable]="false"
                  class="form-select ng-select-custom" />
              </div>
            </div>
          </div>
        </form>
      </div>
      @if (filtersLoading()) { <p role="status">Cargando filtros…</p> }
      @if (error()) { <div class="alert alert-warning" role="alert">{{ error() }}</div> }
      @if (!ready() && error()) {
        <button type="button" class="btn btn-outline-secondary" (click)="loadFilters()">Reintentar filtros</button>
      }
      <p class="text-body-secondary">El informe se descarga en Excel; los datos personales no se muestran en pantalla.</p>
    </div>
  `,
})
export class PersonalData {
  private readonly http = inject(HttpClient);
  private readonly destroyRef = inject(DestroyRef);
  readonly form = inject(FormBuilder).nonNullable.group({
    studentType: ['', Validators.required],
    modality: ['', Validators.required],
    year: ['', Validators.required],
  });
  private readonly filters = signal<Filters>({ studentTypes: [], modalities: [], years: [] });
  protected readonly options = computed(() => {
    const option = (value: string): Option => ({ value, text: value });
    return {
      studentType: this.filters().studentTypes,
      modality: this.filters().modalities.map(option),
      year: this.filters().years.map(option),
    };
  });
  protected readonly filtersLoading = signal(false);
  protected readonly exporting = signal(false);
  protected readonly ready = signal(false);
  protected readonly error = signal('');
  private downloadSubscription?: Subscription;

  constructor() {
    this.form.controls.studentType.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => {
      this.form.controls.modality.setValue('Todos', { emitEvent: false });
      this.loadFilters();
    });
    this.form.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => this.error.set(''));
    this.destroyRef.onDestroy(() => this.downloadSubscription?.unsubscribe());
    this.loadFilters();
  }

  protected loadFilters(): void {
    this.filtersLoading.set(true);
    this.ready.set(false);
    this.error.set('');
    const type = this.form.controls.studentType.value;
    this.http.get<{ success: boolean; data: Filters }>(BASE + '/filters',
      { params: type ? { studentType: type } : {} })
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: result => {
          this.filtersLoading.set(false);
          if (!result.success) { this.error.set('No se pudieron cargar los filtros.'); return; }
          const current = this.form.getRawValue();
          this.filters.set(result.data);
          this.form.patchValue({
            studentType: result.data.studentTypes.some(row => row.value === current.studentType)
              ? current.studentType : result.data.studentTypes[0]?.value ?? '',
            modality: result.data.modalities.includes(current.modality)
              ? current.modality : result.data.modalities[0] ?? 'Todos',
            year: result.data.years.includes(current.year)
              ? current.year : result.data.years[0] ?? 'Todos',
          }, { emitEvent: false });
          this.ready.set(!this.form.invalid);
        },
        error: () => { this.filtersLoading.set(false); this.error.set('No se pudieron cargar los filtros.'); },
      });
  }

  protected download(): void {
    if (!this.ready() || this.form.invalid || this.exporting()) return;
    this.exporting.set(true);
    this.error.set('');
    this.downloadSubscription = this.http.get(BASE + '/download',
      { params: { ...this.form.getRawValue() }, responseType: 'blob' })
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: blob => {
          this.exporting.set(false);
          if (blob.size === 0) { this.error.set('Sin Datos'); return; }
          const url = URL.createObjectURL(blob);
          const link = document.createElement('a');
          try {
            link.href = url;
            link.download = 'Datos_Personales_Postgrado.xlsx';
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
