# Deployment Plan — Process Tracking

**Branch:** `feat/aws-deployment-plan`
**Started:** 2026-06-17
**Plan file:** [DEPLOYMENT_PLAN.md](DEPLOYMENT_PLAN.md)

---

## Phase Status

| Phase | Status | Started | Completed | Notes |
|-------|--------|---------|-----------|-------|
| Phase 1: AWS Infrastructure | ⏳ Not started | — | — | |
| Phase 2: Application Changes | ⏳ Not started | — | — | |
| Phase 3: EC2 Bootstrap | ⏳ Not started | — | — | |
| Phase 4: CI/CD Pipeline | ⏳ Not started | — | — | |
| Phase 5: Polish & Docs | ⏳ Not started | — | — | |

Status: ⏳ Not started | 🔄 In progress | ✅ Done | ❌ Blocked

---

## Session Log

### 2026-06-17 — Planning Session

- Explored codebase: 5 .NET 10 services, Docker Compose, dual DB provider
- Critic reviewed architecture — 9 findings resolved
- Decisions made:
  - Single EC2 t4g.nano, stop-when-idle (~$2.58/month)
  - SQLite only (drop PostgreSQL)
  - Baked CA cert for TLS (primary), Cloudflare Tunnel (future option)
  - Git SHA image tags, S3 deploy config
  - 20 min auto-stop watchdog
  - Mobile ServerWakeScreen with polling
- Plan approved, deployment_plan/ written
- Files created:
  - `deployment_plan/DEPLOYMENT_PLAN.md` — full plan
  - `deployment_plan/CLOUDFLARE_TUNNEL.md` — future TLS alternative
  - `deployment_plan/process.md` — this file

---

## Decisions Log

| Date | Decision | Rationale |
|------|----------|-----------|
| 2026-06-17 | SQLite-only production | Saves ~256MB RAM, eliminates WAL-corruption-on-stop risk, already tested daily in dev |
| 2026-06-17 | Stop-when-idle vs always-on | 60% cost savings, acceptable UX tradeoff for 1h/week usage |
| 2026-06-17 | Baked CA cert for TLS | Zero cost, zero external dependencies, works on raw IP, same pattern as dev |
| 2026-06-17 | 20 min auto-stop timeout | Long enough to not kill active downloads, short enough to not waste $ |
| 2026-06-17 | Git SHA tags, never `:latest` | Enables rollback, prevents "mystery image" problem |
| 2026-06-17 | SSM Session Manager, no SSH | More secure, auditable, no key management |
| 2026-06-17 | GitHub OIDC, no AWS access keys | No long-lived credentials to leak or rotate |
