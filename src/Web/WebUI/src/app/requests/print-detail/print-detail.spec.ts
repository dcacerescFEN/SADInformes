import { Location } from '@angular/common';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, ParamMap } from '@angular/router';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { RequestPrintDetail } from './print-detail';

const REPORT_URL = '/api/reports/requests/by-status/report';
const PRINT_DETAIL_URL = '/api/reports/requests/by-status/print-detail';

const rowOne = {
  personCode: 'P1', studentCode: 'A1', name: 'ANA PÉREZ', requestCode: '20242/00001', rawRequestCode: '2024200001',
  date: '15/03/2024', type: 'Reconocimiento de Cátedras Aprobadas en Postgrado',
  situation: 'REGULAR', comment: 'Comentario', firstRequest: '20242/00001', lastRequest: '20242/00001',
};
const rowTwo = {
  ...rowOne, personCode: 'P2', studentCode: 'A2', name: 'LUIS SOTO', requestCode: '20242/00002',
  rawRequestCode: '2024200002', type: 'Homologación de Seminario',
};

describe('Imprimir detalle de solicitudes', () => {
  let http: HttpTestingController;
  let fixture: ComponentFixture<RequestPrintDetail> | undefined;
  const back = vi.fn();
  let queryParams: ParamMap = convertToParamMap({});
  const createUrl = vi.fn(() => 'blob:preview');
  const revokeUrl = vi.fn();

  beforeEach(() => {
    queryParams = convertToParamMap({});
    back.mockReset();
    createUrl.mockReset().mockReturnValue('blob:preview');
    revokeUrl.mockReset();
    vi.spyOn(HTMLIFrameElement.prototype, 'src', 'set').mockImplementation(() => {});
    vi.stubGlobal('URL', class extends URL {
      static override createObjectURL = createUrl;
      static override revokeObjectURL = revokeUrl;
    });
    TestBed.configureTestingModule({
      imports: [RequestPrintDetail],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: ActivatedRoute, useValue: { get snapshot() { return { queryParamMap: queryParams }; } } },
        { provide: Location, useValue: { back } },
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    fixture?.destroy();
    fixture = undefined;
    http.verify();
    TestBed.resetTestingModule();
    vi.restoreAllMocks();
    vi.unstubAllGlobals();
  });

  function reportRequest(career: string, status: string, type: string) {
    return http.expectOne((request) =>
      request.url === REPORT_URL
      && request.params.get('career') === career
      && request.params.get('status') === status
      && request.params.get('type') === type);
  }

  function pdfRequest(rawRequestCode: string) {
    return http.expectOne((request) =>
      request.url === PRINT_DETAIL_URL && request.params.get('requestCode') === rawRequestCode);
  }

  async function loadRows(
    rows: readonly object[],
    filters = { career: 'IC', status: 'NO IMPRESA', type: '67' },
  ) {
    fixture = TestBed.createComponent(RequestPrintDetail);
    fixture.detectChanges();
    reportRequest(filters.career, filters.status, filters.type)
      .flush({ success: true, data: { ...filters, rows } });
    await fixture.whenStable();
    fixture.detectChanges();
    await fixture.whenStable();
    return fixture;
  }

  it('carga la lista una vez y sólo pide el PDF de la primera solicitud', async () => {
    const loaded = await loadRows([rowOne, rowTwo]);
    const root: HTMLElement = loaded.nativeElement;

    expect(root.querySelectorAll('app-table thead th').length).toBe(3);
    const headers = Array.from(root.querySelectorAll('app-table thead th')).map((cell) => cell.textContent?.trim());
    expect(headers).toEqual(['Solicitud', 'Alumno', 'Tipo']);
    expect(root.querySelector('app-table tbody tr')?.classList.contains('row-clickable')).toBe(true);
    expect(root.textContent).toContain('ANA PÉREZ');
    expect(root.textContent).toContain('LUIS SOTO');

    pdfRequest('2024200001').flush(new Blob(['pdf']));
    await loaded.whenStable();
    http.expectNone((request) => request.url === PRINT_DETAIL_URL
      && request.params.get('requestCode') === '2024200002');
    expect(root.querySelectorAll('app-report-viewer')).toHaveLength(1);
  });

  it('filtra la lista con el buscador', async () => {
    const loaded = await loadRows([rowOne, rowTwo]);
    pdfRequest('2024200001').flush(new Blob(['first']));
    await loaded.whenStable();
    const root: HTMLElement = loaded.nativeElement;
    const search = root.querySelector('app-search input') as HTMLInputElement;
    search.value = 'LUIS';
    search.dispatchEvent(new Event('input'));
    loaded.detectChanges();
    await loaded.whenStable();
    expect(root.querySelectorAll('app-table tbody tr').length).toBe(1);
    expect(root.textContent).toContain('LUIS SOTO');
    expect(root.textContent).not.toContain('ANA PÉREZ');
  });

  it('pide el PDF de la fila seleccionada al hacer clic', async () => {
    const loaded = await loadRows([rowOne, rowTwo]);
    pdfRequest('2024200001').flush(new Blob(['first']));
    await loaded.whenStable();

    const rows = loaded.nativeElement.querySelectorAll('app-table tbody tr');
    (rows[1] as HTMLElement).click();
    loaded.detectChanges();
    await loaded.whenStable();

    const request = pdfRequest('2024200002');
    request.flush(new Blob(['second']));
    await loaded.whenStable();

    expect(createUrl).toHaveBeenCalledTimes(2);
    expect(revokeUrl).toHaveBeenCalledWith('blob:preview');
  });

  it('informa que no hay solicitudes y no pide ningún PDF', async () => {
    const loaded = await loadRows([]);
    const root: HTMLElement = loaded.nativeElement;

    http.expectNone((request) => request.url === PRINT_DETAIL_URL);
    expect(root.querySelector('app-report-viewer')).toBeNull();
    expect(root.textContent).toContain('No hay solicitudes para imprimir.');
  });

  it('usa los filtros recibidos por query params', async () => {
    queryParams = convertToParamMap({ career: 'IICG', status: 'PENDIENTE', type: '%' });

    const loaded = await loadRows([], { career: 'IICG', status: 'PENDIENTE', type: '%' });
    expect((loaded.nativeElement as HTMLElement).textContent).toContain('IICG');
    expect((loaded.nativeElement as HTMLElement).textContent).toContain('PENDIENTE');
  });

  it('muestra el error de carga sin pedir PDFs', async () => {
    fixture = TestBed.createComponent(RequestPrintDetail);
    fixture.detectChanges();
    reportRequest('IC', 'NO IMPRESA', '67').flush(null, { status: 500, statusText: 'Error' });
    await fixture.whenStable();
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent)
      .toContain('No se pudo cargar la lista de solicitudes.');
    http.expectNone((request) => request.url === PRINT_DETAIL_URL);
  });

  it('vuelve a la pantalla anterior con el botón Volver', async () => {
    const loaded = await loadRows([]);
    (loaded.nativeElement.querySelector('[data-action="back"]') as HTMLButtonElement).click();
    expect(back).toHaveBeenCalledTimes(1);
  });
});
