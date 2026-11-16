import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { NavigationService } from '../../core/navigation/navigation-service';
import { AutomaticProcessingRequests } from './automatic-processing-requests';

const BASE = '/api/reports/requests/automatic-processing';
const filters = {
  periods: ['20242', '20241'],
  careers: [{ value: '', text: 'TODAS' }, { value: 'IC', text: 'Ingeniería Comercial' }],
  types: [{ value: '', text: 'TODOS' }],
  states: [{ value: 'SIN DATO', text: 'SIN DATO' }, { value: 'ACEPTADA', text: 'ACEPTADA' }],
};
const row = {
  name: 'ANA PÉREZ', studentCode: 'A1', requestCode: '20242/00001', date: '15/03/2024',
  type: 'Diferir', requested: 'Cursar 30 UD', administrativeProcessing: 'APROBADA',
  result: 'Aceptada', processDate: '20/03/2024',
};

describe('Solicitudes procesadas automáticamente', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [AutomaticProcessingRequests],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([]),
        { provide: NavigationService, useValue: { studentRequestsUrl: (code: string) =>
          `https://sad.example.test/alumnos/default.aspx?codigo=${code}&modo=11` } }],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => { http.verify(); TestBed.resetTestingModule(); });

  async function load() {
    const fixture = TestBed.createComponent(AutomaticProcessingRequests);
    fixture.detectChanges();
    http.expectOne(BASE + '/filters').flush({ success: true, data: filters });
    await fixture.whenStable();
    http.expectOne((request) => request.url === BASE + '/report'
      && request.params.get('period') === '20242'
      && request.params.get('career') === ''
      && request.params.get('type') === ''
      && request.params.get('status') === 'SIN DATO')
      .flush({ success: true, data: { period: '20242', career: '', type: '', status: 'SIN DATO', rows: [row] } });
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture;
  }

  it('loads filters and the nine legacy columns', async () => {
    const fixture = await load();
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('h1')?.textContent).toContain('Solicitudes procesadas automáticamente');
    expect(root.querySelectorAll('app-table thead th')).toHaveLength(9);
    expect(root.textContent).toContain('ANA PÉREZ');
    expect(root.textContent).toContain('Total: 1');
    expect(root.querySelector('tbody a')?.getAttribute('href')).toContain('codigo=A1');
    expect(root.querySelector('tbody a')?.getAttribute('href')).toContain('modo=11');
  });

  it('reloads when a filter changes', async () => {
    const fixture = await load();
    const component = fixture.componentInstance as unknown as {
      form: { controls: { career: { setValue: (value: string) => void } } };
    };
    component.form.controls.career.setValue('IC');
    fixture.detectChanges();
    http.expectOne((request) => request.url === BASE + '/report'
      && request.params.get('career') === 'IC')
      .flush({ success: true, data: { period: '20242', career: 'IC', type: '', status: 'SIN DATO', rows: [] } });
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No se encontraron registros');
  });

  it('sanitizes a failed report', async () => {
    const fixture = TestBed.createComponent(AutomaticProcessingRequests);
    fixture.detectChanges();
    http.expectOne(BASE + '/filters').flush({ success: true, data: filters });
    await fixture.whenStable();
    http.expectOne((request) => request.url === BASE + '/report')
      .flush('private', { status: 503, statusText: 'private' });
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No se pudo cargar el informe.');
    expect(fixture.nativeElement.textContent).not.toContain('private');
  });
});
