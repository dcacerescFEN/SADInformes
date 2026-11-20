import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpClient } from '@angular/common/http';
import { NavigationService } from '../../core/navigation/navigation-service';
import { Search } from '../../shared/components/search';
import { Table, TableColumn } from '../../shared/components/table';

interface Row {
  studentCode: string;
  name: string;
  studentType: string;
  decree: string;
  decreeDescription: string;
  mention: string;
  graduatedFrom: string;
  situation: string;
  entrySemester: string;
  promotion: string;
}
interface RowView extends Row { decreeText: string; }
interface Report { period: string; rows: Row[]; }

const ENDPOINT = '/api/reports/regulation/graduates-over-3-years/report';
const COLUMNS: readonly TableColumn[] = [
  { key: 'studentCode', name: 'Código', sortable: true },
  { key: 'name', name: 'Nombre', sortable: true },
  { key: 'studentType', name: 'Carrera', sortable: true },
  { key: 'decreeText', name: 'Decreto', sortable: true, multiline: true },
  { key: 'situation', name: 'Situación', sortable: true },
  { key: 'graduatedFrom', name: 'Egresado de', sortable: true },
  { key: 'entrySemester', name: 'Semestre de Ingreso', sortable: true },
  { key: 'promotion', name: 'Promoción', sortable: true },
];

function decreeText(row: Row): string {
  const decree = (row.decree ?? '').trim();
  const description = (row.decreeDescription ?? '').trim();
  if (!description || description === decree) return decree;
  return decree ? `${decree}\n${description}` : description;
}

@Component({
  selector: 'app-graduates-over-3-years',
  imports: [Search, Table],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="container-fluid px-0 py-4">
      <div class="page-header">
        <div>
          <h1 class="page-title">
            <i class="bi bi-mortarboard" aria-hidden="true"></i>
            Detección de Alumnos Egresados Más de 3 Años
          </h1>
          <p class="page-subtitle mb-0">Alumnos egresados hace más de tres años con su situación académica
            @if (period()) { · {{ period() }} }.</p>
        </div>
      </div>

      @if (error()) { <div class="alert alert-warning" role="alert">{{ error() }}</div> }

      @if (!error()) {
        <div class="form-section">
          <h2 class="section-title"><i class="bi bi-table" aria-hidden="true"></i>Resultados</h2>
          <div class="d-flex justify-content-end mb-3">
            <div class="search-box"><app-search (search)="searchTerm.set($event)" /></div>
          </div>
          <app-table [loading]="loading() || !loaded()" aria-label="Alumnos egresados más de 3 años"
            [data]="rows()" [columns]="columns" [searchTerm]="searchTerm()"
            [showSummary]="false" [showRowNumber]="false" />
          <p class="fw-semibold mt-3 mb-0">Total: {{ rows().length }}</p>
        </div>
      }
    </div>
  `,
  styles: '.search-box { width: 20rem; max-width: 100%; }',
})
export class GraduatesOver3Years implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly destroyRef = inject(DestroyRef);
  private readonly navigation = inject(NavigationService);
  protected readonly columns = COLUMNS.map(column => column.key === 'studentCode'
    ? { ...column, link: (row: object) => this.navigation.studentGradesUrl((row as Row).studentCode) }
    : column);
  protected readonly rows = signal<RowView[]>([]);
  protected readonly searchTerm = signal('');
  protected readonly loading = signal(false);
  protected readonly loaded = signal(false);
  protected readonly error = signal('');
  protected readonly period = signal('');

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
          this.period.set(result.data.period);
          this.rows.set(result.data.rows.map(row => ({ ...row, decreeText: decreeText(row) })));
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
