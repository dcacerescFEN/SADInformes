import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { AuthService } from '../auth/auth-service';
import { NavigationService } from './navigation-service';

describe('Menú central', () => {
  let navigation: NavigationService;
  let http: HttpTestingController;
  let hasSiteAccess: ReturnType<typeof vi.fn>;
  beforeEach(() => {
    hasSiteAccess = vi.fn((id: string) => id === 'SADAlumnos');
    TestBed.configureTestingModule({ providers: [
      provideHttpClient(), provideHttpClientTesting(),
      { provide: AuthService, useValue: {
        isAuthenticated: () => true, hasSiteAccess,
      } },
    ] });
    navigation = TestBed.inject(NavigationService);
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());

  async function load(menu: unknown) {
    const pending = navigation.load();
    http.expectOne('/api/config/site').flush({ success: true, message: null, data: menu });
    await pending;
  }

  it('usa el contrato común con claves canónicas y oculta destinos sin acceso', async () => {
    await load({
      studentsMenuUrl: 'https://alumnos.example.test', studentsMenuClientId: ' SADAlumnos ',
      teachersMenuUrl: 'https://profesores.example.test', teachersMenuClientId: 'SADProfesores',
      coursesMenuUrl: 'https://cursos.example.test',
    });
    expect(navigation.links()).toEqual([{
      key: 'student', label: 'Alumnos', icon: 'bi-person-badge', url: 'https://alumnos.example.test',
    }]);
    expect(navigation.error()).toBe('');
  });

  it('inserta Informes configurado inmediatamente antes de Administración', async () => {
    hasSiteAccess.mockImplementation((id: string) => ['SADAlumnos', 'SADInformes-iis', 'SADAdministracion'].includes(id));
    await load({
      studentsMenuUrl: 'https://alumnos.example.test', studentsMenuClientId: 'SADAlumnos',
      reportsMenuUrl: 'https://sadinformes-dev.fen.uchile.cl', reportsMenuClientId: 'SADInformes-iis',
      administrationMenuUrl: 'https://admin.example.test', administrationMenuClientId: 'SADAdministracion',
    });
    expect(navigation.links().map((item) => item.key)).toEqual(['student', 'reports', 'administration']);
    expect(navigation.links()[1]).toMatchObject({
      label: 'Informes', url: 'https://sadinformes-dev.fen.uchile.cl',
    });
    expect(hasSiteAccess).toHaveBeenCalledWith('SADInformes-iis');
  });

  it('oculta Informes si el cliente configurado no tiene acceso', async () => {
    await load({
      reportsMenuUrl: 'https://sadinformes-dev.fen.uchile.cl', reportsMenuClientId: 'SADInformes',
    });
    expect(navigation.links()).toEqual([]);
    expect(hasSiteAccess).toHaveBeenCalledWith('SADInformes');
  });

  it.each([
    null, '/relativa', 'javascript:alert(1)', 'data:text/html,x',
    'https://usuario:clave@example.test', 'https://example .test', 'https:///example.test',
  ])('deja deshabilitado un destino autorizado con URL inválida: %s', async (url) => {
    await load({ studentsMenuClientId: 'SADAlumnos', studentsMenuUrl: url });
    expect(navigation.links()).toHaveLength(1);
    expect(navigation.links()[0].url).toBeNull();
  });

  it('no inventa enlaces ante un contrato vacío', async () => {
    await load(null);
    expect(navigation.links()).toEqual([]);
  });

  it('rechaza un envelope fallido aunque contenga destinos', async () => {
    const pending = navigation.load();
    http.expectOne('/api/config/site').flush({
      success: false, data: { studentsMenuClientId: 'SADAlumnos', studentsMenuUrl: 'https://example.test' },
    });
    await pending;
    expect(navigation.links()).toEqual([]);
    expect(navigation.error()).toBe('No se pudo cargar la navegación.');
  });

  it('retira enlaces anteriores y muestra aviso si AppConfig falla', async () => {
    await load({ studentsMenuClientId: 'SADAlumnos', studentsMenuUrl: 'https://alumnos.example.test' });
    const pending = navigation.load();
    http.expectOne('/api/config/site').flush({}, { status: 503, statusText: 'Unavailable' });
    await pending;
    expect(navigation.links()).toEqual([]);
    expect(navigation.error()).toBe('No se pudo cargar la navegación.');
  });
});
