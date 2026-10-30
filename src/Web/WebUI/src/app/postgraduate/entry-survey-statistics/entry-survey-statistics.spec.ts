import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { EntrySurveyStatistics } from './entry-survey-statistics';

const BASE = '/api/reports/postgraduate/entry-survey-statistics';

describe('Entry survey statistics', () => {
  let http: HttpTestingController;
  let downloads: HTMLAnchorElement[];
  const createUrl = vi.fn(() => 'blob:entry-survey');
  const revokeUrl = vi.fn();

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [EntrySurveyStatistics],
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

  it('renders the full question dictionary without the raw response table', () => {
    const fixture = TestBed.createComponent(EntrySurveyStatistics);
    fixture.detectChanges();
    const body: HTMLElement = fixture.nativeElement;
    expect(body.querySelectorAll('tbody tr')).toHaveLength(40);
    expect(body.querySelector('thead')?.textContent).toContain('Código');
    expect(body.textContent).toContain('Q1_1');
    expect(body.textContent).toContain('Rango de Edad');
    expect(body.textContent).not.toContain('Cod_Alumno');
    fixture.destroy();
  });

  it('downloads the legacy Excel filename and revokes its blob URL', () => {
    const fixture = TestBed.createComponent(EntrySurveyStatistics);
    fixture.detectChanges();
    fixture.nativeElement.querySelector('[data-action=export]').click();
    const request = http.expectOne(req => req.url === BASE + '/download');
    request.flush(new Blob(['xlsx']));
    return fixture.whenStable().then(() => {
      fixture.detectChanges();
      expect(downloads).toHaveLength(1);
      expect(downloads[0].download).toBe('EncuestaEntradaPostgrado.xlsx');
      expect(revokeUrl).toHaveBeenCalledWith('blob:entry-survey');
      fixture.destroy();
    });
  });

  it('shows Sin Datos for an empty export and sanitizes errors', () => {
    const fixture = TestBed.createComponent(EntrySurveyStatistics);
    fixture.detectChanges();
    fixture.nativeElement.querySelector('[data-action=export]').click();
    http.expectOne(req => req.url === BASE + '/download')
      .flush(new Blob(['{}'], { type: 'application/json' }), { status: 400, statusText: 'Bad Request' });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Sin Datos');
    fixture.nativeElement.querySelector('[data-action=export]').click();
    http.expectOne(req => req.url === BASE + '/download')
      .flush(new Blob(['private-sql'], { type: 'text/plain' }), { status: 503, statusText: 'private' });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No se pudo descargar el informe.');
    expect(fixture.nativeElement.textContent).not.toContain('private');
    fixture.destroy();
  });
});
