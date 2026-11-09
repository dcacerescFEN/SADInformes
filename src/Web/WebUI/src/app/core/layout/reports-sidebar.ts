import { NgTemplateOutlet } from '@angular/common';
import {
  ChangeDetectionStrategy, Component, DestroyRef, TemplateRef, computed, effect, inject, input, signal, viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterLinkActive } from '@angular/router';
import { NgbOffcanvas, NgbOffcanvasRef } from '@ng-bootstrap/ng-bootstrap';
import { AuthService } from '../auth/auth-service';
import { REPORT_SECTIONS, ReportSection } from './report-sections';
import { LEGACY_POSTGRADUATE_SLUGS, POSTGRADUATE_REPORTS, PostgraduateReport } from '../../postgraduate/postgraduate-catalogue';
import { ACADEMIC_ENROLLMENT_REPORTS } from '../../academic-enrollment/academic-enrollment-catalogue';

const ALL_REPORTS = [...POSTGRADUATE_REPORTS, ...ACADEMIC_ENROLLMENT_REPORTS];

const FAVORITE_ROUTES_STORAGE_KEY = 'sadinformes.sidebar.favorite-routes';

interface FavoriteReport {
  readonly report: PostgraduateReport;
  readonly routePrefix: string;
}

export function migrateFavoriteSlugs(slugs: readonly string[]): string[] {
  const migrated = slugs.map(slug =>
    LEGACY_POSTGRADUATE_SLUGS[slug as keyof typeof LEGACY_POSTGRADUATE_SLUGS] ?? slug);
  return [...new Set(migrated)]
    .filter(slug => ALL_REPORTS.some(report => report.slug === slug));
}

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
        <ng-container [ngTemplateOutlet]="navigation" [ngTemplateOutletContext]="{ mobile: true }" />
      </div>
    </ng-template>
    <ng-template #navigation let-mobile="mobile">
      <div class="sidebar-brand" title="SAD Informes">
        <i class="bi bi-file-earmark-bar-graph" aria-hidden="true"></i>
        <span class="sidebar-label">SAD Informes</span>
      </div>
      <nav class="sidebar-navigation" aria-label="Opciones de informes">
        @if (!collapsed() || mobile) {
          <div class="sidebar-search">
            <i class="bi bi-search" aria-hidden="true"></i>
            <input #searchInput type="search" placeholder="Buscar opción o grupo…"
              aria-label="Buscar opción o grupo" [value]="searchQuery()"
              (input)="updateSearch(searchInput.value)" />
          </div>
        }
        @if (favoriteReports().length > 0 && (!collapsed() || mobile)) {
          <div class="sidebar-group sidebar-favorites" data-favorites>
            <div class="sidebar-section-label">Favoritos</div>
            @for (favorite of favoriteReports(); track favorite.report.slug) {
              <div class="sidebar-report-row">
                <a class="sidebar-link sidebar-report" [routerLink]="reportRoute(favorite.report, favorite.routePrefix)"
                  routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }"
                  ariaCurrentWhenActive="page" [title]="favorite.report.title" [attr.aria-label]="favorite.report.title"
                  (click)="closeMobile()">{{ favorite.report.title }}</a>
                <button class="sidebar-favorite-toggle is-active" type="button"
                  aria-label="Quitar de favoritos" (click)="toggleFavorite(favorite.report.slug)">
                  <i class="bi bi-star-fill" aria-hidden="true"></i>
                </button>
              </div>
            }
          </div>
        }
        @for (section of visibleSections(); track section.id) {
          @if (collapsed() && !mobile) {
            <div class="sidebar-group sidebar-collapsed-section" [attr.data-collapsed-section]="section.id"
              (mouseenter)="setCollapsedSectionHover(section.id)"
              (mouseleave)="clearCollapsedSectionHover()"
              (focusin)="setCollapsedSectionHover(section.id)"
              (focusout)="handleCollapsedSectionFocusOut($event)">
              <button type="button" class="sidebar-link sidebar-section-toggle sidebar-icon-button"
                [class.active]="isActive(section)" [class.is-open]="isCollapsedSectionOpen(section.id)"
                [attr.data-section]="section.id" [attr.aria-expanded]="isCollapsedSectionOpen(section.id)"
                aria-haspopup="menu" [title]="section.title" [attr.aria-label]="section.title">
                <i class="bi" [class]="'bi ' + section.icon" aria-hidden="true"></i>
              </button>
              @if (isCollapsedSectionOpen(section.id)) {
                <div class="reports-flyout" role="menu" [attr.aria-label]="section.title"
                  (mouseenter)="setCollapsedSectionHover(section.id)"
                  (mouseleave)="clearCollapsedSectionHover()"
                  (focusin)="setCollapsedSectionHover(section.id)"
                  (focusout)="handleCollapsedSectionFocusOut($event)">
                  <div class="reports-flyout__header">{{ section.title }}</div>
                  <div class="reports-flyout__items">
                    @for (report of section.reports; track report.slug) {
                      <a class="reports-flyout__item"
                        [routerLink]="reportRoute(report, section.routePrefix)"
                        routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }"
                        ariaCurrentWhenActive="page" [title]="report.title"
                        [attr.aria-label]="report.title"
                        (click)="closeCollapsedFlyout(); closeMobile()">{{ report.title }}</a>
                    } @empty {
                      <p class="reports-flyout__empty">Pendiente de migración</p>
                    }
                  </div>
                </div>
              }
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
      @if (visibleSections().length === 0 && favoriteReports().length === 0) {
        <div class="sidebar-no-results" role="status">
          <i class="bi bi-search" aria-hidden="true"></i>
          <span>No se encontraron opciones</span>
        </div>
      }
      <div class="sidebar-profile" [title]="username()">
        <span class="sidebar-avatar" aria-hidden="true">{{ initials() }}</span>
        <div class="sidebar-label sidebar-profile-details">
          <span class="sidebar-username">{{ username() }}</span>
          <span class="sidebar-name">{{ name() }}</span>
        </div>
      </div>
    </ng-template>
    <ng-template #sectionItems let-section>
      @for (report of section.reports; track report.slug) {
        <div class="sidebar-report-row">
          <a class="sidebar-link sidebar-report" [routerLink]="reportRoute(report, section.routePrefix)"
            routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }"
            ariaCurrentWhenActive="page" [title]="report.title" [attr.aria-label]="report.title"
            (click)="closeCollapsedFlyout(); closeMobile()">{{ report.title }}</a>
          <button class="sidebar-favorite-toggle" type="button"
            [class.is-active]="isFavorite(report.slug)"
            [attr.aria-label]="isFavorite(report.slug) ? 'Quitar de favoritos' : 'Agregar a favoritos'"
            (click)="toggleFavorite(report.slug)">
            <i class="bi" [class.bi-star-fill]="isFavorite(report.slug)"
              [class.bi-star]="!isFavorite(report.slug)" aria-hidden="true"></i>
          </button>
        </div>
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
  protected readonly searchQuery = signal('');
  protected readonly favorites = signal<string[]>(this.loadFavorites());
  private readonly currentUrl = signal('');
  readonly collapsed = input(false);
  private readonly open = signal(false);
  readonly mobileOpen = this.open.asReadonly();
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly offcanvas = inject(NgbOffcanvas);
  private readonly drawer = viewChild.required<TemplateRef<unknown>>('drawer');
  private drawerRef?: NgbOffcanvasRef;
  private hoverCloseTimeoutId: ReturnType<typeof setTimeout> | null = null;
  protected readonly hoveredCollapsedSection = signal<string | null>(null);
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
    effect(() => {
      const slugs = this.favorites();
      try {
        localStorage.setItem(FAVORITE_ROUTES_STORAGE_KEY, JSON.stringify(slugs));
      } catch {
        // Sin almacenamiento disponible los favoritos viven solo en la sesión.
      }
    });
    effect(() => {
      if (!this.collapsed()) {
        this.cancelPendingHoverClose();
        this.hoveredCollapsedSection.set(null);
      }
    });
    inject(DestroyRef).onDestroy(() => {
      this.closeMobile();
      this.cancelPendingHoverClose();
    });
  }

  protected updateSearch(query: string): void {
    this.searchQuery.set(query);
  }

  protected isFavorite(slug: string): boolean {
    return this.favorites().includes(slug);
  }

  protected toggleFavorite(slug: string): void {
    this.favorites.update(slugs =>
      slugs.includes(slug) ? slugs.filter(value => value !== slug) : [...slugs, slug]);
  }

  protected readonly visibleSections = computed<ReportSection[]>(() => {
    const query = this.searchQuery().trim().toLocaleLowerCase('es');
    if (!query) return [...this.sections];
    return this.sections.flatMap(section => {
      const sectionMatches = section.title.toLocaleLowerCase('es').includes(query);
      const reports = sectionMatches
        ? section.reports
        : section.reports.filter(report => report.title.toLocaleLowerCase('es').includes(query));
      return reports.length > 0 || (sectionMatches && section.reports.length === 0)
        ? [{ ...section, reports }]
        : [];
    });
  });

  protected readonly favoriteReports = computed<FavoriteReport[]>(() => {
    const query = this.searchQuery().trim().toLocaleLowerCase('es');
    const slugs = this.favorites();
    return this.sections.flatMap(section =>
      section.reports.map(report => ({ report, routePrefix: section.routePrefix ?? '/postgraduate' })))
      .filter(({ report }) => slugs.includes(report.slug))
      .filter(({ report }) => !query || report.title.toLocaleLowerCase('es').includes(query));
  });

  protected reportRoute(report: PostgraduateReport, routePrefix?: string): string {
    return report.routePath ?? `${routePrefix ?? '/postgraduate'}/${report.slug}`;
  }

  protected toggleSection(id: string): void {
    this.expanded.update(ids => ids.includes(id) ? ids.filter(value => value !== id) : [...ids, id]);
  }

  protected isActive(section: ReportSection): boolean {
    return !!section.routePrefix && this.currentUrl().startsWith(section.routePrefix + '/');
  }

  protected setCollapsedSectionHover(id: string): void {
    if (!this.collapsed()) return;
    this.cancelPendingHoverClose();
    this.hoveredCollapsedSection.set(id);
  }

  protected clearCollapsedSectionHover(): void {
    this.cancelPendingHoverClose();
    this.hoverCloseTimeoutId = setTimeout(() => {
      this.hoveredCollapsedSection.set(null);
      this.hoverCloseTimeoutId = null;
    }, 120);
  }

  protected handleCollapsedSectionFocusOut(event: FocusEvent): void {
    const nextTarget = event.relatedTarget;
    if (nextTarget instanceof Node && event.currentTarget instanceof Node && event.currentTarget.contains(nextTarget)) {
      return;
    }
    this.clearCollapsedSectionHover();
  }

  protected isCollapsedSectionOpen(id: string): boolean {
    return this.hoveredCollapsedSection() === id;
  }

  protected closeCollapsedFlyout(): void {
    this.cancelPendingHoverClose();
    this.hoveredCollapsedSection.set(null);
  }

  private cancelPendingHoverClose(): void {
    if (this.hoverCloseTimeoutId === null) return;
    clearTimeout(this.hoverCloseTimeoutId);
    this.hoverCloseTimeoutId = null;
  }

  private updateNavigation(url: string): void {
    this.currentUrl.set(url);
    const active = this.sections.find(section =>
      section.routePrefix && url.startsWith(section.routePrefix + '/'));
    if (active && !this.expanded().includes(active.id)) {
      this.expanded.update(ids => [...ids, active.id]);
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

  private loadFavorites(): string[] {
    try {
      const stored = localStorage.getItem(FAVORITE_ROUTES_STORAGE_KEY);
      const parsed: unknown = stored ? JSON.parse(stored) : [];
      if (!Array.isArray(parsed)) return [];
      const migrated = parsed
        .filter((slug): slug is string => typeof slug === 'string')
        .map(slug => LEGACY_POSTGRADUATE_SLUGS[slug as keyof typeof LEGACY_POSTGRADUATE_SLUGS] ?? slug);
      const favorites = [...new Set(migrated)]
        .filter(slug => ALL_REPORTS.some(report => report.slug === slug));
      if (stored && JSON.stringify(parsed) !== JSON.stringify(favorites))
        localStorage.setItem(FAVORITE_ROUTES_STORAGE_KEY, JSON.stringify(favorites));
      return favorites;
    } catch {
      return [];
    }
  }

  protected closeDrawerOnDesktop(): void {
    if (window.innerWidth >= 992) this.closeMobile();
  }
}
