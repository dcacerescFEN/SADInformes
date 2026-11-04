import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Licenciatura } from './licenciatura';

const BASE = '/api/reports/postgraduate/licenciatura-pregrado';
const report = {
  success: true,
  data: {
    total: 2,
    rows: [
      { studentCode: 'A/1', name: 'Ana Pérez', career: 'Ingeniería Comercial',
        degree: 'Licenciado en Ciencias', status: 'Cumple' },
      { studentCode: 'A/2', name: 'Luis Soto', career: 'Administración',
        degree: 'Licenciado en Administración', status: 'Pendiente' },
    ],
  },
};

describe('Licenciatura report', () => {
  let http: HttpTestingController;
  let downloads: HTMLAnchorElement[];
  const createUrl = vi.fn(() => 'blob:licenciatura');
  const revokeUrl = vi.fn();

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [Licenciatura],
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

  it('does not load data until Buscar is pressed', () => {
    const fixture = TestBed.createComponent(Licenciatura);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('app-table')).toBeNull();
    http.expectNone(BASE + '/report');
    fixture.destroy();
  });

  it('loads the shared table and filters it with search', async () => {
    const fixture = TestBed.createComponent(Licenciatura);
    fixture.detectChanges();
    fixture.nativeElement.querySelector('[data-action=search]').click();
    http.expectOne(BASE + '/report').flush(report);
    await fixture.whenStable(); fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('app-table table')).not.toBeNull();
    expect(fixture.nativeElement.querySelectorAll('app-table tbody tr')).toHaveLength(2);
    const input = fixture.nativeElement.querySelector('app-search input') as HTMLInputElement;
    input.value = 'Soto';
    input.dispatchEvent(new Event('input')); fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('app-table tbody tr')).toHaveLength(1);
    expect(fixture.nativeElement.textContent).toContain('Luis Soto');
    fixture.destroy();
  });

  it('exports the workbook and sanitizes failures', async () => {
    const fixture = TestBed.createComponent(Licenciatura);
    fixture.detectChanges();
    fixture.nativeElement.querySelector('[data-action=search]').click();
    http.expectOne(BASE + '/report').flush(report);
    await fixture.whenStable(); fixture.detectChanges();
    fixture.nativeElement.querySelector('[data-action=export]').click();
    http.expectOne(BASE + '/download').flush(new Blob(['xlsx']));
    await fixture.whenStable(); fixture.detectChanges();
    expect(downloads[0].download).toBe('ProximasLicenciatura.xlsx');
    expect(revokeUrl).toHaveBeenCalledWith('blob:licenciatura');

    fixture.nativeElement.querySelector('[data-action=export]').click();
    http.expectOne(BASE + '/download')
      .flush(new Blob(['private'], { type: 'application/json' }), { status: 503, statusText: 'private' });
    await fixture.whenStable(); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No se pudo exportar el informe.');
    expect(fixture.nativeElement.textContent).not.toContain('private');
    fixture.destroy();
  });

  it('shows Sin Datos for an empty result', async () => {
    const fixture = TestBed.createComponent(Licenciatura);
    fixture.detectChanges();
    fixture.nativeElement.querySelector('[data-action=search]').click();
    http.expectOne(BASE + '/report').flush({ success: true, data: { total: 0, rows: [] } });
    await fixture.whenStable(); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No se encontraron registros');
    expect(fixture.nativeElement.querySelector('app-table')).not.toBeNull();
    fixture.destroy();
  });
});
