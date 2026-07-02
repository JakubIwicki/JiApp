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

Note: `imagetools` has no CORS/JWT-Validate gate — do not add `CorsAllowedOrigins` there.

---

## Outstanding for the NEXT deploy

_(Add items here as changes land; clear them once deployed. Empty = nothing pending.)_

- [ ] _(none currently — F2 + CORS-prod-config deployed 2026-07-02)_
