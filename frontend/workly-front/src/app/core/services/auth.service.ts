import { inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, map, of, throwError } from 'rxjs';
import { IdentityApiClient } from './api-client.service';
import { StorageService } from './storage.service';
import { WORKLY_SESSION_KEY } from './session.constants';
import { Session } from '../models/session';
import { UserRole } from '../models/user-role';

type LoginResponse =
  | {
      token: string;
    }
  | string;

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly api = inject(IdentityApiClient);
  private readonly storage = inject(StorageService);
  private readonly router = inject(Router);

  readonly session = signal<Session | null>(this.storage.get<Session>(WORKLY_SESSION_KEY));

  readonly isAuthenticated = () => !!this.session()?.token;
  readonly role = () => this.session()?.role;

  login(email: string, password: string) {
    return this.api.post<LoginResponse>('/api/auth/login', { email, password }).pipe(
      map((res) => this.normalizeLoginResponse(res, email)),
      map((nextSession) => {
        this.storage.set(WORKLY_SESSION_KEY, nextSession);
        this.session.set(nextSession);
        return nextSession;
      }),
    );
  }

  logout() {
    this.storage.remove(WORKLY_SESSION_KEY);
    this.session.set(null);
    void this.router.navigateByUrl('/login');
  }

  routeAfterLogin(role: UserRole | undefined) {
    if (role === 'Admin') return '/admin';
    return '/app/dashboard';
  }

  private normalizeLoginResponse(res: LoginResponse, emailInput: string): Session {
    const token = typeof res === 'string' ? res : res.token;
    const payload = safeDecodeJwtPayload(token);

    const role: UserRole = inferRole(payload);

    const email = emailInput || payload?.email || payload?.sub || 'unknown';

    const companyId = payload?.CompanyId ?? payload?.companyId;

    return { token, role, email, companyId };
  }
}

function safeDecodeJwtPayload(token: string): any {
  try {
    const parts = token.split('.');
    if (parts.length < 2) return null;
    const payload = parts[1];
    const normalized = payload.replace(/-/g, '+').replace(/_/g, '/');
    const padded = normalized.padEnd(normalized.length + ((4 - (normalized.length % 4)) % 4), '=');
    const json = atob(padded);
    return JSON.parse(json);
  } catch {
    return null;
  }
}

function inferRole(payload: any): UserRole {
  const possible =
    payload?.role ??
    payload?.roles ??
    payload?.['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];

  const first = Array.isArray(possible) ? possible[0] : possible;
  const asString = typeof first === 'string' ? first : '';

  if (asString.toLowerCase().includes('admin')) return 'Admin';
  return 'Member';
}
