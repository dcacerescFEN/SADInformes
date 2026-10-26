import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AuthService } from './core/auth/auth-service';
import { Header } from './core/layout/header';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Header],
  template: '@if (auth.isAuthenticated()) { <app-header /> } <router-outlet />',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {
  readonly auth = inject(AuthService);
}
