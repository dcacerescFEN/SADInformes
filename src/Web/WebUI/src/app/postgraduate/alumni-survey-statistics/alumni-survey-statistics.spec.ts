import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { AlumniSurveyStatistics } from './alumni-survey-statistics';

const BASE = '/api/reports/postgraduate/alumni-survey-statistics';

describe('Alumni survey statistics report', () => {
  let http: HttpTestingController;
  let downloads: HTMLAnchorElement[];
  const createUrl = vi.fn(() => 'blob:alumni-survey');
  const revokeUrl = vi.fn();

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [AlumniSurveyStatistics],
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
  afterEach(() => { http.verify(); vi.restoreAllMocks(); vi.unstubAllGlobals(); TestBed.resetTestingModule(); });

  it('renders the shared dictionary table with the survey questions', () => {
    const fixture = TestBed.createComponent(AlumniSurveyStatistics);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('app-table table')).not.toBeNull();
    expect(fixture.nativeElement.querySelectorAll('app-table tbody tr')).toHaveLength(10);
    expect(fixture.nativeElement.textContent).toContain('P1');
    expect(fixture.nativeElement.querySelector('table thead')!.textContent).toContain('Descripción');
    const lastPage = [...fixture.nativeElement.querySelectorAll('.page-link')]
      .find((button: HTMLButtonElement) => button.textContent?.trim() === '3') as HTMLButtonElement;
    lastPage.click(); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('P10_4');
    expect(fixture.nativeElement.querySelector('app-table')).not.toBeNull();
    fixture.destroy();
  });

  it('filters the dictionary with the shared search', () => {
    const fixture = TestBed.createComponent(AlumniSurveyStatistics);
    fixture.detectChanges();
    const input = fixture.nativeElement.querySelector('app-search input') as HTMLInputElement;
    input.value = 'P10';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('app-table tbody tr')).toHaveLength(5);
    expect(fixture.nativeElement.textContent).toContain('Nombre de Revista');
    input.value = 'sin-coincidencias';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No se encontraron registros');
    fixture.destroy();
  });

  it('downloads the workbook with the legacy filename and revokes the blob', async () => {
    const fixture = TestBed.createComponent(AlumniSurveyStatistics);
    fixture.detectChanges();
    fixture.nativeElement.querySelector('[data-action=export]').click();
    http.expectOne(BASE + '/download').flush(new Blob(['xlsx']));
    await fixture.whenStable(); fixture.detectChanges();
    expect(downloads).toHaveLength(1);
    expect(downloads[0].download).toBe('EncuestaAlumniPostgrado.xlsx');
    expect(revokeUrl).toHaveBeenCalledWith('blob:alumni-survey');
    fixture.destroy();
  });

  it('shows Sin Datos and sanitizes export failures', async () => {
    const fixture = TestBed.createComponent(AlumniSurveyStatistics);
    fixture.detectChanges();
    fixture.nativeElement.querySelector('[data-action=export]').click();
    http.expectOne(BASE + '/download')
      .flush(new Blob(['{}'], { type: 'application/json' }), { status: 400, statusText: 'Bad Request' });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Sin Datos');

    fixture.nativeElement.querySelector('[data-action=export]').click();
    http.expectOne(BASE + '/download')
      .flush(new Blob(['private'], { type: 'text/plain' }), { status: 503, statusText: 'private' });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No se pudo exportar el informe.');
    expect(fixture.nativeElement.textContent).not.toContain('private');
    fixture.destroy();
  });
});
