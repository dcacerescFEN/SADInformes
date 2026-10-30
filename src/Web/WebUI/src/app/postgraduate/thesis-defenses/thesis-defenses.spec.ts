import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ThesisDefenses } from './thesis-defenses';

const BASE = '/api/reports/postgraduate/thesis-defenses';
const catalog = {
  success: true,
  data: { years: ['2026', '2024'], studentTypes: ['Todos', 'MBA'], modalities: ['Todos'] },
};
const coordination = {
  studentCode: 'A/1', studentName: 'Ana Pérez', program: 'MBA', modality: 'PT',
  entryYear: '2020', examDate: '15/06/2024', period: '2024F', courseCode: 'MBA600',
  section: '01', grade: '5.5', examStatus: 'Aprobado', thesisTopic: 'Tesis', guide: 'Guía',
  coGuide: 'CoGuía', informant1: 'Inf1', informant2: 'Inf2', informant3: 'Inf3',
  costCenter: 'Centro', defenseYear: '2024', resolutionDate: '01/07/2024',
};
const treasury = {
  period: '2024F', courseCode: 'MBA600', section: '01', professorCode: 'P/1',
  professorName: 'Ana Pérez', type: 'GUIA', studentCode: 'A/1', grade: '5.5',
  studentName: 'Ana Pérez', program: 'MBA', modality: 'PT', entryYear: '2020',
  thesisTopic: 'Tesis', examDate: '15/06/2024 00:00:00', examStatus: 'Vigente',
  costCenter: 'Centro', resolutionDate: '01/07/2024',
};

describe('Postgraduate thesis defenses', () => {
  let http: HttpTestingController;
  let downloads: HTMLAnchorElement[];
  const createUrl = vi.fn(() => 'blob:thesis-defenses');
  const revokeUrl = vi.fn();

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ThesisDefenses],
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

  afterEach(() => { http.verify(); vi.restoreAllMocks(); vi.unstubAllGlobals(); });

  async function ready() {
    const fixture = TestBed.createComponent(ThesisDefenses);
    fixture.detectChanges();
    http.expectOne(request => request.url === BASE + '/filters').flush(catalog);
    await fixture.whenStable(); fixture.detectChanges();
    return fixture;
  }
  const submit = (fixture: any) =>
    fixture.nativeElement.querySelector('form').dispatchEvent(new Event('submit'));

  it('defaults all filters to Todos and waits for Buscar', async () => {
    const fixture = await ready();
    expect(fixture.componentInstance.form.getRawValue()).toEqual({
      defenseYear: 'Todos', studentType: 'Todos', modality: 'Todos', entryYear: 'Todos',
    });
    expect(fixture.nativeElement.querySelectorAll('ng-select[data-filter]')).toHaveLength(4);
    const options = (fixture.componentInstance as any).options();
    expect(options.defenseYear.map((row: any) => row.value)).toEqual(['Todos', '2026', '2024']);
    expect(options.entryYear.map((row: any) => row.value)).toEqual(['Todos', '2026', '2024']);
    http.expectNone(request => request.url === BASE);
    expect(fixture.nativeElement.querySelector('[data-action=show] i.bi-search')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('table')).toBeNull();
    expect(fixture.nativeElement.querySelectorAll('[data-report-tab]')).toHaveLength(2);
    fixture.destroy();
  });

  it('reloads modalities when the student type changes', async () => {
    const fixture = await ready();
    fixture.componentInstance.form.controls.studentType.setValue('MBA');
    const request = http.expectOne(req => req.url === BASE + '/filters');
    expect(request.request.params.get('studentType')).toBe('MBA');
    request.flush({ ...catalog, data: { ...catalog.data, modalities: ['Todos', 'PT', 'FT'] } });
    await fixture.whenStable(); fixture.detectChanges();
    expect(fixture.componentInstance.form.controls.modality.value).toBe('Todos');
    expect((fixture.componentInstance as any).options().modality.map((row: any) => row.value))
      .toEqual(['Todos', 'PT', 'FT']);
    fixture.destroy();
  });

  it('renders Coordination 20 columns and Treasury 17 columns from one search', async () => {
    const fixture = await ready();
    submit(fixture);
    const request = http.expectOne(req => req.url === BASE);
    expect(request.request.params.get('defenseYear')).toBe('Todos');
    expect(request.request.params.get('studentType')).toBe('Todos');
    expect(request.request.params.get('modality')).toBe('Todos');
    expect(request.request.params.get('entryYear')).toBe('Todos');
    request.flush({ success: true, data: {
      coordination: [coordination], treasury: [treasury], coordinationTotal: 1, treasuryTotal: 1,
    } });
    await fixture.whenStable(); fixture.detectChanges();
    const body: HTMLElement = fixture.nativeElement;
    let tables = body.querySelectorAll('table');
    expect(tables).toHaveLength(1);
    expect(tables[0].querySelectorAll('tbody tr:first-child td')).toHaveLength(20);
    expect(body.textContent).toContain('Reporte Comisiones formato Coordinación');
    expect(body.textContent).toContain('Tema/Nombre Tésis');
    (body.querySelector('[data-report-tab=treasury]') as HTMLButtonElement).click();
    fixture.detectChanges();
    tables = body.querySelectorAll('table');
    expect(tables).toHaveLength(1);
    expect(tables[0].querySelectorAll('tbody tr:first-child td')).toHaveLength(17);
    expect(body.textContent).toContain('Reporte Comisiones formato Tesorería');
    expect(body.textContent).not.toContain('Total Coordinación:');
    fixture.destroy();
  });

  it('downloads both formats for the shown selection', async () => {
    const fixture = await ready();
    submit(fixture);
    http.expectOne(req => req.url === BASE).flush({ success: true, data: {
      coordination: [coordination], treasury: [treasury], coordinationTotal: 1, treasuryTotal: 1,
    } });
    await fixture.whenStable(); fixture.detectChanges();
    const body: HTMLElement = fixture.nativeElement;
    (body.querySelector('[data-action=export-coordination]') as HTMLButtonElement).click();
    const coordinationRequest = http.expectOne(req => req.url === BASE + '/download/coordination');
    expect(coordinationRequest.request.params.get('studentType')).toBe('Todos');
    coordinationRequest.flush(new Blob(['xlsx']));
    await fixture.whenStable(); fixture.detectChanges();
    (body.querySelector('[data-action=export-treasury]') as HTMLButtonElement).click();
    const treasuryRequest = http.expectOne(req => req.url === BASE + '/download/treasury');
    treasuryRequest.flush(new Blob(['xlsx']));
    await fixture.whenStable(); fixture.detectChanges();
    expect(downloads).toHaveLength(2);
    expect(downloads[0].download).toBe('ReporteComisionesCoordinacion.xlsx');
    expect(downloads[1].download).toBe('ReporteComisionesTesoreria.xlsx');
    expect(revokeUrl).toHaveBeenCalledTimes(2);
    fixture.destroy();
  });

  it('paginates and filters both grids independently', async () => {
    const fixture = await ready();
    submit(fixture);
    const coordinationRows = Array.from({ length: 120 }, (_, index) => ({
      ...coordination, studentCode: 'A-' + String(index + 1).padStart(3, '0'),
    }));
    const treasuryRows = Array.from({ length: 12 }, (_, index) => ({
      ...treasury, professorCode: 'P-' + String(index + 1).padStart(3, '0'), studentCode: 'A-' + String(index + 1).padStart(3, '0'),
    }));
    http.expectOne(req => req.url === BASE).flush({ success: true, data: {
      coordination: coordinationRows, treasury: treasuryRows,
      coordinationTotal: 120, treasuryTotal: 12,
    } });
    await fixture.whenStable(); fixture.detectChanges();
    const body: HTMLElement = fixture.nativeElement;
    let tables = body.querySelectorAll('table');
    expect(tables).toHaveLength(1);
    expect(tables[0].querySelectorAll('tbody tr')).toHaveLength(10);
    const navs = body.querySelectorAll('nav');
    expect(Array.from(navs[0].querySelectorAll('.page-item'))
      .map(item => item.textContent?.trim()))
      .toEqual(['Anterior', '1', '2', '3', '...', '12', 'Siguiente']);
    let searches = body.querySelectorAll('app-search input');
    (searches[0] as HTMLInputElement).value = 'A-011';
    searches[0].dispatchEvent(new Event('input'));
    fixture.detectChanges();
    expect(tables[0].querySelectorAll('tbody tr')).toHaveLength(1);
    (body.querySelector('[data-report-tab=treasury]') as HTMLButtonElement).click();
    fixture.detectChanges();
    tables = body.querySelectorAll('table');
    expect(tables[0].querySelectorAll('tbody tr')).toHaveLength(10);
    searches = body.querySelectorAll('app-search input');
    (searches[0] as HTMLInputElement).value = 'P-011';
    searches[0].dispatchEvent(new Event('input'));
    fixture.detectChanges();
    expect(tables[0].querySelectorAll('tbody tr')).toHaveLength(1);
    fixture.destroy();
  });

  it('shows Sin Datos when both grids are empty and sanitizes errors', async () => {
    const fixture = await ready();
    submit(fixture);
    http.expectOne(req => req.url === BASE).flush({ success: true, data: {
      coordination: [], treasury: [], coordinationTotal: 0, treasuryTotal: 0,
    } });
    await fixture.whenStable(); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Sin Datos');
    submit(fixture);
    http.expectOne(req => req.url === BASE).flush('private-sql', { status: 503, statusText: 'private' });
    await fixture.whenStable(); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No se pudo cargar el reporte.');
    expect(fixture.nativeElement.textContent).not.toContain('private');
    fixture.destroy();
  });
});
