import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { BackgroundTaskCenter } from './background-task-center';
import { BackgroundTask } from './background-task';

@Component({
  selector: 'app-background-task-host',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (center.tasks().length > 0) {
      @if (center.minimized()) {
        <button type="button" class="task-pill" (click)="center.expand()"
          [attr.aria-label]="'Abrir progreso de ' + current()!.title">
          <i class="bi bi-arrow-repeat me-2" aria-hidden="true"></i>
          <span class="task-pill__text">
            <strong>{{ current()!.title }}</strong>
            <small>{{ stage(current()) }} · {{ progressText(current()) }}</small>
          </span>
          @if (center.tasks().length > 1) {
            <span class="badge bg-primary ms-2">{{ center.tasks().length }}</span>
          }
        </button>
      } @else {
        <div class="modal-backdrop show task-backdrop"></div>
        <div class="modal d-block task-layer" role="dialog" aria-modal="true"
          [attr.aria-label]="current()!.title">
          <div class="modal-dialog modal-dialog-centered modal-dialog-scrollable">
            <div class="modal-content">
              <div class="modal-header">
                <h2 class="modal-title fs-5">
                  <i class="bi bi-arrow-repeat me-2" aria-hidden="true"></i>
                  {{ current()!.title }}
                </h2>
                <button type="button" class="btn-close" aria-label="Cerrar"
                  (click)="current()!.terminal ? center.dismiss(current()!.id) : center.minimize()"></button>
              </div>

              <div class="modal-body">
                @if (current()!.description) {
                  <p class="text-body-secondary">{{ current()!.description }}</p>
                }
                @if (center.tasks().length > 1) {
                  <div class="task-tabs">
                    @for (task of center.tasks(); track task.id) {
                      <button type="button" class="btn btn-sm"
                        [class.btn-primary]="task.id === current()!.id"
                        [class.btn-outline-secondary]="task.id !== current()!.id"
                        (click)="center.expand(task.id)">{{ task.title }}</button>
                    }
                  </div>
                }

                @if (current(); as task) {
                  <div class="progress my-3" role="progressbar"
                    [attr.aria-valuenow]="task.total ? task.current : null"
                    [attr.aria-valuemin]="0" [attr.aria-valuemax]="task.total ?? null">
                    <div class="progress-bar" [style.width.%]="percent(task)"
                      [class.progress-bar-striped]="!task.terminal"
                      [class.progress-bar-animated]="!task.terminal"></div>
                  </div>
                  <p class="mb-3"><strong>{{ stage(task) }}</strong> · {{ progressText(task) }}</p>
                  <dl class="row mb-0">
                    @for (detail of task.details; track detail.label) {
                      <dt class="col-6">{{ detail.label }}</dt>
                      <dd class="col-6 text-end mb-1">{{ detail.value }}</dd>
                    }
                  </dl>
                }
              </div>

              <div class="modal-footer">
                <button type="button" class="btn btn-clear" (click)="center.minimize()">
                  <i class="bi bi-dash-lg me-1" aria-hidden="true"></i>Minimizar
                </button>
                @if (current()!.cancel && !current()!.terminal) {
                  <button type="button" class="btn btn-danger" (click)="current()!.cancel!()">
                    <i class="bi bi-x-circle me-1" aria-hidden="true"></i>
                    {{ current()!.cancelLabel ?? 'Cancelar' }}
                  </button>
                }
                @if (current()!.terminal) {
                  <button type="button" class="btn btn-save" (click)="center.dismiss(current()!.id)">
                    Cerrar
                  </button>
                }
              </div>
            </div>
          </div>
        </div>
      }
    }
  `,
  styles: `
    .task-backdrop { z-index: 1070; }
    .task-layer { z-index: 1080; background: transparent; }
    .task-layer .modal-content { border: 0; box-shadow: 0 1rem 3rem rgba(0,0,0,.25); }
    .task-pill { position: fixed; right: 1rem; bottom: 1rem; z-index: 1080; display: flex;
      align-items: center; max-width: min(24rem, calc(100vw - 2rem)); padding: .75rem 1rem;
      border: 1px solid var(--bs-border-color); border-radius: .75rem; background: var(--bs-body-bg);
      color: var(--bs-body-color); box-shadow: 0 .5rem 1.5rem rgba(0,0,0,.2); text-align: left; }
    .task-pill__text { display: flex; flex-direction: column; min-width: 0; }
    .task-pill__text strong, .task-pill__text small {
      overflow: hidden; text-overflow: ellipsis; white-space: nowrap;
    }
    .task-tabs { display: flex; flex-wrap: wrap; gap: .35rem; margin-bottom: 1rem; }
  `,
})
export class BackgroundTaskHost {
  protected readonly center = inject(BackgroundTaskCenter);
  protected readonly current = computed(() =>
    this.center.tasks().find(task => task.id === this.center.selectedId()) ?? this.center.tasks()[0]);

  protected percent(task: BackgroundTask): number {
    if (!task.total || task.total <= 0) return task.terminal ? 100 : 35;
    return Math.min(100, Math.round((task.current / task.total) * 100));
  }

  protected progressText(task: BackgroundTask | undefined): string {
    if (!task) return '';
    return task.total ? `${task.current} de ${task.total}` : `${task.current} procesados`;
  }

  protected stage(task: BackgroundTask | undefined): string {
    if (!task) return '';
    if (task.terminal) return this.statusLabel(task.status);
    return task.stage ?? this.statusLabel(task.status);
  }

  protected statusLabel(status: string | undefined): string {
    return ({
      queued: 'En cola',
      running: 'En progreso',
      completed: 'Completado',
      partial: 'Completado con fallos',
      failed: 'Fallido',
      cancelled: 'Cancelado',
    } as Record<string, string>)[status ?? ''] ?? '';
  }
}
