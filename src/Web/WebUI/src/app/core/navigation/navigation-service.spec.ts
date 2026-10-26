import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { AuthService } from '../auth/auth-service';
import { NavigationService } from './navigation-service';

describe('Menú central', () => {
  let navigation: NavigationService;
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [
      provideHttpClient(), provideHttpClientTesting(),
      { provide: AuthService, useValue: {
        isAuthenticated: () => true, hasSiteAccess: (id: string) => id === 'SADAlumnos',
      } },
    ] });
    navigation = TestBed.inject(NavigationService);
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());

  async function load(menu: unknown) {
    const pending = navigation.load();
    http.expectOne('/api/config/site').flush({ menu });
    await pending;
  }

  it('usa la configuración y oculta destinos sin cliente o acceso', async () => {
    await load({
      studentUrl: 'https://alumnos.example.test', studentClientId: ' SADAlumnos ',
      teacherUrl: 'https://profesores.example.test', teacherClientId: 'SADProfesores',
      coursesMenuUrl: 'https://cursos.example.test',
    });
    expect(navigation.links()).toEqual([{
      key: 'student', label: 'Alumnos', icon: 'bi-person-badge', url: 'https://alumnos.example.test',
    }]);
    expect(navigation.error()).toBe('');
  });

  it.each([
    null, '/relativa', 'javascript:alert(1)', 'data:text/html,x',
    'https://usuario:clave@example.test', 'https://example .test', 'https:///example.test',
  ])('deja deshabilitado un destino autorizado con URL inválida: %s', async (url) => {
    await load({ studentClientId: 'SADAlumnos', studentUrl: url });
    expect(navigation.links()).toHaveLength(1);
    expect(navigation.links()[0].url).toBeNull();
  });

  it('no inventa enlaces ante un contrato vacío', async () => {
    await load(null);
    expect(navigation.links()).toEqual([]);
  });

  it('retira enlaces anteriores y muestra aviso si AppConfig falla', async () => {
    await load({ studentClientId: 'SADAlumnos', studentUrl: 'https://alumnos.example.test' });
    const pending = navigation.load();
    http.expectOne('/api/config/site').flush({}, { status: 503, statusText: 'Unavailable' });
    await pending;
    expect(navigation.links()).toEqual([]);
    expect(navigation.error()).toBe('No se pudo cargar la navegación.');
  });
});
