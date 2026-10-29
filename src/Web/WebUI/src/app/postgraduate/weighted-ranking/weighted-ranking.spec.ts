import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { NavigationService } from '../../core/navigation/navigation-service';
import { WeightedRanking } from './weighted-ranking';

const BASE = '/api/reports/postgraduate/weighted-ranking';
const catalog = {
  success: true,
  data: {
    studentTypes: [{ value: 'MBA', text: 'MBA' }, { value: 'MGP', text: 'MGP' }],
    modalities: ['PT', 'FT'], years: ['2026', '2024'],
  },
};
const row = (name = 'Ana') => ({
  position: 2, name, rut: '00123456-K', studentCode: 'A/1', entrySemester: '2024A',
  situation: 'TITULADO', weightedAverage: '5.250', percentile: '0.5000',
});

describe('Postgraduate weighted ranking', () => {
  let http: HttpTestingController;
  let downloads: HTMLAnchorElement[];
  const links = signal([{ key: 'student', label: 'Alumnos', icon: '', url: 'https://alumnos.example.test' }]);
  const createUrl = vi.fn(() => 'blob:weighted-ranking');
  const revokeUrl = vi.fn();

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [WeightedRanking],
      providers: [provideHttpClient(), provideHttpClientTesting(),
        { provide: NavigationService, useValue: { links } }],
    });
    http = TestBed.inject(HttpTestingController);
    downloads = [];
    createUrl.mockClear();
    revokeUrl.mockClear();
    links.set([{ key: 'student', label: 'Alumnos', icon: '', url: 'https://alumnos.example.test' }]);
    const NativeURL = URL;
    vi.stubGlobal('URL', class extends NativeURL {
      static override createObjectURL = createUrl;
      static override revokeObjectURL = revokeUrl;
    });
    vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(function (this: HTMLAnchorElement) {
      downloads.push(this);
    });
  });

  afterEach(() => {
    http.verify();
    vi.restoreAllMocks();
    vi.unstubAllGlobals();
  });

  async function ready() {
    const fixture = TestBed.createComponent(WeightedRanking);
    fixture.detectChanges();
    http.expectOne(request => request.url === BASE + '/filters').flush(catalog);
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture;
  }

  const submit = (fixture: any) =>
    fixture.nativeElement.querySelector('form').dispatchEvent(new Event('submit'));

  it('defaults concrete filters without Todos and waits for Mostrar', async () => {
    const fixture = await ready();
    expect(fixture.componentInstance.form.getRawValue()).toEqual({
      studentType: 'MBA', modality: 'PT', cohort: '2026', year: '2026',
    });
    expect(fixture.nativeElement.querySelectorAll('ng-select[data-filter]')).toHaveLength(4);
    expect(fixture.nativeElement.textContent).not.toContain('Todos');
    http.expectNone(request => request.url === BASE);
    expect(fixture.nativeElement.querySelector('[data-action=export]').disabled).toBe(true);
    const show = fixture.nativeElement.querySelector('button[type=submit]');
    expect(show.textContent).toContain('Mostrar');
    expect(show.querySelector('i.bi-search')).not.toBeNull();
    expect(fixture.nativeElement.textContent).not.toContain('Volver');
    fixture.destroy();
  });

  it('preserves server order/precision and renders photo fallback plus authorized encoded student link', async () => {
    const fixture = await ready();
    fixture.componentInstance.form.controls.cohort.setValue('2024');
    submit(fixture);
    const report = http.expectOne(request => request.url === BASE);
    expect(report.request.params.get('cohort')).toBe('2024');
    expect(report.request.params.get('year')).toBe('2026');
    report.flush({ success: true, data: { rows: [row(), { ...row('Luis'), position: 1, studentCode: 'A2' }], total: 2 } });
    await fixture.whenStable();
    fixture.detectChanges();
    const body: HTMLElement = fixture.nativeElement;
    const firstRow = body.querySelector('tbody tr')!;
    expect(firstRow.querySelector('td')?.textContent?.trim()).toBe('2');
    for (const value of ['5.250', '0.5000']) expect(body.textContent).toContain(value);
    expect(body.querySelector('a.student-file-link')?.getAttribute('href'))
      .toBe('https://alumnos.example.test/students/detail/A%2F1');
    const photo = body.querySelector('tbody img') as HTMLImageElement;
    expect(photo.src).toBe('https://fotos.fen.uchile.cl/00123456-K.jpg');
    photo.dispatchEvent(new Event('error'));
    expect(photo.src).toBe('https://fotos.fen.uchile.cl/perfilNN.jpg');
    links.set([]);
    fixture.detectChanges();
    expect(body.querySelector('a.student-file-link')).toBeNull();
    expect(body.textContent).toContain('A/1');
    fixture.destroy();
  });

  it('invalidates old results and ignores stale dependent catalog responses', async () => {
    const fixture = TestBed.createComponent(WeightedRanking);
    fixture.detectChanges();
    const stale = http.expectOne(request => request.url === BASE + '/filters');
    fixture.componentInstance.form.controls.studentType.setValue('MGP');
    const fresh = http.expectOne(request => request.url === BASE + '/filters');
    expect(fresh.request.params.get('studentType')).toBe('MGP');
    fresh.flush({ ...catalog, data: { ...catalog.data, modalities: ['GLOBAL'] } });
    stale.flush(catalog);
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.componentInstance.form.controls.modality.value).toBe('GLOBAL');
    submit(fixture);
    const pending = http.expectOne(request => request.url === BASE);
    fixture.componentInstance.form.controls.year.setValue('2024');
    expect(pending.cancelled).toBe(true);
    expect(fixture.nativeElement.querySelector('[data-action=export]').disabled).toBe(true);
    fixture.destroy();
  });

  it('downloads the shown selection as Excel and revokes its blob URL', async () => {
    const fixture = await ready();
    submit(fixture);
    http.expectOne(request => request.url === BASE)
      .flush({ success: true, data: { rows: [row()], total: 1 } });
    await fixture.whenStable();
    fixture.detectChanges();
    fixture.nativeElement.querySelector('[data-action=export]').click();
    const request = http.expectOne(request => request.url === BASE + '/download');
    expect(request.request.params.get('studentType')).toBe('MBA');
    expect(request.request.params.get('modality')).toBe('PT');
    expect(request.request.params.get('cohort')).toBe('2026');
    request.flush(new Blob(['xlsx']));
    await fixture.whenStable();
    fixture.detectChanges();
    expect(downloads).toHaveLength(1);
    expect(downloads[0].download).toBe('Ranking_Programas_Graduados.xlsx');
    expect(revokeUrl).toHaveBeenCalledWith('blob:weighted-ranking');
    fixture.destroy();
  });

  it('shows Sin Datos for an empty ranking and sanitizes SP errors without exporting', async () => {
    const fixture = await ready();
    submit(fixture);
    http.expectOne(request => request.url === BASE)
      .flush({ success: true, data: { rows: [], total: 0 } });
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Sin Datos');
    expect(fixture.nativeElement.querySelector('[data-action=export]').disabled).toBe(true);
    submit(fixture);
    http.expectOne(request => request.url === BASE)
      .flush('private-sql', { status: 503, statusText: 'private' });
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No se pudo cargar el ranking.');
    expect(fixture.nativeElement.textContent).not.toContain('private');
    expect(downloads).toHaveLength(0);
    fixture.destroy();
  });
});
