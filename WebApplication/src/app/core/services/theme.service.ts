import { DOCUMENT } from '@angular/common';
import { inject, Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly document = inject(DOCUMENT);
  private readonly dark = signal(false);
  readonly isDark = this.dark.asReadonly();

  initialize(): void {
    let dark = false;
    try {
      dark = this.document.defaultView?.localStorage.getItem('dealer-theme') === 'dark';
    } catch {
      // Storage may be unavailable; the theme still works for this session.
    }
    this.apply(dark);
  }

  setDark(dark: boolean): void {
    this.apply(dark);
    try {
      this.document.defaultView?.localStorage.setItem('dealer-theme', dark ? 'dark' : 'light');
    } catch {
      // Keep the selected theme even when the browser cannot persist it.
    }
  }

  private apply(dark: boolean): void {
    this.dark.set(dark);
    this.document.documentElement.classList.toggle('app-dark', dark);
    this.document.documentElement.style.colorScheme = dark ? 'dark' : 'light';
  }
}
