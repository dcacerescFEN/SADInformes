import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { AfeReport } from './afe-report';

const BASE = '/api/reports/postgraduate/afe-report';
const filters = {
  success: true,
  data: {
    periods: [{ value: '2024A', text: '2024 - Primer Semestre' }],
    studentTypes: [{ value: 'Todos', text: 'Todos' }, { value: 'MBA', text: 'MBA' }],
    modalities: [{ value: 'Todos', text: 'Todos' }],
    courses: [{ value: 'ENECO775/1', text: 'ENECO775/1' }, { value: 'ENFIN716/2', text: 'ENFIN716/2' }],
  },
};
const advances = {
  success: true,
  data: { advances: [{ value: '-1', text: 'Todos' }, { value: '10', text: '1' }] },
};
const report = {
  success: true,
  data: {
    total: 2,
    rows: [
      { advance: '2', title: 'Avance dos', createdAt: '02/01/2024', dueAt: '03/01/2024',
        teacher: 'Profesor Uno', course: 'Economía (ENECO775/1)', student: 'Pérez Ana', group: '3',
        deliveredAt: '04/01/2024', grade: '6.5', evaluatedAt: '05/01/2024',
        evaluationObservation: 'Buen trabajo' },
      { advance: '1', title: 'Avance uno', createdAt: '01/01/2024', dueAt: '02/01/2024',
        teacher: 'Profesor Uno', course: 'Economía (ENECO775/1)', student: 'Soto Luis', group: '',
        deliveredAt: '', grade: '', evaluatedAt: '', evaluationObservation: '' },
    ],
  },
};

describe('AFE report', () => {
  let http: HttpTestingController;
  let downloads: HTMLAnchorElement[];
  const createUrl = vi.fn(() => 'blob:afe');
  const revokeUrl = vi.fn();

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [AfeReport],
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
    const fixture = TestBed.createComponent(AfeReport);
    fixture.detectChanges();
    http.expectOne(req => req.url === BASE + '/filters').flush(filters);
    await fixture.whenStable();
    http.expectOne(req => req.url === BASE + '/advances').flush(advances);
    await fixture.whenStable(); fixture.detectChanges();
    return fixture;
  }

  it('loads five cascading filters and advances without querying the report', async () => {
    const fixture = await ready();
    expect(fixture.componentInstance.form.getRawValue()).toEqual({
      period: '2024A', studentType: 'Todos', modality: 'Todos',
      course: 'ENECO775/1', advance: '-1',
    });
    expect(fixture.nativeElement.querySelectorAll('ng-select[data-filter]')).toHaveLength(5);
    expect(fixture.nativeElement.querySelector('app-table')).toBeNull();
    http.expectNone(BASE + '/report');
    fixture.destroy();
  });

  it('reloads advances when the course changes', async () => {
    const fixture = await ready();
    fixture.componentInstance.form.controls.course.setValue('ENFIN716/2');
    const request = http.expectOne(req => req.url === BASE + '/advances');
    expect(request.request.params.get('course')).toBe('ENFIN716/2');
    request.flush({ success: true, data: { advances: [{ value: '30', text: '1' }] } });
    await fixture.whenStable(); fixture.detectChanges();
    expect(fixture.componentInstance.form.controls.advance.value).toBe('30');
    fixture.destroy();
  });

  it('shows the shared table, filters it and exports the workbook', async () => {
    const fixture = await ready();
    fixture.nativeElement.querySelector('[data-action=show]').click();
    const request = http.expectOne(req => req.url === BASE + '/report');
    expect(request.request.params.get('advance')).toBe('-1');
    request.flush(report);
    await fixture.whenStable(); fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('app-table table')).not.toBeNull();
    expect(fixture.nativeElement.querySelectorAll('app-table tbody tr')).toHaveLength(2);
    const input = fixture.nativeElement.querySelector('app-search input') as HTMLInputElement;
    input.value = 'Pérez';
    input.dispatchEvent(new Event('input')); fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('app-table tbody tr')).toHaveLength(1);

    fixture.nativeElement.querySelector('[data-action=export]').click();
    http.expectOne(req => req.url === BASE + '/download').flush(new Blob(['xlsx']));
    await fixture.whenStable(); fixture.detectChanges();
    expect(downloads[0].download).toBe('Reporte nivel curso AFE - coordinacion.xlsx');
    expect(revokeUrl).toHaveBeenCalledWith('blob:afe');
    fixture.destroy();
  });

  it('shows No se encontraron resultados and sanitizes failures', async () => {
    const fixture = await ready();
    fixture.nativeElement.querySelector('[data-action=show]').click();
    http.expectOne(req => req.url === BASE + '/report')
      .flush({ success: true, data: { total: 0, rows: [] } });
    await fixture.whenStable(); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No se encontraron resultados');

    fixture.nativeElement.querySelector('[data-action=show]').click();
    http.expectOne(req => req.url === BASE + '/report')
      .flush({ message: 'private' }, { status: 503, statusText: 'private' });
    await fixture.whenStable(); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No se pudo cargar el reporte.');
    expect(fixture.nativeElement.textContent).not.toContain('private');
    fixture.destroy();
  });
});
