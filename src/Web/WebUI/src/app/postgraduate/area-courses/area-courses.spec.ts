import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { AreaCourses } from './area-courses';

const BASE = '/api/reports/postgraduate/area-courses';
const catalog = { success: true, data: { years: ['2026', '2024'] } };
const row = (id = '2') => ({
  id, description: 'Gestión', area: 'ADM', period: '2024F', courseCode: 'MBA600',
  section: '01', credits: '4', teacherName: 'Ana Pérez', personCode: 'P/1',
  teacherType: 'FULL TIME', start: '01/03/2024', end: '30/06/2024', program: 'MBA',
  modality: 'SUNRISE', promotion: '2024', courseName: 'Gestión Estratégica',
  courseType: 'Cátedra', scheduleType: 'Diurno', aqpq: 'AQ', teachingCategory: 'SA',
  confirmed: 'Confirmado', dedicatedHours: '4.5', totalStudents: '20',
  postgraduateStudents: '15', undergraduateStudents: '5',
  teachingEvaluationAverage: '6,25', courseObservations: 'Observación',
  observationUser: 'usuario 01-03-2024 10:00',
});

describe('Postgraduate area courses', () => {
  let http: HttpTestingController;
  let downloads: HTMLAnchorElement[];
  const createUrl = vi.fn(() => 'blob:area-courses');
  const revokeUrl = vi.fn();

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [AreaCourses],
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
    const fixture = TestBed.createComponent(AreaCourses);
    fixture.detectChanges();
    http.expectOne(request => request.url === BASE + '/filters').flush(catalog);
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture;
  }

  const submit = (fixture: any) =>
    fixture.nativeElement.querySelector('form').dispatchEvent(new Event('submit'));

  it('defaults both years and waits for Mostrar', async () => {
    const fixture = await ready();
    expect(fixture.componentInstance.form.getRawValue()).toEqual({ year1: '2026', year2: '2026' });
    expect(fixture.nativeElement.querySelectorAll('ng-select[data-filter]')).toHaveLength(2);
    http.expectNone(request => request.url === BASE);
    expect(fixture.nativeElement.querySelector('[data-action=export]').disabled).toBe(true);
    const show = fixture.nativeElement.querySelector('[data-action=show]');
    expect(show.textContent).toContain('Mostrar');
    expect(show.querySelector('i.bi-search')).not.toBeNull();
    fixture.destroy();
  });

  it('renders all twenty-eight columns and exports the shown selection', async () => {
    const fixture = await ready();
    fixture.componentInstance.form.controls.year2.setValue('2024');
    submit(fixture);
    const report = http.expectOne(request => request.url === BASE);
    expect(report.request.params.get('year1')).toBe('2026');
    expect(report.request.params.get('year2')).toBe('2024');
    report.flush({ success: true, data: { rows: [row()], total: 1 } });
    await fixture.whenStable();
    fixture.detectChanges();
    const body: HTMLElement = fixture.nativeElement;
    expect(body.querySelectorAll('tbody tr:first-child td')).toHaveLength(28);
    expect(body.textContent).toContain('MBA600');
    expect(body.textContent).toContain('6,25');
    expect(body.textContent).toContain('Observación');
    expect((body.querySelector('[data-action=export]') as HTMLButtonElement).disabled).toBe(false);
    (body.querySelector('[data-action=export]') as HTMLButtonElement).click();
    const request = http.expectOne(request => request.url === BASE + '/download');
    expect(request.request.params.get('year1')).toBe('2026');
    expect(request.request.params.get('year2')).toBe('2024');
    request.flush(new Blob(['xlsx']));
    await fixture.whenStable();
    fixture.detectChanges();
    expect(downloads).toHaveLength(1);
    expect(downloads[0].download).toBe('AlumnosPregradoPostgrado.xlsx');
    expect(revokeUrl).toHaveBeenCalledWith('blob:area-courses');
    fixture.destroy();
  });

  it('paginates ten rows with two before/two after and ellipses', async () => {
    const fixture = await ready();
    submit(fixture);
    const many = Array.from({ length: 120 }, (_, index) => ({
      ...row(String(index + 1)), description: 'Area ' + (index + 1),
    }));
    http.expectOne(request => request.url === BASE)
      .flush({ success: true, data: { rows: many, total: 120 } });
    await fixture.whenStable();
    fixture.detectChanges();
    const body: HTMLElement = fixture.nativeElement;
    const labels = () => Array.from(body.querySelectorAll('nav .page-item'))
      .map((item) => (item as Element).textContent?.trim());
    expect(body.querySelectorAll('tbody tr')).toHaveLength(10);
    expect(labels()).toEqual(['Anterior', '1', '2', '3', '...', '12', 'Siguiente']);
    (fixture.componentInstance as any).setPage(6);
    fixture.detectChanges();
    expect(labels()).toEqual(['Anterior', '1', '...', '4', '5', '6', '7', '8', '...', '12', 'Siguiente']);
    fixture.destroy();
  });

  it('filters all visible fields, resets the page and shows an empty search state', async () => {
    const fixture = await ready();
    submit(fixture);
    const many = Array.from({ length: 12 }, (_, index) => ({
      ...row(String(index + 1)), description: 'Area ' + (index + 1),
    }));
    http.expectOne(request => request.url === BASE)
      .flush({ success: true, data: { rows: many, total: 12 } });
    await fixture.whenStable();
    fixture.detectChanges();
    const body: HTMLElement = fixture.nativeElement;
    (body.querySelector('.page-item:last-child button') as HTMLButtonElement).click();
    fixture.detectChanges();
    expect(body.textContent).toContain('Mostrando 11–12 de 12');
    const input = body.querySelector('app-search input') as HTMLInputElement;
    input.value = 'Area 11';
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

  it('cancels a pending report and clears results when a year changes', async () => {
    const fixture = await ready();
    submit(fixture);
    const pending = http.expectOne(request => request.url === BASE);
    fixture.componentInstance.form.controls.year1.setValue('2024');
    expect(pending.cancelled).toBe(true);
    expect(fixture.nativeElement.querySelector('[data-action=export]').disabled).toBe(true);
    expect(fixture.nativeElement.querySelector('table')).toBeNull();
    fixture.destroy();
  });

  it('shows Sin Datos for an empty result and sanitizes server errors', async () => {
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
    expect(fixture.nativeElement.textContent).toContain('No se pudo cargar el reporte.');
    expect(fixture.nativeElement.textContent).not.toContain('private');
    fixture.destroy();
  });
});
