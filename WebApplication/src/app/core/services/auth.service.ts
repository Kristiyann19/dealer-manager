import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import {
  Observable,
  catchError,
  finalize,
  map,
  of,
  shareReplay,
  switchMap,
  tap,
  throwError,
} from 'rxjs';
import { CurrentUser, LoginRequest, RegisterRequest } from '../models/auth.models';
import { API_BASE_URL } from '../../configuration/api.config';
import { WorkspaceService } from './workspace.service';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly workspace = inject(WorkspaceService);
  private readonly url = `${inject(API_BASE_URL)}/auth`;
  private readonly user = signal<CurrentUser | null>(null);
  readonly currentUser = this.user.asReadonly();
  readonly initialized = signal(false);
  readonly initializationError = signal(false);
  readonly busy = signal(false);
  readonly notice = signal<string | null>(null);
  readonly isAuthenticated = computed(() => this.user() !== null);
  readonly initials = computed(() => this.user()?.email.slice(0, 2).toUpperCase() ?? '');
  private initialization?: Observable<boolean>;

  roleLabel(role: string) {
    return role === 'Owner' ? 'auth.owner' : role;
  }
  bootstrapCsrf() {
    return this.http.get<{ token: string }>(`${this.url}/csrf`);
  }
  refreshCurrentUser() {
    return this.http.get<CurrentUser>(`${this.url}/me`).pipe(
      catchError((error: HttpErrorResponse) =>
        error.status === 401 ? of(null) : throwError(() => error),
      ),
      tap((user) => this.user.set(user)),
    );
  }
  initialize(): Observable<boolean> {
    if (this.initialized()) return of(true);
    if (!this.initialization) {
      this.initializationError.set(false);
      this.initialization = this.bootstrapCsrf().pipe(
        switchMap(() => this.refreshCurrentUser()),
        tap(() => this.initialized.set(true)),
        map(() => true),
        catchError(() => {
          this.initializationError.set(true);
          return of(false);
        }),
        finalize(() => (this.initialization = undefined)),
        shareReplay({ bufferSize: 1, refCount: false }),
      );
    }
    return this.initialization;
  }
  login(request: LoginRequest) {
    return this.signIn('login', request);
  }
  register(request: RegisterRequest) {
    return this.signIn('register', request);
  }
  private signIn(action: string, request: LoginRequest | RegisterRequest) {
    this.notice.set(null);
    return this.bootstrapCsrf().pipe(
      switchMap(() => this.http.post<CurrentUser>(`${this.url}/${action}`, request)),
      switchMap(() => this.bootstrapCsrf()),
      switchMap(() => this.refreshCurrentUser()),
      tap((user) => {
        if (user) {
          this.initialized.set(true);
          this.workspace.searchQuery.set('');
        }
      }),
    );
  }
  logout() {
    return this.bootstrapCsrf().pipe(
      switchMap(() =>
        this.http
          .post<void>(`${this.url}/logout`, null)
          .pipe(
            catchError((error: HttpErrorResponse) =>
              error.status === 401 ? of(null) : throwError(() => error),
            ),
          ),
      ),
      switchMap(() =>
        this.bootstrapCsrf().pipe(
          // The server has already signed out even if CSRF renewal fails.
          catchError(() => {
            this.notice.set('auth.errors.generic');
            return of(null);
          }),
        ),
      ),
      tap(() => {
        this.clear();
        void this.router.navigateByUrl('/login');
      }),
    );
  }
  logoutFromMenu() {
    if (this.busy()) return;
    this.busy.set(true);
    this.notice.set(null);
    this.logout()
      .pipe(finalize(() => this.busy.set(false)))
      .subscribe({
        error: () => this.notice.set('auth.errors.generic'),
      });
  }
  sessionExpired() {
    const hadSession = this.isAuthenticated();
    this.clear();
    if (hadSession) void this.router.navigateByUrl('/login');
  }
  private clear() {
    this.user.set(null);
    this.workspace.searchQuery.set('');
  }
}
