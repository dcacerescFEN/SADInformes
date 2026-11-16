import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { Router } from '@angular/router';
import { NotificationService } from '../../core/services/notification-service';

interface Option { value: string; text: string; }
interface Settings {
  period: string;
  activePeriod: string;
  periods: Option[];
  startDate: string;
  endDate: string;
  startTime: string;
  endTime: string;
  pendingEmailText: string;
  laggardEmailText: string;
}
interface SettingsBody {
  period: string;
  startDate: string;
  endDate: string;
  startTime: string;
  endTime: string;
  pendingEmailText: string;
  laggardEmailText: string;
  processPeriod: boolean;
}

const BASE = '/api/reports/academic-enrollment/teacher-availability';
const AVAILABILITY_ROUTE = '/academic-enrollment/teacher-availability';

@Component({
  selector: 'app-teacher-availability-settings',
  imports: [ReactiveFormsModule, NgSelectModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="container-fluid px-0 py-4">
      <div class="page-header">
        <div class="d-flex flex-wrap justify-content-between align-items-start gap-3">
          <div>
            <h1 class="page-title">
              <i class="bi bi-gear" aria-hidden="true"></i>
              Administración Disponibilidad Horaria
            </h1>
            <p class="page-subtitle mb-0">
              Configuración del período de disponibilidad
              @if (activePeriod()) { · Activo: {{ activePeriod() }} }.
            </p>
          </div>
          <button type="button" class="btn btn-clear" data-action="back" (click)="cancel()">
            <i class="bi bi-arrow-left me-1" aria-hidden="true"></i>Volver a Disponibilidad Horaria
          </button>
        </div>
      </div>

      <div class="form-section">
        <h2 class="section-title"><i class="bi bi-sliders" aria-hidden="true"></i>Configuración del período</h2>

        @if (loading()) {
          <p role="status">Cargando configuración…</p>
        } @else {
          @if (error()) { <div class="alert alert-warning" role="alert">{{ error() }}</div> }
          @if (periods().length === 0) {
            <p class="mb-0">No hay períodos académicos disponibles.</p>
          } @else {
            <form [formGroup]="form" (ngSubmit)="save()">
              <div class="row g-3">
                <div class="col-lg-4 col-md-6">
                  <div class="ng-select-container-custom">
                    <label class="floating-label" for="availability-settings-period">Período</label>
                    <ng-select id="availability-settings-period" formControlName="period" appendTo="body"
                      [items]="periods()" bindLabel="text" bindValue="value"
                      [searchable]="true" [clearable]="false"
                      class="form-select ng-select-custom" data-field="period"
                      (change)="onPeriodChange()" />
                  </div>
                </div>
                <div class="col-lg-4 col-md-6">
                  <div class="form-floating">
                    <input id="availability-settings-start-date" type="date" class="form-control"
                      placeholder="Fecha inicio" formControlName="startDate" data-field="startDate" />
                    <label for="availability-settings-start-date">Fecha inicio</label>
                  </div>
                </div>
                <div class="col-lg-2 col-md-3">
                  <div class="form-floating">
                    <input id="availability-settings-start-time" type="time" class="form-control"
                      placeholder="Hora inicio" formControlName="startTime" data-field="startTime" />
                    <label for="availability-settings-start-time">Hora inicio</label>
                  </div>
                </div>
                <div class="col-lg-2 col-md-3">
                  <div class="form-floating">
                    <input id="availability-settings-end-date" type="date" class="form-control"
                      placeholder="Fecha término" formControlName="endDate" data-field="endDate" />
                    <label for="availability-settings-end-date">Fecha término</label>
                  </div>
                </div>
                <div class="col-lg-2 col-md-3">
                  <div class="form-floating">
                    <input id="availability-settings-end-time" type="time" class="form-control"
                      placeholder="Hora término" formControlName="endTime" data-field="endTime" />
                    <label for="availability-settings-end-time">Hora término</label>
                  </div>
                </div>
                <div class="col-lg-5 col-md-6">
                  <div class="form-floating">
                    <input id="availability-settings-pending-text" type="text" maxlength="100" class="form-control"
                      placeholder="Texto fecha pendiente correo" formControlName="pendingEmailText"
                      data-field="pendingEmailText" />
                    <label for="availability-settings-pending-text">Texto fecha pendiente correo</label>
                  </div>
                </div>
                <div class="col-lg-5 col-md-6">
                  <div class="form-floating">
                    <input id="availability-settings-laggard-text" type="text" maxlength="100" class="form-control"
                      placeholder="Texto fecha rezagados correo" formControlName="laggardEmailText"
                      data-field="laggardEmailText" />
                    <label for="availability-settings-laggard-text">Texto fecha rezagados correo</label>
                  </div>
                </div>
                <div class="col-12">
                  <div class="form-check">
                    <input id="availability-settings-process" class="form-check-input" type="checkbox"
                      formControlName="processPeriod" data-field="processPeriod" />
                    <label class="form-check-label" for="availability-settings-process">Procesar período seleccionado</label>
                  </div>
                </div>
              </div>

              <div class="d-flex flex-wrap justify-content-end gap-2 mt-4">
                <button type="button" class="btn btn-clear" data-action="cancel" (click)="cancel()">
                  <i class="bi bi-x-circle me-1" aria-hidden="true"></i>Cancelar
                </button>
                <button type="submit" class="btn btn-create" data-action="save"
                  [disabled]="saving() || loading()">
                  <i class="bi bi-save me-1" aria-hidden="true"></i>{{ saving() ? 'Guardando…' : 'Guardar' }}
                </button>
              </div>
            </form>
          }
        }
      </div>
    </div>
  `,
})
export class TeacherAvailabilitySettings {
  private readonly http = inject(HttpClient);
  private readonly destroyRef = inject(DestroyRef);
  private readonly notification = inject(NotificationService);
  private readonly router = inject(Router);
  readonly form = inject(FormBuilder).nonNullable.group({
    period: '',
    startDate: '',
    endDate: '',
    startTime: '',
    endTime: '',
    pendingEmailText: '',
    laggardEmailText: '',
    processPeriod: false,
  });
  protected readonly periods = signal<Option[]>([]);
  protected readonly activePeriod = signal('');
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly error = signal('');
  private revision = 0;

  constructor() { this.load(); }

  protected onPeriodChange(): void {
    const period = this.form.controls.period.value;
    if (period) this.load(period);
  }

  protected save(): void {
    if (this.saving() || !this.valid()) return;
    const body = this.form.getRawValue();
    if (!body.processPeriod) { this.put(body); return; }
    void this.notification.confirm(
      'Activar período',
      'El período seleccionado quedará como período activo de disponibilidad. ¿Continuar?',
      'Activar', 'Cancelar')
      .then(result => { if (result.isConfirmed) this.put(body); });
  }

  protected cancel(): void {
    void this.router.navigate([AVAILABILITY_ROUTE]);
  }

  private valid(): boolean {
    const value = this.form.getRawValue();
    if (!value.period || !value.startDate || !value.endDate || !value.startTime || !value.endTime) {
      this.error.set('Complete período, fecha y hora de inicio y término.');
      return false;
    }
    if (value.pendingEmailText.length > 100 || value.laggardEmailText.length > 100) {
      this.error.set('Los textos de correo no pueden superar 100 caracteres.');
      return false;
    }
    this.error.set('');
    return true;
  }

  private load(period?: string): void {
    const revision = ++this.revision;
    this.loading.set(true);
    this.error.set('');
    this.http.get<{ success: boolean; data: Settings }>(BASE + '/settings', {
      params: period ? { period } : {},
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: result => {
        if (revision !== this.revision) return;
        this.loading.set(false);
        if (!result.success) { this.error.set('No se pudo cargar la configuración.'); return; }
        this.periods.set(result.data.periods);
        this.activePeriod.set(result.data.activePeriod);
        this.form.patchValue({
          period: result.data.period,
          startDate: result.data.startDate,
          endDate: result.data.endDate,
          startTime: result.data.startTime,
          endTime: result.data.endTime,
          pendingEmailText: result.data.pendingEmailText,
          laggardEmailText: result.data.laggardEmailText,
          processPeriod: false,
        });
      },
      error: () => {
        if (revision !== this.revision) return;
        this.loading.set(false);
        this.error.set('No se pudo cargar la configuración.');
      },
    });
  }

  private put(body: SettingsBody): void {
    this.saving.set(true);
    this.error.set('');
    this.http.put<void>(BASE + '/settings', body)
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: () => {
          this.saving.set(false);
          this.notification.showSuccess('Datos guardados', 'La configuración fue actualizada.');
          this.load(body.period);
        },
        error: (error: HttpErrorResponse) => {
          this.saving.set(false);
          const title = error.error?.title;
          this.error.set(typeof title === 'string' && title.length > 0
            ? title : 'No se pudo guardar la configuración.');
        },
      });
  }
}
