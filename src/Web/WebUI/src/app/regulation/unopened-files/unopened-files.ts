import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpClient } from '@angular/common/http';
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
  phone: string;
  fenEmail: string;
  personalEmail: string;
}
interface RowView extends Row { photoUrl: string; }
interface Report { rows: Row[]; }

const ENDPOINT = '/api/reports/regulation/unopened-files/report';
const DEFAULT_PHOTO = 'https://fotos.fen.uchile.cl/perfilNN.jpg';
const COLUMNS: readonly TableColumn[] = [
  { key: 'name', name: 'Nombre', sortable: true },
  { key: 'photoUrl', name: 'Foto', sortable: false },
  { key: 'degreeTitle', name: 'Título/Grado', sortable: true },
  { key: 'personCode', name: 'Rut', sortable: true },
  { key: 'career', name: 'Carrera', sortable: true },
  { key: 'situation', name: 'Situación Académica', sortable: true },
  { key: 'promotion', name: 'Promoción', sortable: true },
  { key: 'phone', name: 'Teléfono(s)', sortable: true },
  { key: 'fenEmail', name: 'Correo Fen', sortable: true },
  { key: 'personalEmail', name: 'Correo Personal', sortable: true },
];

function photoUrl(personCode: string): string {
  const code = (personCode ?? '').trim();
  return code ? `https://fotos.fen.uchile.cl/${encodeURIComponent(code)}.jpg` : DEFAULT_PHOTO;
}

@Component({
  selector: 'app-unopened-files',
  imports: [Search, Table],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="container-fluid px-0 py-4">
      <div class="page-header">
        <div>
          <h1 class="page-title">
            <i class="bi bi-folder-x" aria-hidden="true"></i>
            Alumnos que cumplen requisitos y no han abierto expediente
          </h1>
          <p class="page-subtitle mb-0">Alumnos que cumplen los requisitos de la reglamentación y aún
            no han abierto su expediente de titulación.</p>
        </div>
      </div>

      <div class="alert alert-warning" role="note" data-notice="university-debts">
        <i class="bi bi-exclamation-triangle me-1" aria-hidden="true"></i>
         SAD no valida deudas a nivel de universidad. Esto debe ser verificado en los sistemas corporativos de la Universidad.
      </div>

      @if (error()) { <div class="alert alert-warning" role="alert">{{ error() }}</div> }

      @if (!error()) {
        <div class="form-section">
          <h2 class="section-title"><i class="bi bi-table" aria-hidden="true"></i>Resultados</h2>
          <div class="d-flex justify-content-end mb-3">
            <div class="search-box"><app-search (search)="searchTerm.set($event)" /></div>
          </div>
          <app-table [loading]="loading() || !loaded()" aria-label="Alumnos sin expediente abierto"
            [data]="rows()" [columns]="columns" [searchTerm]="searchTerm()"
            [showSummary]="false" [showRowNumber]="false" />
          <p class="fw-semibold mt-3 mb-0">Total: {{ rows().length }}</p>
        </div>
      }
    </div>
  `,
  styles: '.search-box { width: 20rem; max-width: 100%; }',
})
export class UnopenedFiles implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly destroyRef = inject(DestroyRef);
  private readonly navigation = inject(NavigationService);
  protected readonly columns = COLUMNS.map(column => column.key === 'name'
    ? { ...column, link: (row: object) => this.navigation.studentFileOpeningUrl((row as Row).studentCode) }
    : column);
  protected readonly rows = signal<RowView[]>([]);
  protected readonly searchTerm = signal('');
  protected readonly loading = signal(false);
  protected readonly loaded = signal(false);
  protected readonly error = signal('');

  ngOnInit(): void {
    this.loadReport();
  }

  protected loadReport(): void {
    this.loading.set(true);
    this.loaded.set(false);
    this.rows.set([]);
    this.error.set('');
    this.http.get<{ success: boolean; data: Report }>(ENDPOINT)
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
