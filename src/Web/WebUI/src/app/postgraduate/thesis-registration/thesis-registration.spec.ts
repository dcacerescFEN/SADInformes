import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ThesisRegistration } from './thesis-registration';

const BASE = '/api/reports/postgraduate/thesis-registration';
const catalog = {
  success: true,
  data: {
    studentTypes: [{ value: 'Todos', text: 'Todos' }, { value: 'MBA', text: 'MBA' }],
    modalities: ['Todos', 'PT'],
    years: ['Todos', '2024'],
  },
};

describe('Thesis registration report', () => {
  let http: HttpTestingController;
  let downloads: HTMLAnchorElement[];
  const createUrl = vi.fn(() => 'blob:thesis-registration');
  const revokeUrl = vi.fn();

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ThesisRegistration],
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
  afterEach(() => {
    http.verify(); vi.restoreAllMocks(); vi.unstubAllGlobals(); TestBed.resetTestingModule();
  });

  async function ready() {
    const fixture = TestBed.createComponent(ThesisRegistration);
    fixture.detectChanges();
    http.expectOne(req => req.url === BASE + '/filters').flush(catalog);
    await fixture.whenStable(); fixture.detectChanges();
    return fixture;
  }

  it('loads three dependent filters without an on-screen data table', async () => {
    const fixture = await ready();
    expect(fixture.componentInstance.form.getRawValue()).toEqual({
      studentType: 'Todos', modality: 'Todos', year: 'Todos',
    });
    expect(fixture.nativeElement.querySelectorAll('ng-select[data-filter]')).toHaveLength(3);
    expect(fixture.nativeElement.querySelector('table')).toBeNull();
    fixture.destroy();
  });

  it('reloads modalities when the student type changes', async () => {
    const fixture = await ready();
    fixture.componentInstance.form.controls.studentType.setValue('MBA');
    const request = http.expectOne(req => req.url === BASE + '/filters');
    expect(request.request.params.get('studentType')).toBe('MBA');
    request.flush({ ...catalog, data: { ...catalog.data, modalities: ['Todos', 'GLOBAL'] } });
    await fixture.whenStable(); fixture.detectChanges();
    expect(fixture.componentInstance.form.controls.modality.value).toBe('Todos');
    expect((fixture.componentInstance as any).options().modality.map((row: any) => row.value))
      .toEqual(['Todos', 'GLOBAL']);
    fixture.destroy();
  });

  it('downloads the selected filters and revokes the blob', async () => {
    const fixture = await ready();
    fixture.componentInstance.form.controls.studentType.setValue('MBA');
    http.expectOne(req => req.url === BASE + '/filters' && req.params.get('studentType') === 'MBA')
      .flush(catalog);
    await fixture.whenStable(); fixture.detectChanges();
    fixture.componentInstance.form.patchValue({ modality: 'PT', year: '2024' });
    fixture.nativeElement.querySelector('[data-action=export]').click();
    const request = http.expectOne(req => req.url === BASE + '/download');
    expect(request.request.params.get('studentType')).toBe('MBA');
    expect(request.request.params.get('modality')).toBe('PT');
    expect(request.request.params.get('year')).toBe('2024');
    request.flush(new Blob(['xlsx']));
    await fixture.whenStable(); fixture.detectChanges();
    expect(downloads).toHaveLength(1);
    expect(downloads[0].download).toBe('RegistroTesisPostgrado.xlsx');
    expect(revokeUrl).toHaveBeenCalledWith('blob:thesis-registration');
    fixture.destroy();
  });

  it('shows Sin Datos and sanitizes export errors', async () => {
    const fixture = await ready();
    fixture.nativeElement.querySelector('[data-action=export]').click();
    http.expectOne(req => req.url === BASE + '/download')
      .flush(new Blob(['{}'], { type: 'application/json' }), { status: 400, statusText: 'Bad Request' });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Sin Datos');
    fixture.nativeElement.querySelector('[data-action=export]').click();
    http.expectOne(req => req.url === BASE + '/download')
      .flush(new Blob(['private'], { type: 'text/plain' }), { status: 503, statusText: 'private' });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No se pudo exportar el informe.');
    expect(fixture.nativeElement.textContent).not.toContain('private');
    fixture.destroy();
  });
});
