import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { NavigationService } from '../../core/navigation/navigation-service';
import { UnopenedFiles } from './unopened-files';

const ENDPOINT = '/api/reports/regulation/unopened-files/report';
const ROW = {
  studentCode: 'A1', titleCode: 'IC', personCode: '12345678-9', name: 'ANA PÉREZ',
  degreeTitle: 'Licenciatura en Ciencias', career: 'Ingeniería Comercial',
  situation: 'EGRESADO', promotion: '2020', phone: '+56911111111',
  fenEmail: 'ana@fen.uchile.cl', personalEmail: 'ana@example.test',
};
const REPORT = { rows: [ROW] };

describe('Alumnos que cumplen requisitos y no han abierto expediente', () => {
  let http: HttpTestingController;
  let studentFileOpeningUrl: ReturnType<typeof vi.fn>;
  beforeEach(() => {
    studentFileOpeningUrl = vi.fn((code: string) =>
      `https://sad.example.test/alumnos/students/detail/${code}/academic-status/file-opening`);
    TestBed.configureTestingModule({
      imports: [UnopenedFiles],
      providers: [
        provideHttpClient(), provideHttpClientTesting(),
        { provide: NavigationService, useValue: { studentFileOpeningUrl } },
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => { http.verify(); TestBed.resetTestingModule(); });

  async function render(report: unknown = REPORT) {
    const fixture = TestBed.createComponent(UnopenedFiles);
    fixture.detectChanges();
    const table = fixture.nativeElement.querySelector('app-table table');
    expect(table?.getAttribute('aria-busy')).toBe('true');
    http.expectOne(ENDPOINT).flush({ success: true, message: null, data: report });
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture;
  }

  it('consulta el informe una sola vez con las diez columnas legacy y su aviso', async () => {
    const fixture = await render();
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('h1')?.textContent?.trim())
      .toBe('Alumnos que cumplen requisitos y no han abierto expediente');
    expect([...root.querySelectorAll('app-table thead th')]
      .map(header => header.textContent?.trim())).toEqual([
      'Nombre', 'Foto', 'Título/Grado', 'Rut', 'Carrera', 'Situación Académica',
      'Promoción', 'Teléfono(s)', 'Correo Fen', 'Correo Personal',
    ]);
    expect(root.querySelectorAll('app-table tbody tr')).toHaveLength(1);
    expect(root.querySelector('app-table table')?.getAttribute('aria-busy')).toBe('false');
    expect(root.textContent).toContain('ANA PÉREZ');
    expect(root.textContent).toContain('Licenciatura en Ciencias');
    expect(root.textContent).toContain('12345678-9');
    expect(root.textContent).toContain('Ingeniería Comercial');
    expect(root.textContent).toContain('EGRESADO');
    expect(root.textContent).toContain('2020');
    expect(root.textContent).toContain('+56911111111');
    expect(root.textContent).toContain('ana@fen.uchile.cl');
    expect(root.textContent).toContain('ana@example.test');
    expect(root.textContent.trim()).toContain('Total: 1');
    const cells = root.querySelectorAll('app-table tbody td') as NodeListOf<HTMLElement>;
    expect(cells[3].textContent?.trim()).toBe('12345678-9');
    const notice = root.querySelector('[data-notice="university-debts"]');
    expect(notice?.textContent).toContain('no valida deudas a nivel de universidad');
    expect(root.querySelector('form')).toBeNull();
  });

  it('muestra la foto del alumno con su código de persona', async () => {
    const fixture = await render();
    expect(fixture.nativeElement.querySelector('app-table tbody img')?.getAttribute('src'))
      .toBe('https://fotos.fen.uchile.cl/12345678-9.jpg');
  });

  it('usa la foto por defecto cuando falta el código de persona', async () => {
    const fixture = await render({ rows: [{ ...ROW, personCode: '' }] });
    expect(fixture.nativeElement.querySelector('app-table tbody img')?.getAttribute('src'))
      .toBe('https://fotos.fen.uchile.cl/perfilNN.jpg');
  });

  it('enlaza el nombre con la apertura de expediente de la ficha nueva de SADAlumnos', async () => {
    const fixture = await render();
    const link = fixture.nativeElement.querySelector('app-table tbody td a') as HTMLAnchorElement;
    expect(studentFileOpeningUrl).toHaveBeenCalledTimes(1);
    expect(studentFileOpeningUrl).toHaveBeenCalledWith('A1');
    expect(link.textContent?.trim()).toBe('ANA PÉREZ');
    expect(link.getAttribute('href')).toBe(
      'https://sad.example.test/alumnos/students/detail/A1/academic-status/file-opening');
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

  it('muestra el estado vacío con el total en cero', async () => {
    const fixture = await render({ rows: [] });
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('app-table')).not.toBeNull();
    expect(root.textContent).toContain('No se encontraron registros');
    expect(root.textContent).toContain('Total: 0');
  });

  it('sanitiza un informe fallido sin tabla ni enlaces', async () => {
    const fixture = TestBed.createComponent(UnopenedFiles);
    fixture.detectChanges();
    http.expectOne(ENDPOINT).flush('private', { status: 503, statusText: 'private' });
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
