import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, Router, provideRouter } from '@angular/router';
import { NavigationService } from '../../core/navigation/navigation-service';
import { AuthService } from '../../core/auth/auth-service';
import { NotificationService } from '../../core/services/notification-service';
import { RequestsByStatus } from './requests-by-status';

const BASE = '/api/reports/requests/by-status';
const row = {
  personCode: 'P1', studentCode: 'A1', name: 'ANA PÉREZ', requestCode: '20242/00001', rawRequestCode: '2024200001',
  date: '15/03/2024', type: 'Reconocimiento de Cátedras Aprobadas en Postgrado',
  situation: 'REGULAR', comment: 'Comentario', firstRequest: '20242/00001', lastRequest: '20242/00001',
};

describe('Solicitudes no impresas', () => {
  let http: HttpTestingController;
  const confirm = vi.fn().mockResolvedValue({ isConfirmed: true });
  const showSuccess = vi.fn();
  const routeData = { requestType: '67' };
  beforeEach(() => {
    routeData.requestType = '67';
    TestBed.configureTestingModule({
      imports: [RequestsByStatus],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([]),
        { provide: ActivatedRoute, useValue: { snapshot: { data: routeData } } },
        { provide: NavigationService, useValue: { studentRequestsUrl: (code: string) =>
          `https://sad.example.test/alumnos/students/detail/${code}/registration/requests` } },
        { provide: AuthService, useValue: { hasPermission: (permission: string) =>
          permission === 'REPORT_PRINT_REQUESTS' || permission === 'REPORT_EDIT_REQUEST_VALIDATION' } },
        { provide: NotificationService, useValue: { confirm, showSuccess } }],
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
    expect(root.querySelectorAll('app-table thead th')).toHaveLength(8);
    expect(root.querySelector('[data-filter="type"]')).toBeNull();
    expect(root.querySelector('[data-filter="status"]')).toBeNull();
    expect(root.querySelector('[data-action="confirm-print"]')).not.toBeNull();
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

  it('navigates to print detail with the current filters', async () => {
    const fixture = await load();
    const router = TestBed.inject(Router);
    const navigate = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    const button = fixture.nativeElement.querySelector('[data-action="print-detail"]') as HTMLButtonElement;
    expect(button).not.toBeNull();
    expect(button.classList.contains('btn-primary')).toBe(true);
    expect(button.classList.contains('btn-create')).toBe(false);
    button.click();
    expect(navigate).toHaveBeenCalledWith(['/requests/print-detail'], {
      queryParams: { career: 'IC', status: 'NO IMPRESA', type: '67' },
    });
  });

  it('confirms print with the visible raw request codes', async () => {
    const fixture = await load();
    (fixture.nativeElement.querySelector('[data-action="confirm-print"]') as HTMLButtonElement).click();
    expect(confirm).toHaveBeenCalled();
    await fixture.whenStable();
    const request = http.expectOne((item) => item.url === BASE + '/confirm-print' && item.method === 'POST');
    expect(request.request.body).toEqual({ requestCodes: ['2024200001'] });
    request.flush({ success: true, data: 1 });
    await fixture.whenStable();
    expect(showSuccess).toHaveBeenCalledWith('Impresión confirmada', 'Se marcaron 1 solicitudes como impresas.');
    http.expectOne((item) => item.url === BASE + '/report').flush({
      success: true, data: { career: 'IC', status: 'NO IMPRESA', type: '67', rows: [] },
    });
    await fixture.whenStable();
  });

  it('edits and saves the validation comment inline', async () => {
    const fixture = await load();
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('[title="Guardar comentario"]')).toBeNull();
    expect(root.querySelector('[title="Cancelar edición"]')).toBeNull();
    (root.querySelector('[title="Editar comentario"]') as HTMLButtonElement).click();
    fixture.detectChanges();
    expect((fixture.componentInstance as unknown as { editingRequest: () => string | null }).editingRequest()).toBe('2024200001');
    const textarea = root.querySelector('tbody textarea') as HTMLTextAreaElement;
    expect(textarea.value).toBe('Comentario');
    expect(root.querySelector('[title="Guardar comentario"]')?.classList.contains('btn-outline-success')).toBe(true);
    expect(root.querySelector('[title="Cancelar edición"]')).not.toBeNull();
    expect(root.querySelector('[title="Editar comentario"]')).toBeNull();
    textarea.value = 'Nuevo comentario';
    textarea.dispatchEvent(new Event('input'));
    (root.querySelector('[title="Guardar comentario"]') as HTMLButtonElement).click();
    const request = http.expectOne((item) => item.url === BASE + '/comment' && item.method === 'PUT');
    expect(request.request.body).toEqual({ requestCode: '2024200001', comment: 'Nuevo comentario' });
    request.flush(null, { status: 204, statusText: 'No Content' });
    await fixture.whenStable();
    fixture.detectChanges();
    expect(showSuccess).toHaveBeenCalledWith('Comentario guardado', 'El comentario fue actualizado.');
    expect(root.querySelector('tbody textarea')).toBeNull();
    expect(root.textContent).toContain('Nuevo comentario');
  });

  it('cancels comment editing without sending a request', async () => {
    const fixture = await load();
    const root: HTMLElement = fixture.nativeElement;
    (root.querySelector('[title="Editar comentario"]') as HTMLButtonElement).click();
    fixture.detectChanges();
    const textarea = root.querySelector('tbody textarea') as HTMLTextAreaElement;
    textarea.value = 'Descartado';
    textarea.dispatchEvent(new Event('input'));
    (root.querySelector('[title="Cancelar edición"]') as HTMLButtonElement).click();
    fixture.detectChanges();
    expect(root.querySelector('tbody textarea')).toBeNull();
    expect(root.textContent).toContain('Comentario');
    expect(root.textContent).not.toContain('Descartado');
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
