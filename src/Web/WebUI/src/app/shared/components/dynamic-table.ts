import { Component, input, output } from '@angular/core';
import { Table, type TableAction, type TableColumn } from './table';

/**
 * Tabla de informes con modo cliente o servidor. En modo servidor la página,
 * la búsqueda y el orden los resuelve la API; el componente solo renderiza.
 * Reutiliza `app-table` para conservar estructura, estilos, skeleton y estados.
 */
@Component({
  selector: 'app-dynamic-table',
  imports: [Table],
  template: `
    <app-table
      [data]="data()"
      [columns]="columns()"
      [actions]="actions()"
      [searchTerm]="searchTerm()"
      [showSummary]="showSummary()"
      [showRowNumber]="showRowNumber()"
      [loading]="loading()"
      [mode]="mode()"
      [total]="total()"
      [page]="page()"
      [pageSize]="pageSize()"
      [hasMore]="hasMore()"
      (pageChange)="pageChange.emit($event)"
      (sortChange)="sortChange.emit($event)"
    />
  `,
})
export class DynamicTable {
  readonly data = input<readonly object[]>([]);
  readonly columns = input<readonly TableColumn[]>([]);
  readonly actions = input<readonly TableAction[]>([]);
  readonly searchTerm = input<string>('');
  readonly showSummary = input<boolean>(true);
  readonly showRowNumber = input<boolean>(false);
  readonly loading = input<boolean>(false);
  readonly mode = input<'client' | 'server'>('client');
  readonly total = input<number | null>(null);
  readonly page = input<number>(1);
  readonly pageSize = input<number>(10);
  readonly hasMore = input<boolean>(false);
  readonly pageChange = output<number>();
  readonly sortChange = output<{ key: string; direction: 'asc' | 'desc' }>();
}
