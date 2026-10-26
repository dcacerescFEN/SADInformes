import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { AuthService } from './core/auth/auth-service';

@Component({
  selector: 'app-home',
  template: `
    <section class="home-page" aria-labelledby="home-title">
      <h1 id="home-title">SADInformes</h1>
      <p>Base del sistema de informes.</p>
      @if (auth.isAuthenticated()) {
        <p>Sesión iniciada: {{ auth.username() }}</p>
      } @else {
        <p>Tu sesión ha finalizado.</p>
        <button type="button" (click)="login()">Iniciar sesión</button>
      }
      @if (error()) { <p role="alert">{{ error() }}</p> }
      <p>Los informes se incorporarán en etapas posteriores.</p>
    </section>
  `,
  styles: ':host { display: block; } .home-page { max-width: 64rem; margin: 0 auto; }',
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
