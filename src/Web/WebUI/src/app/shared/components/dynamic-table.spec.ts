import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { DynamicTable } from './dynamic-table';
import { TableColumn } from './table';

const columns: readonly TableColumn[] = [
  { key: 'name', name: 'Nombre' },
  { key: 'rut', name: 'Rut' },
];

@Component({
  imports: [DynamicTable],
  template: `
    <app-dynamic-table
      mode="server"
      [data]="rows"
      [columns]="columns"
      [page]="page"
      [pageSize]="10"
      [hasMore]="hasMore"
      [total]="null"
      (pageChange)="requested = $event" />
  `,
})
class ServerHost {
  readonly columns = columns;
  rows = [{ name: 'Ana', rut: '1' }, { name: 'Luis', rut: '2' }];
  page = 1;
  hasMore = true;
  requested = 0;
}

describe('DynamicTable server mode', () => {
  it('renders only the received page and emits page changes without local slicing', () => {
    const fixture = TestBed.createComponent(ServerHost);
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelectorAll('tbody tr')).toHaveLength(2);
    expect(root.textContent).toContain('Mostrando 2 registros');
    expect(root.textContent).toContain('Página 1');
    expect(root.textContent).not.toContain('...');

    const next = Array.from(root.querySelectorAll<HTMLButtonElement>('button.page-link'))
      .find(button => button.textContent?.includes('Siguiente'));
    next?.click();
    expect(fixture.componentInstance.requested).toBe(2);
  });

  it('disables Siguiente when the server reports no more rows', () => {
    const fixture = TestBed.createComponent(ServerHost);
    fixture.componentInstance.hasMore = false;
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;
    const next = Array.from(root.querySelectorAll<HTMLButtonElement>('button.page-link'))
      .find(button => button.textContent?.includes('Siguiente'));
    expect(next?.disabled).toBe(true);
  });
});
