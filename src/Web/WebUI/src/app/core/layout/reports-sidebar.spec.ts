import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { NgbConfig } from '@ng-bootstrap/ng-bootstrap';
import { AuthService } from '../auth/auth-service';
import { ReportsSidebar } from './reports-sidebar';

@Component({ template: '' })
class TestPage {}

describe('Sidebar de Informes', () => {
  let fixture: ComponentFixture<ReportsSidebar>;
  let originalWidth: PropertyDescriptor;
  beforeEach(async () => {
    originalWidth = Object.getOwnPropertyDescriptor(window, 'innerWidth')!;
    Object.defineProperty(window, 'innerWidth', { configurable: true, writable: true, value: 375 });
    await TestBed.configureTestingModule({ imports: [ReportsSidebar], providers: [
      provideRouter([{ path: '', component: TestPage }, { path: 'otra', component: TestPage }]),
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

  it('muestra solo Inicio, perfil y estado colapsado, sin opciones administrativas', () => {
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('#reports-sidebar')?.getAttribute('aria-label')).toBe('Informes');
    expect(root.querySelectorAll('.sidebar-navigation a').length).toBe(1);
    expect(root.querySelector('.sidebar-navigation a')?.textContent).toContain('Inicio');
    expect(root.querySelector('.sidebar-navigation a')?.getAttribute('aria-current')).toBe('page');
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
    document.querySelector<HTMLAnchorElement>('[role="dialog"] .sidebar-navigation a')?.click();
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
