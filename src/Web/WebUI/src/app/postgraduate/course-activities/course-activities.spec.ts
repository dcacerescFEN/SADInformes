import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { CourseActivities } from './course-activities';

const BASE = '/api/reports/postgraduate/course-activities';
const filters = { success: true, data: { years: [
  { value: '2024', text: '2024' }, { value: '2023', text: '2023' },
] } };
const report = {
  success: true,
  data: {
    year: '2024',
    total: 1,
    rows: [{
      id: '1', type: 'Clase', course: 'Álgebra', from: '01-01-2024 10:00:00',
      to: '01-01-2024 12:00:00', period: '2024A', courseCode: 'ENECO775', section: '1',
      program: 'MBA', modality: 'PT', promotion: '2024', room: 'Sala 1', roomCapacity: '30',
      enrolledStudents: '25', roomType: 'Sala', hybridRoom: 'Sí', entryDate: '01-01-2024 09:00:00',
      entryUser: 'usuario', suspended: 'Falso', suspensionDate: '02-01-2024 09:00:00',
      suspensionUser: 'usuario2', observations: 'Advertencia x Capacidad Sala',
    }],
  },
};

describe('Course activities report', () => {
  let http: HttpTestingController;
  let downloads: HTMLAnchorElement[];
  const createUrl = vi.fn(() => 'blob:course-activities');
  const revokeUrl = vi.fn();

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [CourseActivities],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
    downloads = []; createUrl.mockClear(); revokeUrl.mockClear();
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

  async function ready() {
    const fixture = TestBed.createComponent(CourseActivities);
    fixture.detectChanges();
    http.expectOne(BASE + '/filters').flush(filters);
    await fixture.whenStable();
    http.expectOne(req => req.url === BASE + '/report').flush(report);
    await fixture.whenStable(); fixture.detectChanges();
    return fixture;
  }

  it('loads the default year and the 22-column shared table', async () => {
    const fixture = await ready();
    expect(fixture.componentInstance.form.controls.year.value).toBe('2024');
    expect(fixture.nativeElement.querySelector('app-table table')).not.toBeNull();
    expect(fixture.nativeElement.querySelectorAll('app-table thead th')).toHaveLength(22);
    expect(fixture.nativeElement.textContent).toContain('Álgebra');
    fixture.destroy();
  });

  it('reloads when Mostrar is pressed and filters with search', async () => {
    const fixture = await ready();
    fixture.componentInstance.form.controls.year.setValue('2023');
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('app-table')).toBeNull();
    fixture.nativeElement.querySelector('[data-action=show]').click();
    const request = http.expectOne(req => req.url === BASE + '/report');
    expect(request.request.params.get('year')).toBe('2023');
    request.flush({ ...report, data: { ...report.data, year: '2023' } });
    await fixture.whenStable(); fixture.detectChanges();
    const input = fixture.nativeElement.querySelector('app-search input') as HTMLInputElement;
    input.value = 'sin-coincidencias';
    input.dispatchEvent(new Event('input')); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No se encontraron registros');
    fixture.destroy();
  });

  it('exports with the selected year and sanitizes failures', async () => {
    const fixture = await ready();
    fixture.nativeElement.querySelector('[data-action=export]').click();
    http.expectOne(req => req.url === BASE + '/download').flush(new Blob(['xlsx']));
    await fixture.whenStable(); fixture.detectChanges();
    expect(downloads[0].download).toBe('Reporte Actividades Cursos Postgrado - 2024.xlsx');
    expect(revokeUrl).toHaveBeenCalledWith('blob:course-activities');

    fixture.nativeElement.querySelector('[data-action=export]').click();
    http.expectOne(req => req.url === BASE + '/download')
      .flush(new Blob(['private'], { type: 'application/json' }), { status: 503, statusText: 'private' });
    await fixture.whenStable(); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No se pudo exportar el informe.');
    expect(fixture.nativeElement.textContent).not.toContain('private');
    fixture.destroy();
  });

  it('shows Sin Datos and sanitizes report failures', async () => {
    const fixture = TestBed.createComponent(CourseActivities);
    fixture.detectChanges();
    http.expectOne(BASE + '/filters').flush(filters);
    await fixture.whenStable();
    http.expectOne(req => req.url === BASE + '/report')
      .flush({ success: true, data: { year: '2024', total: 0, rows: [] } });
    await fixture.whenStable(); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No se encontraron registros');
    expect(fixture.nativeElement.querySelector('app-table')).not.toBeNull();

    fixture.nativeElement.querySelector('[data-action=show]').click();
    http.expectOne(req => req.url === BASE + '/report')
      .flush({ message: 'private' }, { status: 503, statusText: 'private' });
    await fixture.whenStable(); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No se pudo cargar el informe.');
    expect(fixture.nativeElement.textContent).not.toContain('private');
    fixture.destroy();
  });
});
