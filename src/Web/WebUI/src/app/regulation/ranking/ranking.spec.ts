import { TestBed, ComponentFixture } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { NavigationService } from '../../core/navigation/navigation-service';
import { Ranking } from './ranking';

const BASE = '/api/reports/regulation/ranking';
const FILTERS = {
  success: true,
  data: {
    periods: [
      { value: '2024A', text: '2024 Primer Semestre' },
      { value: '2023B', text: '2023 Segundo Semestre' },
    ],
  },
};
const ROW = {
  studentCode: 'A1', name: 'ANA PÉREZ', decree: 'DEC-1',
  decreeDescription: 'Decreto de titulación', average: '6.2', promotion: '2020',
  entrySemester: '2016-1', ranking: 5, total: 40,
};
const REPORT = {
  group: 'ICMA', period: '2024A', selection: 'PROMOCION', sortBy: 'RANKING',
  direction: 'ASC', rows: [ROW],
};
const HEADERS = [
  'Código', 'Nombre', 'Decreto', 'Promedio', 'Promoción', 'Semestre de Ingreso', 'Lugar',
];

describe('Ranking de Licenciatura y Egreso', () => {
  let http: HttpTestingController;
  let studentProgressUrl: ReturnType<typeof vi.fn>;
  beforeEach(() => {
    studentProgressUrl = vi.fn((code: string) =>
      `https://sad.example.test/alumnos/students/detail/${code}/grade-reports/progress`);
    TestBed.configureTestingModule({
      imports: [Ranking],
      providers: [
        provideHttpClient(), provideHttpClientTesting(),
        { provide: NavigationService, useValue: { studentProgressUrl } },
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => { http.verify(); TestBed.resetTestingModule(); });

  function render(): ComponentFixture<Ranking> {
    const fixture = TestBed.createComponent(Ranking);
    fixture.detectChanges();
    return fixture;
  }

  async function ready(filters: object = FILTERS): Promise<ComponentFixture<Ranking>> {
    const fixture = render();
    http.expectOne(BASE + '/filters').flush(filters);
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture;
  }

  function submit(fixture: ComponentFixture<Ranking>): void {
    fixture.nativeElement.querySelector('form')!.dispatchEvent(new Event('submit'));
    fixture.detectChanges();
  }

  async function query(report: unknown = REPORT): Promise<ComponentFixture<Ranking>> {
    const fixture = await ready();
    submit(fixture);
    http.expectOne(req => req.url === BASE + '/report')
      .flush({ success: true, message: null, data: report });
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture;
  }

  it('carga los períodos una sola vez y no consulta el informe al iniciar', async () => {
    const fixture = render();
    const request = http.expectOne(BASE + '/filters');
    expect(request.request.method).toBe('GET');
    request.flush(FILTERS);
    await fixture.whenStable();
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('h1')?.textContent?.trim())
      .toContain('Ranking de Alumnos por Licenciatura/Egreso');
    expect(root.querySelectorAll('ng-select[data-filter]')).toHaveLength(5);
    expect(fixture.componentInstance.form.getRawValue()).toEqual({
      group: 'ICMA', period: '2024A', selection: 'PROMOCION', sortBy: 'RANKING', direction: 'ASC',
    });
    expect(root.textContent).toContain('Ingeniería Comercial Mención Administración');
    http.expectNone(BASE + '/report');
    expect(root.querySelector('app-table')).toBeNull();
  });

  it('mantiene las opciones legacy de grupo, selección, orden y dirección', async () => {
    const fixture = await ready();
    const root: HTMLElement = fixture.nativeElement;
    expect(root.textContent).toContain('Ranking por Promoción');
    expect(root.textContent).toContain('Ordenar por Ranking');
    expect(root.textContent).toContain('Ascendente');
    const groups: readonly [string, string][] = [
      ['ICMA', 'Ingeniería Comercial Mención Administración'],
      ['ICME', 'Ingeniería Comercial Mención Economía'],
      ['IICG', 'Ingeniería en Información y Control de Gestión'],
      ['CA', 'Contador Auditor'],
      ['LICADM', 'Licenciatura en Administración'],
      ['LICECO', 'Licenciatura en Economía'],
      ['LICINF', 'Licenciatura en Información'],
    ];
    for (const [value, text] of groups) {
      fixture.componentInstance.form.controls.group.setValue(value);
      fixture.detectChanges();
      expect(root.textContent).toContain(text);
    }
    const { controls } = fixture.componentInstance.form;
    controls.selection.setValue('INGRESO');
    fixture.detectChanges();
    expect(root.textContent).toContain('Ranking por Período de Ingreso');
    controls.sortBy.setValue('NOMBRE');
    fixture.detectChanges();
    expect(root.textContent).toContain('Ordenar Por Nombre');
    controls.direction.setValue('DESC');
    fixture.detectChanges();
    expect(root.textContent).toContain('Descendente');
    http.expectNone(BASE + '/report');
  });

  it('envía los cinco parámetros y muestra las siete columnas legacy', async () => {
    const fixture = await ready();
    http.expectNone(BASE + '/report');
    submit(fixture);
    const request = http.expectOne(req => req.url === BASE + '/report');
    expect(request.request.method).toBe('GET');
    expect(request.request.params.keys().sort())
      .toEqual(['direction', 'group', 'period', 'selection', 'sortBy']);
    expect(request.request.params.get('group')).toBe('ICMA');
    expect(request.request.params.get('period')).toBe('2024A');
    expect(request.request.params.get('selection')).toBe('PROMOCION');
    expect(request.request.params.get('sortBy')).toBe('RANKING');
    expect(request.request.params.get('direction')).toBe('ASC');
    expect(fixture.nativeElement.querySelector('app-table table')?.getAttribute('aria-busy'))
      .toBe('true');
    request.flush({ success: true, message: null, data: REPORT });
    await fixture.whenStable();
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;
    expect([...root.querySelectorAll('app-table thead th')]
      .map(header => header.textContent?.trim())).toEqual(HEADERS);
    expect(root.querySelectorAll('app-table tbody tr')).toHaveLength(1);
    expect(root.querySelector('app-table table')?.getAttribute('aria-busy')).toBe('false');
    expect(root.textContent).toContain('A1');
    expect(root.textContent).toContain('ANA PÉREZ');
    expect(root.textContent).toContain('DEC-1');
    expect(root.textContent).toContain('Decreto de titulación');
    expect(root.textContent).toContain('6.2');
    expect(root.textContent).toContain('2020');
    expect(root.textContent).toContain('2016-1');
    expect(root.textContent).toContain('5 de 40');
    expect(root.textContent).toContain('Total: 1');
    expect(root.querySelector('app-search')).not.toBeNull();
    http.expectNone(BASE + '/report');
  });

  it('envía los filtros elegidos por el usuario', async () => {
    const fixture = await ready();
    fixture.componentInstance.form.setValue({
      group: 'LICINF', period: '2023B', selection: 'INGRESO', sortBy: 'NOMBRE', direction: 'DESC',
    });
    submit(fixture);
    const request = http.expectOne(req => req.url === BASE + '/report');
    expect(request.request.params.get('group')).toBe('LICINF');
    expect(request.request.params.get('period')).toBe('2023B');
    expect(request.request.params.get('selection')).toBe('INGRESO');
    expect(request.request.params.get('sortBy')).toBe('NOMBRE');
    expect(request.request.params.get('direction')).toBe('DESC');
    request.flush({ success: true, message: null, data: { ...REPORT, rows: [] } });
    await fixture.whenStable();
    fixture.detectChanges();
  });

  it('no consulta el informe si falta el período', async () => {
    const fixture = await ready({ success: true, data: { periods: [] } });
    submit(fixture);
    http.expectNone(BASE + '/report');
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('[role="alert"]')?.textContent).toContain('Debe seleccionar un período.');
    expect(root.querySelector('app-table')).toBeNull();
  });

  it('enlaza el código con la ficha de avance de SADAlumnos', async () => {
    const fixture = await query();
    const link = fixture.nativeElement.querySelector('app-table tbody td a') as HTMLAnchorElement;
    expect(studentProgressUrl).toHaveBeenCalledTimes(1);
    expect(studentProgressUrl).toHaveBeenCalledWith('A1');
    expect(link.textContent?.trim()).toBe('A1');
    expect(link.getAttribute('href')).toBe(
      'https://sad.example.test/alumnos/students/detail/A1/grade-reports/progress');
  });

  it('muestra el mensaje legacy cuando no hay alumnos', async () => {
    const fixture = await query({ ...REPORT, rows: [] });
    const root: HTMLElement = fixture.nativeElement;
    expect(root.textContent).toContain('No existen alumnos con los requisitos indicados');
    expect(root.textContent).toContain('Total: 0');
    expect(studentProgressUrl).not.toHaveBeenCalled();
  });

  it('sanitiza un informe fallido sin tabla ni enlaces', async () => {
    const fixture = await ready();
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
