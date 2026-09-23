# Dealer Manager Client

Angular standalone client with routing and CSS. Server integration will be added later.

## Requirements

- Node.js 24.19.0 recommended (see `.node-version`). Supported versions are declared in `package.json`.
- pnpm 11.19.0 (declared in `packageManager`).

## Development

Run from `WebApplication`:

```sh
pnpm install --frozen-lockfile
pnpm start
```

Open http://localhost:4200/. A minimal Dealer Manager header is displayed.

## Production build

```sh
pnpm build
```

The browser build is written to `dist/dealer-manager-client/browser`.

## Structure

```text
src/
  app/
    auth-guards/
    configuration/
    header/
      header.component.html
      header.component.ts
    interceptors/
    app.component.html
    app.component.ts
    app.config.ts
    app.routes.ts
  assets/
    i18n/
    img/
  favicon.ico
  index.html
  main.ts
  styles.css
```

`auth-guards`, `configuration`, `interceptors`, `i18n`, and `img` are placeholders preserved with `.gitkeep`. There is no authentication, API integration, or translation library configured yet. Static assets are served under `/assets/`, and the favicon at `/favicon.ico`. Global styles belong in `src/styles.css`.
