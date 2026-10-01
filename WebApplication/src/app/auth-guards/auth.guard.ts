import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs';
import { AuthService } from '../core/services/auth.service';
export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService); const router = inject(Router);
  return auth.initialize().pipe(map(ready => !ready ? false : auth.isAuthenticated() ? true : router.parseUrl('/login')));
};
export const guestGuard: CanActivateFn = () => {
  const auth = inject(AuthService); const router = inject(Router);
  return auth.initialize().pipe(map(ready => !ready ? false : auth.isAuthenticated() ? router.parseUrl('/dashboard') : true));
};
