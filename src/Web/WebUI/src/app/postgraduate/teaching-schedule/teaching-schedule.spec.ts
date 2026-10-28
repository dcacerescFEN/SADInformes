import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { NgSelectComponent } from '@ng-select/ng-select';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TeachingSchedule } from './teaching-schedule';

const FILTERS = '/api/reports/postgraduate/teaching-schedule/filters';
const REPORT = '/api/reports/postgraduate/teaching-schedule';
const DESCRIPTION = 'Descripcion larga de promocion';
const filters = { success: true, message: null, data: {
  periodTypes: [{ value: 'PERÍODO', text: 'Bimestre' }, { value: 'SEMESTRE', text: 'Semestre' }, { value: 'AÑO', text: 'Año' }],
  periods: [{ value: '2024B', text: '2024 - Bimestre' }],
  programs: [{ value: 'MBA', text: 'MBA' }, { value: 'MGP', text: 'MGP' }],
  modalities: ['Todas', 'PT'],
  promotions: [{ value: 'Todas', text: 'Todas' }, { value: DESCRIPTION, text: 'P2024' }],
} };
const row = (values: Record<string, unknown> = {}) => ({ period: '2024B', periodLabel: '2024 - Bimestre', course: 'MBA101/1',
  name: 'Cátedra', type: 'Obligatoria', units: 3, teachers: 'Profesor A', classes: 'Lunes', ...values });

describe('Postgraduate teaching schedule', () => {
  let fixture: ComponentFixture<TeachingSchedule>;
  let http: HttpTestingController;
  let root: HTMLElement;
  async function settle() { await fixture.whenStable(); fixture.detectChanges(); }
  function requestFilters() { return http.expectOne(request => request.url === FILTERS); }
  async function change(control: 'periodType' | 'period' | 'program' | 'modality' | 'promotion', value: string) {
    fixture.componentInstance.form.controls[control].setValue(value);
    await settle();
  }
  async function submit() { root.querySelector('form')!.dispatchEvent(new Event('submit')); await settle(); }
  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [TeachingSchedule],
      providers: [provideHttpClient(), provideHttpClientTesting()] }).compileComponents();
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(TeachingSchedule);
    root = fixture.nativeElement;
    fixture.detectChanges();
    const initial = requestFilters();
    expect(initial.request.params.get('tipoPeriodo')).toBe('PERÍODO');
    expect(initial.request.params.has('tipoAlumno')).toBe(false);
    initial.flush(filters);
    await settle();
    const scoped = requestFilters();
    expect(scoped.request.params.get('tipoAlumno')).toBe('MBA');
    scoped.flush(filters);
    await settle();
  });
  afterEach(() => { http.verify(); fixture.destroy(); vi.restoreAllMocks(); });

  it('uses five shared filters and the legacy initial selections', () => {
    expect(root.querySelectorAll('.page-header')).toHaveLength(2);
    expect(root.querySelectorAll('ng-select[data-filter]')).toHaveLength(5);
    expect(fixture.componentInstance.form.getRawValue()).toEqual({
      periodType: 'PERÍODO', program: 'MBA', modality: 'Todas', promotion: 'Todas', period: '2024B',
    });
    const promotion = fixture.debugElement.queryAll(By.directive(NgSelectComponent))[3].componentInstance as NgSelectComponent;
    const items = (promotion.items as unknown as () => unknown)();
    expect(items).toEqual(filters.data.promotions);
    expect(root.textContent).toContain('Programación Docente Postgrado X Programa y Modalidad');
  });

  it('displays Promocion but sends the complete Descripcion, without replacing or truncating it', async () => {
    await change('promotion', DESCRIPTION);
    await submit();
    const request = http.expectOne(request => request.url === REPORT);
    expect(request.request.params.get('promocion')).toBe(DESCRIPTION);
    expect(request.request.params.get('promocion')).not.toBe('P2024');
    expect(request.request.params.get('tipoPeriodo')).toBe('PERÍODO');
    expect(request.request.params.get('tipoAlumno')).toBe('MBA');
    expect(request.request.params.get('modalidad')).toBe('Todas');
    expect(request.request.params.get('periodo')).toBe('2024B');
    request.flush({ success: true, message: null, data: { rows: [row()], total: 1 } });
    await settle();
    expect(root.querySelectorAll('app-table th')).toHaveLength(7);
    expect(root.textContent).toContain('Profesor A');
    expect(root.textContent).toContain('Total:1');
  });

  it.each([['SEMESTRE', '20241'], ['AÑO', '2024']])('reloads the period catalogue for %s without rewriting promotion', async (kind, period) => {
    await change('promotion', DESCRIPTION);
    await change('periodType', kind);
    const request = requestFilters();
    expect(request.request.params.get('tipoPeriodo')).toBe(kind);
    expect(fixture.componentInstance.form.controls.period.disabled).toBe(true);
    request.flush({ ...filters, data: { ...filters.data, periods: [{ value: period, text: period }] } });
    await settle();
    expect(fixture.componentInstance.form.getRawValue().period).toBe(period);
    expect(fixture.componentInstance.form.getRawValue().promotion).toBe(DESCRIPTION);
    await submit();
    const report = http.expectOne(request => request.url === REPORT);
    expect(report.request.params.get('tipoPeriodo')).toBe(kind);
    expect(report.request.params.get('periodo')).toBe(period);
    report.flush({ success: true, message: null, data: { rows: [], total: 0 } });
    await settle();
    expect(root.textContent).toContain('No se encontraron cursos');
  });

  it('clears report/export on filter changes and ignores older report responses', async () => {
    await submit();
    const older = http.expectOne(request => request.url === REPORT);
    await change('period', '2024C');
    expect((root.querySelector('[data-action="export"]') as HTMLButtonElement).disabled).toBe(true);
    await submit();
    const newer = http.expectOne(request => request.url === REPORT);
    newer.flush({ success: true, message: null, data: { rows: [row({ name: 'New course' })], total: 1 } });
    await settle();
    older.flush({ success: true, message: null, data: { rows: [row({ name: 'Old course' })], total: 1 } });
    await settle();
    expect(root.textContent).toContain('New course');
    expect(root.textContent).not.toContain('Old course');
    await change('promotion', DESCRIPTION);
    expect(root.querySelector('app-table')).toBeNull();
  });

  it('retains a newer promotion selection while a catalogue request is pending', async () => {
    await change('program', 'MGP');
    const pending = requestFilters();
    await change('promotion', DESCRIPTION);
    pending.flush(filters);
    await settle();
    expect(fixture.componentInstance.form.getRawValue().promotion).toBe(DESCRIPTION);
    await submit();
    const report = http.expectOne(request => request.url === REPORT);
    expect(report.request.params.get('promocion')).toBe(DESCRIPTION);
    report.flush({ success: true, message: null, data: { rows: [], total: 0 } });
    await settle();
  });

  it('ignores out-of-order filter catalogues', async () => {
    await change('periodType', 'SEMESTRE');
    const older = requestFilters();
    await change('periodType', 'AÑO');
    const newer = requestFilters();
    newer.flush({ ...filters, data: { ...filters.data, periods: [{ value: '2024', text: '2024' }] } });
    await settle();
    older.flush({ ...filters, data: { ...filters.data, periods: [{ value: '20241', text: 'Semestre' }] } });
    await settle();
    expect(fixture.componentInstance.form.getRawValue().period).toBe('2024');
  });

  it('sanitizes failures and does not consult while catalogues are unavailable', async () => {
    await change('program', 'MGP');
    requestFilters().flush({ title: 'private-sql' }, { status: 503, statusText: 'Unavailable' });
    await settle();
    expect(root.textContent).toContain('No se pudieron cargar los filtros del informe.');
    expect(root.textContent).not.toContain('private-sql');
    await submit();
    http.expectNone(request => request.url === REPORT);
    expect(root.querySelector('app-table')).toBeNull();
  });

  it('escapes table content and sanitizes report failures', async () => {
    await submit();
    http.expectOne(request => request.url === REPORT).flush({ success: true, message: null,
      data: { rows: [row({ teachers: '<img src=x onerror=alert(1)>' })], total: 1 } });
    await settle();
    expect(root.querySelector('app-table img')).toBeNull();
    expect(root.querySelector('app-table')?.textContent).toContain('<img');
    await submit();
    http.expectOne(request => request.url === REPORT).flush({ title: 'private-sql' }, { status: 503, statusText: 'Unavailable' });
    await settle();
    expect(root.textContent).toContain('No se pudo cargar el informe.');
    expect(root.textContent).not.toContain('private-sql');
    expect(root.querySelector('app-table')).toBeNull();
  });

  it('exports legacy columns/file name with BOM, escaping, nullable units and formula protection', async () => {
    let download: Blob | undefined;
    let filename = '';
    vi.spyOn(URL, 'createObjectURL').mockImplementation(blob => { download = blob as Blob; return 'blob:safe'; });
    vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => undefined);
    vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(function(this: HTMLAnchorElement) { filename = this.download; });
    expect((root.querySelector('[data-action="export"]') as HTMLButtonElement).disabled).toBe(true);
    await submit();
    http.expectOne(request => request.url === REPORT).flush({ success: true, message: null,
      data: { rows: [row({ units: null, teachers: '  =SUM(1;2)', classes: 'Lunes; "Sala"\nMartes' }), row({ units: 0 })], total: 2 } });
    await settle();
    (root.querySelector('[data-action="export"]') as HTMLButtonElement).click();
    expect(filename).toBe('CursosProfesores.csv');
    const bytes = new Uint8Array(await download!.arrayBuffer());
    expect([...bytes.slice(0, 3)]).toEqual([0xef, 0xbb, 0xbf]);
    const csv = await download!.text();
    expect(csv.split('\r\n')[0]).toBe('PeriodoLargo;Curso;Cátedra;Tipo;Ud;Profesores;Clases');
    expect(csv).toContain('Obligatoria;;');
    expect(csv).toContain('Obligatoria;0;');
    expect(csv).toContain('"\'  =SUM(1;2)"');
    expect(csv).toContain('"Lunes; ""Sala""\nMartes"');
  });
});
