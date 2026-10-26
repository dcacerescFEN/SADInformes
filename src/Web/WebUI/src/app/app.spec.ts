import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { App } from './app';
import { routes } from './app.routes';
import { AuthService } from './core/auth/auth-service';
import { signal, WritableSignal } from '@angular/core';
import { NgbConfig } from '@ng-bootstrap/ng-bootstrap';
import { NavigationService } from './core/navigation/navigation-service';
import { ThemeStore } from './core/layout/theme-store';

describe('Base de SADInformes', () => {
  let authenticated: WritableSignal<boolean>;
  let username: WritableSignal<string>;
  let name: WritableSignal<string>;
  beforeEach(() => {
    authenticated = signal(true);
    username = signal('usuario');
    name = signal('Usuario Prueba');
    TestBed.configureTestingModule({ imports: [App], providers: [
      provideRouter(routes),
      { provide: NavigationService, useValue: { links: signal([]), error: signal(''), load: vi.fn() } },
      { provide: ThemeStore, useValue: { preference: signal('auto'), resolvedTheme: () => 'light', setPreference: vi.fn() } },
      { provide: AuthService, useValue: {
        isAuthenticated: authenticated, username, name,
        logout: vi.fn().mockResolvedValue(undefined), login: vi.fn().mockResolvedValue(undefined),
      } },
    ] });
    TestBed.inject(NgbConfig).animation = false;
  });

  it('crea el contenedor standalone', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('router-outlet')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('app-header')).not.toBeNull();
    fixture.nativeElement.querySelector('[data-action="logout"]').click();
    expect(TestBed.inject(AuthService).logout).toHaveBeenCalledOnce();
  });

  it('integra sidebar, main y toggle de escritorio sin alterar el menú superior', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelectorAll('router-outlet')).toHaveLength(1);
    expect(root.querySelectorAll('main')).toHaveLength(1);
    expect(root.querySelector('#reports-sidebar')).not.toBeNull();
    const desktop = root.querySelector<HTMLButtonElement>('[data-sidebar-mode="desktop"]')!;
    expect(desktop.getAttribute('aria-expanded')).toBe('true');
    desktop.click();
    fixture.detectChanges();
    expect(desktop.getAttribute('aria-expanded')).toBe('false');
    expect(root.querySelector('app-reports-sidebar')?.classList.contains('collapsed')).toBe(true);
    const navigation = root.querySelector<HTMLButtonElement>('.navbar-toggler')!;
    navigation.click();
    fixture.detectChanges();
    expect(navigation.getAttribute('aria-expanded')).toBe('true');
    expect(desktop.getAttribute('aria-expanded')).toBe('false');
  });

  it('cierra panel y perfil al perder sesión sin recrear el router-outlet', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;
    const outlet = root.querySelector('router-outlet');
    root.querySelector<HTMLButtonElement>('[data-sidebar-mode="mobile"]')!.click();
    await fixture.whenStable();
    expect(document.querySelector('[role="dialog"]')?.textContent).toContain('Usuario Prueba');
    authenticated.set(false);
    fixture.detectChanges();
    await fixture.whenStable();
    await new Promise<void>((resolve) => setTimeout(resolve, 0));
    expect(root.querySelector('app-header, app-reports-sidebar')).toBeNull();
    expect(document.querySelector('[role="dialog"]')).toBeNull();
    expect(document.querySelector('ngb-offcanvas-backdrop')).toBeNull();
    expect(document.body.textContent).not.toContain('Usuario Prueba');
    expect(root.querySelector('router-outlet')).toBe(outlet);
    username.set('otro-usuario');
    name.set('Otra Persona');
    authenticated.set(true);
    fixture.detectChanges();
    expect(root.querySelector('#reports-sidebar')?.textContent).toContain('Otra Persona');
    expect(root.querySelectorAll('app-reports-sidebar')).toHaveLength(1);
    expect(document.querySelector('[role="dialog"]')).toBeNull();
  });

  it('muestra la página inicial autenticada sin funciones de alumnos', async () => {
    const harness = await RouterTestingHarness.create('/');
    expect(harness.routeNativeElement?.querySelector('h1')?.textContent).toBe('SADInformes');
    expect(harness.routeNativeElement?.textContent).toContain('Base del sistema de informes.');
    expect(harness.routeNativeElement?.querySelector('nav, aside, form')).toBeNull();
    expect(harness.routeNativeElement?.textContent).toContain('Sesión iniciada: usuario');
    expect(harness.routeNativeElement?.querySelector('button')).toBeNull();
  });

  it('redirige las rutas desconocidas al inicio', async () => {
    const harness = await RouterTestingHarness.create('/ruta-inexistente');
    expect(harness.routeNativeElement?.querySelector('h1')?.textContent).toBe('SADInformes');
  });
});
