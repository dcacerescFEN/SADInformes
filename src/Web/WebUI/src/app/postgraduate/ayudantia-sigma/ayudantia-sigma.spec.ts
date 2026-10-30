import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { AyudantiaSigma } from './ayudantia-sigma';

const BASE = '/api/reports/postgraduate/ayudantias-sigma';
const catalog = { success: true, data: { years: ['2026', '2024'] } };
const row = {
  cod: 'COD-1', nombre1: 'Ana', nombre2: 'María', apellido1: 'Pérez', apellido2: 'González',
  rut: 'P/1', email: 'ana@fen.uchile.cl', courseCode: 'MBA600', courseName: 'Gestión',
  section: '01', start: '01/03/2024', end: '30/06/2024', year: '2024', period: '2024F',
  studentType: 'MBA', modality: 'PT', promotion: '2024', costCenter: 'Centro',
  paymentType: 'AYUDANTIA', amount: '1000', existsInSigma: 'SI',
  sigmaDate: '01/03/2024 10:30:00', step: 'PASO 2', flag: '🚩', remunerationId: '10',
};

describe('Postgraduate Sigma tutorships', () => {
  let http: HttpTestingController;
  let downloads: HTMLAnchorElement[];
  const createUrl = vi.fn(() => 'blob:ayudantia-sigma');
  const revokeUrl = vi.fn();

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [AyudantiaSigma],
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
    const fixture = TestBed.createComponent(AyudantiaSigma);
    fixture.detectChanges();
    http.expectOne(request => request.url === BASE + '/filters').flush(catalog);
    await fixture.whenStable(); fixture.detectChanges();
    return fixture;
  }
  const submit = (fixture: any) =>
    fixture.nativeElement.querySelector('form').dispatchEvent(new Event('submit'));

  it('defaults a year and waits for Mostrar', async () => {
    const fixture = await ready();
    expect(fixture.componentInstance.form.getRawValue()).toEqual({ year: '2026' });
    expect(fixture.nativeElement.querySelectorAll('ng-select[data-filter]')).toHaveLength(1);
    http.expectNone(request => request.url === BASE);
    expect(fixture.nativeElement.querySelector('[data-action=export]').disabled).toBe(true);
    expect(fixture.nativeElement.querySelector('[data-action=show] i.bi-search')).not.toBeNull();
    fixture.destroy();
  });

  it('renders all twenty-five columns including Sigma state', async () => {
    const fixture = await ready();
    submit(fixture);
    const request = http.expectOne(req => req.url === BASE);
    expect(request.request.params.get('year')).toBe('2026');
    request.flush({ success: true, data: { rows: [row], total: 1 } });
    await fixture.whenStable(); fixture.detectChanges();
    const body: HTMLElement = fixture.nativeElement;
    expect(body.querySelectorAll('tbody tr:first-child td')).toHaveLength(25);
    expect(body.textContent).toContain('COD-1');
    expect(body.textContent).toContain('01/03/2024 10:30:00');
    expect(body.textContent).toContain('PASO 2');
    expect(body.textContent).toContain('🚩');
    expect((body.querySelector('[data-action=export]') as HTMLButtonElement).disabled).toBe(false);
    fixture.destroy();
  });

  it('downloads the shown year and revokes the blob URL', async () => {
    const fixture = await ready();
    submit(fixture);
    http.expectOne(req => req.url === BASE).flush({ success: true, data: { rows: [row], total: 1 } });
    await fixture.whenStable(); fixture.detectChanges();
    fixture.nativeElement.querySelector('[data-action=export]').click();
    const request = http.expectOne(req => req.url === BASE + '/download');
    expect(request.request.params.get('year')).toBe('2026');
    request.flush(new Blob(['xlsx']));
    await fixture.whenStable(); fixture.detectChanges();
    expect(downloads).toHaveLength(1);
    expect(downloads[0].download).toBe('ReporteAyudantiaSigma-2026.xlsx');
    expect(revokeUrl).toHaveBeenCalledWith('blob:ayudantia-sigma');
    fixture.destroy();
  });

  it('paginates ten rows and resets the page when searching', async () => {
    const fixture = await ready();
    submit(fixture);
    const many = Array.from({ length: 120 }, (_, index) => ({
      ...row, cod: 'COD-' + String(index + 1).padStart(3, '0'),
    }));
    http.expectOne(req => req.url === BASE).flush({ success: true, data: { rows: many, total: 120 } });
    await fixture.whenStable(); fixture.detectChanges();
    const body: HTMLElement = fixture.nativeElement;
    expect(body.querySelectorAll('tbody tr')).toHaveLength(10);
    const labels = Array.from(body.querySelectorAll('nav .page-item'))
      .map(item => item.textContent?.trim());
    expect(labels).toEqual(['Anterior', '1', '2', '3', '...', '12', 'Siguiente']);
    (body.querySelector('.page-item:last-child button') as HTMLButtonElement).click();
    fixture.detectChanges();
    expect(body.textContent).toContain('Mostrando 11–20 de 120');
    const input = body.querySelector('app-search input') as HTMLInputElement;
    input.value = 'COD-011';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    expect(body.querySelectorAll('tbody tr')).toHaveLength(1);
    expect(body.textContent).toContain('Mostrando 1–1 de 1');
    fixture.destroy();
  });

  it('cancels a pending report when the year changes', async () => {
    const fixture = await ready();
    submit(fixture);
    const pending = http.expectOne(req => req.url === BASE);
    fixture.componentInstance.form.controls.year.setValue('2024');
    expect(pending.cancelled).toBe(true);
    expect(fixture.nativeElement.querySelector('table')).toBeNull();
    fixture.destroy();
  });

  it('shows Sin Datos and sanitizes errors', async () => {
    const fixture = await ready();
    submit(fixture);
    http.expectOne(req => req.url === BASE).flush({ success: true, data: { rows: [], total: 0 } });
    await fixture.whenStable(); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Sin Datos');
    submit(fixture);
    http.expectOne(req => req.url === BASE).flush('private-sql', { status: 503, statusText: 'private' });
    await fixture.whenStable(); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No se pudo cargar el reporte.');
    expect(fixture.nativeElement.textContent).not.toContain('private');
    fixture.destroy();
  });
});
