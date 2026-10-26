import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { App } from './app';
import { routes } from './app.routes';
import { AuthService } from './core/auth/auth-service';

describe('Base de SADInformes', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [App], providers: [
      provideRouter(routes),
      { provide: AuthService, useValue: {
        isAuthenticated: () => true, username: () => 'usuario',
        logout: vi.fn().mockResolvedValue(undefined), login: vi.fn().mockResolvedValue(undefined),
      } },
    ] });
  });

  it('crea el contenedor standalone', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('router-outlet')).not.toBeNull();
  });

  it('muestra la página inicial autenticada sin funciones de alumnos', async () => {
    const harness = await RouterTestingHarness.create('/');
    expect(harness.routeNativeElement?.querySelector('h1')?.textContent).toBe('SADInformes');
    expect(harness.routeNativeElement?.textContent).toContain('Base del sistema de informes.');
    expect(harness.routeNativeElement?.querySelector('nav, aside, form')).toBeNull();
    expect(harness.routeNativeElement?.textContent).toContain('Sesión iniciada: usuario');
    const button = harness.routeNativeElement?.querySelector('button');
    expect(button?.textContent).toBe('Cerrar sesión');
    button?.click();
    expect(TestBed.inject(AuthService).logout).toHaveBeenCalledOnce();
  });

  it('redirige las rutas desconocidas al inicio', async () => {
    const harness = await RouterTestingHarness.create('/ruta-inexistente');
    expect(harness.routeNativeElement?.querySelector('h1')?.textContent).toBe('SADInformes');
  });
});
