import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { inject, Pipe, PipeTransform } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

@Pipe({ name: 'localizedDate', pure: false })
export class LocalizedDatePipe implements PipeTransform {
  private readonly translate = inject(TranslateService);
  transform(
    value: string | number | Date | null | undefined,
    format = 'mediumDate',
    timezone?: string,
  ): string | null {
    return new DatePipe(this.translate.currentLang() || 'en').transform(value, format, timezone);
  }
}

@Pipe({ name: 'localizedNumber', pure: false })
export class LocalizedNumberPipe implements PipeTransform {
  private readonly translate = inject(TranslateService);
  transform(value: string | number | null | undefined, digits?: string): string | null {
    return new DecimalPipe(this.translate.currentLang() || 'en').transform(value, digits);
  }
}

@Pipe({ name: 'localizedCurrency', pure: false })
export class LocalizedCurrencyPipe implements PipeTransform {
  private readonly translate = inject(TranslateService);
  transform(
    value: string | number | null | undefined,
    code = 'EUR',
    display = 'symbol',
    digits = '1.2-2',
  ): string | null {
    return new CurrencyPipe(this.translate.currentLang() || 'en').transform(
      value,
      code,
      display,
      digits,
    );
  }
}
