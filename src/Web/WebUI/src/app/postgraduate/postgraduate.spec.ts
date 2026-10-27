import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { routes } from '../app.routes';
import { AuthService } from '../core/auth/auth-service';
import { POSTGRADUATE_REPORTS } from './postgraduate-catalogue';

describe('Navegación visual de Postgrado', () => {
  const auth = {
    isAuthenticated: vi.fn(() => true), username: () => 'usuario',
    login: vi.fn().mockResolvedValue(undefined),
  };

  beforeEach(() => {
    auth.isAuthenticated.mockReturnValue(true);
    auth.login.mockClear();
    TestBed.configureTestingModule({ providers: [
      provideRouter(routes), provideHttpClient(), provideHttpClientTesting(),
      { provide: AuthService, useValue: auth },
    ] });
  });

  afterEach(() => TestBed.inject(HttpTestingController).verify());

  it('incluye las 30 pantallas principales, sin duplicar el detalle de profesor', () => {
    expect(POSTGRADUATE_REPORTS).toHaveLength(30);
    expect(new Set(POSTGRADUATE_REPORTS.map(report => report.slug)).size).toBe(30);
    expect(new Set(POSTGRADUATE_REPORTS.map(report => report.legacyPath)).size).toBe(30);
    expect(POSTGRADUATE_REPORTS.every(report => report.legacyPath.startsWith('Informes/Postgrado/'))).toBe(true);
    expect(POSTGRADUATE_REPORTS.some(report => report.legacyPath.includes('DetalleProfesor'))).toBe(false);
    expect(POSTGRADUATE_REPORTS.some(report => report.legacyPath.endsWith('EstadisticasISS/Resumen.aspx'))).toBe(true);
    expect(POSTGRADUATE_REPORTS.some(report => report.legacyPath.endsWith('EncuestaSalidaGraduados/Default.aspx'))).toBe(true);
  });

  it('cada ruta muestra su ficha pendiente sin formularios, acciones ni solicitudes HTTP', async () => {
    const harness = await RouterTestingHarness.create();
    for (const report of POSTGRADUATE_REPORTS) {
      await harness.navigateByUrl('/postgrado/' + report.slug);
      const root = harness.routeNativeElement!;
      expect(root.querySelector('h1')?.textContent).toBe(report.title);
      expect(root.textContent).toContain(report.description);
      expect(root.textContent).toContain('Pendiente de migración');
      expect(root.textContent).toContain(report.legacyPath);
      expect(root.querySelector('form, input, select, button, iframe')).toBeNull();
      expect(root.querySelector('a')?.getAttribute('href')).toBe('/');
    }
    TestBed.inject(HttpTestingController).expectNone(() => true);
  });

  it('conserva Inicio y redirige las opciones desconocidas al inicio', async () => {
    const harness = await RouterTestingHarness.create('/postgrado/no-existe');
    expect(TestBed.inject(Router).url).toBe('/');
    expect(harness.routeNativeElement?.querySelector('h1')?.textContent).toBe('SADInformes');
  });

  it('no permite abrir una ficha sin sesión e inicia el login', async () => {
    auth.isAuthenticated.mockReturnValue(false);
    const harness = await RouterTestingHarness.create();
    const target = '/postgrado/' + POSTGRADUATE_REPORTS[0].slug;
    await harness.navigateByUrl(target);
    expect(auth.login).toHaveBeenCalledExactlyOnceWith(target);
    expect(harness.routeNativeElement?.querySelector('h1')).toBeFalsy();
  });
});
