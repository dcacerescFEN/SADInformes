import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ElectivesWithoutCommonPlan } from './electives-without-common-plan';

const BASE = '/api/reports/academic-enrollment/electives-without-common-plan';

describe('Electivos de mención sin plan común', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ElectivesWithoutCommonPlan],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => { http.verify(); TestBed.resetTestingModule(); });

  it('loads automatically and renders the three legacy columns', async () => {
    const fixture = TestBed.createComponent(ElectivesWithoutCommonPlan);
    fixture.detectChanges();
    http.expectOne(BASE + '/report').flush({
      success: true,
      data: {
        total: 2,
        rows: [
          { decree: '009615/2003', courseCode: 'ENECO660', courseName: 'Electivo de mención' },
          { decree: 'ND IC', courseCode: 'ENECO661', courseName: 'Electivo de profundización' },
        ],
      },
    });
    await fixture.whenStable();
    fixture.detectChanges();

    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('h1')?.textContent?.trim())
      .toBe('Electivos de mención que no están en el plan común');
    expect(root.querySelectorAll('app-table thead th')).toHaveLength(3);
    expect(root.querySelectorAll('app-table tbody tr')).toHaveLength(2);
    expect(root.textContent).toContain('ENECO660');
    expect(root.textContent).toContain('Electivo de profundización');
    expect(root.textContent).toContain('Total: 2');
  });

  it('filters with the shared search and shows the empty state', async () => {
    const fixture = TestBed.createComponent(ElectivesWithoutCommonPlan);
    fixture.detectChanges();
    http.expectOne(BASE + '/report').flush({
      success: true,
      data: { total: 1, rows: [
        { decree: 'ND IC', courseCode: 'ENECO661', courseName: 'Electivo de profundización' },
      ] },
    });
    await fixture.whenStable();
    fixture.detectChanges();

    const input = fixture.nativeElement.querySelector('app-search input') as HTMLInputElement;
    input.value = 'ENECO661';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('app-table tbody tr')).toHaveLength(1);

    input.value = 'sin coincidencias';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No se encontraron registros');
  });

  it('keeps the results section visible for an empty response', async () => {
    const fixture = TestBed.createComponent(ElectivesWithoutCommonPlan);
    fixture.detectChanges();
    http.expectOne(BASE + '/report').flush({ success: true, data: { total: 0, rows: [] } });
    await fixture.whenStable();
    fixture.detectChanges();

    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('.form-section')).not.toBeNull();
    expect(root.querySelector('app-table')).not.toBeNull();
    expect(root.textContent).toContain('No se encontraron registros');
    expect(root.textContent).toContain('Total: 0');
  });

  it('sanitizes a failed request without inventing rows', async () => {
    const fixture = TestBed.createComponent(ElectivesWithoutCommonPlan);
    fixture.detectChanges();
    http.expectOne(BASE + '/report').flush('private', { status: 503, statusText: 'private' });
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('No se pudo cargar el informe.');
    expect(fixture.nativeElement.textContent).not.toContain('private');
    expect(fixture.nativeElement.querySelector('app-table')).toBeNull();
  });
});
