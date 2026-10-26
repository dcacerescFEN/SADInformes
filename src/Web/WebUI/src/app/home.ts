import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { AuthService } from './core/auth/auth-service';

@Component({
  selector: 'app-home',
  template: `
    <main>
      <h1>SADInformes</h1>
      <p>Base del sistema de informes.</p>
      @if (auth.isAuthenticated()) {
        <p>Sesión iniciada: {{ auth.username() }}</p>
      } @else {
        <p>Tu sesión ha finalizado.</p>
        <button type="button" (click)="login()">Iniciar sesión</button>
      }
      @if (error()) { <p role="alert">{{ error() }}</p> }
      <p>La sidebar y los informes se incorporarán en etapas posteriores.</p>
    </main>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Home {
  readonly auth = inject(AuthService);
  readonly error = signal('');

  async login(): Promise<void> {
    try { await this.auth.login(); }
    catch { this.error.set('No se pudo iniciar sesión. Intenta nuevamente.'); }
  }

}
