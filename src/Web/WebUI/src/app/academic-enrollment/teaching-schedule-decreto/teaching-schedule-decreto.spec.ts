import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute } from '@angular/router';
import { TeachingScheduleDecreto } from './teaching-schedule-decreto';

const BASE = '/api/reports/academic-enrollment/teaching-schedule-decreto';
const slug = 'teaching-schedule-ic-old-plan';
const row = {
  course: 'ENECO660/01', courseCode: 'ENECO660', courseName: 'ELECTIVO', type: 'Obligatoria',
  credits: '4', mention: 'PC', requirements: 'Sin requisitos', teachers: 'ANA PÉREZ',
  schedule: 'Lun 10:00',
};
const filters = {
  success: true,
  data: {
    title: 'Ingeniería Comercial Plan Antiguo',
    periods: [{ value: '2024A', text: '2024 Primer Semestre' }],
  },
};

describe('Programación Docente por decreto', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [TeachingScheduleDecreto],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: ActivatedRoute, useValue: { snapshot: { data: { report: { slug } } } } },
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => { http.verify(); TestBed.resetTestingModule(); });

  async function load(fixture: { detectChanges: () => void; whenStable: () => Promise<unknown> }) {
    fixture.detectChanges();
    http.expectOne(request => request.url === BASE + '/filters'
      && request.params.get('slug') === slug).flush(filters);
    fixture.detectChanges();
    http.expectOne(request => request.url === BASE + '/report'
      && request.params.get('slug') === slug
      && request.params.get('period') === '2024A').flush({
      success: true,
      data: { title: 'Ingeniería Comercial Plan Antiguo', rows: [row], total: 1 },
    });
    await fixture.whenStable();
    fixture.detectChanges();
  }

  it('loads the plan title and renders the nine legacy columns', async () => {
    const fixture = TestBed.createComponent(TeachingScheduleDecreto);
    await load(fixture);
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('h1')?.textContent?.trim()).toContain('Ingeniería Comercial Plan Antiguo');
    expect(root.querySelectorAll('app-table thead th')).toHaveLength(9);
    expect(root.querySelectorAll('app-table tbody tr')).toHaveLength(1);
    expect(root.textContent).toContain('ANA PÉREZ');
    expect(root.textContent).toContain('Sin requisitos');
    expect(root.textContent).toContain('Total: 1');
  });

  it('reloads the report when the period changes', async () => {
    const fixture = TestBed.createComponent(TeachingScheduleDecreto);
    await load(fixture);
    fixture.componentInstance.form.controls.period.setValue('2024B');
    fixture.detectChanges();
    http.expectOne(request => request.url === BASE + '/report'
      && request.params.get('period') === '2024B').flush({
      success: true,
      data: { title: 'Ingeniería Comercial Plan Antiguo', rows: [], total: 0 },
    });
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Total: 0');
  });

  it('shows the empty state and keeps the results section', async () => {
    const fixture = TestBed.createComponent(TeachingScheduleDecreto);
    fixture.detectChanges();
    http.expectOne(request => request.url === BASE + '/filters').flush(filters);
    fixture.detectChanges();
    http.expectOne(request => request.url === BASE + '/report').flush({ success: true, data: { title: 'Plan', rows: [], total: 0 } });
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No se encontraron registros');
    expect(fixture.nativeElement.querySelector('app-table')).not.toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Total: 0');
  });

  it('sanitizes a failed report', async () => {
    const fixture = TestBed.createComponent(TeachingScheduleDecreto);
    fixture.detectChanges();
    http.expectOne(request => request.url === BASE + '/filters').flush(filters);
    fixture.detectChanges();
    http.expectOne(request => request.url === BASE + '/report').flush('private', { status: 503, statusText: 'private' });
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No se pudo cargar el informe.');
    expect(fixture.nativeElement.textContent).not.toContain('private');
  });
});
