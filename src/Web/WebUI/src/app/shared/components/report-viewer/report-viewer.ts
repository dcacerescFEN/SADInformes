import { HttpClient } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, DestroyRef, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';

const PRINT_DETAIL_URL = '/api/reports/requests/by-status/print-detail';

@Component({
  selector: 'app-report-viewer',
  imports: [],
  templateUrl: './report-viewer.html',
  styleUrl: './report-viewer.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReportViewer {
  private readonly http = inject(HttpClient);
  private readonly destroyRef = inject(DestroyRef);
  private readonly sanitizer = inject(DomSanitizer);

  readonly requestCode = input<string>('');
  readonly title = input<string>('Detalle de solicitud');

  readonly isLoading = signal(false);
  readonly hasError = signal(false);
  private readonly blob = signal<Blob | null>(null);
  private readonly rawBlobUrl = signal<string | null>(null);
  readonly hasBlob = computed(() => this.blob() !== null);
  readonly safeBlobUrl = computed<SafeResourceUrl | null>(() => {
    const url = this.rawBlobUrl();
    return url ? this.sanitizer.bypassSecurityTrustResourceUrl(url) : null;
  });
  private revision = 0;

  constructor() {
    this.destroyRef.onDestroy(() => this.revokeBlobUrl());
    effect(() => {
      const requestCode = this.requestCode().trim();
      untracked(() => this.loadPdf(requestCode));
    });
  }

  retry(): void {
    this.loadPdf(this.requestCode().trim());
  }

  download(): void {
    const blob = this.blob();
    if (!blob) {
      return;
    }

    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `Solicitud_${this.requestCode().replaceAll('/', '').trim()}.pdf`;
    link.click();
    URL.revokeObjectURL(url);
  }

  private loadPdf(requestCode: string): void {
    const revision = ++this.revision;
    this.hasError.set(false);

    if (!requestCode) {
      this.isLoading.set(false);
      this.revokeBlobUrl();
      return;
    }

    this.isLoading.set(true);
    this.revokeBlobUrl();

    this.http.get(PRINT_DETAIL_URL, {
      params: { requestCode },
      observe: 'response',
      responseType: 'blob',
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: response => {
        if (revision !== this.revision) {
          return;
        }
        this.isLoading.set(false);
        if (!response.body) {
          this.hasError.set(true);
          return;
        }
        this.blob.set(response.body);
        this.rawBlobUrl.set(URL.createObjectURL(response.body));
      },
      error: () => {
        if (revision !== this.revision) {
          return;
        }
        this.isLoading.set(false);
        this.hasError.set(true);
      },
    });
  }

  private revokeBlobUrl(): void {
    const current = this.rawBlobUrl();
    if (!current) {
      return;
    }
    URL.revokeObjectURL(current);
    this.rawBlobUrl.set(null);
    this.blob.set(null);
  }
}
