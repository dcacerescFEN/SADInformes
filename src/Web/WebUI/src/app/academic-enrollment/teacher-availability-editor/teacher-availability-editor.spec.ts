import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { NgSelectComponent } from '@ng-select/ng-select';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { NotificationService } from '../../core/services/notification-service';
import { TeacherAvailabilityEditor } from './teacher-availability-editor';

const BASE = '/api/reports/academic-enrollment/teacher-availability';
const data = {
  period: '2024A',
  teacherCode: 'P1',
  teacherName: 'ANA PÉREZ',
  courses: [{ code: 'ENECO660', section: '01', name: 'ELECTIVO', description: '1 módulo', moduleType: 4 }],
  moduleTypes: [{ value: 4, text: '1 módulo' }, { value: 3, text: '2 módulos' }],
  modules: [{ id: 10, moduleType: 4, moduleCascade: 'LUN1' }],
  moduleOptions: [
    { moduleType: 4, value: 'LUN1' }, { moduleType: 4, value: 'MAR2' },
    { moduleType: 3, value: 'LUN1MAR2' },
  ],
};

describe('Editar disponibilidad', () => {
  let http: HttpTestingController;
  let showSuccess: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    showSuccess = vi.fn();
    TestBed.configureTestingModule({
      imports: [TeacherAvailabilityEditor],
      providers: [
        provideHttpClient(), provideHttpClientTesting(), provideRouter([]),
        { provide: NotificationService, useValue: { showSuccess } },
        { provide: ActivatedRoute, useValue: { snapshot: {
          paramMap: { get: () => 'P1' }, queryParamMap: { get: () => '2024A' },
        } } },
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => { http.verify(); vi.restoreAllMocks(); TestBed.resetTestingModule(); });

  async function load(payload = data) {
    const fixture = TestBed.createComponent(TeacherAvailabilityEditor);
    fixture.detectChanges();
    http.expectOne((request) => request.url === BASE + '/editor'
      && request.params.get('period') === '2024A'
      && request.params.get('teacherCode') === 'P1')
      .flush({ success: true, data: payload });
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture;
  }

  it('loads courses and current modules', async () => {
    const fixture = await load();
    const root: HTMLElement = fixture.nativeElement;
    expect(root.textContent).toContain('ANA PÉREZ · 2024A');
    expect(root.textContent).toContain('ENECO660');
    expect(root.querySelectorAll('[data-tab]')).toHaveLength(2);
    expect(root.querySelectorAll('table thead th')).toHaveLength(5);
    expect(root.querySelector('[data-action="save"]')).not.toBeNull();
  });

  it('shows No Requiere Horario when the course has no module type', async () => {
    const fixture = await load({
      ...data,
      courses: [{ ...data.courses[0], moduleType: 0, description: 'No Requiere Horario' }],
    });
    const root: HTMLElement = fixture.nativeElement;
    expect(root.textContent).toContain('No Requiere Horario');
  });

  it('selects a course modality from the ng-select options', async () => {
    const fixture = await load();
    const root: HTMLElement = fixture.nativeElement;
    (root.querySelector('[data-action="edit-course"]') as HTMLButtonElement).click();
    fixture.detectChanges();
    const select = fixture.debugElement.query(By.directive(NgSelectComponent)).componentInstance as NgSelectComponent;
    expect(select.appendTo()).toBe('body');
    select.open();
    fixture.detectChanges();
    const option = Array.from(document.querySelectorAll<HTMLElement>('.ng-dropdown-panel .ng-option'))
      .find(item => item.textContent?.includes('2 módulos'));
    if (!option) throw new Error('No se encontró la opción 2 módulos');
    option.click();
    fixture.detectChanges();
    const component = fixture.componentInstance as unknown as {
      data: () => { courses: { moduleType: number }[] };
    };
    expect(component.data().courses[0].moduleType).toBe(3);
    expect(select.element.textContent).toContain('2 módulos');
  });

  it('edits a course modality in memory and saves it with the bottom action', async () => {
    const fixture = await load();
    const root: HTMLElement = fixture.nativeElement;
    (root.querySelector('[data-action="edit-course"]') as HTMLButtonElement).click();
    fixture.detectChanges();
    expect(root.querySelector('tbody ng-select')).not.toBeNull();
    const component = fixture.componentInstance as unknown as {
      data: () => { courses: { code: string; section: string }[] };
      changeCourseType: (course: { code: string; section: string }, moduleType: number) => void;
    };
    const course = component.data().courses[0];
    component.changeCourseType(course, 3);
    fixture.detectChanges();
    (root.querySelector('[data-action="save-course"]') as HTMLButtonElement).click();
    fixture.detectChanges();
    expect(root.textContent).toContain('2 módulos');
    expect(root.querySelector('tbody ng-select')).toBeNull();

    (root.querySelector('[data-action="save"]') as HTMLButtonElement).click();
    const request = http.expectOne((item) => item.url === BASE + '/editor' && item.method === 'PUT');
    expect(request.request.body).toMatchObject({
      courses: [{ code: 'ENECO660', section: '01', moduleType: 3 }],
    });
    request.flush(null, { status: 204, statusText: 'No Content' });
    await fixture.whenStable();
    http.expectOne((item) => item.url === BASE + '/editor' && item.method === 'GET')
      .flush({ success: true, data });
    await fixture.whenStable();
  });

  it('creates a module row, saves it in memory and sends it with the bottom action', async () => {
    const fixture = await load();
    const root: HTMLElement = fixture.nativeElement;
    (root.querySelector('[data-tab="modules"]') as HTMLButtonElement).click();
    fixture.detectChanges();
    const create = root.querySelector('[data-action="create-module"]') as HTMLButtonElement;
    expect(create.classList.contains('btn-create')).toBe(true);
    create.click();
    fixture.detectChanges();
    expect((root.querySelector('[data-action="save"]') as HTMLButtonElement).disabled).toBe(true);
    expect(root.textContent).toContain('Seleccione un módulo');
    expect(root.querySelector('tbody ng-select')).not.toBeNull();
    const component = fixture.componentInstance as unknown as {
      newModules: () => { key: number }[];
      changeNewModuleCascade: (module: { key: number }, cascade: string) => void;
    };
    const created = component.newModules()[0];
    component.changeNewModuleCascade(created, 'MAR2');
    fixture.detectChanges();
    expect((root.querySelector('[data-action="save"]') as HTMLButtonElement).disabled).toBe(false);
    (root.querySelector('[data-action="save-new-module"]') as HTMLButtonElement).click();
    fixture.detectChanges();
    expect(root.textContent).toContain('MAR2');

    (root.querySelector('[data-action="save"]') as HTMLButtonElement).click();
    const request = http.expectOne((item) => item.url === BASE + '/editor' && item.method === 'PUT');
    expect(request.request.body).toMatchObject({
      newModules: [{ moduleType: 4, moduleCascade: 'MAR2' }],
    });
    request.flush(null, { status: 204, statusText: 'No Content' });
    await fixture.whenStable();
    http.expectOne((item) => item.url === BASE + '/editor' && item.method === 'GET')
      .flush({ success: true, data });
    await fixture.whenStable();
  });

  it('saves course and module changes', async () => {
    const fixture = await load();
    (fixture.nativeElement.querySelector('[data-action="save"]') as HTMLButtonElement).click();
    const request = http.expectOne((item) => item.url === BASE + '/editor' && item.method === 'PUT');
    expect(request.request.body).toMatchObject({
      period: '2024A', teacherCode: 'P1',
      courses: [{ code: 'ENECO660', section: '01', moduleType: 4 }],
      modules: [{ id: 10, moduleCascade: 'LUN1' }],
      newModules: [],
    });
    request.flush(null, { status: 204, statusText: 'No Content' });
    await fixture.whenStable();
    expect(showSuccess).toHaveBeenCalledWith('Disponibilidad guardada', 'Los cambios fueron aplicados.');
    http.expectOne((item) => item.url === BASE + '/editor' && item.method === 'GET')
      .flush({ success: true, data });
    await fixture.whenStable();
  });
});
