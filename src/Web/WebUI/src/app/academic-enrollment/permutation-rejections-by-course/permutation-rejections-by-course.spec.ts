import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { PermutationRejectionsByCourse } from './permutation-rejections-by-course';

const BASE = '/api/reports/academic-enrollment/permutation-rejections-by-course';
const row = {
  courseCode: 'ENECO660', courseName: 'Electivo de mención', section: '01',
  studentType: 'IC', rejections: 3,
};

describe('Permutas rechazadas por vacantes', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [PermutationRejectionsByCourse],
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
    http.expectOne(request => request.url === BASE + '/report'
      && request.params.get('period') === '2024A').flush({
      success: true, data: { rows: [row], total: 1 },
    });
    await fixture.whenStable();
    fixture.detectChanges();
  }

  it('loads the period and renders the five legacy columns', async () => {
    const fixture = TestBed.createComponent(PermutationRejectionsByCourse);
    await loadReport(fixture);
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('h1')?.textContent?.trim())
      .toContain('Permutas rechazadas por vacantes');
    expect(root.querySelectorAll('app-table thead th')).toHaveLength(5);
    expect(root.querySelectorAll('app-table tbody tr')).toHaveLength(1);
    expect(root.textContent).toContain('ENECO660');
    expect(root.textContent).toContain('3');
    expect(root.textContent).toContain('Total: 1');
    expect(root.querySelector('app-table tbody td.text-end')?.textContent?.trim()).toBe('3');
  });

  it('shows the empty state and keeps the results section', async () => {
    const fixture = TestBed.createComponent(PermutationRejectionsByCourse);
    fixture.detectChanges();
    http.expectOne(BASE + '/filters').flush({
      success: true, data: { periods: [{ value: '2024A', text: '2024 Primer Semestre' }] },
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
    const fixture = TestBed.createComponent(PermutationRejectionsByCourse);
    fixture.detectChanges();
    http.expectOne(BASE + '/filters').flush({
      success: true, data: { periods: [{ value: '2024A', text: '2024 Primer Semestre' }] },
    });
    fixture.detectChanges();
    http.expectOne(request => request.url === BASE + '/report').flush('private', { status: 503, statusText: 'private' });
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No se pudo cargar el informe.');
    expect(fixture.nativeElement.textContent).not.toContain('private');
  });
});
