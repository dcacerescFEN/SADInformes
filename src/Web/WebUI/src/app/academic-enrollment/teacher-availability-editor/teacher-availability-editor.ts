import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { ActivatedRoute, Router } from '@angular/router';
import { NotificationService } from '../../core/services/notification-service';

interface Course { code: string; section: string; name: string; description: string; moduleType: number; editing?: boolean; }
interface ModuleType { value: number; text: string; }
interface Module { id: number; moduleType: number; moduleCascade: string; editing?: boolean; }
interface ModuleOption { moduleType: number; value: string; }
interface NewModule { key: number; moduleType: number; moduleCascade: string; editing: boolean; }
interface EditorData {
  period: string;
  teacherCode: string;
  teacherName: string;
  courses: Course[];
  moduleTypes: ModuleType[];
  modules: Module[];
  moduleOptions: ModuleOption[];
}

const BASE = '/api/reports/academic-enrollment/teacher-availability';
const AVAILABILITY_ROUTE = '/academic-enrollment/teacher-availability';

@Component({
  selector: 'app-teacher-availability-editor',
  imports: [FormsModule, NgSelectModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="container-fluid px-0 py-4">
      <div class="page-header">
        <div class="d-flex flex-wrap justify-content-between align-items-start gap-3">
          <div>
            <h1 class="page-title">
              <i class="bi bi-pencil-square" aria-hidden="true"></i>
              Editar disponibilidad
            </h1>
            <p class="page-subtitle mb-0">
              @if (data(); as value) { {{ value.teacherName }} · {{ value.period }}. }
            </p>
          </div>
          <button type="button" class="btn btn-clear" data-action="back" (click)="back()">
            <i class="bi bi-arrow-left me-1" aria-hidden="true"></i>Volver a Disponibilidad Horaria
          </button>
        </div>
      </div>

      @if (loading()) {
        <p role="status">Cargando disponibilidad…</p>
      } @else if (error()) {
        <div class="alert alert-warning" role="alert">{{ error() }}</div>
      } @else if (data(); as value) {
        <ul class="nav nav-tabs app-tabs mb-3" role="tablist">
          <li class="nav-item" role="presentation">
            <button type="button" class="nav-link" [class.active]="activeTab() === 'courses'"
              data-tab="courses" (click)="activeTab.set('courses')">Cursos y modalidad horaria</button>
          </li>
          <li class="nav-item" role="presentation">
            <button type="button" class="nav-link" [class.active]="activeTab() === 'modules'"
              data-tab="modules" (click)="activeTab.set('modules')">Módulos de disponibilidad</button>
          </li>
        </ul>

        @if (activeTab() === 'courses') {
          <div class="form-section mb-3">
            <h2 class="section-title"><i class="bi bi-journal-bookmark" aria-hidden="true"></i>Cursos y modalidad horaria</h2>
            @if (value.courses.length === 0) {
              <p class="mb-0">El profesor no tiene cursos en este período.</p>
            } @else {
              <div class="table-responsive">
                <table class="table table-hover align-middle app-table">
                  <thead>
                    <tr>
                      <th>Código</th>
                      <th>Sección</th>
                      <th>Cátedra</th>
                      <th>Modalidad actual</th>
                      <th>Acciones</th>
                    </tr>
                  </thead>
                  <tbody>
                    @for (course of value.courses; track course.code + '/' + course.section) {
                      <tr>
                        <td>{{ course.code }}</td>
                        <td>{{ course.section }}</td>
                        <td>{{ course.name }}</td>
                        <td>
                          @if (course.editing) {
                            <ng-select class="ng-select-custom ng-select-custom-plain" appendTo="body"
                              [items]="typeOptions()" bindLabel="text" bindValue="value"
                              [searchable]="true" [clearable]="false"
                              [ngModel]="course.moduleType" (ngModelChange)="changeCourseType(course, $event)"
                              [attr.aria-label]="'Modalidad de ' + course.code">
                            </ng-select>
                          } @else {
                            {{ courseTypeName(course) }}
                          }
                        </td>
                        <td class="table-action-cell">
                          @if (course.editing) {
                            <button type="button" class="btn btn-sm btn-availability-single btn-icon-only"
                              title="Guardar modalidad" aria-label="Guardar modalidad"
                              data-action="save-course" (click)="saveCourse(course)">
                              <i class="bi bi-save" aria-hidden="true"></i>
                            </button>
                          } @else {
                            <button type="button" class="btn btn-sm btn-availability-single btn-icon-only"
                              title="Editar modalidad" aria-label="Editar modalidad"
                              data-action="edit-course" (click)="editCourse(course)">
                              <i class="bi bi-pencil" aria-hidden="true"></i>
                            </button>
                          }
                        </td>
                      </tr>
                    }
                  </tbody>
                </table>
              </div>
            }
          </div>
        } @else {
          <div class="form-section mb-3">
            <div class="d-flex flex-wrap justify-content-between align-items-center gap-2">
              <h2 class="section-title mb-0"><i class="bi bi-grid-3x3-gap" aria-hidden="true"></i>Módulos de disponibilidad</h2>
              <button type="button" class="btn btn-create" data-action="create-module" (click)="addModule()">
                <i class="bi bi-plus-circle me-1" aria-hidden="true"></i>Crear módulo
              </button>
            </div>

            <div class="table-responsive mt-3">
              <table class="table table-hover align-middle app-table">
                <thead>
                  <tr>
                    <th>Tipo de módulo</th>
                    <th>Módulo cascada</th>
                    <th>Acciones</th>
                  </tr>
                </thead>
                <tbody>
                  @for (module of value.modules; track module.id) {
                    <tr>
                      <td>{{ moduleTypeName(module.moduleType) }}</td>
                      <td>
                        @if (module.editing) {
                          <ng-select class="ng-select-custom ng-select-custom-plain" appendTo="body"
                            [items]="optionsFor(module.moduleType)" bindLabel="value" bindValue="value"
                            [searchable]="true" [clearable]="false" placeholder="Seleccione un módulo"
                            [ngModel]="module.moduleCascade" (ngModelChange)="changeModule(module, $event)"
                            [attr.aria-label]="'Módulo cascada ' + module.id">
                          </ng-select>
                        } @else {
                          {{ module.moduleCascade }}
                        }
                      </td>
                      <td class="table-action-cell">
                        @if (module.editing) {
                          <button type="button" class="btn btn-sm btn-availability-single btn-icon-only"
                            title="Guardar módulo" aria-label="Guardar módulo"
                            data-action="save-module" (click)="saveModule(module)">
                            <i class="bi bi-save" aria-hidden="true"></i>
                          </button>
                        } @else {
                          <button type="button" class="btn btn-sm btn-availability-single btn-icon-only"
                            title="Editar módulo" aria-label="Editar módulo"
                            data-action="edit-module" (click)="editModule(module)">
                            <i class="bi bi-pencil" aria-hidden="true"></i>
                          </button>
                        }
                      </td>
                    </tr>
                  }
                  @for (module of newModules(); track module.key) {
                    <tr>
                      <td>
                        @if (module.editing) {
                          <ng-select class="ng-select-custom ng-select-custom-plain" appendTo="body"
                            [items]="newTypeOptions()" bindLabel="text" bindValue="value"
                            [searchable]="true" [clearable]="false" placeholder="Tipo de módulo"
                            [ngModel]="module.moduleType" (ngModelChange)="changeNewModuleType(module, $event)"
                            aria-label="Tipo de módulo nuevo">
                          </ng-select>
                        } @else {
                          {{ moduleTypeName(module.moduleType) }}
                        }
                      </td>
                      <td>
                        @if (module.editing) {
                          <ng-select class="ng-select-custom ng-select-custom-plain" appendTo="body"
                            [items]="optionsFor(module.moduleType)" bindLabel="value" bindValue="value"
                            [searchable]="true" [clearable]="false" placeholder="Seleccione un módulo"
                            [fixedPlaceholder]="!module.moduleCascade"
                            [ngModel]="module.moduleCascade || null" (ngModelChange)="changeNewModuleCascade(module, $event)"
                            aria-label="Módulo cascada nuevo">
                          </ng-select>
                        } @else {
                          {{ module.moduleCascade }}
                        }
                      </td>
                      <td class="table-action-cell">
                        @if (module.editing) {
                          <button type="button" class="btn btn-sm btn-availability-single btn-icon-only"
                            title="Guardar módulo nuevo" aria-label="Guardar módulo nuevo"
                            data-action="save-new-module" [disabled]="!module.moduleCascade.trim()" (click)="saveNewModule(module)">
                            <i class="bi bi-save" aria-hidden="true"></i>
                          </button>
                        } @else {
                          <button type="button" class="btn btn-sm btn-availability-single btn-icon-only"
                            title="Editar módulo nuevo" aria-label="Editar módulo nuevo"
                            data-action="edit-new-module" (click)="editNewModule(module)">
                            <i class="bi bi-pencil" aria-hidden="true"></i>
                          </button>
                        }
                      </td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          </div>
        }

        <div class="d-flex flex-wrap justify-content-end gap-2 mt-4">
          <button type="button" class="btn btn-clear" data-action="cancel" (click)="back()">
            <i class="bi bi-x-circle me-1" aria-hidden="true"></i>Cancelar
          </button>
          <button type="button" class="btn btn-create" data-action="save"
            [disabled]="saving() || !canSave()" (click)="save()">
            <i class="bi bi-save me-1" aria-hidden="true"></i>{{ saving() ? 'Guardando…' : 'Guardar' }}
          </button>
        </div>
      }
    </div>
  `,
})
export class TeacherAvailabilityEditor {
  private readonly http = inject(HttpClient);
  private readonly destroyRef = inject(DestroyRef);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly notification = inject(NotificationService);
  protected readonly data = signal<EditorData | null>(null);
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly error = signal('');
  protected readonly newModules = signal<NewModule[]>([]);
  protected readonly activeTab = signal<'courses' | 'modules'>('courses');
  private newModuleKey = 0;

  constructor() { this.load(); }

  private readonly emptyOptions: ModuleOption[] = [];
  protected readonly moduleTypes = signal<ModuleType[]>([]);
  private readonly moduleOptions = signal<ModuleOption[]>([]);
  private readonly optionsByType = computed(() => {
    const map = new Map<number, ModuleOption[]>();
    for (const option of this.moduleOptions()) {
      const options = map.get(option.moduleType) ?? [];
      options.push(option);
      map.set(option.moduleType, options);
    }
    return map;
  });

  protected optionsFor(moduleType: number): ModuleOption[] {
    return this.optionsByType().get(moduleType) ?? this.emptyOptions;
  }

  protected moduleTypeName(value: number): string {
    return this.moduleTypes().find(type => type.value === value)?.text ?? String(value);
  }

  protected editCourse(course: Course): void {
    this.data.update(value => value === null ? value : {
      ...value,
      courses: value.courses.map(row => ({
        ...row, editing: row.code === course.code && row.section === course.section,
      })),
    });
  }

  protected saveCourse(course: Course): void {
    this.data.update(value => value === null ? value : {
      ...value,
      courses: value.courses.map(row => row.code === course.code && row.section === course.section
        ? { ...row, editing: false } : row),
    });
  }

  protected readonly typeOptions = computed(() => {
    const types = this.moduleTypes();
    return types.some(type => type.value === 0)
      ? types : [{ value: 0, text: 'No Requiere Horario' }, ...types];
  });

  protected readonly newTypeOptions = computed(() =>
    this.moduleTypes().filter(type => type.value > 0));

  protected courseTypeName(course: Course): string {
    if (course.moduleType <= 0) return 'No Requiere Horario';
    const type = this.moduleTypes().find(item => item.value === course.moduleType);
    return type?.text ?? course.description?.trim() ?? 'No Requiere Horario';
  }

  protected canSave(): boolean {
    const existing = this.data()?.modules ?? [];
    return existing.every(module => module.moduleCascade.trim().length > 0)
      && this.newModules().every(module =>
        module.moduleType > 0 && module.moduleCascade.trim().length > 0);
  }

  protected changeCourseType(course: Course, moduleType: number): void {
    this.data.update(value => value === null ? value : {
      ...value,
      courses: value.courses.map(row => row.code === course.code && row.section === course.section
        ? { ...row, moduleType } : row),
    });
  }

  protected editModule(module: Module): void {
    this.data.update(value => value === null ? value : {
      ...value,
      modules: value.modules.map(row => ({ ...row, editing: row.id === module.id })),
    });
  }

  protected saveModule(module: Module): void {
    this.data.update(value => value === null ? value : {
      ...value,
      modules: value.modules.map(row => row.id === module.id ? { ...row, editing: false } : row),
    });
  }

  protected changeModule(module: Module, moduleCascade: string): void {
    this.data.update(value => value === null ? value : {
      ...value,
      modules: value.modules.map(row => row.id === module.id ? { ...row, moduleCascade } : row),
    });
  }

  protected addModule(): void {
    const type = this.newTypeOptions()[0]?.value ?? 0;
    this.newModules.update(rows => [...rows, {
      key: ++this.newModuleKey, moduleType: type, moduleCascade: '', editing: true,
    }]);
    this.error.set('');
  }

  protected changeNewModuleType(module: NewModule, moduleType: number): void {
    this.newModules.update(rows => rows.map(row => row.key === module.key
      ? { ...row, moduleType, moduleCascade: '' } : row));
  }

  protected changeNewModuleCascade(module: NewModule, moduleCascade: string): void {
    this.newModules.update(rows => rows.map(row => row.key === module.key
      ? { ...row, moduleCascade } : row));
  }

  protected saveNewModule(module: NewModule): void {
    const current = this.newModules().find(row => row.key === module.key);
    if (!current?.moduleCascade.trim()) {
      this.error.set('Seleccione el módulo cascada.');
      return;
    }
    this.newModules.update(rows => rows.map(row => row.key === module.key
      ? { ...row, editing: false } : row));
    this.error.set('');
  }

  protected editNewModule(module: NewModule): void {
    this.newModules.update(rows => rows.map(row => row.key === module.key
      ? { ...row, editing: true } : row));
  }

  protected save(): void {
    const value = this.data();
    if (!value || this.saving()) return;
    if (this.newModules().some(module => !module.moduleCascade)) {
      this.error.set('Complete los módulos nuevos.');
      return;
    }
    this.saving.set(true);
    this.error.set('');
    this.http.put<void>(BASE + '/editor', {
      period: value.period,
      teacherCode: value.teacherCode,
      courses: value.courses.map(course => ({
        code: course.code, section: course.section, moduleType: course.moduleType,
      })),
      modules: value.modules.map(module => ({
        id: module.id, moduleCascade: module.moduleCascade,
      })),
      newModules: this.newModules().map(module => ({
        moduleType: module.moduleType, moduleCascade: module.moduleCascade,
      })),
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.saving.set(false);
        this.notification.showSuccess('Disponibilidad guardada', 'Los cambios fueron aplicados.');
        this.load();
      },
      error: (error: HttpErrorResponse) => {
        this.saving.set(false);
        const title = error.error?.title;
        this.error.set(typeof title === 'string' && title.length > 0
          ? title : 'No se pudo guardar la disponibilidad.');
      },
    });
  }

  protected back(): void {
    void this.router.navigate([AVAILABILITY_ROUTE]);
  }

  private load(): void {
    const teacherCode = this.route.snapshot.paramMap.get('teacherCode') ?? '';
    const period = this.route.snapshot.queryParamMap.get('period') ?? '';
    this.loading.set(true);
    this.error.set('');
    this.http.get<{ success: boolean; data: EditorData }>(BASE + '/editor', {
      params: { period, teacherCode },
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: result => {
        this.loading.set(false);
        if (!result.success) { this.error.set('No se pudo cargar la disponibilidad.'); return; }
        this.data.set(result.data);
        this.moduleTypes.set(result.data.moduleTypes);
        this.moduleOptions.set(result.data.moduleOptions);
        this.newModules.set([]);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('No se pudo cargar la disponibilidad.');
      },
    });
  }
}
