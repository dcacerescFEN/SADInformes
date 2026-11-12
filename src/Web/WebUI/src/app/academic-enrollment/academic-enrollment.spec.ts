import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { routes } from '../app.routes';
import { AuthService } from '../core/auth/auth-service';
import { ACADEMIC_ENROLLMENT_REPORTS } from './academic-enrollment-catalogue';

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
    expect(ACADEMIC_ENROLLMENT_REPORTS).toHaveLength(22);
    expect(new Set(ACADEMIC_ENROLLMENT_REPORTS.map(report => report.slug)).size).toBe(22);
    expect(ACADEMIC_ENROLLMENT_REPORTS.every(report =>
      report.legacyPath.startsWith('Informes/Inscripcion/'))).toBe(true);
    expect(ACADEMIC_ENROLLMENT_REPORTS.find(report => report.slug === 'electives-without-common-plan')?.available)
      .toBe(true);
    expect(ACADEMIC_ENROLLMENT_REPORTS.find(report => report.slug === 'students-without-priority')?.available)
      .toBe(true);
    expect(ACADEMIC_ENROLLMENT_REPORTS.find(report => report.slug === 'postulation-results-by-course')?.available)
      .toBe(true);
    expect(ACADEMIC_ENROLLMENT_REPORTS.find(report => report.slug === 'add-results-by-course')?.available)
      .toBe(true);
    expect(ACADEMIC_ENROLLMENT_REPORTS.find(report => report.slug === 'permutation-results-by-student')?.available)
      .toBe(true);
    expect(ACADEMIC_ENROLLMENT_REPORTS.find(report => report.slug === 'permutation-rejections-by-course')?.available)
      .toBe(true);
    expect(ACADEMIC_ENROLLMENT_REPORTS.find(report => report.slug === 'students-completed-free-courses')?.available)
      .toBe(true);
    expect(ACADEMIC_ENROLLMENT_REPORTS.find(report => report.slug === 'students-completed-electives')?.available)
      .toBe(true);
    expect(ACADEMIC_ENROLLMENT_REPORTS.find(report => report.slug === 'automatic-mention-changes')?.available)
      .toBe(true);
    expect(ACADEMIC_ENROLLMENT_REPORTS.find(report => report.slug === 'enrolled-approved-courses')?.available)
      .toBe(true);
    expect(ACADEMIC_ENROLLMENT_REPORTS.find(report => report.slug === 'enrolled-without-requirements')?.available)
      .toBe(true);
    expect(ACADEMIC_ENROLLMENT_REPORTS.find(report => report.slug === 'failed-mandatory-not-enrolled')?.available)
      .toBe(true);
    expect(ACADEMIC_ENROLLMENT_REPORTS.find(report => report.slug === 'enrollment-teaching-schedule')?.available)
      .toBe(true);
    for (const slug of ['teaching-schedule-ic-old-plan', 'teaching-schedule-ic-new-plan',
      'teaching-schedule-iicg-old-plan', 'teaching-schedule-iicg-new-plan']) {
      expect(ACADEMIC_ENROLLMENT_REPORTS.find(report => report.slug === slug)?.available).toBe(true);
    }
    expect(ACADEMIC_ENROLLMENT_REPORTS.find(report => report.slug === 'postgraduate-teaching-schedule')?.routePath)
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

  it('carga el informe funcional de alumnos sin prioridad', async () => {
    const harness = await RouterTestingHarness.create(
      '/academic-enrollment/students-without-priority');
    const http = TestBed.inject(HttpTestingController);
    http.expectOne('/api/reports/academic-enrollment/students-without-priority/report')
      .flush({ success: true, data: { rows: [
        { studentCode: 'A/1', name: 'Ana Pérez', academicStatus: 'Regular', studentType: 'IC' },
      ], total: 1 } });
    await harness.detectChanges();
    const root = harness.routeNativeElement!;
    expect(root.querySelector('h1')?.textContent?.trim())
      .toBe('Alumnos que pueden postular y no tienen prioridad');
    expect(root.querySelectorAll('app-table tbody tr')).toHaveLength(1);
    expect(root.textContent).toContain('Ana Pérez');
    expect(root.textContent).not.toContain('Pendiente de migración');
  });

  it('cada opción pendiente abre su ficha local sin formularios ni solicitudes HTTP', async () => {
    const harness = await RouterTestingHarness.create();
    for (const report of ACADEMIC_ENROLLMENT_REPORTS.filter(item => !item.routePath && item.available !== true)) {
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
