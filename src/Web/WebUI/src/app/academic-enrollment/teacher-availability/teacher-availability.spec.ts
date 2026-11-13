import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TeacherAvailability } from './teacher-availability';

const BASE = '/api/reports/academic-enrollment/teacher-availability';
const row = {
  code: 'ENECO660/01', course: 'ELECTIVO', teacherCode: 'P1', teacher: 'ANA PÉREZ',
  necessary: '2', given: '1', availability: 'Lun 10:00', createdAt: '2024-03-01',
  state: 'PENDIENTE', mail: 'ana@fen.uchile.cl', welcomeEmail: '0', observations: 'Sin observaciones',
};

describe('Disponibilidad Horaria', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [TeacherAvailability],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
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
