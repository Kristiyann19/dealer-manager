import { InjectionToken } from '@angular/core';

// Development uses proxy.conf.cjs. Production should reverse-proxy /api to the WebAPI.
export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL', {
  providedIn: 'root',
  factory: () => '/api',
});
