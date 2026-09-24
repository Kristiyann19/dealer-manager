import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter, withInMemoryScrolling } from '@angular/router';
import { providePrimeNG } from 'primeng/config';
import Aura from '@primeuix/themes/aura';
import { definePreset } from '@primeuix/themes';
import {
  provideLucideIcons,
  LucideArrowLeft,
  LucideArrowUpRight,
  LucideBell,
  LucideCarFront,
  LucideChartNoAxesCombined,
  LucideChevronRight,
  LucideCircleCheck,
  LucideClipboardList,
  LucideClock,
  LucideHandshake,
  LucideLandmark,
  LucideLayoutDashboard,
  LucideListChecks,
  LucideMenu,
  LucidePlus,
  LucideReceiptText,
  LucideRefreshCw,
  LucideSearch,
  LucideSettings,
  LucideTag,
  LucideTrendingUp,
  LucideTruck,
  LucideUsers,
  LucideWallet,
  LucideWarehouse,
  LucideWrench,
} from '@lucide/angular';
import { routes } from './app.routes';

const AutoCapitalTheme = definePreset(Aura, {
  semantic: {
    primary: {
      50: '{blue.50}',
      100: '{blue.100}',
      200: '{blue.200}',
      300: '{blue.300}',
      400: '{blue.400}',
      500: '{blue.500}',
      600: '{blue.600}',
      700: '{blue.700}',
      800: '{blue.800}',
      900: '{blue.900}',
      950: '{blue.950}',
    },
  },
});

export const appConfig: ApplicationConfig = {
  providers: [
    provideHttpClient(),
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withInMemoryScrolling({ scrollPositionRestoration: 'enabled' })),
    providePrimeNG({
      theme: {
        preset: AutoCapitalTheme,
        options: {
          darkModeSelector: false,
          cssLayer: { name: 'primeng', order: 'theme, base, primeng, components, utilities' },
        },
      },
    }),
    provideLucideIcons(
      LucideArrowLeft,
      LucideArrowUpRight,
      LucideBell,
      LucideCarFront,
      LucideChartNoAxesCombined,
      LucideChevronRight,
      LucideCircleCheck,
      LucideClipboardList,
      LucideClock,
      LucideHandshake,
      LucideLandmark,
      LucideLayoutDashboard,
      LucideListChecks,
      LucideMenu,
      LucidePlus,
      LucideReceiptText,
      LucideRefreshCw,
      LucideSearch,
      LucideSettings,
      LucideTag,
      LucideTrendingUp,
      LucideTruck,
      LucideUsers,
      LucideWallet,
      LucideWarehouse,
      LucideWrench,
    ),
  ],
};
