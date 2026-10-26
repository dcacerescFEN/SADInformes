import { ChangeDetectionStrategy, Component, inject, signal, viewChild } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AuthService } from './core/auth/auth-service';
import { Header } from './core/layout/header';
import { ReportsSidebar } from './core/layout/reports-sidebar';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Header, ReportsSidebar],
  template: `
    <div class="layout-shell">
      @if (auth.isAuthenticated()) {
        <app-header [showSidebarToggle]="true" [sidebarCollapsed]="sidebarCollapsed()"
          [sidebarMobileOpen]="sidebar()?.mobileOpen() ?? false" (sidebarToggle)="toggleSidebar($event)" />
      }
      <div class="layout-body">
        @if (auth.isAuthenticated()) { <app-reports-sidebar [collapsed]="sidebarCollapsed()" /> }
        <main class="layout-content" aria-label="Contenido de informes"><router-outlet /></main>
      </div>
    </div>
  `,
  styleUrl: './app.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {
  readonly auth = inject(AuthService);
  protected readonly sidebarCollapsed = signal(false);
  protected readonly sidebar = viewChild(ReportsSidebar);

  protected toggleSidebar(mobile: boolean): void {
    if (mobile) this.sidebar()?.openMobile();
    else this.sidebarCollapsed.update(value => !value);
  }
}
