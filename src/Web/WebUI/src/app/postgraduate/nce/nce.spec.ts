import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Nce } from './nce';

const BASE = '/api/reports/postgraduate/nce';
const report = {
  success: true,
  data: {
    total: 2,
    rows: [
      { id: '11111111-1111-1111-1111-111111111111', date: '02/01/2024 03:04', cause: 'Causal A',
        studentCode: 'A1', studentType: 'MBA', modality: 'PT', academicSituation: 'ELIM',
        reason: 'Bajo rendimiento', name: 'ANA MARÍA PÉREZ SOTO' },
      { id: '22222222-2222-2222-2222-222222222222', date: '01/01/2024 02:03', cause: 'Causal B',
        studentCode: 'A2', studentType: 'MBA', modality: 'PT', academicSituation: 'ELIM',
        reason: 'Inasistencia', name: 'LUIS SOTO' },
    ],
  },
};

describe('NCE report', () => {
  let http: HttpTestingController;
  let downloads: HTMLAnchorElement[];
  const createUrl = vi.fn(() => 'blob:nce');
  const revokeUrl = vi.fn();

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [Nce],
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

  async function ready() {
    const fixture = TestBed.createComponent(Nce);
    fixture.detectChanges();
    http.expectOne(BASE + '/report').flush(report);
    await fixture.whenStable(); fixture.detectChanges();
    return fixture;
  }

  it('loads the report and its 10 columns plus row number', async () => {
    const fixture = await ready();
    expect(fixture.nativeElement.querySelector('app-table table')).not.toBeNull();
    expect(fixture.nativeElement.querySelectorAll('app-table thead th')).toHaveLength(10);
    expect(fixture.nativeElement.textContent).toContain('ANA MARÍA PÉREZ SOTO');
    const pdf = fixture.nativeElement.querySelector('[data-action=export-pdf]') as HTMLButtonElement;
    expect(pdf.textContent).toContain('PDF');
    expect(pdf.textContent).not.toContain('Imprimir');
    expect(pdf.querySelector('.bi-file-earmark-pdf')).not.toBeNull();
    expect(pdf.classList.contains('btn-export-pdf')).toBe(true);
    const excel = fixture.nativeElement.querySelector('[data-action=export-excel]') as HTMLButtonElement;
    expect(excel.classList.contains('btn-export-excel')).toBe(true);
    fixture.destroy();
  });

  it('filters the report with the shared search', async () => {
    const fixture = await ready();
    const input = fixture.nativeElement.querySelector('app-search input') as HTMLInputElement;
    input.value = 'A2';
    input.dispatchEvent(new Event('input')); fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('app-table tbody tr')).toHaveLength(1);
    expect(fixture.nativeElement.textContent).toContain('LUIS SOTO');
    fixture.destroy();
  });

  it('exports CSV and Excel and sanitizes export failures', async () => {
    const fixture = await ready();
    fixture.nativeElement.querySelector('[data-action=export-csv]').click();
    expect(downloads[0].download).toBe('Informe Notificaciones Causal de Eliminación.csv');
    fixture.nativeElement.querySelector('[data-action=export-excel]').click();
    http.expectOne(BASE + '/download').flush(new Blob(['xlsx']));
    await fixture.whenStable(); fixture.detectChanges();
    expect(downloads[1].download).toBe('Informe Notificaciones Causal de Eliminación.xlsx');
    fixture.nativeElement.querySelector('[data-action=export-pdf]').click();
    http.expectOne(BASE + '/download/pdf').flush(new Blob(['%PDF-1.7'], { type: 'application/pdf' }));
    await fixture.whenStable(); fixture.detectChanges();
    expect(downloads[2].download).toBe('Informe Notificaciones Causal de Eliminación.pdf');

    fixture.nativeElement.querySelector('[data-action=export-excel]').click();
    http.expectOne(BASE + '/download')
      .flush(new Blob(['private'], { type: 'application/json' }), { status: 503, statusText: 'private' });
    await fixture.whenStable(); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No se pudo exportar el informe.');
    expect(fixture.nativeElement.textContent).not.toContain('private');
    fixture.destroy();
  });

  it('shows Sin Datos and sanitizes report failures', async () => {
    const fixture = TestBed.createComponent(Nce);
    fixture.detectChanges();
    http.expectOne(BASE + '/report').flush({ success: true, data: { total: 0, rows: [] } });
    await fixture.whenStable(); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No se encontraron registros');
    expect(fixture.nativeElement.querySelector('app-table table')).not.toBeNull();

    const failed = TestBed.createComponent(Nce);
    failed.detectChanges();
    http.expectOne(BASE + '/report')
      .flush({ message: 'private' }, { status: 503, statusText: 'private' });
    await failed.whenStable(); failed.detectChanges();
    expect(failed.nativeElement.textContent).toContain('No se pudo cargar el informe.');
    expect(failed.nativeElement.textContent).not.toContain('private');
    fixture.destroy(); failed.destroy();
  });
});
