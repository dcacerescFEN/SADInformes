import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '../auth/auth-service';

export interface NavigationLink { key: string; label: string; icon: string; url: string | null; }
const DESTINATIONS = [
  ['student', 'Alumnos', 'bi-person-badge', 'studentUrl', 'studentClientId'],
  ['teacher', 'Profesores', 'bi-person-lines-fill', 'teacherUrl', 'teacherClientId'],
  ['courses', 'Cursos', 'bi-book', 'coursesMenuUrl', 'coursesMenuClientId'],
  ['exams', 'Exámenes', 'bi-clipboard-check', 'examsMenuUrl', 'examsMenuClientId'],
  ['activities', 'Actividades', 'bi-calendar-event', 'activitiesMenuUrl', 'activitiesMenuClientId'],
  ['assistants', 'Ayudantes', 'bi-people', 'assistantsMenuUrl', 'assistantsMenuClientId'],
  ['processes', 'Procesos', 'bi-gear', 'processesMenuUrl', 'processesMenuClientId'],
  ['administration', 'Administración', 'bi-shield-lock', 'administrationMenuUrl', 'administrationMenuClientId'],
] as const;

function safeUrl(value: unknown): string | null {
  if (typeof value !== 'string' || !/^https?:\/\/[^/?#]/i.test(value)
    || /[\s\\\u0000-\u001f\u007f]/.test(value)) return null;
  try {
    const url = new URL(value);
    return (url.protocol === 'http:' || url.protocol === 'https:') && url.hostname
      && !url.username && !url.password ? value : null;
  } catch { return null; }
}

@Injectable({ providedIn: 'root' })
export class NavigationService {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);
  private readonly menu = signal<Record<string, unknown>>({});
  readonly error = signal('');
  readonly links = computed<NavigationLink[]>(() => {
    if (!this.auth.isAuthenticated()) return [];
    return DESTINATIONS.flatMap(([key, label, icon, urlKey, clientKey]) => {
      const value = this.menu()[clientKey];
      const clientId = typeof value === 'string' ? value.trim() : '';
      return this.auth.hasSiteAccess(clientId)
        ? [{ key, label, icon, url: safeUrl(this.menu()[urlKey]) }] : [];
    });
  });

  async load(): Promise<void> {
    this.menu.set({});
    this.error.set('');
    try {
      const config = await firstValueFrom(this.http.get<{ menu?: unknown }>('/api/config/site'));
      const menu = config?.menu;
      if (typeof menu === 'object' && menu !== null && !Array.isArray(menu))
        this.menu.set(menu as Record<string, unknown>);
    } catch {
      this.menu.set({});
      this.error.set('No se pudo cargar la navegación.');
    }
  }
}
