import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { IssStatistics } from './iss-statistics';

const BASE = '/api/reports/postgraduate/iss-statistics';
const filters = {
  success: true,
  data: {
    periods: [{ value: '2024A', text: '2024 - Primer Semestre' }],
    courseSections: [{ value: 'ENECO775/1', text: 'Economía / ENECO775 / 1' }],
  },
};
const summary = {
  success: true,
  data: {
    sample: {
      courseCode: 'ENECO775', section: '1', period: '2024A',
      enrolled: 10, answered: 8, percentage: 80, missing: 2,
    },
    categories: [{
      id: 20, description: 'Docencia',
      questions: [{
        question: 'Pregunta uno', alternative1: 1, alternative2: 2, alternative3: 0,
        alternative4: 1, alternative5: 1, total: 5, average: 3.5,
      }],
    }],
    observations: ['Observación uno'],
  },
};

describe('ISS statistics report', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [IssStatistics],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => { http.verify(); TestBed.resetTestingModule(); });

  async function ready() {
    const fixture = TestBed.createComponent(IssStatistics);
    fixture.detectChanges();
    http.expectOne(req => req.url === BASE + '/filters').flush(filters);
    await fixture.whenStable(); fixture.detectChanges();
    return fixture;
  }

  it('loads the dependent filters and waits for Mostrar', async () => {
    const fixture = await ready();
    expect(fixture.componentInstance.form.getRawValue()).toEqual({
      period: '2024A', courseSection: 'ENECO775/1',
    });
    expect(fixture.nativeElement.querySelectorAll('ng-select[data-filter]')).toHaveLength(2);
    expect(fixture.nativeElement.querySelector('table')).toBeNull();
    http.expectNone(BASE + '/summary');
    fixture.destroy();
  });

  it('reloads course sections when the period changes and clears the previous report', async () => {
    const fixture = await ready();
    fixture.nativeElement.querySelector('[data-action=show]').click();
    http.expectOne(req => req.url === BASE + '/summary').flush(summary);
    await fixture.whenStable(); fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[data-section=sample]')).not.toBeNull();
    expect(fixture.nativeElement.querySelectorAll('[data-tab]')).toHaveLength(3);

    fixture.componentInstance.form.controls.period.setValue('2023B');
    const request = http.expectOne(req => req.url === BASE + '/filters');
    expect(request.request.params.get('period')).toBe('2023B');
    request.flush({ ...filters, data: {
      periods: [...filters.data.periods, { value: '2023B', text: '2023 - Segundo Semestre' }],
      courseSections: [{ value: 'ENFIN716/2', text: 'Finanzas / ENFIN716 / 2' }],
    } });
    await fixture.whenStable(); fixture.detectChanges();
    expect(fixture.componentInstance.form.getRawValue()).toEqual({
      period: '2023B', courseSection: 'ENFIN716/2',
    });
    expect(fixture.nativeElement.querySelector('[data-section=sample]')).toBeNull();
    expect(fixture.nativeElement.querySelector('[data-tab]')).toBeNull();
    fixture.destroy();
  });

  it('renders each element in its own tab with the shared table', async () => {
    const fixture = await ready();
    fixture.nativeElement.querySelector('[data-action=show]').click();
    http.expectOne(req => req.url === BASE + '/summary').flush(summary);
    await fixture.whenStable(); fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('[data-tab]')).toHaveLength(3);
    expect(fixture.nativeElement.querySelector('[data-section=sample] app-table table')).not.toBeNull();
    const sampleText = fixture.nativeElement.querySelector('[data-section=sample]').textContent;
    expect(sampleText).toContain('ENECO775');
    expect(sampleText).toContain('80.0 %');

    fixture.nativeElement.querySelector('[data-tab="category-20"]').click();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[data-section="category-20"]')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('[data-section="category-20"] app-table table')).not.toBeNull();
    const categoryText = fixture.nativeElement.querySelector('[data-section="category-20"]').textContent;
    expect(categoryText).toContain('Pregunta uno');
    expect(categoryText).toContain('1 / 20.0 %');
    expect(categoryText).toContain('3.5');

    fixture.nativeElement.querySelector('[data-tab=observations]').click();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[data-section=observations] app-table table')).not.toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Observación uno');
    fixture.destroy();
  });

  it('hides tabs without data and shows a single empty state', async () => {
    const fixture = await ready();
    fixture.nativeElement.querySelector('[data-action=show]').click();
    http.expectOne(req => req.url === BASE + '/summary').flush({
      success: true, data: {
        sample: null,
        categories: [{ id: 20, description: 'Categoría vacía', questions: [] }],
        observations: [],
      },
    });
    await fixture.whenStable(); fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('[data-tab]')).toHaveLength(0);
    expect(fixture.nativeElement.textContent).not.toContain('Categoría vacía');
    expect(fixture.nativeElement.textContent).toContain('Sin Datos');
    expect(fixture.nativeElement.querySelector('app-table')).toBeNull();

    fixture.nativeElement.querySelector('[data-action=show]').click();
    http.expectOne(req => req.url === BASE + '/summary')
      .flush({ message: 'private' }, { status: 503, statusText: 'private' });
    await fixture.whenStable(); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No se pudo cargar el informe.');
    expect(fixture.nativeElement.textContent).not.toContain('private');
    fixture.destroy();
  });
});
