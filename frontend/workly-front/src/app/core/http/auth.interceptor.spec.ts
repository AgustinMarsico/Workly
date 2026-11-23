import { TestBed } from '@angular/core/testing';
import { HttpClient, HttpErrorResponse, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { beforeEach, afterEach, describe, expect, it, vi } from 'vitest';
import { APP_ENV } from '../config/app-env';
import { AuthService, isTokenExpired } from '../services/auth.service';
import { WORKLY_SESSION_KEY } from '../services/session.constants';
import { authInterceptor } from './auth.interceptor';

function token(exp: number): string {
  return 'test.' + btoa(JSON.stringify({ exp, role: 'Admin' })) + '.signature';
}

describe('Session expiration', () => {
  let http: HttpClient;
  let requests: HttpTestingController;
  let auth: AuthService;
  const navigateByUrl = vi.fn().mockResolvedValue(true);
  const url = APP_ENV.apiBaseUrl + '/api/companies';

  beforeEach(() => {
    localStorage.clear();
    navigateByUrl.mockClear();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: Router, useValue: { navigateByUrl } },
      ],
    });
    http = TestBed.inject(HttpClient);
    requests = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    requests.verify();
    localStorage.clear();
  });

  function session(value = token(Date.now() / 1000 + 3600)) {
    localStorage.setItem(WORKLY_SESSION_KEY, JSON.stringify({ token: value, email: 'test@example.com', role: 'Admin' }));
    auth = TestBed.inject(AuthService);
    return value;
  }

  it('rejects expired, malformed and missing-expiration tokens', () => {
    expect(isTokenExpired(token(Date.now() / 1000 - 1))).toBe(true);
    expect(isTokenExpired('invalid')).toBe(true);
    expect(isTokenExpired('test.' + btoa('{}') + '.signature')).toBe(true);
    expect(isTokenExpired(token(Date.now() / 1000 + 3600))).toBe(false);
  });

  it('clears an expired session restored from storage', () => {
    session(token(Date.now() / 1000 - 1));
    expect(auth.isAuthenticated()).toBe(false);
    expect(auth.session()).toBeNull();
    expect(localStorage.getItem(WORKLY_SESSION_KEY)).toBeNull();
  });

  it('attaches a valid token to API requests', () => {
    const value = session();
    http.get(url).subscribe();
    const request = requests.expectOne(url);
    expect(request.request.headers.get('Authorization')).toBe('Bearer ' + value);
    request.flush([]);
  });

  it('clears state and returns to login when an authenticated API returns 401', () => {
    session();
    let failure: HttpErrorResponse | undefined;
    http.get(url).subscribe({ error: (error) => { failure = error; } });
    requests.expectOne(url).flush({}, { status: 401, statusText: 'Unauthorized' });
    expect(failure?.status).toBe(401);
    expect(auth.session()).toBeNull();
    expect(localStorage.getItem(WORKLY_SESSION_KEY)).toBeNull();
    expect(navigateByUrl).toHaveBeenCalledWith('/login');
  });

  it('blocks a token that expires while the app is open', () => {
    session();
    auth.session.update((current) => ({ ...current!, token: token(Date.now() / 1000 - 1) }));
    http.get(url).subscribe({ error: () => {} });
    requests.expectNone(url);
    expect(auth.session()).toBeNull();
    expect(navigateByUrl).toHaveBeenCalledWith('/login');
  });

  it('does not log out for forbidden or server errors', () => {
    session();
    for (const status of [403, 500]) {
      http.get(url).subscribe({ error: () => {} });
      requests.expectOne(url).flush({}, { status, statusText: 'Error' });
    }
    expect(auth.isAuthenticated()).toBe(true);
    expect(navigateByUrl).not.toHaveBeenCalled();
  });

  it('keeps invalid login errors on the login page without forwarding an old token', () => {
    session();
    const login = APP_ENV.identityApiBaseUrl + '/api/auth/login';
    http.post(login, {}).subscribe({ error: () => {} });
    const request = requests.expectOne(login);
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush({}, { status: 401, statusText: 'Unauthorized' });
    expect(navigateByUrl).not.toHaveBeenCalled();
  });

  it('does not send tokens to other origins', () => {
    session();
    const external = 'https://example.com/api';
    http.get(external).subscribe();
    const request = requests.expectOne(external);
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush({});
  });

  it('ignores a delayed 401 belonging to a previous session', () => {
    session();
    http.get(url).subscribe({ error: () => {} });
    const request = requests.expectOne(url);
    auth.session.update((current) => ({ ...current!, token: token(Date.now() / 1000 + 7200) }));
    request.flush({}, { status: 401, statusText: 'Unauthorized' });
    expect(auth.isAuthenticated()).toBe(true);
    expect(navigateByUrl).not.toHaveBeenCalled();
  });
});
