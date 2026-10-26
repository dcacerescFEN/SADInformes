import { DestroyRef, Injectable, computed, inject, signal } from '@angular/core';

export type ThemePreference = 'light' | 'dark' | 'auto';
@Injectable({ providedIn: 'root' })
export class ThemeStore {
  private readonly storageKey = 'sadinformes-theme-preference';
  private readonly media = window.matchMedia('(prefers-color-scheme: dark)');
  private readonly systemDark = signal(this.media.matches);
  readonly preference = signal<ThemePreference>(this.storedPreference());
  readonly resolvedTheme = computed(() => this.preference() === 'auto'
    ? (this.systemDark() ? 'dark' : 'light') : this.preference() as 'light' | 'dark');

  constructor() {
    const changed = () => { this.systemDark.set(this.media.matches); this.applyTheme(); };
    this.media.addEventListener('change', changed);
    inject(DestroyRef).onDestroy(() => this.media.removeEventListener('change', changed));
    this.applyTheme();
  }

  setPreference(preference: ThemePreference): void {
    this.preference.set(preference);
    try { localStorage.setItem(this.storageKey, preference); } catch { /* Almacenamiento opcional. */ }
    this.applyTheme();
  }

  private storedPreference(): ThemePreference {
    try {
      const value = localStorage.getItem(this.storageKey);
      return value === 'light' || value === 'dark' ? value : 'auto';
    } catch { return 'auto'; }
  }

  private applyTheme(): void {
    const root = document.documentElement;
    root.setAttribute('data-bs-theme', this.resolvedTheme());
    root.setAttribute('data-theme', this.resolvedTheme());
    root.style.colorScheme = this.resolvedTheme();
  }
}
