import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed, type ComponentFixture } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { ReportViewer } from './report-viewer';

const PRINT_DETAIL_URL = '/api/reports/requests/by-status/print-detail';

describe('ReportViewer de detalle de solicitud', () => {
  let http: HttpTestingController;
  let fixture: ComponentFixture<ReportViewer> | undefined;
  const createUrl = vi.fn<(blob: Blob) => string>();
  const revokeUrl = vi.fn<(url: string) => void>();

  beforeEach(() => {
    createUrl.mockReset().mockReturnValue('blob:first');
    revokeUrl.mockReset();
    vi.spyOn(HTMLIFrameElement.prototype, 'src', 'set').mockImplementation(() => {});
    vi.stubGlobal('URL', class extends URL {
      static override createObjectURL = createUrl;
      static override revokeObjectURL = revokeUrl;
    });
    TestBed.configureTestingModule({
      imports: [ReportViewer],
      providers: [provideHttpClient(), provideHttpClientTesting()],
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

  function createViewer(requestCode = ''): ReportViewer {
    fixture = TestBed.createComponent(ReportViewer);
    fixture.componentRef.setInput('requestCode', requestCode);
    fixture.detectChanges();
    return fixture.componentInstance;
  }

  function detailRequest(requestCode: string) {
    return http.expectOne((request) =>
      request.url === PRINT_DETAIL_URL && request.params.get('requestCode') === requestCode);
  }

  it('no solicita el reporte cuando el código está vacío', async () => {
    const viewer = createViewer('');
    await fixture!.whenStable();
    http.expectNone((request) => request.url === PRINT_DETAIL_URL);
    expect(viewer.isLoading()).toBe(false);
    expect(viewer.hasError()).toBe(false);
    expect(createUrl).not.toHaveBeenCalled();
    expect((fixture!.nativeElement as HTMLElement).querySelector('iframe')).toBeNull();
  });

  it('carga el PDF del código recibido y lo muestra en el iframe', async () => {
    const viewer = createViewer('2024200001');
    const request = detailRequest('2024200001');
    expect(request.request.method).toBe('GET');
    expect(request.request.responseType).toBe('blob');
    expect(viewer.isLoading()).toBe(true);
    const blob = new Blob(['pdf'], { type: 'application/pdf' });
    request.flush(blob);
    await fixture!.whenStable();
    expect(viewer.isLoading()).toBe(false);
    expect(viewer.hasError()).toBe(false);
    expect(createUrl).toHaveBeenCalledExactlyOnceWith(blob);
    expect(viewer.safeBlobUrl()).not.toBeNull();
    expect((fixture!.nativeElement as HTMLElement).querySelector('iframe')).not.toBeNull();
  });

  it('revoca la URL anterior y pide sólo el PDF de la nueva solicitud al cambiar el código', async () => {
    createViewer('2024200001');
    detailRequest('2024200001').flush(new Blob(['first']));
    await fixture!.whenStable();
    createUrl.mockReturnValue('blob:second');

    fixture!.componentRef.setInput('requestCode', '2024200002');
    fixture!.detectChanges();
    await fixture!.whenStable();

    expect(revokeUrl).toHaveBeenCalledExactlyOnceWith('blob:first');
    expect(viewerIsEmpty()).toBe(true);
    detailRequest('2024200002').flush(new Blob(['second']));
    await fixture!.whenStable();
    expect(createUrl).toHaveBeenCalledTimes(2);
    expect(createUrl.mock.calls[1][0]).toEqual(new Blob(['second']));
    http.expectNone((request) =>
      request.url === PRINT_DETAIL_URL && request.params.get('requestCode') === '2024200001');

    fixture!.destroy();
    fixture = undefined;
    expect(revokeUrl.mock.calls).toEqual([['blob:first'], ['blob:second']]);
  });

  it('ignora la respuesta obsoleta cuando el código cambia antes de responder', async () => {
    createViewer('2024200001');
    const stale = detailRequest('2024200001');

    fixture!.componentRef.setInput('requestCode', '2024200002');
    fixture!.detectChanges();
    await fixture!.whenStable();
    const fresh = detailRequest('2024200002');

    const staleBlob = new Blob(['stale']);
    const freshBlob = new Blob(['fresh']);
    stale.flush(staleBlob);
    fresh.flush(freshBlob);
    await fixture!.whenStable();

    expect(createUrl).toHaveBeenCalledExactlyOnceWith(freshBlob);
  });

  it('muestra el error y permite reintentar la carga', async () => {
    const viewer = createViewer('2024200001');
    detailRequest('2024200001').flush(null, { status: 500, statusText: 'Error del servidor' });
    await fixture!.whenStable();

    expect(viewer.hasError()).toBe(true);
    expect(viewer.isLoading()).toBe(false);
    const element: HTMLElement = fixture!.nativeElement;
    expect(element.textContent).toContain('No se pudo cargar la vista previa.');

    element.querySelector<HTMLButtonElement>('[data-action="retry"]')!.click();
    const retry = detailRequest('2024200001');
    retry.flush(new Blob(['retry'], { type: 'application/pdf' }));
    await fixture!.whenStable();

    expect(viewer.hasError()).toBe(false);
    expect(viewer.isLoading()).toBe(false);
    expect(viewer.safeBlobUrl()).not.toBeNull();
  });

  it('descarga el PDF ya cargado sin pedir otro reporte', async () => {
    const clicks: string[] = [];
    vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(function (this: HTMLAnchorElement) {
      clicks.push(this.download);
    });
    const viewer = createViewer('2024200001');
    detailRequest('2024200001').flush(new Blob(['pdf'], { type: 'application/pdf' }));
    await fixture!.whenStable();

    createUrl.mockReturnValue('blob:download');
    viewer.download();

    expect(clicks).toEqual(['Solicitud_2024200001.pdf']);
    http.expectNone((request) => request.url === PRINT_DETAIL_URL);
  });

  it('quita la barra del código al nombrar el PDF descargado', async () => {
    const clicks: string[] = [];
    vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(function (this: HTMLAnchorElement) {
      clicks.push(this.download);
    });
    const viewer = createViewer('20242/00001');
    detailRequest('20242/00001').flush(new Blob(['pdf'], { type: 'application/pdf' }));
    await fixture!.whenStable();

    createUrl.mockReturnValue('blob:download');
    viewer.download();

    expect(clicks).toEqual(['Solicitud_2024200001.pdf']);
  });

  function viewerIsEmpty(): boolean {
    return fixture!.componentInstance.safeBlobUrl() === null;
  }
});
