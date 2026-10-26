import { ChangeDetectionStrategy, Component, OnInit, computed, inject, input, output, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import {
  NgbCollapseModule, NgbDropdown, NgbDropdownToggle, NgbDropdownMenu, NgbDropdownItem, NgbTooltip,
} from '@ng-bootstrap/ng-bootstrap';
import { AuthService } from '../auth/auth-service';
import { NavigationService } from '../navigation/navigation-service';
import { ThemeStore } from './theme-store';

@Component({
  selector: 'app-header',
  imports: [RouterLink, NgbCollapseModule, NgbDropdown, NgbDropdownToggle, NgbDropdownMenu, NgbDropdownItem, NgbTooltip],
  template: `
    <nav class="navbar navbar-expand-lg navbar-dark shadow-sm nav-header" aria-label="Navegación principal">
      <div class="container-fluid">
        @if (showSidebarToggle()) {
          <button class="header-toggle-btn d-none d-lg-inline-flex me-2" type="button"
            data-sidebar-mode="desktop" aria-label="Alternar menú de informes" aria-controls="reports-sidebar"
            [attr.aria-expanded]="!sidebarCollapsed()" (click)="sidebarToggle.emit(false)">
            <i class="bi bi-list fs-4" aria-hidden="true"></i>
          </button>
          <button class="header-toggle-btn d-lg-none" type="button" data-sidebar-mode="mobile"
            aria-label="Abrir menú de informes" aria-haspopup="dialog" [attr.aria-expanded]="sidebarMobileOpen()"
            (click)="sidebarToggle.emit(true)">
            <i class="bi bi-list fs-4" aria-hidden="true"></i>
          </button>
        }
        <a class="navbar-brand" routerLink="/" aria-label="Inicio de SADInformes">
          <img src="/logo-fen-white-letters.svg" height="28" alt="FEN Universidad de Chile" class="mx-auto">
        </a>
        <button class="navbar-toggler" type="button" aria-label="Alternar navegación"
          aria-controls="main-navigation" [attr.aria-expanded]="!isMenuCollapsed"
          (click)="isMenuCollapsed = !isMenuCollapsed">
          <span class="navbar-toggler-icon"></span>
        </button>
        <div id="main-navigation" class="collapse navbar-collapse" [ngbCollapse]="isMenuCollapsed">
          <ul class="navbar-nav me-auto">
            @for (item of navigation.links(); track item.key) {
              <li class="nav-item">
                <a class="nav-link header-nav-link" [attr.aria-label]="item.label" [attr.href]="item.url"
                  [class.active]="item.key === 'reports'" [attr.aria-current]="item.key === 'reports' ? 'page' : null"
                  [attr.aria-disabled]="item.url ? null : 'true'" [attr.tabindex]="item.url ? null : -1"
                  (click)="isMenuCollapsed = true">
                  <i [class]="'bi ' + item.icon" aria-hidden="true"></i><span>{{ item.label }}</span>
                </a>
              </li>
            }
          </ul>
          <div class="header-actions d-flex align-items-center">
            <div ngbDropdown class="me-3">
              <button id="themeMenu" class="header-icon-btn dropdown-toggle" type="button" ngbDropdownToggle aria-label="Cambiar tema">
                <i [class]="'bi fs-5 ' + themeIcon()" aria-hidden="true"></i>
              </button>
              <div class="dropdown-menu dropdown-menu-end" ngbDropdownMenu aria-labelledby="themeMenu">
                @for (choice of themes; track choice[0]) {
                  <button ngbDropdownItem type="button" [attr.data-theme-choice]="choice[0]"
                    [attr.aria-pressed]="theme.preference() === choice[0]" (click)="theme.setPreference(choice[0])">
                    <i [class]="'bi me-2 ' + choice[2]" aria-hidden="true"></i>{{ choice[1] }}
                    @if (theme.preference() === choice[0]) { <i class="bi bi-check2 ms-auto" aria-hidden="true"></i> }
                  </button>
                }
              </div>
            </div>
            <a class="text-white me-3" href="http://g.fen.uchile.cl" target="_blank" ngbTooltip="Correo FEN"
              rel="noopener noreferrer" aria-label="Correo FEN">
              <i class="bi bi-envelope fs-5" aria-hidden="true"></i>
            </a>
            <div ngbDropdown class="me-3">
              <button id="userMenu" class="header-icon-btn dropdown-toggle" type="button" ngbDropdownToggle aria-label="Menú de usuario">
                <i class="bi bi-person-circle fs-5" aria-hidden="true"></i>
              </button>
              <div class="dropdown-menu dropdown-menu-end" ngbDropdownMenu aria-labelledby="userMenu">
                <a ngbDropdownItem href="https://cuenta.fen.uchile.cl" target="_blank" rel="noopener noreferrer">
                  <i class="bi bi-key-fill me-2" aria-hidden="true"></i>Cambiar Contraseña
                </a>
                <button ngbDropdownItem type="button" data-action="logout" (click)="logout()">
                  <i class="bi bi-box-arrow-right me-2" aria-hidden="true"></i>Cerrar Sesión
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </nav>
    @if (navigation.error()) { <p class="navigation-status" role="status">{{ navigation.error() }}</p> }
    @if (actionError()) { <p class="navigation-status" role="alert">{{ actionError() }}</p> }
  `,
  styleUrl: './header.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Header implements OnInit {
  readonly showSidebarToggle = input(false);
  readonly sidebarCollapsed = input(false);
  readonly sidebarMobileOpen = input(false);
  readonly sidebarToggle = output<boolean>();
  readonly auth = inject(AuthService);
  readonly navigation = inject(NavigationService);
  readonly theme = inject(ThemeStore);
  readonly actionError = signal('');
  readonly themes = [
    ['light', 'Claro', 'bi-sun-fill'], ['dark', 'Oscuro', 'bi-moon-stars-fill'], ['auto', 'Auto', 'bi-circle-half'],
  ] as const;
  readonly themeIcon = computed(() => this.theme.preference() === 'auto'
    ? 'bi-circle-half' : this.theme.resolvedTheme() === 'dark' ? 'bi-moon-stars-fill' : 'bi-sun-fill');
  isMenuCollapsed = true;

  ngOnInit(): void { void this.navigation.load(); }

  async logout(): Promise<void> {
    this.actionError.set('');
    try { await this.auth.logout(); }
    catch { this.actionError.set('No se pudo cerrar sesión. Intenta nuevamente.'); }
  }
}
