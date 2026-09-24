import { DOCUMENT } from '@angular/common';
import { effect, inject, Injectable, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { Router, TitleStrategy } from '@angular/router';
import { PrimeNG } from 'primeng/config';
import { firstValueFrom } from 'rxjs';

export type Language = 'bg' | 'en';

@Injectable({ providedIn: 'root' })
export class LanguageService {
  private readonly translate = inject(TranslateService);
  private readonly document = inject(DOCUMENT);
  private readonly router = inject(Router);
  private readonly titles = inject(TitleStrategy);
  private readonly primeNG = inject(PrimeNG);
  readonly current = this.translate.currentLang;
  readonly loading = signal(false);
  readonly error = signal(false);

  constructor() {
    effect(() => {
      const lang = this.current();
      if (!lang) return;
      this.document.documentElement.lang = lang;
      this.updateTitle();
      this.primeNG.setTranslation(this.translate.instant('primeNG'));
    });
  }

  async initialize() {
    let lang: Language = 'bg';
    try {
      if (this.document.defaultView?.localStorage.getItem('dealer-language') === 'en') lang = 'en';
    } catch {
      /* Storage can be disabled. */
    }
    await this.change(lang);
  }

  async change(lang: Language) {
    if (this.loading()) return;
    this.loading.set(true);
    this.error.set(false);
    try {
      await firstValueFrom(this.translate.use(lang));
      try {
        this.document.defaultView?.localStorage.setItem('dealer-language', lang);
      } catch {
        /* Translation still works without storage. */
      }
    } catch {
      this.error.set(true);
    } finally {
      this.loading.set(false);
    }
  }

  private updateTitle() {
    this.titles.updateTitle(this.router.routerState.snapshot);
  }
}
