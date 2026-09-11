import { Injectable, signal } from '@angular/core';
import Keycloak from 'keycloak-js';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly keycloak = new Keycloak({
    url: 'http://localhost:8080',
    realm: 'orderflow',
    clientId: 'orderflow-web'
  });

  readonly authenticated = signal(false);
  readonly username = signal<string | null>(null);
  readonly identityAvailable = signal(true);

  async initialize(): Promise<boolean> {
    try {
      const authenticated = await this.keycloak.init({
        onLoad: 'check-sso',
        pkceMethod: 'S256',
        checkLoginIframe: false
      });

      this.updateState(authenticated);
      this.keycloak.onAuthSuccess = () => this.updateState(true);
      this.keycloak.onAuthLogout = () => this.updateState(false);
      this.keycloak.onAuthRefreshError = () => this.updateState(false);

      return authenticated;
    } catch {
      this.identityAvailable.set(false);
      this.updateState(false);
      return false;
    }
  }

  isAdministrator(): boolean {
    return this.authenticated() && this.keycloak.hasRealmRole('administrator');
  }

  async login(redirectUri = window.location.href): Promise<void> {
    await this.keycloak.login({ redirectUri });
  }

  async logout(): Promise<void> {
    await this.keycloak.logout({ redirectUri: window.location.origin });
  }

  async getValidToken(): Promise<string | null> {
    if (!this.keycloak.authenticated) {
      return null;
    }

    await this.keycloak.updateToken(30);
    return this.keycloak.token ?? null;
  }

  private updateState(authenticated: boolean): void {
    this.authenticated.set(authenticated);
    this.username.set(authenticated
      ? this.keycloak.tokenParsed?.['preferred_username'] as string | undefined ?? null
      : null);
  }
}
