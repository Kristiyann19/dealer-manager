import { provideLucideIcons, LucideLayoutDashboard, LucideCarFront, LucideClipboardList, LucideHandshake, LucideUsers, LucideWallet, LucideChartNoAxesCombined, LucideListChecks, LucideSettings } from '@lucide/angular';
import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { provideTranslateService, TranslateService } from '@ngx-translate/core';
import { SidebarComponent } from '../layout/sidebar/sidebar.component';
import { AuthService } from './auth.service';
import { authInterceptor } from '../../interceptors/auth.interceptor';
import { authGuard, guestGuard } from '../../auth-guards/auth.guard';
import { AuthPageComponent } from '../../features/auth/auth-page.component';
import en from '../../../assets/i18n/en.json';

@Component({ template: '' })
class Page {}
const user = {
  id: 1,
  email: 'owner@example.test',
  dealership: { id: 2, name: 'BM Auto' },
  roles: ['Owner'],
};
const credentials = { email: user.email, password: 'Secure!Password123' };
describe('Cookie authentication', () => {
  let auth: AuthService;
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideLucideIcons(LucideLayoutDashboard, LucideCarFront, LucideClipboardList, LucideHandshake, LucideUsers, LucideWallet, LucideChartNoAxesCombined, LucideListChecks, LucideSettings),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        provideTranslateService(),
        provideRouter([
          { path: 'login', component: Page, canActivate: [guestGuard] },
          {
            path: 'register',
            component: AuthPageComponent,
            data: { register: true },
            canActivate: [guestGuard],
          },
          { path: 'dashboard', component: Page, canActivate: [authGuard] },
        ]),
      ],
    });
    auth = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('en', en);
    translate.use('en').subscribe();
  });
  afterEach(() => http.verify());
  function csrf() {
    http.expectOne('/api/auth/csrf').flush({ token: 'csrf-test' });
  }
  function initialize(signedIn = true) {
    auth.initialize().subscribe();
    csrf();
    const request = http.expectOne('/api/auth/me');
    if (signedIn) request.flush(user);
    else request.flush(null, { status: 401, statusText: 'Unauthorized' });
  }
  it('restores a session from me with one shared initialization', () => {
    auth.initialize().subscribe();
    auth.initialize().subscribe();
    expect(auth.initialized()).toBe(false);
    csrf();
    http.expectOne('/api/auth/me').flush(user);
    expect(auth.currentUser()).toEqual(user);
    expect(auth.initialized()).toBe(true);
  });
  it('treats initial me 401 as anonymous without a global error', () => {
    initialize(false);
    expect(auth.isAuthenticated()).toBe(false);
    expect(auth.notice()).toBeNull();
    expect(auth.initialized()).toBe(true);
  });
  it('can retry a failed bootstrap without falsely initializing', () => {
    auth.initialize().subscribe();
    http.expectOne('/api/auth/csrf').flush(null, { status: 503, statusText: 'Unavailable' });
    expect(auth.initialized()).toBe(false);
    expect(auth.initializationError()).toBe(true);
    initialize();
    expect(auth.initializationError()).toBe(false);
    expect(auth.isAuthenticated()).toBe(true);
  });
  for (const action of ['login', 'register'] as const) {
    it(`${action} renews CSRF and loads authoritative me before setting user`, () => {
      const storage = vi.spyOn(Storage.prototype, 'setItem');
      const request =
        action === 'login'
          ? auth.login(credentials)
          : auth.register({
              ...credentials,
              confirmPassword: credentials.password,
              dealershipName: 'BM Auto',
            });
      request.subscribe();
      csrf();
      http.expectOne(`/api/auth/${action}`).flush(user);
      expect(auth.currentUser()).toBeNull();
      csrf();
      http.expectOne('/api/auth/me').flush(user);
      expect(auth.currentUser()).toEqual(user);
      expect(storage).not.toHaveBeenCalled();
      storage.mockRestore();
    });
  }
  it('leaves failed login anonymous', () => {
    let failed = false;
    auth.login(credentials).subscribe({ error: () => (failed = true) });
    csrf();
    http.expectOne('/api/auth/login').flush({}, { status: 401, statusText: 'Unauthorized' });
    expect(failed).toBe(true);
    expect(auth.currentUser()).toBeNull();
  });
  it('logs out, refreshes CSRF and clears state', () => {
    initialize();
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    auth.logout().subscribe();
    csrf();
    http.expectOne('/api/auth/logout').flush(null);
    csrf();
    expect(auth.currentUser()).toBeNull();
    expect(navigate).toHaveBeenCalledWith('/login');
  });
  it('clears state even if CSRF refresh fails after successful logout', () => {
    initialize();
    vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    auth.logout().subscribe();
    csrf();
    http.expectOne('/api/auth/logout').flush(null);
    http.expectOne('/api/auth/csrf').flush(null, { status: 503, statusText: 'Unavailable' });
    expect(auth.currentUser()).toBeNull();
  });
  it('redirects anonymous visitors to login', async () => {
    initialize(false);
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/dashboard');
    expect(TestBed.inject(Router).url).toBe('/login');
  });
  it('allows dashboard and redirects signed-in visitors away from login', async () => {
    initialize();
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/dashboard');
    expect(TestBed.inject(Router).url).toBe('/dashboard');
    await harness.navigateByUrl('/login');
    expect(TestBed.inject(Router).url).toBe('/dashboard');
  });
  it('redirects only once for concurrent protected API 401s without component errors', () => {
    initialize();
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    const client = TestBed.inject(HttpClient);
    let errors = 0;
    client.get('/api/vehicles').subscribe({ error: () => errors++ });
    client.get('/api/dashboard').subscribe({ error: () => errors++ });
    http.expectOne('/api/vehicles').flush(null, { status: 401, statusText: 'Unauthorized' });
    http.expectOne('/api/dashboard').flush(null, { status: 401, statusText: 'Unauthorized' });
    expect(auth.currentUser()).toBeNull();
    expect(navigate).toHaveBeenCalledTimes(1);
    expect(errors).toBe(0);
  });
  it('keeps session for 403 and CSRF 400 without retrying a mutation', () => {
    initialize();
    const client = TestBed.inject(HttpClient);
    client.get('/api/vehicles').subscribe({ error: () => {} });
    http.expectOne('/api/vehicles').flush(null, { status: 403, statusText: 'Forbidden' });
    expect(auth.notice()).toBe('auth.errors.forbidden');
    expect(auth.isAuthenticated()).toBe(true);
    client.post('/api/vehicles', {}).subscribe({ error: () => {} });
    http.expectOne('/api/vehicles').flush({}, { status: 400, statusText: 'Bad Request' });
    expect(auth.isAuthenticated()).toBe(true);
  });
  it('validates register passwords and dealership fields before submit', async () => {
    initialize(false);
    const harness = await RouterTestingHarness.create();
    const page = await harness.navigateByUrl('/register', AuthPageComponent);
    expect(page.form.invalid).toBe(true);
    page.form.setValue({ ...credentials, dealershipName: 'BM Auto', confirmPassword: 'mismatch' });
    expect(page.form.invalid).toBe(true);
    page.form.controls.confirmPassword.setValue(credentials.password);
    expect(page.form.valid).toBe(true);
    page.form.controls.dealershipName.setValue('   ');
    expect(page.form.invalid).toBe(true);
  });
  it('uses the readable CSRF cookie for unsafe relative API calls', () => {
    document.cookie = 'XSRF-TOKEN=browser-csrf; path=/';
    const client = TestBed.inject(HttpClient);
    client.post('/api/candidates', {}).subscribe();
    const request = http.expectOne('/api/candidates');
    expect(request.request.headers.get('X-XSRF-TOKEN')).toBe('browser-csrf');
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush({});
    document.cookie = 'XSRF-TOKEN=; max-age=0; path=/';
  });
  it('shows the current dealership and email in the actual sidebar', () => {
    initialize();
    const fixture = TestBed.createComponent(SidebarComponent);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('BM Auto');
    expect(fixture.nativeElement.textContent).toContain(user.email);
    expect(fixture.nativeElement.textContent).toContain('Owner');
    expect(fixture.nativeElement.textContent).not.toContain('Boris Marinov');
  });
  it('handles an expired session during logout', () => {
    initialize();
    vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    auth.logout().subscribe();
    csrf();
    http.expectOne('/api/auth/logout').flush(null, { status: 401, statusText: 'Unauthorized' });
    csrf();
    expect(auth.currentUser()).toBeNull();
  });
});
