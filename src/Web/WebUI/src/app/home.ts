import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-home',
  template: `
    <main>
      <h1>SADInformes</h1>
      <p>Base del sistema de informes.</p>
      <p>Autenticación, navegación e informes se incorporarán en etapas posteriores.</p>
    </main>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Home {}
