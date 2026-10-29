import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { Subject } from 'rxjs';
import { StudentRequests } from './student-requests';

const BASE = '/api/reports/postgraduate/student-requests';
const catalog = { success: true, data: { studentTypes: [{ value: 'Todos', text: 'Todos' }, { value: 'MBA', text: 'MBA' }],
  modalities: ['ANTOFAGASTA'], cohorts: ['Todos', '2026'], states: ['Todos', 'PENDIENTE', 'EN CURSO', 'RESUELTA', 'ANULADA'] } };

describe('Student requests Excel download', () => {
  let http: HttpTestingController;
  const modal = { open: vi.fn() };
  const createUrl = vi.fn(() => 'blob:excel');
  const revokeUrl = vi.fn();
  let links: HTMLAnchorElement[];

  beforeEach(() => {
    modal.open.mockReset().mockReturnValue({ componentInstance: { titleValue: '', close: new Subject<void>(), load: vi.fn() }, dismiss: vi.fn() });
    TestBed.configureTestingModule({ imports: [StudentRequests], providers: [provideHttpClient(), provideHttpClientTesting(),
      { provide: NgbModal, useValue: modal }] });
    http = TestBed.inject(HttpTestingController);
    createUrl.mockClear(); revokeUrl.mockClear(); links = [];
    vi.stubGlobal('URL', Object.assign(URL, { createObjectURL: createUrl, revokeObjectURL: revokeUrl }));
    vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(function (this: HTMLAnchorElement) { links.push(this); });
  });
  afterEach(() => { http.verify(); vi.restoreAllMocks(); vi.unstubAllGlobals(); });

  async function ready() {
    const fixture = TestBed.createComponent(StudentRequests); fixture.detectChanges();
    http.expectOne(r => r.url === BASE + '/filters').flush(catalog);
    await fixture.whenStable(); fixture.detectChanges();
    return fixture;
  }

  it('downloads Excel directly with selected filters, correct filename and no viewer', async () => {
    const fixture = await ready();
    fixture.componentInstance.form.controls.studentType.setValue('MBA');
    http.expectOne(r => r.url === BASE + '/filters').flush(catalog);
    await fixture.whenStable(); fixture.detectChanges();
    fixture.componentInstance.form.patchValue({ modality: 'ANTOFAGASTA', cohort: '2026', state: 'RESUELTA' });
    fixture.nativeElement.querySelector('form').dispatchEvent(new Event('submit'));
    const request = http.expectOne(r => r.url === BASE);
    expect(request.request.params.keys().sort()).toEqual(['cohort', 'modality', 'state', 'studentType']);
    expect(request.request.params.get('state')).toBe('RESUELTA');
    expect(request.request.params.get('cohort')).toBe('2026');
    expect(request.request.responseType).toBe('blob');
    expect(modal.open).not.toHaveBeenCalled();
    request.flush(new Blob(['xlsx'], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' }));
    await fixture.whenStable(); fixture.detectChanges();
    expect(links).toHaveLength(1);
    expect(links[0].download).toBe('SolicitudesAlumnoPostgrado.xlsx');
    expect(links[0].href).toBe('blob:excel');
    expect(revokeUrl).toHaveBeenCalledWith('blob:excel');
    expect(fixture.nativeElement.querySelector('iframe, app-report-viewer')).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Descargar Excel');
    fixture.destroy();
  });

  it('blocks duplicate downloads and cancels stale selections before saving a file', async () => {
    const fixture = await ready();
    const submit = () => fixture.nativeElement.querySelector('form').dispatchEvent(new Event('submit'));
    submit();
    const stale = http.expectOne(r => r.url === BASE);
    submit(); http.expectNone(r => r.url === BASE);
    fixture.componentInstance.form.controls.state.setValue('ANULADA');
    expect(stale.cancelled).toBe(true);
    expect(links).toHaveLength(0);
    submit();
    const fresh = http.expectOne(r => r.url === BASE);
    expect(fresh.request.params.get('state')).toBe('ANULADA');
    fresh.flush(new Blob(['xlsx']));
    await fixture.whenStable(); fixture.detectChanges();
    expect(links).toHaveLength(1);
    expect(modal.open).not.toHaveBeenCalled();
    fixture.destroy();
  });

  it('shows sanitized download failure and allows retry without saving an error blob', async () => {
    const fixture = await ready();
    fixture.nativeElement.querySelector('form').dispatchEvent(new Event('submit'));
    http.expectOne(r => r.url === BASE).flush(new Blob(['private sql']), { status: 503, statusText: 'private' });
    await fixture.whenStable(); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No se pudo descargar el informe.');
    expect(fixture.nativeElement.textContent).not.toContain('private');
    expect(fixture.nativeElement.querySelector('button[type=submit]').disabled).toBe(false);
    expect(createUrl).not.toHaveBeenCalled();
    fixture.nativeElement.querySelector('button[type=button]').click();
    http.expectOne(r => r.url === BASE).flush(new Blob(['xlsx']));
    await fixture.whenStable(); fixture.detectChanges();
    expect(links).toHaveLength(1);
    fixture.destroy();
  });

  it('ignores stale catalogs and keeps download disabled after catalog failure', async () => {
    const fixture = TestBed.createComponent(StudentRequests); fixture.detectChanges();
    const stale = http.expectOne(r => r.url === BASE + '/filters');
    fixture.componentInstance.form.controls.studentType.setValue('MBA');
    const fresh = http.expectOne(r => r.url === BASE + '/filters');
    fresh.flush(catalog);
    stale.flush({ success: true, data: { ...catalog.data, modalities: ['STALE'] } });
    await fixture.whenStable(); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).not.toContain('STALE');
    fixture.componentInstance.form.controls.studentType.setValue('Todos');
    http.expectOne(r => r.url === BASE + '/filters').flush('private sql', { status: 503, statusText: 'private' });
    await fixture.whenStable(); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No se pudieron cargar los filtros.');
    expect(fixture.nativeElement.textContent).not.toContain('private');
    expect(fixture.nativeElement.querySelector('button[type=submit]').disabled).toBe(true);
    fixture.destroy();
  });
});
