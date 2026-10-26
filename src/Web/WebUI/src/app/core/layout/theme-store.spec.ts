import { TestBed } from '@angular/core/testing';
import { ThemeStore } from './theme-store';

describe('Tema de SADInformes', () => {
  let changed: () => void;
  let media: { matches: boolean; addEventListener: ReturnType<typeof vi.fn>; removeEventListener: ReturnType<typeof vi.fn> };
  beforeEach(() => {
    localStorage.removeItem('sadinformes-theme-preference');
    media = {
      matches: true,
      addEventListener: vi.fn((_event, listener) => { changed = listener; }),
      removeEventListener: vi.fn(),
    };
    vi.stubGlobal('matchMedia', vi.fn().mockReturnValue(media as unknown as MediaQueryList));
  });
  afterEach(() => {
    TestBed.resetTestingModule();
    localStorage.removeItem('sadinformes-theme-preference');
    document.documentElement.removeAttribute('data-theme');
    document.documentElement.removeAttribute('data-bs-theme');
    document.documentElement.style.colorScheme = '';
    vi.restoreAllMocks();
    vi.unstubAllGlobals();
  });

  it('sigue el tema del sistema en auto y reacciona a cambios', () => {
    const theme = TestBed.inject(ThemeStore);
    expect(theme.preference()).toBe('auto');
    expect(theme.resolvedTheme()).toBe('dark');
    expect(document.documentElement.getAttribute('data-bs-theme')).toBe('dark');
    media.matches = false;
    changed();
    expect(theme.resolvedTheme()).toBe('light');
    expect(document.documentElement.style.colorScheme).toBe('light');
  });

  it('persiste una preferencia propia sin cambiar la de SADAlumnos', () => {
    localStorage.setItem('sadalumnos-theme-preference', 'dark');
    const theme = TestBed.inject(ThemeStore);
    theme.setPreference('light');
    expect(theme.preference()).toBe('light');
    expect(localStorage.getItem('sadinformes-theme-preference')).toBe('light');
    expect(localStorage.getItem('sadalumnos-theme-preference')).toBe('dark');
    expect(document.documentElement.getAttribute('data-theme')).toBe('light');
    localStorage.removeItem('sadalumnos-theme-preference');
  });

  it('recupera la preferencia guardada y tolera almacenamiento no disponible', () => {
    localStorage.setItem('sadinformes-theme-preference', 'dark');
    const theme = TestBed.inject(ThemeStore);
    expect(theme.preference()).toBe('dark');
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => { throw new Error('bloqueado'); });
    expect(() => theme.setPreference('light')).not.toThrow();
    expect(theme.resolvedTheme()).toBe('light');
  });
});
