import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { StudentsWithoutPriority } from './students-without-priority';

const BASE = '/api/reports/academic-enrollment/students-without-priority';

describe('Alumnos sin prioridad', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [StudentsWithoutPriority],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => { http.verify(); TestBed.resetTestingModule(); });

  it('loads automatically and renders the four legacy columns', async () => {
    const fixture = TestBed.createComponent(StudentsWithoutPriority);
    fixture.detectChanges();
    http.expectOne(BASE + '/report').flush({
      success: true,
      data: {
        total: 2,
        rows: [
          { studentCode: 'A/1', name: 'Ana Pérez', academicStatus: 'Regular', studentType: 'IC' },
          { studentCode: 'A/2', name: 'Luis Soto', academicStatus: 'Regular', studentType: 'IICG' },
        ],
      },
    });
    await fixture.whenStable();
    fixture.detectChanges();

    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('h1')?.textContent?.trim())
      .toBe('Alumnos que pueden postular y no tienen prioridad');
    expect(root.querySelectorAll('app-table thead th')).toHaveLength(4);
    expect(root.querySelectorAll('app-table tbody tr')).toHaveLength(2);
    expect(root.textContent).toContain('Ana Pérez');
    expect(root.textContent).toContain('IICG');
    expect(root.textContent).toContain('Total: 2');
  });

  it('filters with the shared search and shows the empty state', async () => {
    const fixture = TestBed.createComponent(StudentsWithoutPriority);
    fixture.detectChanges();
    http.expectOne(BASE + '/report').flush({
      success: true,
      data: { total: 1, rows: [
        { studentCode: 'A/1', name: 'Ana Pérez', academicStatus: 'Regular', studentType: 'IC' },
      ] },
    });
    await fixture.whenStable();
    fixture.detectChanges();

    const input = fixture.nativeElement.querySelector('app-search input') as HTMLInputElement;
    input.value = 'Pérez';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('app-table tbody tr')).toHaveLength(1);

    input.value = 'sin coincidencias';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No se encontraron registros');
  });

  it('keeps the results section visible for an empty response', async () => {
    const fixture = TestBed.createComponent(StudentsWithoutPriority);
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
    const fixture = TestBed.createComponent(StudentsWithoutPriority);
    fixture.detectChanges();
    http.expectOne(BASE + '/report').flush('private', { status: 503, statusText: 'private' });
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('No se pudo cargar el informe.');
    expect(fixture.nativeElement.textContent).not.toContain('private');
    expect(fixture.nativeElement.querySelector('app-table')).toBeNull();
  });
});
