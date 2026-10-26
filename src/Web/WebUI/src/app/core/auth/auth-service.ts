import { Injectable, InjectionToken, inject, signal } from '@angular/core';
import Keycloak from 'keycloak-js';
import { hasClientAccess } from './site-access';

export interface PublicKeycloakConfig { url: string; realm: string; clientId: string; }
export const KEYCLOAK_FACTORY = new InjectionToken<(config: PublicKeycloakConfig) => Keycloak>(
  'KEYCLOAK_FACTORY', { providedIn: 'root', factory: () => (config) => new Keycloak(config) },
);

function isConfiguration(value: unknown): value is PublicKeycloakConfig {
  if (typeof value !== 'object' || value === null) return false;
  return ['url', 'realm', 'clientId'].every((key) =>
    key in value && typeof Reflect.get(value, key) === 'string' && Reflect.get(value, key).trim());
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly createKeycloak = inject(KEYCLOAK_FACTORY);
  private keycloak?: Keycloak;
  private readonly authenticated = signal(false);
  private readonly user = signal('');
  private readonly resourceAccess = signal<unknown>(undefined);
  readonly isAuthenticated = this.authenticated.asReadonly();
  readonly username = this.user.asReadonly();

  async initialize(): Promise<void> {
    const response = await fetch('/api/config/keycloak');
    const configuration: unknown = await response.json();
    if (!response.ok || !isConfiguration(configuration))
      throw new Error('La configuración de Keycloak es inválida.');

    const keycloak = this.createKeycloak(configuration);
    this.keycloak = keycloak;
    keycloak.onAuthLogout = () => this.syncSession();
    keycloak.onTokenExpired = () => {
      void this.getValidToken().catch(() => undefined);
    };
    await keycloak.init({
      onLoad: 'check-sso', pkceMethod: 'S256', responseMode: 'query', checkLoginIframe: false,
    });
    this.syncSession();
  }

  async login(returnUrl = '/'): Promise<void> {
    const redirect = new URL(returnUrl, window.location.origin);
    if (redirect.origin !== window.location.origin)
      throw new Error('El destino de acceso debe pertenecer a esta aplicación.');
    await this.requireKeycloak().login({ redirectUri: redirect.href });
  }

  async logout(): Promise<void> {
    await this.requireKeycloak().logout({ redirectUri: window.location.origin + '/' });
    this.authenticated.set(false);
    this.user.set('');
    this.resourceAccess.set(undefined);
  }

  hasSiteAccess(clientId: string): boolean {
    return this.isAuthenticated() && hasClientAccess(this.resourceAccess(), clientId);
  }

  async getValidToken(): Promise<string | undefined> {
    const keycloak = this.requireKeycloak();
    if (!keycloak.authenticated) {
      this.syncSession();
      return undefined;
    }
    try {
      await keycloak.updateToken(30);
      this.syncSession();
      return keycloak.token;
    } catch {
      keycloak.clearToken();
      this.syncSession();
      throw new Error('La sesión ha expirado. Inicia sesión nuevamente.');
    }
  }

  private requireKeycloak(): Keycloak {
    if (!this.keycloak) throw new Error('Keycloak no está inicializado.');
    return this.keycloak;
  }

  private syncSession(): void {
    this.authenticated.set(this.keycloak?.authenticated === true);
    this.resourceAccess.set(this.authenticated() ? this.keycloak?.tokenParsed?.['resource_access'] : undefined);
    this.user.set(this.authenticated() ? this.keycloak?.tokenParsed?.['preferred_username'] ?? '' : '');
  }
}
