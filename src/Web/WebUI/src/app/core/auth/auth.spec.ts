import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import Keycloak from 'keycloak-js';
import { AuthService, KEYCLOAK_FACTORY } from './auth-service';
import { authGuard } from './auth-guard';
import { authInterceptor } from './auth-interceptor';

const config = { url: 'https://sso-dev.fen.uchile.cl', realm: 'FenUChile', clientId: 'SADInformes' };

describe('Sesión Keycloak', () => {
  let auth: AuthService;
  let adapter: {
    authenticated: boolean; token: string | undefined;
    tokenParsed: { preferred_username: string; resource_access?: unknown; name?: unknown };
    init: ReturnType<typeof vi.fn>; updateToken: ReturnType<typeof vi.fn>;
    login: ReturnType<typeof vi.fn>; logout: ReturnType<typeof vi.fn>;
    clearToken: ReturnType<typeof vi.fn>;
    onTokenExpired?: () => void; onAuthLogout?: () => void;
  };
  let factory: ReturnType<typeof vi.fn>;
  beforeEach(() => {
    adapter = {
      authenticated: true, token: 'token-prueba', tokenParsed: { preferred_username: 'usuario' },
      init: vi.fn().mockResolvedValue(true), updateToken: vi.fn().mockResolvedValue(false),
      login: vi.fn().mockResolvedValue(undefined), logout: vi.fn().mockResolvedValue(undefined),
      clearToken: vi.fn(() => { adapter.authenticated = false; adapter.token = undefined; }),
    };
    factory = vi.fn(() => adapter as unknown as Keycloak);
    TestBed.configureTestingModule({ providers: [{ provide: KEYCLOAK_FACTORY, useValue: factory }] });
    auth = TestBed.inject(AuthService);
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response(JSON.stringify(config)));
  });
  afterEach(() => vi.restoreAllMocks());

  it('obtiene solo configuración pública e inicializa check-sso con PKCE', async () => {
    await auth.initialize();
    expect(fetch).toHaveBeenCalledWith('/api/config/keycloak');
    expect(factory).toHaveBeenCalledWith(config);
    expect(adapter.init).toHaveBeenCalledWith({
      onLoad: 'check-sso', pkceMethod: 'S256', responseMode: 'query', checkLoginIframe: false,
    });
    expect(auth.isAuthenticated()).toBe(true);
    expect(auth.username()).toBe('usuario');
  });

  it.each([
    { body: {}, status: 200 },
    { body: { ...config, clientId: '' }, status: 200 },
    { body: config, status: 500 },
  ])('rechaza configuración incompleta o error HTTP: $status', async ({ body, status }) => {
    vi.mocked(fetch).mockResolvedValue(new Response(JSON.stringify(body), { status }));
    await expect(auth.initialize()).rejects.toThrow();
    expect(factory).not.toHaveBeenCalled();
    expect(auth.isAuthenticated()).toBe(false);
  });

  it('conserva el estado anónimo cuando no existe sesión SSO', async () => {
    adapter.authenticated = false;
    adapter.init.mockResolvedValue(false);
    await auth.initialize();
    expect(auth.isAuthenticated()).toBe(false);
    expect(await auth.getValidToken()).toBeUndefined();
    expect(adapter.updateToken).not.toHaveBeenCalled();
  });

  it('renueva el token antes de utilizarlo y al expirar', async () => {
    await auth.initialize();
    expect(await auth.getValidToken()).toBe('token-prueba');
    expect(adapter.updateToken).toHaveBeenCalledWith(30);
    adapter.onTokenExpired?.();
    expect(adapter.updateToken).toHaveBeenCalledTimes(2);
  });

  it('limpia la sesión y no devuelve tokens si falla la renovación', async () => {
    await auth.initialize();
    adapter.updateToken.mockRejectedValue(new Error('expirado'));
    await expect(auth.getValidToken()).rejects.toThrow();
    expect(adapter.clearToken).toHaveBeenCalledOnce();
    expect(auth.isAuthenticated()).toBe(false);
    expect(auth.username()).toBe('');
    expect(auth.name()).toBe('');
  });

  it('mantiene el destino de login dentro del origen y cierra sesión en Keycloak', async () => {
    await auth.initialize();
    await auth.login('/?vista=resumen');
    expect(adapter.login).toHaveBeenCalledWith({ redirectUri: location.origin + '/?vista=resumen' });
    await expect(auth.login('https://externo.invalid/')).rejects.toThrow();
    expect(adapter.login).toHaveBeenCalledTimes(1);
    await auth.logout();
    expect(adapter.logout).toHaveBeenCalledWith({ redirectUri: location.origin + '/' });
  });

  it.each([
    ['SADAlumnos', { SADAlumnos: { roles: ['ROLE_STUDENT_ADMIN'] } }, true],
    ['SADAlumnos', { SADAlumnos: { roles: ['ROLE_STUDENT_SUPERADMIN'] } }, true],
    ['SADAlumnos', { SADAlumnos: { roles: ['PERMISSION_STUDENT_ACCESS_STUDENT_MODULE'] } }, true],
    ['SADAdministracion', { SADAdministracion: { roles: ['PERMISSION_ADMINISTRATION_ACCESS'] } }, true],
    ['SADProfesores', { SADProfesores: { roles: ['ROLE_TEACHER_ADMIN'] } }, true],
    ['SADCursos', { SADCursos: { roles: ['ROLE_COURSE_SUPERADMIN'] } }, true],
    ['SADExamenes', { SADExamenes: { roles: ['ROLE_EXAM_ADMIN'] } }, true],
    ['SADActividades', { SADActividades: { roles: ['ROLE_ACTIVITY_ADMIN'] } }, true],
    ['SADAyudantes', { SADAyudantes: { roles: ['ROLE_ASSISTANT_ADMIN'] } }, true],
    ['SADProcesos', { SADProcesos: { roles: ['ROLE_PROCESS_ADMIN'] } }, true],
    ['NewSAD', { NewSAD: { roles: ['PERMISSION_SAD_ACCESS'] } }, true],
    ['SADAlumnos', { SADAlumnos: { roles: ['administrator', 'superadmin', 'STUDENT_ADMIN'] } }, false],
    ['SADAlumnos', { SADAdministracion: { roles: ['ROLE_STUDENT_ADMIN'] } }, false],
    ['SADAlumnos-iis', { SADAlumnos: { roles: ['ROLE_STUDENT_ADMIN'] } }, false],
    ['SADAlumnos', { 'SADAlumnos-iis': { roles: ['ROLE_STUDENT_ADMIN'] } }, false],
    ['SADAlumnos-iis', { 'SADAlumnos-iis': { roles: ['ROLE_STUDENT_ADMIN'] } }, true],
    ['SADInformes', { SADInformes: { roles: ['ROLE_REPORT_ADMIN'] } }, true],
    ['SADInformes', { SADInformes: { roles: ['ROLE_REPORT_SUPERADMIN'] } }, true],
    ['SADInformes', { SADInformes: { roles: ['PERMISSION_REPORT_ACCESS'] } }, true],
    ['SADInformes-iis', { 'SADInformes-iis': { roles: ['PERMISSION_REPORT_ACCESS'] } }, true],
    ['SADInformes', { SADInformes: { roles: ['REPORT_ACCESS', 'REPORT_ADMIN', 'administrator'] } }, false],
    ['SADInformes', { SADInformes: { roles: [] } }, false],
    ['SADInformes', { SADAlumnos: { roles: ['PERMISSION_REPORT_ACCESS'] } }, false],
    ['SADInformes', { 'SADInformes-iis': { roles: ['PERMISSION_REPORT_ACCESS'] } }, false],
    ['SADInformes-iis', { SADInformes: { roles: ['PERMISSION_REPORT_ACCESS'] } }, false],
    ['SADOtro', { SADOtro: { roles: ['ROLE_REPORT_ADMIN'] } }, false],
    ['SADProfesores', { SADProfesores: { roles: ['PERMISSION_TEACHER_ACCESS'] } }, false],
    ['SADAlumnos', { SADAlumnos: { roles: 'ROLE_STUDENT_ADMIN' } }, false],
    ['SADAlumnos', [], false],
    ['', { SADAlumnos: { roles: ['ROLE_STUDENT_ADMIN'] } }, false],
  ])('filtra cliente físico y roles nuevos: %s', async (clientId, resourceAccess, allowed) => {
    adapter.tokenParsed.resource_access = resourceAccess;
    await auth.initialize();
    expect(auth.hasSiteAccess(clientId as string)).toBe(allowed);
  });

  it('actualiza acceso tras renovar y lo retira al cerrar sesión', async () => {
    adapter.tokenParsed.resource_access = { SADAlumnos: { roles: ['ROLE_STUDENT_ADMIN'] } };
    await auth.initialize();
    expect(auth.hasSiteAccess('SADAlumnos')).toBe(true);
    adapter.tokenParsed.resource_access = {};
    await auth.getValidToken();
    expect(auth.hasSiteAccess('SADAlumnos')).toBe(false);
    adapter.authenticated = false;
    adapter.onAuthLogout?.();
    expect(auth.hasSiteAccess('SADAlumnos')).toBe(false);
  });

  it.each([
    [' Usuario Prueba ', 'Usuario Prueba'], [undefined, 'usuario'], [{ invalid: true }, 'usuario'],
  ])('expone nombre público o username como fallback sin almacenarlo: %s', async (name, expected) => {
    adapter.tokenParsed.name = name;
    await auth.initialize();
    expect(auth.name()).toBe(expected);
    await auth.logout();
    expect(auth.name()).toBe('');
  });

  it('refleja el cierre de sesión del adaptador', async () => {
    await auth.initialize();
    adapter.authenticated = false;
    adapter.onAuthLogout?.();
    expect(auth.isAuthenticated()).toBe(false);
    expect(auth.username()).toBe('');
  });
});

describe('Guard e interceptor', () => {
  let auth: { isAuthenticated: ReturnType<typeof vi.fn>; login: ReturnType<typeof vi.fn>; getValidToken: ReturnType<typeof vi.fn> };
  beforeEach(() => {
    auth = {
      isAuthenticated: vi.fn(() => false), login: vi.fn().mockResolvedValue(undefined),
      getValidToken: vi.fn().mockResolvedValue('token-prueba'),
    };
    TestBed.configureTestingModule({ providers: [
      { provide: AuthService, useValue: auth },
      provideHttpClient(withInterceptors([authInterceptor])), provideHttpClientTesting(),
    ] });
  });
  afterEach(() => TestBed.inject(HttpTestingController).verify());

  it('permite la ruta autenticada y envía al login a quien no tiene sesión', async () => {
    const route = {} as ActivatedRouteSnapshot;
    const state = { url: '/?vista=resumen' } as RouterStateSnapshot;
    const run = () => TestBed.runInInjectionContext(() => authGuard(route, state));
    expect(await run()).toBe(false);
    expect(auth.login).toHaveBeenCalledWith(state.url);
    auth.isAuthenticated.mockReturnValue(true);
    expect(await run()).toBe(true);
  });

  it('adjunta el token renovado únicamente a /api/', async () => {
    TestBed.inject(HttpClient).get('/api/auth/me').subscribe();
    await Promise.resolve();
    const request = TestBed.inject(HttpTestingController).expectOne('/api/auth/me');
    expect(request.request.headers.get('Authorization')).toBe('Bearer token-prueba');
    request.flush({ username: 'usuario' });
  });

  it.each(['https://externo.invalid/api/me', '//externo.invalid/api/me', '/assets/logo.svg'])(
    'no expone el token a %s', (url) => {
      TestBed.inject(HttpClient).get(url).subscribe();
      const request = TestBed.inject(HttpTestingController).expectOne(url);
      expect(request.request.headers.has('Authorization')).toBe(false);
      expect(auth.getValidToken).not.toHaveBeenCalled();
      request.flush({});
    },
  );

  it('no envía la petición si la renovación falla', async () => {
    auth.getValidToken.mockRejectedValue(new Error('expirado'));
    const failure = vi.fn();
    TestBed.inject(HttpClient).get('/api/auth/me').subscribe({ error: failure });
    await new Promise<void>((resolve) => setTimeout(resolve, 0));
    TestBed.inject(HttpTestingController).expectNone('/api/auth/me');
    expect(failure).toHaveBeenCalledOnce();
  });
});
