import {
  ApplicationConfig,
  inject,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { provideTranslateService } from '@ngx-translate/core';
import { provideTranslateHttpLoader } from '@ngx-translate/http-loader';
import { LanguageService } from './core/services/language.service';
import { registerLocaleData } from '@angular/common';
import bgLocale from '@angular/common/locales/bg';

registerLocaleData(bgLocale);
import { provideHttpClient } from '@angular/common/http';
import { provideRouter, TitleStrategy, withInMemoryScrolling } from '@angular/router';
import { TranslatedTitleStrategy } from './core/services/translated-title.strategy';
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
    provideTranslateService({
      fallbackLang: 'en',
      loader: provideTranslateHttpLoader({
        prefix: './assets/i18n/',
        suffix: '.json',
        failOnError: true,
      }),
    }),
    provideAppInitializer(() => inject(LanguageService).initialize()),
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withInMemoryScrolling({ scrollPositionRestoration: 'enabled' })),
    { provide: TitleStrategy, useClass: TranslatedTitleStrategy },
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
