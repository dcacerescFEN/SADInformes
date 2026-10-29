import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';
import { Search } from '../../shared/components/search';

export interface InsatisfactionRow { professor: string; count: number; professorCode: string; period: string; studentType: string; modality: string; }
export interface TeacherDetail { professor: string; motives: string[]; }
const PAGE_SIZE = 10;

@Component({
  selector: 'app-insatisfaction',
  imports: [Search],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <p class="text-body-secondary">¿Tuvo profesores que lo dejaron insatisfecho? Indique el profesor para revisar los motivos.</p>
    @if (rows().length > 0) {
      <div class="d-flex justify-content-end mb-3"><div class="search-box"><app-search (search)="search($event)" /></div></div>
      @if (filteredRows().length > 0) {
      <div class="table-responsive">
        <table class="table table-hover align-middle app-table" aria-label="Profesores con insatisfacción">
          <thead class="table-light"><tr><th>Profesor</th><th class="text-center">Cantidad</th><th class="text-center">Detalle</th></tr></thead>
          <tbody>@for (row of pagedRows(); track row.professorCode) {
            <tr><td>{{ row.professor }}</td><td class="text-center">{{ row.count }}</td>
              <td class="text-center">
                <button type="button" class="btn-action" [attr.data-detail]="row.professorCode"
                  [attr.title]="'Ver motivos de ' + row.professor" [attr.aria-label]="'Ver motivos de ' + row.professor"
                  (click)="openDetail(row)">
                  <i class="bi bi-eye" aria-hidden="true"></i>
                </button>
              </td></tr>
          }</tbody>
        </table>
      </div>
      <div class="d-flex flex-wrap justify-content-between align-items-center gap-2">
        <small class="text-body-secondary">Mostrando {{ firstItem() }}–{{ lastItem() }} de {{ filteredRows().length }}</small>
        @if (totalPages() > 1) {
          <nav aria-label="Paginación de profesores insatisfechos">
            <ul class="pagination pagination-sm mb-0">
              <li class="page-item" [class.disabled]="currentPage() === 1"><button type="button" class="page-link" (click)="setPage(currentPage() - 1)">Anterior</button></li>
              @for (number of pageNumbers(); track number) {
                <li class="page-item" [class.active]="currentPage() === number"><button type="button" class="page-link" (click)="setPage(number)">{{ number }}</button></li>
              }
              <li class="page-item" [class.disabled]="currentPage() === totalPages()"><button type="button" class="page-link" (click)="setPage(currentPage() + 1)">Siguiente</button></li>
            </ul>
          </nav>
        }
      </div>
      } @else {
        <p class="text-body-secondary" role="status">No se encontraron registros.</p>
      }
    } @else {
      <p class="text-body-secondary" role="status">No existen datos disponibles.</p>
    }
    @if (modalOpen()) {
      <div class="modal fade show d-block" tabindex="-1" role="dialog" aria-modal="true" aria-labelledby="teacher-detail-title" (click)="closeDetail()">
        <div class="modal-dialog modal-lg modal-dialog-centered modal-dialog-scrollable" (click)="$event.stopPropagation()">
          <div class="modal-content">
            <div class="modal-header">
              <h3 class="modal-title h5" id="teacher-detail-title">Motivos de {{ selectedProfessor() }}</h3>
              <button type="button" class="btn-close" aria-label="Cerrar detalle" (click)="closeDetail()"></button>
            </div>
            <div class="modal-body" data-teacher-detail>
              @if (detailLoading()) { <p role="status">Cargando motivos…</p> }
              @if (detailError()) { <div class="alert alert-warning" role="alert">{{ detailError() }}</div> }
              @if (detail(); as value) {
                @if (value.motives.length > 0) {
                  <ul class="mb-0">@for (motive of value.motives; track $index) {<li>{{ motive }}</li>}</ul>
                } @else {
                  <p class="text-body-secondary mb-0" role="status">Sin motivos registrados.</p>
                }
              }
            </div>
            <div class="modal-footer"><button type="button" class="btn btn-secondary" (click)="closeDetail()">Cerrar</button></div>
          </div>
        </div>
      </div>
      <div class="modal-backdrop fade show"></div>
    }
  `,
  styles: `.search-box { width: 20rem; max-width: 100%; }`,
})
export class Insatisfaction {
  readonly rows = input.required<InsatisfactionRow[]>();
  readonly detail = input<TeacherDetail | null>(null);
  readonly detailLoading = input(false);
  readonly detailError = input('');
  readonly showDetail = output<InsatisfactionRow>();
  protected readonly searchTerm = signal('');
  protected readonly page = signal(1);
  protected readonly filteredRows = computed(() => {
    const term = this.searchTerm().trim().toLowerCase();
    if (!term) return this.rows();
    return this.rows().filter(row => row.professor.toLowerCase().includes(term)
      || row.professorCode.toLowerCase().includes(term) || String(row.count).includes(term));
  });
  protected readonly modalOpen = signal(false);
  protected readonly selectedProfessor = signal('');
  protected readonly totalPages = computed(() => Math.max(1, Math.ceil(this.filteredRows().length / PAGE_SIZE)));
  protected readonly currentPage = computed(() => Math.min(this.page(), this.totalPages()));
  protected readonly pagedRows = computed(() => {
    const start = (this.currentPage() - 1) * PAGE_SIZE;
    return this.filteredRows().slice(start, start + PAGE_SIZE);
  });
  protected readonly pageNumbers = computed(() => Array.from({ length: this.totalPages() }, (_, index) => index + 1));
  protected readonly firstItem = computed(() => this.filteredRows().length === 0 ? 0 : (this.currentPage() - 1) * PAGE_SIZE + 1);
  protected readonly lastItem = computed(() => Math.min(this.currentPage() * PAGE_SIZE, this.filteredRows().length));

  protected search(term: string): void {
    this.searchTerm.set(term);
    this.page.set(1);
  }

  protected setPage(page: number): void {
    this.page.set(Math.min(Math.max(page, 1), this.totalPages()));
  }

  protected openDetail(row: InsatisfactionRow): void {
    this.selectedProfessor.set(row.professor);
    this.modalOpen.set(true);
    this.showDetail.emit(row);
  }

  protected closeDetail(): void { this.modalOpen.set(false); }
}
