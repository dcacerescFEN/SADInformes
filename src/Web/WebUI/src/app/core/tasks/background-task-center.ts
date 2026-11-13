import { Injectable, signal } from '@angular/core';
import { Subscription, timer } from 'rxjs';
import { switchMap, takeWhile } from 'rxjs/operators';
import { BackgroundTask, BackgroundTaskPatch, BackgroundTaskPoll, BackgroundTaskStatus } from './background-task';

const STORAGE_KEY = 'sadinformes.background-tasks';
const CHANNEL_NAME = 'sadinformes.background-tasks';

@Injectable({ providedIn: 'root' })
export class BackgroundTaskCenter {
  readonly tasks = signal<BackgroundTask[]>([]);
  readonly minimized = signal(false);
  readonly selectedId = signal<string | null>(null);
  private readonly polls = new Map<string, Subscription>();
  private readonly channel = typeof BroadcastChannel !== 'undefined'
    ? new BroadcastChannel(CHANNEL_NAME) : null;

  constructor() {
    this.restore();
    this.channel?.addEventListener('message', event => this.receive(event.data));
  }

  start(task: BackgroundTask, poll?: BackgroundTaskPoll): void {
    this.stopPoll(task.id);
    this.tasks.update(tasks => [...tasks.filter(item => item.id !== task.id), task]);
    this.selectedId.set(task.id);
    this.minimized.set(false);
    if (poll && !task.terminal) this.startPoll(task.id, poll);
    this.persist();
    this.publish(task);
  }

  update(id: string, patch: BackgroundTaskPatch): void {
    this.tasks.update(tasks => tasks.map(task => task.id === id ? { ...task, ...patch } : task));
    const task = this.tasks().find(item => item.id === id);
    if (task?.terminal) this.stopPoll(id);
    this.persist();
    if (task) this.publish(task);
  }

  finish(id: string, status: BackgroundTaskStatus): void {
    this.update(id, { status, terminal: true });
  }

  minimize(): void { this.minimized.set(true); }
  expand(id?: string): void {
    if (id) this.selectedId.set(id);
    this.minimized.set(false);
  }

  dismiss(id: string): void {
    this.stopPoll(id);
    this.tasks.update(tasks => tasks.filter(task => task.id !== id));
    if (this.selectedId() === id) this.selectedId.set(this.tasks()[0]?.id ?? null);
    if (this.tasks().length === 0) this.minimized.set(false);
    this.persist();
  }

  private startPoll(id: string, poll: BackgroundTaskPoll): void {
    const subscription = timer(0, 2000).pipe(
      switchMap(() => poll()),
      takeWhile(patch => !patch.terminal, true),
    ).subscribe({
      next: patch => this.update(id, patch),
      error: () => this.finish(id, 'failed'),
    });
    this.polls.set(id, subscription);
  }

  private stopPoll(id: string): void {
    this.polls.get(id)?.unsubscribe();
    this.polls.delete(id);
  }

  private persist(): void {
    if (typeof sessionStorage === 'undefined') return;
    try {
      const tasks = this.tasks().map(({ id, kind, title, description, status, current, total, details, stage, terminal, resumable }) =>
        ({ id, kind, title, description, status, current, total, details, terminal, resumable }));
      sessionStorage.setItem(STORAGE_KEY, JSON.stringify({
        tasks, selectedId: this.selectedId(), minimized: this.minimized(),
      }));
    } catch { /* almacenamiento no disponible o lleno: la tarea sigue en memoria */ }
  }

  private restore(): void {
    // El estado en sessionStorage no incluye el poller. Restaurar tareas produciría
    // un progreso congelado; los adaptadores reanudan su job real al abrir la pantalla.
    if (typeof sessionStorage === 'undefined') return;
    try { sessionStorage.removeItem(STORAGE_KEY); } catch { /* sin persistencia */ }
  }

  private publish(task: BackgroundTask): void {
    if (!this.channel) return;
    try { this.channel.postMessage({ type: 'task', task: { ...task } }); }
    catch { /* la sincronización entre pestañas es opcional */ }
  }

  private receive(message: unknown): void {
    if (!message || typeof message !== 'object') return;
    const data = message as { type?: string; task?: BackgroundTask };
    if (data.type !== 'task' || !data.task?.id) return;
    this.tasks.update(tasks => {
      const current = tasks.find(task => task.id === data.task!.id);
      return current
        ? tasks.map(task => task.id === data.task!.id ? { ...task, ...data.task } : task)
        : [...tasks, data.task!];
    });
  }
}
