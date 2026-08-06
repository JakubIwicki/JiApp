# JiApp — Code Review & Remediation Backlog

**Branch:** `main` · **Date:** 2026-08-05 · **Status:** Wave 1 landed — G1.1, G1.2, G2.2, G2.4, G2.6, G5.1 fixed (see per-finding notes); Wave 2 landed — PR-A G4.1–G4.5, PR-B G3.1, G3.2, G3.3, G4.6, G10.3, PR-C G2.1, G2.3 fixed (see per-finding notes); Wave 3 COMPLETE — PR-D G6.1, G6.2, PR-E G7.1, G7.3, PR-F G12.1 fixed (see per-finding notes); Wave 4 COMPLETE — PR-A G8.2, G8.3 (also G1.3, G1.6, G11.14), PR-B G8.1, PR-C G8.4, PR-D G8.5, G8.6, PR-E G8.7, PR-F G8.8 fixed (see per-finding notes); Wave 5 COMPLETE — PR-A G9.3, PR-B G9.4, PR-C G9.5, PR-E G9.2, PR-D G9.1, G9.7, G9.8, PR-F G9.6 fixed (see per-finding notes); **Wave 6 COMPLETE — PR-A #121 G10.1, PR-B #123 G10.2, PR-C #124 G10.6, PR-D #122 G9.6 IntegrationTestBase fill, PR-F #125 G7.2, PR-E1 #126 G11.1, G11.4, G11.7–G11.13, G11.16–G11.18, PR-H #127 G11.5, G11.6, PR-G #128 G7.4 + G9.1 UserDetail tests, PR-E2 #129 G11.2, G11.3, G11.15, G11.19, G11.20 fixed (see per-finding notes); Wave 7 COMPLETE — W7-B G2.5, G2.7, W7-C G3.4, G3.5, W7-A G1.4, G1.5, W7-D G5.2, W7-E G5.3, G5.4, W7-F G10.5 fixed (see per-finding notes); 8 of 84 open (G2.8, G5.5, G7.3 partial, G10.4, G12.2-G12.5)**

This file is the single working document for the review. It is organised into **12 work groups**,
each sized to be picked up as one PR/session by someone with no prior context. Every finding keeps
its original ID from the two review passes (`H1`, `M7`, `N2`, `L13`…) so older notes stay traceable.

**How it was produced.** Pass 1: full inline manual read of the kernel, every security-relevant
path, all composition roots, representative slices per service, and the mobile service/context/
navigation layers. Pass 2: seven parallel read-only audits, each with a different cluster of the
coding/testing standards injected as binding rules, each blind to pass 1. Every pass-2 claim was
re-verified by hand; seven did not survive and are listed in §D.

---

## Contents

| § | Group | Findings | Sev mix | Theme |
|---|---|---|---|---|
| **G1** | [Fail-open configuration & transport](#g1) | 6 | 2H 4M | Config that fails open, or diverges between services |
| **G2** | [Token lifecycle & revocation](#g2) | 8 | 3H 4M 1L | Refresh, logout, revocation, security stamp |
| **G3** | [Authorization & tenancy](#g3) | 5 | 1H 4M | Cross-tenant leaks, missing guards, TOCTOU |
| **G4** | [Rate limiting & abuse surface](#g4) | 6 | 1H 5M | Unpartitioned limits, uncapped inputs, quota accounting |
| **G5** | [Streaming & the error contract](#g5) | 5 | 1H 3M 1L | SSE, mid-stream failures, response-started crashes |
| **G6** | [Mobile project non-negotiables](#g6) | 3 | 2H 1✓ | The three rules in `docs/agents/README.md` |
| **G7** | [Mobile boundary validation & layering](#g7) | 4 | 3M 1L | Zod gaps, I/O outside services, laundered types |
| **G8** | [Extract-the-pattern refactors](#g8) | 8 | 8M | One correct implementation, N divergent copies |
| **G9** | [Test standards & coverage](#g9) | 8 | 7M 1L | Untested surfaces, unadopted foundations |
| **G10** | [Performance, scale & dead weight](#g10) | 6 | 6M | N+1s, replica assumptions, dead service |
| **G11** | [Correctness bugs & hygiene](#g11) | 20 | 1M 19L | Small and unambiguous — good first-PR material |
| **G12** | [CI & tooling gates](#g12) | 5 | — | Why everything above can regress silently |

| § | Reference material |
|---|---|
| **A** | [Verification baseline](#a) — what was green when this was written |
| **B** | [Standards conformance matrix](#b) — per-skill verdict |
| **C** | [What is genuinely good](#c) — constrains how fixes should be made |
| **D** | [Refuted claims — do not action](#d) |
| **E** | [Corrections to pass 1](#e) |
| **F** | [Suggested execution order](#f) |

**Totals:** 10 High · 35 Medium · 22 Low. No Critical — no authentication bypass, RCE, or injection
was found; input validation and parameterisation are solid throughout.

---

<a name="a"></a>
## A. Verification baseline

Everything in this document was found in a codebase that is **currently green**.

| Check | Command | Result |
|---|---|---|
| Backend tests | `dotnet test` | **769 passed**, 0 failed (Identity 143, Scheduler 227, LovingBoards 174, YtDownloader 170, Gateway 51, ImageTools 4) |
| Mobile tests | `npx jest` | **605 passed**, 73 suites, 0 failed |
| Mobile typecheck | `npx tsc --noEmit` | clean, exit 0 |

CI-green ≠ correct. Every finding below survives a green build.

---

## The thesis

The failures in this codebase are **asymmetries, not absences**. Almost every group below contains
the same shape: a correct pattern established in one place and not propagated.

- Security-stamp recheck exists — wired to 8 endpoints, all deletes. `RemoveBoardMember` has it;
  `AddBoardMember`, which *grants* a privilege, does not.
- Rate limiting is partitioned per-IP in the Gateway and global in Identity.
- `ClockSkew = TimeSpan.Zero` is set in Identity and defaulted in all four services that consume tokens.
- Four services ship `"Key": ""` so a missing env var crashes the boot; Gateway ships a real key so it doesn't.
- `BoardWriteLock` guards the JSON member list in LovingBoards and does not exist in Scheduler.
- `ResetPassword` and `DisableUser` revoke refresh tokens; `ChangePassword` does not.
- The backend's semantic test doubles are exemplary; the mobile ones are mode-flag state machines.
- PR #80 built stories-as-fixtures; 3 of 47 test files use it.

This matters for planning: **the highest-value work is not writing new code.** It is extracting the
implementations that are already correct somewhere in this repo into one place and deleting the
divergent copies. G8 exists for exactly that, and most of the Medium tier collapses as its side effect.

---

<a name="g1"></a>
# G1 — Fail-open configuration & transport

**Why grouped:** all six are config or transport that either fails *open* on misconfiguration, or
silently differs between services. They touch the same five `Startup.cs` / `appsettings.json` files,
so they are one PR. Fixing G1.3 and G1.6 together is a single edit.

### G1.1 (HIGH) — Gateway disables TLS certificate validation in all environments · `H1`

**FIXED (Wave 1).** Both TLS bypasses (YARP + healthCheck) now guarded by `env.IsDevelopment()`.

`backend/src/JiApp.Gateway/Startup.cs:140-160`

```csharp
// YARP reverse proxy — in dev, bypass SSL validation for self-signed certs
services.AddReverseProxy()
    .LoadFromConfig(configuration.GetSection("ReverseProxy"))
    .ConfigureHttpClient((context, handler) =>
    {
        handler.SslOptions.RemoteCertificateValidationCallback =
            (sender, cert, chain, errors) => true;   // <-- unconditional
    });

services.AddHttpClient("healthCheck")
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (_, _, _, _) => true   // <-- unconditional
    });
```

The comment says "in dev"; there is no `env.IsDevelopment()` guard. Every Gateway→service hop in
production accepts any certificate. All bearer tokens and all user data traverse these connections.
The fail-closed discipline used for CORS in the same file was not applied here.

**Fix:** wrap both callbacks in `if (env.IsDevelopment())`. `Startup` already receives `IWebHostEnvironment env`.

### G1.2 (HIGH) — Gateway ships a committed JWT key with no production override · `N1`

**FIXED (Wave 1).** Committed key blanked to `""`; the Gateway now fail-closes on a missing `JWT_KEY` at boot like its peers.

`backend/src/JiApp.Gateway/appsettings.json:32` — tracked in git; the repo is public.

```json
"Jwt": { "Key": "dev-gateway-key-at-least-32-chars!!",
```

Every peer ships an empty key in its tracked base config:

```
JiApp.Identity          (no key at all)
JiApp.Scheduler         "Key": ""
JiApp.YtDownloader      "Key": ""
api.JiApp.LovingBoards  "Key": ""
```

An empty key fails `Validate()` (`Jwt:Key is not configured` / `must be at least 32 characters`), so
a peer with a missing `JWT_KEY` env var **crashes on boot — fail-closed**. Gateway's committed
default is 35 characters and passes `Validate()`. `appsettings.Production.json` has **no `Jwt`
section at all**, so production depends entirely on env-var substitution.

**Failure scenario:** `JWT_KEY` is missing or mistyped in production. Every other service refuses to
start — loud and obvious. The Gateway starts and validates bearer tokens against a signing key
published on GitHub. Tokens forged with that key are accepted for every proxied route.

**Fix:** set `"Key": ""` in `Gateway/appsettings.json` to match its peers. `GatewaySettings.Validate()`
then turns the misconfiguration into a boot crash.

### G1.3 (MEDIUM) — `ClockSkew` zeroed in Identity, defaulted everywhere else · `N2`

Verified by grep:

```
JiApp.Identity/Services/JwtTokenService.cs      ClockSkew: 1  (= TimeSpan.Zero, line 39)
JiApp.Gateway/Startup.cs                        ClockSkew: 0
JiApp.Scheduler/Startup.cs                      ClockSkew: 0
JiApp.YtDownloader/Startup.cs                   ClockSkew: 0
api.JiApp.LovingBoards/Startup.cs               ClockSkew: 0
```

Identity builds its parameters through the shared `JwtTokenService.CreateValidationParameters()`
factory. The four services that actually *consume* tokens each hand-roll a `TokenValidationParameters`
literal and inherit Microsoft's 5-minute default.

**Consequence:** an expired access token is rejected by Identity but accepted by Scheduler,
YtDownloader, LovingBoards and the Gateway for a further 5 minutes. Compounds G2.5 — since the
security-stamp recheck runs on only 8 endpoints, token expiry *is* the revocation mechanism for
everything else, and it is 5 minutes looser than intended on every service that matters.

**Fix:** promote `CreateValidationParameters` to `JiApp.Common` and call it from all five services.
Resolves G8.2 in the same edit.

**FIXED (Wave 4).** `ClockSkew` is now zero everywhere: the shared `JiApp.Common/Authentication/TokenValidationParametersFactory` produces the single canonical `TokenValidationParameters` (`ClockSkew=0`, HS256) used by all five services — Gateway/Scheduler/YtDownloader/LovingBoards no longer inherit the 5-minute default. Resolved by G8.2 (PR #103, main `80c09c0`).

### G1.4 (MEDIUM) — Settings validation is incomplete, differently, in every service · `N13`

**FIXED (Wave 7).** Settings validation now covers all 5 services; env-aware `Validate(IWebHostEnvironment?)`, accumulating errors; `DeepSeek.ApiKey` and `Bootstrap.AdminUsername` stay optional. W7-A PR #134.

No two `Validate()` methods check the same set of runtime-required fields:

| Service | Consumed at runtime, never validated |
|---|---|
| Scheduler | `CorsAllowedOrigins`, `IdentityBaseUrl` |
| LovingBoards | `CorsAllowedOrigins`, `IdentityBaseUrl`, all 8 numeric limits (no range check) |
| YtDownloader | the entire `DeepSeek` section, including `ApiKey` |
| Identity | `Bootstrap.AdminUsername` |
| Gateway | `CorsAllowedOrigins` |

`CorsAllowedOrigins` is the notable one: every service throws at *DI-registration* time if it is
missing in production (inside `AddCors`), not at `Validate()` time with the other config errors. The
failure is late and the message is disconnected from the settings report.

### G1.5 (MEDIUM) — `LovingBoardsSettings.Validate()` / `SchedulerSettings.Validate()` short-circuit · `L14`

**FIXED (Wave 7).** Accumulation regression tests added. Note: the short-circuit claim was already resolved by Wave 4's shared `JwtSettings` returning `List<string>`; Wave 7 adds per-service regression tests proving multiple unrelated errors surface in one `Validate()` call. W7-A PR #134.

Both accumulate into an `errors` list but call `Jwt.Validate()`, which **throws immediately**. The
first JWT error hides every other config error, so operators fix them one deploy at a time.
`IdentitySettings` accumulates correctly and is the model.

### G1.6 (MEDIUM) — `JwtSettings` is copy-pasted into four projects · `M12`

`JiApp.Identity/Configuration/IdentitySettings.cs:57-71` · `JiApp.Gateway/Configuration/GatewaySettings.cs:64-70` ·
`JiApp.Scheduler/Configuration/SchedulerSettings.cs:29-47` · `api.JiApp.LovingBoards/Configuration/LovingBoardsSettings.cs:37-55`

Scheduler's and LovingBoards' copies are **byte-identical**. Gateway's is a different shape
(non-nullable, no length check on issuer/audience). Identity's is a third variant with `Validated*`
properties. Consequence: the `Key.Length < 32` floor is enforced in three of four services and the
drift is invisible.

**Fix:** one `JiApp.Common.Configuration.JwtSettings` with one `Validate()`.

**FIXED (Wave 4).** The four `JwtSettings` copies (Identity nested, Gateway nested, Scheduler top-level, LovingBoards top-level) are deleted; one `JiApp.Common/Authentication/JwtSettings.cs` (union of the variants, accumulating `Validate()`, whitespace-only key/issuer/audience rejected) serves all services. Resolved by G8.2 (PR #103, main `80c09c0`).

---

<a name="g2"></a>
# G2 — Token lifecycle & revocation

**Why grouped:** all eight concern the same subsystem — issuing, refreshing, revoking and expiring
credentials — and several interact. G2.1 and G2.2 are the same incident from two ends.

### G2.1 (HIGH) — Three uncoordinated refresh implementations trip the server's theft detection · `H2`

**FIXED (Wave 2).** The single-flight `refreshAuth()` is now exported from `apiClient`; the `chatService` and `boardStreamService` 401 handlers call it; the dormant `authService.refreshToken` was deleted. SSE 401 tests assert a single refresh with no raw `/auth/refresh`.

`mobile/src/services/apiClient.ts:45-94` · `mobile/src/services/chatService.ts:189-200` ·
`mobile/src/modules/lovingBoards/services/boardStreamService.ts:152-170` ·
`backend/src/JiApp.Identity/Features/Auth/Refresh/RefreshHandler.cs:29-37`

`apiClient` implements a correct single-flight refresh guard (`refreshPromise`). `chatService` and
`boardStreamService` each reimplement the refresh call **without joining that guard** —
`boardStreamService` documents it: *"mirrors chatService"*.

Server side, refresh is single-use and reuse is treated as theft:

```csharp
if (storedToken.IsRevoked)
{
    logger.RefreshTokenReuseDetected(storedToken.Id, storedToken.UserId);
    await refreshTokenService.RevokeAllForUserAsync(storedToken.UserId, CancellationToken.None);
    return Result<RefreshResponse>.Failure("Invalid or expired refresh token");
}
```

**Failure scenario:** a user sits on `BoardDetailScreen` (SSE open) when the access token expires. The
stream 401s and starts a refresh; concurrently a screen request 401s and `apiClient` starts its own.
The loser presents the now-revoked token → **every refresh token for that user is revoked** → hard
logout on all devices with no explanation. Frequency scales with time spent on a board screen.

**Fix:** export the single-flight `refreshAuth()` from `apiClient.ts` (or a `tokenRefreshService`);
have the other two call it. Deleting two copies also removes the risk they drift further.

### G2.2 (HIGH) — Refresh theft-response is rolled back by its own transaction · `H3`

**FIXED (Wave 1).** Rollback now runs first, then revoke-all outside the transaction with `CancellationToken.None`; the rollback-then-revoke order is pinned by a new test.

`backend/src/JiApp.Identity/Features/Auth/Refresh/RefreshHandler.cs:49-58`

```csharp
await using var transaction = await refreshTokenService.BeginTransactionAsync(ct);

var wasRevoked = await refreshTokenService.RevokeAsync(storedToken.Id, ct);
if (!wasRevoked)
{
    logger.RefreshTokenReuseDetected(storedToken.Id, storedToken.UserId);
    await refreshTokenService.RevokeAllForUserAsync(storedToken.UserId, ct);   // inside the tx
    await transaction.RollbackAsync(ct);                                       // undoes it
    return Result<RefreshResponse>.Failure("Invalid or expired refresh token");
}
```

`RevokeAllForUserAsync` issues `ExecuteUpdateAsync` on the same `DbContext` that owns the
transaction, so it enlists — and the next line rolls it back. The security response on the
concurrent-race path is a **no-op**; the attacker's other tokens survive.

The fast path at line 35 is correct (outside any transaction, `CancellationToken.None`), which is
what makes the discrepancy easy to miss.

**Fix:** commit the revoke-all on its own scope, or `RollbackAsync` first and then revoke-all with
`CancellationToken.None`, matching the fast path.

**Note:** invisible to the green suite because the mocked `IRefreshTokenService` has no transaction
semantics to roll back — see G9.2.

### G2.3 (HIGH) — Mobile never calls `/auth/logout`; refresh tokens survive logout · `H7`

**FIXED (Wave 2).** `logout()` and `dismissFarewell()` now best-effort revoke `/auth/logout` (idempotent, 5s timeout); a new `authEvents` emitter lets `apiClient` publish `authInvalidated` on refresh failure, and `AuthProvider` subscribes to it → dispatches `LOGOUT`. Covered by tests for the logout POST and `emitAuthInvalidated`.

`mobile/src/context/AuthContext.tsx:249-268`

`grep -rn "auth/logout" mobile/src` returns **nothing**. The backend implements refresh-token
revocation on logout, with a rate-limit policy, a validator, and tests. The client never calls it.

**Failure scenario:** a user logs out on a shared or stolen device. The refresh token is deleted
locally but remains **valid server-side for `Jwt:RefreshTokenExpireDays`**. Anyone who extracted it
beforehand keeps minting access tokens.

Two compounding defects in the same file:

- `logout()` (line 249) only sets `showFarewell`; credentials are cleared in `dismissFarewell()`.
  Kill the app while the farewell overlay is showing and **nothing is cleared**.
- When `apiClient`'s refresh fails it clears storage (`apiClient.ts:79-87`) but has no channel to
  tell `AuthContext`. `state.token` stays populated → the app renders an authenticated UI in which
  every request 401s until relaunch.

**Fix:** call the logout endpoint in `dismissFarewell` (and in `logout` before showing the overlay);
publish an auth-invalidated event from `apiClient` that `AuthProvider` dispatches `LOGOUT` on.

### G2.4 (MEDIUM) — `ChangePassword` does not revoke refresh tokens · `M15`

**FIXED (Wave 1).** `ChangePasswordHandler` now calls `RevokeAllForUserAsync` (`CancellationToken.None`) after a successful change, matching `ResetPassword`/`DisableUser`.

`backend/src/JiApp.Identity/Features/Auth/ChangePassword/ChangePasswordHandler.cs:26-34`

`UserManager.ChangePasswordAsync` rotates the security stamp, so outstanding *access* tokens die at
the next recheck. But there is **no `RevokeAllForUserAsync` call**, so every outstanding *refresh*
token stays valid and reissues access tokens carrying the new stamp. Changing your password after a
suspected compromise does not evict the attacker.

The codebase already knows the shape: `ResetPasswordHandler.cs:29` and `DisableUserHandler.cs:40`
both call it, with the comment *"Security cleanup must complete even if the request aborts."* The
self-service path missed it.

### G2.5 (MEDIUM) — Security-stamp recheck is wired to 8 endpoints, all deletes · `M1`

**FIXED (Wave 7).** Security-stamp recheck extended to `AddBoardMember` in BOTH services; count text corrected from "8 endpoints" to 11, now 13 endpoints with the recheck filter. W7-B PR #131.

`grep -rn "AddEndpointFilter<SecurityStampRecheckFilter>"` returns exactly 8 hits:

| Service | Endpoints with recheck |
|---|---|
| Scheduler | DeleteAppointment, DeleteService, DeleteExpense, DeleteClient, DeleteBoard, RemoveBoardMember |
| LovingBoards | DeleteItem, DeleteBoard, RemoveBoardMember, ResetWeeklyItems, ClearCompleted |

Every read, create and update runs with no revocation check. A disabled or role-revoked user keeps
full access until token expiry (+5 min, per G1.3).

Internally inconsistent on its own terms: `RemoveBoardMember` (revokes a privilege) has the filter;
**`AddBoardMember` (grants one) does not.**

**Fix:** decide the policy explicitly — apply at `MapGroup` level for all mutating endpoints, or
document why deletes alone warrant it. The current state reads as "revocation is enforced" while
enforcing it on ~10% of the surface.

### G2.6 (MEDIUM) — `RefreshTokenCleanupService` can take down the Identity service · `M2`

**FIXED (Wave 1).** Sweep wrapped in try/catch with an injected `ILogger`; a failed sweep is logged and the hourly loop survives instead of stopping the host.

`backend/src/JiApp.Identity/Services/RefreshTokenCleanupService.cs:10-23`

```csharp
while (!stoppingToken.IsCancellationRequested)
{
    await Task.Delay(CleanupInterval, stoppingToken);
    using var scope = scopeFactory.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    await dbContext.RefreshTokens
        .Where(rt => rt.ExpiresAt < DateTime.UtcNow || rt.IsRevoked)
        .ExecuteDeleteAsync(stoppingToken);      // no try/catch
}
```

No exception handling. .NET's default `BackgroundServiceExceptionBehavior` is **`StopHost`**, so one
`SQLITE_BUSY` on the hourly sweep terminates the whole Identity process. Realistic on SQLite under
concurrent writes.

No `ILogger` is injected at all, so a failure is invisible. Contrast
`YtDownloader/Services/TempFileCleanupService.cs:12-19`, which does this correctly. The two
background services in this solution disagree on the most basic point.

### G2.7 (MEDIUM) — `HttpResponseMessage` leak on retried security-stamp checks · `M22`

**FIXED (Wave 7).** `RemoteSecurityStampValidator` now disposes per attempt and maps caller-cancellation → Unavailable (503); `RetryPolicyFactory`'s `ShouldHandle` checks the resilience-context token. W7-B PR #131.

`JiApp.Common/Services/RemoteSecurityStampValidator.cs:34-39` — `using var response = await policy.ExecuteAsync(...)`
disposes only the *final* response. Each retried attempt leaves an undisposed `HttpResponseMessage`
on a per-request auth path.

Related: `RetryPolicyFactory.RetryOnTransientHttp_WithExponentialBackoff` handles
`TaskCanceledException` (line 31), so caller-initiated cancellation is a retry candidate. And in
Polly v8 an outer-token cancellation surfaces as `OperationCanceledException`, which neither
`catch (HttpRequestException)` nor `catch (TaskCanceledException) when (!ct.IsCancellationRequested)`
catches — so a client disconnect during recheck escapes as an unhandled exception → 500.

**Fix:** dispose per attempt; add `catch (OperationCanceledException) when (ct.IsCancellationRequested)`.

### G2.8 (LOW) — Login timing mitigation is inverted · `L3`

`Login/LoginHandler.cs:30-32` — the unknown-user path *hashes then verifies* (2 KDF passes); the
known-user path does one verify. The unknown-user branch is measurably **slower**, so the
enumeration oracle survives with its sign flipped.

---

<a name="g3"></a>
# G3 — Authorization & tenancy

**Why grouped:** all five are "the caller reached data or a mutation they should not have". Four are
in Scheduler.

### G3.1 (HIGH) — Cross-board IDOR: appointments accept Client/Service IDs from other boards · `H4`

**FIXED (Wave 2).** Board-scoped FK lookups: `AppointmentHelpers.ClientExistsAsync`/`FindServiceAsync` now take a `boardId`; Create uses `request.BoardId`, Update uses `appointment.BoardId`. Covered by the `CreateAppointment_WithClientFromAnotherBoard_ReturnsNotFound` and `UpdateAppointment_WithClientFromAnotherBoard_ReturnsNotFound` tests.

`backend/src/JiApp.Scheduler/Features/Common/AppointmentHelpers.cs:9-14` ·
`…/CreateAppointment/CreateAppointmentHandler.cs:18-29` · `…/UpdateAppointment/UpdateAppointmentHandler.cs:22-33`

`Client` and `Service` are board-scoped (`Client.BoardId`, `Service.BoardId`) and every read path
enforces it. The appointment write path validates the referenced IDs **globally**:

```csharp
internal static async Task<bool> ClientExistsAsync(ISchedulerDbContext db, long clientId, CancellationToken ct) =>
    await db.Clients.AnyAsync(c => c.Id == clientId, ct);          // no BoardId filter

internal static async Task<Service?> FindServiceAsync(ISchedulerDbContext db, long serviceId, CancellationToken ct) =>
    await db.Services.FirstOrDefaultAsync(s => s.Id == serviceId, ct);   // no BoardId filter
```

`VerifyBoardAccessAsync` only checks `request.BoardId`.

**Failure scenario:** a member of board A posts `{ boardId: A, clientId: <id on board B>, serviceId: <id on board B> }`.
Accepted. Then `service.BasePrice` from board B is copied into board A's appointment
(`ResolvePrice`, line 29) — pricing leak. And `RevenueReportHandler` groups by `a.Service.Name` /
`a.Client.Name` (`…/RevenueReport/RevenueReportHandler.cs:44-46`) while `GetClientHandler` renders
`a.Service.Name` — so **board B's client and service names are rendered to board A's users** by ID
enumeration.

**Fix:** scope both helpers by board. Add a handler test asserting a foreign `clientId` returns `NotFound`.

### G3.2 (MEDIUM) — 403-vs-404 existence oracle on Scheduler appointments · `N3`

**FIXED (Wave 2).** GET/PUT/PATCH/DELETE appointment id-routes now collapse `AccessDenied` into a uniform 404 — same status and `"Appointment not found"` body — while the handler keeps the category distinction; the dead `Produces(403)` is dropped. Non-member GET/PUT/PATCH/DELETE endpoint tests return 404.

`Features/Appointments/GetAppointment/GetAppointmentHandler.cs:12-18` + `GetAppointmentEndpoint.cs:19-22`

```csharp
var appointment = await db.Appointments.FindAsync([id], ct);
if (appointment is null)
    return Result<AppointmentResponse>.Failure("Appointment not found", ResultCategories.NotFound);   // 404

var boardResult = await BoardAccessGuard.VerifyBoardAccessAsync(db, appointment.BoardId, currentUser, ct);
if (!boardResult.IsSuccess)
    return Result<AppointmentResponse>.Failure(boardResult.Error!, boardResult.ErrorCategory);        // 403
```

Existence is checked **before** authorization, and the endpoint maps the outcomes to different codes
(`AccessDenied => Results.Forbid()`, `_ => Results.NotFound(...)`).

**Failure scenario:** an authenticated user enumerates appointment IDs. `404` = unused ID; `403` =
exists on a board they cannot see. Leaks the existence, ID range, and rough volume of every other
tenant's appointments. Same ordering in `UpdateAppointment` and `DeleteAppointment`.

**Fix:** check board access first; return `404` for both "missing" and "not yours".

### G3.3 (MEDIUM) — Scheduler's board-member handlers lack the write lock LovingBoards uses · `N4`

**FIXED (Wave 2).** `BoardWriteLock` ported from LovingBoards (per-board semaphore, `AddSingleton`); the `AddBoardMember`/`RemoveBoardMember` read-modify-write is now locked.

`JiApp.Scheduler/Features/Boards/AddBoardMember/AddBoardMemberHandler.cs:8-23`

```csharp
public sealed class AddBoardMemberHandler(ISchedulerDbContext db, ICurrentUserService currentUser)
{   // no BoardWriteLock
    …
    board.MemberUserIds.Add(request.UserId);
    await db.SaveChangesAsync(ct);
```

The functionally identical LovingBoards handler opens with
`using var _ = await boardLock.AcquireAsync(boardId, ct);`. The type does not exist in Scheduler.

**Failure scenario:** two concurrent `AddBoardMember` calls both read the same `MemberUserIds` JSON
list, both append, and the second `SaveChangesAsync` overwrites the first. One addition silently
lost, 200 returned to both. Same in `RemoveBoardMember` — where a lost *removal* means a user
retains board access they were supposed to lose.

### G3.4 (MEDIUM) — TOCTOU on every resource limit · `M23`

**FIXED (Wave 7).** TOCTOU: `CreateBoard` per-user cap and `CreateItem` per-board cap both under per-user/per-board in-process locks (single-instance assumption documented). W7-C PR #132.

`CreateBoardHandler.cs:17-23` (`MaxBoardsPerUser`) and `CreateItemHandler.cs:24-30`
(`MaxItemsPerBoard`) both count-then-insert with no lock and no DB constraint. Concurrent requests
exceed the cap. `CreateItemHandler` notably does **not** take the `BoardWriteLock` the
member-mutating handlers use.

Low exploitability, but the limits are the only thing between a user and unbounded storage, and
`dotnet-security-baseline` calls out TOCTOU-safe writes explicitly.

### G3.5 (MEDIUM) — `AddBoardMember` has no member cap and does not verify the user exists

**FIXED (Wave 7).** `AddBoardMember`: `MaxMembersPerBoard` cap (default 100) + cross-service user-existence check via new Identity endpoint `GET /api/v1/auth/users/{userId}/exists` (fail-closed 503 when Identity unavailable). W7-C PR #132.

`api.JiApp.LovingBoards/Features/Boards/AddBoardMember/AddBoardMemberHandler.cs:25-29` appends
`request.UserId` to the JSON list with no maximum and no check that the ID belongs to a real user.
`LovingBoardsSettings` caps boards-per-user and items-per-board but not members-per-board. The JSON
column has no length limit (`BoardConfiguration.cs:21-26`).

---

<a name="g4"></a>
# G4 — Rate limiting & abuse surface

**Why grouped:** all six are "a client can consume more than its share". Fixing G4.1 and G4.2
together is one `ForwardedHeaders` + partition-key change.

### G4.1 (HIGH) — Identity rate limiters are global, not per-client · `H5`

**FIXED (Wave 2).** Identity's four limiters now partition per authenticated user (`user:{sub}`, `ClaimTypes.NameIdentifier`) falling back to client IP (`ip:{...}`); `UseAuthentication` moved before `UseRateLimiter` so the claim is present at limiter time. The Gateway `identity-route` now carries the `X-Forwarded: Set` transform (this PR), so Identity's anonymous `ip:{...}` partition sees real client IPs once the prod trust list is set (`todo-deploy.md`). Covered by new `RateLimitPartitioningTests`.

`backend/src/JiApp.Identity/Startup.cs:193-228`

```csharp
options.AddFixedWindowLimiter("Login", config =>
{
    config.PermitLimit = 10;
    config.Window = TimeSpan.FromMinutes(1);
    …
});
```

`AddFixedWindowLimiter(name, …)` creates **one limiter shared by every request** on that policy —
no partition key. The Gateway gets this right
(`RateLimitPartition.GetFixedWindowLimiter(remoteIp, …)`, `Gateway/Startup.cs:104-112`), which makes
the Identity version an oversight rather than a decision.

**Failure scenario:** any single client issuing 10 login attempts per minute consumes the *entire
system's* login budget — everyone else gets 429. A one-line denial of service against the whole
product. It also means legitimate concurrent logins collide at ~10/min globally.

### G4.2 (MEDIUM) — Rate-limit partitions collapse behind the proxy · `H5 (secondary)`

**FIXED (Wave 2).** Shared `JiApp.Common` `UseTrustedForwardedHeaders` extension applied as the first middleware in Gateway, Identity and YtDownloader; it trusts X-Forwarded-* only from `ForwardedHeaders:KnownNetworks`/`KnownProxies`. Unconfigured → safe no-op (no crash, old behavior); configured-but-misformatted (scalar env key, or prefix-less `KnownNetworks` entries) → logs a warning so the disable is never silent. Prod trust list tracked in `todo-deploy.md` — in this topology only identity and ytdownloader should set one; the Gateway's own entry must stay unconfigured (nothing proxies it, and trusting client-supplied X-Forwarded-* there would let clients spoof its rate-limit partition).

The Gateway partitions on `httpContext.Connection.RemoteIpAddress`. Behind AWS API Gateway every
request arrives from the proxy's address, so that partition also collapses to a single bucket. No
`UseForwardedHeaders` / `X-Forwarded-For` handling exists anywhere in the solution.

**Fix:** configure `ForwardedHeadersOptions` with `KnownProxies`/`KnownNetworks`; derive the
partition key from the authenticated user id where available, falling back to a validated client IP.

### G4.3 (MEDIUM) — Assistant burns the user's daily quota on a 503 · `M3`

**FIXED (Wave 2).** `AssistantStreamGate.TryEnter()` now runs before `PreCheckAsync` consumes the daily quota, so a busy assistant returns 503 without burning a message.

`backend/src/JiApp.YtDownloader/Features/Assistant/AssistantChatEndpoint.cs:45-52`

```csharp
var preCheck = await handler.PreCheckAsync(userId, dailyLimit, httpContext.RequestAborted);
…
if (!streamGate.TryEnter())
    return Busy(language);
```

`PreCheckAsync` calls `usage.TryConsumeAsync` — it **increments the counter**. If the single-slot
`AssistantStreamGate` is occupied, the request is rejected 503 *after* the quota was consumed. A user
who hits a busy assistant three times has silently lost 3 of 30 daily messages with no reply.

**Fix:** acquire the gate first, then consume quota inside the `try`. Consider refunding on abort.

### G4.4 (MEDIUM) — Download URL built from unvalidated client headers · `M6`

**FIXED (Wave 2).** `GetDownloadLinkEndpoint` builds the download URL from a configured `App:PublicBaseUrl` (scheme + authority, preserving the port) when set, falling back to `Request.Scheme`/`Request.Host` in dev. The host never comes from the client `Host` header — the Gateway's YARP `X-Forwarded: Set` transform would otherwise promote it into a trusted `X-Forwarded-Host`.

`backend/src/JiApp.YtDownloader/Features/GetDownloadLink/GetDownloadLinkEndpoint.cs:31-45`

```csharp
var scheme = httpContext.Request.Scheme;
var host = httpContext.Request.Host.Value ?? "localhost";
if (Uri.TryCreate(settings.App?.PublicBaseUrl, UriKind.Absolute, out var parsedBase))
{
    scheme = parsedBase.Scheme;
    host = parsedBase.Authority;
}
var response = DownloadResponse.WithUrl(result.Value!.TempId, scheme, host);
```

Originally the URL was built by reading `X-Forwarded-Proto`/`X-Forwarded-Host` directly off the request with no `KnownProxies` validation — a caller set `X-Forwarded-Host: evil.example` and got back a download URL pointing at the attacker's host over cleartext. Self-targeted so impact is limited — but it becomes a redirect/exfiltration primitive the moment the URL is shared, cached, logged, or rendered in a notification.

### G4.5 (MEDIUM) — Assistant accepts an unbounded number of messages · `M4`

**FIXED (Wave 2).** `AssistantSettings.MaxMessagesPerTurn` (default 20, range-validated at startup) caps `Messages.Count`; the validator rejects over-limit turns. Covered by new validator tests.

`Features/Assistant/AssistantChatValidator.cs:12-32` caps each message at 4000 chars and requires the
last to be from the user, but **`Messages.Count` has no maximum**. A client can post 10,000 messages
of 4000 chars — 40 MB forwarded to a paid LLM API. The daily counter counts *turns*, not tokens, so
one turn can cost arbitrarily much.

**Fix:** cap via `settings.Assistant.MaxMessagesPerTurn`, not a `const` (backend owns config).

### G4.6 (MEDIUM) — `take` is unbounded on the Scheduler client list · `M8`

**FIXED (Wave 2).** `SchedulerSettings.ClampTake` (`DefaultPageSize` 50 / `MaxPageSize` 100) added; `ListClients` now clamps `take`. Covered by a `ClampTake` boundary theory.

`Features/Clients/ListClients/ListClientsEndpoint.cs:10-21` — `handler.HandleAsync(q, skip ?? 0, take ?? 50, ct)`,
no clamp. `take=1000000` is honoured. Identity does it correctly:
`Math.Clamp(pageSize ?? 20, 1, 100)` (`ListUsersEndpoint.cs:21`). Combined with G10.1 this is the
cheapest way to stress the Scheduler.

The cap belongs in `SchedulerSettings`, which currently has **no limits section at all** — unlike
`LovingBoardsSettings`, which properly declares `MaxBoardsPerUser`, `DefaultPageSize`,
`MaxItemsPerBoard`. LovingBoards is the model here.

---

<a name="g5"></a>
# G5 — Streaming & the error contract

**Why grouped:** all five are about long-lived responses and what happens when they fail. G5.1 is the
root cause that makes the others hard to diagnose.

### G5.1 (HIGH) — Global exception middleware crashes a second time on any streaming endpoint · `H6`

**FIXED (Wave 1).** `HasStarted` guard added so a mid-stream failure rethrows instead of crashing a second time; `UnauthorizedAccessException` now maps to 401 (catch placed before the generic filter); covered by the new `JiApp.Common.Tests` suite.

`backend/src/JiApp.Common/Middleware/GlobalExceptionMiddleware.cs:20-44`

```csharp
catch (Exception ex) when (ex is not OperationCanceledException)
{
    logger.LogError(ex, "Unhandled exception occurred");
    context.Response.StatusCode = StatusCodes.Status500InternalServerError;   // throws if HasStarted
    context.Response.ContentType = "application/json";
    …
}
```

No `if (context.Response.HasStarted) throw;` guard. Three long-lived endpoints write headers
immediately and keep writing:

- `api.JiApp.LovingBoards/Features/Boards/StreamBoard/StreamBoardEndpoint.cs:54-64`
- `JiApp.YtDownloader/Features/Assistant/AssistantChatEndpoint.cs:120-130`
- `JiApp.YtDownloader/Features/StreamPreview/StreamPreviewEndpoint.cs`

**Failure scenario:** the DeepSeek client throws mid-stream. Setting `StatusCode` on a started
response throws `InvalidOperationException` inside the catch → the original exception is lost, the
connection tears down abruptly, and the log shows the wrong error. This degrades exactly the
endpoints that are hardest to debug.

Same file: **`UnauthorizedAccessException` is not special-cased.** `CurrentUserService.EvaluateUserId()`
(`JiApp.Common/Services/CurrentUserService.cs:20`) throws it when the identity claim is missing —
surfacing as a **500 with a stack trace in Development** instead of a 401.

**Fix:**
```csharp
catch (Exception ex) when (ex is not OperationCanceledException)
{
    logger.LogError(ex, "Unhandled exception occurred");
    if (context.Response.HasStarted) throw;   // let the host abort the stream
    …
}
catch (UnauthorizedAccessException) { /* 401 */ }
```

### G5.2 (MEDIUM) — `boardStreamService` can resurrect a stream after the consumer closed it · `M10`

**FIXED (Wave 7).** Mobile SSE closed-guard: `userClosed` (set only by `close()`, never reset) split from `reconnecting` mid-refresh guard; no zombie stream after `close()` during a 401 refresh. W7-D PR #133.

`mobile/src/modules/lovingBoards/services/boardStreamService.ts:138-179`

```ts
es?.close();
es = null;
closed = true;                 // (a) used as a re-entrancy flag, not "consumer closed"
try {
  … await axios.post(refresh) …
  closed = false;              // (b) unconditionally reopens
  await startConnection(true);
```

`closed` carries two meanings: "the consumer called `close()`" and "we're mid-reconnect". If the
component unmounts during the refresh round-trip, `close()` sets `closed = true`, then line (b)
overwrites it to `false` and reconnects — a **zombie SSE connection with live callbacks into an
unmounted tree**.

**Fix:** separate the flags — `let userClosed = false` (never reset) and a local `reconnecting`.

### G5.3 (MEDIUM) — `StreamPreviewEndpoint` is 130 lines of process lifecycle in a route lambda · `N10`

**FIXED (Wave 7).** Preview-stream lifecycle extracted into owned `AudioPreviewStream : IAsyncDisposable` (yt-dlp+ffmpeg processes, kill-on-timeout/dispose); endpoint slimmed. W7-E PR #135.

`JiApp.YtApi/YoutubeClient.cs:27` declares `Process BuildPreviewAudioProcess(string videoId)` on the
adapter interface. The consequence is `Features/StreamPreview/StreamPreviewEndpoint.cs:24-151` —
process start ordering, stdout/stderr piping, timeout callbacks, and kill-on-error/kill-on-completion
cleanup inside the endpoint. Simultaneously the worst adapter-isolation breach and the worst VSA
fat-endpoint in the codebase.

Runner-up: `AssistantChatEndpoint.cs:75-139` — SSE orchestration, language normalisation, and three
localized response factories inline in the endpoint class.

**Fix:** return an owned `IAudioPreviewStream : IAsyncDisposable` from the adapter so the endpoint is
`Results.Stream(preview.GetAudioStream(), "audio/mpeg")`.

### G5.4 (MEDIUM) — No retry on the two most failure-prone remote calls · `N14`

**FIXED (Wave 7).** Retry added: `RetryingChatClient : IChatClient` decorator for DeepSeek (stream-start retry), both `YoutubeClient` `ExecuteAsync` sites wrapped, `SingleTryGoogleHttpClientFactory` (`NumTries=1`) so Polly owns retry. W7-E PR #135.

`DeepSeekChatClientProvider.cs:30-39` builds the `IChatClient` with `UseFunctionInvocation` and no
resilience pipeline — a single 503/429 from DeepSeek ends the whole chat turn and the user retypes.
`YoutubeClient.cs:58,74` call the Google API with no retry; the only fallback (line 128) is a
strategy *change*, not a retry. `IRetryPolicyFactory` exists and is used only by the security-stamp
validator.

### G5.5 (LOW) — `StreamBoardEndpoint` drops an event on loop exit · `L16`

`StreamBoard/StreamBoardEndpoint.cs:75-102` — the `Task.WhenAny` loop abandons the losing task on
`break`; an event already read by the dangling `readTask` is silently dropped.

---

<a name="g6"></a>
# G6 — Mobile project non-negotiables

**Why grouped:** these are the three rules `docs/agents/README.md` declares non-negotiable. Project
docs override the global standards, so these outrank most Mediums above regardless of technical severity.

### G6.1 (HIGH) — ~76 hardcoded user-facing strings, ~95% in the Scheduler module · `H8` + `C1` + `C2`

**FIXED (Wave 3).** PR #99 extracted every Scheduler user-facing string into a new `scheduler` i18n namespace — 126 keys + plural forms in both `en.json` and `pl.json` — including `SearchBar`'s clear label and the `AppointmentCard` status badges. The `i18n.test.ts` key-parity guard now covers it.

> Rule: *"All user-facing strings use i18n — no hardcoded text; every key must exist in both `en.json` and `pl.json`."*

**Corrected count: ~76.** (Pass 1 reported 45 by grepping only `<Text>` children; pass 2 found 31
more in `Alert.alert()` titles/messages, `placeholder=`, and `accessibilityLabel=`.)

12 Scheduler files — every screen and component except `BoardManagementScreen.tsx`:

```
modules/scheduler/screens/AppointmentDetailScreen.tsx:94,102,117,126,136,144,154,164,182,191,202
   + Alert.alert() at 40,49,54,66,74,84  (6 calls, hardcoded titles AND messages)
modules/scheduler/screens/CreateAppointmentScreen.tsx:239,242,247,263,281,288,299,304,309,314,321,326,333,338,353
   + Alert.alert('Validation', …) at 184, Alert.alert('Error', …) at 208-211
   + placeholder="YYYY-MM-DD" (247), placeholder="HH:mm" (304)
modules/scheduler/screens/ReportsScreen.tsx:21,24-27,34,48,53-56,119,136,145,162,169,186,197,207,219
modules/scheduler/screens/ClientListScreen.tsx:113,122,136,145,160,169,177
modules/scheduler/screens/ServiceListScreen.tsx:87,97,125,139,151
modules/scheduler/screens/ServiceEditScreen.tsx  (entire screen; incl. hardcoded "PLN" at 216)
modules/scheduler/screens/WeekendGridScreen.tsx:76,78,107,113,121,131,140,155
modules/scheduler/screens/ClientDetailScreen.tsx:79,87,113
modules/scheduler/components/BoardSelector.tsx:45,52-63,84,87,106-107,127,140,150,161,172
modules/scheduler/components/SummaryBar.tsx:36,41,48,53
modules/scheduler/components/ClientPicker.tsx:117,131,140
modules/scheduler/components/WeekendNavigator.tsx:29,43,52
modules/scheduler/components/DayColumn.tsx:45
modules/scheduler/components/DayTotalFooter.tsx:17,21,28
```

**And one outside Scheduler** — pass 1 wrongly claimed there were none:

```
components/SearchBar.tsx:136   accessibilityLabel="Clear search"
```

Screen-reader-only text in a core shared component, which is why the `<Text>` grep missed it. A
Polish user with TalkBack hears an English label app-wide.

**Fix:** extract to `scheduler.*` keys in both `en.json` and `pl.json`. The existing
`i18n/__tests__/i18n.test.ts` key-parity test then guards them.

### G6.2 (HIGH) — Three network `<Image>` without `onError` · `H9`

**FIXED (Wave 3).** PR #99 added `onError` state + grey-box placeholder to `HistoryItem`, `VideoCard` and `DownloadScreen`; covered by 3 new tests.

> Rule: *"Every `<Image>` with a network `uri` has `onError` fallback — broken URLs must render a placeholder, not a blank box."*

Verified — none of these files contains `onError`:

```
mobile/src/components/HistoryItem.tsx:89-94    source={{ uri: downloadItem.imageUrl }}
mobile/src/components/VideoCard.tsx:46-51      source={{ uri: video.imageUrl }}
mobile/src/screens/DownloadScreen.tsx:164-169  source={{ uri: imageUrl }}
```

All three render YouTube thumbnails, which 404 routinely when a video is removed or region-blocked —
precisely the case the rule exists for. `VideoCard` and `HistoryItem` are list rows, so a bad URL
leaves a blank hole mid-scroll.

**Fix:** add `onError` state + a local placeholder. `VideoCard.test.tsx` and `HistoryItem.test.tsx`
already exist — add an assertion to each.

### G6.3 ✅ CLEAN — Hooks before early returns · `C3`

> Rule: *"All hooks appear before any conditional return."*

Pass 1 marked this **Unverified** (CI has no ESLint step). Pass 2 checked all 43 screens and
components by hand: **zero violations**, including in the five longest screens. The rule is being
followed by discipline.

**No fix needed — and now machine-guarded:** PR #101's `eslint` gate (G12.1) enforces `react-hooks/rules-of-hooks` and `react-hooks/exhaustive-deps` with `--max-warnings=0`, so the rule can no longer regress silently.

---

<a name="g7"></a>
# G7 — Mobile boundary validation & layering

**Why grouped:** all four are "untrusted data entered the app without being validated, or I/O
happened outside a service". Three of four centre on Scheduler, matching G6.

### G7.1 (MEDIUM) — Scheduler services perform zero runtime validation · `M9`

**FIXED (Wave 3).** PR #100 added 12 Zod schemas; all 6 Scheduler services now parse at the boundary instead of blind-casting. The backend `AppointmentResponse` now returns nested `Client`/`Service`, so the wire shape matches the schemas.

`mobile/src/modules/scheduler/services/*.ts` — **0 `parse`/`safeParse` calls across all 6 services.**

```ts
export const listAppointments = async (boardId: number, dates: string[]): Promise<Appointment[]> => {
  const response = await apiClient.get<Appointment[]>('/scheduler/appointments', { params: { boardId, date: dates } });
  return response.data;      // blind cast — the generic is a lie at runtime
};
```

Unvalidated endpoints: `appointmentService.ts:34,43,49` · `clientService.ts:37,41,48` ·
`boardService.ts:13,23` · `expenseService.ts:48,56,63` · `reportService.ts:10,20` ·
`serviceCatalogService.ts:26,38,45`.

Root cause: `modules/scheduler/types/api.ts` declares all 11 types as hand-written interfaces with no
Zod schemas. Contrast `modules/admin/types/api.ts`, `modules/lovingBoards/types/api.ts` and
`src/types/api.ts`, which all use `z.object()` + `z.infer` correctly. Scheduler is the sole holdout.

**Fix:** add `modules/scheduler/types/schemas.ts` mirroring the LovingBoards pattern; infer the types
from the schemas.

### G7.2 (MEDIUM) — Screens perform I/O directly, bypassing the service layer · `N5`

**FIXED (Wave 6, PR-F #125).** Screens no longer perform I/O or import axios: new `serverWakeService` (fetch + AbortController + Zod boundary) plus hooks `useServerWake`/`useRememberMe`/`useSearchHistory`/`useDownloads`/`useLanguage`/`usePersistedModule` own the logic and screens compose; `errorUtils.getFriendlyErrorMessage` is the single axios-aware place (server-provided messages now win). Verified tsc clean, jest 724/724, eslint `--max-warnings=0`; grep proof — no `import axios` / no `fetch(` in `screens/`.

```
screens/ServerWakeScreen.tsx:99    const response = await fetch(healthUrl, {
screens/ServerWakeScreen.tsx:118   await fetch(WAKE_API_URL + '/start', { method: 'POST' });
screens/EditProfileScreen.tsx:4    import axios from 'axios';
screens/LoginScreen.tsx:5          import axios from 'axios';
screens/RegisterScreen.tsx:4       import axios from 'axios';
```

`react-native-encapsulation` states services are the only I/O boundary. `ServerWakeScreen` calls
`fetch` twice with no service, no Zod validation of the health response, and no shared
timeout/retry. The three `axios` imports are for error classification, which belongs in
`utils/errorUtils.ts` or a service — and `errorUtils.ts` is itself coupled to axios types, so the
abstraction leaks in both directions.

Also importing services directly into screens/components rather than going through a hook:
`EditProfileScreen.tsx:5` (`authService`), `LoginScreen.tsx:13` (`storageService`),
`SearchScreen.tsx:15` (`searchService`), `DownloadsScreen.tsx:11` (`downloadService`),
`components/LanguagePicker.tsx:3,22` (`storageService`), `navigation/RootNavigator.tsx:7,55,84`
(`storageService` in a navigator).

### G7.3 (LOW) — Type assertions launder unvalidated persisted data · `N15`

**FIXED (Wave 3).** PR #100 replaced the `expenseService` `category as Expense['category']` cast with `ExpenseApiRawSchema` enum validation. (The three storage-read casts in `storageService.ts` and `ThemeContext.tsx` remain open — not addressed by Wave 3.)

```
services/storageService.ts:130                   return value as ModuleId | null;
context/ThemeContext.tsx:66                      setPaletteState(stored as PaletteName)
context/ThemeContext.tsx:74                      setThemeModeState(storedMode as ThemeMode)
modules/scheduler/services/expenseService.ts:39  category: raw.category as Expense['category']
```

`zod-boundary-validation` counts storage reads as an external boundary. The first three cast an
arbitrary stored string to a union type; a stale value from an older app version silently produces
an invalid state. The fourth casts an **unvalidated API response field** — a double violation on a
file that already has no schema (G7.1).

### G7.4 (MEDIUM) — Five screens over 450 lines with logic that belongs in hooks

**FIXED (Wave 6, PR-G #128).** All five oversized screens decomposed into hooks — lovingBoards `useItemSheet`/`useBoardDetail`/`useUndoSnackbar`, admin `useUserDetailScreen`, `useEditProfile` and Scheduler `useCreateAppointment` — screens now compose. The same PR added the G9.1 admin `UserDetailScreen` tests (16) and fixed the BoardDetail snackbar-timer cleanup leak (see the G9 latent notes).

`modules/lovingBoards/screens/ItemSheet.tsx` (653) · `BoardDetailScreen.tsx` (635) ·
`modules/admin/screens/UserDetailScreen.tsx` (580) · `screens/EditProfileScreen.tsx` (510) ·
`modules/scheduler/screens/CreateAppointmentScreen.tsx` (472).

`react-native-encapsulation`: screens compose, hooks hold logic. `UserDetailScreen` at 580 lines also
has zero test coverage (G9.1).

---

<a name="g8"></a>
# G8 — Extract-the-pattern refactors

**Why grouped:** this is the highest-leverage group. Every item is *one correct implementation and N
divergent copies*. None requires designing anything new — the right version already exists in the
repo. Completing G8 collapses a large share of the Medium tier as a side effect.

### G8.1 (MEDIUM) — No centralised `Result<T>` → HTTP mapping; three incompatible strategies

`JiApp.Common/Abstractions/ResultExtensions.cs` has no `ToHttp()`. Every service inlines its own
mapping, yielding three strategies:

| Strategy | Where | Shape |
|---|---|---|
| int + `Results.Json` | 21 Identity endpoint files | `int statusCode = result.ErrorCategory switch {...}; Results.Json(body, statusCode)` |
| direct `Results.Xxx()` | Scheduler, LovingBoards | `result.ErrorCategory switch { NotFound => Results.NotFound(...), ... }` |
| hardcoded single code | YtDownloader | `Results.Json(body, statusCode: 404)` |

And the fallback arm differs between sibling slices: `CreateAppointmentEndpoint.cs:32` → `Conflict`,
`DeleteBoardEndpoint.cs:23` → `BadRequest`, `GetAppointmentEndpoint.cs:21` → `NotFound`,
`UpdateBoardEndpoint.cs:31` → `NotFound`. Several endpoints collapse *all* errors to `BadRequest`
(`CreateBoardEndpoint.cs:27`, `ListBoardsEndpoint.cs:17`, `ListClientsEndpoint.cs:20`, and the
LovingBoards equivalents) — so `NotFound` and `AccessDenied` come back as 400.

**Fix:** one `ToHttp<T>()` in `ResultExtensions.cs`; replace ~40 switch expressions. Also fixes the
endpoint half of G3.2.

**FIXED (Wave 4).** Shared `ToHttp<T>()` in `JiApp.Common/Abstractions/ResultExtensions.cs`; 61 mapping sites migrated (Identity 9, Scheduler 31, LovingBoards 14, YtDownloader 7). Uniform mapping incl. AccessDenied→404 (G3.2 endpoint half), uncategorized `_`→500, new `BadGateway`→502. PR #104 (main `af74705`).

`backend/src/JiApp.Common/Abstractions/ResultExtensions.cs` · `backend/src/JiApp.Common.Tests/ResultExtensionsTests.cs`

### G8.2 (MEDIUM) — Shared JWT validation parameters · rolls up `G1.3` + `G1.6`

`JwtTokenService.CreateValidationParameters()` is already the correct implementation. Promote it to
`JiApp.Common`, call it from all five services, delete the four hand-rolled
`TokenValidationParameters` literals and the four `JwtSettings` copies.

**FIXED (Wave 4).** New shared `JiApp.Common/Authentication/TokenValidationParametersFactory.cs` — single canonical `TokenValidationParameters` (`ValidateIssuer`/`Audience`/`SigningKey`/`Lifetime`, `ClockSkew=0`, `ValidAlgorithms=["HS256"]`) — now used by all five services; Identity's `JwtTokenService.CreateValidationParameters` deleted. Four `JwtSettings` copies replaced by one shared `JiApp.Common/Authentication/JwtSettings.cs` (accumulating `Validate()`, whitespace-only key/issuer/audience rejected). **Behavior change (intended, = G1.3):** the four consumers now reject expired tokens exactly (were 5-min skew). Also closes G1.3, G1.6, G11.14. Covered by new `TokenValidationParametersFactoryTests` + `JwtSettingsTests`. PR #103 (main `80c09c0`).

### G8.3 (MEDIUM) — `"permission"` claim type is a magic string in 12 places across 7 files · `M11`

```
JiApp.Common/Authorization/PermissionAuthorizationHandler.cs:9
JiApp.Identity/Services/JwtTokenService.cs:68            (writes the claim)
JiApp.Identity/Services/UserAccessService.cs:42
JiApp.Identity/Services/RoleSeeder.cs:59, 67, 78, 106
JiApp.Identity/Features/Admin/Roles/ListRoles/ListRolesHandler.cs:19
JiApp.Identity/Features/Admin/Roles/CreateRole/CreateRoleHandler.cs:34
JiApp.Identity/Features/Admin/Roles/UpdateRolePermissions/UpdateRolePermissionsHandler.cs:40, 46, 53
```

This string decides authorization. A typo in the writer fails **open**; in the reader, **closed** —
silently, with no compile error. `Permissions.cs` and `RoleNames.cs` already exist as constant
holders; the claim type never got one.

**Fix:** `public const string PermissionClaimType = "permission";` in `JiApp.Common.Permissions`.

**FIXED (Wave 4).** `Permissions.PermissionClaimType = "permission"` added; all 12 magic-string sites + test-side literals replaced. PR #103 (main `80c09c0`).

### G8.4 (MEDIUM) — Zero `TimeProvider`; 33 direct `DateTime.UtcNow` call sites · `M16`

`grep -rln TimeProvider backend/src` → **no matches**. 33 `DateTime.UtcNow` hits outside migrations,
including domain field initialisers:

```csharp
api.JiApp.LovingBoards/Domain/Board.cs:11        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
api.JiApp.LovingBoards/Domain/BoardItem.cs:18-19
JiApp.Scheduler/Domain/Appointment.cs:22, Board.cs:10
```

Visible consequence: `WeeklyResetTests` is clean because `WeeklyReset` takes `nowUtc` as a parameter
— but nothing touching `CreatedAt`/`UpdatedAt`/expiry can assert on time, so those assertions simply
do not exist. `TempFileStore` expiry, refresh-token expiry and the weekly-reset trigger are all
untestable at the boundary. `RetryPolicyFactory` also lacks a `TimeProvider`, so retry delays are
wall-clock-bound.

**Fix:** register `TimeProvider.System`; inject into handlers, `TempFileStore`, `RefreshTokenService`,
`RetryPolicyFactory`; set `CreatedAt` in the handler, not the field initialiser.

**FIXED (Wave 4).** `TimeProvider.System` registered in all 5 service Startups; injected into handlers,
`RefreshTokenService`, `JwtTokenService`, `RefreshTokenCleanupService`, `RetryPolicyFactory`, and the
3 YtDownloader sites. Entity initializers removed; `CreatedAt`/`UpdatedAt` stamped by creating handlers
(`CreateAppointmentHandler`, `CreateBoardHandler`, `CreateItemHandler`, `ListBoardsHandler` seeder).
Note: `TempFileStore` does not exist repo-wide — dropped. `RetryPolicyFactory` sets `TimeProvider` on
`ResiliencePipelineBuilder` (no such member on `RetryStrategyOptions` in Polly 8.5.2). Grep proof: only
the 5 `DateTime.UtcNow` health-endpoint timestamps remain. PR #106 (main `ec09700`).

### G8.5 (MEDIUM) — Composition roots are one monolithic method each · `M20`

| File | `ConfigureServices` length |
|---|---|
| `JiApp.Identity/Startup.cs:49-231` | 182 lines |
| `JiApp.Scheduler/Startup.cs:53-207` | 154 lines |
| `JiApp.Gateway/Startup.cs:22-161` | 139 lines |
| `JiApp.YtDownloader/Startup.cs:40-171` | 131 lines |
| `api.JiApp.LovingBoards/Startup.cs:37-148` | 111 lines |

`dotnet-composition-root` requires grouping into `private static ConfigureXxx(IServiceCollection)`.
Scheduler alone has ~50 consecutive `AddScoped` calls. The `DependencyInjectionTests` conventions
added in #78 cover *resolvability*, not organisation, so this drifts unchecked.

**FIXED (Wave 4).** All five composition roots split into `private static ConfigureXxx` groups
(`ConfigureInfrastructure`/`ConfigureOpenApi`/`ConfigurePersistence`/`ConfigureAuth`/`ConfigureCors`/
`ConfigureApplicationServices`/`ConfigureFeatureHandlers`/`ConfigureRateLimiting`/`ConfigureBackgroundServices`;
Gateway adds `ConfigureReverseProxy` + `ConfigureHttpClients`). `ConfigureServices` stays the instance entry
point (DI fixtures call `startup.ConfigureServices(Services)`); `Program.cs` and `Configure(WebApplication)`
untouched. smart-auditor verified registration type-sets byte-identical vs pre-change — pure reorder, zero
semantic change. PR #108 (main `42182e8`).

### G8.6 (MEDIUM) — Identity's four rate-limit policies are copy-paste with magic-string names · `M21`

`JiApp.Identity/Startup.cs:197-227` — four blocks differing only in name and `PermitLimit`. Names
`"Register"`, `"Login"`, `"Refresh"`, `"Logout"` are string literals in both `Startup.cs` and each
endpoint's `.RequireRateLimiting("…")`. The Gateway solved this with `RateLimitPolicyNames` constants
**and** config-driven policies (`GatewaySettings.RateLimiting`); Identity uses neither. A typo in
`.RequireRateLimiting("Logn")` throws only when that route is first hit.

**FIXED (Wave 4).** Identity now mirrors the Gateway shape: new `JiApp.Identity.RateLimiting.RateLimitPolicyNames`
consts (`LoginPolicy`/`RegisterPolicy`/`RefreshPolicy`/`LogoutPolicy`), `IdentitySettings.RateLimiting`
dictionary + nested `[Serializable] RateLimitPolicyConfig`, `Validate()` fail-fasts on a missing section/key,
and a config-driven `AddRateLimiter` loop naming policies `sectionName + "Policy"`. Partition key stays
`RateLimitPartitioning.GetPartitionKey(httpContext)` (per-user, not `RemoteIpAddress`); no `OnRejected` writer
(429 body unchanged). Same effective limits (Login 10, Register 5, Refresh 10, Logout 10/min). Grep proof: no
`RequireRateLimiting("` literals remain, 6 `RateLimitPolicyNames.` hits. PR #108 (main `42182e8`).

### G8.7 (MEDIUM) — `JiApp.Testing.Common` depends on `JiApp.Scheduler` · `M17`

`backend/tests/JiApp.Testing.Common/JiApp.Testing.Common.csproj:18`

```xml
<ProjectReference Include="..\..\src\JiApp.Scheduler\JiApp.Scheduler.csproj" />
```

The *shared* test-infrastructure library references one concrete service, because
`Bases/HandlerTestBase.cs:1-26` hardcodes `SchedulerDbContext`/`ISchedulerDbContext`. Every test
project that wants the shared assertions transitively pulls in the Scheduler assembly. The cost is
already paid: LovingBoards wrote its own `Bases/LovingBoardsHandlerTestBase.cs` (95% identical, plus
a `PRAGMA foreign_keys = ON` line). Two parallel base classes for one job.

**Fix:** `HandlerTestBase<TDbContext> where TDbContext : DbContext`; drop the project reference;
delete the LovingBoards duplicate (keeping its PRAGMA line in the generic base).

**FIXED (Wave 4).** PR #110 genericized `HandlerTestBase<TDbContext>` (`where TDbContext : DbContext`,
instantiated via `Activator` on the primary `DbContextOptions<TDbContext>` ctor — no `new()` constraint),
dropped the `..\..\src\JiApp.Scheduler` ProjectReference from `JiApp.Testing.Common.csproj`, and deleted
the LovingBoards duplicate (its `PRAGMA foreign_keys = ON` line folded into the generic base). 10
Scheduler + 4 LovingBoards test classes renamed to the generic base. **Behavior change:** Scheduler
tests now run with SQLite `foreign_keys = ON` (was off) — all 240 pass, no fixture store-order
remediation needed. Grep proof: zero `JiApp.Scheduler` references in `JiApp.Testing.Common/`, zero
`LovingBoardsHandlerTestBase` in `backend/`. PR #110 (main `b1ba6d3`).

### G8.8 (MEDIUM) — Vendor SDK exception types leak past the `IYoutubeClient` adapter · `N9`

`JiApp.YtDownloader/Features/SearchVideos/SearchVideosHandler.cs:107` and
`JiApp.YtDownloader/Agent/YtAgentToolService.cs:96` both catch `Google.GoogleApiException`.

`IYoutubeClient` is the owned adapter interface and declares no thrown-exception contract, yet two
call sites in a different project branch on a Google SDK type. Swapping the provider means editing
every handler, not just the adapter.

**FIXED (Wave 4).** New owned `JiApp.YtApi.YoutubeApiException`; `YoutubeClient` wraps the two `ExecuteAsync` call sites (`SearchVideosAsync`, `GetVideoByIdAsync`) catching `Google.GoogleApiException` and rethrowing the owned type with the Google exception as inner. The two YtDownloader call sites (`SearchVideosHandler.cs`, `YtAgentToolService.cs`) now catch `YoutubeApiException` — behavior unchanged (log + generic failure; handler keeps `ResultCategories.BadGateway`). Grep proof: `Google.GoogleApiException` appears only in `YoutubeClient.cs` (the wrap). PR #112 (main `687d0ca`).

---

<a name="g9"></a>
# G9 — Test standards & coverage

**Why grouped:** all eight are "the safety net has a hole". Do G9.3 before G9.1 — the mocks must be
rebuilt before the missing specs can be written cleanly.

**RESOLVED (Wave 5).** All eight findings fixed: G9.3 PR-A #117, G9.4 PR-B #115, G9.5 PR-C #114, G9.2 PR-E #116, G9.1/G9.7/G9.8 PR-D #118, G9.6 PR-F #119. New latent findings surfaced during Wave 5 are recorded at the end of this group.

### G9.1 (MEDIUM) — Coverage map, with the privileged surfaces called out · `M19` + expanded

| Directory | prod | tests | stories | Verdict |
|---|---|---|---|---|
| `modules/admin/screens` | 5 | **0** | **0** | ❌ RBAC UI: role permission toggles, user create/delete/disable/reset-password. Zero of either. |
| `modules/admin/services` | 1 | **0** | 0 | ❌ `adminService` untested |
| `modules/lovingBoards/*` | 18 | **0** | 5 | ❌ entire module untested — incl. SSE `boardStreamService` and `useItemReminders` |
| `modules/scheduler/screens` | 8 | 1 | 8 | ❌ 7/8 untested; 8 stories exist and are unused |
| `modules/scheduler/components` | 8 | **0** | 7 | ❌ 0/8 tested; **7 stories exist, 0 consumed** |
| `modules/scheduler/hooks` | 6 | 3 | — | ⚠️ `useBoard`, `useReports`, `useWeekendGrid` untested |
| `src/components` | 32 | 23 | 17 | ⚠️ 8 untested (PalettePicker, RefreshableScrollView, SegmentedControl, TabBarButton, Toast, ToastContainer, ChatMessageList, ChatVideoResults) |
| `src/screens` | 13 | 10 | 10 | ⚠️ ChatScreen, ProfileSection, ChangePasswordSection untested |
| `src/hooks` | 9 | 6 | — | ⚠️ `useAuth`, `useKeepAwake`, `useToast` untested |
| `src/services` | 9 | 6 | — | ⚠️ historyService, playbackService, previewService untested |
| `src/context` | 4 | 2 | — | ⚠️ BoardContext, ToastContext untested |
| `src/utils` | 4 | 3 | — | ⚠️ `permissions.ts` untested (maps permissions → visible modules) |

**Top 5 highest-value missing tests:** (1) `modules/lovingBoards` end-to-end — real-time sync logic
is impossible to verify manually; (2) `modules/admin/screens` — the privilege-escalation surface;
(3) `screens/ChatScreen.tsx` — the core feature, SSE + tool-call rendering; (4) `hooks/useAuth.ts` —
7 methods, 5 state fields, only tested indirectly through `AuthContext`; (5) the 7 Scheduler screens,
whose fixtures are already written as stories.

**FIXED (Wave 5, PR-D #118).** Screen tests added for the called-out module surfaces: scheduler `WeekendGridScreen`/`ClientListScreen`/`CreateAppointmentScreen`, lovingBoards `BoardListScreen`/`BoardDetailScreen`/`ItemSheet`/`BoardMembersScreen`, admin `UserListScreen` — built on the PR-A builder mocks. The ⚠️ rows (`src/components`, `src/screens`, `src/hooks`, `src/services`, `src/context`, `src/utils`) remain on the backlog. **FIXED (Wave 6, PR-G #128) — addendum:** the admin ❌ `modules/admin/screens` row is now closed — `UserDetailScreen` covered by 16 tests via the new `useUserDetailScreen` hook (the last admin screen with zero coverage).

### G9.2 (MEDIUM) — Identity handler tests are 100% mock-only · `N6`

Across `JiApp.Identity.Tests/`, exactly **one** file references `IdentityDbContext` —
`Services/RefreshTokenServiceTests.cs`. All 20 handler test classes mock `UserManager`,
`RoleManager`, `SignInManager`.

`persistence-testing` names this explicitly ("mocking repositories to 'unit test' persistence logic
— you are testing your mocks"). Never exercised anywhere in the Identity suite: unique-constraint
violation on duplicate username/email (the exact path `RegisterHandler.IsUniqueConstraintViolation`
exists to handle), cascade delete of `RefreshTokens`, identity-column generation, the security-stamp
concurrency token.

**This is why G2.2 is invisible to a green suite:** the mocked `IRefreshTokenService` has no
transaction semantics to roll back.

**FIXED (Wave 5).** PR-E #116 added real-context handler tests; PR-F #119 added the full-pipeline integration suite (real HTTP through routing, auth middleware, handlers, and a real migrated in-memory SQLite store) exercising exactly the paths named above — duplicate register/email unique-constraint, security-stamp rotation, role claims, rate-limit policies.

### G9.3 (MEDIUM) — Mobile mock services use mode-flag state machines · `N8`

`modules/scheduler/services/__mocks__/appointmentService.ts` (155 lines) · `clientService.ts` ·
`expenseService.ts` · `modules/lovingBoards/services/__mocks__/boardService.ts`

Each exposes `setAppointmentMode('success' | 'empty' | 'error')`-style global flags instead of the
`.withAppointments(...)` / `.withAppointmentError()` builders `semantic-test-doubles` prescribes. A
`getThisWeekend()` date helper is duplicated across three of them.
`modules/admin/services/__mocks__/adminService.ts` is a raw `jest.fn()` grab-bag with no builders and
no `reset()`. `services/__mocks__/previewService.ts` uses `createMockFn` but exposes no scenario builders.

**The backend implements this standard exemplarily** (`MockUserManager.GetSuccessful()`,
`WithFindByNameAsync(...)`). The two stacks sit at opposite ends of the same rule — copy the backend shape.

**FIXED (Wave 5, PR-A #117).** Builder-style test doubles (`.withX(...)` + `reset()`) replace the mode-flag state machines for `appointmentService`/`clientService`/`expenseService`/`boardService`; `adminService` and `itemService` get builders + reset. Body above describes the pre-A state.

### G9.4 (MEDIUM) — The stories-as-fixtures foundation is built and unadopted · `N7`

Verified across `mobile/src/**/*.test.tsx`:

```
files using rtlRender (the provider-wrapping custom render):   3
files importing raw render from @testing-library/react-native: 47
files using composeStories:                                     3
```

PR #80 landed `src/test/rtlUtils.tsx`, `src/test/mocks/mockServices.ts`, and 46 story files. Three
test files use it. The other 47 hand-wire providers and rebuild prop fixtures a story already
defines — `ErrorMessage`, `FormInput`, `SearchBar`, `LanguagePicker`, `HistoryItem`, `HistorySection`,
`LoadingSpinner`, `Logo`, `RegisterScreen`, `SettingsScreen`, `HistoryScreen`, `SearchScreen`,
`DownloadScreen`, `ModuleSelectionScreen` all have a story AND a test that ignores it.
`modules/scheduler/components/` is the sharpest case: **7 stories, 0 tests.**

**FIXED (Wave 5, PR-B #115).** composeStories adopted for the 7 Scheduler component stories. Body above describes the pre-B state.

### G9.5 (MEDIUM) — No test project for `JiApp.Common` · `M18`

The kernel — `Result<T>`, `GlobalExceptionMiddleware`, `CurrentUserService`,
`PermissionAuthorizationHandler`, `RetryPolicyFactory`, `SecurityStampRecheckFilter`,
`RemoteSecurityStampValidator` — has no test project. What tests exist are parked in the wrong place:

```
JiApp.Scheduler.Tests/Security/RemoteSecurityStampValidatorTests.cs
JiApp.Scheduler.Tests/Security/SecurityStampRecheckFilterTests.cs
JiApp.Scheduler.Tests/Resilience/RetryPolicyFactoryTests.cs
```

Direct result: `GlobalExceptionMiddleware` (G5.1) and `CurrentUserService` are **completely
untested** — the two components every request passes through.

**FIXED (Wave 5, PR-C #114).** `JiApp.Common.Tests` test project added covering the kernel — `Result<T>`, `GlobalExceptionMiddleware`, `CurrentUserService`, `PermissionAuthorizationHandler`, `RetryPolicyFactory`, `SecurityStampRecheckFilter`, `RemoteSecurityStampValidator`.

### G9.6 (MEDIUM) — Integration coverage by service

| Service | Full-pipeline tests | Risk |
|---|---|---|
| Gateway | ✅ `GatewayWebApplicationFactory` + 51 tests | — |
| YtDownloader | ✅ `AssistantChatIntegrationTests` | — |
| **Identity** | ❌ **none** | **Highest.** The auth core. Routing, model binding, the `OnTokenValidated` stamp check, the JWT challenge path, and the rate-limit policies are never exercised end-to-end. |
| Scheduler | ❌ none | Medium — the tenancy guards (G3.1, G3.2) are exactly what a pipeline test would catch. |
| LovingBoards | ❌ none | Medium — SSE endpoint untested at any level. |

`JiApp.Testing.Common/Bases/IntegrationTestBase.cs` is an **empty class** — it names a capability the
repo does not have, which is why Gateway and YtDownloader each built their own factory.

**FIXED (Wave 5, PR-F #119).** Identity full-pipeline suite shipped via a new ad-hoc `IdentityWebApplicationFactory` (real migrated in-memory SQLite on a shared connection object; closed-`IOptions` rate-limit budget override): register/login round trip, duplicate register + duplicate email, JWT role/security-stamp claims, invalid-password 401, rotated security-stamp 401 (the `OnTokenValidated` recheck), missing-header 401, non-admin 403, and isolated per-fact rate-limit 429s. `IntegrationTestBase.cs` remains empty — its fill is **deferred to Wave 6** (the factory is ad-hoc, not based on the shared base). **FIXED (Wave 6, PR-D #122) — addendum:** the deferral is closed — `IntegrationTestBase.cs` is filled as a two-layer `WebApplicationFactory` base (`IntegrationTestBase<TEntryPoint>` + `SqliteIntegrationTestBase<TEntryPoint,TDbContext>`), consolidating the duplicated Gateway / Identity / IdentityRateLimit factories (WSL inotify workaround, Test-env wiring, SQLite store-swap, instance-scoped shared connection, `InFreshScope` all promoted to the base). Behavior-preserving: Gateway 54 + Identity 171 tests pass unchanged.

### G9.7 (MEDIUM) — Missing negative-space assertions

Failure-path tests that assert the error but never assert the side effect did *not* happen:

```
mobile/src/screens/__tests__/RegisterScreen.test.tsx:144-158    never asserts mockNavigate NOT called
mobile/src/screens/__tests__/LoginScreen.test.tsx:102-113       never asserts mockNavigate NOT called
mobile/src/screens/__tests__/EditProfileScreen.test.tsx:326-341 never asserts mockShowSuccess NOT called
backend/tests/JiApp.Scheduler.Tests/Features/Appointments/AppointmentHandlerTests.cs:127-140
   asserts result.Value > 0 but never re-reads the store

**FIXED (Wave 5, PR-D #118).** Negative-space assertions added: `RegisterScreen`/`LoginScreen` assert `mockNavigate` NOT called on auth failure; `EditProfileScreen` asserts `mockShowSuccess` NOT called on email-taken.
```

### G9.8 (LOW) — Test-suite hygiene · `N16`

- `mobile/__tests__/App.test.tsx:20-23` — `test('renders without crashing')` calls `render(<App />)`
  with **zero assertions**. Also carries a duplicated `/** @format */` docblock (lines 1-7).
- `mobile/src/screens/__tests__/ModuleSelectionScreen.test.tsx:78-170` — uses
  `// Arrange` / `// Act` / `// Assert` comment markers, which `unit-test-anatomy` bans; also
  hand-wires the full provider tree at line 142.
- No mobile test name follows `Behavior_Scenario_Expected`. **Decision needed, not a fix** — this may
  be a JS-convention clash rather than a defect.

**FIXED (Wave 5, PR-D #118) — naming exemption recorded:** sentence-case `it('...')` test naming is the
mobile standard and stays; the backend keeps `Behavior_Scenario_Expected`. The hygiene items above are
fixed in PR-D (zero-assertion `App.test.tsx` gains an assertion, duplicate docblock removed,
`// Arrange/Act/Assert` markers stripped from `ModuleSelectionScreen.test.tsx`).

### New findings surfaced during Wave 5 (backlog — not fixed)

- **Identity register enumeration (G2.2-adjacent, M-level).** The default `UserValidator` pre-checks uniqueness via `FindByNameAsync`/`FindByEmailAsync` and leaks `"Username 'x' is already taken."` / `"Email 'x' is already taken."` before the `RegisterHandler` generic DB-constraint path (`RegisterHandler.cs:76`, `SqliteErrorCode 19 → "Registration failed"`) ever runs — `RegisterHandler.cs:23-27`'s no-enumeration comment is intent only. Pinned by the PR-F facts (assert current behavior with WHY; fix = neutralize the default `UserValidator` in `Startup.ConfigureIdentity`).
- **BoardDetailScreen snackbar-timer cleanup leak (new, mobile).** `BoardDetailScreen.tsx:111-118` cleanup reads `undoTimerRef`/`clearTimerRef` at mount (null) and never clears the 5s undo / 4s cleared-message timers assigned later — they outlive the component. Surfaced by the PR-D tests (required `jest.useFakeTimers()` for isolation). Fix = read the refs inside the cleanup. **FIXED (Wave 6, PR-G #128).** The undo/cleared-message timers moved into the new `useUndoSnackbar` hook, whose cleanup reads the refs it later sets — they no longer outlive the component.
- **useWeekendGrid Saturday-expense data-loss (new, mobile).** `useWeekendGrid.ts:51-58` loads Saturday then Sunday expenses; `useExpenses.loadExpenses` REPLACES state, so the Saturday load is always clobbered and the Saturday column renders empty (`DayColumn.tsx:34` filters by date). Pinned by the PR-D test (seeds Sat+Sun, asserts Sunday renders and the Saturday title is absent). Fix = merge instead of replace in `useExpenses.loadExpenses`.

---

<a name="g10"></a>
# G10 — Performance, scale & dead weight

### G10.1 (MEDIUM) — Two handlers load every board in the system into memory · `M7` + `N12`

**FIXED (Wave 6, PR-A #121).** `ListBoards`/`ListClients` no longer load the full board table — membership is now filtered in SQL (parameterized, boundary-anchored `LIKE` over the `MemberUserIds` JSON text, ORed with `OwnerUserId`) via `FromSqlInterpolated`/`SqlQuery<long>`, which is the G10.1 finding body's short-term fix (the long-term `BoardMembers` join table was not built). Covered by format-pinning, board-id boundary-set, and client-visible-boards tests (226 Scheduler tests pass).

`JiApp.Scheduler/Features/Clients/ListClients/ListClientsHandler.cs:12-20` and
`JiApp.Scheduler/Features/Boards/ListBoards/ListBoardsHandler.cs:13-18` — identical pattern:

```csharp
var userBoardIds = (await db.Boards
        .AsNoTracking()
        .ToListAsync(ct))                                        // full table into memory
    .Where(b => b.MemberUserIds.Contains(currentUser.UserId))    // client-side filter
    .Select(b => b.Id)
    .ToList();
```

`MemberUserIds` is a JSON column so it cannot be filtered in SQL — but the fix is not to materialise
the whole table. Cost grows with the total number of boards across **all** tenants, on every call.

**Short term:** narrow the query; fix at both sites at once. **Long term:** a `BoardMembers` join
table removes this, `BoardWriteLock` (G3.3), and the JSON `ValueComparer` requirement in one move.

### G10.2 (MEDIUM) — Four pieces of in-memory state break silently on a second replica · `M13` + `N11`

**FIXED (Wave 6, PR-B #123).** `SingleInstanceGuard` (file-based exclusive lease on the shared `jiapp_data` volume) added to all 5 deployable `Program.cs` — a duplicate replica logs **Critical** + `exit(1)` before serving traffic; `RateLimitPolicyService.EndpointCache` is now capped at `EndpointCacheMaxEntries` (default 4096). Decision recorded in §F: **horizontal scaling NOT intended — replicas pinned to 1**. Covered by `SingleInstanceGuardTests` + a fitness convention test scanning the 5 `Program.cs`.

| State | File | Symptom with 2 replicas |
|---|---|---|
| `TempFileStore._store` | `YtDownloader/Services/TempFileStore.cs:16` | A downloads the file; the `GET /downloads/mp3/file/{id}` lands on B, whose dictionary is empty → **404 on a file that exists**. B's cleanup also deletes A's files. |
| `BoardBroadcaster._boards` | `LovingBoards/Realtime/BoardBroadcaster.cs:10` | A subscriber on A never receives events published on B. **Silent event loss** — the board updates for one user and not the other. |
| `BoardWriteLock._locks` | `LovingBoards/Common/BoardWriteLock.cs:12` | Each instance holds its own semaphore → the lock stops serialising → the exact lost update it exists to prevent. |
| `AssistantStreamGate` / `DownloadSemaphore` | `Assistant/AssistantStreamGate.cs:10`, `GetDownloadLinkHandler.cs:22` | N replicas = N× intended concurrency. Capacity trap, not data loss. |

Two carry doc comments acknowledging the single-instance assumption. **None is enforced** — nothing
pins replicas to 1, nothing fails fast if a second starts.

Separately, all three are unbounded: `BoardBroadcaster` has no cap on subscribers per board or per
user (each `Subscribe` allocates a 100-slot channel); `BoardWriteLock._locks` never removes entries
or disposes semaphores; `RateLimitPolicyService.EndpointCache` is a **`static`** dictionary on a
singleton whose `CreatePolicyEndpoint` key is `(request path, policy)` — **partly attacker-controlled**,
i.e. unbounded growth keyed on user input.

### G10.3 (MEDIUM) — `RevenueReport` weekend grouping sorts wrong and is culture-dependent · `M14`

**FIXED (Wave 2).** Weekend grouping is now keyed by the `DateOnly` Saturday and ordered chronologically; labels are formatted with `InvariantCulture` at the boundary. Covered by multi-month and pl-PL culture tests.

`Features/Reports/RevenueReport/RevenueReportHandler.cs:88-93, 84`

```csharp
private static string GetWeekendGroupKey(DateOnly date)
{
    var saturday = date.DayOfWeek == DayOfWeek.Sunday ? date.AddDays(-1) : date;
    return $"{saturday:yyyy dd MMM}";
}
…
.OrderBy(r => r.GroupKey)
```

Two defects in three lines. **`MMM` is the abbreviated month name in the current culture** — labels
change with server locale; on a Polish-locale host they render as `sty`/`lut`. No
`CultureInfo.InvariantCulture`. And **`OrderBy` on that string is alphabetical**: `"2026 03 Apr"`
sorts before `"2026 10 Jan"`. Every multi-month revenue report is in the wrong order.

**Fix:** group on the `DateOnly` Saturday, order by it, format only at the response boundary.

### G10.4 (MEDIUM) — N+1 queries on list endpoints

`ListUsersHandler.cs:29-34` calls `GetRolesAsync` per user in the page (bounded by the page cap, so
acceptable but noted). `UserAccessService.GetEffectivePermissionsAsync` does `FindByNameAsync` +
`GetClaimsAsync` per role on every login and refresh.

### G10.5 (MEDIUM) — `JiApp.ImageTools` is a deployed service with no functionality · `M24`

**FIXED (Wave 7).** `JiApp.ImageTools` service DELETED + every reference swept (src, tests, sln, compose base+prod, Gateway YARP/rate-limit/health-dashboard, start/stop-dev scripts, firewall script, aws/ scripts + cloudformation, CI deploy workflow, README/URLS/todo-deploy). Deploy-atomic, single PR. W7-F PR #136.

`backend/src/JiApp.ImageTools/Startup.cs` — the whole service:

```csharp
tools.MapGet("/health", () => Results.Ok(new { status = "healthy", … }));
tools.MapGet("/ping",   () => Results.Ok(new { module = "image-tools", status = "ok" }));
```

It consumes a container in `docker-compose.yml`, a Gateway route, a `RateLimiting:*` entry, a
`PathPolicyMap` entry (`RateLimitPolicySelector.cs:56`, mapped to `null` = **no rate limiting**), a
test project, and a health-dashboard slot. It has no auth of its own.

Flagged as dead infrastructure in the 2026-07-02 audit and still present. **Decide: give it a purpose
with a dated note, or delete it and its route entries.** (Adding auth to two health probes is not the fix.)

### G10.6 (MEDIUM) — The MP3 download should be a durable async command

**FIXED (Wave 6, PR-C #124).** Durable async `DownloadCommand` rows (DB-as-queue): idempotent `CreateJob` (active-only dedupe on `UserId`+`VideoId` via a unique filtered index), atomic SQL claim, retry ladder (30s/2m) with exhausted-`Failed` dead-letter rows, crash recovery (`Processing`→`Queued` at worker start), poll + channel wake loop, TTL cleanup, and a SQLite WAL + busy-timeout interceptor. `DownloadSemaphore` becomes the worker's concurrency cap (3). Migration `AddDownloadCommand` (`20260805120000`) — **the only Wave-6 deploy gate (already merged)**.

`GetDownloadLinkHandler.cs:25-92` runs yt-dlp inline in the request: slow (seconds to minutes),
fragile (cookies, proxy, YouTube availability), stateful (writes to disk). No command document, no
idempotency, no status lifecycle, no retry, no dead-letter. A restart mid-download loses the work
silently; a hung yt-dlp permanently burns one of three semaphore slots; failed downloads leave no
record for the user to retry.

**Fix:** persist a `DownloadCommand` (`Queued → Processing → Completed/Failed`), return immediately,
process in the background. `DownloadSemaphore` becomes the processor's concurrency limit.

**Explicitly NOT the assistant chat.** SSE streaming is the correct pattern there — the stream *is*
the response, not a side effect. Its gap is retry resilience (G5.4), not a flow redesign.

---

<a name="g11"></a>
# G11 — Correctness bugs & hygiene

Small, unambiguous, individually cheap. Good first-PR material.

| # | Finding | Location |
|---|---|---|
| **G11.1 (M)** | **FIXED (Wave 6, PR-E1 #126)** — register anti-enumeration: the handler now returns a single generic `"Registration failed"` for every `IdentityResult` failure (duplicate username/email, weak password), with the real error text logged only — both duplicate-username paths (UserValidator + DB constraint) now agree. **`RegisterHandler` leaks Identity error text, defeating its own anti-enumeration design.** The handler carries a comment explaining duplicate pre-checks were omitted *"because they leak user enumeration info"*, then returns `createResult.Errors` verbatim — and `DuplicateUserName`'s description is literally `"Username 'alice' is already taken."` The `DbUpdateException` path at line 44 correctly returns a generic message, so the two paths for the same condition disagree. `M5` | `Register/RegisterHandler.cs:23-52` |
| G11.2 | **FIXED (Wave 6, PR-E2 #129)** — `indent_style`/`indent_size` added to `backend/.editorconfig`; the tab-indented `.cs` files converted to spaces (`dotnet format whitespace`). 51 `.cs` files indent with tabs, the rest spaces. `backend/.editorconfig` has only analyzer severities — **no `indent_style`** — so nothing enforces it. The tab files cluster in the RBAC/admin work. `L1` | `Permissions.cs`, `RoleNames.cs`, all `Features/Admin/**`, `Authorization/**`, `Resilience/**` |
| G11.3 | **FIXED (Wave 6, PR-E2 #129)** — the two flush-left members in `ValidationConstants.cs` re-indented. Two members at column 0 inside the class body. `L2` | `JiApp.Common/Constants/ValidationConstants.cs:9-10` |
| G11.4 | **FIXED (Wave 6, PR-E1 #126) — accepted deviation (see §F).** The distinct *"Too many attempts. Try again later."* lockout message is kept deliberately (UX); the handler now returns `ResultCategories.AccountLocked` so the endpoint can distinguish it — the account-enumeration oracle it creates is a documented, user-accepted deviation. Handler returns distinct messages for lockout vs bad password; the endpoint overwrites every failure with `"Invalid credentials"`. The differentiation is dead code and the lockout hint never reaches the user. The handler also returns failures with **no `ResultCategories`**, so the endpoint cannot distinguish them except by string. `L4` | `Login/LoginHandler.cs:35,43,49` + `LoginEndpoint.cs:30-32` |
| G11.5 | **FIXED (Wave 6, PR-H #127)** — `updateProfile` no longer fabricates `roles: []`/`permissions: []`; the return type is narrowed to `UpdateProfileResult` (id/displayName/email), so the caller can't silently wipe in-memory roles/permissions. `authService.updateProfile` returns `roles: []`, `permissions: []` — hardcoded lies in a DTO. Harmless only because `AuthContext` discards the result. `L5` | `mobile/src/services/authService.ts:103-104` |
| G11.6 | **FIXED (Wave 6, PR-H #127)** — the request interceptor now respects an explicit `Authorization` header (e.g. `checkToken`'s) instead of overwriting it from storage. `checkToken(token)` passes an explicit `Authorization` header, but the request interceptor unconditionally overwrites it from storage — the parameter is silently ignored. `L6` | `apiClient.ts:28-31` vs `authService.ts:64-66` |
| G11.7 | **FIXED (Wave 6, PR-E1 #126)** — `DownloadVideoAsync` now runs `ValidateVideoId` like `BuildPreviewAudioProcess`. `ValidateVideoId` exists and is called by `BuildPreviewAudioProcess` but **not** by `DownloadVideoAsync`. Not exploitable (the FluentValidation regex covers it) but inconsistently applied. `L7` | `JiApp.YtApi/YoutubeClient.cs:98,154,162` |
| G11.8 | **FIXED (Wave 6, PR-E1 #126)** — `DownloadVideoAsync` now takes the job `tempId`; the worker resolves its own output instead of guessing — no cross-resolution between concurrent downloads by the same user. `DownloadVideoAsync`'s fallback picks the newest `*.mp3` in the user's folder when yt-dlp reports no path — two concurrent downloads by the same user can cross-resolve. `L8` | `YoutubeClient.cs:141-143` |
| G11.9 | **FIXED (Wave 6, PR-E1 #126)** — `AddAsync`/`SaveChangesAsync` now thread the `CancellationToken` (worker `RecordHistoryAsync`). `downloadHistoryRepository.AddAsync/SaveChangesAsync` called without `CancellationToken`, against the edge-to-edge threading from #81. `L9` | `GetDownloadLinkHandler.cs:88-89` |
| G11.10 | **FIXED (Wave 6, PR-E1 #126)** — `validator.ValidateAsync(request, ct)` in `GetDownloadLinkEndpoint` and `SearchVideosEndpoint`. `validator.ValidateAsync(request)` called with no `ct`, unlike every other endpoint. Same in `SearchVideosEndpoint.cs:20`. `L10` | `GetDownloadLinkEndpoint.cs:21` |
| G11.11 | **FIXED (Wave 6, PR-C #124)** — the inline yt-dlp flow (and its bare-`Exception` catch) was removed by the G10.6 durable-`DownloadCommand` rewrite: the request handler now only enqueues, and the worker handles timeouts/cancellation explicitly. Catches bare `Exception` around the download, converting client disconnects into `"Failed to process download."` `L11` | `GetDownloadLinkHandler.cs:39` |
| G11.12 | **FIXED (Wave 6, PR-E1 #126)** — `IUserAccessService` now takes a `CancellationToken` and `RoleSeeder.SeedAsync(ct)` actually uses it. `IUserAccessService` takes no `CancellationToken`; `IRoleSeeder.SeedAsync(ct)` takes one and never uses it. `L12` | `UserAccessService.cs:9-10`, `RoleSeeder.cs:27` |
| G11.13 | **FIXED (Wave 6, PR-E1 #126)** — new non-generic `Result` + `WithValue<T>`; `Appointment.TryTransitionTo` now returns `Result`, and `AdminAccessGuard` migrated off `Result<bool>` onto it. `Appointment.TryTransitionTo(status, out string? error)` uses the `out`-bool idiom where `dotnet-domain-modeling` prescribes returning `Result`. Blocked on there being **no non-generic `Result`** — `AdminAccessGuard` works around the same gap with `Result<bool>`. `L13` | `Scheduler/Domain/Appointment.cs:29` |
| G11.14 | **FIXED (Wave 4)** — G8.2 deleted the four `JwtSettings` copies (one shared `JiApp.Common` type remains per file). `L15` | `Configuration/*Settings.cs` |
| G11.15 | **FIXED (Wave 6, PR-E2 #129)** — root orphans moved: `Permissions.cs`/`RoleNames.cs` → `JiApp.Common/Constants/` (namespace `JiApp.Common.Constants`, 28 usings updated); flat `JiApp.YtApi` restructured into `Clients/` + `Contracts/` concern folders. Root-level orphan files: `Permissions.cs` and `RoleNames.cs` sit at the `JiApp.Common` root while a `Constants/` folder exists; `JiApp.YtApi` is entirely flat (3 files, no folders). `L17` | `JiApp.Common/`, `JiApp.YtApi/` |
| G11.16 | **FIXED (Wave 6, PR-E1 #126)** — `RateLimitPolicySelector` now returns 500 (server fault, logged) instead of 403 for "no rate-limit policy configured" — a server misconfiguration no longer masquerades as a client authorization failure. `RateLimitPolicySelector` returns **403 Forbidden** for "no rate-limit policy configured" — a server misconfiguration reported as a client authorization failure. `L18` | `RateLimitPolicySelector.cs:122-128` |
| G11.17 | **FIXED (Wave 6, PR-E1 #126)** — `ListUsersEndpoint` guards `result.IsSuccess` before touching `.Value`; pagination clamping moved into the handler. `ListUsersEndpoint` calls `result.Value` **without checking `result.IsSuccess`**. Latent only because `ListUsersHandler` always succeeds today. Also does its pagination clamping in the endpoint lambda rather than the handler. | `Admin/Users/ListUsers/ListUsersEndpoint.cs:20-24` |
| G11.18 | **FIXED (Wave 6, PR-E1 #126)** — `StreamPreviewHandler` returns `Result<T>`; the invented `"YoutubeDl"` category became `ResultCategories.YoutubeDl`. `StreamPreviewHandler` returns a custom `StreamPreviewResult` discriminated union instead of `Result<T>`, bypassing the shared error contract. And `GetDownloadLinkHandler` invents a `"YoutubeDl"` error category outside `ResultCategories`. | `StreamPreview/StreamPreviewHandler.cs:49-56`, `GetDownloadLinkHandler.cs:20` |
| G11.19 | **FIXED (Wave 6, PR-E2 #129)** — central `TableNames` constants replace string-literal `ToTable()` calls (YtDownloader + Identity Persistence); values byte-identical — no migration change. `ToTable()` uses string literals with no central `TableNames` constants; Scheduler and LovingBoards rely on convention instead — inconsistent either way. | `YtDownloader/Persistence/Configurations/*.cs:11`, `Identity/…/RefreshTokenConfiguration.cs:12` |
| G11.20 | **FIXED (Wave 6, PR-E2 #129)** — owned enums moved beside their aggregate (`AppointmentStatus`/`ExpenseCategory`/`ServiceCategory` in Scheduler, `BoardItemStatus` in LovingBoards); style only, no explicit int values (§D). Owned enums live in their own files rather than beside their aggregate (`AppointmentStatus`, `ExpenseCategory`, `ServiceCategory`, `BoardItemStatus`). Style only — see §D for why the "explicit values" version of this claim was rejected. | `Scheduler/Domain/`, `LovingBoards/Domain/` |

---

<a name="g12"></a>
# G12 — CI & tooling gates

**Why grouped:** this group is why every other group can regress silently. Cheapest by effort,
highest by leverage on everything above.

`.github/workflows/ci.yml`

| # | Gap | Detail |
|---|---|---|
| G12.1 | **FIXED (Wave 3)** — lint gate added | `npx eslint . --max-warnings=0` now runs in the mobile CI job (PR #101); 94 findings fixed to zero. `react-hooks/rules-of-hooks` + `react-hooks/exhaustive-deps` — the mechanical guard for non-negotiable #3 (G6.3) — now run on every push. `react-doctor.yml` stays advisory. |
| G12.2 | **Warnings not errors** | `dotnet build backend/JiApp.sln` — no `-warnaserror`, no `--configuration Release`. Warnings and NuGet advisories pass silently. |
| G12.3 | **No format gate** | No `dotnet format --verify-no-changes`, which is why G11.2/G11.3 survive. |
| G12.4 | **No coverage** | Neither stack collects or gates coverage, so G9.1's blind spots are invisible to CI. |
| G12.5 | **Stale trigger** | `on: push: branches: [main, micros]` — `micros` looks like a dead branch. |

**Missing architecture fitness tests.** `Testing.Common/Conventions/` has `DependencyInjectionConvention`
and `EndpointAuthorizationConvention` with collect-all-violations reporting — the right shape.
Candidates to add, each of which would have caught a finding above:

- Every `TokenValidationParameters` sets `ClockSkew` → G1.3
- Every `Settings.Validate()` covers every property read at startup → G1.4
- Every mutating endpoint carries the security-stamp filter (or an explicit opt-out attribute) → G2.5
- Every `BackgroundService.ExecuteAsync` body is wrapped in try/catch → G2.6
- No handler references `DateTime.UtcNow` → G8.4
- Every endpoint maps `Result<T>` through `ToHttp()` → G8.1

Already present and working: the CI-vs-`PathPolicyMap` rate-limit-policy drift check in
`RateLimitPolicySelectorTests`.

---

<a name="b"></a>
# B. Standards conformance matrix

| Standard | Verdict | Notes |
|---|---|---|
| `dotnet-vsa-slice` | **Strong** | Endpoint/Handler/Request/Validator/Response per slice, primary-constructor handlers, interface-segregated DbContexts, FluentValidation per slice. Fat endpoints: G5.3. |
| `backend-service-anatomy` | **Good** | Thin transport, `Result<T>` + categories, fail-fast settings. Gaps: background-job anatomy (G2.6), streaming not covered by the error contract (G5.1). |
| `dotnet-security-baseline` | **Gaps — floor breaches** | JWT validation is correct in shape everywhere (`ValidAlgorithms = ["HS256"]`, issuer+audience+lifetime). But: G1.1, G1.2, G2.5, G3.1–G3.4, G4.1. *The baseline is a floor — these are not preferences.* |
| `dotnet-composition-root` | **Good** | Thin `Program.cs` → `Startup` ✓, settings bound + validated ✓, `MapXxx` slice extensions ✓, `ConfigureXxx` grouping ✓ (G8.5). |
| `dotnet-project-topology` | **Partial** | Feature folders and namespace-mirroring clean. Violations: G9.5, G11.15. |
| `dotnet-domain-modeling` | **Good** | `BaseEntity<TKey>`, bounded string columns in every configuration, single-aggregate enums, `ValueComparer` correctly present on the `List<long>` JSON column (`BoardConfiguration.cs:11-14`). Deviations: G11.13. |
| `csharp-fixture-testing` | **Excellent** | `LoginHandlerTests` is a model implementation — private `Fixture`, `Sut`, `With_xxx` builders, semantic doubles. Shared `ResultAssertions` gives `AssertNotFound`/`AssertAccessDenied`/`AssertConflict`. |
| `semantic-test-doubles` | **Backend strong, mobile weak** | Backend: `MockUserManager.GetSuccessful()`, `CapturingBoardBroadcaster`, `MockObject<T>`, one mocking library, real SQLite. Mobile: G9.3. |
| `unit-test-anatomy` | **Good** | AAA, one behaviour per test, `Behavior_Scenario_Expected` naming on the backend. Gaps: G9.7, G9.8. |
| `persistence-testing` | **Mixed** | Real ephemeral SQLite per test with proper disposal where used — but Identity handlers never touch it (G9.2). |
| `integration-testing` | **Uneven** | Gateway ✅, YtDownloader ✅, Identity/Scheduler/LovingBoards ❌ (G9.6). |
| `architecture-fitness-tests` | **Good foundation** | `DependencyInjectionConvention` + `EndpointAuthorizationConvention`, collect-all-violations, wired into 5 services. Under-exploited — see G12. |
| `integration-adapter-isolation` | **Good** | Owned `IYoutubeClient` / `IAssistantChatClientProvider` interfaces exist; YouTube provider exceptions wrapped in the owned `YoutubeApiException` (G8.8). Breaches: G5.3, G5.4. |
| `cross-service-data-flow` | **Good** | Each service owns its schema; no service reads another's tables. The sync auth call is the one request-critical hop and it has a defined failure mode (`StampValidationResult.Unavailable` → 503). Gap: G10.2. |
| `async-command-processing` | **One gap** | G10.6. Correctly *not* applicable to the assistant chat. |
| `react-native-encapsulation` | **Strong** | Presentational/hook/context/service layering clean; **zero `any` in production code** (the only `any` is in `test/createMockFn.ts`). Gaps: G7.2, G7.4. |
| `zod-boundary-validation` | **Uneven** | Core, admin, LovingBoards services and SSE events all validate. Scheduler validates nothing (G7.1); storage reads unvalidated (G7.3). |
| `storybook-component-testing` | **Built, unadopted** | G9.4. |
| `solid-principles` | **Good** | Dependency direction holds; domain never depends outward. SRP breach: `boardStreamService` owns streaming *and* token refresh (G2.1 / G5.2). |
| **Project non-negotiable #1 (i18n)** | **✅ FIXED** | G6.1 — Scheduler fully extracted to the `scheduler` namespace (PR #99) |
| **Project non-negotiable #2 (Image onError)** | **✅ FIXED** | G6.2 — all network `<Image>` have `onError` + placeholder (PR #99) |
| **Project non-negotiable #3 (hooks first)** | **✅ CLEAN + machine-guarded** | G6.3 — verified by hand; now enforced by the eslint gate (G12.1, PR #101) |

---

<a name="c"></a>
# C. What is genuinely good

Stated plainly, because it constrains how the fixes should be made.

- **VSA discipline is real and uniform.** 90+ slices across four services follow the same five-file
  shape. A new developer can predict where any file lives.
- **`Result<T>` + `ResultCategories` is consistently used**, and duplicating the mapping per endpoint
  rather than hiding it in a base class is the right call for VSA — G8.1 asks for a shared *extension*,
  not a base class.
- **The Identity test suite is a reference implementation** of `csharp-fixture-testing`.
  `LoginHandlerTests`'s `Fixture` with `WithExistingUser()` / `WithNonexistentUser()` /
  `WithWrongPassword()` builders is exactly what the standard describes. Copy this into mobile (G9.3).
- **Architecture fitness tests exist and work** — the right shape, with collect-all-violations reporting.
- **Refresh-token handling is well-designed** — SHA-256 hashed at rest, 64 bytes of CSPRNG entropy,
  single-use rotation, reuse detection, atomic revoke+create in a transaction, cascade FK, background
  cleanup. G2.2 is a bug *within* a sophisticated design.
- **The security-stamp mechanism is well-built** — remote validator with bounded retry, fail-closed
  `NoOpSecurityStampValidator` that refuses to exist outside Development, three-state result mapped
  to 401/503. G2.5 is a wiring gap, not a design gap.
- **`AssistantTextSanitizer`** is careful, correct work: stateful cross-chunk marker detection with
  proper partial-suffix buffering. Non-obvious, and it has real tests.
- **Zero `any` in mobile production code** across 294 files.
- **Comments explain *why*, not *what*** — `RefreshTokenService.CreateAsync:45-47`,
  `RegisterHandler:23-27`, `BoardWriteLock`'s single-instance caveat, `AssistantStreamGate`'s RAM
  rationale. Above-average engineering hygiene.

---

<a name="d"></a>
# D. Refuted claims — do not action

From the pass-2 audits; each was checked and did not survive. Recorded so nobody re-litigates them.

| Claim | Verdict |
|---|---|
| "Enums lack explicit integer values → adding a member reorders and corrupts persisted data" (5 enums) | **REFUTED.** All four persisted enums use `HasConversion<string>()` (`AppointmentConfiguration.cs:14`, `BoardItemConfiguration.cs:16`, `ServiceConfiguration.cs:13`, `ExpenseConfiguration.cs:12`). String storage is immune to reordering; the stated failure cannot occur. Explicit values remain a style preference with no correctness argument — see G11.20. |
| "Certificate password `JiAppDev2026!` is committed in `JiApp.Identity/appsettings.Development.json`" | **REFUTED.** Not in `git ls-files` — only `appsettings.Development.example.json` and `appsettings.json` are tracked. The password exists solely in an untracked local dev file. (The *Gateway JWT key* claim did check out — G1.2.) |
| "Gateway is missing `Program.Extensions.cs`, so `WebApplicationFactory<Program>` won't compile" | **REFUTED.** `GatewayWebApplicationFactory.cs:14` reads `: WebApplicationFactory<JiApp.Gateway.Program>` and its 51 tests pass. |
| "Gateway is missing `services.AddSingleton(settings)`" | **TRUE BUT HARMLESS.** `Startup` captures `settings` via its primary constructor; nothing resolves it from DI. Not a defect. |
| "Hardcoded defaults in Settings POCOs (`MaxBoardsPerUser = 50`, `DailyMessageLimitPerUser = 30`, …) violate fail-fast" | **REJECTED for this project.** These are operational tuning values with sensible defaults, and centralising them in backend settings is the project's explicit "backend owns config" rule. `LovingBoardsSettings` is the model the other services should copy (G4.6), not a violation. Range validation (G1.4) is the real gap. |
| "`ImageTools` has no authentication" | **TRUE, NOT ACTIONABLE AS STATED.** Correct, but G10.5 supersedes: the service has no functionality to protect. Delete it rather than add auth to two health probes. |
| "Login should return `Created` rather than `Ok`" | **REJECTED.** Style opinion, no defect. |

---

<a name="e"></a>
# E. Corrections to pass 1

Kept visible so the record is honest about what the second pass changed.

| # | Original claim | Correction |
|---|---|---|
| C1 | "45 hardcoded user-facing strings" | **~76.** The original grep matched only `<Text>` children; it missed 31 in `Alert.alert()` titles/messages, `placeholder=`, and `accessibilityLabel=`. Folded into G6.1. |
| C2 | "every single one is in the Scheduler module" | **False.** `components/SearchBar.tsx:136` has `accessibilityLabel="Clear search"` in a core shared component. The directional claim (Scheduler ≈95%, only module with zero i18n adoption) still holds. Folded into G6.1. |
| C3 | Non-negotiable #3 marked "Unverified" | **Verified clean.** All 43 screens and components checked by hand; zero violations. Now G6.3. |

---

<a name="f"></a>
# F. Suggested execution order

Ordered by (risk reduced) ÷ (diff size), not by group number.

**Wave 1 — small diffs, highest severity.** One PR, ~half a day.
`G1.1` (guard both TLS callbacks) · `G1.2` (blank the Gateway key) · `G2.2` (transaction rollback) ·
`G5.1` (`HasStarted` + 401) · `G2.6` (try/catch + logger) · `G2.4` (revoke on password change).
Every one is a handful of lines and each closes a fail-open or a crash path.

**Wave 2 — live user-facing bugs.**
`G2.1` (single shared `refreshAuth()` — fixes the mass-logout) · `G3.1` (board-scope the FK lookups
+ regression test) · `G2.3` (call logout; auth-invalidated event) · `G4.1` + `G4.2` (partition the
limiters; `ForwardedHeaders`) · `G3.2` · `G3.3` · `G10.3` (report sort) · `G4.3`–`G4.6`.

**Wave 3 — project-doc compliance.** Non-negotiable by the project's own rules.
`G6.1` (~76 strings) · `G6.2` (three `onError`) · `G7.1` (Scheduler Zod) · `G12.1` (add eslint, so
`G6.3` stops being unverifiable).

**Wave 4 — the leverage group — COMPLETE.** `G8.1`–`G8.8` all landed via PRs #103–#112, closing the
roll-ups `G1.3` + `G1.6` + `G11.14` in the same pass. `G8.2` (shared JWT params — also closes G1.3 +
G1.6 + G11.14) · `G8.1` (`ToHttp()` — also closes the fallback-arm mess and half of G3.2) · `G8.3` ·
`G8.7` · `G8.4` · `G8.5` · `G8.6` · `G8.8`.

**Wave 5 — the safety net.** `G9.3` before `G9.1` — the mocks must be rebuilt before the missing
specs can be written cleanly. Then `G9.4` (adopt `composeStories`, starting with the 7 Scheduler
component stories that already exist), `G9.2`, `G9.5`, `G9.6`. Add the G12 fitness tests as each
corresponding fix lands, so it cannot regress. — **COMPLETE**

**Wave 6 — structural — COMPLETE.** `G10.1` (or the `BoardMembers` join table, which also kills G3.3 and the
JSON `ValueComparer` need) · `G10.2` · `G10.6` · `G7.2` · `G7.4` · `G11` sweep. 9 PRs #121–#129;
header **33 → 9 of 84 open (Wave 6) → 8 of 84 open (Wave 7)**.

**Wave 6 decisions:**
- `G11.4` — **accepted deviation (user decision).** The distinct *"Too many attempts. Try again later."* lockout message is kept deliberately for UX; the handler returns `ResultCategories.AccountLocked` so the endpoint can distinguish it, and the account-enumeration oracle it creates is a documented, user-accepted trade-off.
- `G10.2` — **horizontal scaling is NOT intended.** `SingleInstanceGuard` pins replicas to 1 (file-based exclusive lease on the shared `jiapp_data` volume; a second replica logs **Critical** + `exit(1)` before serving traffic) and the endpoint cache is capped — the four in-memory states need no redesign.

**Wave 7 — closeout — COMPLETE.** Execution order: 1. `W7-B` (G2.5, G2.7) → 2. `W7-C` (G3.4, G3.5 — before A, settings coupling) → 3. `W7-A` (G1.4, G1.5) → 4. `W7-D` (G5.2 — independent) → 5. `W7-E` (G5.3, G5.4) → 6. `W7-F` (G10.5 — deploy-atomic, last) → 7. closeout. PRs #131–#136; header **9 → 8 of 84 open**.

**Decide, don't default:**
- `G2.5` — write down the revocation policy, then make the wiring match it. **Resolved (Wave 7): the recheck filter is extended to `AddBoardMember` in both services — revocation is now enforced on every mutating endpoint (W7-B #131).**
- `G10.5` — keep `ImageTools` with a stated purpose, or delete it. **Resolved (Wave 7): deleted — the dead service and all its references were removed in a single deploy-atomic PR (W7-F #136).**
- `G9.8` — mobile test-naming grammar: adopt `Behavior_Scenario_Expected` or record the exemption.
- `G10.2` — is horizontal scaling ever intended? If yes, four things need redesign. If no, pin
  replicas to 1 and fail fast on a second instance. **Resolved (Wave 6): horizontal scaling is NOT
  intended — `SingleInstanceGuard` pins replicas to 1 and fails fast on a second instance; the
  endpoint cache is capped.**
