# Deployment Plan — Process Tracking

**Branch:** `feat/aws-deployment-plan`
**Started:** 2026-06-17
**Plan file:** [DEPLOYMENT_PLAN.md](DEPLOYMENT_PLAN.md)

---

## Phase Status

| Phase | Status | Started | Completed | Notes |
|-------|--------|---------|-----------|-------|
| Phase 1: AWS Infrastructure | ✅ Done — IaC ready | 2026-06-17 | — | CloudFormation + setup script written, needs `aws configure` + `./setup.sh` |
| Phase 2: Application Changes | ✅ Done | 2026-06-17 | 2026-06-17 | SQLite prod, HTTPS Gateway, certs, ServerWakeScreen, 481 tests pass |
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

### 2026-06-17 — Phase 1 Implementation

- Wrote `deployment_plan/phase1/cloudformation.yml` — S3 (2 buckets), ECR (5 repos with lifecycle), IAM (3 roles: EC2, Lambda, GitHub OIDC), security group
- Wrote `deployment_plan/phase1/lambda/starter.py` — Python 3.12 Lambda, idempotent start check
- Wrote `deployment_plan/phase1/setup.sh` — orchestrates CloudFormation deploy → EC2 launch → Elastic IP → Lambda + API Gateway
- **Status:** IaC ready. User needs to `aws configure` then `./setup.sh`.
- **Outputs:** EC2_INSTANCE_ID, Elastic IP, API Gateway URL, GitHub Role ARN

### 2026-06-17 — Phase 2 Implementation

**Backend:**
- Generated production CA cert + server PFX (`backend/certs/prod/`), private keys gitignored
- Rewrote `docker-compose.prod.yml`: removed PostgreSQL, all services use SQLite, Gateway serves HTTPS with cert
- Rewrote `docker-compose.yml` (base): removed PostgreSQL dependency
- Updated `docker-compose.dev.yml`: removed postgres profile suppression
- Updated `Gateway appsettings.Production.json`: HTTPS Kestrel endpoint + Serilog file sink
- Updated `.env.example`: removed POSTGRES_PASSWORD, added CERT_PASSWORD
- Updated CI (`ci.yml`): removed POSTGRES_PASSWORD, added CERT_PASSWORD
- Deleted `docker/init-databases.sh`
- Baked production CA cert into `mobile/.../res/raw/jiapp_prod_ca`

**Mobile (react-native-coder agent):**
- Created `ServerWakeScreen.tsx`: animated wake→poll→retry flow, 120s timeout
- Updated `AppNavigator.tsx`: wake screen in production, disabled in dev (`__DEV__`)
- Updated `network_security_config.xml`: production domain-config with PROD_IP markers
- Updated `build-apk.sh`: injects WAKE_API_URL and PROD_IP from .env
- Added `WAKE_API_URL` to config.ts/config.generated.ts
- Added i18n strings (en.json + pl.json) for wake screen
- Created 6 tests for ServerWakeScreen
- **481 tests pass**, react-doctor 100/100

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
