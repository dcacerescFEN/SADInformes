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
    expect(POSTGRADUATE_REPORTS.slice(0, 29).every(report => report.legacyPath.startsWith('Informes/Postgrado/'))).toBe(true);
    expect(POSTGRADUATE_REPORTS[29].legacyPath).toBe('Informes/Inscripcion/ProgramacionXProgramaModalidad.aspx');
    expect(POSTGRADUATE_REPORTS.some(report => report.legacyPath.includes('DetalleProfesor'))).toBe(false);
    expect(POSTGRADUATE_REPORTS.some(report => report.legacyPath.endsWith('EstadisticasISS/Resumen.aspx'))).toBe(true);
    expect(POSTGRADUATE_REPORTS.some(report => report.legacyPath.endsWith('EncuestaSalidaGraduados/Default.aspx'))).toBe(true);
  });

  it('cada ruta pendiente muestra su ficha sin formularios, acciones ni solicitudes HTTP', async () => {
    const harness = await RouterTestingHarness.create();
    for (const report of POSTGRADUATE_REPORTS.filter(item => item.available !== true)) {
      await harness.navigateByUrl('/postgraduate/' + report.slug);
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
    const harness = await RouterTestingHarness.create('/postgraduate/teacher-survey');
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
    const harness = await RouterTestingHarness.create('/postgraduate/courses-by-students');
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
    const harness = await RouterTestingHarness.create('/postgraduate/mass-enrollment');
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
    const harness = await RouterTestingHarness.create('/postgraduate/unanswered-teacher-survey');
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
    const harness = await RouterTestingHarness.create('/postgraduate/teaching-schedule');
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

  it('opens student requests using its existing slug and four SSRS filters', async () => {
    const harness = await RouterTestingHarness.create('/postgraduate/student-requests');
    await harness.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(request => request.url === '/api/reports/postgraduate/student-requests/filters')
      .flush({ success: true, data: { studentTypes: [], modalities: [], cohorts: [], states: [] } });
    await harness.detectChanges();
    expect(harness.routeNativeElement?.querySelector('h1')?.textContent?.trim()).toBe('Solicitudes alumno');
    expect(harness.routeNativeElement?.querySelectorAll('ng-select[data-filter]')).toHaveLength(4);
    expect(harness.routeNativeElement?.textContent).not.toContain('Pendiente de migración');
  });

  it('opens SAD VTI audit with the original slug and direct date-filtered export', async () => {
    const harness = await RouterTestingHarness.create('/postgraduate/sad-vti-updates');
    await harness.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    http.expectOne('/api/reports/postgraduate/sad-vti-updates/filters')
      .flush({ success: true, data: { states: [{ value: '-1', text: 'Todos' }] } });
    await harness.detectChanges();
    const root = harness.routeNativeElement!;
    expect(root.querySelector('h1')?.textContent?.trim()).toBe('Actualización datos SAD–VTI');
    expect(root.querySelectorAll('input[type=date]')).toHaveLength(2);
    expect(root.querySelectorAll('ng-select')).toHaveLength(1);
    expect(root.textContent).toContain('Descargar Excel');
    expect(root.textContent).not.toContain('Pendiente de migración');
    expect(root.querySelector('iframe')).toBeNull();
  });

  it('opens weighted ranking with concrete filters and no placeholder', async () => {
    const harness = await RouterTestingHarness.create('/postgraduate/weighted-ranking');
    await harness.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(request => request.url === '/api/reports/postgraduate/weighted-ranking/filters')
      .flush({ success: true, data: { studentTypes: [{ value: 'MBA', text: 'MBA' }], modalities: ['PT'], years: ['2026'] } });
    await harness.detectChanges();
    const root = harness.routeNativeElement!;
    expect(root.querySelector('h1')?.textContent?.trim()).toBe('Ranking Notas Ponderado');
    expect(root.querySelectorAll('ng-select[data-filter]')).toHaveLength(4);
    expect(root.textContent).not.toContain('Pendiente de migración');
    http.verify();
  });

  it('opens basic right payment with five filters and no placeholder', async () => {
    const harness = await RouterTestingHarness.create('/postgraduate/basic-right-payment');
    await harness.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(request => request.url === '/api/reports/postgraduate/basic-right-payment/filters')
      .flush({ success: true, data: {
        studentTypes: [{ value: 'MBA', text: 'MBA' }], modalities: [{ value: 'PT', text: 'PT' }],
        years: [{ value: '2024', text: '2024' }],
        paymentStatuses: [{ value: 'Todos', text: 'Todos' }],
      } });
    await harness.detectChanges();
    const root = harness.routeNativeElement!;
    expect(root.querySelector('h1')?.textContent?.trim()).toBe('Pago Derecho Básico Anual');
    expect(root.querySelectorAll('ng-select[data-filter]')).toHaveLength(5);
    expect(root.textContent).not.toContain('Pendiente de migración');
    http.verify();
  });

  it('opens the read-only NCE report and no placeholder', async () => {
    const harness = await RouterTestingHarness.create('/postgraduate/nce');
    await harness.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(request => request.url === '/api/reports/postgraduate/nce/report')
      .flush({ success: true, data: { total: 0, rows: [] } });
    await harness.detectChanges();
    const root = harness.routeNativeElement!;
    expect(root.querySelector('h1')?.textContent?.trim()).toBe('Informe Notificaciones Causal de Eliminación');
    expect(root.querySelectorAll('[data-tab]')).toHaveLength(0);
    expect(root.textContent).not.toContain('Pendiente de migración');
    http.verify();
  });

  it('opens course activities with year filter, auto-loaded table and no placeholder', async () => {
    const harness = await RouterTestingHarness.create('/postgraduate/course-activities');
    await harness.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(request => request.url === '/api/reports/postgraduate/course-activities/filters')
      .flush({ success: true, data: { years: [{ value: '2024', text: '2024' }] } });
    await harness.detectChanges();
    http.expectOne(request => request.url === '/api/reports/postgraduate/course-activities/report')
      .flush({ success: true, data: { year: '2024', total: 0, rows: [] } });
    await harness.detectChanges();
    const root = harness.routeNativeElement!;
    expect(root.querySelector('h1')?.textContent?.trim()).toBe('Actividades Cursos Postgrado');
    expect(root.querySelector('ng-select[data-filter=year]')).not.toBeNull();
    expect(root.textContent).not.toContain('Pendiente de migración');
    http.verify();
  });

  it('opens licenciatura with on-demand search and no placeholder', async () => {
    const harness = await RouterTestingHarness.create('/postgraduate/undergraduate-licenciatura');
    await harness.detectChanges();
    const root = harness.routeNativeElement!;
    expect(root.querySelector('h1')?.textContent?.trim()).toBe('Próximas Licenciaturas Pregrado');
    expect(root.querySelector('[data-action=search]')).not.toBeNull();
    expect(root.textContent).not.toContain('Pendiente de migración');
    TestBed.inject(HttpTestingController).verify();
  });

  it('opens AFE report with five filters and no placeholder', async () => {
    const harness = await RouterTestingHarness.create('/postgraduate/afe-report');
    await harness.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(request => request.url === '/api/reports/postgraduate/afe-report/filters')
      .flush({ success: true, data: {
        periods: [{ value: '2024A', text: '2024' }], studentTypes: [{ value: 'Todos', text: 'Todos' }],
        modalities: [{ value: 'Todos', text: 'Todos' }], courses: [{ value: 'ENECO775/1', text: 'ENECO775/1' }],
      } });
    await harness.detectChanges();
    http.expectOne(request => request.url === '/api/reports/postgraduate/afe-report/advances')
      .flush({ success: true, data: { advances: [{ value: '-1', text: 'Todos' }] } });
    await harness.detectChanges();
    const root = harness.routeNativeElement!;
    expect(root.querySelector('h1')?.textContent?.trim()).toBe('Reporte Avances AFE');
    expect(root.querySelectorAll('ng-select[data-filter]')).toHaveLength(5);
    expect(root.textContent).not.toContain('Pendiente de migración');
    http.verify();
  });

  it('opens Alumni survey statistics with the shared dictionary and no placeholder', async () => {
    const harness = await RouterTestingHarness.create('/postgraduate/alumni-survey-statistics');
    await harness.detectChanges();
    const root = harness.routeNativeElement!;
    expect(root.querySelector('h1')?.textContent?.trim()).toBe('Estadísticas Encuesta Alumni');
    expect(root.querySelector('app-table')).not.toBeNull();
    expect(root.textContent).not.toContain('Pendiente de migración');
    TestBed.inject(HttpTestingController).verify();
  });

  it('opens ISS statistics with two dependent filters and no placeholder', async () => {
    const harness = await RouterTestingHarness.create('/postgraduate/iss-statistics');
    await harness.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(request => request.url === '/api/reports/postgraduate/iss-statistics/filters')
      .flush({ success: true, data: { periods: [{ value: '2024A', text: '2024' }], courseSections: [{ value: 'ENECO775/1', text: 'Economía' }] } });
    await harness.detectChanges();
    const root = harness.routeNativeElement!;
    expect(root.querySelector('h1')?.textContent?.trim()).toBe('Estadísticas ISS');
    expect(root.querySelectorAll('ng-select[data-filter]')).toHaveLength(2);
    expect(root.textContent).not.toContain('Pendiente de migración');
    http.verify();
  });

  it('opens thesis registration with three filters and no placeholder', async () => {
    const harness = await RouterTestingHarness.create('/postgraduate/thesis-registration');
    await harness.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(request => request.url === '/api/reports/postgraduate/thesis-registration/filters')
      .flush({ success: true, data: { studentTypes: [{ value: 'Todos', text: 'Todos' }], modalities: ['Todos'], years: ['Todos'] } });
    await harness.detectChanges();
    const root = harness.routeNativeElement!;
    expect(root.querySelector('h1')?.textContent?.trim()).toBe('Registro de tesis');
    expect(root.querySelectorAll('ng-select[data-filter]')).toHaveLength(3);
    expect(root.textContent).not.toContain('Pendiente de migración');
    http.verify();
  });

  it('opens personal data with three filters and no placeholder', async () => {
    const harness = await RouterTestingHarness.create('/postgraduate/personal-data');
    await harness.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(request => request.url === '/api/reports/postgraduate/personal-data/filters')
      .flush({ success: true, data: { studentTypes: [{ value: 'Todos', text: 'Todos' }], modalities: ['Todos'], years: ['Todos'] } });
    await harness.detectChanges();
    const root = harness.routeNativeElement!;
    expect(root.querySelector('h1')?.textContent?.trim()).toBe('Datos Personales');
    expect(root.querySelectorAll('ng-select[data-filter]')).toHaveLength(3);
    expect(root.textContent).not.toContain('Pendiente de migración');
    http.verify();
  });

  it('opens course teacher evaluation with year/type filters and no placeholder', async () => {
    const harness = await RouterTestingHarness.create('/postgraduate/course-teacher-evaluation');
    await harness.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(request => request.url === '/api/reports/postgraduate/course-teacher-evaluation/filters')
      .flush({ success: true, data: { years: ['2026'], types: ['EX', 'FT'] } });
    await harness.detectChanges();
    const root = harness.routeNativeElement!;
    expect(root.querySelector('h1')?.textContent?.trim()).toBe('Evaluación Docente/Curso');
    expect(root.querySelectorAll('ng-select[data-filter]')).toHaveLength(2);
    expect(root.textContent).not.toContain('Pendiente de migración');
    http.verify();
  });

  it('opens entry survey statistics with its question dictionary and no placeholder', async () => {
    const harness = await RouterTestingHarness.create('/postgraduate/entry-survey-statistics');
    await harness.detectChanges();
    const root = harness.routeNativeElement!;
    expect(root.querySelector('h1')?.textContent?.trim()).toBe('Estadísticas Encuesta Entrada');
    expect(root.querySelectorAll('tbody tr')).toHaveLength(40);
    expect(root.textContent).not.toContain('Pendiente de migración');
    TestBed.inject(HttpTestingController).expectNone(() => true);
  });

  it('opens Sigma tutorships with its year filter and no placeholder', async () => {
    const harness = await RouterTestingHarness.create('/postgraduate/sigma-tutorships');
    await harness.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(request => request.url === '/api/reports/postgraduate/ayudantias-sigma/filters')
      .flush({ success: true, data: { years: ['2026'] } });
    await harness.detectChanges();
    const root = harness.routeNativeElement!;
    expect(root.querySelector('h1')?.textContent?.trim()).toBe('Reporte Ayudantías Postgrado Sigma');
    expect(root.querySelectorAll('ng-select[data-filter]')).toHaveLength(1);
    expect(root.textContent).not.toContain('Pendiente de migración');
    http.verify();
  });

  it('opens thesis defenses with four Todos filters and no placeholder', async () => {
    const harness = await RouterTestingHarness.create('/postgraduate/thesis-defenses');
    await harness.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(request => request.url === '/api/reports/postgraduate/thesis-defenses/filters')
      .flush({ success: true, data: { years: ['2026'], studentTypes: ['Todos', 'MBA'], modalities: ['Todos'] } });
    await harness.detectChanges();
    const root = harness.routeNativeElement!;
    expect(root.querySelector('h1')?.textContent?.trim()).toBe('Reporte Defensas de Tesis Postgrado');
    expect(root.querySelectorAll('ng-select[data-filter]')).toHaveLength(4);
    expect(root.textContent).not.toContain('Pendiente de migración');
    http.verify();
  });

  it('opens SAD Sigma with one year filter and no placeholder', async () => {
    const harness = await RouterTestingHarness.create('/postgraduate/sad-sigma');
    await harness.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(request => request.url === '/api/reports/postgraduate/sad-sigma/filters')
      .flush({ success: true, data: { years: ['2026', '2024'] } });
    await harness.detectChanges();
    const root = harness.routeNativeElement!;
    expect(root.querySelector('h1')?.textContent?.trim()).toBe('Reporte Vista Sad Sigma');
    expect(root.querySelectorAll('ng-select[data-filter]')).toHaveLength(1);
    expect(root.textContent).not.toContain('Pendiente de migración');
    http.verify();
  });

  it('opens area courses with two year filters and no placeholder', async () => {
    const harness = await RouterTestingHarness.create('/postgraduate/area-courses');
    await harness.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(request => request.url === '/api/reports/postgraduate/area-courses/filters')
      .flush({ success: true, data: { years: ['2026', '2024'] } });
    await harness.detectChanges();
    const root = harness.routeNativeElement!;
    expect(root.querySelector('h1')?.textContent?.trim()).toBe('Reporte Cátedras Áreas');
    expect(root.querySelectorAll('ng-select[data-filter]')).toHaveLength(2);
    expect(root.textContent).not.toContain('Pendiente de migración');
    http.verify();
  });

  it('opens teacher validities with its year filter and no placeholder', async () => {
    const harness = await RouterTestingHarness.create('/postgraduate/teacher-validities');
    await harness.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(request => request.url === '/api/reports/postgraduate/teacher-validities/filters')
      .flush({ success: true, data: { years: ['2026', '2024'] } });
    await harness.detectChanges();
    const root = harness.routeNativeElement!;
    expect(root.querySelector('h1')?.textContent?.trim()).toBe('Profesores Confirmados x Cátedra');
    expect(root.querySelectorAll('ng-select[data-filter]')).toHaveLength(1);
    expect(root.textContent).not.toContain('Pendiente de migración');
    http.verify();
  });

  it('opens enrolled students with the legacy export-only page and no placeholder', async () => {
    const harness = await RouterTestingHarness.create('/postgraduate/enrolled-students');
    await harness.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(request => request.url === '/api/reports/postgraduate/enrolled-students/filters')
      .flush({ success: true, data: { modalities: [{ value: 'SUNRISE', text: 'SUNRISE' }], years: ['2026'] } });
    await harness.detectChanges();
    const root = harness.routeNativeElement!;
    expect(root.querySelector('h1')?.textContent?.trim()).toBe('Alumnos Matriculados MBA');
    expect(root.querySelectorAll('ng-select[data-filter]')).toHaveLength(2);
    expect(root.querySelector('table')).toBeNull();
    expect(root.textContent).not.toContain('Pendiente de migración');
    http.verify();
  });

  it('opens exit survey enrollment with automatic listing and no placeholder', async () => {
    const harness = await RouterTestingHarness.create('/postgraduate/exit-survey-enrollment');
    await harness.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(request => request.url === '/api/reports/postgraduate/exit-survey-enrollment/filters')
      .flush({ success: true, data: { studentTypes: [{ value: 'Todos', text: 'Todos' }], modalities: [{ value: 'Todos', text: 'Todos' }], years: ['2024'] } });
    await harness.detectChanges();
    await Promise.resolve(); await Promise.resolve();
    await harness.detectChanges();
    http.expectOne(request => request.url === '/api/reports/postgraduate/exit-survey-enrollment')
      .flush({ success: true, data: { rows: [], total: 0, canEnroll: true } });
    await harness.detectChanges();
    const root = harness.routeNativeElement!;
    expect(root.querySelector('h1')?.textContent?.trim()).toBe('Inscripción Encuesta Salida');
    expect(root.querySelectorAll('ng-select')).toHaveLength(3);
    expect(root.textContent).toContain('No hay resultados');
    expect(root.textContent).not.toContain('Pendiente de migración');
  });

  it('opens exit survey statistics shell on its pending slug with 14 tabs', async () => {
    const harness = await RouterTestingHarness.create('/postgraduate/exit-survey-statistics');
    await harness.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(request => request.url === '/api/reports/postgraduate/exit-survey-statistics/filters')
      .flush({ success: true, data: { periods: [{ value: '2024A', text: '2024' }], studentTypes: [{ value: 'Todos', text: 'Todos' }], modalities: [{ value: 'Todos', text: 'Todos' }] } });
    await harness.detectChanges(); await Promise.resolve(); await harness.detectChanges();
    http.expectOne(request => request.url === '/api/reports/postgraduate/exit-survey-statistics/sample')
      .flush({ success: true, data: { rows: [], totals: { sample: 0, inProgress: 0, answered: 0 } } });
    await harness.detectChanges();
    http.expectOne(request => request.url === '/api/reports/postgraduate/exit-survey-statistics/likert')
      .flush({ success: true, data: { section: 'malla', title: 'Malla Curricular', rows: [], totals: { alternative1: 0, alternative2: 0, alternative3: 0, alternative4: 0, alternative5: 0 } } });
    await harness.detectChanges();
    const root = harness.routeNativeElement!;
    expect(root.querySelector('h1')?.textContent?.trim()).toBe('Estadísticas Encuesta de Salida');
    expect(root.querySelectorAll('[data-top]')).toHaveLength(5); expect(root.querySelectorAll('[data-tab]')).toHaveLength(8);
    expect(root.textContent).toContain('No hay resultados');
    expect(root.textContent).not.toContain('Pendiente de migración');
  });

  it('no conserva rutas antiguas de Postgrado', () => {
    expect(routes.some(route => route.path?.startsWith('postgrado'))).toBe(false);
  });

  it('conserva Inicio y redirige las opciones desconocidas al inicio', async () => {
    const harness = await RouterTestingHarness.create('/postgraduate/no-existe');
    expect(TestBed.inject(Router).url).toBe('/');
    expect(harness.routeNativeElement?.querySelector('h1')?.textContent).toBe('SADInformes');
  });

  it('no permite abrir una ficha sin sesión e inicia el login', async () => {
    auth.isAuthenticated.mockReturnValue(false);
    const harness = await RouterTestingHarness.create();
    const target = '/postgraduate/' + POSTGRADUATE_REPORTS[0].slug;
    await harness.navigateByUrl(target);
    expect(auth.login).toHaveBeenCalledExactlyOnceWith(target);
    expect(harness.routeNativeElement?.querySelector('h1')).toBeFalsy();
  });
});
