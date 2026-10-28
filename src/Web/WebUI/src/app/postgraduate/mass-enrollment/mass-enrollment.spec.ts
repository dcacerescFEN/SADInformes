import Swal from 'sweetalert2';
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
  async function settle() {
    await fixture.whenStable();
    fixture.detectChanges();
    // The catalogue refresh resolves before its enrollment continuation updates the notice.
    await fixture.whenStable();
    fixture.detectChanges();
  }
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
    vi.spyOn(Swal, 'fire').mockResolvedValue({ isConfirmed: true, isDenied: false, isDismissed: false });
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
    vi.mocked(Swal.fire).mockResolvedValue({ isConfirmed: false, isDenied: false, isDismissed: true });
    await list();
    root.querySelector<HTMLInputElement>('tbody input')!.click();
    await settle();
    button('enroll').click();
    await settle();
    expect(Swal.fire).toHaveBeenCalledWith(expect.objectContaining({
      text: 'Desea inscribir a los alumnos seleccionados?', showCancelButton: true,
      confirmButtonText: 'Sí, inscribir', cancelButtonText: 'Cancelar',
    }));
    http.expectNone(req => req.method === 'POST');
  });

  it('sends only selected IDs with confirmation and refreshes after verified success', async () => {
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
    http.expectOne(req => req.url === BASE + '/filters').flush(filters);
    await settle();
    http.expectOne(req => req.method === 'GET' && req.url === BASE)
      .flush({ success: true, data: { rows: [row('A1', true), row('A2', true)], total: 2 } });
    await settle();
    expect(root.textContent).toContain('Alumnos inscritos: 1');
    expect(button('enroll').disabled).toBe(true);
  });

  it('a failed write does not claim success or allow a blind retry', async () => {
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
  it('does not turn confirmed enrollment into a list error when the last vacancy is consumed', async () => {
    await list();
    root.querySelector<HTMLInputElement>('tbody input')!.click();
    await settle();
    button('enroll').click();
    await settle();
    http.expectOne(req => req.method === 'POST').flush({ success: true, data: { enrolled: 1 } });
    await settle();
    http.expectOne(req => req.url === BASE + '/filters').flush({
      success: true, data: { ...filters.data, courses: [{ value: 'OTHER/1/0', text: 'Otro curso' }] },
    });
    await settle();
    expect(root.textContent).toContain('Alumnos inscritos: 1');
    expect(root.textContent).not.toContain('No se pudo cargar el listado de alumnos.');
    expect(root.textContent).toContain('El curso ya no está disponible');
    expect(fixture.componentInstance.form.controls.course.value).toBe('');
    expect(root.querySelectorAll('tbody input')).toHaveLength(0);
    expect(button('enroll').disabled).toBe(true);
    http.expectNone(req => req.url === BASE && req.method === 'GET');
  });
  it('locks the selection while SweetAlert confirmation is pending and cancels without a write', async () => {
    let resolve!: (value: { isConfirmed: boolean; isDenied: boolean; isDismissed: boolean }) => void;
    vi.mocked(Swal.fire).mockImplementation(() => new Promise(done => { resolve = done; }));
    await list();
    root.querySelector<HTMLInputElement>('tbody input')!.click();
    await settle();
    button('enroll').click();
    fixture.detectChanges();
    expect(button('enroll').disabled).toBe(true);
    expect(fixture.componentInstance.form.disabled).toBe(true);
    button('enroll').click();
    expect(Swal.fire).toHaveBeenCalledTimes(1);
    resolve({ isConfirmed: false, isDenied: false, isDismissed: true });
    await settle();
    http.expectNone(req => req.method === 'POST');
    expect(fixture.componentInstance.form.enabled).toBe(true);
  });

  it('retains confirmed success if catalogue refresh fails and prevents a blind retry', async () => {
    await list();
    root.querySelector<HTMLInputElement>('tbody input')!.click(); await settle();
    button('enroll').click(); await settle();
    http.expectOne(req => req.method === 'POST').flush({ success: true, data: { enrolled: 1 } });
    await settle();
    http.expectOne(req => req.url === BASE + '/filters').flush({ detail: 'private' },
      { status: 503, statusText: 'Unavailable' });
    await settle();
    expect(root.textContent).toContain('Alumnos inscritos: 1');
    expect(root.textContent).toContain('La inscripción fue confirmada');
    expect(root.textContent).not.toContain('No se pudo confirmar la inscripción.');
    expect(root.textContent).not.toContain('private');
    expect(button('enroll').disabled).toBe(true);
    http.expectNone(req => req.url === BASE && req.method === 'GET');
  });

  it('retains confirmed success if the refreshed list fails', async () => {
    await list();
    root.querySelector<HTMLInputElement>('tbody input')!.click(); await settle();
    button('enroll').click(); await settle();
    http.expectOne(req => req.method === 'POST').flush({ success: true, data: { enrolled: 1 } });
    await settle();
    http.expectOne(req => req.url === BASE + '/filters').flush(filters); await settle();
    http.expectOne(req => req.url === BASE && req.method === 'GET').flush({ detail: 'private' },
      { status: 503, statusText: 'Unavailable' });
    await settle();
    expect(root.textContent).toContain('Alumnos inscritos: 1');
    expect(root.textContent).toContain('No se pudo cargar el listado de alumnos.');
    expect(root.textContent).not.toContain('No se pudo confirmar la inscripción.');
    expect(button('enroll').disabled).toBe(true);
  });

  it('does not POST a stale consent target if selection changes while confirmation is pending', async () => {
    let resolve!: (value: { isConfirmed: boolean; isDenied: boolean; isDismissed: boolean }) => void;
    vi.mocked(Swal.fire).mockImplementation(() => new Promise(done => { resolve = done; }));
    await list();
    root.querySelector<HTMLInputElement>('tbody input')!.click();
    await settle();
    button('enroll').click();
    fixture.componentInstance.form.controls.course.setValue('CHANGED/1/0');
    await settle();
    resolve({ isConfirmed: true, isDenied: false, isDismissed: false });
    await settle();
    http.expectNone(req => req.method === 'POST');
    expect(button('enroll').disabled).toBe(true);
  });
});
