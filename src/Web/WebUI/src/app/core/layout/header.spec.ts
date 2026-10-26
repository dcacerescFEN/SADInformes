import { TestBed, ComponentFixture } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { signal } from '@angular/core';
import { AuthService } from '../auth/auth-service';
import { NavigationService, NavigationLink } from '../navigation/navigation-service';
import { ThemeStore } from './theme-store';
import { Header } from './header';

describe('Navbar basada en SADAlumnos', () => {
  let fixture: ComponentFixture<Header>;
  const links = signal<NavigationLink[]>([]);
  const menuError = signal('');
  const preference = signal<'light' | 'dark' | 'auto'>('auto');
  const logout = vi.fn();
  const load = vi.fn();
  const setPreference = vi.fn();
  beforeEach(async () => {
    links.set([{ key: 'student', label: 'Alumnos', icon: 'bi-person-badge', url: 'https://alumnos.example.test' }]);
    menuError.set('');
    preference.set('auto');
    logout.mockReset().mockResolvedValue(undefined);
    load.mockReset().mockResolvedValue(undefined);
    setPreference.mockReset().mockImplementation((value) => preference.set(value));
    await TestBed.configureTestingModule({ imports: [Header], providers: [
      provideRouter([]),
      { provide: AuthService, useValue: { username: () => 'usuario', logout } },
      { provide: NavigationService, useValue: { links, error: menuError, load } },
      { provide: ThemeStore, useValue: { preference, resolvedTheme: () => 'light', setPreference } },
    ] }).compileComponents();
    fixture = TestBed.createComponent(Header);
    fixture.detectChanges();
  });

  it('carga menú, muestra marca y solo destinos proporcionados, sin sidebar', () => {
    const root: HTMLElement = fixture.nativeElement;
    expect(load).toHaveBeenCalledOnce();
    expect(root.querySelector('nav[aria-label="Navegación principal"]')).not.toBeNull();
    expect(root.querySelector('.navbar-brand img')?.getAttribute('src')).toBe('/logo-fen-white-letters.svg');
    expect(root.querySelector('.navbar-brand')?.textContent?.trim()).toBe('');
    expect(root.querySelector('nav')?.classList.contains('navbar-expand-lg')).toBe(true);
    expect(root.querySelector('nav')?.classList.contains('navbar-expand-xl')).toBe(false);
    expect(root.querySelector('a[aria-label="Alumnos"]')?.getAttribute('href')).toBe('https://alumnos.example.test');
    expect(root.textContent).not.toContain('Profesores');
    expect(root.querySelector('aside')).toBeNull();
    expect(root.querySelector('a[aria-label="Correo FEN"]')?.hasAttribute('title')).toBe(false);
  });

  it('muestra Informes antes de Administración y lo marca como sitio actual', () => {
    links.set([
      { key: 'student', label: 'Alumnos', icon: 'bi-person-badge', url: 'https://alumnos.example.test' },
      { key: 'reports', label: 'Informes', icon: 'bi-file-earmark-bar-graph', url: 'https://sadinformes-dev.fen.uchile.cl' },
      { key: 'administration', label: 'Administración', icon: 'bi-shield-lock', url: 'https://admin.example.test' },
    ]);
    fixture.detectChanges();
    const anchors = [...fixture.nativeElement.querySelectorAll('a.header-nav-link')] as HTMLAnchorElement[];
    expect(anchors.map((link) => link.getAttribute('aria-label'))).toEqual(['Alumnos', 'Informes', 'Administración']);
    expect(anchors[1].classList.contains('active')).toBe(true);
    expect(anchors[1].getAttribute('aria-current')).toBe('page');
    expect(anchors[2].classList.contains('active')).toBe(false);
    expect(fixture.nativeElement.querySelector('.navbar-brand').hasAttribute('aria-current')).toBe(false);
  });

  it('alterna el menú móvil y lo cierra al elegir destino', () => {
    const root: HTMLElement = fixture.nativeElement;
    const toggle = root.querySelector<HTMLButtonElement>('.navbar-toggler')!;
    expect(toggle.getAttribute('aria-expanded')).toBe('false');
    toggle.click();
    fixture.detectChanges();
    expect(toggle.getAttribute('aria-expanded')).toBe('true');
    const link = root.querySelector<HTMLAnchorElement>('a[aria-label="Alumnos"]')!;
    link.addEventListener('click', (event) => event.preventDefault());
    link.click();
    fixture.detectChanges();
    expect(toggle.getAttribute('aria-expanded')).toBe('false');
  });

  it('deja sin href y fuera de tabulación el destino sin URL', () => {
    links.set([{ key: 'student', label: 'Alumnos', icon: 'bi-person-badge', url: null }]);
    fixture.detectChanges();
    const link = fixture.nativeElement.querySelector('a[aria-label="Alumnos"]');
    expect(link.hasAttribute('href')).toBe(false);
    expect(link.getAttribute('aria-disabled')).toBe('true');
    expect(link.getAttribute('tabindex')).toBe('-1');
  });

  it('permite cambiar tema y cerrar sesión desde el menú del usuario', async () => {
    const root: HTMLElement = fixture.nativeElement;
    root.querySelector<HTMLButtonElement>('#themeMenu')!.click();
    fixture.detectChanges();
    root.querySelector<HTMLButtonElement>('[data-theme-choice="dark"]')!.click();
    fixture.detectChanges();
    expect(setPreference).toHaveBeenCalledWith('dark');
    expect(root.querySelector('[data-theme-choice="dark"]')?.getAttribute('aria-pressed')).toBe('true');
    root.querySelector<HTMLButtonElement>('#userMenu')!.click();
    fixture.detectChanges();
    root.querySelector<HTMLButtonElement>('[data-action="logout"]')!.click();
    await fixture.whenStable();
    expect(logout).toHaveBeenCalledOnce();
    expect(root.querySelector('a[href="https://cuenta.fen.uchile.cl"]')?.getAttribute('rel')).toContain('noopener');
  });

  it('mantiene controles de sesión/tema ante fallo de AppConfig y muestra fallo de logout', async () => {
    menuError.set('No se pudo cargar la navegación.');
    links.set([]);
    logout.mockRejectedValue(new Error('falló'));
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('[role="status"]')?.textContent).toContain('No se pudo cargar la navegación.');
    expect(root.querySelector('#themeMenu')).not.toBeNull();
    root.querySelector<HTMLButtonElement>('[data-action="logout"]')!.click();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(root.querySelector('[role="alert"]')?.textContent).toContain('No se pudo cerrar sesión.');
  });
});
