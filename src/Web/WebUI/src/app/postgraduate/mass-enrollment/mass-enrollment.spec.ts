import { TestBed, ComponentFixture } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { MassEnrollment } from './mass-enrollment';

const BASE = '/api/reports/postgraduate/mass-enrollment';
const selection = { studentType: 'MBA', modality: 'PT', cohort: '2024', period: '2024A', course: 'MBA00101/1/0' };
const filters = { success: true, data: {
  studentTypes: [{ value: 'MBA', text: 'MBA' }], modalities: ['PT'], cohorts: ['2024'],
  periods: [{ value: '2024A', text: 'Primer semestre' }], courses: [{ value: 'MBA00101/1/0', text: 'Curso Uno' }],
} };
const row = (code: string, enrolled = false) => ({
  studentCode: code, name: code === 'A1' ? '<img src=x onerror=alert(1)>' : 'Alumno Dos',
  rut: '12345678-9', entrySemester: '2024A', entryType: 'REGULAR',
  situation: 'REGULAR', reason: '', decree: 'D1', enrolled,
});

describe('Inscripción masiva', () => {
  let fixture: ComponentFixture<MassEnrollment>;
  let http: HttpTestingController;
  let root: HTMLElement;
  async function settle() { await fixture.whenStable(); fixture.detectChanges(); }
  const button = (action: string) => root.querySelector<HTMLButtonElement>('[data-action="' + action + '"]')!;
  async function list() {
    root.querySelector('form')!.dispatchEvent(new Event('submit'));
    await settle();
    http.expectOne(req => req.method === 'GET' && req.url === BASE).flush({
      success: true, data: { rows: [row('A1'), row('A2', true)], total: 2 },
    });
    await settle();
  }
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MassEnrollment], providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(MassEnrollment);
    root = fixture.nativeElement;
    fixture.detectChanges();
    http.expectOne(req => req.url === BASE + '/filters').flush(filters);
    await settle();
  });
  afterEach(() => { http.verify(); fixture.destroy(); vi.restoreAllMocks(); });

  it('uses the existing report format and concrete cascading filters', () => {
    expect(root.querySelector('h1')?.textContent).toContain('Inscripción masiva');
    expect(root.querySelectorAll('ng-select')).toHaveLength(5);
    expect(root.querySelectorAll('.page-header')).toHaveLength(2);
    expect(fixture.componentInstance.form.getRawValue()).toEqual(selection);
    expect(root.textContent).not.toContain('Todos');
    expect(button('enroll').disabled).toBe(true);
  });

  it('renders legacy fields safely and selects only eligible students', async () => {
    await list();
    expect(root.textContent).toContain('D1');
    expect(root.textContent).toContain('2024A');
    expect(root.querySelector('td img[src="x"]')).toBeNull();
    const checks = root.querySelectorAll<HTMLInputElement>('tbody input[type=checkbox]');
    expect(checks).toHaveLength(2);
    expect(checks[1].disabled).toBe(true);
    root.querySelector<HTMLInputElement>('[data-action="select-all"]')!.click();
    await settle();
    expect(checks[0].checked).toBe(true);
    expect(checks[1].checked).toBe(false);
    expect(button('enroll').disabled).toBe(false);
  });

  it('cancelling confirmation never sends a write', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(false);
    await list();
    root.querySelector<HTMLInputElement>('tbody input')!.click();
    await settle();
    button('enroll').click();
    await settle();
    expect(window.confirm).toHaveBeenCalledWith('Desea inscribir a los alumnos seleccionados?');
    http.expectNone(req => req.method === 'POST');
  });

  it('sends only selected IDs with confirmation and refreshes after verified success', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    await list();
    root.querySelector<HTMLInputElement>('tbody input')!.click();
    await settle();
    button('enroll').click();
    await settle();
    const request = http.expectOne(req => req.method === 'POST' && req.url === BASE);
    expect(request.request.body).toEqual({ selection, studentCodes: ['A1'], confirmed: true });
    expect(button('enroll').disabled).toBe(true);
    request.flush({ success: true, data: { enrolled: 1 } });
    await settle();
    http.expectOne(req => req.method === 'GET' && req.url === BASE)
      .flush({ success: true, data: { rows: [row('A1', true), row('A2', true)], total: 2 } });
    await settle();
    expect(root.textContent).toContain('Alumnos inscritos: 1');
    expect(button('enroll').disabled).toBe(true);
  });

  it('a failed write does not claim success or allow a blind retry', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    await list();
    root.querySelector<HTMLInputElement>('tbody input')!.click();
    await settle();
    button('enroll').click();
    await settle();
    http.expectOne(req => req.method === 'POST').flush({ detail: 'private-sql-detail' },
      { status: 503, statusText: 'Unavailable' });
    await settle();
    expect(root.textContent).toContain('Actualice el listado antes de reintentar');
    expect(root.textContent).not.toContain('private-sql-detail');
    expect(root.textContent).not.toContain('Alumnos inscritos:');
    expect(button('enroll').disabled).toBe(true);
  });

  it('changing a filter clears stale candidates and selection before reloading', async () => {
    await list();
    root.querySelector<HTMLInputElement>('tbody input')!.click();
    await settle();
    fixture.componentInstance.form.controls.studentType.setValue('MGP');
    await settle();
    expect(root.querySelectorAll('tbody input')).toHaveLength(0);
    expect(button('enroll').disabled).toBe(true);
    http.expectOne(req => req.url === BASE + '/filters').flush({
      success: true, data: { ...filters.data, studentTypes: [{ value: 'MGP', text: 'MGP' }], modalities: [], periods: [], courses: [] },
    });
    await settle();
    expect(fixture.componentInstance.form.controls.modality.value).toBe('');
  });

  it('an older catalogue response cannot replace a newer filter selection', async () => {
    fixture.componentInstance.form.controls.studentType.setValue('MGP');
    await settle();
    const older = http.expectOne(req => req.url === BASE + '/filters');
    fixture.componentInstance.form.controls.studentType.setValue('MBA');
    await settle();
    const newer = http.expectOne(req => req.url === BASE + '/filters');
    newer.flush(filters); await settle();
    older.flush({ success: true, data: { ...filters.data, modalities: ['STALE'] } }); await settle();
    expect(fixture.componentInstance.form.controls.modality.value).toBe('PT');
  });
});
