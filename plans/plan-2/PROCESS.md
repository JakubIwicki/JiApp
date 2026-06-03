# JiApp Microservices Migration — Phase-by-Phase Execution Guide

This document contains the detailed, task-level execution plan for migrating JiApp from a monolith to a microservices architecture. Each phase delivers a working vertical slice (backend + frontend + infrastructure). Tasks are ordered by dependency within each phase.

**Granularity:** File paths, method signatures, validation rules, acceptance criteria. No inline code blocks.

**Reference code:** The existing monolith at `backend/src/JiApp.Api/` serves as the reference for porting logic. Key files:
- `backend/src/JiApp.Api/Startup.cs` — DI registrations decomposing across services
- `backend/src/JiApp.Api/Features/` — Endpoints migrating to JiApp.Identity and JiApp.YtDownloader
- `backend/src/JiApp.Infrastructure/Persistence/JiAppDbContext.cs` — Database schema splitting
- `backend/src/JiApp.Infrastructure/Services/JwtTokenService.cs` — JWT generation (moves to Identity)
- `backend/src/JiApp.YtApi/YoutubeClient.cs` — YouTube client (moves to YtDownloader)
- `mobile/src/context/AuthContext.tsx` — Auth state to update with refresh token flow
- `mobile/src/services/apiClient.ts` — HTTP client to update with refresh interceptor
- `mobile/src/navigation/MainNavigator.tsx` — Navigation to replace with ModuleLoader

---

## Phase 0: Foundation

**Goal:** Docker Compose orchestration, Identity Server skeleton, Gateway skeleton, mobile shell directory structure. Nothing deploys to production — this is scaffolding.

### Prerequisites
- .NET 10 SDK installed (WSL, already in use)
- Docker Desktop (or Docker Engine + Docker Compose) installed
- Node.js 18+ and npm installed
- React Native CLI environment set up (JDK 17, Android SDK)
- Existing monolith builds and tests pass (`dotnet build backend/JiApp.sln` succeeds, `npm test` in mobile/ succeeds)

### Backend Tasks

**B0.1: Create new backend projects**

- Create web project: `backend/src/JiApp.Identity/JiApp.Identity.csproj` (TargetFramework net10.0, Sdk: Microsoft.NET.Sdk.Web)
- Create web project: `backend/src/JiApp.YtDownloader/JiApp.YtDownloader.csproj` (TargetFramework net10.0, Sdk: Microsoft.NET.Sdk.Web)
- Create web project: `backend/src/JiApp.Gateway/JiApp.Gateway.csproj` (TargetFramework net10.0, Sdk: Microsoft.NET.Sdk.Web)
- Create test project: `backend/tests/JiApp.Identity.Tests/JiApp.Identity.Tests.csproj` (TargetFramework net10.0)
- Create test project: `backend/tests/JiApp.YtDownloader.Tests/JiApp.YtDownloader.Tests.csproj` (TargetFramework net10.0)
- Add all 5 projects to `backend/JiApp.sln`
- Set project references:
  - JiApp.Identity references JiApp.Common
  - JiApp.YtDownloader references JiApp.Common, JiApp.YtApi
  - JiApp.Gateway references JiApp.Common
  - JiApp.Identity.Tests references JiApp.Identity
  - JiApp.YtDownloader.Tests references JiApp.YtDownloader
- Acceptance: `dotnet build backend/JiApp.sln` succeeds with zero errors

**B0.2: Install NuGet packages for new projects**

- JiApp.Identity packages:
  - Microsoft.AspNetCore.Identity.EntityFrameworkCore (10.x)
  - Microsoft.AspNetCore.Authentication.JwtBearer (10.x)
  - Microsoft.EntityFrameworkCore (10.x)
  - Microsoft.EntityFrameworkCore.Sqlite (10.x)
  - Microsoft.EntityFrameworkCore.Design (10.x, PrivateAssets=all)
  - Npgsql.EntityFrameworkCore.PostgreSQL (latest stable)
  - FluentValidation.DependencyInjectionExtensions (latest)
  - Serilog.AspNetCore (latest)
- JiApp.YtDownloader packages:
  - Microsoft.AspNetCore.Authentication.JwtBearer (10.x)
  - Microsoft.EntityFrameworkCore (10.x)
  - Microsoft.EntityFrameworkCore.Sqlite (10.x)
  - Microsoft.EntityFrameworkCore.Design (10.x, PrivateAssets=all)
  - Npgsql.EntityFrameworkCore.PostgreSQL (latest stable)
  - FluentValidation.DependencyInjectionExtensions (latest)
  - Serilog.AspNetCore (latest)
  - Google.Apis.YouTube.v3 (latest)
  - YoutubeDLSharp (latest)
  - FFMpegCore (latest)
- JiApp.Gateway packages:
  - Yarp.ReverseProxy (latest stable)
  - Microsoft.AspNetCore.Authentication.JwtBearer (10.x)
  - Serilog.AspNetCore (latest)
- Test projects: xunit, xunit.runner.visualstudio, Microsoft.NET.Test.Sdk, Moq, FluentAssertions, Microsoft.AspNetCore.Mvc.Testing
- Acceptance: `dotnet restore` and `dotnet build` both succeed

**B0.3: Create RefreshToken model and IdentityDbContext**

- File: `backend/src/JiApp.Identity/Models/RefreshToken.cs`
  - `public sealed class RefreshToken`
  - Properties: `long Id`, `string Token` (hashed, MaxLength 128), `long UserId` (FK to AspNetUsers), `DateTime ExpiresAt`, `DateTime CreatedAt`, `bool IsRevoked`
- File: `backend/src/JiApp.Identity/Persistence/IdentityDbContext.cs`
  - `public sealed class IdentityDbContext : IdentityDbContext<User, IdentityRole<long>, long>`
  - Constructor: `IdentityDbContext(DbContextOptions<IdentityDbContext> options)`
  - DbSet: `RefreshTokens`
  - Override `OnModelCreating`: call base, configure `RefreshToken` entity with index on `Token`
- File: `backend/src/JiApp.Identity/Persistence/Configurations/RefreshTokenConfiguration.cs`
  - Implements `IEntityTypeConfiguration<RefreshToken>`
  - HasOne<User>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade)
  - HasIndex(e => e.Token).IsUnique()
- Acceptance: DbContext compiles; configuration applied correctly

**B0.4: Create Identity service Program.cs and Startup.cs**

- File: `backend/src/JiApp.Identity/Program.cs`
  - Create WebApplication builder, bind Settings, validate, create Startup, call ConfigureServices then Configure
  - Use Serilog from configuration
- File: `backend/src/JiApp.Identity/Startup.cs`
  - `ConfigureServices`: AddEndpointsApiExplorer, AddDbContext with SQLite (dev) / PostgreSQL (prod) provider swap, AddIdentity<User, IdentityRole<long>> with same password rules as monolith, AddAuthentication(JwtBearer) with same validation params, AddAuthorization, AddScoped handler/validator registrations, AddSingleton<IJwtTokenService, JwtTokenService>, AddScoped<IRefreshTokenService, RefreshTokenService>, AddScoped<ICurrentUserService, CurrentUserService>, AddHttpContextAccessor, AddCors (AllowAnyMethod/Header/Origin with AllowCredentials)
  - `Configure`: UseMiddleware<GlobalExceptionMiddleware>, UseSerilogRequestLogging, UseRouting, UseCors, UseAuthentication, UseAuthorization, MapGroup("/api/v1") endpoints
- File: `backend/src/JiApp.Identity/appsettings.json`
  - ConnectionStrings: IdentityDb (SQLite path default for dev, PostgreSQL for prod)
  - Jwt: Key, Issuer ("JiApp-Identity"), Audience ("jiapp-gateway"), AccessTokenExpireMinutes (15), RefreshTokenExpireDays (7)
  - Serilog: Console + File sinks
- File: `backend/src/JiApp.Identity/appsettings.Development.json`
  - ConnectionStrings:IdentityDb = "Data Source=../../.data/identity_dev.db"
  - Jwt:Key = development base64 key
  - Kestrel endpoints: Http at `http://*:5001`
- File: `backend/src/JiApp.Identity/Properties/launchSettings.json`
  - Profile "http": applicationUrl `http://localhost:5001`, ASPNETCORE_ENVIRONMENT Development
- Acceptance: `dotnet run --project backend/src/JiApp.Identity` starts without error; health check returns 200

**B0.5: Create JWT token service for Identity**

- Copy `backend/src/JiApp.Infrastructure/Services/JwtTokenService.cs` to `backend/src/JiApp.Identity/Services/JwtTokenService.cs`
- Copy `backend/src/JiApp.Infrastructure/Services/IJwtTokenService.cs` to `backend/src/JiApp.Identity/Services/IJwtTokenService.cs`
- Update namespace to `JiApp.Identity.Services`
- The interface stays: `GenerateToken(long userId, string username)`, `IsTokenValid(string token)`, `GetUsernameFromToken(string token)`, `GetUserIdFromToken(string token)`
- Configure via `JwtSettings` from config (not IConfiguration directly)
- Acceptance: Unit test generates token, validates it, extracts claims

**B0.6: Create RefreshTokenService**

- File: `backend/src/JiApp.Identity/Services/IRefreshTokenService.cs`
  - Method: `Task<RefreshToken> CreateAsync(long userId)` — generates opaque 64-byte random string, hashes with SHA256, stores in DB, returns entity with raw token
  - Method: `Task<RefreshToken?> ValidateAsync(string rawToken)` — hashes input, looks up in DB, checks not expired, not revoked, returns entity or null
  - Method: `Task RevokeAsync(long refreshTokenId)` — sets IsRevoked = true
  - Method: `Task RevokeAllForUserAsync(long userId)` — revokes all user's tokens (logout-all)
- File: `backend/src/JiApp.Identity/Services/RefreshTokenService.cs`
  - Constructor receives `IdentityDbContext`
  - CreateAsync: `using var rng = RandomNumberGenerator.Create(); var bytes = new byte[64]; rng.GetBytes(bytes); var raw = Convert.ToBase64String(bytes); var hashed = SHA256.HashData(Encoding.UTF8.GetBytes(raw));` store entity with hashed token, return entity with raw Token set
  - ValidateAsync: hash input, query DB `FirstOrDefaultAsync(t => t.Token == hashed && t.ExpiresAt > DateTime.UtcNow && !t.IsRevoked)`
- Acceptance: Unit test: create token, validate returns entity, validate with wrong token returns null, revoke then validate returns null

**B0.7: Create initial Identity database migration**

- Run: `dotnet ef migrations add InitIdentity --project backend/src/JiApp.Identity --startup-project backend/src/JiApp.Identity --output-dir Persistence/Migrations`
- Verify migration creates: AspNetUsers (with DisplayName), AspNetRoles, AspNetUserRoles, AspNetUserClaims, AspNetUserLogins, AspNetUserTokens, AspNetRoleClaims, RefreshTokens
- Run: `dotnet ef database update --project backend/src/JiApp.Identity --startup-project backend/src/JiApp.Identity`
- Acceptance: SQLite database created; all tables exist with correct schema

**B0.8: Create Gateway service**

- File: `backend/src/JiApp.Gateway/Program.cs`
  - Create WebApplication builder
  - Add YARP reverse proxy: `builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))`
  - Add JWT Bearer authentication with same params as Identity Server (ValidateIssuer/Audience/Key)
  - Add CORS (same permissive policy)
  - Pipeline: UseRouting, UseCors, UseAuthentication, UseAuthorization, MapReverseProxy()
- File: `backend/src/JiApp.Gateway/appsettings.json`
  - ReverseProxy section with Routes and Clusters:
    - Route "identity-route": Match path "/api/v1/auth/{**catch-all}", ClusterId "identity-cluster"
    - Route "yt-route": Match path "/api/v1/yt/{**catch-all}", ClusterId "yt-cluster"
    - Cluster "identity-cluster": Destination "identity-dest" — Address "http://localhost:5001"
    - Cluster "yt-cluster": Destination "yt-dest" — Address "http://localhost:5002"
  - Jwt section (same format as Identity for validation)
- File: `backend/src/JiApp.Gateway/Properties/launchSettings.json`
  - Profile "http": applicationUrl `http://localhost:5000`
- Acceptance: `dotnet run --project backend/src/JiApp.Gateway` starts; `GET http://localhost:5000/api/v1/auth/health` proxies to Identity (when running)

**B0.9: Create Docker Compose**

- File: `backend/docker-compose.yml`
  - Service "postgres": image postgres:16, ports 5432:5432, env POSTGRES_MULTIPLE_DATABASES=jiapp_identity,jiapp_ytdownloader, volume pgdata:/var/lib/postgresql/data
  - Service "identity": build ./src/JiApp.Identity, ports 5001:5001, depends_on postgres, env ConnectionStrings__IdentityDb with PostgreSQL connection string
  - Service "ytdownloader": build ./src/JiApp.YtDownloader, ports 5002:5002, depends_on postgres (placeholder — point to existing monolith or stub for Phase 0)
  - Service "gateway": build ./src/JiApp.Gateway, ports 5000:5000, depends_on [identity, ytdownloader]
  - Volumes: pgdata
- File: `backend/docker-compose.dev.yml`
  - Override ASPNETCORE_ENVIRONMENT=Development for all services
  - Mount source directories as volumes for hot reload
  - Override connection strings for SQLite (bypasses PostgreSQL in dev)
- File: `backend/src/JiApp.Identity/Dockerfile`
  - FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
  - Copy solution, restore, build, publish
  - FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
  - COPY --from=build, ENTRYPOINT dotnet JiApp.Identity.dll
- File: `backend/src/JiApp.Gateway/Dockerfile` (same pattern)
- File: `backend/src/JiApp.YtDownloader/Dockerfile` (same pattern, later extended with yt-dlp + ffmpeg)
- Acceptance: `docker compose build` succeeds for all services (may fail if YtDownloader has no Program.cs yet — OK as placeholder)

**B0.10: Add Gateway rate limiting**

- File: `backend/src/JiApp.Gateway/Startup.cs` (or Program.cs inline)
  - Add `services.AddRateLimiter(options => { ... })` with same 11 policies from monolith's Startup.cs (Login, Register, Health, DownloadFile, SearchVideos, SearchHistory, DownloadHistory, GetHistory, Me, GetDownloadLink, Preview)
  - Port rate limiting logic from `JiApp.Api.Startup.ConfigureRateLimiting` verbatim
  - Add `UseRateLimiter()` to pipeline before UseAuthentication
- File: `backend/src/JiApp.Common/Constants/RateLimitPolicyNames.cs` (move/extract from JiApp.Api if needed)
- Acceptance: Rate limiting policies configured; hitting Gateway with rapid requests returns 429

### Frontend Tasks

**F0.1: Create mobile shell directory structure**

- Create directories under `mobile/src/`:
  - `shell/` — Shell infrastructure
  - `modules/yt-downloader/` — YT Downloader module container
  - `modules/yt-downloader/screens/` — Screen components
  - `modules/yt-downloader/hooks/` — Module hooks
  - `modules/yt-downloader/services/` — Module services
  - `modules/yt-downloader/types/` — Module types
- Acceptance: Directories exist, listed in tsconfig.json paths

**F0.2: Define JiModule interface and ModuleRegistry**

- File: `mobile/src/shell/types.ts`
  - `export interface JiModule`
  - Properties: `id: string`, `name: string` (i18n key), `icon: string` (IconName for TabIcon), `component: React.ComponentType<any>`, `enabled: boolean`
- File: `mobile/src/shell/ModuleRegistry.ts`
  - `import type { JiModule } from './types'`
  - Export `moduleRegistry: JiModule[]` with first entry:
    - id: 'yt-downloader', name: 'modules.ytdownloader', icon: 'youtube', component: lazy-loaded placeholder, enabled: true
  - `export function getModule(id: string): JiModule | undefined` — find by id
  - `export function getEnabledModules(): JiModule[]` — filter enabled
- Acceptance: TypeScript compiles; `getEnabledModules()` returns one module

**F0.3: Create ModuleLoader placeholder**

- File: `mobile/src/shell/ModuleLoader.tsx`
  - `export default function ModuleLoader()`
  - Reads `getEnabledModules()` from ModuleRegistry
  - Renders a `createBottomTabNavigator()` with one tab per module
  - Each tab uses the module's icon (TabIcon) and component
  - For Phase 0: renders placeholder modules showing module name as text
- Acceptance: App renders placeholder module tabs; TypeScript type-safe

**F0.4: Update config for gateway URL**

- Edit `mobile/src/config.ts`: Change default `API_BASE_URL` from the monolith port to the gateway port `'http://localhost:5000/api/v1'`
- Add comment noting: override via `JIAPP_API_URL` env var for physical device IP
- Acceptance: API_BASE_URL points to gateway port; app compiles

**F0.5: Add refresh token storage functions**

- Edit `mobile/src/services/storageService.ts`:
  - Add constant `REFRESH_TOKEN_KEY = 'auth_refresh_token'`
  - Add functions: `saveRefreshToken(token: string): Promise<void>`, `getRefreshToken(): Promise<string | null>`, `clearRefreshToken(): Promise<void>`
  - Use EncryptedStorage (same as access token)
- Acceptance: New functions compile; unit test reads/writes refresh token

**F0.6: Update TypeScript types for new API contract**

- Edit `mobile/src/types/api.ts`:
  - Update `LoginResponse`: change `token` field to `accessToken: string`, add `refreshToken: string`, add `expiresIn: number`, rename `id` to `userId`
  - Add `RefreshRequest`: `{ refreshToken: string }`
  - Add `RefreshResponse`: `{ accessToken: string, refreshToken: string, expiresIn: number }`
  - Add `LogoutRequest`: `{ refreshToken: string }`
  - Keep existing types: LoginRequest, RegisterRequest, SearchRequest, SearchResponse, VideoItem, DownloadRequest, DownloadResponse, SearchHistoryItem, DownloadHistoryItem, HistoryResponse
- Acceptance: All existing code that uses `LoginResponse.token` now shows TypeScript errors (will be fixed in Phase 1)

**F0.7: Add i18n keys for new UI**

- Edit `mobile/src/i18n/pl.json`: Add keys for:
  - `modules.ytdownloader` — "YT Downloader"
  - `modules.pdfsuite` — "PDF Suite" (placeholder for Phase 4)
  - `modules.imagetools` — "Narzędzia Obrazów" (placeholder for Phase 4)
  - `shell.settings` — "Ustawienia"
  - `shell.modules` — "Moduły"
  - `auth.sessionExpired` — "Sesja wygasła. Zaloguj się ponownie."
  - `auth.tokenRefreshing` — "Odświeżanie sesji..."
- Edit `mobile/src/i18n/en.json`: English equivalents
- Acceptance: New keys accessible via `useTranslation()`; app compiles

### Integration Tasks

- Verify: `dotnet build backend/JiApp.sln` succeeds with all new projects
- Verify: Identity service starts on port 5001, health endpoint responds
- Verify: Gateway starts on port 5000, proxies health check to Identity
- Verify: `npm run build` in mobile/ succeeds with new directory structure
- Verify: All existing tests still pass (backend: `dotnet test`, mobile: `npm test`)

### Definition of Done — Phase 0

- [ ] `dotnet build backend/JiApp.sln` succeeds with zero errors (5 existing + 5 new projects)
- [ ] `dotnet run --project backend/src/JiApp.Identity` starts on port 5001
- [ ] `dotnet run --project backend/src/JiApp.Gateway` starts on port 5000
- [ ] Gateway proxies `/api/v1/auth/health` → Identity service
- [ ] Identity database created with AspNetUsers + RefreshTokens tables
- [ ] Docker Compose file valid (`docker compose config` succeeds)
- [ ] Mobile shell directory structure created: `mobile/src/shell/`, `mobile/src/modules/`
- [ ] ModuleRegistry exports one enabled module (yt-downloader)
- [ ] API_BASE_URL points to gateway port 5000
- [ ] Refresh token storage functions added to storageService.ts
- [ ] TypeScript types updated for new login/refresh/logout contracts
- [ ] All existing monolith features still work (can be verified independently)

---

## Phase 1: Service Extraction

**Goal:** Extract YT Downloader into standalone microservice. Migrate Identity Server to handle auth (login, register, refresh, logout, me). Update mobile to use refresh token flow. The monolith JiApp.Api can still run during this phase — no need to shut it down until Phase 1 is verified.

### Prerequisites
- Phase 0 complete and verified
- Identity service runs on port 5001
- Gateway runs on port 5000

### Backend Tasks

**B1.1: Implement Identity endpoints — Register**

- Port from `backend/src/JiApp.Api/Features/Auth/Register/` to `backend/src/JiApp.Identity/Features/Auth/Register/`
- Copy and update namespace: RegisterRequest.cs, RegisterResponse.cs, RegisterValidator.cs, RegisterHandler.cs, RegisterEndpoint.cs
- RegisterHandler: receives `UserManager<User>`, logic identical — check username exists, check email exists, create User with DisplayName, call CreateAsync with password
- RegisterEndpoint: POST `/api/v1/auth/register`, anonymous, returns 201 Created on success, 400 with errors on failure
- Acceptance: POST to `http://localhost:5001/api/v1/auth/register` creates user; duplicate username returns 400; validation errors return 400
- Acceptance: POST to `http://localhost:5000/api/v1/auth/register` through gateway works identically

**B1.2: Implement Identity endpoints — Login (with refresh token)**

- Port from `backend/src/JiApp.Api/Features/Auth/Login/` to `backend/src/JiApp.Identity/Features/Auth/Login/`
- Files: LoginRequest.cs (unchanged — Username, Password), LoginValidator.cs (unchanged), LoginResponse.cs (UPDATED — `long UserId`, `string? DisplayName`, `string AccessToken`, `string RefreshToken`, `int ExpiresIn`), LoginHandler.cs, LoginEndpoint.cs
- LoginHandler changes:
  - After successful sign-in: calls `_jwtTokenService.GenerateToken(user.Id, user.UserName!)` for access token
  - Calls `_refreshTokenService.CreateAsync(user.Id)` for refresh token
  - Returns `LoginResponse { UserId = user.Id, DisplayName = user.DisplayName, AccessToken = jwt, RefreshToken = refreshToken.Token, ExpiresIn = accessTokenExpireMinutes * 60 }`
- LoginEndpoint: POST `/api/v1/auth/login`, anonymous, returns 200 with LoginResponse, 401 with "Invalid credentials"
- Acceptance: POST login returns accessToken (JWT), refreshToken (opaque string), userId, displayName, expiresIn (900)
- Acceptance: JWT decodes to correct claims (nameidentifier=userId, name=username)

**B1.3: Implement Identity endpoints — Refresh**

- File: `backend/src/JiApp.Identity/Features/Auth/Refresh/RefreshRequest.cs`
  - `public sealed record RefreshRequest(string RefreshToken)`
- File: `backend/src/JiApp.Identity/Features/Auth/Refresh/RefreshResponse.cs`
  - `public sealed record RefreshResponse(string AccessToken, string RefreshToken, int ExpiresIn)`
- File: `backend/src/JiApp.Identity/Features/Auth/Refresh/RefreshHandler.cs`
  - Constructor: `RefreshTokenService refreshTokenService`, `UserManager<User> userManager`, `JwtTokenService jwtTokenService`
  - Method: `async Task<Result<RefreshResponse>> HandleAsync(RefreshRequest request)`
  - Logic: call `refreshTokenService.ValidateAsync(request.RefreshToken)`, if null return Failure("Invalid or expired refresh token"), get user by refreshToken.UserId, revoke old refresh token, generate new access token + new refresh token, return Success
- File: `backend/src/JiApp.Identity/Features/Auth/Refresh/RefreshEndpoint.cs`
  - POST `/api/v1/auth/refresh`, anonymous, returns 200 on success, 401 on failure
- Acceptance: Valid refresh token returns new accessToken + new refreshToken; old refreshToken cannot be reused (rotation)

**B1.4: Implement Identity endpoints — Logout**

- File: `backend/src/JiApp.Identity/Features/Auth/Logout/LogoutRequest.cs`
  - `public sealed record LogoutRequest(string RefreshToken)`
- File: `backend/src/JiApp.Identity/Features/Auth/Logout/LogoutHandler.cs`
  - Constructor: `RefreshTokenService refreshTokenService`
  - Method: `async Task<Result> HandleAsync(LogoutRequest request)`
  - Logic: validate refresh token, if valid — revoke it, always return Success (idempotent — logging out with invalid token is still a successful logout)
- File: `backend/src/JiApp.Identity/Features/Auth/Logout/LogoutEndpoint.cs`
  - POST `/api/v1/auth/logout`, anonymous, returns 200 (always — idempotent)
- Acceptance: POST logout with valid token returns 200; same token cannot be refreshed after logout

**B1.5: Implement Identity endpoints — Me**

- Port from `backend/src/JiApp.Api/Features/Auth/Me/` to `backend/src/JiApp.Identity/Features/Auth/Me/`
- Files: MeResponse.cs (unchanged — `long Id`, `string? DisplayName`, `string? CurrentUsername`), MeHandler.cs, MeEndpoint.cs
- MeHandler: constructor receives `UserManager<User>`, `ICurrentUserService`; finds user by currentUserId, returns MeResponse
- MeEndpoint: GET `/api/v1/auth/me`, requires authorization, returns 200 or 401
- Acceptance: GET me with valid JWT returns user info; without token returns 401

**B1.6: Create YtDownloader DbContext**

- File: `backend/src/JiApp.YtDownloader/Persistence/YtDbContext.cs`
  - `public sealed class YtDbContext : DbContext` (NOT IdentityDbContext — no identity tables)
  - Constructor: `YtDbContext(DbContextOptions<YtDbContext> options)`
  - DbSet properties: `YoutubeSearchHistory`, `YoutubeDownloadHistory`, `EventLogs`
  - Override `OnModelCreating`: call `builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly())`
- Copy entity configurations from `JiApp.Infrastructure/Persistence/Configurations/`:
  - `YoutubeSearchHistoryConfiguration.cs` — remove FK to User (UserId is just a `long`, no navigation property)
  - `YoutubeDownloadHistoryConfiguration.cs` — same
  - `EventLogConfiguration.cs` — same
- Add index on `UserId` for all three tables (query pattern: per-user history)
- Acceptance: DbContext compiles; no Identity dependencies in the project

**B1.7: Create YtDownloader Program.cs and Startup.cs**

- File: `backend/src/JiApp.YtDownloader/Program.cs`
  - Create builder, bind settings, validate, create Startup, call ConfigureServices then Configure
- File: `backend/src/JiApp.YtDownloader/Startup.cs`
  - `ConfigureServices`:
    - AddEndpointsApiExplorer, AddApiVersioning
    - AddDbContext<YtDbContext> with SQLite/PostgreSQL provider swap
    - AddAuthentication(JwtBearer) — validates tokens issued by Identity Server (same key, issuer, audience)
    - AddAuthorization (default policy: require authenticated user)
    - AddCors, AddHttpContextAccessor
    - AddScoped<ISearchHistoryRepository, SearchHistoryRepository>
    - AddScoped<IDownloadHistoryRepository, DownloadHistoryRepository>
    - AddScoped<IEventLogRepository, EventLogRepository>
    - AddScoped<ITempFileStore, TempFileStore>
    - AddScoped<ICurrentUserService, CurrentUserService>
    - AddSingleton<IYoutubeClient>(sp => new YoutubeClient(settings.Youtube!.ApiKey!, settings.Youtube!.YtDlpPath!, settings.Youtube!.FfmpegPath!))
    - AddSingleton(settings)
    - Register all YT handlers and validators
  - `Configure`:
    - UseMiddleware<GlobalExceptionMiddleware>
    - UseSerilogRequestLogging
    - UseRouting, UseCors, UseAuthentication, UseAuthorization
    - Map endpoints on `/api/v1/yt` group
    - Map health check
- File: `backend/src/JiApp.YtDownloader/appsettings.json`
  - ConnectionStrings: YtDb (SQLite path for dev)
  - Jwt: Key, Issuer, Audience (same values as Identity, for validation)
  - Youtube: ApiKey, YtDlpPath, FfmpegPath
  - Serilog: Console + File
- File: `backend/src/JiApp.YtDownloader/appsettings.Development.json`
  - ConnectionStrings:YtDb = "Data Source=../../.data/yt_dev.db"
  - Kestrel endpoints: Http at `http://*:5002`
- Acceptance: `dotnet run --project backend/src/JiApp.YtDownloader` starts on port 5002; health check returns 200

**B1.8: Create YtDownloader initial migration**

- Run: `dotnet ef migrations add InitYtDownloader --project backend/src/JiApp.YtDownloader --startup-project backend/src/JiApp.YtDownloader --output-dir Persistence/Migrations`
- Run: `dotnet ef database update --project backend/src/JiApp.YtDownloader --startup-project backend/src/JiApp.YtDownloader`
- Acceptance: SQLite database created with YoutubeSearchHistory, YoutubeDownloadHistory, EventLogs tables

**B1.9: Port YT Downloader endpoints**

For each feature slice, copy from `JiApp.Api/Features/` to `JiApp.YtDownloader/Features/`, update namespace, adjust any Identity references:

- Port SearchVideos: POST `/api/v1/yt/search` — handler uses IYoutubeClient, stores to SearchHistoryRepository
- Port SearchHistory: GET `/api/v1/yt/search/history` — handler queries SearchHistoryRepository by userId
- Port ArchiveSearch: DELETE `/api/v1/yt/search/{id}` — handler marks IsArchived = true
- Port GetDownloadLink: POST `/api/v1/yt/downloads/mp3` — handler downloads via IYoutubeClient + yt-dlp
- Port DownloadFile: GET `/api/v1/yt/downloads/mp3/file/{id}` — handler streams file from TempFileStore
- Port DownloadHistory: GET `/api/v1/yt/downloads/history` — handler queries DownloadHistoryRepository
- Port ArchiveDownload: DELETE `/api/v1/yt/downloads/{id}` — handler marks IsArchived = true
- Port GetHistory: GET `/api/v1/yt/history` — combined search + download history
- Port StreamPreview: GET `/api/v1/yt/preview/{videoId}` — audio preview streaming

Key change in all handlers: userId comes from `ICurrentUserService.UserId` (read from JWT claims), not from a joined User table.

- Acceptance: Each endpoint works when called via `http://localhost:5002/api/v1/yt/...` with valid JWT
- Acceptance: Each endpoint works when called via gateway `http://localhost:5000/api/v1/yt/...`

**B1.10: Extract shared middleware to Common**

- Copy `backend/src/JiApp.Api/Middleware/GlobalExceptionMiddleware.cs` to `backend/src/JiApp.Common/Middleware/GlobalExceptionMiddleware.cs`
- Copy `backend/src/JiApp.Api/Configuration/SwaggerConstants.cs` to `backend/src/JiApp.Common/Constants/SwaggerConstants.cs`
- Update namespaces to JiApp.Common
- Reference JiApp.Common from Identity and YtDownloader (already referenced)
- Remove duplicate middleware files from JiApp.Api (no longer needed there long-term)
- Acceptance: Both services use the shared GlobalExceptionMiddleware; error format is consistent

**B1.11: Create YtDownloader Dockerfile with yt-dlp + ffmpeg**

- File: `backend/src/JiApp.YtDownloader/Dockerfile`
  - Stage 1: build from SDK image (same as Identity)
  - Stage 2: FROM mcr.microsoft.com/dotnet/aspnet:10.0
  - RUN apt-get update && apt-get install -y python3 python3-pip ffmpeg
  - RUN pip3 install yt-dlp
  - COPY --from=build /app/publish /app
  - Set yt-dlp and ffmpeg paths in environment or appsettings
- Acceptance: `docker build -t jiapp-ytdownloader ./src/JiApp.YtDownloader` succeeds

**B1.12: Update Gateway configuration for full routing**

- Edit `backend/src/JiApp.Gateway/appsettings.json`:
  - Add health route: Match path "/api/v1/health", ClusterId "identity-cluster"
  - Verify YT routes correctly proxy to `http://localhost:5002` (or `http://ytdownloader:5002` in Docker)
- Add CORS handling in Gateway pipeline (before proxy)
- Acceptance: All API calls through gateway reach the correct service; CORS headers present

**B1.13: Add back-end tests for Identity**

- Create tests in `backend/tests/JiApp.Identity.Tests/`:
  - `RefreshTokenServiceTests`: create, validate valid, validate invalid, validate expired, revoke, validate after revoke
  - `JwtTokenServiceTests`: generate token, validate token, extract claims, expired token
  - `RegisterHandlerTests` (integration): valid registration, duplicate username, duplicate email, validation errors
  - `LoginHandlerTests` (integration): valid login returns tokens, invalid credentials returns failure
  - `RefreshHandlerTests` (integration): valid refresh, invalid token, rotation (old token revoked)
- Acceptance: `dotnet test backend/tests/JiApp.Identity.Tests/` — all tests pass

**B1.14: Add back-end tests for YtDownloader**

- Port relevant tests from `backend/tests/JiApp.Tests/` to `backend/tests/JiApp.YtDownloader.Tests/`:
  - SearchVideosHandler tests
  - DownloadHistoryHandler tests
  - SearchHistoryHandler tests
  - GetHistoryHandler tests
  - Archive endpoint tests
- Use `CustomWebApplicationFactory<Program>` for YtDownloader (port pattern from existing tests)
- Acceptance: `dotnet test backend/tests/JiApp.YtDownloader.Tests/` — all tests pass

### Frontend Tasks

**F1.1: Update authService for new API contract**

- Edit `mobile/src/services/authService.ts`:
  - Update `login()`: read `accessToken`, `refreshToken`, `expiresIn`, `userId`, `displayName` from new LoginResponse shape
  - Update `checkToken()`: the /me endpoint returns `MeResponse` (no token field) — return type should be `MeResponse`, not `LoginResponse`
  - Add `refreshToken(refreshToken: string): Promise<RefreshResponse>` — calls POST `/auth/refresh` with `{ refreshToken }`
  - Add `logoutRemote(refreshToken: string): Promise<void>` — calls POST `/auth/logout` with `{ refreshToken }`
- Acceptance: authService compiles; functions call correct endpoints

**F1.2: Update AuthContext with refresh token flow**

- Edit `mobile/src/context/AuthContext.tsx`:
  - Add `refreshToken: string | null` to AuthState
  - Update LOGIN action: add refreshToken
  - Update LOGOUT action: clear refreshToken
  - Update RESTORE_TOKEN action: add refreshToken
  - Update `login()` callback:
    - Call `authService.login()`, get `{ accessToken, refreshToken, expiresIn, userId, displayName }`
    - Save via storageService: `saveToken(accessToken)`, `saveRefreshToken(refreshToken)`, `saveUserId(userId)`, `saveDisplayName(displayName)`
    - Dispatch LOGIN with all fields including refreshToken
  - Update `logout()` callback:
    - Read refreshToken from storage
    - If refreshToken exists, call `authService.logoutRemote(refreshToken)` (fire-and-forget — don't block on network)
    - Clear all storage: token, refreshToken, userId, displayName, username, credentials
    - Dispatch LOGOUT
  - Update `checkToken()`:
    - Read token from storage (existing behavior)
    - Read refreshToken from storage (new)
    - Call /me with token
    - If 200: dispatch RESTORE_TOKEN with token, refreshToken, userId, displayName, username
    - If 401: try refresh — if succeeds, dispatch RESTORE_TOKEN with new tokens; if fails, clear all and dispatch LOGOUT
  - Remove credential-based silent re-login entirely (no more `savedCredentials` auto-login on 401)
- Acceptance: Login stores both tokens; app restore validates token then falls back to refresh; logout clears everything

**F1.3: Update apiClient with refresh interceptor**

- Edit `mobile/src/services/apiClient.ts`:
  - Remove credential-based re-login response interceptor (delete `getCredentials`, `login` import, credential save/clear logic)
  - New 401 response interceptor:
    - If `error.response?.status === 401 && !config._isRetry`:
    - Read refreshToken from storage
    - If refreshToken exists:
      - Call `authService.refreshToken(refreshToken)` (direct axios call, bypass apiClient to avoid interceptor loop)
      - Save new accessToken + refreshToken to storage
      - Set `config._isRetry = true`
      - Set `config.headers.Authorization = Bearer <newAccessToken>`
      - Retry original request
    - If refresh fails or no refreshToken: clear all auth storage, reject
  - Keep request interceptor: attach Bearer token from EncryptedStorage
- Acceptance: When access token expires, next request triggers refresh and retries automatically; if refresh also fails, user is logged out

**F1.4: Move YT Downloader screens to module directory**

- Move files (preserving git history where possible):
  - `mobile/src/screens/SearchScreen.tsx` → `mobile/src/modules/yt-downloader/screens/SearchScreen.tsx`
  - `mobile/src/screens/DownloadScreen.tsx` → `mobile/src/modules/yt-downloader/screens/DownloadScreen.tsx`
  - `mobile/src/screens/DownloadsScreen.tsx` → `mobile/src/modules/yt-downloader/screens/DownloadsScreen.tsx`
  - `mobile/src/screens/HistoryScreen.tsx` → `mobile/src/modules/yt-downloader/screens/HistoryScreen.tsx`
- Update imports in each moved file (relative paths to hooks, services, types)
- Update any external imports (AppNavigator, MainNavigator) — keep in sync with Phase 2 changes
- Acceptance: App compiles with screens in new locations (temporarily broken navigation — fixed in Phase 2)

**F1.5: Move YT Downloader hooks to module directory**

- Move files:
  - `mobile/src/hooks/useSearch.ts` → `mobile/src/modules/yt-downloader/hooks/useSearch.ts`
  - `mobile/src/hooks/useDownload.ts` → `mobile/src/modules/yt-downloader/hooks/useDownload.ts`
  - `mobile/src/hooks/useHistory.ts` → `mobile/src/modules/yt-downloader/hooks/useHistory.ts`
  - `mobile/src/hooks/usePreview.ts` → `mobile/src/modules/yt-downloader/hooks/usePreview.ts`
- Update imports within hooks (services, types)
- Acceptance: Hooks compile in new locations

**F1.6: Move YT Downloader services to module directory**

- Move files:
  - `mobile/src/services/searchService.ts` → `mobile/src/modules/yt-downloader/services/searchService.ts`
  - `mobile/src/services/downloadService.ts` → `mobile/src/modules/yt-downloader/services/downloadService.ts`
  - `mobile/src/services/historyService.ts` → `mobile/src/modules/yt-downloader/services/historyService.ts`
  - `mobile/src/services/previewService.ts` → `mobile/src/modules/yt-downloader/services/previewService.ts`
- Update apiClient import path (now `../../../services/apiClient`)
- Acceptance: Services compile in new locations

**F1.7: Create YT Downloader module definition**

- File: `mobile/src/modules/yt-downloader/index.ts`
  - Export `ytDownloaderModule: JiModule`:
    - id: 'yt-downloader'
    - name: 'modules.ytdownloader'
    - icon: 'youtube'
    - component: `YtNavigator` (placeholder until Phase 2 — imported from `modules/yt-downloader/navigator`)
  - Export barrel re-exports: `export { useSearch } from './hooks/useSearch'` etc. for external consumers (optional)
- File: `mobile/src/modules/yt-downloader/navigator.tsx`
  - Placeholder: renders `<Text>YT Downloader</Text>` — full navigator written in Phase 2
- Acceptance: Module definition exports valid JiModule; Ready for ModuleRegistry

**F1.8: Move YT Downloader types to module directory**

- Move YT-specific types from `mobile/src/types/api.ts` to `mobile/src/modules/yt-downloader/types/api.ts`:
  - SearchRequest, SearchResponse, VideoItem
  - DownloadRequest, DownloadResponse
  - SearchHistoryItem, DownloadHistoryItem, HistoryResponse
- Keep in `mobile/src/types/api.ts`: shared types only — LoginRequest, LoginResponse, RegisterRequest, RefreshRequest, RefreshResponse, LogoutRequest, MeResponse
- Update all imports throughout the codebase
- Acceptance: All TypeScript imports resolve correctly

**F1.9: Update mobile tests for moved files**

- Move test files to match new source locations:
  - `mobile/src/screens/__tests__/SearchScreen.test.tsx` → `mobile/src/modules/yt-downloader/screens/__tests__/SearchScreen.test.tsx`
  - (similarly for DownloadScreen, HistoryScreen, LoginScreen, RegisterScreen, SettingsScreen test files)
  - Hook tests: `mobile/src/hooks/__tests__/useSearch.test.tsx` → `mobile/src/modules/yt-downloader/hooks/__tests__/useSearch.test.tsx`
- Update import paths in all moved test files
- Add tests for refresh token flow:
  - `mobile/src/services/__tests__/apiClient.test.ts`: add test case — 401 triggers refresh, retries original request; refresh failure logs out
  - `mobile/src/context/__tests__/AuthContext.test.tsx`: add test case — login saves refreshToken, checkToken falls back to refresh, logout calls /auth/logout
- Acceptance: `npm test` passes with zero failures

### Integration Tasks

- Start all three services (Identity on 5001, YtDownloader on 5002, Gateway on 5000)
- Full flow test: Register → Login → get access+refresh tokens → hit YT search with access token → wait/simulate expiry → hit YT search again (should 401 → refresh → retry → 200) → Logout → try refresh (should fail)
- Verify existing monolith still runs on port 5003 (not affected by new services)
- Run full backend test suite: `dotnet test backend/` — all tests pass
- Run full mobile test suite: `npm test` in mobile/ — all tests pass

### Definition of Done — Phase 1

- [ ] Identity service: Register, Login (with refresh token), Refresh (with rotation), Logout, Me — all working via gateway
- [ ] YtDownloader service: All 9 YT endpoints ported, working via gateway with JWT auth
- [ ] Refresh token rotation: using old refresh token after rotation returns 401
- [ ] Mobile: Login stores both access and refresh tokens
- [ ] Mobile: 401 interceptor refreshes token and retries original request
- [ ] Mobile: Refresh failure clears auth and redirects to login
- [ ] Mobile: Logout calls remote logout endpoint and clears local storage
- [ ] Credential-based silent re-login completely removed
- [ ] YT Downloader screens, hooks, services moved to `modules/yt-downloader/`
- [ ] All backend tests pass (Identity + YtDownloader + existing monolith tests)
- [ ] All mobile tests pass with files in new locations
- [ ] Gateway routes all traffic correctly; CORS headers present
- [ ] Monolith still runs on port 5003 (not yet decommissioned)

---

## Phase 2: Mobile Shell

**Goal:** Implement dynamic module loading, shell-level navigation, and settings at the shell level. The mobile app becomes a true "Shell" that loads the YT Downloader module and any future modules.

### Prerequisites
- Phase 1 complete and verified
- YT Downloader module files moved to `modules/yt-downloader/`

### Frontend Tasks

**F2.1: Create YtNavigator (module stack navigator)**

- File: `mobile/src/modules/yt-downloader/navigator.tsx`
  - `export default function YtNavigator()`
  - Creates a `createNativeStackNavigator<YtStackParamList>()` with 4 screens:
    - SearchScreen (component from `../screens/SearchScreen`)
    - DownloadScreen (component from `../screens/DownloadScreen`)
    - DownloadsScreen (component from `../screens/DownloadsScreen`)
    - HistoryScreen (component from `../screens/HistoryScreen`)
  - Stack navigator options: headerShown false (screens manage their own headers)
  - Import `YtStackParamList` from module types
- File: `mobile/src/modules/yt-downloader/types/navigation.ts`
  - `export type YtStackParamList`
  - Keys: Search (undefined), Download (VideoItem), Downloads (undefined), History (undefined)
- Acceptance: YtNavigator renders SearchScreen as initial route; navigation between screens works

**F2.2: Create ShellNavigator**

- File: `mobile/src/shell/ShellNavigator.tsx`
  - `export default function ShellNavigator()`
  - Creates a `createNativeStackNavigator<ShellStackParamList>()` with:
    - ModuleLoader (main screen, headerShown: false)
    - SettingsScreen (stack-level, not a module tab)
  - ShellStackParamList: ModuleLoader (undefined), Settings (undefined)
- File: `mobile/src/shell/types.ts` (add to existing):
  - `export type ShellStackParamList = { ModuleLoader: undefined; Settings: undefined }`
- Acceptance: ShellNavigator renders ModuleLoader by default; can navigate to Settings

**F2.3: Implement ModuleLoader with dynamic tabs**

- Rewrite `mobile/src/shell/ModuleLoader.tsx`:
  - Reads `getEnabledModules()` from ModuleRegistry
  - Creates a `createBottomTabNavigator()` dynamically with one tab per enabled module
  - Each tab: name = module.id, icon = passes module.icon to TabIcon, component = module.component
  - Tab bar options: lazy: true (modules load on first tab press)
  - Uses existing TabIcon and TabBarButton components (imported from components/)
- Acceptance: Bottom tab bar shows YT Downloader tab; pressing it loads YtNavigator with SearchScreen

**F2.4: Update ModuleRegistry with real module**

- Edit `mobile/src/shell/ModuleRegistry.ts`:
  - Import `YtNavigator` component (lazy or direct)
  - Replace placeholder with real module definition
  - Add `component: YtNavigator`
- Acceptance: ModuleRegistry exports yt-downloader module with real navigator

**F2.5: Update AppNavigator for shell**

- Edit `mobile/src/navigation/AppNavigator.tsx`:
  - When authenticated: render `<ShellNavigator />` instead of `<MainNavigator />`
  - When not authenticated: render `<AuthNavigator />` (unchanged)
  - Remove MainNavigator import (keep file for reference during migration)
- Acceptance: Logged-in users see ModuleLoader with YT Downloader tab; logged-out users see AuthNavigator

**F2.6: Move SettingsScreen to shell level**

- SettingsScreen was already at `mobile/src/screens/SettingsScreen.tsx`; it stays there but is now rendered from ShellNavigator (stack-level), not as a tab
- Edit SettingsScreen: remove any tab-specific navigation (it's now a stack screen)
- Add a "Modules" section placeholder (will list available modules in the future)
- Full settings functionality preserved: language switching, logout, app version
- Acceptance: Settings accessible from shell header/navigation; all existing settings work

**F2.7: Add shell header with settings gear**

- Edit `mobile/src/shell/ModuleLoader.tsx`:
  - Add header/right button: gear icon — navigates to Settings (ShellNavigator stack)
- Or edit individual module navigators to include a common header with settings access
- Acceptance: Settings icon visible from module tabs; navigates to SettingsScreen

**F2.8: Clean up deprecated navigators**

- Remove `mobile/src/navigation/MainNavigator.tsx` (replaced by ShellNavigator + ModuleLoader)
- Remove `mobile/src/navigation/__tests__/MainNavigator.test.tsx`
- Update any imports referencing MainNavigator
- Keep `mobile/src/navigation/AuthNavigator.tsx` (unchanged)
- Keep `mobile/src/navigation/types.ts` — update to export `ShellStackParamList` or remove type definitions that moved to shell/types.ts
- Acceptance: `npm run build` succeeds; no reference to MainNavigator

**F2.9: Add module-level tests**

- File: `mobile/src/shell/__tests__/ModuleLoader.test.tsx`
  - Test: renders all enabled modules as tabs
  - Test: disabled modules are excluded
  - Test: empty registry renders empty state
- File: `mobile/src/shell/__tests__/ShellNavigator.test.tsx`
  - Test: renders ModuleLoader by default
  - Test: can navigate to Settings
- File: `mobile/src/shell/__tests__/ModuleRegistry.test.ts`
  - Test: getModule returns correct module by id
  - Test: getEnabledModules filters disabled
- File: `mobile/src/modules/yt-downloader/__tests__/navigator.test.tsx`
  - Test: YtNavigator renders SearchScreen initially
- Acceptance: Shell tests pass; `npm test` zero failures

### Integration Tasks

- Verify: app launches → Login → ModuleLoader with YT tab → tap YT tab → SearchScreen loads
- Verify: Settings accessible; language change works; logout works
- Verify: Adding a second placeholder module to ModuleRegistry creates second tab without code changes to ModuleLoader
- Verify: Lazy loading: second module's JS bundle not loaded until tab pressed

### Definition of Done — Phase 2

- [ ] ModuleLoader dynamically renders tabs from ModuleRegistry
- [ ] YT Downloader works as a loaded module (Search → Download → History)
- [ ] SettingsScreen accessible from shell level
- [ ] Language switching, logout work from shell-level settings
- [ ] MainNavigator removed; AppNavigator uses ShellNavigator
- [ ] Adding a module to ModuleRegistry automatically creates its tab (no ModuleLoader changes)
- [ ] All mobile tests pass
- [ ] No regression in YT Downloader functionality

---

## Phase 3: Hardening

**Goal:** Health checks for all services, structured logging correlation, unified error handling, rate limiting at gateway, Docker Compose full integration test.

### Prerequisites
- Phase 2 complete and verified
- All services runnable locally

### Backend Tasks

**B3.1: Add health checks to all services**

- Add to Identity `Program.cs`/`Startup.cs`:
  - `services.AddHealthChecks().AddDbContextCheck<IdentityDbContext>("identity-db")`
  - Map `/health` endpoint returning health status JSON
- Add to YtDownloader:
  - `services.AddHealthChecks().AddDbContextCheck<YtDbContext>("yt-db").AddCheck("yt-dlp", () => { ... })` — check yt-dlp binary exists
- Add to Gateway:
  - Proxy health checks: `/health` → aggregate Identity + YtDownloader health
  - Add `/health/ready` (readiness, checked by Docker)
  - Add `/health/live` (liveness, always 200 unless process is dying)
- Acceptance: `GET /health` on each service returns structured JSON with dependency status

**B3.2: Add structured logging with correlation IDs**

- In Gateway Program.cs:
  - Middleware that generates or reads `X-Correlation-ID` header; forwards to downstream services
  - Log correlation ID on every request
- In Identity and YtDownloader Program.cs:
  - Read `X-Correlation-ID` from incoming headers (forwarded by Gateway)
  - Enrich Serilog with correlation ID: `Enrich.WithProperty("CorrelationId", ...)`
- Configure Serilog in all services:
  - Console sink in Development (compact JSON for Docker)
  - File sink with rolling interval for local dev
  - Use `{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj} {Properties:j}` template
- Acceptance: All logs from a single API request share the same CorrelationId across services

**B3.3: Unified error response format**

- Verify all error responses across Identity and YtDownloader use `ApiErrorResponse` from JiApp.Common
- Check: GlobalExceptionMiddleware, JWT challenge, rate limiter, validation errors, handler errors
- In Identity: ensure refresh token errors, login errors, register validation errors all use same shape: `{ error: string, details?: Record<string, string[]>, retryAfterSeconds?: string }`
- In YtDownloader: ensure download errors, search errors, file not found errors use same shape
- Acceptance: All error responses have consistent JSON shape; mobile error handling works for all endpoints

**B3.4: Gateway rate limiting with service-aware policies**

- Verify rate limiting policies from Phase 0 B0.10 are correctly routing limits:
  - Login (5 req/15s) — applied to `/api/v1/auth/login`
  - Register (3 req/60s) — applied to `/api/v1/auth/register`
  - SearchVideos (30 req/60s) — applied to `/api/v1/yt/search`
  - DownloadFile (10 req/60s) — applied to `/api/v1/yt/downloads/mp3`
  - Preview (30 req/60s) — applied to `/api/v1/yt/preview`
  - etc.
- Add `X-RateLimit-Remaining` and `X-RateLimit-Reset` response headers on rate-limited endpoints
- Acceptance: Rapid requests return 429 with consistent error format; headers present

**B3.5: Docker Compose production config**

- Create `backend/docker-compose.prod.yml` with overrides:
  - ASPNETCORE_ENVIRONMENT=Production
  - PostgreSQL connection strings (not SQLite)
  - Identity: no hot reload volumes
  - YtDownloader: no hot reload volumes
  - Postgres: persistent volume, no port exposure to host (internal network only)
  - Gateway: expose port 5000 only (no direct access to Identity/YtDownloader from host)
- Create `.env.example` with required environment variables (JWT key, YouTube API key, DB passwords)
- Acceptance: `docker compose -f docker-compose.yml -f docker-compose.prod.yml up` starts all services; gateway accessible on port 5000

**B3.6: CI/CD configuration (placeholder)**

- File: `.github/workflows/build.yml` (or `.gitlab-ci.yml`)
  - Job: Build — `dotnet build backend/JiApp.sln`, `npm ci && npm run build` in mobile/
  - Job: Test — `dotnet test backend/`, `npm test` in mobile/
  - Job: Docker — `docker compose build`
- Acceptance: CI file exists; can be triggered manually (GitHub Actions / GitLab CI setup deferred to user)

**B3.7: Add health check dashboard (optional)**

- File: `backend/src/JiApp.Gateway/HealthDashboard/HealthDashboardEndpoint.cs`
  - GET `/health/dashboard` (Development only) — returns HTML page showing all service statuses with auto-refresh
- Acceptance: Dev-only dashboard shows Identity + YtDownloader health status

### Frontend Tasks

**F3.1: Add network error retry logic**

- Edit `mobile/src/services/apiClient.ts`:
  - Add response interceptor for network errors (status 0, no response): show "Network error. Check your connection." toast
  - Add retry logic for 5xx errors: retry once with 1s delay (only for GET requests, not POST)
- Acceptance: Network errors show user-friendly message; 5xx errors retried once

**F3.2: Add session expired toast**

- Edit `mobile/src/context/AuthContext.tsx`:
  - When refresh token fails and user is logged out: show toast "Sesja wygasła. Zaloguj się ponownie."
  - Use existing ToastContext
- Acceptance: After session expiry and failed refresh, user sees toast before login screen

**F3.3: Add loading states across modules**

- Audit all YT Downloader screens for loading/empty/error states:
  - SearchScreen: loading spinner during search, "no results" empty state, error with retry
  - DownloadScreen: progress during download, success animation, error state
  - DownloadsScreen: loading during fetch, empty state ("No downloads yet"), error with retry
  - HistoryScreen: loading during fetch, empty state ("No history yet"), error with retry
- Ensure all states use existing LoadingSpinner, ErrorMessage, SuccessCheckmark components
- Acceptance: Every screen handles loading, empty, and error states gracefully

**F3.4: Run full mobile test suite and fix regressions**

- Run `npm test` in mobile/ — ensure all tests pass
- If any moved files caused test breakage, fix import paths and mocks
- Verify TypeScript strict mode: `npx tsc --noEmit` passes
- Acceptance: Zero TypeScript errors; zero test failures

### Integration Tasks

- Run `docker compose up` fully: verify Identity, YtDownloader, Gateway, Postgres all start
- Full API test via Gateway: Register → Login → Search → Download → History → Refresh → Logout
- Check logs: all requests have matching CorrelationId across services
- Rate limiting: hammer login endpoint, verify 429 after threshold
- Health checks: verify all services report healthy

### Definition of Done — Phase 3

- [ ] All services have health checks with dependency status
- [ ] Structured logging with correlation IDs across all services
- [ ] Unified error response format on all endpoints
- [ ] Rate limiting enforced at gateway for all endpoints
- [ ] Docker Compose production config ready
- [ ] CI pipeline file exists (build + test + docker)
- [ ] Network error handling in mobile
- [ ] Session expired toast on refresh failure
- [ ] All screens have loading/empty/error states
- [ ] Full end-to-end flow works via Docker Compose

---

## Phase 4: Module PoC

**Goal:** Add a second module (placeholder) to prove the shell pattern works end-to-end. The module doesn't need real functionality — just proves that adding a new module is a matter of registration + navigator.

### Prerequisites
- Phase 3 complete and verified
- Shell module loading works for YT Downloader

### Backend Tasks

**B4.1: Create placeholder microservice**

- File: `backend/src/JiApp.ImageTools/JiApp.ImageTools.csproj` (web project, TargetFramework net10.0)
- File: `backend/src/JiApp.ImageTools/Program.cs` — minimal startup
- File: `backend/src/JiApp.ImageTools/Startup.cs` — registers JWT auth, health check, one stub endpoint
- File: `backend/src/JiApp.ImageTools/Features/Placeholder/PlaceholderEndpoint.cs`
  - GET `/api/v1/imagetools/ping` — returns `{ module: "image-tools", status: "ok" }` (requires auth)
- Add to solution, add Gateway route for `/api/v1/imagetools/{**catch-all}` — `http://localhost:5003`
- File: `backend/docker-compose.yml` — add `imagetools` service on port 5003
- Acceptance: Gateway proxies `/api/v1/imagetools/ping` to the placeholder service; returns 200 with auth or 401 without

**B4.2: Add ImageTools tests**

- File: `backend/tests/JiApp.ImageTools.Tests/JiApp.ImageTools.Tests.csproj`
- Single test: `GET /api/v1/imagetools/ping` with auth token returns 200
- Acceptance: `dotnet test` includes and passes ImageTools tests

### Frontend Tasks

**F4.1: Create ImageTools module (placeholder)**

- Directory: `mobile/src/modules/image-tools/`
- File: `mobile/src/modules/image-tools/index.ts` — export `imageToolsModule: JiModule`
- File: `mobile/src/modules/image-tools/navigator.tsx`
  - Stack navigator with one screen: `PlaceholderScreen`
  - PlaceholderScreen: shows module name, icon, "Coming soon" text
- File: `mobile/src/modules/image-tools/screens/PlaceholderScreen.tsx`
  - Calls `GET /api/v1/imagetools/ping` on mount, shows "Image Tools — Connected" or "Image Tools — Offline"
- Acceptance: Placeholder screen shows connection status

**F4.2: Register ImageTools in ModuleRegistry**

- Edit `mobile/src/shell/ModuleRegistry.ts`:
  - Add entry: `id: 'image-tools', name: 'modules.imagetools', icon: 'image', component: ImageToolsNavigator, enabled: true`
- Add i18n key `modules.imagetools` — "Image Tools" (en) / "Narzędzia Obrazów" (pl)
- Acceptance: "Image Tools" tab appears alongside "YT Downloader"; tapping shows placeholder screen

**F4.3: Add feature flag mechanism**

- Edit `mobile/src/shell/ModuleRegistry.ts`:
  - Add logic to read enabled/disabled state from local storage or env (for Phase 4: mock with in-memory toggle)
  - Add function `setModuleEnabled(id: string, enabled: boolean): void` — saves to AsyncStorage
- Edit `mobile/src/screens/SettingsScreen.tsx`:
  - Add "Modules" section that lists all registered modules with toggle switches (enabled/disabled)
  - Reads state from ModuleRegistry/storage
- Acceptance: Toggling a module off in Settings removes its tab immediately; toggling on restores it

**F4.4: Verify two-module shell**

- Both modules loaded via ModuleLoader
- Tab bar shows both tabs
- Each tab opens its respective navigator
- Settings can disable/enable modules
- Disabled modules' tabs hidden from ModuleLoader
- Acceptance: Two independent modules co-exist in single shell

### Integration Tasks

- Full stack: Gateway → Identity (auth) → YtDownloader (working) + ImageTools (ping)
- API test: Register, Login, Search YT, Ping ImageTools — all via Gateway
- Docker Compose: all 4 services (gateway, identity, ytdownloader, imagetools) + postgres

### Definition of Done — Phase 4

- [ ] Second microservice (ImageTools) running with placeholder endpoint
- [ ] Gateway routes to ImageTools correctly
- [ ] Second mobile module (ImageTools) with navigator and placeholder screen
- [ ] ModuleRegistry contains both modules
- [ ] Feature flags: modules can be enabled/disabled from Settings
- [ ] Adding a third module follows the exact same pattern (no code changes to shell)
- [ ] All tests pass (backend + mobile)
- [ ] Docker Compose runs 4 services + Postgres

---

## Migration Strategy Notes

### Running Old and New Side-by-Side

During Phases 0-1, the monolith on port 5003 continues to run. The mobile app can switch between old and new backends by changing `API_BASE_URL`. This allows:

1. Test new Identity service independently (direct calls or via Gateway)
2. Test new YtDownloader service independently
3. When confident, switch mobile API_BASE_URL to Gateway
4. Once all features verified, decommission monolith

### Data Migration

When migrating from monolith SQLite to PostgreSQL:
1. Export monolith SQLite data as JSON
2. Split into per-service datasets (Users → Identity, History → YtDownloader)
3. Import into respective PostgreSQL databases
4. Run smoke tests to verify data integrity

### Rollback Plan

If any phase breaks:
1. Switch mobile `API_BASE_URL` back to monolith (port 5003)
2. Fix issue in new service
3. Switch back to Gateway
4. No data loss — monolith DB untouched during development
