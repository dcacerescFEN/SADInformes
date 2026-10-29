import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { NavigationService } from '../../core/navigation/navigation-service';
import { NotificationService } from '../../core/services/notification-service';
import { ExitSurveyEnrollment } from './exit-survey-enrollment';

const BASE = '/api/reports/postgraduate/exit-survey-enrollment';
const catalog = { success: true, data: { studentTypes: [{ value: 'Todos', text: 'Todos' }, { value: 'MBA', text: 'MBA' }], modalities: [{ value: 'Todos', text: 'Todos' }, { value: 'PT', text: 'PT' }], years: ['2024', '2023'] } };
const row = (values: Record<string, unknown> = {}) => ({ studentType: 'MBA', modality: 'PT', personCode: 'P1', name: 'ANA PÉREZ',
  studentCode: '100-20241', entrySemester: '2024A', exitDate: '15-12-2023', degreeDate: '10-01-2024', enrolled: false, progress: null, ...values });
const listing = (rows: unknown[], canEnroll = true) => ({ success: true, data: { rows, total: rows.length, canEnroll } });

describe('Exit survey enrollment', () => {
  let http: HttpTestingController;
  let confirm: ReturnType<typeof vi.fn>;
  const links = signal([{ key: 'student', label: 'Alumnos', icon: '', url: 'https://alumnos.example.test' }]);
  beforeEach(() => {
    confirm = vi.fn().mockResolvedValue({ isConfirmed: true });
    TestBed.configureTestingModule({ imports: [ExitSurveyEnrollment], providers: [provideHttpClient(), provideHttpClientTesting(),
      { provide: NavigationService, useValue: { links } }, { provide: NotificationService, useValue: { confirm } }] });
    http = TestBed.inject(HttpTestingController);
    links.set([{ key: 'student', label: 'Alumnos', icon: '', url: 'https://alumnos.example.test' }]);
  });
  afterEach(() => { http.verify(); TestBed.resetTestingModule(); vi.restoreAllMocks(); });
  async function ready(rows: unknown[] = [row(), row({ studentCode: '200-20241', personCode: 'P2', name: 'LUIS SOTO' })], canEnroll = true) {
    const fixture = TestBed.createComponent(ExitSurveyEnrollment); fixture.detectChanges();
    http.expectOne(r => r.url === BASE + '/filters').flush(catalog);
    await fixture.whenStable(); fixture.detectChanges();
    await Promise.resolve(); fixture.detectChanges();
    http.expectOne(r => r.url === BASE).flush(listing(rows, canEnroll));
    await fixture.whenStable(); fixture.detectChanges();
    return fixture;
  }
  function select(fixture: any, code: string) {
    const box = fixture.nativeElement.querySelector(`input[data-student="${code}"]`) as HTMLInputElement;
    box.click(); fixture.detectChanges(); return box;
  }

  it('loads filters and listing automatically with legacy columns and Todos preserved', async () => {
    const fixture = await ready();
    expect(fixture.componentInstance.form.getRawValue()).toEqual({ studentType: 'Todos', modality: 'Todos', year: '2024' });
    const body: HTMLElement = fixture.nativeElement;
    expect(body.textContent).toContain('Tipo Alumno'); expect(body.textContent).toContain('Modalidad');
    expect(body.textContent).toContain('ANA PÉREZ'); expect(body.textContent).toContain('15-12-2023');
    expect(body.querySelector('img')?.getAttribute('src')).toBe('https://fotos.fen.uchile.cl/P1.jpg');
    expect(body.querySelector('a.student-file-link')?.getAttribute('href')).toBe('https://alumnos.example.test/students/detail/100-20241');
    fixture.destroy();
  });

  it('disables inscribed rows, shows Sin contestar and selects only eligible rows', async () => {
    const fixture = await ready([row({ enrolled: true, progress: 'NULL' }), row({ studentCode: '200-20241', personCode: 'P2', name: 'LUIS' })]);
    const body: HTMLElement = fixture.nativeElement;
    expect(body.textContent).toContain('Sin contestar');
    expect((body.querySelector('input[data-student="100-20241"]') as HTMLInputElement).disabled).toBe(true);
    (body.querySelector('[data-action="select-all"]') as HTMLInputElement).click(); fixture.detectChanges();
    expect([...fixture.componentInstance.selected()]).toContain('200-20241');
    expect(fixture.componentInstance.selected().size).toBe(1);
    fixture.destroy();
  });

  it('does not expose enrollment controls without manage permission', async () => {
    const fixture = await ready([row()], false);
    expect((fixture.nativeElement.querySelector('[data-action="enroll"]') as HTMLButtonElement).disabled).toBe(true);
    expect((fixture.nativeElement.querySelector('input[data-student="100-20241"]') as HTMLInputElement).disabled).toBe(true);
    fixture.destroy();
  });

  it('confirms before posting selected codes only, never client dates or periods', async () => {
    const fixture = await ready();
    select(fixture, '100-20241');
    (fixture.nativeElement.querySelector('[data-action="enroll"]') as HTMLButtonElement).click();
    await fixture.whenStable();
    expect(confirm).toHaveBeenCalledOnce();
    const request = http.expectOne(r => r.url === BASE && r.method === 'POST');
    expect(request.request.body).toEqual({ studentType: 'Todos', modality: 'Todos', year: '2024', studentCodes: ['100-20241'], confirmed: true });
    request.flush({ success: true, data: { enrolled: 1, skipped: 0 } });
    await fixture.whenStable(); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Alumnos Inscritos: 1');
    const reload = http.expectOne(r => r.url === BASE && r.method === 'GET');
    reload.flush(listing([row({ enrolled: true })]));
    await fixture.whenStable(); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('SI');
    fixture.destroy();
  });

  it('cancel does not post, while filter changes invalidate the previous selection', async () => {
    const fixture = await ready();
    select(fixture, '100-20241');
    confirm.mockResolvedValueOnce({ isConfirmed: false });
    (fixture.nativeElement.querySelector('[data-action="enroll"]') as HTMLButtonElement).click();
    await fixture.whenStable(); fixture.detectChanges();
    expect(confirm).toHaveBeenCalledOnce();
    http.expectNone(r => r.method === 'POST');
    expect(fixture.componentInstance.selected().size).toBe(1);
    fixture.componentInstance.form.controls.year.setValue('2023'); fixture.detectChanges();
    expect(fixture.componentInstance.selected().size).toBe(0);
    http.expectOne(r => r.url === BASE && r.method === 'GET').flush(listing([row()]));
    fixture.destroy();
  });

  it('sanitizes enrollment failures, clears selection and reloads authoritative state', async () => {
    const fixture = await ready(); select(fixture, '100-20241');
    (fixture.nativeElement.querySelector('[data-action="enroll"]') as HTMLButtonElement).click();
    await fixture.whenStable();
    http.expectOne(r => r.method === 'POST').flush('private-sql', { status: 503, statusText: 'private' });
    await fixture.whenStable(); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No se pudo confirmar la inscripción.');
    expect(fixture.nativeElement.textContent).not.toContain('private');
    expect(fixture.componentInstance.selected().size).toBe(0);
    http.expectOne(r => r.method === 'GET').flush(listing([row()]));
    fixture.destroy();
  });
});
