import { TestBed, ComponentFixture } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { NavigationService } from '../../core/navigation/navigation-service';
import { HonorRoll } from './honor-roll';

const BASE = '/api/reports/others/honor-roll';
const FILTERS = {
  success: true,
  data: {
    careers: [
      { value: 'IC', text: 'Ingeniería Comercial' },
      { value: 'IICG', text: 'Ingeniería en Información y Control de Gestión' },
      { value: 'CA', text: 'Contador Auditor' },
    ],
    years: [
      { value: '2020', text: '2020' },
      { value: '2019', text: '2019' },
    ],
  },
};
const ROW = {
  position: 1, name: 'ANA PÉREZ', rut: '11111111-1', studentCode: 'A/1',
  entrySemester: '2019-1', situation: 'REGULAR', average: '6.5', percentile: '99.5',
};
const REPORT = { career: 'IC', entryYear: '2019', honorYear: '2020', rows: [ROW] };
const HEADERS = [
  'Posición', 'Foto', 'Nombre', 'Rut', 'Código Alumno',
  'Semestre de Ingreso', 'Situación', 'Promedio', 'Percentil',
];

describe('Cuadro de Honor', () => {
  let http: HttpTestingController;
  let studentGradesUrl: ReturnType<typeof vi.fn>;
  beforeEach(() => {
    studentGradesUrl = vi.fn((code: string) =>
      `https://sad.example.test/alumnos/students/detail/${code}/grade-reports/grades`);
    TestBed.configureTestingModule({
      imports: [HonorRoll],
      providers: [
        provideHttpClient(), provideHttpClientTesting(),
        { provide: NavigationService, useValue: { studentGradesUrl } },
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => { http.verify(); TestBed.resetTestingModule(); });

  function render(): ComponentFixture<HonorRoll> {
    const fixture = TestBed.createComponent(HonorRoll);
    fixture.detectChanges();
    return fixture;
  }

  async function ready(filters: object = FILTERS): Promise<ComponentFixture<HonorRoll>> {
    const fixture = render();
    http.expectOne(BASE + '/filters').flush(filters);
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture;
  }

  function submit(fixture: ComponentFixture<HonorRoll>): void {
    fixture.nativeElement.querySelector('form')!.dispatchEvent(new Event('submit'));
    fixture.detectChanges();
  }

  async function query(report: unknown = REPORT): Promise<ComponentFixture<HonorRoll>> {
    const fixture = await ready();
    fixture.componentInstance.form.patchValue({ entryYear: '2019', honorYear: '2020' });
    submit(fixture);
    http.expectOne(req => req.url === BASE + '/report')
      .flush({ success: true, message: null, data: report });
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture;
  }

  function heading(fixture: ComponentFixture<HonorRoll>): string {
    return (fixture.nativeElement.querySelector('[data-heading="honor-roll"]') as HTMLElement | null)
      ?.textContent?.replace(/\s+/g, ' ').trim() ?? '';
  }

  it('carga los filtros una sola vez y no consulta el informe al iniciar', async () => {
    const fixture = render();
    const request = http.expectOne(BASE + '/filters');
    expect(request.request.method).toBe('GET');
    request.flush(FILTERS);
    await fixture.whenStable();
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('h1')?.textContent?.trim()).toBe('Cuadro de Honor');
    expect(root.querySelectorAll('ng-select[data-filter]')).toHaveLength(3);
    expect(fixture.componentInstance.form.getRawValue()).toEqual({
      career: 'IC', entryYear: '2020', honorYear: '2020',
    });
    expect(root.textContent).toContain('Ingeniería Comercial');
    http.expectNone(BASE + '/report');
    expect(root.querySelector('app-table')).toBeNull();
    expect(root.textContent).not.toContain('Total:');
  });

  it('mantiene las carreras legacy IC, IICG y CA', async () => {
    const fixture = await ready();
    const root: HTMLElement = fixture.nativeElement;
    const careers: readonly [string, string][] = [
      ['IC', 'Ingeniería Comercial'],
      ['IICG', 'Ingeniería en Información y Control de Gestión'],
      ['CA', 'Contador Auditor'],
    ];
    for (const [value, text] of careers) {
      fixture.componentInstance.form.controls.career.setValue(value);
      fixture.detectChanges();
      expect(root.textContent).toContain(text);
    }
    http.expectNone(BASE + '/report');
  });

  it('envía los tres parámetros y muestra las nueve columnas legacy', async () => {
    const fixture = await ready();
    http.expectNone(BASE + '/report');
    fixture.componentInstance.form.patchValue({ career: 'IICG', entryYear: '2019', honorYear: '2020' });
    submit(fixture);
    const request = http.expectOne(req => req.url === BASE + '/report');
    expect(request.request.method).toBe('GET');
    expect(request.request.params.keys().sort())
      .toEqual(['career', 'entryYear', 'honorYear']);
    expect(request.request.params.get('career')).toBe('IICG');
    expect(request.request.params.get('entryYear')).toBe('2019');
    expect(request.request.params.get('honorYear')).toBe('2020');
    expect(fixture.nativeElement.querySelector('app-table table')?.getAttribute('aria-busy'))
      .toBe('true');
    request.flush({
      success: true, message: null,
      data: { ...REPORT, career: 'IICG' },
    });
    await fixture.whenStable();
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;
    expect([...root.querySelectorAll('app-table thead th')]
      .map(header => header.textContent?.trim())).toEqual(HEADERS);
    expect(root.querySelectorAll('app-table tbody tr')).toHaveLength(1);
    expect(root.querySelector('app-table table')?.getAttribute('aria-busy')).toBe('false');
    expect(root.textContent).toContain('ANA PÉREZ');
    expect(root.textContent).toContain('11111111-1');
    expect(root.textContent).toContain('2019-1');
    expect(root.textContent).toContain('REGULAR');
    expect(root.textContent).toContain('6.5');
    expect(root.textContent).toContain('99.5');
    expect(root.textContent).toContain('Total: 1');
    expect(root.querySelector('app-search')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('app-table tbody img')?.getAttribute('src'))
      .toBe('https://fotos.fen.uchile.cl/11111111-1.jpg');
    http.expectNone(BASE + '/report');
  });

  it('muestra el título legacy con la carrera y ambos años del informe', async () => {
    const fixture = await query();
    expect(heading(fixture)).toBe(
      'Cuadro de Honor Ingeniería Comercial Año de Ingreso 2019 '
      + 'considerando las notas hasta el período 2020');
  });

  it('enlaza el código con las notas de la ficha nueva de SADAlumnos', async () => {
    const fixture = await query();
    const link = fixture.nativeElement.querySelector('app-table tbody td a') as HTMLAnchorElement;
    expect(studentGradesUrl).toHaveBeenCalledTimes(1);
    expect(studentGradesUrl).toHaveBeenCalledWith('A/1');
    expect(link.textContent?.trim()).toBe('A/1');
    expect(link.getAttribute('href'))
      .toBe('https://sad.example.test/alumnos/students/detail/A/1/grade-reports/grades');
  });

  it('usa la foto por defecto cuando falta el rut', async () => {
    const fixture = await query({ ...REPORT, rows: [{ ...ROW, rut: '' }] });
    expect(fixture.nativeElement.querySelector('app-table tbody img')?.getAttribute('src'))
      .toBe('https://fotos.fen.uchile.cl/perfilNN.jpg');
  });

  it('no consulta el informe si falta alguno de los años', async () => {
    const fixture = await ready();
    fixture.componentInstance.form.patchValue({ entryYear: '' });
    submit(fixture);
    http.expectNone(BASE + '/report');
    let root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('[role="alert"]')?.textContent)
      .toContain('Debe seleccionar el año de ingreso.');
    expect(root.querySelector('app-table')).toBeNull();
    fixture.componentInstance.form.patchValue({ entryYear: '2019', honorYear: '' });
    submit(fixture);
    http.expectNone(BASE + '/report');
    root = fixture.nativeElement;
    expect(root.querySelector('[role="alert"]')?.textContent)
      .toContain('Debe seleccionar el año del cuadro.');
    expect(root.querySelector('app-table')).toBeNull();
  });

  it('muestra Sin Datos con el total en cero cuando no hay alumnos', async () => {
    const fixture = await query({ ...REPORT, rows: [] });
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('app-table')).not.toBeNull();
    expect(root.textContent).toContain('Sin Datos');
    expect(root.textContent).toContain('Total: 0');
    expect(studentGradesUrl).not.toHaveBeenCalled();
  });

  it('sanitiza un informe fallido sin tabla ni enlaces', async () => {
    const fixture = await ready();
    fixture.componentInstance.form.patchValue({ entryYear: '2019', honorYear: '2020' });
    submit(fixture);
    http.expectOne(req => req.url === BASE + '/report')
      .flush('private', { status: 503, statusText: 'private' });
    await fixture.whenStable();
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;
    expect(root.textContent).toContain('No se pudo cargar el informe.');
    expect(root.textContent).not.toContain('private');
    expect(root.querySelector('app-table')).toBeNull();
    expect(root.textContent).not.toContain('Total:');
    expect(studentGradesUrl).not.toHaveBeenCalled();
  });

  it('filtra en el buscador sin volver a consultar el endpoint', async () => {
    const fixture = await query();
    const input = fixture.nativeElement.querySelector('app-search input') as HTMLInputElement;
    input.value = 'ZZZ';
    input.dispatchEvent(new Event('input'));
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No se encontraron registros');
    http.expectNone(BASE + '/report');
  });
});
