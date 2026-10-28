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

  it('incluye las 31 pantallas principales, sin duplicar el detalle de profesor', () => {
    expect(POSTGRADUATE_REPORTS).toHaveLength(31);
    expect(new Set(POSTGRADUATE_REPORTS.map(report => report.slug)).size).toBe(31);
    expect(new Set(POSTGRADUATE_REPORTS.map(report => report.legacyPath)).size).toBe(31);
    expect(POSTGRADUATE_REPORTS.slice(0, 30).every(report => report.legacyPath.startsWith('Informes/Postgrado/'))).toBe(true);
    expect(POSTGRADUATE_REPORTS[30].legacyPath).toBe('Informes/Inscripcion/ProgramacionXProgramaModalidad.aspx');
    expect(POSTGRADUATE_REPORTS.some(report => report.legacyPath.includes('DetalleProfesor'))).toBe(false);
    expect(POSTGRADUATE_REPORTS.some(report => report.legacyPath.endsWith('EstadisticasISS/Resumen.aspx'))).toBe(true);
    expect(POSTGRADUATE_REPORTS.some(report => report.legacyPath.endsWith('EncuestaSalidaGraduados/Default.aspx'))).toBe(true);
  });

  it('cada ruta pendiente muestra su ficha sin formularios, acciones ni solicitudes HTTP', async () => {
    const harness = await RouterTestingHarness.create();
    for (const report of POSTGRADUATE_REPORTS.filter(item => item.available !== true)) {
      await harness.navigateByUrl('/postgrado/' + report.slug);
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

  it('la encuesta docente abre la pantalla funcional en lugar de la ficha pendiente', async () => {
    const harness = await RouterTestingHarness.create('/postgrado/encuesta-docente-por-curso');
    await harness.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    const requests = http.match(request =>
      request.url.startsWith('/api/reports/postgraduate/teacher-survey/filters'));
    expect(requests.map(request => request.request.url))
      .toEqual(['/api/reports/postgraduate/teacher-survey/filters']);
    requests[0].flush({
      success: true, message: null,
      data: { periods: [], studentTypes: [], modalities: [] },
    });
    await harness.detectChanges();
    const root = harness.routeNativeElement!;
    expect(root.querySelector('h1')?.textContent?.trim()).toBe('Encuesta docente por curso');
    expect(root.textContent).not.toContain('Pendiente de migración');
    expect(root.querySelectorAll('ng-select[data-filter]')).toHaveLength(3);
    http.verify();
  });

  it('cursos vs alumnos abre la pantalla funcional en lugar de la ficha pendiente', async () => {
    const harness = await RouterTestingHarness.create('/postgrado/cursos-por-alumnos');
    await harness.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    const requests = http.match(request =>
      request.url.startsWith('/api/reports/postgraduate/courses-by-students/filters'));
    expect(requests.map(request => request.request.url))
      .toEqual(['/api/reports/postgraduate/courses-by-students/filters']);
    requests[0].flush({
      success: true, message: null,
      data: { periods: [], studentTypes: [], modalities: [] },
    });
    await harness.detectChanges();
    const root = harness.routeNativeElement!;
    expect(root.querySelector('h1')?.textContent?.trim()).toBe('Cursos vs. alumnos');
    expect(root.textContent).not.toContain('Pendiente de migración');
    expect(root.querySelectorAll('ng-select[data-filter]')).toHaveLength(3);
    http.verify();
  });

  it('inscripción masiva abre la pantalla funcional en lugar de la ficha pendiente', async () => {
    const harness = await RouterTestingHarness.create('/postgrado/inscripcion-masiva');
    await harness.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    const requests = http.match(request =>
      request.url.startsWith('/api/reports/postgraduate/mass-enrollment/filters'));
    expect(requests.map(request => request.request.url))
      .toEqual(['/api/reports/postgraduate/mass-enrollment/filters']);
    requests[0].flush({
      success: true, message: null,
      data: { periods: [], studentTypes: [], modalities: [], cohorts: [], courses: [] },
    });
    await harness.detectChanges();
    const root = harness.routeNativeElement!;
    expect(root.querySelector('h1')?.textContent?.trim()).toBe('Inscripción masiva');
    expect(root.textContent).not.toContain('Pendiente de migración');
    expect(root.querySelectorAll('ng-select')).toHaveLength(5);
    http.verify();
  });

  it('opens unanswered teacher survey on its existing catalogue slug', async () => {
    const harness = await RouterTestingHarness.create('/postgrado/alumnos-sin-contestar-encuesta');
    await harness.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    const request = http.expectOne('/api/reports/postgraduate/unanswered-teacher-survey/filters');
    request.flush({ success: true, message: null, data: { periods: [], studentTypes: [], modalities: [] } });
    await harness.detectChanges();
    expect(harness.routeNativeElement?.querySelector('h1')?.textContent?.trim())
      .toBe('Alumnos sin contestar encuesta docente');
    expect(harness.routeNativeElement?.querySelectorAll('ng-select[data-filter]')).toHaveLength(3);
    expect(harness.routeNativeElement?.textContent).not.toContain('Pendiente de migración');
  });

  it('opens teaching schedule with its postgraduate route and scoped period catalogue', async () => {
    const harness = await RouterTestingHarness.create('/postgrado/programacion-docente-programa-modalidad');
    await harness.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    const request = http.expectOne(request => request.url === '/api/reports/postgraduate/teaching-schedule/filters');
    expect(request.request.params.get('tipoPeriodo')).toBe('PERÍODO');
    request.flush({ success: true, message: null, data: { periodTypes: [], periods: [], programs: [], modalities: [], promotions: [] } });
    await harness.detectChanges();
    expect(harness.routeNativeElement?.querySelector('h1')?.textContent?.trim())
      .toBe('Programación Docente Postgrado X Programa y Modalidad');
    expect(harness.routeNativeElement?.querySelectorAll('ng-select[data-filter]')).toHaveLength(5);
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
