import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';
import { Table, type TableColumn } from './table';

describe('Table', () => {
  const columns: TableColumn[] = [
    { key: 'name', name: 'Nombre', sortable: true },
    { key: 'status', name: 'Estado' },
  ];

  it('pagina localmente de a 10 y filtra y ordena mediante entradas públicas', async () => {
    const fixture = TestBed.createComponent(Table);
    fixture.componentRef.setInput('columns', columns);
    fixture.componentRef.setInput(
      'data',
      Array.from({ length: 12 }, (_, index) => ({
        id: `id-${index}`,
        name: `Nombre ${String(index).padStart(2, '0')}`,
        status: index === 11 ? 'Especial' : 'Activo',
      })),
    );
    fixture.componentRef.setInput('showRowNumber', false);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelectorAll('tbody tr')).toHaveLength(10);
    expect(fixture.nativeElement.querySelector('tbody .badge')).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Mostrando 10 de 12 registros');

    const next = [...fixture.nativeElement.querySelectorAll('button')].find(
      (button: HTMLButtonElement) => button.textContent?.trim() === 'Siguiente',
    ) as HTMLButtonElement;
    next.click();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('tbody tr')).toHaveLength(2);

    fixture.componentRef.setInput('searchTerm', 'especial');
    fixture.detectChanges();
    expect(fixture.componentInstance.currentPage()).toBe(1);
    expect(fixture.nativeElement.querySelectorAll('tbody tr')).toHaveLength(1);
    expect(fixture.nativeElement.textContent).toContain('Nombre 11');

    fixture.componentRef.setInput('searchTerm', '');
    fixture.detectChanges();
    (fixture.nativeElement.querySelector('thead button') as HTMLButtonElement).click();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('thead th')?.getAttribute('aria-sort')).toBe(
      'ascending',
    );
  });

  it('numera la segunda página desde 11 y conserva el identificador estable de sus acciones', () => {
    const fixture = TestBed.createComponent(Table);
    fixture.componentRef.setInput('columns', [
      { key: 'name', name: 'Nombre' },
      {
        key: 'action',
        name: 'Acciones',
        action: {
          icon: 'bi-eye',
          label: '',
          actionKey: 'detail',
          ariaLabel: row => `Ver ${(row as { id: string }).id}`,
          testId: row => `row-${(row as { id: string }).id}`,
        },
      },
    ] satisfies TableColumn[]);
    fixture.componentRef.setInput('data', Array.from({ length: 12 }, (_, index) => ({
      id: `stable-${index}`,
      name: `Fila ${index}`,
      action: '',
    })));
    fixture.componentRef.setInput('rowId', 'id');
    const events: unknown[] = [];
    fixture.componentInstance.actionEvent.subscribe(event => events.push(event));
    fixture.detectChanges();

    expect([...fixture.nativeElement.querySelectorAll('thead th')].map((cell: HTMLElement) => cell.textContent?.trim()))
      .toEqual(['#', 'Nombre', 'Acciones']);
    const next = [...fixture.nativeElement.querySelectorAll('button')].find(
      (button: HTMLButtonElement) => button.textContent?.trim() === 'Siguiente',
    ) as HTMLButtonElement;
    next.click();
    fixture.detectChanges();

    const firstRow = fixture.nativeElement.querySelector('tbody tr') as HTMLTableRowElement;
    expect(firstRow.cells[0].textContent?.trim()).toBe('11');
    const action = firstRow.querySelector('[data-testid="row-stable-10"]') as HTMLButtonElement;
    expect(action.getAttribute('aria-label')).toBe('Ver stable-10');
    expect(action.querySelector('i.bi-eye')).not.toBeNull();
    action.click();
    expect(events).toEqual([{ id: 'stable-10', actionKey: 'detail' }]);
  });

  it('usa rowId para acciones sin exigir una columna de ID y conserva el fallback', () => {
    const fixture = TestBed.createComponent(Table);
    fixture.componentRef.setInput('columns', [
      { key: 'name', name: 'Nombre' },
      { key: 'action', name: 'Detalle', action: { label: 'Ver detalle', actionKey: 'detail' } },
    ] satisfies TableColumn[]);
    fixture.componentRef.setInput('data', [{ id: 'internal-7', name: 'Visible', action: '' }]);
    fixture.componentRef.setInput('rowId', 'id');
    fixture.componentRef.setInput('showRowNumber', false);
    const events: unknown[] = [];
    fixture.componentInstance.actionEvent.subscribe(event => events.push(event));
    fixture.detectChanges();

    expect([...fixture.nativeElement.querySelectorAll('thead th')].map((cell: HTMLElement) => cell.textContent?.trim()))
      .toEqual(['Nombre', 'Detalle']);
    (fixture.nativeElement.querySelector('tbody button') as HTMLButtonElement).click();
    expect(events).toEqual([{ id: 'internal-7', actionKey: 'detail' }]);

    fixture.componentRef.setInput('rowId', null);
    fixture.detectChanges();
    (fixture.nativeElement.querySelector('tbody button') as HTMLButtonElement).click();
    expect(events.at(-1)).toEqual({ id: 'Visible', actionKey: 'detail' });
  });

  it('conserva la columna Actions reservada para las acciones globales', () => {
    const fixture = TestBed.createComponent(Table);
    fixture.componentRef.setInput('columns', [
      { key: 'id', name: 'ID' },
      { key: 'Actions', name: 'Acciones' },
    ] satisfies TableColumn[]);
    fixture.componentRef.setInput('data', [{ id: 'row-1', Actions: 'no-renderizar' }]);
    fixture.componentRef.setInput('actions', [{ label: 'Editar', actionKey: 'edit' }]);
    fixture.componentRef.setInput('showRowNumber', false);
    const events: unknown[] = [];
    fixture.componentInstance.actionEvent.subscribe(event => events.push(event));
    fixture.detectChanges();

    const row = fixture.nativeElement.querySelector('tbody tr') as HTMLTableRowElement;
    expect(row.cells).toHaveLength(2);
    expect(row.textContent).not.toContain('no-renderizar');
    (row.querySelector('button') as HTMLButtonElement).click();
    expect(events).toEqual([{ id: 'row-1', actionKey: 'edit' }]);
  });

  it('mantiene acciones alternas y adicionales visibles, deshabilitadas y accesibles', () => {
    const fixture = TestBed.createComponent(Table);
    fixture.componentRef.setInput('columns', [
      { key: 'id', name: 'ID' },
      {
        key: 'action',
        name: 'Acciones',
        action: { label: 'Principal', actionKey: 'main' },
        additionalActions: [
          { label: 'Secundaria', actionKey: 'secondary', ariaLabel: row => `Secundaria ${(row as { id: string }).id}` },
        ],
        alternateAction: { label: 'Alterna', actionKey: 'alternate' },
        alternateActionVisible: row => (row as { alternate: boolean }).alternate,
        actionDisabled: row => (row as { disabled: boolean }).disabled,
      },
    ] satisfies TableColumn[]);
    fixture.componentRef.setInput('data', [
      { id: 'one', action: '', alternate: true, disabled: false },
      { id: 'two', action: '', alternate: false, disabled: true },
    ]);
    fixture.componentRef.setInput('showRowNumber', false);
    const events: unknown[] = [];
    fixture.componentInstance.actionEvent.subscribe(event => events.push(event));
    fixture.detectChanges();

    const rows = [...fixture.nativeElement.querySelectorAll('tbody tr')] as HTMLTableRowElement[];
    expect(rows[0].querySelectorAll('button')).toHaveLength(1);
    expect(rows[0].querySelector('button')?.textContent).toContain('Alterna');
    expect(rows[1].querySelectorAll('button')).toHaveLength(2);
    expect((rows[1].querySelector('button') as HTMLButtonElement).disabled).toBe(true);
    expect(rows[1].querySelectorAll('button')[1].getAttribute('aria-label')).toBe('Secundaria two');
    (rows[0].querySelector('button') as HTMLButtonElement).click();
    expect(events).toEqual([{ id: 'one', actionKey: 'alternate' }]);
  });

  it('renderiza badges válidos antes de su acción y oculta fechas inválidas', () => {
    const fixture = TestBed.createComponent(Table);
    fixture.componentRef.setInput('columns', [
      { key: 'id', name: 'ID' },
      {
        key: 'milestone',
        name: 'Hito',
        badges: true,
        action: { label: 'Revisar', actionKey: 'review', ariaLabel: row => `Revisar ${(row as { id: string }).id}` },
        actionVisible: row => (row as { visible: boolean }).visible,
      },
    ] satisfies TableColumn[]);
    fixture.componentRef.setInput('data', [
      { id: 'valid', milestone: 'Ana\n29/02/2024 12:00:00', visible: true },
      { id: 'invalid', milestone: 'Bea\n31/02/2024 12:00:00', visible: false },
    ]);
    fixture.componentRef.setInput('showRowNumber', false);
    fixture.detectChanges();

    const rows = [...fixture.nativeElement.querySelectorAll('tbody tr')] as HTMLTableRowElement[];
    expect([...rows[0].querySelectorAll('.badge')].map(value => value.textContent?.trim()))
      .toEqual(['Ana', '29/02/2024 12:00:00']);
    expect([...rows[1].querySelectorAll('.badge')].map(value => value.textContent?.trim()))
      .toEqual(['Bea']);
    expect(rows[0].querySelector('button')?.getAttribute('aria-label')).toBe('Revisar valid');
    expect(rows[1].querySelector('button')).toBeNull();
  });

  it('mantiene botones accesibles, estado vacío y selección pública', () => {
    const fixture = TestBed.createComponent(Table);
    fixture.componentRef.setInput('columns', columns);
    fixture.componentRef.setInput('data', []);
    fixture.componentRef.setInput('selectionEnabled', true);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No se encontraron registros');
    expect(fixture.nativeElement.querySelector('nav')?.getAttribute('aria-label')).toBeTruthy();

    fixture.componentRef.setInput('data', [{ name: 'Ana', status: 'Activo', selected: false }]);
    fixture.detectChanges();
    const checkbox = fixture.nativeElement.querySelector('input[type="checkbox"]') as HTMLInputElement;
    expect(checkbox).not.toBeNull();
  });
});
