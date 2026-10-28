import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { NgSelectComponent } from '@ng-select/ng-select';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { UnansweredTeacherSurvey } from './unanswered-teacher-survey';

const filters = {
  success: true,
  message: null,
  data: {
    periods: [{ value: '2024A', text: '2024 - Primer Semestre' }],
    studentTypes: [{ value: 'MBA', text: 'MBA' }, { value: 'MGP', text: 'Magíster en Gestión Pública' }],
    modalities: ['PT', 'FT'],
  },
};
const FILTERS = '/api/reports/postgraduate/unanswered-teacher-survey/filters';
const REPORT = '/api/reports/postgraduate/unanswered-teacher-survey';
const COMPLETE = '?periodo=2024A&tipoAlumno=MBA&modalidad=PT';

function row(overrides: Record<string, unknown> = {}) {
  return {
    period: '2024A', course: 'MBA101/1', name: 'Gestión (MBA101)', studentCode: 'A001',
    studentName: 'Alumno Prueba', ...overrides,
  };
}

describe('Informe Alumnos sin contestar encuesta docente', () => {
  let http: HttpTestingController;
  let fixture: ComponentFixture<UnansweredTeacherSurvey>;
  let root: HTMLElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [UnansweredTeacherSurvey],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(UnansweredTeacherSurvey);
    root = fixture.nativeElement;
    fixture.detectChanges();
    http.expectOne(FILTERS).flush(filters);
    await settle();
    http.expectOne(FILTERS + '?tipoAlumno=MBA').flush(filters);
    await settle();
  });

  afterEach(() => {
    http.verify();
    fixture.destroy();
    vi.restoreAllMocks();
  });

  async function settle() {
    await fixture.whenStable();
    fixture.detectChanges();
    await new Promise<void>((resolve) => setTimeout(resolve, 0));
    fixture.detectChanges();
  }

  function element(selector: string): HTMLElement {
    const found = root.querySelector<HTMLElement>(selector);
    if (!found) throw new Error('No se encontró ' + selector);
    return found;
  }

  async function selectFilter(control: 'period' | 'studentType' | 'modality', value: string) {
    fixture.componentInstance.form.controls[control].setValue(value);
    await settle();
  }

  async function submit() {
    element('form').dispatchEvent(new Event('submit'));
    await settle();
  }

  async function completeFilters() {
    await selectFilter('period', '2024A');
    await selectFilter('studentType', 'MBA');
    http.expectOne(FILTERS + '?tipoAlumno=MBA').flush(filters);
    await settle();
    await selectFilter('modality', 'PT');
  }

  // En esta versión de ng-select `items` es una señal expuesta como función.
  function itemsOf(select: NgSelectComponent): unknown {
    return (select.items as unknown as () => unknown)();
  }

  function selects(): NgSelectComponent[] {
    return fixture.debugElement.queryAll(By.directive(NgSelectComponent))
      .map(element => element.componentInstance as NgSelectComponent);
  }

  it('carga los catálogos en ng-select y recarga modalidades al cambiar el tipo', async () => {
    expect(root.querySelectorAll('ng-select[data-filter]')).toHaveLength(3);
    expect(itemsOf(selects()[0])).toEqual(filters.data.periods);
    expect(itemsOf(selects()[1])).toEqual(filters.data.studentTypes);
    await selectFilter('studentType', 'MBA');
    http.expectOne(FILTERS + '?tipoAlumno=MBA').flush(filters);
    await settle();
    expect(itemsOf(selects()[2])).toEqual(['PT', 'FT']);
  });

  it('preselecciona los primeros valores como el SAD', async () => {
    expect(fixture.componentInstance.form.getRawValue()).toEqual({
      period: '2024A', studentType: 'MBA', modality: 'PT',
    });
    expect(root.querySelector('[data-action="clear"]')).toBeNull();
  });

  it('deshabilita la modalidad cuando no hay tipo de alumno', async () => {
    fixture.componentInstance.form.controls.studentType.setValue('');
    await settle();
    http.expectOne(FILTERS).flush({ ...filters, data: { ...filters.data, modalities: [] } });
    await settle();
    expect(fixture.componentInstance.form.controls.modality.disabled).toBe(true);
    expect(selects()[2]).toBeDefined();
  });

  it('no consulta sin filtros completos y explica qué falta', async () => {
    fixture.componentInstance.form.reset({ period: '', studentType: '', modality: '' }, { emitEvent: false });
    await settle();
    await submit();
    expect(root.querySelector('[role="alert"]')?.textContent)
      .toContain('Seleccione período, tipo de alumno y modalidad');
    http.expectNone(REPORT);
  });

  it('muestra filas, total y columnas del SAD sin interpretar HTML', async () => {
    await completeFilters();
    await submit();
    http.expectOne(REPORT + COMPLETE).flush({
      success: true, message: null,
      data: { rows: [row({ studentName: '<img src=x onerror=alert(1)>' })], total: 1 },
    });
    await settle();
    const table = element('app-table table');
    expect(table.textContent).toContain('MBA101/1');
    expect(table.textContent).toContain('A001');
    expect(table.textContent).toContain('2024A');
    expect(table.querySelector('img')).toBeNull();
    expect(root.textContent).toContain('Total:1');
    expect(root.querySelector('[data-action="export"]')?.classList.contains('btn-create')).toBe(true);
  });

  it('uses the teacher-survey layout without Todos or survey questions', () => {
    expect(root.querySelectorAll('.page-header')).toHaveLength(2);
    expect(root.querySelector('h1')?.textContent).toContain('Alumnos sin contestar encuesta docente');
    expect(root.textContent).toContain('Período');
    expect(filters.data.studentTypes.some(option => option.value === 'Todos')).toBe(false);
    expect(root.querySelector('[data-glossary]')).toBeNull();
  });

  it('informa un resultado vacío sin inventar filas', async () => {
    await completeFilters();
    await submit();
    http.expectOne(REPORT + COMPLETE).flush({ success: true, message: null, data: { rows: [], total: 0 } });
    await settle();
    expect(root.querySelector('app-table table')).toBeNull();
    expect(root.textContent).toContain('Sin resultados para los filtros seleccionados.');
  });

  it('muestra un aviso sanitizado y descarta datos previos si el informe falla', async () => {
    await completeFilters();
    await submit();
    http.expectOne(REPORT + COMPLETE)
      .flush({ title: 'El informe no está disponible.' }, { status: 503, statusText: 'Unavailable' });
    await settle();
    expect(root.textContent).toContain('No se pudo cargar el informe.');
    expect(root.querySelector('app-table table')).toBeNull();
  });

  it('discards displayed rows and disables export as soon as filters change', async () => {
    await submit();
    http.expectOne(REPORT + COMPLETE).flush({ success: true, message: null, data: { rows: [row()], total: 1 } });
    await settle();
    expect(root.querySelector('app-table')).not.toBeNull();
    await selectFilter('period', '2024B');
    expect(root.querySelector('app-table')).toBeNull();
    expect((element('[data-action="export"]') as HTMLButtonElement).disabled).toBe(true);
  });

  it('ignores an older report response after a newer selection succeeds', async () => {
    await submit();
    const older = http.expectOne(REPORT + COMPLETE);
    await selectFilter('period', '2024B');
    await submit();
    const newer = http.expectOne(REPORT + COMPLETE.replace('2024A', '2024B'));
    newer.flush({ success: true, message: null, data: { rows: [row({ studentName: 'New selection', period: '2024B' })], total: 1 } });
    await settle();
    older.flush({ success: true, message: null, data: { rows: [row({ studentName: 'Old selection' })], total: 1 } });
    await settle();
    expect(root.textContent).toContain('New selection');
    expect(root.textContent).not.toContain('Old selection');
    expect((element('[data-action="show"]') as HTMLButtonElement).disabled).toBe(false);
  });

  it('ignores an older filter response and its incompatible modality', async () => {
    await selectFilter('studentType', 'MGP');
    const older = http.expectOne(FILTERS + '?tipoAlumno=MGP');
    expect(fixture.componentInstance.form.controls.modality.disabled).toBe(true);
    await selectFilter('studentType', 'MBA');
    const newer = http.expectOne(FILTERS + '?tipoAlumno=MBA');
    newer.flush(filters);
    await settle();
    older.flush({ ...filters, data: { ...filters.data, modalities: ['FT'] } });
    await settle();
    expect(fixture.componentInstance.form.getRawValue().modality).toBe('PT');
    expect(itemsOf(selects()[2])).toEqual(['PT', 'FT']);
  });

  it('keeps failed catalogue requests sanitized and prevents consulting stale modalities', async () => {
    await selectFilter('studentType', 'MGP');
    http.expectOne(FILTERS + '?tipoAlumno=MGP')
      .flush({ title: 'private-sql-detail' }, { status: 503, statusText: 'Unavailable' });
    await settle();
    expect(root.textContent).toContain('No se pudieron cargar los filtros del informe.');
    expect(root.textContent).not.toContain('private-sql-detail');
    expect(fixture.componentInstance.form.controls.modality.disabled).toBe(true);
    await submit();
    http.expectNone(request => request.url === REPORT);
    expect(root.querySelector('app-table')).toBeNull();
  });

  it.each(['=1', '+1', '-1', '@x', '  =1', '\t=1', '\r=1', '\n=1'])
    ('neutralizes spreadsheet formula prefix %j', async value => {
      let download: Blob | undefined;
      vi.spyOn(URL, 'createObjectURL').mockImplementation(blob => { download = blob as Blob; return 'blob:safe'; });
      vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => undefined);
      vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => undefined);
      await submit();
      http.expectOne(REPORT + COMPLETE).flush({ success: true, message: null,
        data: { rows: [row({ studentName: value })], total: 1 } });
      await settle();
      element('[data-action="export"]').click();
      expect(await download!.text()).toContain("'" + value);
    });

  it('exporta las filas visibles como CSV y no descarga vacío', async () => {
    const downloads: Blob[] = [];
    const created: string[] = [];
    vi.spyOn(URL, 'createObjectURL').mockImplementation((blob) => { downloads.push(blob as Blob); return 'blob:prueba'; });
    vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => undefined);
    vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(function (this: HTMLAnchorElement) {
      created.push(this.download);
    });

    const exportButton = () => element('[data-action="export"]') as HTMLButtonElement;
    expect(exportButton().disabled).toBe(true);
    await completeFilters();
    await submit();
    http.expectOne(REPORT + COMPLETE).flush({ success: true, message: null, data: { rows: [row(), row({ studentName: '=SUM(1;2)', name: 'Cátedra; "Prueba"\nOtra' })], total: 2 } });
    await settle();
    expect(exportButton().disabled).toBe(false);
    exportButton().click();

    expect(created).toEqual(['AlumnosSinContestarEncuesta.csv']);
    // Blob.text() consume el BOM: se verifica en los bytes reales del archivo.
    const bytes = new Uint8Array(await downloads[0].arrayBuffer());
    expect([bytes[0], bytes[1], bytes[2]]).toEqual([0xEF, 0xBB, 0xBF]);
    const text = await downloads[0].text();
    expect(text.split('\r\n')[0]).toBe('Curso;Cátedra;Código Alumno;Nombre Alumno;Periodo');
    expect(text).toContain('MBA101/1');
    expect(text).toContain('A001;Alumno Prueba;2024A');
    expect(text).toContain('"\'=SUM(1;2)"');
    expect(text).toContain('"Cátedra; ""Prueba""\nOtra"');
    expect(created).toHaveLength(1);
    expect(URL.revokeObjectURL).toHaveBeenCalledWith('blob:prueba');
  });
});
