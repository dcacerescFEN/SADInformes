import { Injectable, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, map, of, tap, throwError } from 'rxjs';
import { BackgroundTaskCenter } from '../../core/tasks/background-task-center';
import { BackgroundTaskPatch, BackgroundTaskStatus } from '../../core/tasks/background-task';

const BASE = '/api/reports/academic-enrollment/teacher-availability/email';
interface StartResponse { success: boolean; data: { jobId: string; state: string }; }
interface StatusResponse {
  success: boolean;
  data: { jobId: string; state: string; phase: string; total: number; sent: number; omitted: number; failed: number };
}

@Injectable({ providedIn: 'root' })
export class TeacherAvailabilityEmailDispatchService {
  private readonly http = inject(HttpClient);
  private readonly center = inject(BackgroundTaskCenter);
  readonly active = computed(() => this.center.tasks().some(task => !task.terminal));

  start(period: string, mode: string, teacherCode?: string): Observable<StartResponse> {
    return this.http.post<StartResponse>(BASE, { period, mode, teacherCode }).pipe(
      tap(response => {
        if (response.success) this.track(response.data.jobId);
      }),
    );
  }

  private track(jobId: string): void {
    this.center.start({
      id: jobId,
      kind: 'email',
      title: 'Envío de correos de disponibilidad',
      description: 'Disponibilidad Horaria',
      status: 'queued',
      stage: 'Cargando lista de profesores',
      current: 0,
      details: this.details(0, 0, 0),
      terminal: false,
      resumable: true,
      cancelLabel: 'Cancelar envío',
      cancel: () => this.cancel(jobId).subscribe({ error: () => undefined }),
    }, () => this.status(jobId));
  }

  private status(jobId: string): Observable<BackgroundTaskPatch> {
    return this.http.get<StatusResponse>(`${BASE}/${jobId}`).pipe(map(response => {
      const data = response.data;
      const status = this.statusValue(data.state);
      return {
        status,
        stage: this.stageValue(data.phase),
        current: data.sent + data.failed,
        total: data.total,
        details: this.details(data.sent, data.omitted, data.failed),
        terminal: status === 'completed' || status === 'partial' || status === 'failed'
          || status === 'cancelled',
      };
    }), catchError(() => of({
      status: 'failed' as BackgroundTaskStatus,
      stage: 'El envío anterior ya no está disponible',
      current: 0,
      total: 0,
      details: this.details(0, 0, 0),
      terminal: true,
    })));
  }

  private cancel(jobId: string): Observable<StatusResponse> {
    this.center.update(jobId, { stage: 'Cancelando envío…' });
    return this.http.post<StatusResponse>(`${BASE}/${jobId}/cancel`, {}).pipe(
      tap(response => {
        const status = this.statusValue(response.data.state);
        this.center.update(jobId, {
          status,
          stage: this.stageValue(response.data.phase),
          terminal: status === 'cancelled' || status === 'completed'
            || status === 'partial' || status === 'failed',
        });
      }),
      catchError(error => {
        this.status(jobId).subscribe({
          next: patch => this.center.update(jobId, patch),
          error: () => this.center.finish(jobId, 'failed'),
        });
        return throwError(() => error);
      }),
    );
  }

  private statusValue(state: string): BackgroundTaskStatus {
    return ({ QUEUED: 'queued', RUNNING: 'running', COMPLETED: 'completed',
      PARTIAL: 'partial', FAILED: 'failed', CANCELLED: 'cancelled' } as Record<string, BackgroundTaskStatus>)[state] ?? 'failed';
  }

  private stageValue(phase: string): string {
    return ({ LOADING: 'Cargando lista de profesores', SENDING: 'Enviando correos',
      COMPLETED: 'Envío finalizado', FAILED: 'Envío fallido', CANCELLED: 'Envío cancelado',
    } as Record<string, string>)[phase] ?? 'Enviando correos';
  }

  private details(sent: number, omitted: number, failed: number) {
    return [
      { label: 'Enviados', value: String(sent) },
      { label: 'Omitidos', value: String(omitted) },
      { label: 'Fallidos', value: String(failed) },
    ];
  }
}
