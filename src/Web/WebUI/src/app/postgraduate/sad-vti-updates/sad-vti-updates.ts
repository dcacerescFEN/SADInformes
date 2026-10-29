import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { Subscription } from 'rxjs';

interface StateOption { value: string; text: string; }
const BASE = '/api/reports/postgraduate/sad-vti-updates';
const EMPTY = 'No hay registros para exportar.';

@Component({
  selector: 'app-sad-vti-updates',
  imports: [ReactiveFormsModule, NgSelectModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="container-fluid px-0 py-4">
      <div class="page-header">
        <h1 class="page-title"><i class="bi bi-arrow-repeat" aria-hidden="true"></i>Actualización datos SAD–VTI</h1>
        <p class="page-subtitle mb-0">Auditoría de actualización de datos de alumnos por fecha y estado.</p>
      </div>
      <div class="page-header">
        <form [formGroup]="form" (ngSubmit)="download()">
          <div class="row g-3 align-items-end">
            <div class="col-lg-4 col-md-6">
              <div class="ng-select-container-custom">
                <label class="floating-label" for="sad-vti-state">Estado</label>
                <ng-select labelForId="sad-vti-state" formControlName="state" data-filter="state"
                  [items]="states()" bindLabel="text" bindValue="value" [clearable]="false"
                  class="form-select ng-select-custom" />
              </div>
            </div>
            <div class="col-lg-3 col-md-6">
              <label class="form-label" for="sad-vti-from">Desde</label>
              <input id="sad-vti-from" type="date" class="form-control" formControlName="from" required />
            </div>
            <div class="col-lg-3 col-md-6">
              <label class="form-label" for="sad-vti-to">Hasta</label>
              <input id="sad-vti-to" type="date" class="form-control" formControlName="to" required />
            </div>
            <div class="col-auto">
              <button type="submit" class="btn btn-search" [disabled]="loading() || downloading() || !ready() || form.invalid">
                <i class="bi bi-file-earmark-excel me-1" aria-hidden="true"></i>{{ downloading() ? 'Descargando…' : 'Descargar Excel' }}
              </button>
              @if (error()) {
                <button type="button" class="btn btn-outline-secondary ms-2" (click)="ready() ? download() : loadStates()">Reintentar</button>
              }
            </div>
          </div>
          <p class="text-body-secondary mt-3 mb-0">Las fechas incluyen ambos días completos.</p>
        </form>
      </div>
      @if (loading()) { <p role="status">Cargando estados…</p> }
      @if (notice()) { <div class="alert alert-info" role="status">{{ notice() }}</div> }
      @if (error()) { <div class="alert alert-warning" role="alert">{{ error() }}</div> }
    </div>
  `,
})
export class SadVtiUpdates {
  private readonly http = inject(HttpClient);
  private readonly destroyRef = inject(DestroyRef);
  readonly form = inject(FormBuilder).nonNullable.group({
    state: '-1', from: ['', Validators.required], to: ['', Validators.required],
  });
  protected readonly states = signal<StateOption[]>([]);
  protected readonly loading = signal(false);
  protected readonly ready = signal(false);
  protected readonly downloading = signal(false);
  protected readonly error = signal('');
  protected readonly notice = signal('');
  private stateRevision = 0;
  private downloadRevision = 0;
  private downloadSubscription?: Subscription;

  constructor() {
    this.form.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => {
      this.cancelDownload(); this.error.set(''); this.notice.set('');
    });
    this.destroyRef.onDestroy(() => this.cancelDownload());
    this.loadStates();
  }

  protected loadStates(): void {
    const revision = ++this.stateRevision;
    this.loading.set(true); this.ready.set(false); this.error.set('');
    this.http.get<{ success: boolean; data: { states: StateOption[] } }>(BASE + '/filters')
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: result => {
          if (revision !== this.stateRevision) return;
          this.loading.set(false);
          if (!result.success) { this.error.set('No se pudieron cargar los estados.'); return; }
          this.states.set(result.data.states); this.ready.set(true);
        },
        error: () => {
          if (revision !== this.stateRevision) return;
          this.loading.set(false); this.error.set('No se pudieron cargar los estados.');
        },
      });
  }

  protected download(): void {
    if (!this.ready() || this.loading() || this.downloading() || this.form.invalid) return;
    const snapshot = { ...this.form.getRawValue() };
    if (snapshot.from > snapshot.to) {
      this.error.set('La fecha desde no puede ser posterior a la fecha hasta.'); return;
    }
    const revision = ++this.downloadRevision;
    this.downloading.set(true); this.error.set(''); this.notice.set('');
    this.downloadSubscription = this.http.get(BASE + '/download', { params: snapshot, responseType: 'blob' })
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: blob => {
          if (revision !== this.downloadRevision) return;
          this.downloading.set(false);
          if (blob.size === 0) { this.notice.set(EMPTY); return; }
          const url = URL.createObjectURL(blob);
          const link = document.createElement('a');
          try {
            link.href = url; link.download = 'AudiPostgradoAlumnosGuia.xlsx';
            document.body.appendChild(link); link.click();
          } finally { link.remove(); URL.revokeObjectURL(url); }
        },
        error: (error: HttpErrorResponse) => { void this.handleError(error, revision); },
      });
  }

  private async handleError(error: HttpErrorResponse, revision: number): Promise<void> {
    let empty = false;
    if (error.status === 400 && error.error instanceof Blob) {
      try { empty = JSON.parse(await error.error.text()).title === EMPTY; } catch { /* Never display arbitrary server text. */ }
    }
    if (revision !== this.downloadRevision) return;
    this.downloading.set(false);
    if (empty) this.notice.set(EMPTY);
    else this.error.set('No se pudo descargar el informe.');
  }

  private cancelDownload(): void {
    ++this.downloadRevision;
    this.downloadSubscription?.unsubscribe();
    this.downloadSubscription = undefined;
    this.downloading.set(false);
  }
}
