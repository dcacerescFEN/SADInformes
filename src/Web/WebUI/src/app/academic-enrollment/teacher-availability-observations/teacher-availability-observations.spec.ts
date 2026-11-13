import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TeacherAvailabilityObservations } from './teacher-availability-observations';

const BASE = '/api/reports/academic-enrollment/teacher-availability';
const periods = [
  { value: '2024A', text: '2024 Primer Semestre' },
  { value: '2023B', text: '2023 Segundo Semestre' },
];
const row = { teacherCode: 'P1', teacher: 'ANA PÉREZ', observations: 'Requiere sala especial' };

describe('Disponibilidad Observaciones', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [TeacherAvailabilityObservations],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => { http.verify(); TestBed.resetTestingModule(); });

  it('loads periods and observations', async () => {
    const fixture = TestBed.createComponent(TeacherAvailabilityObservations);
    fixture.detectChanges();
    http.expectOne(BASE + '/observations').flush({
      success: true, data: { period: '2024A', periods, rows: [row] },
    });
    await fixture.whenStable();
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelectorAll('app-table thead th')).toHaveLength(3);
    expect(root.textContent).toContain('ANA PÉREZ');
    expect(root.textContent).toContain('Requiere sala especial');
    expect(root.textContent).toContain('Total: 1');
  });

  it('reloads when the period changes', async () => {
    const fixture = TestBed.createComponent(TeacherAvailabilityObservations);
    fixture.detectChanges();
    http.expectOne(BASE + '/observations').flush({
      success: true, data: { period: '2024A', periods, rows: [row] },
    });
    await fixture.whenStable();
    fixture.detectChanges();
    fixture.componentInstance.form.controls.period.setValue('2023B');
    fixture.detectChanges();
    http.expectOne(request => request.url === BASE + '/observations'
      && request.params.get('period') === '2023B').flush({
      success: true, data: { period: '2023B', periods, rows: [] },
    });
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No se encontraron registros');
  });
});
