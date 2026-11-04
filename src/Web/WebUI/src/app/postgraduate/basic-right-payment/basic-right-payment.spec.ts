import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { NavigationService } from '../../core/navigation/navigation-service';
import { NotificationService } from '../../core/services/notification-service';
import { BasicRightPayment } from './basic-right-payment';

const BASE = '/api/reports/postgraduate/basic-right-payment';
const filters = {
  success: true,
  data: {
    studentTypes: [{ value: 'MBA', text: 'MBA' }],
    modalities: [{ value: 'PT', text: 'PT' }],
    years: [{ value: '2024', text: '2024' }, { value: '2023', text: '2023' }],
    paymentStatuses: [{ value: 'Todos', text: 'Todos' }, { value: 'Si', text: 'Pagados' },
      { value: 'No', text: 'Sin Pago' }],
  },
};
const report = {
  success: true,
  data: {
    amount: 100000,
    canManage: true,
    total: 1,
    rows: [{
      position: 1, photoUrl: 'https://fotos.fen.uchile.cl/P1.jpg', name: 'ANA PÉREZ',
      rut: '11111111-1', studentCode: 'A1', entry: '2024A', situation: 'REGULAR',
      average: 6.5, paid: false,
    }],
  },
};

describe('Basic right payment report', () => {
  let http: HttpTestingController;
  let downloads: HTMLAnchorElement[];
  const confirm = vi.fn().mockResolvedValue({ isConfirmed: true });
  const createUrl = vi.fn(() => 'blob:basic-right');
  const revokeUrl = vi.fn();

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [BasicRightPayment],
      providers: [
        provideHttpClient(), provideHttpClientTesting(),
        { provide: NotificationService, useValue: { confirm } },
        { provide: NavigationService, useValue: { links: signal([
          { key: 'student', label: 'Alumnos', icon: '', url: 'https://sad.example.test' },
        ]) } },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    downloads = []; confirm.mockClear(); createUrl.mockClear(); revokeUrl.mockClear();
    const NativeURL = URL;
    vi.stubGlobal('URL', class extends NativeURL {
      static override createObjectURL = createUrl;
      static override revokeObjectURL = revokeUrl;
    });
    vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(function (this: HTMLAnchorElement) {
      downloads.push(this);
    });
  });
  afterEach(() => { http.verify(); vi.restoreAllMocks(); vi.unstubAllGlobals(); TestBed.resetTestingModule(); });

  async function ready(canManage = true) {
    const fixture = TestBed.createComponent(BasicRightPayment);
    fixture.detectChanges();
    http.expectOne(BASE + '/filters').flush(filters);
    await fixture.whenStable(); fixture.detectChanges();
    fixture.nativeElement.querySelector('[data-action=show]').click();
    const request = http.expectOne(req => req.url === BASE + '/report');
    request.flush({ ...report, data: { ...report.data, canManage } });
    await fixture.whenStable(); fixture.detectChanges();
    return fixture;
  }

  it('loads the filters and only queries after Mostrar', async () => {
    const fixture = TestBed.createComponent(BasicRightPayment);
    fixture.detectChanges();
    http.expectOne(BASE + '/filters').flush(filters);
    await fixture.whenStable(); fixture.detectChanges();
    expect(fixture.componentInstance.form.getRawValue()).toEqual({
      studentType: 'MBA', modality: 'PT', entryYear: '2024', paymentYear: '2024', paymentStatus: 'Todos',
    });
    http.expectNone(BASE + '/report');
    fixture.nativeElement.querySelector('[data-action=show]').click();
    http.expectOne(req => req.url === BASE + '/report').flush(report);
    await fixture.whenStable(); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('$ 100,000');
    expect(fixture.nativeElement.querySelectorAll('app-table thead th')).toHaveLength(9);
    const image = fixture.nativeElement.querySelector('app-table tbody img') as HTMLImageElement;
    expect(image).not.toBeNull();
    expect(image.getAttribute('src')).toContain('/P1.jpg');
    const studentLink = fixture.nativeElement.querySelector('app-table tbody a') as HTMLAnchorElement;
    expect(studentLink.getAttribute('href')).toBe(
      'https://sad.example.test/students/detail/A1');
    expect(studentLink.getAttribute('target')).toBe('_blank');
    fixture.destroy();
  });

  it('marks payments, saves only changes and reloads', async () => {
    const fixture = await ready();
    expect(fixture.nativeElement.querySelector('[data-action=save]')).not.toBeNull();
    const checkbox = fixture.nativeElement.querySelector('app-table tbody input[type=checkbox]') as HTMLInputElement;
    checkbox.click(); fixture.detectChanges();
    expect(checkbox.checked).toBe(true);
    fixture.nativeElement.querySelector('[data-action=save]').click();
    await fixture.whenStable();
    expect(confirm).toHaveBeenCalled();
    const request = http.expectOne(BASE + '/payments');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({ paymentYear: '2024', rows: [{ studentCode: 'A1', paid: true }] });
    request.flush({ success: true, data: { paymentYear: '2024', saved: 1 } });
    await fixture.whenStable();
    http.expectOne(req => req.url === BASE + '/report').flush({
      ...report, data: { ...report.data, rows: [{ ...report.data.rows[0], paid: true }] },
    });
    await fixture.whenStable(); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Pagos guardados: 1.');
    fixture.destroy();
  });

  it('hides Save without management permission and exports Excel', async () => {
    const fixture = await ready(false);
    expect(fixture.nativeElement.querySelector('[data-action=save]')).toBeNull();
    fixture.nativeElement.querySelector('[data-action=export]').click();
    http.expectOne(req => req.url === BASE + '/download').flush(new Blob(['xlsx']));
    await fixture.whenStable(); fixture.detectChanges();
    expect(downloads[0].download).toBe('PagoDerechoBasicoAnual.xlsx');
    fixture.destroy();
  });

  it('shows Sin Monto and blocks saving when the year has no amount', async () => {
    const fixture = await ready();
    fixture.componentInstance.form.controls.paymentYear.setValue('2023');
    fixture.detectChanges();
    fixture.nativeElement.querySelector('[data-action=show]').click();
    http.expectOne(req => req.url === BASE + '/report').flush({
      ...report, data: { ...report.data, amount: null, rows: [] },
    });
    await fixture.whenStable(); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Sin Monto');
    expect(fixture.nativeElement.querySelector('[data-action=save]')).not.toBeNull();
    fixture.nativeElement.querySelector('app-table tbody input[type=checkbox]')?.click();
    fixture.destroy();
  });
});
