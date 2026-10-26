import { NgTemplateOutlet } from '@angular/common';
import {
  ChangeDetectionStrategy, Component, DestroyRef, TemplateRef, computed, inject, input, signal, viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterLinkActive } from '@angular/router';
import { NgbOffcanvas, NgbOffcanvasRef } from '@ng-bootstrap/ng-bootstrap';
import { AuthService } from '../auth/auth-service';

@Component({
  selector: 'app-reports-sidebar',
  imports: [NgTemplateOutlet, RouterLink, RouterLinkActive],
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
        <ng-container [ngTemplateOutlet]="navigation" />
      </div>
    </ng-template>
    <ng-template #navigation>
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
      </nav>
      <div class="sidebar-profile" [title]="username()">
        <span class="sidebar-avatar" aria-hidden="true">{{ initials() }}</span>
        <div class="sidebar-label sidebar-profile-details">
          <span class="sidebar-username">{{ username() }}</span>
          <span class="sidebar-name">{{ name() }}</span>
        </div>
      </div>
    </ng-template>
  `,
  styleUrl: './reports-sidebar.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReportsSidebar {
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
    this.router.events.pipe(takeUntilDestroyed()).subscribe(event => {
      if (event instanceof NavigationEnd) this.closeMobile();
    });
    inject(DestroyRef).onDestroy(() => this.closeMobile());
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
