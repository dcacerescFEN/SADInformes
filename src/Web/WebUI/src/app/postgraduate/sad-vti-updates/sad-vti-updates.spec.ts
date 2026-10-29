import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { SadVtiUpdates } from './sad-vti-updates';

const BASE = '/api/reports/postgraduate/sad-vti-updates';
const catalog = { success: true, data: { states: [{ value: '-1', text: 'Todos' }, { value: 'ERR', text: 'Error' }] } };

describe('SAD VTI audit Excel export', () => {
  let http: HttpTestingController;
  let links: HTMLAnchorElement[];
  const createUrl = vi.fn(() => 'blob:sad-vti');
  const revokeUrl = vi.fn();
  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [SadVtiUpdates], providers: [provideHttpClient(), provideHttpClientTesting()] });
    http = TestBed.inject(HttpTestingController);
    links = []; createUrl.mockClear(); revokeUrl.mockClear();
    const NativeURL = URL;
    vi.stubGlobal('URL', class extends NativeURL { static override createObjectURL = createUrl; static override revokeObjectURL = revokeUrl; });
    vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(function (this: HTMLAnchorElement) { links.push(this); });
  });
  afterEach(() => { http.verify(); vi.restoreAllMocks(); vi.unstubAllGlobals(); });
  async function ready() {
    const fixture = TestBed.createComponent(SadVtiUpdates); fixture.detectChanges();
    http.expectOne(BASE + '/filters').flush(catalog);
    await fixture.whenStable(); fixture.detectChanges();
    return fixture;
  }
  const submit = (fixture: any) => fixture.nativeElement.querySelector('form').dispatchEvent(new Event('submit'));

  it('shows the state and required date controls, without grid or SSRS viewer', async () => {
    const fixture = await ready();
    expect(fixture.componentInstance.form.getRawValue()).toEqual({ state: '-1', from: '', to: '' });
    expect(fixture.nativeElement.querySelectorAll('ng-select')).toHaveLength(1);
    expect(fixture.nativeElement.querySelectorAll('input[type=date]')).toHaveLength(2);
    expect(fixture.nativeElement.querySelector('button[type=submit]').disabled).toBe(true);
    expect(fixture.nativeElement.querySelector('iframe, app-report-viewer, app-table')).toBeNull();
    http.expectNone(r => r.url === BASE + '/download');
    fixture.destroy();
  });

  it('downloads the legacy report using selected state and inclusive ISO dates', async () => {
    const fixture = await ready();
    fixture.componentInstance.form.setValue({ state: 'ERR', from: '2026-10-01', to: '2026-10-01' });
    submit(fixture);
    const request = http.expectOne(r => r.url === BASE + '/download');
    expect(request.request.params.keys().sort()).toEqual(['from', 'state', 'to']);
    expect(request.request.params.get('state')).toBe('ERR');
    expect(request.request.params.get('from')).toBe('2026-10-01');
    expect(request.request.params.get('to')).toBe('2026-10-01');
    expect(request.request.responseType).toBe('blob');
    request.flush(new Blob(['xlsx']));
    await fixture.whenStable(); fixture.detectChanges();
    expect(links).toHaveLength(1);
    expect(links[0].download).toBe('AudiPostgradoAlumnosGuia.xlsx');
    expect(links[0].href).toBe('blob:sad-vti');
    expect(revokeUrl).toHaveBeenCalledWith('blob:sad-vti');
    fixture.destroy();
  });

  it('rejects missing or reversed dates without downloading', async () => {
    const fixture = await ready(); submit(fixture);
    http.expectNone(r => r.url.endsWith('/download'));
    fixture.componentInstance.form.patchValue({ from: '2026-10-02', to: '2026-10-01' }); submit(fixture);
    await fixture.whenStable(); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('La fecha desde no puede ser posterior a la fecha hasta.');
    http.expectNone(r => r.url.endsWith('/download'));
    fixture.destroy();
  });

  it('blocks duplicate export and cancels an outdated filter selection', async () => {
    const fixture = await ready(); fixture.componentInstance.form.patchValue({ from: '2026-10-01', to: '2026-10-01' });
    submit(fixture); const stale = http.expectOne(r => r.url.endsWith('/download'));
    submit(fixture); http.expectNone(r => r.url.endsWith('/download'));
    fixture.componentInstance.form.controls.state.setValue('ERR');
    expect(stale.cancelled).toBe(true);
    expect(links).toHaveLength(0);
    submit(fixture); http.expectOne(r => r.url.endsWith('/download')).flush(new Blob(['xlsx']));
    await fixture.whenStable(); fixture.detectChanges(); expect(links).toHaveLength(1);
    fixture.destroy();
  });

  it('shows the exact legacy empty-result message without saving an error blob', async () => {
    const fixture = await ready(); fixture.componentInstance.form.patchValue({ from: '2026-10-01', to: '2026-10-01' }); submit(fixture);
    const error = new Blob(['error']);
    Object.defineProperty(error, 'text', { value: async () => JSON.stringify({ title: 'No hay registros para exportar.' }) });
    http.expectOne(r => r.url.endsWith('/download')).flush(error, { status: 400, statusText: 'Bad request' });
    await fixture.whenStable(); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No hay registros para exportar.');
    expect(createUrl).not.toHaveBeenCalled(); expect(links).toHaveLength(0);
    fixture.destroy();
  });

  it('sanitizes failures and keeps download disabled until catalogs are ready', async () => {
    const fixture = TestBed.createComponent(SadVtiUpdates); fixture.detectChanges();
    http.expectOne(BASE + '/filters').flush('private sql', { status: 503, statusText: 'private' });
    await fixture.whenStable(); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No se pudieron cargar los estados.');
    expect(fixture.nativeElement.textContent).not.toContain('private');
    expect(fixture.nativeElement.querySelector('button[type=submit]').disabled).toBe(true);
    fixture.destroy();
  });
});
