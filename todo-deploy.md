# Deploy TODO — pre-deploy checklist & required prod config

**Purpose:** a living checklist so a deploy never breaks on a config gap the code
now requires. Fill in the **Outstanding** section whenever a change adds a new
prod requirement (a fail-closed config gate, a new env var, a new internal URL, a
new externally-routed path, a migration, a new secret). Public repo — put env-var
**names** here, never secret values / IPs / hosts.

> Why this file exists: the F2 deploy (2026-07-02) crash-looped every CORS-guarded
> service because #59 made CORS **fail-closed** in non-Development and `CorsAllowedOrigins`
> was never set in prod. "Re-enabling / adding a dormant gate is a risk event" — record
> the required config here **in the same PR** that adds the gate.

---

## Before every deploy — ask these

- [ ] **New fail-closed config gate this cycle?** Did any `Startup`/settings `Validate()`
      start throwing in non-Development when a value is missing (CORS, JWT key length,
      an `IdentityBaseUrl`-style URL, a cert password)? → add the value to the prod env
      **now** (see registry below) or the container crash-loops on boot.
- [ ] **New externally-routed endpoint?** The Gateway `RateLimitPolicySelector` **fails
      closed** — a path with no policy mapping returns `403 {"error":"No rate limit policy
      configured for this endpoint"}` and never reaches the service. Add the path→policy
      in `RateLimitPolicySelector.cs`. (Internal service→service calls, e.g. `/auth/validate`,
      bypass the Gateway and don't need a mapping.)
- [ ] **New service→service call?** Add the target's internal URL env (pattern:
      `IdentityBaseUrl: http://identity:6701`) to `docker-compose.prod.yml`.
- [ ] **New secret?** Add it to `aws/.env.prod` (gitignored) **and** `aws/.env.prod.example`
      (placeholder) **and** wire it in `docker-compose.prod.yml`.
- [ ] **Migration?** Auto-applies on container start (`db.Database.Migrate()` in each
      `Program.cs`) — no manual step, but confirm the new migration is committed.
- [ ] **Mobile client change?** Rebuild `./build-apk.sh --prod --release`, `./publish-apk.sh`,
      commit the versionCode bump.
- [ ] **Rebuild vs restart?** Code changed → full `./aws/release.sh` (~15-20 min build).
      Only compose/env changed → `./aws/release.sh --no-build` (~2-3 min, no rebuild).

## After every deploy — verify (all three, not just /health)

- [ ] **Fresh containers:** `docker ps` app containers show a new `CreatedAt` from *this*
      deploy (release.sh's trailing `status.sh` can be green against the *old* containers —
      the pull+recreate is dispatched async).
- [ ] **External** health (not SSM-localhost): `curl -sk https://<EIP>:6700/health` → `200 healthy`.
      `HTTP 000` = gateway unreachable/crash-looping → read `docker logs jiapp-gateway-1`.
- [ ] **Re-test the behavior you changed** (a 200 on `/health` does not prove the new code works).

---

## Required prod config registry (env-var NAMES per concern)

| Concern | Env / config (names only) | Where set | Gate behaviour if missing |
|---|---|---|---|
| JWT signing | `JWT_KEY` (≥32 chars), `JWT_ISSUER`, `JWT_AUDIENCE` | `aws/.env.prod` → compose | `Validate()` throws on boot (all 5 auth services) |
| CORS (fail-closed, #59) | `CorsAllowedOrigins__0` (+`__1`…) | `docker-compose.prod.yml` (gateway, identity, ytdownloader, scheduler, lovingboards) | **Throws on boot in non-Development** |
| Cross-service stamp recheck (F2) | `IdentityBaseUrl` | `docker-compose.prod.yml` (scheduler, lovingboards) | Throws on boot in non-Development if unset |
| Gateway TLS | `CERT_PASSWORD` (matches `server.pfx`) | `aws/.env.prod` → compose | Gateway crash-loops (PKCS12 `Mac verify error`) |
| YouTube | `YOUTUBE_API_KEY`, `YOUTUBE_COOKIES_FILE`, `YOUTUBE_PROXY` (WARP) | `aws/.env.prod` → compose | 502 on song download |
| Assistant | `DEEPSEEK_API_KEY` | `aws/.env.prod` → compose | Assistant returns 503 |

---

## Outstanding for the NEXT deploy

_(Add items here as changes land; clear them once deployed. Empty = nothing pending.)_

- [ ] _(none currently — F2 + CORS-prod-config deployed 2026-07-02)_
- [x] ~~**Async download jobs (ytdownloader):** new rate-limit policy `DownloadStatus` (120/min) added to gateway config; new endpoint `GET /api/v1/yt/downloads/mp3/status/{id}` is externally-routed — gateway `RateLimitPolicySelector` already maps it, but confirm `RateLimiting:DownloadStatus` is present in gateway prod env/appsettings; ytdownloader `App:DownloadJobTimeoutMinutes` (default 30) is set in appsettings, no env var needed unless override wanted.~~ **DEPLOYED + E2E VERIFIED 2026-08-03** (tag `20260803-194754`): 30min+ video POST 0.49s → ready → 29.8MB mp3; short video fast path; empty-search 200; no 429s on polling.
- [ ] **Admin-role self-heal env var:** `BOOTSTRAP_ADMIN_USERNAME` (mapped to `Bootstrap__AdminUsername` on the identity service) must be set in `aws/.env.prod` so the Admin role auto-recovers if a future migration empties it.
- [ ] **Gateway committed JWT key blanked (Wave 1 G1.2):** the Gateway no longer ships a working default signing key in `appsettings.json` — it now fail-closes on a missing `JWT_KEY` at boot like the other 4 auth services. Confirm `JWT_KEY` is present in `aws/.env.prod` (`docker-compose.prod.yml` already requires `Jwt__Key: ${JWT_KEY:?required}`, so a missing value fails the deploy loudly rather than booting with a published key).
- [ ] **ForwardedHeaders trust list (Wave 2 G4.2/G4.4):** before this PR deploys, set the trust list on **identity and ytdownloader** (NOT the Gateway) in `aws/.env.prod` → compose. They must trust the **Gateway** container as their immediate TCP peer — in this topology mobile hits the Gateway EIP directly; the AWS API Gateway only fronts the wake Lambda, so the AWS API Gateway CIDR is the wrong trust entry. Use the **indexed** env format (a scalar var binds `null` and silently disables the fix):
  - `ForwardedHeaders__KnownNetworks__0=<gateway-cidr>` — entries must carry a `/prefix` (e.g. `172.18.0.0/16`); prefix-less entries are silently dropped.
  - and/or `ForwardedHeaders__KnownProxies__0=<gateway-ip>` for a fixed Gateway IP.
  - **The Gateway's own ForwardedHeaders stays UNCONFIGURED** — nothing proxies it in this topology; setting it would trust client-supplied `X-Forwarded-For` and let clients spoof the Gateway rate-limit partition.
  - [x] **YtDownloader G4.4 public base URL:** set `App__PublicBaseUrl` to the public Gateway base URL (`scheme://host:port`) so download links use a stable host, not the client `Host` header. **DEPLOYED 2026-08-07** — `YT_PUBLIC_BASE_URL=https://18.153.244.141:6700` in `aws/.env.prod` → compose (`App__PublicBaseUrl: ${YT_PUBLIC_BASE_URL:?required}`), ytdownloader container verified; `/yt/downloads/mp3` returns a public-EIP `downloadUrl` (E2E-verified). **Regression note:** the pre-fix "no config" fallback is NOT a safe no-op — with G4.4 unset the downloadUrl leaks the docker service name (`http://ytdownloader:6702/...`), which phones can't resolve → "Unable to resolve host ytdownloader". That is what broke downloads after the 2026-08-07 big deploy.
  - The Gateway `identity-route` now carries the `X-Forwarded: Set` transform (this PR), so Identity sees real client IPs once the trust list is set.
  No config for G4.2 → safe no-op (old behavior). G4.4 is now REQUIRED (fail-closed by `${YT_PUBLIC_BASE_URL:?required}`).
- [ ] **yt-dlp nightly + default-client-first + 5-min job timeout:** deploy = full `./aws/release.sh` (Dockerfile change ⇒ image rebuild); **optional** env var `YT_DOWNLOAD_TIMEOUT_MIN` (maps to `App__DownloadJobTimeoutMinutes`, default 5 — overriding it is a `--no-build` env-only restart, no rebuild); no EF migration; post-deploy verify = `aws/smoke-download-url.sh` plus a live download of video `WEIW6ERRSv4` (the video that exposed the breakage).
- [x] **App version gate — ARM step (PR #97 + #98):** the new `GET /api/v1/app/version` endpoint is fail-safe (dormant `APP_UPDATE_MIN_VERSION_CODE=0` → no gate, no boot throw), so the initial deploy needs no config. **When arming** the gate later, set in `aws/.env.prod` → compose: `APP_UPDATE_MIN_VERSION_CODE` (MUST be ≤ latest published APK versionCode — preflight vs S3 `apk-metadata.json` or every install gets gated and cannot install the fix) and `APP_UPDATE_DOWNLOAD_URL` (public S3 `JiApp-latest.apk`). Arming = env-only restart `./aws/release.sh --no-build`. **ARMED + DEPLOYED 2026-08-07** — `APP_UPDATE_MIN_VERSION_CODE=66` + download URL set in prod; `/api/v1/app/version` returns `{minVersionCode: 66, downloadUrl}` (verified live).
- [x] **RoleSeeder empty-role convergence (fix/roleseeder-converge-empty-role):** No new prod config required. RoleSeeder now converges create-only roles that exist with zero permission claims; prod User role was already fixed manually on 2026-07-16, so this is a no-op on prod. **MERGED + DEPLOYED 2026-08-07** (PR #85).
