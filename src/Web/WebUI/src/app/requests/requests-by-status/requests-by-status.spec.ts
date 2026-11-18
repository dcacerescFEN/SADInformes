import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { NavigationService } from '../../core/navigation/navigation-service';
import { RequestsByStatus } from './requests-by-status';

const BASE = '/api/reports/requests/by-status';
const row = {
  personCode: 'P1', studentCode: 'A1', name: 'ANA PÉREZ', requestCode: '20242/00001',
  date: '15/03/2024', type: 'Reconocimiento de Cátedras Aprobadas en Postgrado',
  situation: 'REGULAR', comment: 'Comentario', firstRequest: '20242/00001', lastRequest: '20242/00001',
};

describe('Solicitudes no impresas', () => {
  let http: HttpTestingController;
  const routeData = { requestType: '67' };
  beforeEach(() => {
    routeData.requestType = '67';
    TestBed.configureTestingModule({
      imports: [RequestsByStatus],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([]),
        { provide: ActivatedRoute, useValue: { snapshot: { data: routeData } } },
        { provide: NavigationService, useValue: { studentRequestsUrl: (code: string) =>
          `https://sad.example.test/alumnos/students/detail/${code}/registration/requests` } }],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => { http.verify(); TestBed.resetTestingModule(); });

  async function load() {
    const fixture = TestBed.createComponent(RequestsByStatus);
    fixture.detectChanges();
    http.expectOne((request) => request.url === BASE + '/report'
      && request.params.get('career') === 'IC'
      && request.params.get('status') === 'NO IMPRESA'
      && request.params.get('type') === '67')
      .flush({ success: true, data: { career: 'IC', status: 'NO IMPRESA', type: '67', rows: [row] } });
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture;
  }

  it('loads the fixed type 67 report and the student requests link', async () => {
    const fixture = await load();
    const root: HTMLElement = fixture.nativeElement;
    expect(root.textContent).toContain('Reconocimiento de Cátedras Aprobadas en Postgrado');
    expect(root.querySelectorAll('app-table thead th')).toHaveLength(7);
    expect(root.querySelector('[data-filter="type"]')).toBeNull();
    expect(root.querySelector('[data-filter="status"]')).toBeNull();
    expect(root.textContent).toContain('ANA PÉREZ');
    expect(root.querySelector('tbody a')?.getAttribute('href'))
      .toBe('https://sad.example.test/alumnos/students/detail/A1/registration/requests');
    expect(root.querySelector('tbody img')?.getAttribute('src'))
      .toBe('https://fotos.fen.uchile.cl/P1.jpg');
  });

  it('uses type 65 for the seminar homologation route', async () => {
    routeData.requestType = '65';
    const fixture = TestBed.createComponent(RequestsByStatus);
    fixture.detectChanges();
    http.expectOne((request) => request.url === BASE + '/report'
      && request.params.get('type') === '65')
      .flush({ success: true, data: { career: 'IC', status: 'NO IMPRESA', type: '65', rows: [] } });
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Homologación de Seminario X Electivos Magister');
  });

  it('uses pending status and shows the type filter for the pending route', async () => {
    routeData.requestType = '%';
    const fixture = TestBed.createComponent(RequestsByStatus);
    fixture.detectChanges();
    http.expectOne((request) => request.url === BASE + '/types'
      && request.params.get('status') === 'PENDIENTE'
      && request.params.get('typeFilter') === '%')
      .flush({ success: true, data: { types: [
        { value: '%', text: 'TODOS' }, { value: '1', text: 'Tipo uno' },
      ] } });
    await fixture.whenStable();
    http.expectOne((request) => request.url === BASE + '/report'
      && request.params.get('status') === 'PENDIENTE'
      && request.params.get('type') === '%')
      .flush({ success: true, data: { career: 'IC', status: 'PENDIENTE', type: '%', rows: [] } });
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Solicitudes pendientes');
  });

  it('uses OTROS and shows the type filter for the other types route', async () => {
    routeData.requestType = 'OTROS';
    const fixture = TestBed.createComponent(RequestsByStatus);
    fixture.detectChanges();
    http.expectOne((request) => request.url === BASE + '/types'
      && request.params.get('career') === 'IC'
      && request.params.get('status') === 'NO IMPRESA'
      && request.params.get('typeFilter') === 'OTROS')
      .flush({ success: true, data: { types: [
        { value: 'OTROS', text: 'TODOS' }, { value: '1', text: 'Tipo uno' },
      ] } });
    await fixture.whenStable();
    http.expectOne((request) => request.url === BASE + '/report'
      && request.params.get('type') === 'OTROS')
      .flush({ success: true, data: { career: 'IC', status: 'NO IMPRESA', type: 'OTROS', rows: [] } });
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Otros Tipos');
  });

  it('reloads when the career changes', async () => {
    const fixture = await load();
    const component = fixture.componentInstance as unknown as {
      form: { controls: { career: { setValue: (value: string) => void } } };
    };
    component.form.controls.career.setValue('IICG');
    fixture.detectChanges();
    http.expectOne((request) => request.url === BASE + '/report'
      && request.params.get('career') === 'IICG'
      && request.params.get('status') === 'NO IMPRESA'
      && request.params.get('type') === '67')
      .flush({ success: true, data: { career: 'IICG', status: 'NO IMPRESA', type: '67', rows: [] } });
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No se encontraron registros');
  });
});
