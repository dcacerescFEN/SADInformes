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
      <section class="report-preview" aria-labelledby="report-title">
        <p class="text-body-secondary mb-2">Postgrado</p>
        <h1 id="report-title">{{ report.title }}</h1>
        <p><span class="badge text-bg-secondary">Pendiente de migración</span></p>
        <p>{{ report.description }}</p>
        <div class="alert alert-info">
          Esta ficha es únicamente visual. La funcionalidad todavía no está migrada:
          no consulta datos ni permite realizar operaciones.
        </div>
        <h2 class="h6">Referencia en SAD</h2>
        <p><code>{{ report.legacyPath }}</code></p>
        <a routerLink="/">Volver al inicio</a>
      </section>
    }
  `,
  styles: `
    :host { display: block; }
    .report-preview { max-width: 64rem; margin: 0 auto; }
    h1, code { overflow-wrap: anywhere; }
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
