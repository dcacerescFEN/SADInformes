import { Component, output, signal } from '@angular/core';

@Component({
  selector: 'app-search',
  imports: [],
  template: `
    <div class="input-group">
      <span class="input-group-text"><i class="bi bi-search"></i></span>
      <input type="text" class="form-control" placeholder="Buscar..." [value]="value()" (input)="input($event)" />
      @if (value()) {
        <button class="btn btn-clear btn-search-clear" type="button" (click)="clear()" aria-label="Limpiar búsqueda">
          <i class="bi bi-x-lg"></i>
        </button>
      }
    </div>
  `,
})
export class Search {
  value = signal('');
  readonly search = output<string>();
  input(event: Event): void {
    const term = (event.target as HTMLInputElement).value;
    this.value.set(term);
    this.search.emit(term);
  }
  clear(): void { this.value.set(''); this.search.emit(''); }
}
