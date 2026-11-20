import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture } from '@angular/core/testing';
import { NavigationService } from '../../core/navigation/navigation-service';
import { OpenedFilesByPeriod } from './opened-files-by-period';

const ENDPOINT = '/api/reports/regulation/opened-files-by-period/report';
const ROW = {
  studentCode: 'A1', titleCode: 'IC', personCode: '12345678-9', name: 'ANA PÉREZ',
  degreeTitle: 'Licenciatura en Ciencias', career: 'Ingeniería Comercial',
  situation: 'EGRESADO', promotion: '2020', openingDate: '2024-03-05',
  phone: '+56911111111', fenEmail: 'ana@fen.uchile.cl', personalEmail: 'ana@example.test',
};
const REPORT = { from: '2024-01-01', to: '2024-06-30', rows: [ROW] };

describe('Expedientes Abiertos en un Período Dado', () => {
  let http: HttpTestingController;
  let studentFileOpeningUrl: ReturnType<typeof vi.fn>;
  beforeEach(() => {
    studentFileOpeningUrl = vi.fn((code: string) =>
      `https://sad.example.test/alumnos/students/detail/${code}/academic-status/file-opening`);
    TestBed.configureTestingModule({
      imports: [OpenedFilesByPeriod],
      providers: [
        provideHttpClient(), provideHttpClientTesting(),
        { provide: NavigationService, useValue: { studentFileOpeningUrl } },
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => { http.verify(); TestBed.resetTestingModule(); });

  function render(): ComponentFixture<OpenedFilesByPeriod> {
    const fixture = TestBed.createComponent(OpenedFilesByPeriod);
    fixture.detectChanges();
    return fixture;
  }

  function fill(fixture: ComponentFixture<OpenedFilesByPeriod>, from: string, to: string): void {
    const root: HTMLElement = fixture.nativeElement;
    for (const [id, value] of [['from', from], ['to', to]] as const) {
      const input = root.querySelector<HTMLInputElement>(`input[formcontrolname="${id}"]`)!;
      input.value = value;
      input.dispatchEvent(new Event('input'));
    }
  }

  function submit(fixture: ComponentFixture<OpenedFilesByPeriod>): void {
    fixture.nativeElement.querySelector('form')!
      .dispatchEvent(new Event('submit'));
    fixture.detectChanges();
  }

  async function query(report: unknown = REPORT): Promise<ComponentFixture<OpenedFilesByPeriod>> {
    const fixture = render();
    fill(fixture, '2024-01-01', '2024-06-30');
    submit(fixture);
    http.expectOne(req => req.url === ENDPOINT)
      .flush({ success: true, message: null, data: report });
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture;
  }

  it('no consulta el informe al iniciar', () => {
    const fixture = render();
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('h1')?.textContent?.trim()).toContain('Expedientes Abiertos');
    expect(root.querySelectorAll('input[type="date"]')).toHaveLength(2);
    expect(root.querySelector('form')).not.toBeNull();
    http.expectNone(ENDPOINT);
    expect(root.querySelector('app-table')).toBeNull();
  });

  it('envía solo el rango de fechas al endpoint y muestra las once columnas legacy', async () => {
    const fixture = render();
    fill(fixture, '2024-01-01', '2024-06-30');
    submit(fixture);
    const request = http.expectOne(req => req.url === ENDPOINT);
    expect(request.request.method).toBe('GET');
    expect(request.request.params.keys().sort()).toEqual(['from', 'to']);
    expect(request.request.params.get('from')).toBe('2024-01-01');
    expect(request.request.params.get('to')).toBe('2024-06-30');
    expect(fixture.nativeElement.querySelector('app-table table')?.getAttribute('aria-busy'))
      .toBe('true');
    request.flush({ success: true, message: null, data: REPORT });
    await fixture.whenStable();
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;
    expect([...root.querySelectorAll('app-table thead th')]
      .map(header => header.textContent?.trim())).toEqual([
      'Nombre', 'Foto', 'Título/Grado', 'Rut', 'Carrera', 'Situación Académica',
      'Promoción', 'Fecha Apertura', 'Teléfono(s)', 'Correo Fen', 'Correo Personal',
    ]);
    expect(root.querySelectorAll('app-table tbody tr')).toHaveLength(1);
    expect(root.querySelector('app-table table')?.getAttribute('aria-busy')).toBe('false');
    expect(root.textContent).toContain('ANA PÉREZ');
    expect(root.textContent).toContain('Licenciatura en Ciencias');
    expect(root.textContent).toContain('12345678-9');
    expect(root.textContent).toContain('Ingeniería Comercial');
    expect(root.textContent).toContain('EGRESADO');
    expect(root.textContent).toContain('2020');
    expect(root.textContent).toContain('2024-03-05');
    expect(root.textContent).toContain('+56911111111');
    expect(root.textContent).toContain('ana@fen.uchile.cl');
    expect(root.textContent).toContain('ana@example.test');
    expect(root.textContent).toContain('Total: 1');
    expect(root.querySelector('app-search')).not.toBeNull();
    http.expectNone(ENDPOINT);
  });

  it('no consulta y avisa cuando falta una de las fechas', () => {
    const fixture = render();
    fill(fixture, '2024-01-01', '');
    submit(fixture);
    http.expectNone(ENDPOINT);
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('[role="alert"]')?.textContent)
      .toContain('Debe indicar la fecha desde y la fecha hasta.');
    expect(root.querySelector('app-table')).toBeNull();
  });

  it('no consulta y avisa cuando el rango está invertido', () => {
    const fixture = render();
    fill(fixture, '2024-06-30', '2024-01-01');
    submit(fixture);
    http.expectNone(ENDPOINT);
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('[role="alert"]')?.textContent)
      .toContain('La fecha desde no puede ser posterior a la fecha hasta.');
    expect(root.querySelector('app-table')).toBeNull();
  });

  it('enlaza el nombre con la apertura de expediente de la ficha nueva de SADAlumnos', async () => {
    const fixture = await query();
    const link = fixture.nativeElement.querySelector('app-table tbody td a') as HTMLAnchorElement;
    expect(studentFileOpeningUrl).toHaveBeenCalledTimes(1);
    expect(studentFileOpeningUrl).toHaveBeenCalledWith('A1');
    expect(link.textContent?.trim()).toBe('ANA PÉREZ');
    expect(link.getAttribute('href')).toBe(
      'https://sad.example.test/alumnos/students/detail/A1/academic-status/file-opening');
  });

  it('muestra la foto del alumno con su código de persona', async () => {
    const fixture = await query();
    expect(fixture.nativeElement.querySelector('app-table tbody img')?.getAttribute('src'))
      .toBe('https://fotos.fen.uchile.cl/12345678-9.jpg');
  });

  it('usa la foto por defecto cuando falta el código de persona', async () => {
    const fixture = await query({ ...REPORT, rows: [{ ...ROW, personCode: '' }] });
    expect(fixture.nativeElement.querySelector('app-table tbody img')?.getAttribute('src'))
      .toBe('https://fotos.fen.uchile.cl/perfilNN.jpg');
  });

  it('filtra en el buscador sin volver a consultar el endpoint', async () => {
    const fixture = await query();
    const input = fixture.nativeElement.querySelector('app-search input') as HTMLInputElement;
    input.value = 'ZZZ';
    input.dispatchEvent(new Event('input'));
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No se encontraron registros');
    http.expectNone(ENDPOINT);
  });

  it('muestra el estado vacío con el total en cero', async () => {
    const fixture = await query({ ...REPORT, rows: [] });
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('app-table')).not.toBeNull();
    expect(root.textContent).toContain('No se encontraron registros');
    expect(root.textContent).toContain('Total: 0');
  });

  it('sanitiza un informe fallido sin tabla ni enlaces', async () => {
    const fixture = render();
    fill(fixture, '2024-01-01', '2024-06-30');
    submit(fixture);
    http.expectOne(req => req.url === ENDPOINT).flush('private', { status: 503, statusText: 'private' });
    await fixture.whenStable();
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;
    expect(root.textContent).toContain('No se pudo cargar el informe.');
    expect(root.textContent).not.toContain('private');
    expect(root.querySelector('app-table')).toBeNull();
    expect(root.textContent).not.toContain('Total:');
    expect(studentFileOpeningUrl).not.toHaveBeenCalled();
  });
});
