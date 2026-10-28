import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { NgbConfig } from '@ng-bootstrap/ng-bootstrap';
import { AuthService } from '../auth/auth-service';
import { ReportsSidebar } from './reports-sidebar';
import { POSTGRADUATE_REPORTS } from '../../postgraduate/postgraduate-catalogue';

@Component({ template: '' })
class TestPage {}

describe('Sidebar de Informes', () => {
  let fixture: ComponentFixture<ReportsSidebar>;
  let originalWidth: PropertyDescriptor;
  beforeEach(async () => {
    originalWidth = Object.getOwnPropertyDescriptor(window, 'innerWidth')!;
    Object.defineProperty(window, 'innerWidth', { configurable: true, writable: true, value: 375 });
    await TestBed.configureTestingModule({ imports: [ReportsSidebar], providers: [
      provideRouter([
        { path: '', component: TestPage }, { path: 'otra', component: TestPage },
        ...POSTGRADUATE_REPORTS.map(report => ({ path: 'postgrado/' + report.slug, component: TestPage })),
      ]),
      { provide: AuthService, useValue: { username: () => 'usuario', name: () => 'Usuario Prueba' } },
    ] }).compileComponents();
    TestBed.inject(NgbConfig).animation = false;
    fixture = TestBed.createComponent(ReportsSidebar);
    fixture.detectChanges();
    await TestBed.inject(Router).navigateByUrl('/');
    await settle();
  });
  afterEach(async () => {
    fixture.destroy();
    await new Promise<void>((resolve) => setTimeout(resolve, 0));
    Object.defineProperty(window, 'innerWidth', originalWidth);
  });

  async function settle() {
    fixture.detectChanges();
    await fixture.whenStable();
    await new Promise<void>((resolve) => setTimeout(resolve, 0));
    fixture.detectChanges();
  }

  it('muestra solo las secciones, el perfil y el estado colapsado', () => {
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('#reports-sidebar')?.getAttribute('aria-label')).toBe('Informes');
    expect(root.querySelectorAll('.sidebar-navigation a').length).toBe(0);
    expect(root.querySelector('.sidebar-navigation')?.textContent).not.toContain('Inicio');
    expect(root.textContent).toContain('usuario');
    expect(root.textContent).toContain('Usuario Prueba');
    expect(root.querySelector('.sidebar-avatar')?.textContent?.trim()).toBe('US');
    expect(root.textContent).not.toContain('Gestión de acceso');
    expect(root.textContent).not.toContain('Períodos académicos');
    fixture.componentRef.setInput('collapsed', true);
    fixture.detectChanges();
    expect(root.classList.contains('collapsed')).toBe(true);
    expect(root.querySelector('aside')?.classList.contains('sidebar--collapsed')).toBe(true);
  });

  it('muestra cada opción con nombre accesible, ruta propia y estado activo', async () => {
    const root: HTMLElement = fixture.nativeElement;
    const toggle = root.querySelector<HTMLButtonElement>('[data-section="postgrado"]')!;
    expect(toggle.getAttribute('aria-expanded')).toBe('false');
    toggle.click();
    await settle();
    expect(toggle.getAttribute('aria-expanded')).toBe('true');
    for (const report of POSTGRADUATE_REPORTS) {
      const link = root.querySelector<HTMLAnchorElement>(`a[href="/postgrado/${report.slug}"]`)!;
      expect(link.textContent).toContain(report.title);
      expect(link.getAttribute('aria-label')).toBe(report.title);
      expect(link.title).toBe(report.title);
    }
    await TestBed.inject(Router).navigateByUrl('/postgrado/' + POSTGRADUATE_REPORTS[0].slug);
    await settle();
    expect(root.querySelectorAll('a[aria-current="page"]')).toHaveLength(1);
    expect(root.querySelector('a[aria-current="page"]')?.textContent).toContain(POSTGRADUATE_REPORTS[0].title);
  });

  it('navega a Postgrado desde el panel móvil y lo cierra', async () => {
    fixture.componentInstance.openMobile();
    await settle();
    const target = '/postgrado/' + POSTGRADUATE_REPORTS[0].slug;
    document.querySelector<HTMLButtonElement>('[role="dialog"] [data-section="postgrado"]')!.click();
    await settle();
    document.querySelector<HTMLAnchorElement>(`[role="dialog"] a[href="${target}"]`)!.click();
    await settle();
    expect(TestBed.inject(Router).url).toBe(target);
    expect(fixture.componentInstance.mobileOpen()).toBe(false);
    expect(document.querySelector('[role="dialog"]')).toBeNull();
  });

  it('incluye las 14 secciones del SAD como dropdowns sin activar funcionalidades', async () => {
    const root: HTMLElement = fixture.nativeElement;
    const toggles = [...root.querySelectorAll<HTMLButtonElement>('[data-section]')];
    expect(toggles.map(button => button.getAttribute('aria-label'))).toEqual([
      'Inscripción académica', 'Solicitudes', 'Aplicación de reglamento', 'Certificados',
      'Bienestar', 'Ceremonia', 'Acreditación', 'Postgrado', 'Gestión docente', 'Cadfpreg',
      'Otros', 'NexoComunicacion', 'SEE', 'Híbridas y actividades extracurriculares',
    ]);
    toggles[0].click();
    await settle();
    expect(toggles[0].getAttribute('aria-expanded')).toBe('true');
    expect(root.querySelector('[data-section-content="inscripcion-academica"]')?.textContent)
      .toContain('Pendiente de migración');
    toggles[0].click();
    await settle();
    expect(toggles[0].getAttribute('aria-expanded')).toBe('false');
  });

  it('muestra un flyout navegable al pulsar Postgrado en modo colapsado', async () => {
    fixture.componentRef.setInput('collapsed', true);
    await settle();
    fixture.nativeElement.querySelector('[data-section="postgrado"]').click();
    await settle();
    const menu = document.querySelector<HTMLElement>('.dropdown-menu.show')!;
    expect(menu.querySelectorAll('a')).toHaveLength(31);
    expect(menu.textContent).toContain(POSTGRADUATE_REPORTS[0].title);
    menu.querySelector<HTMLAnchorElement>('a')!.click();
    await settle();
    expect(TestBed.inject(Router).url).toBe('/postgrado/' + POSTGRADUATE_REPORTS[0].slug);
    expect(document.querySelector('.dropdown-menu.show')).toBeNull();
  });

  it('busca secciones y opciones y avisa cuando no hay coincidencias', async () => {
    const root: HTMLElement = fixture.nativeElement;
    const search = root.querySelector<HTMLInputElement>('.sidebar-search input')!;
    search.value = 'tesis';
    search.dispatchEvent(new Event('input'));
    await settle();
    const sections = [...root.querySelectorAll<HTMLElement>('[data-section]')]
      .map(button => button.getAttribute('aria-label'));
    expect(sections).toEqual(['Postgrado']);
    root.querySelector<HTMLButtonElement>('[data-section="postgrado"]')!.click();
    await settle();
    const titles = [...root.querySelectorAll('.sidebar-submenu .sidebar-report')]
      .map(link => link.textContent?.trim());
    expect(titles).toEqual(['Defensas de tesis Postgrado', 'Registro de tesis']);

    search.value = 'zzzz';
    search.dispatchEvent(new Event('input'));
    await settle();
    expect(root.textContent).toContain('No se encontraron opciones');
  });

  it('marca favoritos, los muestra arriba y los persiste con clave propia', async () => {
    const root: HTMLElement = fixture.nativeElement;
    root.querySelector<HTMLButtonElement>('[data-section="postgrado"]')!.click();
    await settle();
    const star = root.querySelector<HTMLButtonElement>(
      '.sidebar-submenu .sidebar-report-row .sidebar-favorite-toggle')!;
    expect(star.getAttribute('aria-label')).toBe('Agregar a favoritos');
    star.click();
    await settle();
    expect(star.getAttribute('aria-label')).toBe('Quitar de favoritos');
    const favorites = root.querySelector<HTMLElement>('[data-favorites]')!;
    expect(favorites.textContent).toContain('Favoritos');
    expect(favorites.textContent).toContain('Encuesta docente por curso');
    expect(JSON.parse(localStorage.getItem('sadinformes.sidebar.favorite-routes')!))
      .toEqual(['encuesta-docente-por-curso']);
    expect(localStorage.getItem('students.detail.favorite-routes')).toBeNull();

    favorites.querySelector<HTMLButtonElement>('.sidebar-favorite-toggle')!.click();
    await settle();
    expect(root.querySelector('[data-favorites]')).toBeNull();
    expect(JSON.parse(localStorage.getItem('sadinformes.sidebar.favorite-routes')!)).toEqual([]);
  });

  it('recupera favoritos guardados al iniciar', async () => {
    localStorage.setItem('sadinformes.sidebar.favorite-routes', JSON.stringify(['registro-tesis']));
    fixture.destroy();
    fixture = TestBed.createComponent(ReportsSidebar);
    fixture.detectChanges();
    await settle();
    const favorites = (fixture.nativeElement as HTMLElement)
      .querySelector<HTMLElement>('[data-favorites]');
    expect(favorites?.textContent).toContain('Registro de tesis');
  });

  it('abre un solo panel, enfoca cierre y Escape lo cierra', async () => {
    const trigger = document.createElement('button');
    document.body.append(trigger);
    try {
      trigger.focus();
      fixture.componentInstance.openMobile();
      fixture.componentInstance.openMobile();
      await settle();
      const dialog = document.querySelector<HTMLElement>('[role="dialog"]');
      expect(document.querySelectorAll('[role="dialog"]').length).toBe(1);
      expect(dialog?.getAttribute('aria-labelledby')).toBe('reports-drawer-title');
      expect(dialog?.textContent).toContain('Informes');
      expect(fixture.componentInstance.mobileOpen()).toBe(true);
      expect(document.activeElement).toBe(dialog?.querySelector('[aria-label="Cerrar menú"]'));
      dialog?.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
      await new Promise<void>((resolve) => requestAnimationFrame(() => resolve()));
      await settle();
      expect(fixture.componentInstance.mobileOpen()).toBe(false);
      expect(document.querySelector('[role="dialog"]')).toBeNull();
      expect(document.activeElement).toBe(trigger);
    } finally { trigger.remove(); }
  });

  it('cierra el panel mediante el enlace, botón, backdrop y navegación', async () => {
    const open = async () => {
      fixture.componentInstance.openMobile(); await settle();
      expect(fixture.componentInstance.mobileOpen()).toBe(true);
      expect(document.querySelector('[role="dialog"]')).not.toBeNull();
    };
    await open();
    document.querySelector<HTMLButtonElement>('[role="dialog"] [data-section="postgrado"]')!.click();
    await settle();
    document.querySelector<HTMLAnchorElement>('[role="dialog"] a[href="/postgrado/encuesta-docente-por-curso"]')!.click();
    await settle();
    expect(fixture.componentInstance.mobileOpen()).toBe(false);
    await open();
    document.querySelector<HTMLButtonElement>('[aria-label="Cerrar menú"]')?.click();
    await settle();
    expect(fixture.componentInstance.mobileOpen()).toBe(false);
    await open();
    document.querySelector<HTMLElement>('ngb-offcanvas-backdrop')?.dispatchEvent(
      new MouseEvent('mousedown', { bubbles: true }),
    );
    await settle();
    expect(fixture.componentInstance.mobileOpen()).toBe(false);
    await open();
    await TestBed.inject(Router).navigateByUrl('/otra');
    await settle();
    expect(fixture.componentInstance.mobileOpen()).toBe(false);
  });

  it('retira el panel al cambiar a escritorio y al destruir la vista', async () => {
    fixture.componentInstance.openMobile();
    await settle();
    expect(fixture.componentInstance.mobileOpen()).toBe(true);
    Object.defineProperty(window, 'innerWidth', { configurable: true, value: 992 });
    window.dispatchEvent(new Event('resize'));
    await settle();
    expect(fixture.componentInstance.mobileOpen()).toBe(false);
    Object.defineProperty(window, 'innerWidth', { configurable: true, value: 375 });
    fixture.componentInstance.openMobile();
    await settle();
    expect(fixture.componentInstance.mobileOpen()).toBe(true);
    fixture.destroy();
    await new Promise<void>((resolve) => setTimeout(resolve, 0));
    expect(document.querySelector('[role="dialog"]')).toBeNull();
    expect(document.querySelector('ngb-offcanvas-backdrop')).toBeNull();
  });
});
