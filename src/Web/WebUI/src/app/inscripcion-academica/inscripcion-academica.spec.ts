import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { routes } from '../app.routes';
import { AuthService } from '../core/auth/auth-service';
import { INSCRIPCION_ACADEMICA_REPORTS } from './inscripcion-academica-catalogue';

describe('Navegación de Inscripción académica', () => {
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

  it('incluye las 22 opciones visibles sin duplicar slugs', () => {
    expect(INSCRIPCION_ACADEMICA_REPORTS).toHaveLength(22);
    expect(new Set(INSCRIPCION_ACADEMICA_REPORTS.map(report => report.slug)).size).toBe(22);
    expect(INSCRIPCION_ACADEMICA_REPORTS.every(report =>
      report.legacyPath.startsWith('Informes/Inscripcion/'))).toBe(true);
    expect(INSCRIPCION_ACADEMICA_REPORTS.find(report => report.slug === 'electives-without-common-plan')?.available)
      .toBe(true);
    expect(INSCRIPCION_ACADEMICA_REPORTS.find(report => report.slug === 'postgraduate-teaching-schedule')?.routePath)
      .toBe('/postgraduate/teaching-schedule');
  });

  it('carga el informe funcional de electivos sin plan común', async () => {
    const harness = await RouterTestingHarness.create(
      '/academic-enrollment/electives-without-common-plan');
    const http = TestBed.inject(HttpTestingController);
    http.expectOne('/api/reports/academic-enrollment/electives-without-common-plan/report')
      .flush({ success: true, data: { rows: [
        { decree: '009615/2003', courseCode: 'ENECO660', courseName: 'Electivo de mención' },
      ], total: 1 } });
    await harness.detectChanges();
    const root = harness.routeNativeElement!;
    expect(root.querySelector('h1')?.textContent?.trim())
      .toBe('Electivos de mención que no están en el plan común');
    expect(root.querySelectorAll('app-table tbody tr')).toHaveLength(1);
    expect(root.textContent).toContain('ENECO660');
    expect(root.textContent).not.toContain('Pendiente de migración');
  });

  it('cada opción pendiente abre su ficha local sin formularios ni solicitudes HTTP', async () => {
    const harness = await RouterTestingHarness.create();
    for (const report of INSCRIPCION_ACADEMICA_REPORTS.filter(item => !item.routePath && item.available !== true)) {
      await harness.navigateByUrl('/academic-enrollment/' + report.slug);
      const root = harness.routeNativeElement!;
      expect(root.querySelector('h1')?.textContent?.trim()).toBe(report.title);
      expect(root.textContent).toContain(report.description);
      expect(root.textContent).toContain('Pendiente de migración');
      expect(root.textContent).toContain(report.legacyPath);
      expect(root.querySelector('form, input, select, button, iframe')).toBeNull();
      expect(root.querySelector('a')?.getAttribute('href')).toBe('/');
    }
    TestBed.inject(HttpTestingController).expectNone(() => true);
  });
});
