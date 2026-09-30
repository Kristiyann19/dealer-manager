import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { ThemeService } from './theme.service';

describe('ThemeService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({});
    localStorage.removeItem('dealer-theme');
  });

  afterEach(() => {
    vi.restoreAllMocks();
    document.documentElement.classList.remove('app-dark');
    document.documentElement.style.removeProperty('color-scheme');
    localStorage.removeItem('dealer-theme');
  });

  it('starts in light mode without a saved preference', () => {
    const service = TestBed.inject(ThemeService);
    service.initialize();
    expect(service.isDark()).toBe(false);
    expect(document.documentElement.style.colorScheme).toBe('light');
  });

  it('restores dark mode from the saved preference', () => {
    localStorage.setItem('dealer-theme', 'dark');
    const service = TestBed.inject(ThemeService);
    service.initialize();
    expect(service.isDark()).toBe(true);
    expect(document.documentElement.classList.contains('app-dark')).toBe(true);
    expect(document.documentElement.style.colorScheme).toBe('dark');
  });

  it('applies and saves both theme choices', () => {
    const service = TestBed.inject(ThemeService);
    service.setDark(true);
    expect(localStorage.getItem('dealer-theme')).toBe('dark');
    expect(document.documentElement.classList.contains('app-dark')).toBe(true);
    service.setDark(false);
    expect(service.isDark()).toBe(false);
    expect(localStorage.getItem('dealer-theme')).toBe('light');
    expect(document.documentElement.classList.contains('app-dark')).toBe(false);
  });

  it('works even when browser storage is blocked', () => {
    vi.spyOn(Storage.prototype, 'getItem').mockImplementation(() => {
      throw new Error('blocked');
    });
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new Error('blocked');
    });
    const service = TestBed.inject(ThemeService);
    expect(() => service.initialize()).not.toThrow();
    expect(() => service.setDark(true)).not.toThrow();
    expect(service.isDark()).toBe(true);
    expect(document.documentElement.classList.contains('app-dark')).toBe(true);
  });
});
