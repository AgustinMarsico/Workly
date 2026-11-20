import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';
import { UserRole } from '../models/user-role';

export function roleGuard(required: UserRole): CanActivateFn {
  return () => {
    const auth = inject(AuthService);
    const router = inject(Router);

    if (!auth.isAuthenticated()) return router.createUrlTree(['/login']);

    if (auth.role() === required) return true;

    return router.parseUrl(auth.routeAfterLogin(auth.role()));
  };
}
