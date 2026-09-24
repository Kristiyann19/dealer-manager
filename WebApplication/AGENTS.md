# Frontend translations

- All application-owned user-facing text belongs in both `src/assets/i18n/bg.json` and `src/assets/i18n/en.json` under matching semantic keys.
- In standalone component templates, import `TranslatePipe` and render text using `{{ 'feature.key' | translate }}`. Translate placeholders, accessibility labels, tooltips, validation messages and button labels through property bindings too.
- Use interpolation parameters for variable text, for example `{{ 'candidate.reviewLabel' | translate: { vehicle: candidate.make } }}`. Do not assemble translated sentences by concatenating fragments.
- Keep API enum values and user-entered data unchanged. Map enums to translated display labels. Keep error/success messages as keys so already visible messages update when the language changes.
- Translate strings consumed by third-party JavaScript APIs reactively using `TranslateService`; do not cache an `instant()` result at component construction.
- Use the shared localized formatting pipes for dates, currency and numbers. Preserve the language switcher and saved language preference.
- Run `pnpm check:i18n` after adding or changing UI text. Use `pnpm test` for behavioral changes and `pnpm build` for final Angular template validation.
