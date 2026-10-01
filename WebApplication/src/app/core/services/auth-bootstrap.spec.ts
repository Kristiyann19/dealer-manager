import { ApplicationInitStatus } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { appConfig } from '../../app.config';
import { AuthService } from './auth.service';
import { LanguageService } from './language.service';
import en from '../../../assets/i18n/en.json';
import bg from '../../../assets/i18n/bg.json';

describe('Application auth and translation bootstrap', () => {
  it('loads real fallback translations and initializes Router before the auth session', async () => {
    localStorage.removeItem('dealer-language');
    TestBed.configureTestingModule({ providers: [...appConfig.providers, provideHttpClientTesting()] });
    const http = TestBed.inject(HttpTestingController);
    const translations = TestBed.inject(TranslateService);
    expect(TestBed.inject(Router)).toBeTruthy();
    http.expectOne('./assets/i18n/en.json').flush(en);
    http.expectOne('./assets/i18n/bg.json').flush(bg);
    await TestBed.inject(ApplicationInitStatus).donePromise;
    expect(TestBed.inject(LanguageService).error()).toBe(false);
    await new Promise<void>((resolve) => translations.use('en').subscribe(() => resolve()));
    expect(translations.instant('auth.login.title')).toBe(en.auth.login.title);

    const auth = TestBed.inject(AuthService);
    auth.initialize().subscribe();
    http.expectOne('/api/auth/csrf').flush({ token: 'bootstrap-token' });
    http.expectOne('/api/auth/me').flush(null, { status: 401, statusText: 'Unauthorized' });
    expect(auth.initialized()).toBe(true);
    expect(auth.isAuthenticated()).toBe(false);
    expect(auth.initializationError()).toBe(false);
    http.verify();
    localStorage.removeItem('dealer-language');
  });
});
