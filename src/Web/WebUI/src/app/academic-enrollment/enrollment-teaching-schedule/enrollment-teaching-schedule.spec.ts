import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { EnrollmentTeachingSchedule } from './enrollment-teaching-schedule';

const BASE = '/api/reports/academic-enrollment/enrollment-teaching-schedule';
const row = {
  course: 'ENECO660/01', courseName: 'ELECTIVO', teachers: 'ANA PÉREZ',
  classes: 'Lun 10:00', assistantships: 'Mar 12:00', room: 'Sala 1',
  assistantRoom: 'Sala 2', vacancies: '20', enrolled: '15', area: 'Economía',
  teacherId: '12.345.678-9',
};
const filters = {
  success: true,
  data: {
    periods: [{ value: '2024A', text: '2024 Primer Semestre' }],
    areas: [{ value: '%', text: 'Todo' }],
  },
};

describe('Programación Docente', () => {
  let http: HttpTestingController;
  let downloads: HTMLAnchorElement[];
  const createUrl = vi.fn(() => 'blob:schedule');
  const revokeUrl = vi.fn();

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [EnrollmentTeachingSchedule],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
    downloads = [];
    createUrl.mockClear(); revokeUrl.mockClear();
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

  async function load(fixture: { detectChanges: () => void; whenStable: () => Promise<unknown> }) {
    fixture.detectChanges();
    http.expectOne(request => request.url === BASE + '/filters'
      && request.params.get('school') === 'PREGRADO').flush(filters);
    fixture.detectChanges();
    http.expectOne(request => request.url === BASE + '/report'
      && request.params.get('school') === 'PREGRADO'
      && request.params.get('period') === '2024A'
      && request.params.get('area') === '%').flush({
      success: true, data: { rows: [row], total: 1 },
    });
    await fixture.whenStable();
    fixture.detectChanges();
  }

  it('loads the filters and renders the eleven legacy columns', async () => {
    const fixture = TestBed.createComponent(EnrollmentTeachingSchedule);
    await load(fixture);
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('h1')?.textContent?.trim()).toContain('Programación Docente');
    expect(root.querySelectorAll('app-table thead th')).toHaveLength(11);
    expect(root.querySelectorAll('app-table tbody tr')).toHaveLength(1);
    expect(root.textContent).toContain('ANA PÉREZ');
    expect(root.textContent).toContain('12.345.678-9');
    expect(root.textContent).toContain('Total: 1');
  });

  it('reloads periods and report when the school changes', async () => {
    const fixture = TestBed.createComponent(EnrollmentTeachingSchedule);
    await load(fixture);
    fixture.componentInstance.form.controls.school.setValue('POSTGRADO');
    fixture.detectChanges();
    http.expectOne(request => request.url === BASE + '/filters'
      && request.params.get('school') === 'POSTGRADO').flush({
      success: true,
      data: {
        periods: [{ value: '2024B', text: '2024 Segundo Semestre' }],
        areas: [{ value: '%', text: 'Todo' }],
      },
    });
    fixture.detectChanges();
    http.expectOne(request => request.url === BASE + '/report'
      && request.params.get('school') === 'POSTGRADO'
      && request.params.get('period') === '2024B').flush({
      success: true, data: { rows: [row], total: 1 },
    });
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('2024 Segundo Semestre');
  });

  it('shows the empty state and sanitizes a failed report', async () => {
    const fixture = TestBed.createComponent(EnrollmentTeachingSchedule);
    fixture.detectChanges();
    http.expectOne(request => request.url === BASE + '/filters').flush(filters);
    fixture.detectChanges();
    http.expectOne(request => request.url === BASE + '/report').flush({ success: true, data: { rows: [], total: 0 } });
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No se encontraron registros');
    expect(fixture.nativeElement.textContent).toContain('Total: 0');

    fixture.destroy();
    const failed = TestBed.createComponent(EnrollmentTeachingSchedule);
    failed.detectChanges();
    http.expectOne(request => request.url === BASE + '/filters').flush('private', { status: 503, statusText: 'private' });
    await failed.whenStable();
    failed.detectChanges();
    expect(failed.nativeElement.textContent).toContain('No se pudieron cargar los filtros.');
    expect(failed.nativeElement.textContent).not.toContain('private');
  });

  it('exports the filtered rows as xlsx', async () => {
    const fixture = TestBed.createComponent(EnrollmentTeachingSchedule);
    await load(fixture);
    fixture.nativeElement.querySelector('[data-action=export]').click();
    http.expectOne(request => request.url === BASE + '/export'
      && request.params.get('school') === 'PREGRADO'
      && request.params.get('period') === '2024A'
      && request.params.get('area') === '%').flush(new Blob(['xlsx']));
    await fixture.whenStable();
    fixture.detectChanges();
    expect(downloads[0].download).toBe('ProgramacionDocente.xlsx');
    expect(revokeUrl).toHaveBeenCalledWith('blob:schedule');
  });
});
