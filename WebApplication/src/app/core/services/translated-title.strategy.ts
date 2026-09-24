import { inject, Injectable } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { RouterStateSnapshot, TitleStrategy } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';

@Injectable()
export class TranslatedTitleStrategy extends TitleStrategy {
  private readonly title = inject(Title);
  private readonly translate = inject(TranslateService);
  override updateTitle(snapshot: RouterStateSnapshot) {
    const key = this.buildTitle(snapshot);
    if (key) this.title.setTitle(`${this.translate.instant(key)} | AutoCapital`);
  }
}
