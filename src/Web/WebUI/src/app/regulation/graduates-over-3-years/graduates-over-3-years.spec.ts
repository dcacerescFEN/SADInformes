import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { NavigationService } from '../../core/navigation/navigation-service';
import { GraduatesOver3Years } from './graduates-over-3-years';

const ENDPOINT = '/api/reports/regulation/graduates-over-3-years/report';
const REPORT = {
  period: '20242',
  rows: [{
    studentCode: 'A/1', name: 'ANA PÉREZ', studentType: 'IC', decree: '009615/2003',
    decreeDescription: 'Reglamento de estudios', mention: 'Menciones',
    graduatedFrom: 'Ingeniería Comercial', situation: 'EGRESADO',
    entrySemester: '2015-1', promotion: '2020',
  }],
};

describe('Egresados más de 3 años', () => {
  let http: HttpTestingController;
  let studentGradesUrl: ReturnType<typeof vi.fn>;
  beforeEach(() => {
    studentGradesUrl = vi.fn((code: string) =>
      `https://sad.example.test/alumnos/students/detail/${code}/grade-reports/grades`);
    TestBed.configureTestingModule({
      imports: [GraduatesOver3Years],
      providers: [
        provideHttpClient(), provideHttpClientTesting(),
        { provide: NavigationService, useValue: { studentGradesUrl } },
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => { http.verify(); TestBed.resetTestingModule(); });

  async function render(report: unknown = REPORT) {
    const fixture = TestBed.createComponent(GraduatesOver3Years);
    fixture.detectChanges();
    const table = fixture.nativeElement.querySelector('app-table table');
    expect(table?.getAttribute('aria-busy')).toBe('true');
    http.expectOne(ENDPOINT).flush({ success: true, message: null, data: report });
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture;
  }

  it('consulta el informe una sola vez con las ocho columnas legacy', async () => {
    const fixture = await render();
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('h1')?.textContent?.trim())
      .toBe('Detección de Alumnos Egresados Más de 3 Años');
    expect([...root.querySelectorAll('app-table thead th')]
      .map(header => header.textContent?.trim())).toEqual([
      'Código', 'Nombre', 'Carrera', 'Decreto', 'Situación', 'Egresado de',
      'Semestre de Ingreso', 'Promoción',
    ]);
    expect(root.querySelectorAll('app-table tbody tr')).toHaveLength(1);
    expect(root.querySelector('app-table table')?.getAttribute('aria-busy')).toBe('false');
    expect(root.textContent).toContain('ANA PÉREZ');
    expect(root.textContent).toContain('IC');
    expect(root.textContent).toContain('009615/2003');
    expect(root.textContent).toContain('Reglamento de estudios');
    expect(root.textContent).toContain('EGRESADO');
    expect(root.textContent).toContain('Ingeniería Comercial');
    expect(root.textContent).toContain('2015-1');
    expect(root.textContent).toContain('2020');
    expect(root.textContent).toContain('20242');
    expect(root.textContent.trim()).toContain('Total: 1');
    expect(root.querySelector('form')).toBeNull();
  });

  it('enlaza el código con las notas de la ficha nueva de SADAlumnos', async () => {
    const fixture = await render();
    const link = fixture.nativeElement.querySelector('app-table tbody td a') as HTMLAnchorElement;
    expect(studentGradesUrl).toHaveBeenCalledTimes(1);
    expect(studentGradesUrl).toHaveBeenCalledWith('A/1');
    expect(link.textContent?.trim()).toBe('A/1');
    expect(link.getAttribute('href'))
      .toBe('https://sad.example.test/alumnos/students/detail/A/1/grade-reports/grades');
  });

  it('filtra en el buscador sin volver a consultar el endpoint', async () => {
    const fixture = await render();
    const input = fixture.nativeElement.querySelector('app-search input') as HTMLInputElement;
    input.value = 'ZZZ';
    input.dispatchEvent(new Event('input'));
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No se encontraron registros');
  });

  it('muestra la descripción del decreto cuando no hay código de decreto', async () => {
    const fixture = await render({ period: '20242', rows: [
      { ...REPORT.rows[0], decree: '  ', decreeDescription: 'Decreto Universitario' },
    ] });
    const cells = fixture.nativeElement.querySelectorAll('app-table tbody td') as NodeListOf<HTMLElement>;
    expect(cells[3].textContent?.trim()).toBe('Decreto Universitario');
  });

  it('muestra el estado vacío con el total en cero', async () => {    const fixture = await render({ period: '20242', rows: [] });
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('app-table')).not.toBeNull();
    expect(root.textContent).toContain('No se encontraron registros');
    expect(root.textContent).toContain('Total: 0');
  });

  it('sanitiza un informe fallido sin tabla ni enlaces', async () => {
    const fixture = TestBed.createComponent(GraduatesOver3Years);
    fixture.detectChanges();
    http.expectOne(ENDPOINT).flush('private', { status: 503, statusText: 'private' });
    await fixture.whenStable();
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;
    expect(root.textContent).toContain('No se pudo cargar el informe.');
    expect(root.textContent).not.toContain('private');
    expect(root.querySelector('app-table')).toBeNull();
    expect(root.textContent).not.toContain('Total:');
    expect(studentGradesUrl).not.toHaveBeenCalled();
  });
});
