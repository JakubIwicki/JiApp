# WEB_PLAN — Process Log

> Running record of decisions, work done, and session handoffs for the portfolio-website project.
> Append newest entries at the top of the log. See [`plan.md`](./plan.md) and [`roadmap.md`](./roadmap.md).

---

## Decision log

| # | Decision | Options considered | Chosen | Why |
|---|---|---|---|---|
| D1 | Where to host the site | GitHub Pages · existing AWS EC2 · Vercel/Netlify | **GitHub Pages** (user site repo `JakubIwicki.github.io`) | Free, zero infra, native fit for a GitHub-centric portfolio; serves at root for the chosen free URL. |
| D2 | How to deliver the APK | GitHub Releases auto-latest button · self-hosted stable file · S3+CloudFront | **Self-hosted stable file on public S3** | Owner wants explicit control over *which* version is live; one fixed URL the site links to. |
| D3 | Where the APK file lives | Public S3 bucket · EC2/nginx · GitHub Release asset | **Public S3 bucket** | Always-online (EC2 sleeps/wakes), handles 94 MB trivially, ~pennies/mo, fits existing AWS tooling. |
| D4 | Domain | Free default · buy custom · already own one | **Free default `jakubiwicki.github.io`** for now | Ship immediately; custom domain later with zero rework (CNAME + DNS). |
| D5 | Site framework | Astro · hand-built HTML/CSS · Next.js · **React+TS (Vite)** | **React + TypeScript + Vite** | Owner codes in React/TS daily; Vite emits pure static files for Pages; no SSR needed. |
| D6 | Page structure | Multi-route SPA · single-page scroll | **Single-page scroll** with anchor sections | A portfolio needs no router; avoids the Pages SPA 404 workaround. |
| D7 | Styling | Tailwind · CSS Modules · plain CSS | **CSS Modules** | Vite-native, scoped, zero extra deps; YAGNI vs Tailwind for one page. |
| D8 | Boundary validation | none · Zod | **Zod** on `apk-metadata.json` | Matches the project's existing Zod-at-the-boundary culture. |

### Conflict resolved during planning
The owner initially picked *both* "GitHub Pages-style free hosting" **and** "self-host the APK file at a
stable URL." These can't coexist — Pages cannot host the 94 MB APK (100 MB Git file limit; not a binary
CDN). Resolved by **separating concerns**: the small static site lives on Pages, the large binary lives
on public S3, connected by one stable URL. The owner then confirmed **public S3** as the APK home.

---

## Open questions / to confirm during implementation

- [ ] Final bio copy + avatar image for the Hero section.
- [ ] LinkedIn/contact-email to surface in the Footer.
- [ ] Per-project one-line descriptions + which repos are public enough to link.
- [ ] Exact bucket name suffix and AWS region for `jiapp-downloads-{account}` (read from `aws/.env`).
- [ ] Whether to archive every versioned APK copy or prune old ones periodically.

---

## Session handoffs

### 2026-06-24 — Planning session (brainstorm → approved plan)
**Did:**
- Ran a no-browser brainstorming session; established the two jobs (identity showcase + mobile APK download).
- Explored the JiApp repo: .NET 10 VSA backend on AWS EC2, `build-apk.sh` → `dist/JiAppMobile-{vc}-{variant}.apk` (~94 MB), existing `aws/` infra + S3 buckets, public GitHub repo `JakubIwicki/JiApp`.
- Locked decisions D1–D8 (see table); resolved the Pages-vs-large-APK conflict.
- Wrote and got approval for the full plan; created `WEB_PLAN/{plan,process,roadmap}.md`.

**State:** Milestone **M0 (Planning) complete**. No site code written yet (planning-only session, by request).

**Next session — start here:**
1. Create the `JakubIwicki.github.io` repo and scaffold Vite + React + TS (roadmap **Phase 1**).
2. M3 (S3 bucket + `publish-apk.sh`) can be built in parallel since it lives in the JiApp repo.
3. Gather the open-question content (bio, avatar, project blurbs) before building the Hero/Projects sections.

**Notes for implementation:**
- Per project convention, all code changes go through the delegated coder workflow; the orchestrator audits diffs.
- Keep the public repo clean — no AWS account IDs/IPs/home paths in tracked files; use `{account}` placeholders, real values in gitignored `aws/.env`.
- Update `URLS.md` when the S3 download URLs and the Pages URL go live.
