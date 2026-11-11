import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { PermutationResultsByStudent } from './permutation-results-by-student';

const BASE = '/api/reports/academic-enrollment/permutation-results-by-student';
const row = {
  studentCode: 'A/1', name: 'Ana Pérez', studentType: 'IC', courseCode: 'ENECO660',
  courseName: 'Electivo de mención', fromSection: '01', toSection: '02', result: 'ACEPTADA',
};

describe('Resultados de permuta por alumno', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [PermutationResultsByStudent],
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
      && request.params.get('period') === '2024A').flush({
      success: true, data: { processes: [
        { value: '1', text: 'Permuta', start: '2024-01-01T08:00', end: '2024-01-05T18:00' },
      ] },
    });
    fixture.detectChanges();
    http.expectOne(request => request.url === BASE + '/report'
      && request.params.get('period') === '2024A'
      && request.params.get('from') === '2024-01-01T08:00'
      && request.params.get('to') === '2024-01-05T18:00').flush({
      success: true, data: { rows: [row], total: 1 },
    });
    await fixture.whenStable();
    fixture.detectChanges();
  }

  it('loads filters, process range, dates and the report', async () => {
    const fixture = TestBed.createComponent(PermutationResultsByStudent);
    await loadReport(fixture);
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('h1')?.textContent?.trim())
      .toContain('Resultados Procesamiento Masivo Permuta');
    expect((root.querySelector('#permutation-from') as HTMLInputElement).value).toBe('2024-01-01T08:00');
    expect(root.querySelectorAll('app-table thead th')).toHaveLength(8);
    expect(root.querySelectorAll('app-table tbody tr')).toHaveLength(1);
    expect(root.textContent).toContain('Ana Pérez');
    expect(root.textContent).toContain('ACEPTADA');
    expect(root.textContent).toContain('Total: 1');
  });

  it('shows the empty state and keeps the results section', async () => {
    const fixture = TestBed.createComponent(PermutationResultsByStudent);
    fixture.detectChanges();
    http.expectOne(BASE + '/filters').flush({
      success: true, data: { periods: [{ value: '2024A', text: '2024 Primer Semestre' }] },
    });
    fixture.detectChanges();
    http.expectOne(request => request.url === BASE + '/processes').flush({
      success: true, data: { processes: [{ value: '1', text: 'Permuta', start: '2024-01-01T08:00', end: '2024-01-05T18:00' }] },
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
    const fixture = TestBed.createComponent(PermutationResultsByStudent);
    fixture.detectChanges();
    http.expectOne(BASE + '/filters').flush({
      success: true, data: { periods: [{ value: '2024A', text: '2024 Primer Semestre' }] },
    });
    fixture.detectChanges();
    http.expectOne(request => request.url === BASE + '/processes').flush({
      success: true, data: { processes: [{ value: '1', text: 'Permuta', start: '2024-01-01T08:00', end: '2024-01-05T18:00' }] },
    });
    fixture.detectChanges();
    http.expectOne(request => request.url === BASE + '/report').flush('private', { status: 503, statusText: 'private' });
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No se pudo cargar el informe.');
    expect(fixture.nativeElement.textContent).not.toContain('private');
  });
});
