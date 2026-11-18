import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { UnresolvedPermanence } from './unresolved-permanence';

const BASE = '/api/reports/requests/unresolved-permanence';
const filters = { careers: [{ value: 'IC', text: 'Ingeniería Comercial' }] };
const row = {
  personCode: 'P1', studentCode: 'A1', name: 'ANA PÉREZ', situation: 'REGULAR',
  entryData: 'Tipo: NUEVO', credit: '50 (20242)', performance: 'UDA: 30', semesters: '2',
  failedCore: 'CORE1', enrolledUnits: '30',
  previousRequests: [{ period: '20241', type: 'Permanencia', resolution: 'Aprobada', authority: 'Autoridad', approved: true }],
  currentRequests: [{ period: '20242', type: 'Permanencia' }], eliminationCauses: [{ text: 'Bajo UD', severity: 'danger' }],
};

describe('Permanencia no resuelta', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [UnresolvedPermanence],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => { http.verify(); TestBed.resetTestingModule(); });

  async function load() {
    const fixture = TestBed.createComponent(UnresolvedPermanence);
    fixture.detectChanges();
    http.expectOne(BASE + '/filters').flush({ success: true, data: filters });
    await fixture.whenStable();
    http.expectOne((request) => request.url === BASE + '/report'
      && request.params.get('career') === 'IC'
      && request.params.get('period') === null)
      .flush({ success: true, data: { period: '20242', career: 'IC', rows: [row] } });
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture;
  }

  it('loads the server-resolved period, legacy columns and avatar', async () => {
    const fixture = await load();
    const root: HTMLElement = fixture.nativeElement;
    expect(root.textContent).toContain('Alumnos con Solicitudes de Permanencia No Resueltas');
    expect(root.textContent).toContain('20242');
    expect(root.querySelectorAll('app-table thead th')).toHaveLength(12);
    expect(root.textContent).toContain('ANA PÉREZ');
    expect(root.textContent).toContain('REGULAR');
    expect(root.querySelector('tbody a')).toBeNull();
    expect(root.querySelector('tbody img')?.getAttribute('src'))
      .toBe('https://fotos.fen.uchile.cl/P1.jpg');
    expect(root.querySelector('.table-icon-success')).not.toBeNull();
    expect(root.querySelector('.table-icon-danger')).not.toBeNull();
    expect(root.querySelector('[data-filter="period"]')).toBeNull();
  });

  it('sanitizes a failed report', async () => {
    const fixture = TestBed.createComponent(UnresolvedPermanence);
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
