import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { NotificationService } from '../../core/services/notification-service';
import { TeacherAvailabilitySettings } from './teacher-availability-settings';

const BASE = '/api/reports/academic-enrollment/teacher-availability';
const settings = {
  period: '2024A',
  activePeriod: '2024A',
  periods: [
    { value: '2024A', text: '2024 Primer Semestre' },
    { value: '2024B', text: '2024 Segundo Semestre' },
  ],
  startDate: '2024-03-01',
  endDate: '2024-03-20',
  startTime: '08:30',
  endTime: '18:00',
  pendingEmailText: '15 de marzo',
  laggardEmailText: '20 de marzo',
};

describe('Administración Disponibilidad Horaria', () => {
  let http: HttpTestingController;
  let router: Router;
  let confirm: ReturnType<typeof vi.fn>;
  let showSuccess: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    confirm = vi.fn().mockResolvedValue({ isConfirmed: true });
    showSuccess = vi.fn();
    TestBed.configureTestingModule({
      imports: [TeacherAvailabilitySettings],
      providers: [
        provideHttpClient(), provideHttpClientTesting(), provideRouter([]),
        { provide: NotificationService, useValue: { confirm, showSuccess } },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
  });

  afterEach(() => { http.verify(); vi.restoreAllMocks(); TestBed.resetTestingModule(); });

  async function load() {
    const fixture = TestBed.createComponent(TeacherAvailabilitySettings);
    fixture.detectChanges();
    http.expectOne((request) => request.url === BASE + '/settings' && request.params.get('period') === null)
      .flush({ success: true, data: settings });
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture;
  }

  it('loads periods and the selected period values', async () => {
    const fixture = await load();
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('h1')?.textContent).toContain('Administración Disponibilidad Horaria');
    expect(root.textContent).toContain('Activo: 2024A');
    expect(root.textContent).toContain('Volver a Disponibilidad Horaria');
    expect(root.querySelectorAll('.form-floating')).toHaveLength(6);
    expect(root.querySelector('ng-select[data-field="period"]')).not.toBeNull();
    expect((root.querySelector('[data-field="startDate"]') as HTMLInputElement).value).toBe('2024-03-01');
    expect((root.querySelector('[data-field="endTime"]') as HTMLInputElement).value).toBe('18:00');
    expect((root.querySelector('[data-field="pendingEmailText"]') as HTMLInputElement).value).toBe('15 de marzo');
    const actions = Array.from(root.querySelectorAll('[data-action="cancel"], [data-action="save"]'))
      .map(button => button.getAttribute('data-action'));
    expect(actions).toEqual(['cancel', 'save']);
  });

  it('reloads settings when the period changes', async () => {
    const fixture = await load();
    const root: HTMLElement = fixture.nativeElement;
    const component = fixture.componentInstance as unknown as {
      form: { controls: { period: { setValue: (value: string) => void } } };
      onPeriodChange: () => void;
    };
    component.form.controls.period.setValue('2024B');
    component.onPeriodChange();
    fixture.detectChanges();
    http.expectOne((request) => request.url === BASE + '/settings'
      && request.params.get('period') === '2024B')
      .flush({ success: true, data: { ...settings, period: '2024B' } });
    await fixture.whenStable();
    fixture.detectChanges();
    expect(component.form.controls.period).toBeTruthy();
  });

  it('saves without activating the period and reloads', async () => {
    const fixture = await load();
    (fixture.nativeElement.querySelector('[data-action="save"]') as HTMLButtonElement).click();
    const request = http.expectOne((item) => item.url === BASE + '/settings' && item.method === 'PUT');
    expect(request.request.body).toMatchObject({ period: '2024A', processPeriod: false });
    request.flush(null, { status: 204, statusText: 'No Content' });
    await fixture.whenStable();
    expect(showSuccess).toHaveBeenCalledWith('Datos guardados', 'La configuración fue actualizada.');
    http.expectOne((item) => item.url === BASE + '/settings'
      && item.params.get('period') === '2024A').flush({ success: true, data: settings });
    await fixture.whenStable();
  });

  it('asks for confirmation before activating the selected period', async () => {
    const fixture = await load();
    const root: HTMLElement = fixture.nativeElement;
    const process = root.querySelector('[data-field="processPeriod"]') as HTMLInputElement;
    process.click();
    (root.querySelector('[data-action="save"]') as HTMLButtonElement).click();
    expect(confirm).toHaveBeenCalled();
    await fixture.whenStable();
    const request = http.expectOne((item) => item.url === BASE + '/settings' && item.method === 'PUT');
    expect(request.request.body).toMatchObject({ processPeriod: true });
    request.flush(null, { status: 204, statusText: 'No Content' });
    await fixture.whenStable();
    http.expectOne((item) => item.url === BASE + '/settings'
      && item.params.get('period') === '2024A').flush({ success: true, data: settings });
    await fixture.whenStable();
  });

  it('navigates back to availability on cancel', async () => {
    const fixture = await load();
    const navigate = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    (fixture.nativeElement.querySelector('[data-action="cancel"]') as HTMLButtonElement).click();
    expect(navigate).toHaveBeenCalledWith(['/academic-enrollment/teacher-availability']);
  });
});
