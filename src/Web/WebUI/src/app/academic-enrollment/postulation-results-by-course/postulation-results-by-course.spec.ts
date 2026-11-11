import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { PostulationResultsByCourse } from './postulation-results-by-course';

const BASE = '/api/reports/academic-enrollment/postulation-results-by-course';
const row = {
  course: 'ENECO660/01', courseName: 'Electivo de mención', studentType: 'IC',
  originalVacancies: 30, otherAccepted: 2, processVacancies: 28, postulations: 10,
  accepted: 8, acceptedProcess: 8, totalOccupied: 10, availableVacancies: 20,
  vacancyRejections: 1, realVacancyRejections: 1, scheduleCapRejections: 1,
  loadRejections: 0, requirementRejections: 0, otherRejections: 1,
};

describe('Resultados de postulación por curso', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [PostulationResultsByCourse],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => { http.verify(); TestBed.resetTestingModule(); });

  async function loadReport(fixture: { detectChanges: () => void; whenStable: () => Promise<unknown> }) {
    fixture.detectChanges();
    http.expectOne(BASE + '/filters').flush({
      success: true, data: { periods: [{ value: '2024A', text: '2024 Primer Semestre' }] },
    });
    fixture.detectChanges();
    http.expectOne(request => request.url === BASE + '/processes'
      && request.params.get('origin') === 'INSCRIPCION'
      && request.params.get('period') === '2024A').flush({
      success: true,
      data: { processes: [
        { value: '1', text: 'Postulación', start: '2024-01-01T08:00', end: '2024-01-05T18:00' },
      ] },
    });
    fixture.detectChanges();
    http.expectOne(request => request.url === BASE + '/report'
      && request.params.get('origin') === 'INSCRIPCION'
      && request.params.get('period') === '2024A'
      && request.params.get('from') === '2024-01-01T08:00'
      && request.params.get('to') === '2024-01-05T18:00').flush({
      success: true, data: { rows: [row], total: 1 },
    });
    await fixture.whenStable();
    fixture.detectChanges();
  }

  it('loads filters, process range, dates and the report', async () => {
    const fixture = TestBed.createComponent(PostulationResultsByCourse);
    await loadReport(fixture);
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('h1')?.textContent?.trim())
      .toContain('Resultados Procesamiento Masivo Postulación');
    expect((root.querySelector('#postulation-from') as HTMLInputElement).value).toBe('2024-01-01T08:00');
    expect(root.querySelectorAll('app-table thead th')).toHaveLength(17);
    expect(root.querySelectorAll('app-table tbody tr')).toHaveLength(1);
    expect(root.textContent).toContain('ENECO660/01');
    expect(root.textContent).toContain('Total: 1');
    expect(root.textContent).toContain('Rec. Otros incluye');
    expect(root.querySelectorAll('app-table tbody td.cell-warning-highlight')).toHaveLength(2);
  });

  it('shows the empty state and keeps the results section', async () => {
    const fixture = TestBed.createComponent(PostulationResultsByCourse);
    fixture.detectChanges();
    http.expectOne(BASE + '/filters').flush({
      success: true, data: { periods: [{ value: '2024A', text: '2024 Primer Semestre' }] },
    });
    fixture.detectChanges();
    http.expectOne(request => request.url === BASE + '/processes').flush({
      success: true, data: { processes: [{ value: '1', text: 'Postulación', start: '2024-01-01T08:00', end: '2024-01-05T18:00' }] },
    });
    fixture.detectChanges();
    http.expectOne(request => request.url === BASE + '/report').flush({ success: true, data: { rows: [], total: 0 } });
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No se encontraron registros');
    expect(fixture.nativeElement.querySelector('app-table')).not.toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Total: 0');
  });

  it('sanitizes a failed report', async () => {
    const fixture = TestBed.createComponent(PostulationResultsByCourse);
    fixture.detectChanges();
    http.expectOne(BASE + '/filters').flush({
      success: true, data: { periods: [{ value: '2024A', text: '2024 Primer Semestre' }] },
    });
    fixture.detectChanges();
    http.expectOne(request => request.url === BASE + '/processes').flush({
      success: true, data: { processes: [{ value: '1', text: 'Postulación', start: '2024-01-01T08:00', end: '2024-01-05T18:00' }] },
    });
    fixture.detectChanges();
    http.expectOne(request => request.url === BASE + '/report').flush('private', { status: 503, statusText: 'private' });
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No se pudo cargar el informe.');
    expect(fixture.nativeElement.textContent).not.toContain('private');
  });
});
