import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { CourseLoadReduction } from './course-load-reduction';

const BASE = '/api/reports/academic-enrollment/course-load-reduction';
const row = {
  period: '2024A', courseCode: 'ENECO660', section: '01', courseName: 'ELECTIVO',
  studentCode: 'A/1', personCode: 'P1', decree: '00921/89', name: 'ANA PÉREZ',
  origin: 'REDUCCION DE CARGA', movementType: 'ELIMINA', state: 'ACEPTADA',
};

describe('Reducción de carga', () => {
  let http: HttpTestingController;
  let downloads: HTMLAnchorElement[];
  const createUrl = vi.fn(() => 'blob:reduction');
  const revokeUrl = vi.fn();

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [CourseLoadReduction],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
    downloads = [];
    createUrl.mockClear(); revokeUrl.mockClear();
    const NativeURL = URL;
    vi.stubGlobal('URL', class extends NativeURL {
      static override createObjectURL = createUrl;
      static override revokeObjectURL = revokeUrl;
    });
    vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(function (this: HTMLAnchorElement) {
      downloads.push(this);
    });
  });

  afterEach(() => { http.verify(); vi.restoreAllMocks(); vi.unstubAllGlobals(); TestBed.resetTestingModule(); });

  async function load(fixture: { detectChanges: () => void; whenStable: () => Promise<unknown> }) {
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

  it('loads the period and renders the eleven legacy columns', async () => {
    const fixture = TestBed.createComponent(CourseLoadReduction);
    await load(fixture);
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('h1')?.textContent?.trim()).toContain('Reducción de carga');
    expect(root.querySelectorAll('app-table thead th')).toHaveLength(11);
    expect(root.querySelectorAll('app-table tbody tr')).toHaveLength(1);
    expect(root.textContent).toContain('ANA PÉREZ');
    expect(root.textContent).toContain('Total: 1');
  });

  it('shows the empty state and sanitizes a failed report', async () => {
    const fixture = TestBed.createComponent(CourseLoadReduction);
    fixture.detectChanges();
    http.expectOne(BASE + '/filters').flush({
      success: true, data: { periods: [{ value: '2024A', text: '2024 Primer Semestre' }] },
    });
    fixture.detectChanges();
    http.expectOne(request => request.url === BASE + '/report').flush({ success: true, data: { rows: [], total: 0 } });
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No se encontraron registros');
    expect(fixture.nativeElement.textContent).toContain('Total: 0');

    fixture.destroy();
    const failed = TestBed.createComponent(CourseLoadReduction);
    failed.detectChanges();
    http.expectOne(BASE + '/filters').flush('private', { status: 503, statusText: 'private' });
    await failed.whenStable();
    failed.detectChanges();
    expect(failed.nativeElement.textContent).toContain('No se pudieron cargar los períodos.');
    expect(failed.nativeElement.textContent).not.toContain('private');
  });

  it('exports the shown period as xlsx', async () => {
    const fixture = TestBed.createComponent(CourseLoadReduction);
    await load(fixture);
    fixture.nativeElement.querySelector('[data-action=export]').click();
    http.expectOne(request => request.url === BASE + '/export'
      && request.params.get('period') === '2024A').flush(new Blob(['xlsx']));
    await fixture.whenStable();
    fixture.detectChanges();
    expect(downloads[0].download).toBe('ReduccionCarga2024A.xlsx');
    expect(revokeUrl).toHaveBeenCalledWith('blob:reduction');
  });
});
