# Jakub Iwicki — Portfolio Site

Personal portfolio + JiApp APK download page. Built with React 18 + TypeScript + Vite, styled with CSS Modules.

**This site will be extracted to its own `JakubIwicki.github.io` GitHub Pages user-site repo** and served at `https://jakubiwicki.github.io`. For now it lives in the JiApp monorepo under `web/`.

## Dev

```bash
npm install
npm run dev        # http://localhost:5173
```

## Build

```bash
npm run build      # emits web/dist/
npm run preview    # preview the production build
```

## Architecture

Single-page scrolling layout with sticky anchor nav. No router, no SSR. Sections:

- **Hero** — name, tagline, bio, avatar, CTAs
- **Projects** — curated card grid from `src/data/projects.ts`
- **Download** — (phase 2) device-aware APK download experience
- **Footer** — GitHub, LinkedIn, email, copyright

## Deploy guard

Before deploying, run:

```bash
npm run build && npm run check:no-placeholder
```

The `check:no-placeholder` script fails if `REPLACE_ME` (the placeholder S3 account ID in `src/config.ts`) is still present in the build output. This ensures a placeholder URL can never ship to production.
