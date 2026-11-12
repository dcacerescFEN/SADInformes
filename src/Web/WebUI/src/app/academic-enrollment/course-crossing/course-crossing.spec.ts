import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { CourseCrossing } from './course-crossing';

const BASE = '/api/reports/academic-enrollment/course-crossing';
const row = {
  course1: 'ENECO660/01', courseName1: 'ELECTIVO', course2: 'ENECO661/02',
  courseName2: 'OTRO ELECTIVO', icLimit: '3', iicgLimit: '4', otherLimit: '5', totalLimit: '12',
};

describe('Cruce de Cursos', () => {
  let http: HttpTestingController;
  let downloads: HTMLAnchorElement[];
  const createUrl = vi.fn(() => 'blob:crossing');
  const revokeUrl = vi.fn();

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [CourseCrossing],
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

  it('loads the period and renders the eight legacy columns', async () => {
    const fixture = TestBed.createComponent(CourseCrossing);
    await load(fixture);
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('h1')?.textContent?.trim()).toContain('Cruce de Cursos');
    expect(root.querySelectorAll('app-table thead th')).toHaveLength(8);
    expect(root.querySelectorAll('app-table tbody tr')).toHaveLength(1);
    expect(root.textContent).toContain('ENECO660/01');
    expect(root.textContent).toContain('Total: 1');
  });

  it('shows the empty state and sanitizes a failed report', async () => {
    const fixture = TestBed.createComponent(CourseCrossing);
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
    const failed = TestBed.createComponent(CourseCrossing);
    failed.detectChanges();
    http.expectOne(BASE + '/filters').flush('private', { status: 503, statusText: 'private' });
    await failed.whenStable();
    failed.detectChanges();
    expect(failed.nativeElement.textContent).toContain('No se pudieron cargar los períodos.');
    expect(failed.nativeElement.textContent).not.toContain('private');
  });

  it('exports the shown period as xlsx', async () => {
    const fixture = TestBed.createComponent(CourseCrossing);
    await load(fixture);
    fixture.nativeElement.querySelector('[data-action=export]').click();
    http.expectOne(request => request.url === BASE + '/export'
      && request.params.get('period') === '2024A').flush(new Blob(['xlsx']));
    await fixture.whenStable();
    fixture.detectChanges();
    expect(downloads[0].download).toBe('CruceCursos.xlsx');
    expect(revokeUrl).toHaveBeenCalledWith('blob:crossing');
  });
});
