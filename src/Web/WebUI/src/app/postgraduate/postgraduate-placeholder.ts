import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { map } from 'rxjs';
import { PostgraduateReport } from './postgraduate-catalogue';

@Component({
  selector: 'app-postgraduate-placeholder',
  imports: [RouterLink],
  template: `
    @if (report(); as report) {
      <div class="container-fluid px-0 py-4">
        <div class="page-header">
          <div class="d-flex flex-wrap justify-content-between align-items-start gap-3">
            <div>
              <h1 id="report-title" class="page-title">
                <i class="bi bi-file-earmark-text" aria-hidden="true"></i>
                {{ report.title }}
              </h1>
              <p class="page-subtitle mb-0">{{ report.description }}</p>
            </div>
            <a class="btn btn-outline-secondary" routerLink="/">
              <i class="bi bi-arrow-left me-2" aria-hidden="true"></i>Volver al inicio
            </a>
          </div>
        </div>

        <div class="form-section">
          <p><span class="badge text-bg-secondary">Pendiente de migración</span></p>
          <div class="alert alert-info mb-3">
            Esta ficha es únicamente visual. La funcionalidad todavía no está migrada:
            no consulta datos ni permite realizar operaciones.
          </div>
          <h2 class="h6">Referencia en SAD</h2>
          <p class="mb-0"><code>{{ report.legacyPath }}</code></p>
        </div>
      </div>
    }
  `,
  styles: `
    :host { display: block; }
    code { overflow-wrap: anywhere; }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PostgraduatePlaceholder {
  private readonly route = inject(ActivatedRoute);
  protected readonly report = toSignal(
    this.route.data.pipe(map(data => data['report'] as PostgraduateReport)),
    { initialValue: this.route.snapshot.data['report'] as PostgraduateReport },
  );
}
