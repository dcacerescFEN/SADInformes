import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { StudentsWithoutPhoto } from './students-without-photo';

const BASE = '/api/reports/academic-enrollment/students-without-photo';
const row = {
  name: 'ANA PÉREZ', rut: 'P2', studentCode: 'A/2', studentType: 'IC',
  mobile: '912345678', mailFen: 'aperez@fen.uchile.cl', personalMail: 'ana@test.cl',
};

describe('Alumnos sin fotos', () => {
  let http: HttpTestingController;
  let downloads: HTMLAnchorElement[];
  const createUrl = vi.fn(() => 'blob:photos');
  const revokeUrl = vi.fn();

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [StudentsWithoutPhoto],
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

  function initialRequest(page = '1', search = '') {
    return (request: { url: string; params: { get: (name: string) => string | null } }) =>
      request.url === BASE + '/report'
      && request.params.get('page') === page
      && request.params.get('pageSize') === '10'
      && request.params.get('search') === search;
  }

  async function load(
    fixture: { detectChanges: () => void; whenStable: () => Promise<unknown> },
    hasMore = true,
  ) {
    fixture.detectChanges();
    http.expectOne(initialRequest()).flush({
      success: true,
      data: { rows: [row], page: 1, hasMore, scanned: 8 },
    });
    await fixture.whenStable();
    fixture.detectChanges();
  }

  it('loads one server page with the seven legacy columns', async () => {
    const fixture = TestBed.createComponent(StudentsWithoutPhoto);
    await load(fixture);
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('h1')?.textContent?.trim()).toContain('Alumnos sin fotos');
    expect(root.querySelectorAll('app-dynamic-table thead th')).toHaveLength(7);
    expect(root.querySelectorAll('app-dynamic-table tbody tr')).toHaveLength(1);
    expect(root.textContent).toContain('ANA PÉREZ');
    expect(root.textContent).toContain('Mostrando 1 registros');
    expect(root.textContent).toContain('Página 1');
    expect(root.textContent).toContain('quedan más resultados');
  });

  it('navigates with stable server page numbers', async () => {
    const fixture = TestBed.createComponent(StudentsWithoutPhoto);
    await load(fixture);
    const root: HTMLElement = fixture.nativeElement;
    const next = Array.from(root.querySelectorAll<HTMLButtonElement>('button.page-link'))
      .find(button => button.textContent?.includes('Siguiente'));
    if (!next) throw new Error('No se encontró el botón Siguiente');
    next.click();
    http.expectOne(initialRequest('2')).flush({
      success: true,
      data: { rows: [], page: 2, hasMore: false, scanned: 0 },
    });
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Página 2');
    expect(fixture.nativeElement.textContent).toContain('No se encontraron registros');

    const previous = Array.from(root.querySelectorAll<HTMLButtonElement>('button.page-link'))
      .find(button => button.textContent?.includes('Anterior'));
    if (!previous) throw new Error('No se encontró el botón Anterior');
    previous.click();
    http.expectOne(initialRequest('1')).flush({
      success: true,
      data: { rows: [row], page: 1, hasMore: true, scanned: 8 },
    });
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Página 1');
  });

  it('sends the search to the server and resets to the first page', async () => {
    const fixture = TestBed.createComponent(StudentsWithoutPhoto);
    await load(fixture);
    const input = fixture.nativeElement.querySelector('app-search input') as HTMLInputElement;
    input.value = 'ANA';
    input.dispatchEvent(new Event('input'));
    await new Promise(resolve => setTimeout(resolve, 350));
    fixture.detectChanges();
    http.expectOne(initialRequest('1', 'ANA')).flush({
      success: true,
      data: { rows: [row], page: 1, hasMore: false, scanned: 1 },
    });
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('ANA PÉREZ');
    expect(fixture.nativeElement.textContent).toContain('Página 1');
  });

  it('shows the empty state and sanitizes a failure', async () => {
    const fixture = TestBed.createComponent(StudentsWithoutPhoto);
    fixture.detectChanges();
    http.expectOne(initialRequest()).flush({
      success: true,
      data: { rows: [], page: 1, hasMore: false, scanned: 0 },
    });
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No se encontraron registros');

    fixture.destroy();
    const failed = TestBed.createComponent(StudentsWithoutPhoto);
    failed.detectChanges();
    http.expectOne(initialRequest()).flush('private', { status: 503, statusText: 'private' });
    await failed.whenStable();
    failed.detectChanges();
    expect(failed.nativeElement.textContent).toContain('No se pudo cargar el informe.');
    expect(failed.nativeElement.textContent).not.toContain('private');
  });

  it('exports the full report as xlsx', async () => {
    const fixture = TestBed.createComponent(StudentsWithoutPhoto);
    await load(fixture);
    fixture.nativeElement.querySelector('[data-action=export]').click();
    http.expectOne(BASE + '/export').flush(new Blob(['xlsx']));
    await fixture.whenStable();
    fixture.detectChanges();
    expect(downloads[0].download).toBe('SinFotos.xlsx');
    expect(revokeUrl).toHaveBeenCalledWith('blob:photos');
  });
});
