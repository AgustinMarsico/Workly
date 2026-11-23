import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { APP_ENV } from '../config/app-env';
import { AuthService, isTokenExpired } from '../services/auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const isApiRequest = Object.values(APP_ENV).some((base) =>
    req.url.startsWith(base.replace(/\/$/, '') + '/'),
  );
  const isLogin = req.url.split('?')[0] === APP_ENV.identityApiBaseUrl.replace(/\/$/, '') + '/api/auth/login';
  if (!isApiRequest || isLogin) return next(req);

  const auth = inject(AuthService);
  const token = auth.session()?.token;
  if (!token) return next(req);

  if (isTokenExpired(token)) {
    auth.logout();
    return throwError(() => new HttpErrorResponse({ status: 401, statusText: 'Session expired', url: req.url }));
  }

  return next(
    req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`,
      },
    }),
  ).pipe(
    catchError((error: unknown) => {
      // Una respuesta antigua no debe cerrar una sesión iniciada posteriormente.
      if (error instanceof HttpErrorResponse && error.status === 401 && auth.session()?.token === token) {
        auth.logout();
      }
      return throwError(() => error);
    }),
  );
};
