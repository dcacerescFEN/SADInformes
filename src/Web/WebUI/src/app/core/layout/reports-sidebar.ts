import { NgTemplateOutlet } from '@angular/common';
import {
  ChangeDetectionStrategy, Component, DestroyRef, TemplateRef, computed, inject, input, signal, viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterLinkActive } from '@angular/router';
import { NgbDropdownModule, NgbOffcanvas, NgbOffcanvasRef } from '@ng-bootstrap/ng-bootstrap';
import { AuthService } from '../auth/auth-service';
import { REPORT_SECTIONS, ReportSection } from './report-sections';

@Component({
  selector: 'app-reports-sidebar',
  imports: [NgTemplateOutlet, RouterLink, RouterLinkActive, NgbDropdownModule],
  host: { '[class.collapsed]': 'collapsed()', '(window:resize)': 'closeDrawerOnDesktop()' },
  template: `
    <aside id="reports-sidebar" class="sidebar" [class.sidebar--collapsed]="collapsed()" aria-label="Informes">
      <ng-container [ngTemplateOutlet]="navigation" />
    </aside>
    <ng-template #drawer let-panel>
      <div class="offcanvas-header">
        <h2 id="reports-drawer-title" class="offcanvas-title fs-5">Informes</h2>
        <button type="button" class="btn-close" aria-label="Cerrar menú" ngbAutofocus (click)="panel.dismiss()"></button>
      </div>
      <div class="sidebar sidebar--drawer">
        <ng-container [ngTemplateOutlet]="navigation" [ngTemplateOutletContext]="{ mobile: true }" />
      </div>
    </ng-template>
    <ng-template #navigation let-mobile="mobile">
      <div class="sidebar-brand" title="SAD Informes">
        <i class="bi bi-file-earmark-bar-graph" aria-hidden="true"></i>
        <span class="sidebar-label">SAD Informes</span>
      </div>
      <nav class="sidebar-navigation" aria-label="Opciones de informes">
        <a class="sidebar-link" routerLink="/" routerLinkActive="active"
          [routerLinkActiveOptions]="{ exact: true }" ariaCurrentWhenActive="page"
          title="Inicio" aria-label="Inicio" (click)="closeMobile()">
          <i class="bi bi-house-door" aria-hidden="true"></i><span class="sidebar-label">Inicio</span>
        </a>
        @for (section of sections; track section.id) {
          @if (collapsed() && !mobile) {
            <div ngbDropdown placement="end-top" container="body" class="sidebar-group" #menu="ngbDropdown">
              <button ngbDropdownToggle type="button" class="sidebar-link sidebar-section-toggle"
                [class.active]="isActive(section)" [attr.data-section]="section.id"
                [title]="section.title" [attr.aria-label]="section.title">
                <i class="bi" [class]="'bi ' + section.icon" aria-hidden="true"></i>
              </button>
              <div ngbDropdownMenu class="reports-flyout" [attr.aria-label]="section.title">
                <h2 class="h6 px-3 pt-2">{{ section.title }}</h2>
                <ng-container [ngTemplateOutlet]="sectionItems"
                  [ngTemplateOutletContext]="{ $implicit: section, menu: menu }" />
              </div>
            </div>
          } @else {
            <div class="sidebar-group">
              <button type="button" class="sidebar-link sidebar-section-toggle"
                [class.active]="isActive(section)" [attr.data-section]="section.id"
                [attr.aria-label]="section.title" [title]="section.title"
                [attr.aria-expanded]="expanded().includes(section.id)" (click)="toggleSection(section.id)">
                <i [class]="'bi ' + section.icon" aria-hidden="true"></i>
                <span>{{ section.title }}</span>
                <i class="bi sidebar-chevron" [class.bi-chevron-down]="expanded().includes(section.id)"
                  [class.bi-chevron-right]="!expanded().includes(section.id)" aria-hidden="true"></i>
              </button>
              @if (expanded().includes(section.id)) {
                <div class="sidebar-submenu" [attr.data-section-content]="section.id">
                  <ng-container [ngTemplateOutlet]="sectionItems" [ngTemplateOutletContext]="{ $implicit: section }" />
                </div>
              }
            </div>
          }
        }
      </nav>
      <div class="sidebar-profile" [title]="username()">
        <span class="sidebar-avatar" aria-hidden="true">{{ initials() }}</span>
        <div class="sidebar-label sidebar-profile-details">
          <span class="sidebar-username">{{ username() }}</span>
          <span class="sidebar-name">{{ name() }}</span>
        </div>
      </div>
    </ng-template>
    <ng-template #sectionItems let-section let-menu="menu">
      @for (report of section.reports; track report.slug) {
        <a ngbDropdownItem class="sidebar-link sidebar-report" [routerLink]="['/postgrado', report.slug]"
          routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }"
          ariaCurrentWhenActive="page" [title]="report.title" [attr.aria-label]="report.title"
          (click)="menu?.close(); closeMobile()">{{ report.title }}</a>
      } @empty {
        <p class="sidebar-pending text-body-secondary px-3 py-2 mb-0">Pendiente de migración</p>
      }
    </ng-template>
  `,
  styleUrl: './reports-sidebar.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReportsSidebar {
  protected readonly sections = REPORT_SECTIONS;
  protected readonly expanded = signal<string[]>([]);
  private readonly currentUrl = signal('');
  readonly collapsed = input(false);
  private readonly open = signal(false);
  readonly mobileOpen = this.open.asReadonly();
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly offcanvas = inject(NgbOffcanvas);
  private readonly drawer = viewChild.required<TemplateRef<unknown>>('drawer');
  private drawerRef?: NgbOffcanvasRef;
  protected readonly username = computed(() => this.auth.username()?.trim() || 'Usuario');
  protected readonly name = computed(() => this.auth.name()?.trim() || this.username());
  protected readonly initials = computed(() => this.username().slice(0, 2).toUpperCase());

  constructor() {
    this.updateNavigation(this.router.url);
    this.router.events.pipe(takeUntilDestroyed()).subscribe(event => {
      if (event instanceof NavigationEnd) {
        this.updateNavigation(event.urlAfterRedirects);
        this.closeMobile();
      }
    });
    inject(DestroyRef).onDestroy(() => this.closeMobile());
  }

  protected toggleSection(id: string): void {
    this.expanded.update(ids => ids.includes(id) ? ids.filter(value => value !== id) : [...ids, id]);
  }

  protected isActive(section: ReportSection): boolean {
    return section.id === 'postgrado' && this.currentUrl().startsWith('/postgrado/');
  }

  private updateNavigation(url: string): void {
    this.currentUrl.set(url);
    if (url.startsWith('/postgrado/') && !this.expanded().includes('postgrado')) {
      this.expanded.update(ids => [...ids, 'postgrado']);
    }
  }

  openMobile(): void {
    if (this.drawerRef) return;
    this.drawerRef = this.offcanvas.open(this.drawer(), {
      ariaLabelledBy: 'reports-drawer-title', position: 'start', keyboard: true, backdrop: true,
    });
    this.open.set(true);
    const reset = () => { this.drawerRef = undefined; this.open.set(false); };
    void this.drawerRef.result.then(reset, reset);
  }

  closeMobile(): void { this.drawerRef?.close(); }

  protected closeDrawerOnDesktop(): void {
    if (window.innerWidth >= 992) this.closeMobile();
  }
}
