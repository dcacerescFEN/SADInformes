import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';
import { Table, type TableColumn } from './table';

describe('Table sorting and filtering', () => {
  const columns: TableColumn[] = [
    { key: 'folio', name: 'FOLIO', sortable: true },
    { key: 'status', name: 'ESTADO', sortable: true },
    { key: 'action', name: 'Acción' },
  ];
  const rows = [
    { folio: '20', status: 'Vigente', action: 'Ver' },
    { folio: '3', status: 'Anulado', action: 'Ver' },
  ];

  it('filters rows and displays direction only on the active sortable header', async () => {
    const fixture = TestBed.createComponent(Table);
    const searchTerm = signal('');
    // Direct Vitest JIT does not extract input metadata; replace the input signals for this fixture.
    Object.defineProperties(fixture.componentInstance, {
      columns: { value: signal(columns) },
      data: { value: signal(rows) },
      showRowNumber: { value: signal(false) },
      searchTerm: { value: searchTerm },
    });
    await fixture.whenStable();

    const headers = () =>
      Array.from(fixture.nativeElement.querySelectorAll('thead th')) as HTMLElement[];
    const buttons = () =>
      Array.from(fixture.nativeElement.querySelectorAll('thead button')) as HTMLButtonElement[];
    const folios = () =>
      Array.from(
        fixture.nativeElement.querySelectorAll(
          'tbody tr td:first-child',
        ) as NodeListOf<HTMLElement>,
      ).map((cell) => cell.textContent?.trim());

    expect(headers().map((header) => header.getAttribute('aria-sort'))).toEqual([
      'none',
      'none',
      'none',
    ]);
    expect(fixture.nativeElement.querySelector('thead')?.classList.contains('table-light')).toBe(
      true,
    );
    expect(fixture.nativeElement.querySelectorAll('thead i.bi-arrow-down-up')).toHaveLength(2);
    expect(headers()[2].querySelector('i')).toBeNull();
    expect(headers()[0].querySelector('i')?.getAttribute('aria-hidden')).toBe('true');

    buttons()[0].click();
    await fixture.whenStable();
    expect(headers()[0].getAttribute('aria-sort')).toBe('ascending');
    expect(headers()[1].getAttribute('aria-sort')).toBe('none');
    expect(headers()[0].querySelector('i')?.classList.contains('bi-caret-up-fill')).toBe(true);
    expect(headers()[0].querySelector('i')?.getAttribute('aria-hidden')).toBe('true');
    expect(headers()[1].querySelector('i')?.classList.contains('bi-arrow-down-up')).toBe(true);
    expect(folios()).toEqual(['3', '20']);

    buttons()[0].click();
    await fixture.whenStable();
    expect(headers()[0].getAttribute('aria-sort')).toBe('descending');
    expect(headers()[0].querySelector('i')?.classList.contains('bi-caret-down-fill')).toBe(true);
    expect(folios()).toEqual(['20', '3']);

    searchTerm.set('anulado');
    await fixture.whenStable();
    expect(folios()).toEqual(['3']);

    buttons()[1].focus();
    expect(document.activeElement).toBe(buttons()[1]);
    // Browser-native buttons retain keyboard activation; jsdom cannot synthesize its Enter click.
    buttons()[1].click();
    await fixture.whenStable();
    expect(headers()[0].getAttribute('aria-sort')).toBe('none');
    expect(headers()[0].querySelector('i')?.classList.contains('bi-arrow-down-up')).toBe(true);
    expect(headers()[1].getAttribute('aria-sort')).toBe('ascending');
  });

  it('leaves non-sortable consumer columns and row selection usable', async () => {
    const fixture = TestBed.createComponent(Table);
    Object.defineProperties(fixture.componentInstance, {
      columns: { value: signal([{ key: 'name', name: 'Nombre' }]) },
      data: { value: signal([{ name: 'Alumno', selected: false }]) },
      selectionEnabled: { value: signal(true) },
      showRowNumber: { value: signal(false) },
    });
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelector('thead button')).toBeNull();
    expect(fixture.nativeElement.querySelector('thead i')).toBeNull();
    expect(fixture.nativeElement.querySelector('tbody td:last-child')?.textContent).toContain(
      'Alumno',
    );
    const checkbox = fixture.nativeElement.querySelector(
      'tbody input[type="checkbox"]',
    ) as HTMLInputElement;
    expect(checkbox).not.toBeNull();
    checkbox.click();
    await fixture.whenStable();
    expect(checkbox.checked).toBe(true);
  });

  it('styles an emphasized action header like sortable buttons without enabling sorting', async () => {
    const fixture = TestBed.createComponent(Table);
    Object.defineProperties(fixture.componentInstance, {
      columns: {
        value: signal<TableColumn[]>([{ key: 'download', name: 'Descargar', emphasized: true }]),
      },
      data: { value: signal([{ download: '' }]) },
      showRowNumber: { value: signal(false) },
    });
    await fixture.whenStable();

    const header = fixture.nativeElement.querySelector('thead th') as HTMLElement;
    expect(header.textContent?.trim()).toBe('Descargar');
    expect(header.querySelector('span.btn-link.fw-bold')).not.toBeNull();
    expect(header.querySelector('button')).toBeNull();
    expect(header.querySelector('i')).toBeNull();
    expect(header.getAttribute('aria-sort')).toBe('none');
    header.click();
    expect(fixture.componentInstance.sortColumn()).toBeNull();
  });

  it('keeps ordinary action columns and plain multiline cells unchanged without opt-in', async () => {
    const fixture = TestBed.createComponent(Table);
    Object.defineProperties(fixture.componentInstance, {
      columns: {
        value: signal([
          { key: 'id', name: 'ID' },
          { key: 'owner', name: 'Owner', multiline: true },
          { key: 'action', name: 'Action', action: { label: 'Open', actionKey: 'open' } },
        ]),
      },
      data: { value: signal([{ id: '42', owner: 'Name\nDate', action: '' }]) },
      showRowNumber: { value: signal(false) },
    });
    await fixture.whenStable();
    const row = fixture.nativeElement.querySelector('tbody tr') as HTMLElement;
    expect(row.querySelectorAll('td')).toHaveLength(3);
    expect(row.querySelectorAll('.badge')).toHaveLength(0);
    expect(row.querySelectorAll('button')).toHaveLength(1);
    expect(row.querySelector('td:nth-child(2)')?.textContent?.trim()).toBe('Name\nDate');
    const events: unknown[] = [];
    fixture.componentInstance.actionEvent.subscribe((event) => events.push(event));
    row.querySelector('button')?.click();
    expect(events).toEqual([{ id: '42', actionKey: 'open' }]);
  });

  it('omits invalid milestone dates while keeping a valid user badge', () => {
    const table = TestBed.createComponent(Table).componentInstance;
    expect(table.milestoneBadges('User\n31/02/2026 12:00:00')).toEqual({ user: 'User', date: '' });
    expect(table.milestoneBadges('User\n29/02/2024 12:00:00')).toEqual({
      user: 'User',
      date: '29/02/2024 12:00:00',
    });
    expect(table.milestoneBadges(null)).toEqual({ user: '', date: '' });
  });

  it('renders milestone badges before visible actions and retains badges when actions are hidden', async () => {
    const fixture = TestBed.createComponent(Table);
    Object.defineProperties(fixture.componentInstance, {
      columns: {
        value: signal<TableColumn[]>([
          { key: 'folio', name: 'Folio' },
          {
            key: 'milestone',
            name: 'Milestone',
            badges: true,
            action: {
              label: 'Review',
              actionKey: 'review',
              ariaLabel: (row) => `Review ${(row as { folio: string }).folio}`,
            },
            actionVisible: (row) => (row as { visible: boolean }).visible,
            actionDisabled: (row) => (row as { disabled: boolean }).disabled,
          },
        ]),
      },
      data: {
        value: signal([
          {
            folio: 'F-1',
            milestone: 'User One\n29/02/2024 12:00:00',
            visible: true,
            disabled: false,
          },
          {
            folio: 'F-2',
            milestone: 'User Two\n01/03/2024 13:00:00',
            visible: false,
            disabled: false,
          },
          {
            folio: 'F-3',
            milestone: 'User Three\n02/03/2024 14:00:00',
            visible: true,
            disabled: true,
          },
        ]),
      },
      showRowNumber: { value: signal(false) },
    });
    await fixture.whenStable();

    const rows = Array.from(fixture.nativeElement.querySelectorAll('tbody tr')) as HTMLElement[];
    expect(rows).toHaveLength(3);
    for (const [index, row] of rows.entries()) {
      expect(row.querySelectorAll('td')).toHaveLength(2);
      expect(
        Array.from(row.querySelectorAll('td:nth-child(2) .badge')).map((badge) =>
          badge.textContent?.trim(),
        ),
      ).toEqual(
        [
          ['User One', '29/02/2024 12:00:00'],
          ['User Two', '01/03/2024 13:00:00'],
          ['User Three', '02/03/2024 14:00:00'],
        ][index],
      );
    }

    const enabledAction = rows[0].querySelector('td:nth-child(2) button') as HTMLButtonElement;
    expect(enabledAction.getAttribute('aria-label')).toBe('Review F-1');
    expect(enabledAction.disabled).toBe(false);
    expect(rows[1].querySelector('button')).toBeNull();
    const disabledAction = rows[2].querySelector('td:nth-child(2) button') as HTMLButtonElement;
    expect(disabledAction.disabled).toBe(true);

    const events: unknown[] = [];
    fixture.componentInstance.actionEvent.subscribe((event) => events.push(event));
    enabledAction.click();
    disabledAction.click();
    expect(events).toEqual([{ id: 'F-1', actionKey: 'review' }]);
  });
});
