import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { CourseTeacherEvaluation } from './course-teacher-evaluation';

const BASE = '/api/reports/postgraduate/course-teacher-evaluation';
const filters = { success: true, data: { years: ['2026', '2024'], types: ['EX', 'FT'] } };

describe('Course teacher evaluation', () => {
  let http: HttpTestingController;
  let downloads: HTMLAnchorElement[];
  const createUrl = vi.fn(() => 'blob:evaluation');
  const revokeUrl = vi.fn();

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [CourseTeacherEvaluation],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
    downloads = []; createUrl.mockClear(); revokeUrl.mockClear();
    const NativeURL = URL;
    vi.stubGlobal('URL', class extends NativeURL {
      static override createObjectURL = createUrl;
      static override revokeObjectURL = revokeUrl;
    });
    vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(function (this: HTMLAnchorElement) {
      downloads.push(this);
    });
  });
  afterEach(() => { http.verify(); vi.restoreAllMocks(); vi.unstubAllGlobals(); });

  async function ready() {
    const fixture = TestBed.createComponent(CourseTeacherEvaluation);
    fixture.detectChanges();
    http.expectOne(req => req.url === BASE + '/filters').flush(filters);
    await fixture.whenStable(); fixture.detectChanges();
    return fixture;
  }

  it('loads year and type filters without an on-screen grid', async () => {
    const fixture = await ready();
    expect(fixture.componentInstance.form.getRawValue()).toEqual({ year: '2026', type: 'EX' });
    expect(fixture.nativeElement.querySelectorAll('ng-select[data-filter]')).toHaveLength(2);
    expect(fixture.nativeElement.querySelector('table')).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Executive');
    fixture.destroy();
  });

  it('downloads the selected type with the legacy filename', async () => {
    const fixture = await ready();
    fixture.componentInstance.form.controls.type.setValue('FT');
    fixture.nativeElement.querySelector('[data-action=export]').click();
    const request = http.expectOne(req => req.url === BASE + '/download');
    expect(request.request.params.get('year')).toBe('2026');
    expect(request.request.params.get('type')).toBe('FT');
    request.flush(new Blob(['xlsx']));
    await fixture.whenStable(); fixture.detectChanges();
    expect(downloads).toHaveLength(1);
    expect(downloads[0].download).toBe('Evaluacion_Docente_Curso.xlsx');
    expect(revokeUrl).toHaveBeenCalledWith('blob:evaluation');
    fixture.destroy();
  });

  it('shows Sin Datos for an empty export and sanitizes errors', async () => {
    const fixture = await ready();
    fixture.nativeElement.querySelector('[data-action=export]').click();
    http.expectOne(req => req.url === BASE + '/download')
      .flush(new Blob(['{}'], { type: 'application/json' }), { status: 400, statusText: 'Bad Request' });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Sin Datos');
    fixture.nativeElement.querySelector('[data-action=export]').click();
    http.expectOne(req => req.url === BASE + '/download')
      .flush(new Blob(['private'], { type: 'text/plain' }), { status: 503, statusText: 'private' });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No se pudo exportar el informe.');
    expect(fixture.nativeElement.textContent).not.toContain('private');
    fixture.destroy();
  });
});
