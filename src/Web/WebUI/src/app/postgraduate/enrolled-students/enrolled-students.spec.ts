import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { EnrolledStudents } from './enrolled-students';

const BASE = '/api/reports/postgraduate/enrolled-students';
const catalog = {
  success: true,
  data: {
    modalities: [
      { value: 'FOR THE AMERICAS', text: 'FOR THE AMERICAS' },
      { value: 'SUNRISE', text: 'SUNRISE' },
    ],
    years: ['2026', '2024'],
  },
};

describe('Postgraduate enrolled students', () => {
  let http: HttpTestingController;
  let downloads: HTMLAnchorElement[];
  const createUrl = vi.fn(() => 'blob:enrolled-students');
  const revokeUrl = vi.fn();

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [EnrolledStudents],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
    downloads = [];
    createUrl.mockClear();
    revokeUrl.mockClear();
    const NativeURL = URL;
    vi.stubGlobal('URL', class extends NativeURL {
      static override createObjectURL = createUrl;
      static override revokeObjectURL = revokeUrl;
    });
    vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(function (this: HTMLAnchorElement) {
      downloads.push(this);
    });
  });

  afterEach(() => {
    http.verify();
    vi.restoreAllMocks();
    vi.unstubAllGlobals();
  });

  async function ready() {
    const fixture = TestBed.createComponent(EnrolledStudents);
    fixture.detectChanges();
    http.expectOne(request => request.url === BASE + '/filters').flush(catalog);
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture;
  }

  it('loads the two legacy modalities and years without an on-screen data table', async () => {
    const fixture = await ready();
    expect(fixture.componentInstance.form.getRawValue())
      .toEqual({ modality: 'FOR THE AMERICAS', year: '2026' });
    expect(fixture.nativeElement.querySelectorAll('ng-select[data-filter]')).toHaveLength(2);
    expect(fixture.nativeElement.querySelector('table')).toBeNull();
    expect(fixture.nativeElement.textContent).not.toContain('Mostrar');
    expect(fixture.nativeElement.querySelector('[data-action=export]').disabled).toBe(false);
    http.expectNone(request => request.url === BASE + '/download');
    fixture.destroy();
  });

  it('downloads the selected Excel and revokes its blob URL', async () => {
    const fixture = await ready();
    fixture.nativeElement.querySelector('[data-action=export]').click();
    const request = http.expectOne(request => request.url === BASE + '/download');
    expect(request.request.params.get('modality')).toBe('FOR THE AMERICAS');
    expect(request.request.params.get('year')).toBe('2026');
    request.flush(new Blob(['xlsx']));
    await fixture.whenStable();
    fixture.detectChanges();
    expect(downloads).toHaveLength(1);
    expect(downloads[0].download).toBe('AlumnosMatriculados.xlsx');
    expect(revokeUrl).toHaveBeenCalledWith('blob:enrolled-students');
    fixture.destroy();
  });

  it('shows Sin Datos when the export has no rows and keeps no attachment', async () => {
    const fixture = await ready();
    fixture.nativeElement.querySelector('[data-action=export]').click();
    http.expectOne(request => request.url === BASE + '/download')
      .flush(new Blob([JSON.stringify({ title: 'Sin Datos' })], { type: 'application/json' }),
        { status: 400, statusText: 'Bad Request' });
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Sin Datos');
    expect(downloads).toHaveLength(0);
    fixture.destroy();
  });

  it('sanitizes an unavailable report and retries filters explicitly', async () => {
    const fixture = TestBed.createComponent(EnrolledStudents);
    fixture.detectChanges();
    http.expectOne(request => request.url === BASE + '/filters')
      .flush('private-sql', { status: 503, statusText: 'private' });
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No se pudieron cargar los filtros.');
    expect(fixture.nativeElement.textContent).not.toContain('private');
    fixture.nativeElement.querySelector('button.btn-outline-secondary').click();
    http.expectOne(request => request.url === BASE + '/filters').flush(catalog);
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[data-action=export]').disabled).toBe(false);
    fixture.destroy();
  });
});
