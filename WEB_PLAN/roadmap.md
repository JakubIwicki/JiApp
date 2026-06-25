# WEB_PLAN — Roadmap

> Phased build order for the portfolio site + APK pipeline. Check items off as they land.
> See [`plan.md`](./plan.md) for the *what & why*, [`process.md`](./process.md) for the running log.

Legend: `[ ]` todo · `[~]` in progress · `[x]` done

---

## Milestone M0 — Planning ✅

- [x] Brainstorm goals, audience, and the two jobs (identity + APK download)
- [x] Lock stack/hosting/delivery decisions (React+TS+Vite · GitHub Pages · public S3)
- [x] Explore JiApp repo (backend, build-apk.sh, aws/ infra, project list)
- [x] Write `WEB_PLAN/{plan,process,roadmap}.md`

**Exit criteria:** plan approved, WEB_PLAN docs reviewed. ← *we are here*

---

## Milestone M1 — Site skeleton live

### Phase 1 — Scaffold the site repo
- [ ] Create new repo `JakubIwicki.github.io` (user site, public)
- [ ] `npm create vite@latest` → React + TypeScript template
- [ ] Set `vite.config.ts` `base: '/'`; confirm build output `dist/`
- [ ] Add CSS Modules convention; strip Vite boilerplate
- [ ] `src/config.ts` with placeholder APK + metadata URLs

**Verify:** `npm run dev` renders a placeholder page; `npm run build` produces `dist/`.

### Phase 2 — Content sections
- [ ] `Nav.tsx` — sticky anchor nav
- [ ] `Hero.tsx` — name, tagline, bio, avatar, CTAs
- [ ] `data/projects.ts` + `types.ts` — curated project list (JiApp, ki, MeSH, trading-api, SlopBot, beesness, permafrost)
- [ ] `Projects.tsx` + `ProjectCard.tsx` — responsive card grid
- [ ] `Footer.tsx` — GitHub / LinkedIn / email
- [ ] `App.tsx` — compose nav + sections

**Verify:** `vite preview` shows the full page; layout holds at mobile + desktop widths.

---

## Milestone M2 — Download feature

### Phase 3 — Device-aware download UX
- [ ] `lib/device.ts` — `isAndroid()` / detection
- [ ] `lib/apkMetadata.ts` — Zod schema + `fetchApkMetadata()` (safeParse → `ApkMetadata | null`)
- [ ] `components/DownloadButton.tsx` — Android `<a download>` CTA
- [ ] `components/QrCode.tsx` — desktop QR to the APK URL
- [ ] `sections/Download.tsx` — live metadata, device branch, install guidance, fallback path
- [ ] One-line note: app talks to a sleep/wake backend (sets tester expectations)

**Verify:** metadata renders version/size; Android shows button, desktop shows QR; blocking the
metadata URL still leaves a working download link.

---

## Milestone M3 — APK distribution infra

### Phase 4 — S3 bucket
- [ ] Add `jiapp-downloads-{account}` to `aws/setup.sh` (create + public-read policy + CORS)
- [ ] Mirror the bucket as a placeholder in `aws/cloudformation.yml`
- [ ] Create the real bucket; confirm policy + CORS

**Verify:** public `GET` of a test object works; CORS preflight OK from the Pages origin.

### Phase 5 — `publish-apk.sh`
- [ ] Script at JiApp root: resolve APK → sha256/size → upload latest + versioned + metadata JSON
- [ ] Reads bucket/account from gitignored `aws/.env`
- [ ] First real publish of the current `dist/` release APK
- [ ] Point `src/config.ts` at the real S3 URLs; commit

**Verify:** `JiApp-latest.apk` + `apk-metadata.json` download at the public URL; sha256 matches the
local file.

---

## Milestone M4 — Ship

### Phase 6 — CI deploy
- [ ] `.github/workflows/deploy.yml` (build Vite → `deploy-pages`)
- [ ] Enable Pages with **Source = GitHub Actions**
- [ ] Push to `main` → confirm `https://jakubiwicki.github.io` is live

**Verify:** site loads; every nav anchor scrolls correctly.

### Phase 7 — End-to-end acceptance (the real test)
- [ ] On a physical Android phone: open the site → tap **Download for Android**
- [ ] Complete the "install unknown apps" flow → install → launch JiApp

**Verify:** APK installs and the app launches.

---

## Backlog / later (out of current scope)

- [ ] Custom domain (`public/CNAME` + DNS) — zero code rework
- [ ] CloudFront in front of the S3 bucket if download traffic ever grows
- [ ] Pull live GitHub stats (stars/last-commit) into project cards at build time
- [ ] Light/dark theme toggle
- [ ] Basic analytics (privacy-friendly) on download clicks

---

## Dependencies between milestones

```
M0 ──▶ M1 ──▶ M2 ──┐
                    ├──▶ M4 (Ship)
        M3 ─────────┘
```

- **M1 → M2**: download UX needs the section/layout shell.
- **M3 is independent of M1/M2** and can be built in parallel (it lives in the JiApp repo).
- **M4 requires both**: the site (M2) needs the real S3 URLs that M3 produces before the final deploy.
