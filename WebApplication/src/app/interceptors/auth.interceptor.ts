import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject, Injector } from '@angular/core';
import { EMPTY, catchError, throwError } from 'rxjs';
import { AuthService } from '../core/services/auth.service';
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  // Translation loading also uses HttpClient during Router/TitleStrategy creation.
  // It must never instantiate AuthService (which depends on Router).
  if (!request.url.startsWith('/api/') || request.url.startsWith('/api/auth/')) {
    return next(request);
  }
  const injector = inject(Injector);
  return next(request).pipe(catchError((error: HttpErrorResponse) => {
    if (error.status === 401) {
      injector.get(AuthService).sessionExpired();
      return EMPTY;
    }
    if (error.status === 403) injector.get(AuthService).notice.set('auth.errors.forbidden');
    return throwError(() => error);
  }));
};
