import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TeacherValidities } from './teacher-validities';

const BASE = '/api/reports/postgraduate/teacher-validities';
const catalog = { success: true, data: { years: ['2026', '2024'] } };
const row = (id = '2', valid = true) => ({
  id, year: '2024', period: '2024F', courseCode: 'MBA600', section: '01',
  courseName: 'Gestión', studentType: 'MBA', modality: 'SUNRISE', promotion: '2024',
  personCode: 'P/1', name: 'Ana Pérez', email: 'ana@fen.uchile.cl',
  dedicatedHours: '4.5', valid,
});

describe('Postgraduate teacher validities', () => {
  let http: HttpTestingController;
  let downloads: HTMLAnchorElement[];
  const createUrl = vi.fn(() => 'blob:teacher-validities');
  const revokeUrl = vi.fn();

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [TeacherValidities],
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
    const fixture = TestBed.createComponent(TeacherValidities);
    fixture.detectChanges();
    http.expectOne(request => request.url === BASE + '/filters').flush(catalog);
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture;
  }

  const submit = (fixture: any) =>
    fixture.nativeElement.querySelector('form').dispatchEvent(new Event('submit'));

  it('defaults a concrete year and waits for Mostrar', async () => {
    const fixture = await ready();
    expect(fixture.componentInstance.form.getRawValue()).toEqual({ year: '2026' });
    expect(fixture.nativeElement.querySelectorAll('ng-select[data-filter]')).toHaveLength(1);
    http.expectNone(request => request.url === BASE);
    expect(fixture.nativeElement.querySelector('[data-action=export]').disabled).toBe(true);
    const show = fixture.nativeElement.querySelector('[data-action=show]');
    expect(show.textContent).toContain('Mostrar');
    expect(show.querySelector('i.bi-search')).not.toBeNull();
    fixture.destroy();
  });

  it('renders all fourteen columns and validity states from the server', async () => {
    const fixture = await ready();
    submit(fixture);
    const report = http.expectOne(request => request.url === BASE);
    expect(report.request.params.get('year')).toBe('2026');
    report.flush({ success: true, data: { rows: [row(), row('3', false)], total: 2 } });
    await fixture.whenStable();
    fixture.detectChanges();
    const body: HTMLElement = fixture.nativeElement;
    expect(body.querySelectorAll('tbody tr')).toHaveLength(2);
    expect(body.querySelectorAll('tbody tr:first-child td')).toHaveLength(14);
    expect(body.textContent).toContain('MBA600');
    expect(body.textContent).toContain('ana@fen.uchile.cl');
    expect(body.textContent).toContain('4.5');
    expect(body.querySelector('tbody .badge.bg-success')?.textContent?.trim()).toBe('Vigente');
    expect(body.querySelector('tbody .badge.bg-danger')?.textContent?.trim()).toBe('No Vigente');
    expect((body.querySelector('[data-action=export]') as HTMLButtonElement).disabled).toBe(false);
    fixture.destroy();
  });

  it('cancels a pending report and clears results when the year changes', async () => {
    const fixture = await ready();
    submit(fixture);
    const pending = http.expectOne(request => request.url === BASE);
    fixture.componentInstance.form.controls.year.setValue('2024');
    expect(pending.cancelled).toBe(true);
    expect(fixture.nativeElement.querySelector('[data-action=export]').disabled).toBe(true);
    expect(fixture.nativeElement.querySelector('table')).toBeNull();
    fixture.destroy();
  });

  it('downloads the shown year and revokes the blob URL', async () => {
    const fixture = await ready();
    submit(fixture);
    http.expectOne(request => request.url === BASE)
      .flush({ success: true, data: { rows: [row()], total: 1 } });
    await fixture.whenStable();
    fixture.detectChanges();
    fixture.nativeElement.querySelector('[data-action=export]').click();
    const request = http.expectOne(request => request.url === BASE + '/download');
    expect(request.request.params.get('year')).toBe('2026');
    request.flush(new Blob(['xlsx']));
    await fixture.whenStable();
    fixture.detectChanges();
    expect(downloads).toHaveLength(1);
    expect(downloads[0].download).toBe('ProfesoresVigencias.xlsx');
    expect(revokeUrl).toHaveBeenCalledWith('blob:teacher-validities');
    fixture.destroy();
  });

  it('paginates ten rows and resets to the first page when searching', async () => {
    const fixture = await ready();
    submit(fixture);
    const many = Array.from({ length: 12 }, (_, index) => ({
      ...row(String(index + 1)), name: 'Profesor ' + (index + 1),
    }));
    http.expectOne(request => request.url === BASE)
      .flush({ success: true, data: { rows: many, total: 12 } });
    await fixture.whenStable();
    fixture.detectChanges();
    const body: HTMLElement = fixture.nativeElement;
    expect(body.querySelectorAll('tbody tr')).toHaveLength(10);
    expect(body.textContent).toContain('Mostrando 1–10 de 12');
    (body.querySelector('.page-item:last-child button') as HTMLButtonElement).click();
    fixture.detectChanges();
    expect(body.querySelectorAll('tbody tr')).toHaveLength(2);
    expect(body.textContent).toContain('Mostrando 11–12 de 12');
    const input = body.querySelector('app-search input') as HTMLInputElement;
    input.value = 'Profesor';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    expect(body.querySelectorAll('tbody tr')).toHaveLength(10);
    expect(body.textContent).toContain('Mostrando 1–10 de 12');
    fixture.destroy();
  });

  it('limits the page window to two before/two after with ellipses', async () => {
    const fixture = await ready();
    submit(fixture);
    const many = Array.from({ length: 120 }, (_, index) => ({
      ...row(String(index + 1)), name: 'Profesor ' + (index + 1),
    }));
    http.expectOne(request => request.url === BASE)
      .flush({ success: true, data: { rows: many, total: 120 } });
    await fixture.whenStable();
    fixture.detectChanges();
    const labels = () => Array.from(fixture.nativeElement.querySelectorAll('nav .page-item'))
      .map((item) => (item as Element).textContent?.trim());
    expect(labels()).toEqual(['Anterior', '1', '2', '3', '...', '12', 'Siguiente']);
    (fixture.componentInstance as any).setPage(6);
    fixture.detectChanges();
    expect(labels()).toEqual(['Anterior', '1', '...', '4', '5', '6', '7', '8', '...', '12', 'Siguiente']);
    fixture.destroy();
  });

  it('filters all visible fields and shows an empty search state', async () => {
    const fixture = await ready();
    submit(fixture);
    http.expectOne(request => request.url === BASE)
      .flush({ success: true, data: { rows: [row('2'), row('3', false)], total: 2 } });
    await fixture.whenStable();
    fixture.detectChanges();
    const body: HTMLElement = fixture.nativeElement;
    const input = body.querySelector('app-search input') as HTMLInputElement;
    input.value = 'No Vigente';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    expect(body.querySelectorAll('tbody tr')).toHaveLength(1);
    expect(body.textContent).toContain('Mostrando 1–1 de 1');
    input.value = 'inexistente';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    expect(body.querySelector('table')).toBeNull();
    expect(body.textContent).toContain('No se encontraron registros.');
    fixture.destroy();
  });

  it('shows Sin Datos for an empty year and sanitizes server errors', async () => {
    const fixture = await ready();
    submit(fixture);
    http.expectOne(request => request.url === BASE)
      .flush({ success: true, data: { rows: [], total: 0 } });
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Sin Datos');
    expect(fixture.nativeElement.querySelector('[data-action=export]').disabled).toBe(true);
    submit(fixture);
    http.expectOne(request => request.url === BASE)
      .flush('private-sql', { status: 503, statusText: 'private' });
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No se pudieron cargar las vigencias.');
    expect(fixture.nativeElement.textContent).not.toContain('private');
    fixture.destroy();
  });
});
