# WEB_PLAN — Plan

> Personal portfolio website for **Jakub Iwicki** + controlled public distribution of the JiApp Android APK.
> Status: **planning complete, approved** · Author: Jakub Iwicki · Last updated: 2026-06-24

---

## 1. Why this exists

Jakub wants a public-facing website that does two jobs:

1. **Identity + showcase** — "who is Jakub Iwicki": a short bio/about, a showcase of his other
   projects, and a prominent link to his GitHub.
2. **Primary feature — mobile APK distribution** — let visitors **on Android phones** download the
   latest APK of the JiApp mobile app directly onto their device, with a great mobile-first
   "tap → install" (and desktop "scan QR") experience.

Today there is no website, and the APK builds only to the local `dist/` folder (~94 MB) via
`build-apk.sh` — it is never published anywhere. This project introduces a small, maintainable
portfolio site plus a deliberate, owner-controlled APK-publishing pipeline.

---

## 2. Locked decisions

| Area | Decision | Rationale |
|---|---|---|
| **Site stack** | React + TypeScript, built with **Vite**. Single-page scrolling layout with anchor-link sections — no router, no SSR. | Jakub codes in React/TS daily; Vite is the standard modern React build and emits pure static files for Pages. A one-page portfolio needs no router. |
| **Hosting** | **GitHub Pages** as a *user site* in a **new repo `JakubIwicki.github.io`**, served at root `https://jakubiwicki.github.io` (Vite `base: '/'`). | Free, zero infra, native fit for a GitHub-centric portfolio. A user-site repo serves at root, which is what the chosen "free default" URL implies. |
| **Deploy** | **GitHub Actions** → build Vite static export → publish via `actions/deploy-pages`. | Push-to-deploy, no manual upload. |
| **APK home** | **New public-read S3 bucket** `jiapp-downloads-{account}`, stable key `JiApp-latest.apk`. | The 94 MB binary cannot live on Pages (100 MB Git file limit; Pages isn't for binary distribution). S3 is always-online, cheap, and fits existing AWS tooling. |
| **APK delivery model** | Owner publishes deliberately via a release script (full manual version control). Stable URL never changes. | Jakub explicitly wanted control over *which* version is live, over an auto-"latest" button. |
| **APK metadata** | `apk-metadata.json` sidecar in S3 (`version`, `versionCode`, `sizeBytes`, `releaseDate`, `sha256`). Site fetches it client-side. | Shows live version/size on the site without rebuilding/redeploying the site. |
| **Domain** | Free default `jakubiwicki.github.io` now; custom domain accommodated later (CNAME + DNS) with zero rework. | Ship now, brand later. |
| **Styling** | Plain **CSS Modules**. | Vite-native, scoped, zero extra deps. YAGNI vs Tailwind for a one-page site. |
| **Boundary validation** | **Zod** on `apk-metadata.json`. | Matches the project's existing Zod-at-the-REST/data-boundary culture. |

---

## 3. Architecture

```
Visitor (Android phone)                 Visitor (desktop)
        │                                       │
        ▼                                       ▼
  jakubiwicki.github.io  ──(GitHub Pages, static React/Vite SPA)──┐
        │                                                          │
        │ client-side fetch apk-metadata.json (CORS GET)           │ render QR → S3 URL
        ▼                                                          ▼
  S3 public bucket  jiapp-downloads-{account}/
        ├── JiApp-latest.apk          ← Download button links here (direct, no CORS needed)
        ├── JiApp-{versionCode}.apk   ← archived versioned copies
        └── apk-metadata.json         ← fetched by site (CORS GET from Pages origin)

  Publish path (owner-controlled):
  JiApp/dist/JiAppMobile-{vc}-release.apk  ──(publish-apk.sh)──▶  S3 (apk + versioned + metadata)
```

**Two repos / two pipelines, fully decoupled:**

- **`JakubIwicki.github.io`** (new) — the site. Push to `main` → GitHub Actions deploys to Pages.
- **`JiApp`** (existing) — gains `publish-apk.sh` + S3-bucket infra. Run on demand to release a new APK.

The site and the binary are intentionally separate concerns connected only by a stable S3 URL held
in one config constant (`src/config.ts`). Swapping the URL (e.g. to a future CloudFront domain) is a
one-line change.

---

## 4. The website (`JakubIwicki.github.io` repo)

### 4.1 Information architecture (single-page scroll)

Sticky top nav with anchor links; sections in order:

1. **Hero / About** — name, role/tagline, 2–4 sentence bio, avatar, primary CTAs
   (GitHub, "Download the app" jump-link). Mobile-first, full-viewport.
2. **Projects** — responsive card grid from a curated `projects.ts`. Each card: name, one-line
   description, tech tags, GitHub link (optional demo link). Seed list:
   **JiApp, ki, MeSH, trading-api, SlopBot, beesness, permafrost** (owner edits freely).
3. **Download (centerpiece)** — see §4.2.
4. **Contact / Footer** — GitHub, LinkedIn, email; copyright; "built with React + Vite".

### 4.2 Download UX (the standout feature)

- **Live metadata**: on mount, fetch `apk-metadata.json`, validate with **Zod**, display `version`,
  human-readable size, and `releaseDate`. On fetch/validation failure → graceful fallback to a
  static direct-download link with generic copy (never crashes).
- **Device detection** (lightweight `navigator.userAgent`):
  - **Android** → big primary **"Download for Android"** button → direct `<a download>` to the stable
    S3 APK URL. Below it: collapsible "How to install" steps (enable *Install unknown apps* for the
    browser, open the file, confirm) + an honest note that it's a sideloaded/self-signed APK.
  - **Desktop / non-Android** → a **QR code** encoding the APK URL ("Scan to download on your phone")
    + the same install note.
- The actual download is a plain `<a download>` — works even if the metadata fetch fails (no JS needed
  for the download itself).

### 4.3 Project structure (Vite + React + TS)

```
JakubIwicki.github.io/
├── index.html
├── vite.config.ts            # base: '/', build output dist/
├── tsconfig.json
├── package.json
├── public/
│   └── CNAME                 # (added only when a custom domain is attached)
├── .github/workflows/deploy.yml
└── src/
    ├── main.tsx
    ├── config.ts             # APK URL + metadata URL (single source)
    ├── App.tsx               # composes sections + sticky nav
    ├── sections/
    │   ├── Hero.tsx          # about / bio / hero CTAs
    │   ├── Projects.tsx      # maps projects.ts → ProjectCard grid
    │   ├── Download.tsx      # device-aware download experience (§4.2)
    │   └── Footer.tsx        # contact + social links
    ├── components/
    │   ├── Nav.tsx           # sticky anchor nav
    │   ├── ProjectCard.tsx   # single project card
    │   ├── DownloadButton.tsx# Android CTA / <a download>
    │   └── QrCode.tsx        # desktop QR
    ├── data/
    │   └── projects.ts       # SINGLE source of truth for the project list
    ├── lib/
    │   ├── apkMetadata.ts    # Zod schema + fetch + parse
    │   └── device.ts         # isAndroid() / device detection
    ├── types.ts              # Project type, ApkMetadata (inferred from Zod)
    └── styles/               # one CSS Module per section/component
```

### 4.4 Boundary validation (Zod)

`src/lib/apkMetadata.ts`:

```ts
ApkMetadataSchema = z.object({
  version: z.string(),
  versionCode: z.number().int(),
  sizeBytes: z.number().int().positive(),
  releaseDate: z.string(),        // ISO date
  sha256: z.string(),
})
type ApkMetadata = z.infer<typeof ApkMetadataSchema>
```

`fetchApkMetadata()` → `safeParse` → returns `ApkMetadata | null` (null triggers the fallback UX).

### 4.5 Deploy workflow (`.github/workflows/deploy.yml`)

- Trigger: push to `main` (+ `workflow_dispatch`).
- `permissions: { contents: read, pages: write, id-token: write }`, `concurrency: pages`.
- **build** job: checkout → setup-node → `npm ci` → `npm run build` → `actions/upload-pages-artifact` (`dist/`).
- **deploy** job: `actions/deploy-pages`.
- One-time manual setup: repo **Settings → Pages → Source = "GitHub Actions"**.

---

## 5. APK publishing pipeline (`JiApp` repo)

### 5.1 New S3 bucket

- Bucket `jiapp-downloads-{account}` (account read from gitignored `aws/.env`, like existing buckets).
- **Public-read bucket policy** scoped to read-only `s3:GetObject` on the download objects.
- **CORS** rule allowing `GET` of `apk-metadata.json` from origin `https://jakubiwicki.github.io`
  (the APK itself is a plain navigation download — no CORS needed, but include it for safety).
- Added as **placeholders** to `aws/setup.sh` (create bucket + policy + CORS) and `aws/cloudformation.yml`.
  Real account IDs stay only in gitignored `aws/.env`.

### 5.2 `publish-apk.sh` (new, JiApp repo root)

1. Resolve the APK to publish (default: newest `dist/JiAppMobile-*-release.apk`; or `--apk <path>`).
2. Parse `versionCode` from the filename; read `versionName` from the build (or `--version`).
3. Compute `sizeBytes` and `sha256`.
4. Upload APK to `s3://…/JiApp-latest.apk` **and** an archived `JiApp-{versionCode}.apk`
   (`--content-type application/vnd.android.package-archive`; `Cache-Control: public,max-age=300` on
   latest, `immutable` on the versioned copy).
5. Generate `apk-metadata.json` and upload it (`Cache-Control: public,max-age=60`, `application/json`).
6. Print the public URLs. Reads bucket/account from `aws/.env`.

This is the **only** way an APK becomes public — run deliberately, full version control.
**The APK is never committed to git.**

---

## 6. Risks & considerations

- **94 MB on S3, not Pages** — intentional and required (Pages 100 MB file limit). Documented so it
  is not "fixed" later.
- **Downloaded prod APK targets the sleeping AWS backend** — first app launch may trigger the existing
  wake flow. App behavior, **out of scope for the site**, but worth a one-line note on the download
  section so testers aren't surprised.
- **Sideloaded/self-signed APK** — Android warns; install guidance + an honest note set expectations.
  Google Play distribution is out of scope.
- **S3 egress cost** — negligible at portfolio scale (~$0.09/GB). Optional CloudFront later (zero
  rework — swap the URL in `config.ts`).
- **Single edit points** — `projects.ts` for the project list; `config.ts` for the bucket URL.
- **Public repo hygiene** — no AWS account IDs / IPs / home paths in tracked files; real IDs stay in
  gitignored `aws/.env`; tracked infra uses `{account}` placeholders.
- **Custom domain later** — add `public/CNAME` + DNS; no code changes (user site already serves at root).

---

## 7. Cross-references

- Phased build order & checkboxes → [`roadmap.md`](./roadmap.md)
- Running decision log & session handoffs → [`process.md`](./process.md)
- APK build mechanics → repo root `build-apk.sh`
- Existing AWS infra → `aws/setup.sh`, `aws/cloudformation.yml`, `URLS.md`
