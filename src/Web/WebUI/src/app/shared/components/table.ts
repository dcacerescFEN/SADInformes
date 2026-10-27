import { NgClass } from '@angular/common';
import { Component, effect, input, output, signal } from '@angular/core';

export interface TableColumn {
  key: string;
  name: string;
  searchable?: boolean;
  sortable?: boolean;
  multiline?: boolean;
  action?: TableAction;
  actionVisible?: (row: object) => boolean;
  actionDisabled?: (row: object) => boolean;
  additionalActions?: readonly TableAction[];
  alternateAction?: TableAction;
  alternateActionVisible?: (row: object) => boolean;
  badges?: boolean;
  emphasized?: boolean;
}

export interface TableAction {
  icon?: string;
  label: string | ((row: object) => string);
  actionKey: string;
  cssClass?: string;
  ariaLabel?: (row: object) => string;
}

@Component({
  selector: 'app-table',
  imports: [NgClass],
  template: `
    <div class="table-container">
      <div class="table-responsive">
        <table class="table table-hover align-middle app-table">
          <thead class="table-light">
            <tr>
              @if (selectionEnabled()) {
                <th>{{ selectionLabel() }}</th>
              }
              @if (showRowNumber()) {
                <th>#</th>
              }
              @for (column of columns(); track column) {
                <th [attr.aria-sort]="ariaSort(column)" [class.fw-bold]="column.emphasized">
                  @if (column.sortable) {
                    <button
                      class="btn btn-link p-0 text-reset text-decoration-none fw-bold"
                      type="button"
                      (click)="toggleSort(column)"
                    >
                      {{ column.name }}
                      <i
                        class="bi ms-1"
                        [ngClass]="
                          sortColumn() !== column.key
                            ? 'bi-arrow-down-up'
                            : sortDirection() === 'asc'
                              ? 'bi-caret-up-fill'
                              : 'bi-caret-down-fill'
                        "
                        aria-hidden="true"
                      ></i>
                    </button>
                  } @else if (column.emphasized) {
                    <span class="btn btn-link p-0 text-reset text-decoration-none fw-bold">{{
                      column.name
                    }}</span>
                  } @else {
                    {{ column.name }}
                  }
                </th>
              }
            </tr>
          </thead>
          <tbody>
            @if (getPaginatedData().length === 0) {
              <tr>
                <td
                  [attr.colspan]="
                    columns().length + (selectionEnabled() ? 1 : 0) + (showRowNumber() ? 1 : 0)
                  "
                  class="text-center empty-state py-4"
                >
                  <i class="bi bi-inbox me-2" style="font-size: 1.5rem;"></i>
                  No se encontraron registros
                </td>
              </tr>
            }

            @for (row of getPaginatedData(); track row; let i = $index) {
              <tr
                [class.row-clickable]="enableRowClick()"
                (click)="enableRowClick() && rowClick.emit(row)"
              >
                @if (selectionEnabled()) {
                  <td>
                    <input
                      class="form-check-input"
                      type="checkbox"
                      [checked]="isRowSelected(row)"
                      (click)="$event.stopPropagation()"
                      (change)="selectionChange.emit(row)"
                    />
                  </td>
                }
                @if (showRowNumber()) {
                  <td>{{ (currentPage() - 1) * itemsPerPage + i + 1 }}</td>
                }
                @for (column of columns(); track column) {
                  @if (column.key !== 'Actions') {
                    @if (column.additionalActions || column.alternateAction) {
                      <td class="table-action-cell">
                        @if (column.alternateAction && column.alternateActionVisible?.(row)) {
                          <button
                            class="btn"
                            type="button"
                            [ngClass]="column.alternateAction.cssClass"
                            [disabled]="isColumnActionDisabled(row, column)"
                            [attr.aria-label]="
                              column.alternateAction.ariaLabel?.(row) ??
                              actionLabel(column.alternateAction, row)
                            "
                            (click)="
                              $event.stopPropagation();
                              onAction(getRowId(row), column.alternateAction.actionKey)
                            "
                          >
                            @if (column.alternateAction.icon) {
                              <i
                                class="bi"
                                [ngClass]="column.alternateAction.icon"
                                aria-hidden="true"
                              ></i>
                            }
                            {{ actionLabel(column.alternateAction, row) }}
                          </button>
                        } @else if (isColumnActionVisible(row, column)) {
                          @if (column.action; as action) {
                            <button
                              class="btn"
                              type="button"
                              [ngClass]="action.cssClass"
                              [disabled]="isColumnActionDisabled(row, column)"
                              [attr.aria-label]="
                                action.ariaLabel?.(row) ?? actionLabel(action, row)
                              "
                              (click)="
                                $event.stopPropagation(); onAction(getRowId(row), action.actionKey)
                              "
                            >
                              @if (action.icon) {
                                <i class="bi" [ngClass]="action.icon" aria-hidden="true"></i>
                              }
                              {{ actionLabel(action, row) }}
                            </button>
                          }
                          @for (action of column.additionalActions ?? []; track action.actionKey) {
                            <button
                              class="btn ms-1"
                              type="button"
                              [ngClass]="action.cssClass"
                              [disabled]="isColumnActionDisabled(row, column)"
                              [attr.aria-label]="
                                action.ariaLabel?.(row) ?? actionLabel(action, row)
                              "
                              (click)="
                                $event.stopPropagation(); onAction(getRowId(row), action.actionKey)
                              "
                            >
                              @if (action.icon) {
                                <i class="bi" [ngClass]="action.icon" aria-hidden="true"></i>
                              }
                              {{ actionLabel(action, row) }}
                            </button>
                          }
                        }
                      </td>
                    } @else if (column.badges) {
                      <td>
                        @if (milestoneBadges(getCellValue(row, column.key)); as badges) {
                          @if (badges.user) {
                            <span
                              class="badge bg-primary-subtle text-primary-emphasis border border-primary-subtle"
                            >
                              {{ badges.user }}
                            </span>
                          }
                          @if (badges.date) {
                            <div>
                              <span
                                class="badge bg-info-subtle text-info-emphasis border border-info-subtle mt-1"
                              >
                                {{ badges.date }}
                              </span>
                            </div>
                          }
                        }
                        @if (column.action && isColumnActionVisible(row, column)) {
                          <button
                            class="btn mt-1"
                            type="button"
                            [ngClass]="column.action.cssClass"
                            [disabled]="isColumnActionDisabled(row, column)"
                            [attr.aria-label]="
                              column.action.ariaLabel?.(row) ?? actionLabel(column.action, row)
                            "
                            (click)="
                              $event.stopPropagation();
                              onAction(getRowId(row), column.action.actionKey)
                            "
                          >
                            @if (column.action.icon) {
                              <i class="bi" [ngClass]="column.action.icon" aria-hidden="true"></i>
                            }
                            {{ actionLabel(column.action, row) }}
                          </button>
                        }
                      </td>
                    } @else if (column.action && isColumnActionVisible(row, column)) {
                      <td class="table-action-cell">
                        <button
                          class="btn"
                          type="button"
                          [ngClass]="column.action.cssClass"
                          [disabled]="isColumnActionDisabled(row, column)"
                          [attr.aria-label]="
                            column.action.ariaLabel?.(row) ?? actionLabel(column.action, row)
                          "
                          (click)="
                            $event.stopPropagation();
                            onAction(getRowId(row), column.action.actionKey)
                          "
                        >
                          @if (column.action.icon) {
                            <i class="bi" [ngClass]="column.action.icon" aria-hidden="true"></i>
                          }
                          {{ actionLabel(column.action, row) }}
                        </button>
                      </td>
                    } @else if (getValueType(getCellValue(row, column.key)) === 'boolean') {
                      <td [class.table-cell-multiline]="column.multiline">
                        @if (getCellValue(row, column.key)) {
                          <i class="bi bi-check-circle-fill"></i>
                        } @else {
                          <i class="bi bi-x-circle-fill"></i>
                        }
                      </td>
                    } @else if (getValueType(getCellValue(row, column.key)) === 'array') {
                      <td>
                        <ul>
                          @for (item of $any(getCellValue(row, column.key)); track item) {
                            <li>{{ item }}</li>
                          }
                        </ul>
                      </td>
                    } @else if (getValueType(getCellValue(row, column.key)) === 'url') {
                      <td>
                        <img
                          [src]="getCellValue(row, column.key)"
                          style="width:40px;height:40px;object-fit:cover;border-radius:50%;"
                          alt="Foto"
                        />
                      </td>
                    } @else {
                      <td>
                        {{ getCellValue(row, column.key) }}
                      </td>
                    }
                  }
                }

                @if (actions().length > 0) {
                  <td>
                    @for (action of actions(); track $index) {
                      @if (action.actionKey !== 'toggle') {
                        <button
                          class="btn"
                          [ngClass]="action.cssClass"
                          (click)="onAction(getRowId(row), action.actionKey)"
                        >
                          <i class="bi" [ngClass]="action.icon"></i> {{ actionLabel(action, row) }}
                        </button>
                        <span>&nbsp;</span>
                      } @else {
                        <button
                          class="btn"
                          [ngClass]="isRowActive(row) ? 'btn-warning' : 'btn-danger'"
                          (click)="onAction(getRowId(row), action.actionKey)"
                        >
                          <i
                            class="bi"
                            [ngClass]="isRowActive(row) ? 'bi-pause-fill' : 'bi-play-fill'"
                          ></i>
                          {{ isRowActive(row) ? 'Pausar' : 'Activar' }}
                        </button>
                        <span>&nbsp;</span>
                      }
                    }
                  </td>
                }
              </tr>
            }
          </tbody>
        </table>
      </div>

      @if (showSummary() && data().length > 0) {
        <div class="d-flex justify-content-start">
          <div class="me-3">
            Mostrando {{ getPaginatedData().length }} de {{ getFilteredData().length }} registros
          </div>
        </div>
      }

      <nav aria-label="Page navigation example">
        <ul class="pagination justify-content-end">
          <li class="page-item" [ngClass]="{ disabled: currentPage() === 1 }">
            <button class="page-link" type="button" (click)="setPage(currentPage() - 1)">
              Anterior
            </button>
          </li>
          @for (page of range(); track page) {
            @if (page === '...') {
              <li class="page-item disabled">
                <span class="page-link">{{ page }}</span>
              </li>
            } @else {
              <li class="page-item" [ngClass]="{ active: currentPage() === page }">
                <button class="page-link" type="button" (click)="setPage(page)">{{ page }}</button>
              </li>
            }
          }
          <li class="page-item" [ngClass]="{ disabled: currentPage() === nPage() }">
            <button class="page-link" type="button" (click)="setPage(currentPage() + 1)">
              Siguiente
            </button>
          </li>
        </ul>
      </nav>
    </div>
  `,
  styles: `
    .table-cell-multiline {
      white-space: pre-line;
    }
    .table-action-cell {
      min-width: 6rem;
      text-align: center;
    }
  `,
})
export class Table {
  readonly data = input<readonly object[]>([]);
  readonly columns = input<readonly TableColumn[]>([]);
  readonly actions = input<readonly TableAction[]>([]);
  readonly searchTerm = input<string>('');
  readonly showSummary = input<boolean>(true);
  readonly showRowNumber = input<boolean>(true);
  readonly selectionEnabled = input<boolean>(false);
  readonly selectionLabel = input<string>('Seleccionar');
  readonly actionEvent = output<{
    id: string | number | null | undefined;
    actionKey: string;
  }>();
  readonly enableRowClick = input<boolean>(false);
  readonly rowClick = output<any>();
  readonly selectionChange = output<object>();
  public currentPage = signal(1);
  public readonly sortColumn = signal<string | null>(null);
  public readonly sortDirection = signal<'asc' | 'desc'>('asc');
  public readonly itemsPerPage = 10;
  public maxPagesToShow = 5; // Máximo de páginas a mostrar a la izquierda y derecha

  constructor() {
    effect(() => {
      this.data();
      this.searchTerm();
      this.currentPage.set(1);
    });
  }

  public setPage(pageNumber: number | string) {
    if (typeof pageNumber === 'string') {
      return;
    }
    this.currentPage.set(Math.min(Math.max(pageNumber, 1), this.nPage()));
  }

  public nPage() {
    const filteredData = this.getFilteredData();
    return Math.max(1, Math.ceil(filteredData.length / this.itemsPerPage));
  }

  public range() {
    const totalPages = this.nPage();
    const currentPage = this.currentPage();
    const maxPages = this.maxPagesToShow;
    const pages = [];

    let startPage: number;
    let endPage: number;

    if (totalPages <= maxPages) {
      // Si el número total de páginas es menor o igual al máximo, mostrar todas las páginas
      startPage = 1;
      endPage = totalPages;
    } else {
      // Calcular las páginas a mostrar alrededor de la página actual
      const halfMaxPages = Math.floor(maxPages / 2);

      if (currentPage <= halfMaxPages) {
        // Mostrar desde la primera página si la página actual está cerca del inicio
        startPage = 1;
        endPage = maxPages;
      } else if (currentPage + halfMaxPages >= totalPages) {
        // Mostrar hasta la última página si la página actual está cerca del final
        startPage = totalPages - maxPages + 1;
        endPage = totalPages;
      } else {
        // Mostrar páginas alrededor de la página actual
        startPage = currentPage - halfMaxPages;
        endPage = currentPage + halfMaxPages;
      }
    }

    // Agregar primera página y "..."
    if (startPage > 1) {
      pages.push(1);
      if (startPage > 2) {
        pages.push('...');
      }
    }

    // Agregar páginas dentro del rango calculado
    for (let i = startPage; i <= endPage; i++) {
      pages.push(i);
    }

    // Agregar última página y "..."
    if (endPage < totalPages) {
      if (endPage < totalPages - 1) {
        pages.push('...');
      }
      pages.push(totalPages);
    }

    return pages;
  }

  public getFilteredData() {
    const term = this.searchTerm().trim().toLowerCase();

    if (!term) {
      return [...this.data()];
    }

    return this.data().filter((item) =>
      this.columns().some(
        (column) =>
          column.key !== 'Actions' &&
          column.searchable !== false &&
          this.getSearchValue(this.getCellValue(item, column.key)).includes(term),
      ),
    );
  }

  public getPaginatedData() {
    const filteredData = this.getSortedData();
    const startIndex = (this.currentPage() - 1) * this.itemsPerPage;
    const endIndex = startIndex + this.itemsPerPage;
    return filteredData.slice(startIndex, endIndex);
  }

  public onAction(id: string | number | null | undefined, actionKey: string) {
    this.actionEvent.emit({ id, actionKey });
  }

  public toggleSort(column: TableColumn): void {
    if (!column.sortable) {
      return;
    }

    if (this.sortColumn() === column.key) {
      this.sortDirection.update((direction) => (direction === 'asc' ? 'desc' : 'asc'));
    } else {
      this.sortColumn.set(column.key);
      this.sortDirection.set('asc');
    }
    this.currentPage.set(1);
  }

  public ariaSort(column: TableColumn): 'ascending' | 'descending' | 'none' {
    if (!column.sortable || this.sortColumn() !== column.key) {
      return 'none';
    }

    return this.sortDirection() === 'asc' ? 'ascending' : 'descending';
  }

  public isColumnActionVisible(row: object, column: TableColumn): boolean {
    return column.actionVisible?.(row) ?? true;
  }

  public actionLabel(action: TableAction, row: object): string {
    return typeof action.label === 'function' ? action.label(row) : action.label;
  }

  public isColumnActionDisabled(row: object, column: TableColumn): boolean {
    return column.actionDisabled?.(row) ?? false;
  }

  public getCellValue(row: object, key: string): unknown {
    return (row as Record<string, unknown>)[key];
  }

  public milestoneBadges(value: unknown): { user: string; date: string } {
    if (typeof value !== 'string') return { user: '', date: '' };
    const [user = '', date = ''] = value.split('\n');
    const candidate = date.trim();
    const parts = /^(\d{2})\/(\d{2})\/(\d{4}) (\d{2}):(\d{2}):(\d{2})$/.exec(candidate);
    const validDate =
      parts !== null &&
      Number(parts[3]) >= 1 &&
      Number(parts[4]) <= 23 &&
      Number(parts[5]) <= 59 &&
      Number(parts[6]) <= 59 &&
      new Date(Date.UTC(Number(parts[3]), Number(parts[2]) - 1, Number(parts[1])))
        .toISOString()
        .slice(0, 10) === `${parts[3]}-${parts[2]}-${parts[1]}`;
    return {
      user: user.trim(),
      date: validDate ? candidate : '',
    };
  }

  private getSearchValue(value: unknown): string {
    if (typeof value === 'string' || typeof value === 'number') {
      return String(value).toLowerCase();
    }

    if (Array.isArray(value)) {
      return value
        .filter(
          (item): item is string | number => typeof item === 'string' || typeof item === 'number',
        )
        .join(' ')
        .toLowerCase();
    }

    return '';
  }

  public getRowId(row: object): string | number | null | undefined {
    const idKey = this.columns()[0]?.key;

    if (!idKey) {
      return undefined;
    }

    const value = this.getCellValue(row, idKey);

    if (typeof value === 'string' || typeof value === 'number' || value == null) {
      return value;
    }

    return undefined;
  }

  public isRowActive(row: object): boolean {
    return this.getCellValue(row, 'isActive') === true;
  }

  public isRowSelected(row: object): boolean {
    return this.getCellValue(row, 'selected') === true;
  }

  private getSortedData(): object[] {
    const column = this.sortColumn();
    if (!column) {
      return this.getFilteredData();
    }

    const direction = this.sortDirection() === 'asc' ? 1 : -1;
    return this.getFilteredData()
      .map((row, index) => ({ row, index }))
      .sort((left, right) => {
        const comparison = String(this.getCellValue(left.row, column) ?? '').localeCompare(
          String(this.getCellValue(right.row, column) ?? ''),
          undefined,
          { numeric: true, sensitivity: 'base' },
        );
        return comparison === 0 ? left.index - right.index : comparison * direction;
      })
      .map(({ row }) => row);
  }

  public getValueType(value: unknown): 'boolean' | 'array' | 'number' | 'url' | 'other' {
    if (typeof value === 'boolean') {
      return 'boolean';
    }

    if (Array.isArray(value)) {
      return 'array';
    }

    if (typeof value === 'number') {
      return 'number';
    }

    if (
      typeof value === 'string' &&
      (value.startsWith('https://') || value.startsWith('http://'))
    ) {
      return 'url';
    }

    return 'other';
  }
}
