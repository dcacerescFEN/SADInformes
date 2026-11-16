import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { AuthService } from '../../core/auth/auth-service';
import { TeacherAvailability } from './teacher-availability';

const BASE = '/api/reports/academic-enrollment/teacher-availability';
const row = {
  code: 'ENECO660/01', course: 'ELECTIVO', teacherCode: 'P1', teacher: 'ANA PÉREZ',
  necessary: '2', given: '1', availability: 'Lun 10:00', createdAt: '2024-03-01',
  state: 'PENDIENTE', mail: 'ana@fen.uchile.cl', welcomeEmail: '0', observations: 'Sin observaciones',
};

describe('Disponibilidad Horaria', () => {
  let http: HttpTestingController;
  let router: Router;
  let hasEdit = false;
  let hasEmail = false;
  beforeEach(() => {
    hasEdit = false;
    hasEmail = false;
    TestBed.configureTestingModule({
      imports: [TeacherAvailability],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([]),
        { provide: AuthService, useValue: { hasPermission: (permission: string) =>
          (permission === 'REPORT_EDIT_AVAILABILITY' && hasEdit)
          || (permission === 'REPORT_SEND_AVAILABILITY_EMAIL' && hasEmail) } }],
    });
    http = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
  });
  afterEach(() => { http.verify(); TestBed.resetTestingModule(); });

  it('loads the active period, counters and table', async () => {
    const fixture = TestBed.createComponent(TeacherAvailability);
    fixture.detectChanges();
    http.expectOne(BASE + '/report').flush({
      success: true,
      data: { period: '2024A', periodName: '2024 Primer Semestre', rows: [row], pending: 1, ok: 0 },
    });
    await fixture.whenStable();
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;
    expect(root.textContent).toContain('2024 Primer Semestre');
    expect(root.textContent).toContain('Pendientes');
    expect(root.querySelectorAll('app-table thead th')).toHaveLength(8);
    expect(root.textContent).toContain('ANA PÉREZ');
    expect(root.textContent).toContain('Total: 1');
    expect(root.querySelector('[aria-label="Sin observaciones"]')?.classList.contains('btn-outline-warning')).toBe(true);
  });

  it('shows the administration button only with the edit permission', async () => {
    hasEdit = true;
    hasEmail = true;
    const fixture = TestBed.createComponent(TeacherAvailability);
    fixture.detectChanges();
    http.expectOne(BASE + '/report').flush({
      success: true,
      data: { period: '2024A', periodName: '2024 Primer Semestre', rows: [row], pending: 1, ok: 0 },
    });
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[data-action="administration"]')).not.toBeNull();
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('[title="Enviar correo"]')?.classList.contains('btn-outline-success')).toBe(true);
    expect(root.querySelector('[title="Editar disponibilidad"]')?.classList.contains('btn-outline-primary')).toBe(true);
  });

  it('hides observations without text and shows edit with the edit permission', async () => {
    hasEdit = true;
    const fixture = TestBed.createComponent(TeacherAvailability);
    fixture.detectChanges();
    http.expectOne(BASE + '/report').flush({
      success: true,
      data: {
        period: '2024A', periodName: '2024 Primer Semestre', pending: 1, ok: 0,
        rows: [{ ...row, observations: '' }],
      },
    });
    await fixture.whenStable();
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('[aria-label="Sin observaciones"]')).toBeNull();
    expect(root.querySelector('[title="Editar disponibilidad"]')?.classList.contains('btn-outline-primary')).toBe(true);
    expect(root.querySelector('[title="Eliminar disponibilidad"]')?.classList.contains('btn-outline-danger')).toBe(true);
    expect(root.querySelector('[title="Editar disponibilidad"]')?.classList.contains('btn-outline-primary')).toBe(true);
  });

  it('navigates to the editor with the teacher code and period', async () => {
    hasEdit = true;
    const navigate = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    const fixture = TestBed.createComponent(TeacherAvailability);
    fixture.detectChanges();
    http.expectOne(BASE + '/report').flush({
      success: true,
      data: { period: '2024A', periodName: '2024 Primer Semestre', rows: [row], pending: 1, ok: 0 },
    });
    await fixture.whenStable();
    fixture.detectChanges();
    const edit = fixture.nativeElement.querySelector('[title="Editar disponibilidad"]') as HTMLButtonElement;
    edit.click();
    expect(navigate).toHaveBeenCalledWith(
      ['/academic-enrollment/teacher-availability', 'P1', 'edit'],
      { queryParams: { period: '2024A' } });
  });

  it('sanitizes a failed load', async () => {
    const fixture = TestBed.createComponent(TeacherAvailability);
    fixture.detectChanges();
    http.expectOne(BASE + '/report').flush('private', { status: 503, statusText: 'private' });
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No se pudo cargar la disponibilidad.');
    expect(fixture.nativeElement.textContent).not.toContain('private');
  });
});
