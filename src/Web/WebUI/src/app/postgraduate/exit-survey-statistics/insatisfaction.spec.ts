import { TestBed } from '@angular/core/testing';
import { Insatisfaction, InsatisfactionRow } from './insatisfaction';

const rows = (count: number): InsatisfactionRow[] => Array.from({ length: count }, (_, index) => ({
  professor: 'Profesor ' + index, count: index + 1, professorCode: 'P' + index,
  period: '2024A', studentType: 'Todos', modality: 'Todos',
}));

describe('Insatisfaction table', () => {
  beforeEach(async () => { await TestBed.configureTestingModule({ imports: [Insatisfaction] }).compileComponents(); });

  it('paginates ten rows and exposes an icon-only tooltip action', () => {
    const fixture = TestBed.createComponent(Insatisfaction);
    fixture.componentRef.setInput('rows', rows(12)); fixture.detectChanges();
    const body: HTMLElement = fixture.nativeElement;
    expect(body.querySelectorAll('tbody tr')).toHaveLength(10);
    expect(body.textContent).toContain('Mostrando 1–10 de 12');
    const action = body.querySelector('[data-detail="P0"]') as HTMLButtonElement;
    expect(action.textContent.trim()).toBe(''); expect(action.title).toBe('Ver motivos de Profesor 0');
    (body.querySelector('.page-item:last-child button') as HTMLButtonElement).click(); fixture.detectChanges();
    expect(body.querySelectorAll('tbody tr')).toHaveLength(2);
    expect(body.textContent).toContain('Mostrando 11–12 de 12');
    fixture.destroy();
  });

  it('filters by professor/code and resets pagination to the first page', () => {
    const fixture = TestBed.createComponent(Insatisfaction);
    fixture.componentRef.setInput('rows', rows(12)); fixture.detectChanges();
    const body: HTMLElement = fixture.nativeElement;
    (body.querySelector('.page-item:last-child button') as HTMLButtonElement).click(); fixture.detectChanges();
    const input = body.querySelector('app-search input') as HTMLInputElement;
    input.value = 'Profesor 11'; input.dispatchEvent(new Event('input')); fixture.detectChanges();
    expect(body.querySelectorAll('tbody tr')).toHaveLength(1);
    expect(body.textContent).toContain('Mostrando 1–1 de 1');
    expect(body.textContent).toContain('Profesor 11');
    expect(body.textContent).not.toContain('Profesor 10');
    fixture.destroy();
  });

  it('opens motives in a modal and closes it', () => {
    const fixture = TestBed.createComponent(Insatisfaction);
    fixture.componentRef.setInput('rows', rows(1)); fixture.componentRef.setInput('detail', { professor: 'Profesor 0', motives: ['Motivo uno'] });
    fixture.detectChanges();
    (fixture.nativeElement.querySelector('[data-detail="P0"]') as HTMLButtonElement).click(); fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.modal')?.textContent).toContain('Motivo uno');
    expect(fixture.nativeElement.querySelector('.modal-dialog.modal-lg')).not.toBeNull();
    (fixture.nativeElement.querySelector('[aria-label="Cerrar detalle"]') as HTMLButtonElement).click(); fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.modal')).toBeNull();
    fixture.destroy();
  });
});
