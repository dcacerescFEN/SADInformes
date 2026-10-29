import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { NgSelectComponent } from '@ng-select/ng-select';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TeacherCourses } from './teacher-courses';

const FILTERS = '/api/reports/postgraduate/teacher-courses/filters';
const REPORT = '/api/reports/postgraduate/teacher-courses';
const filters = {
  success: true, message: null, data: {
    years: [{ value: '2024', text: '2024' }, { value: '2023', text: '2023' }],
    departments: [{ value: 'Ingeniería', text: 'Ingeniería' }, { value: 'Economía', text: 'Economía' }],
  },
};
const row = (values: Record<string, unknown> = {}) => ({
  professorCode: 'PROF001', name: 'ANA PEREZ', evaluatedCourses: 3, average: '5,250000', classification: 'AQ', ...values,
});

describe('Postgraduate teacher courses', () => {
  let fixture: ComponentFixture<TeacherCourses>;
  let http: HttpTestingController;
  let root: HTMLElement;

  async function settle() { await fixture.whenStable(); fixture.detectChanges(); }
  async function change(control: 'year' | 'department', value: string) {
    fixture.componentInstance.form.controls[control].setValue(value);
    await settle();
  }
  async function submit() { root.querySelector('form')!.dispatchEvent(new Event('submit')); await settle(); }

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TeacherCourses],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(TeacherCourses);
    root = fixture.nativeElement;
    fixture.detectChanges();
    http.expectOne(request => request.url === FILTERS).flush(filters);
    await settle();
  });

  afterEach(() => http.verify());

  it('defaults to the first year and department from the server catalogue', () => {
    expect(fixture.componentInstance.form.getRawValue()).toEqual({ year: '2024', department: 'Ingeniería' });
    expect(fixture.componentInstance.form.controls.year.enabled).toBe(true);
  });

  it('queries the report with the selected period and department only on submit', async () => {
    expect(http.match(REPORT)).toHaveLength(0);
    await change('department', 'Economía');
    await submit();
    const request = http.expectOne(request => request.url === REPORT);
    expect(request.request.params.get('periodo')).toBe('2024');
    expect(request.request.params.get('departamento')).toBe('Economía');
    request.flush({ success: true, message: null, data: { rows: [row()], total: 1 } });
    await settle();
    expect(root.textContent).toContain('ANA PEREZ');
    expect(root.textContent).toContain('Total:1');
  });

  it('discards previous rows when a filter changes', async () => {
    await submit();
    http.expectOne(request => request.url === REPORT)
      .flush({ success: true, message: null, data: { rows: [row()], total: 1 } });
    await settle();
    expect(root.textContent).toContain('ANA PEREZ');
    await change('year', '2023');
    expect(root.textContent).not.toContain('ANA PEREZ');
  });

  it('ignores an older response that resolves after a newer filter change', async () => {
    await submit();
    const stale = http.expectOne(request => request.url === REPORT);
    await change('year', '2023');
    await submit();
    const fresh = http.expectOne(request => request.url === REPORT);
    fresh.flush({ success: true, message: null, data: { rows: [row({ name: 'LUIS SOTO' })], total: 1 } });
    await settle();
    stale.flush({ success: true, message: null, data: { rows: [row()], total: 1 } });
    await settle();
    expect(root.textContent).toContain('LUIS SOTO');
    expect(root.textContent).not.toContain('ANA PEREZ');
  });

  it('shows a sanitized error and keeps the form when the report fails', async () => {
    await submit();
    http.expectOne(request => request.url === REPORT)
      .flush({ title: 'private-database-detail' }, { status: 503, statusText: 'Unavailable' });
    await settle();
    expect(root.textContent).toContain('No se pudo cargar el informe.');
    expect(root.textContent).not.toContain('private-database-detail');
    expect(root.textContent).toContain('Mostrar');
  });

  it('keeps the empty result notice without rows', async () => {
    await submit();
    http.expectOne(request => request.url === REPORT)
      .flush({ success: true, message: null, data: { rows: [], total: 0 } });
    await settle();
    expect(root.textContent).toContain('No se encontraron evaluaciones');
  });

  it('does not request the report until both filters are available', async () => {
    http.expectNone(REPORT);
    expect(root.querySelector('[data-action="show"]')).toBeTruthy();
  });

  it('exports the legacy CSV columns with BOM, semicolons and formula protection', async () => {
    let download: Blob | undefined;
    let filename = '';
    vi.spyOn(URL, 'createObjectURL').mockImplementation(blob => { download = blob as Blob; return 'blob:safe'; });
    vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => undefined);
    vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(function (this: HTMLAnchorElement) { filename = this.download; });
    expect((root.querySelector('[data-action="export"]') as HTMLButtonElement).disabled).toBe(true);
    await submit();
    http.expectOne(request => request.url === REPORT).flush({
      success: true, message: null,
      data: { rows: [row({ name: '=1+1', classification: 'AQ;"PQ"' })], total: 1 },
    });
    await settle();
    (root.querySelector('[data-action="export"]') as HTMLButtonElement).click();
    expect(filename).toBe('CursosProfesores_Postgrado.csv');
    const bytes = new Uint8Array(await download!.arrayBuffer());
    expect([...bytes.slice(0, 3)]).toEqual([0xef, 0xbb, 0xbf]);
    const text = await download!.text();
    const lines = text.split('\r\n');
    expect(lines[0]).toBe('Cod_Profesor;Nombre;#CatedrasEvaluadas;Promedio Ev. Docente Profesor;Tipo AQ_PQ_O');
    expect(lines[1]).toBe("PROF001;'=1+1;3;5,250000;\"AQ;\"\"PQ\"\"\"");
  });

  it('renders both catalogue filters as ng-select controls', () => {
    const selects = fixture.debugElement.queryAll(By.directive(NgSelectComponent));
    expect(selects.length).toBe(2);
    expect(root.querySelector('[data-filter="year"]')).toBeTruthy();
    expect(root.querySelector('[data-filter="department"]')).toBeTruthy();
  });
});
