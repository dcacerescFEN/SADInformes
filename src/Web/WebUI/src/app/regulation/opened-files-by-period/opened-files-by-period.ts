import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpClient } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { NavigationService } from '../../core/navigation/navigation-service';
import { Search } from '../../shared/components/search';
import { Table, TableColumn } from '../../shared/components/table';

interface Row {
  studentCode: string;
  titleCode: string;
  personCode: string;
  name: string;
  degreeTitle: string;
  career: string;
  situation: string;
  promotion: string;
  openingDate: string;
  phone: string;
  fenEmail: string;
  personalEmail: string;
}
interface RowView extends Row { photoUrl: string; }
interface Report { rows: Row[]; }

const ENDPOINT = '/api/reports/regulation/opened-files-by-period/report';
const DEFAULT_PHOTO = 'https://fotos.fen.uchile.cl/perfilNN.jpg';
const MISSING_DATES = 'Debe indicar la fecha desde y la fecha hasta.';
const INVALID_RANGE = 'La fecha desde no puede ser posterior a la fecha hasta.';
const COLUMNS: readonly TableColumn[] = [
  { key: 'name', name: 'Nombre', sortable: true },
  { key: 'photoUrl', name: 'Foto', sortable: false },
  { key: 'degreeTitle', name: 'Título/Grado', sortable: true },
  { key: 'personCode', name: 'Rut', sortable: true },
  { key: 'career', name: 'Carrera', sortable: true },
  { key: 'situation', name: 'Situación Académica', sortable: true },
  { key: 'promotion', name: 'Promoción', sortable: true },
  { key: 'openingDate', name: 'Fecha Apertura', sortable: true },
  { key: 'phone', name: 'Teléfono(s)', sortable: true },
  { key: 'fenEmail', name: 'Correo Fen', sortable: true },
  { key: 'personalEmail', name: 'Correo Personal', sortable: true },
];

function photoUrl(personCode: string): string {
  const code = (personCode ?? '').trim();
  return code ? `https://fotos.fen.uchile.cl/${encodeURIComponent(code)}.jpg` : DEFAULT_PHOTO;
}

@Component({
  selector: 'app-opened-files-by-period',
  imports: [ReactiveFormsModule, Search, Table],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="container-fluid px-0 py-4">
      <div class="page-header">
        <div>
          <h1 class="page-title">
            <i class="bi bi-folder2-open" aria-hidden="true"></i>
            Expedientes Abiertos en un Período Dado
          </h1>
          <p class="page-subtitle mb-0">Alumnos que abrieron su expediente de titulación dentro del
            período consultado, con su situación académica y datos de contacto.</p>
        </div>
      </div>

      <div class="form-section mb-3">
        <h2 class="section-title"><i class="bi bi-funnel" aria-hidden="true"></i>Filtros</h2>
        <form [formGroup]="form" (ngSubmit)="submit()">
          <div class="row g-3 align-items-end">
            <div class="col-lg-3 col-md-6">
              <div class="form-floating">
                <input id="opened-files-from" type="date" class="form-control" formControlName="from"
                  placeholder="Desde" required />
                <label for="opened-files-from">Desde</label>
              </div>
            </div>
            <div class="col-lg-3 col-md-6">
              <div class="form-floating">
                <input id="opened-files-to" type="date" class="form-control" formControlName="to"
                  placeholder="Hasta" required />
                <label for="opened-files-to">Hasta</label>
              </div>
            </div>
            <div class="col-auto">
              <button type="submit" class="btn btn-search" [disabled]="loading()">
                <i class="bi bi-search me-1" aria-hidden="true"></i>Mostrar
              </button>
            </div>
          </div>
        </form>
      </div>

      @if (error()) { <div class="alert alert-warning" role="alert">{{ error() }}</div> }

      @if (searched() && !error()) {
        <div class="form-section">
          <h2 class="section-title"><i class="bi bi-table" aria-hidden="true"></i>Resultados</h2>
          <div class="d-flex justify-content-end mb-3">
            <div class="search-box"><app-search (search)="searchTerm.set($event)" /></div>
          </div>
          <app-table [loading]="loading() || !loaded()" aria-label="Expedientes abiertos en el período"
            [data]="rows()" [columns]="columns" [searchTerm]="searchTerm()"
            [showSummary]="false" [showRowNumber]="false" />
          <p class="fw-semibold mt-3 mb-0">Total: {{ rows().length }}</p>
        </div>
      }
    </div>
  `,
  styles: '.search-box { width: 20rem; max-width: 100%; }',
})
export class OpenedFilesByPeriod {
  private readonly http = inject(HttpClient);
  private readonly destroyRef = inject(DestroyRef);
  private readonly navigation = inject(NavigationService);
  protected readonly form = inject(FormBuilder).nonNullable.group({
    from: ['', Validators.required], to: ['', Validators.required],
  });
  protected readonly columns = COLUMNS.map(column => column.key === 'name'
    ? { ...column, link: (row: object) => this.navigation.studentFileOpeningUrl((row as Row).studentCode) }
    : column);
  protected readonly rows = signal<RowView[]>([]);
  protected readonly searchTerm = signal('');
  protected readonly searched = signal(false);
  protected readonly loading = signal(false);
  protected readonly loaded = signal(false);
  protected readonly error = signal('');

  protected submit(): void {
    if (this.loading()) return;
    const { from, to } = this.form.getRawValue();
    this.error.set('');
    if (!from || !to) { this.error.set(MISSING_DATES); return; }
    if (from > to) { this.error.set(INVALID_RANGE); return; }
    this.searched.set(true);
    this.searchTerm.set('');
    this.loadReport({ from, to });
  }

  private loadReport(params: { from: string; to: string }): void {
    this.loading.set(true);
    this.loaded.set(false);
    this.rows.set([]);
    this.http.get<{ success: boolean; data: Report }>(ENDPOINT, { params })
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: result => {
          this.loading.set(false);
          this.loaded.set(true);
          if (!result.success || !result.data) {
            this.error.set('No se pudo cargar el informe.');
            return;
          }
          this.rows.set(result.data.rows.map(row => ({ ...row, photoUrl: photoUrl(row.personCode) })));
        },
        error: () => {
          this.loading.set(false);
          this.loaded.set(true);
          this.rows.set([]);
          this.error.set('No se pudo cargar el informe.');
        },
      });
  }
}
