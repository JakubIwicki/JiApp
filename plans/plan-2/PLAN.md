# JiApp - Microservices Architecture & Multi-Module Platform

## 1. Project Overview

JiApp is evolving from a single-purpose YouTube MP3 downloader into a **Multi-Module Utility Hub**. The mobile app becomes a "Shell" that loads independent modules (YT Downloader, future: Image Tools, PDF Suite, etc.), and the backend decomposes from a monolith into C# microservices behind an API Gateway with a standalone Identity Server.

This plan builds on the completed Phase 0-6 monolith (see `plans/plan-1/`) and migrates it to the new architecture without breaking existing functionality.

### Key Decisions

- **Microservices** — each module gets its own backend service, database, and Docker container
- **API Gateway (YARP)** — single entry point for the mobile app; routes to services by URL prefix
- **Custom Identity Server** — ASP.NET Identity + JWT access tokens + opaque refresh tokens (no Duende — overkill for solo dev)
- **Database-per-service** — PostgreSQL for production, SQLite for dev, EF Core provider swap
- **Mobile Shell** — module registry + dynamic navigator loading; existing YT Downloader becomes the first module
- **Docker Compose** — local multi-service orchestration; no Kubernetes yet
- **Refresh token rotation** — replaces the current credential-based silent re-login
- **.NET 10** for all services (consistent with existing codebase target)

### Target Architecture

```
Mobile Shell (React Native)
  |
  v  HTTPS (Bearer JWT)
API Gateway (YARP, port 5000)
  |-- /api/auth/*      --> JiApp.Identity     (port 5001)
  |-- /api/yt/*        --> JiApp.YtDownloader  (port 5002)
  |-- /api/pdf/*       --> JiApp.PdfSuite      (port 5003)  [future]
  |
  v
JiApp.Identity  ──> JiApp_Identity DB (PostgreSQL)
JiApp.YtDownloader ──> JiApp_YtDownloader DB (PostgreSQL)
JiApp.PdfSuite ──> JiApp_PdfSuite DB (PostgreSQL)
```

---

## 2. Technology Stack

| Layer | Technology | Rationale |
|-------|-----------|-----------|
| Backend Framework | ASP.NET Core (.NET 10) | Consistent with existing codebase |
| API Gateway | YARP (Microsoft.ReverseProxy) | Lightweight, high-perf, Microsoft-backed. Ocelot adds unnecessary middleware overhead. |
| Identity | Custom: ASP.NET Identity + JWT + Refresh Tokens | Duende requires commercial license. Custom is sufficient for <100 users. |
| Database (prod) | PostgreSQL | Standard for .NET + EF Core, supports concurrency |
| Database (dev) | SQLite | Fast iteration, no setup |
| Containerization | Docker Compose | Local multi-service orchestration, no K8s complexity |
| Mobile Shell | React Navigation dynamic navigator | Module registry pattern — simple, typed, sufficient |
| Token Storage | react-native-encrypted-storage | Already in use for JWT; extended for refresh tokens |
| Service Comms | HTTP via Gateway (no gRPC needed) | No inter-service calls; gateway is the only client |

---

## 3. Mobile Architecture (The Shell)

### 3.1 Module Interface

Every module must implement this contract:

```typescript
// src/shell/types.ts
export interface JiModule {
  id: string;                       // unique identifier, e.g. 'yt-downloader'
  name: string;                     // i18n translation key
  icon: string;                     // icon name for TabIcon component
  navigator: React.ComponentType;   // module's stack/tab navigator
  enabled: boolean;                 // feature flag
}
```

### 3.2 Module Registry

```typescript
// src/shell/ModuleRegistry.ts
import type { JiModule } from './types';

export const moduleRegistry: JiModule[] = [
  {
    id: 'yt-downloader',
    name: 'modules.ytdownloader',
    icon: 'youtube',
    navigator: require('../modules/yt-downloader/navigator').default,
    enabled: true,
  },
  // Future modules register here
];
```

### 3.3 Shell Navigation Structure

```
AppNavigator
  ├── AuthNavigator (Login, Register)         [when not authenticated]
  └── ShellNavigator (when authenticated)
        ├── ModuleLoader (bottom tabs)
        │     ├── Tab: yt-downloader → YtNavigator (Search, Download, Downloads, History)
        │     ├── Tab: pdf-suite → PdfNavigator [future]
        │     └── Tab: [other modules]
        └── SettingsScreen (shell-level, not a module)
```

### 3.4 Mobile Directory Restructuring

```
mobile/src/
  shell/                              # NEW: Shell infrastructure
    ModuleRegistry.ts                 # Module definitions
    ModuleLoader.tsx                  # Dynamic tab navigator
    ShellNavigator.tsx                # Top-level: ModuleLoader + Settings
    types.ts                          # JiModule interface

  modules/                            # NEW: Module containers
    yt-downloader/                    # YouTube Downloader module
      index.ts                        # Module definition export
      navigator.tsx                   # Stack navigator (Search, Download, Downloads, History)
      screens/                        # Moved from src/screens/
        SearchScreen.tsx
        DownloadScreen.tsx
        DownloadsScreen.tsx
        HistoryScreen.tsx
      hooks/                          # Moved from src/hooks/
        useSearch.ts
        useDownload.ts
        useHistory.ts
        usePreview.ts
      services/                       # Moved from src/services/
        searchService.ts
        downloadService.ts
        historyService.ts
        previewService.ts
      types/                          # Module-specific types
        api.ts

  context/                            # Unchanged location
    AuthContext.tsx                    # Updated: refresh token flow
    ToastContext.tsx                   # Unchanged

  services/                           # Shared (shell-level) services
    apiClient.ts                      # Updated: refresh token interceptor
    authService.ts                    # Updated: /auth/refresh endpoint
    storageService.ts                 # Updated: refresh token storage

  navigation/
    AppNavigator.tsx                  # Simplified: Auth or Shell
    AuthNavigator.tsx                 # Unchanged

  components/                         # Shared components (unchanged location)
  styles/                             # Shared theme (unchanged)
  i18n/                               # Unchanged
  config.ts                           # Updated: gateway URL
```

---

## 4. Backend Architecture

### 4.1 Solution Structure

```
backend/
  JiApp.sln

  src/
    JiApp.Common/                     # Shared library (Result<T>, BaseEntity, constants, ApiErrorResponse)
    JiApp.YtApi/                      # YouTube client library (unchanged)

    JiApp.Identity/                   # NEW: Standalone auth service
      Program.cs
      Startup.cs
      Configuration/
        IdentitySettings.cs
      Persistence/
        IdentityDbContext.cs          # Users + RefreshTokens only
        Configurations/
        Migrations/
      Features/
        Login/                        # Adapted from JiApp.Api
        Register/                     # Adapted from JiApp.Api
        Refresh/                      # NEW: token refresh with rotation
        Logout/                       # NEW: refresh token revocation
        Me/                           # Adapted from JiApp.Api
      Services/
        IJwtTokenService.cs
        JwtTokenService.cs
        IRefreshTokenService.cs
        RefreshTokenService.cs
      Models/
        User.cs
        RefreshToken.cs
      Dockerfile

    JiApp.YtDownloader/               # NEW: YouTube microservice (extracted from JiApp.Api)
      Program.cs
      Startup.cs
      Persistence/
        YtDbContext.cs                # SearchHistory, DownloadHistory, EventLog only
        Configurations/
        Migrations/
      Repositories/                   # Moved from JiApp.Infrastructure
      Features/
        Search/                       # Moved from JiApp.Api
        Downloads/                    # Moved from JiApp.Api
        History/                      # Moved from JiApp.Api
        Preview/                      # Moved from JiApp.Api
      Services/
        TempFileStore.cs              # Moved from JiApp.Infrastructure
        CurrentUserService.cs         # Moved from JiApp.Api
      Dockerfile

    JiApp.Gateway/                    # NEW: YARP API Gateway
      Program.cs
      appsettings.json                # Route + cluster config
      Dockerfile

  tests/
    JiApp.Tests/                      # Adapted to reference new projects
    JiApp.Identity.Tests/             # NEW
    JiApp.YtDownloader.Tests/         # NEW

  docker-compose.yml                  # NEW
  docker-compose.dev.yml              # NEW: dev overrides
```

### 4.2 Decomposition of Existing JiApp.Api/Startup.cs

The current `Startup.cs` (~185 lines) registers everything. It decomposes as:

| Current Registration | Moves To |
|---------------------|----------|
| `AddDbContext<JiAppDbContext>` (Identity tables) | JiApp.Identity |
| `AddIdentity<User, IdentityRole<long>>` | JiApp.Identity |
| `AddAuthentication(JwtBearer)` | Both services (Identity issues, YtDownloader validates) |
| `IJwtTokenService` | JiApp.Identity |
| `Register/Login/Me` endpoints | JiApp.Identity |
| `IYoutubeClient` + `YoutubeSettings` | JiApp.YtDownloader |
| `ISearchHistoryRepository`, `IDownloadHistoryRepository`, `IEventLogRepository` | JiApp.YtDownloader |
| `ITempFileStore` | JiApp.YtDownloader |
| `ICurrentUserService` | Both services |
| Search/Download/History/Preview endpoints | JiApp.YtDownloader |
| Rate limiting policies | JiApp.Gateway |
| CORS | JiApp.Gateway |
| Serilog | Each service independently |

### 4.3 Database Schema Split

**JiApp_Identity database:**
- AspNetUsers (Id, DisplayName, UserName, Email, PasswordHash, ...)
- AspNetRoles, AspNetUserRoles, AspNetUserClaims, AspNetUserLogins, AspNetUserTokens, AspNetRoleClaims
- RefreshTokens (Id, Token, UserId FK, ExpiresAt, CreatedAt, IsRevoked)

**JiApp_YtDownloader database:**
- YoutubeSearchHistory (Id, UserId, SearchedAt, SearchText, IsArchived)
- YoutubeDownloadHistory (Id, UserId, DownloadedAt, VideoTitle, VideoDescription, VideoId, VideoUrl, ImageUrl, IsArchived)
- EventLogs (Id, Type, UserId, Timestamp, Message, Exception)

Note: `UserId` in YtDownloader is a plain `long` (not a FK to Identity tables). The Identity Server owns user data; other services only store the userId for attribution.

### 4.4 Docker Compose

```yaml
services:
  postgres:
    image: postgres:16
    ports: ["5432:5432"]
    environment:
      POSTGRES_MULTIPLE_DATABASES: jiapp_identity,jiapp_ytdownloader
    volumes: [pgdata:/var/lib/postgresql/data]

  identity:
    build: ./src/JiApp.Identity
    ports: ["5001:5001"]
    depends_on: [postgres]

  ytdownloader:
    build: ./src/JiApp.YtDownloader
    ports: ["5002:5002"]
    depends_on: [postgres]
    # Needs yt-dlp + ffmpeg in container

  gateway:
    build: ./src/JiApp.Gateway
    ports: ["5000:5000"]
    depends_on: [identity, ytdownloader]

volumes:
  pgdata:
```

---

## 5. Security Model

### 5.1 Token Types

| Token | Format | Lifetime | Storage |
|-------|--------|----------|---------|
| Access Token | JWT (HMAC-SHA256 or RSA) | 15 min | EncryptedStorage |
| Refresh Token | Opaque 64-byte random string | 7 days | EncryptedStorage |

### 5.2 OAuth2/OIDC Flow

```
1. LOGIN
   Mobile ──POST /api/auth/login──> Gateway ──> Identity Server
   Identity validates credentials, generates:
     - JWT access token (15 min)
     - Refresh token (7 days, stored hashed in DB)
   Returns: { accessToken, refreshToken, expiresIn, userId, displayName }

2. API CALL
   Mobile ──GET /api/yt/search (Bearer JWT)──> Gateway
   Gateway validates JWT, extracts claims, forwards:
     X-User-Id: 123
     X-Username: jakub
   to YtDownloader service

3. TOKEN REFRESH
   Mobile ──POST /api/auth/refresh { refreshToken }──> Gateway ──> Identity
   Identity validates refresh token (exists, not expired, not revoked)
   Issues new access token + new refresh token (rotation)
   Revokes old refresh token
   Returns: { accessToken, refreshToken, expiresIn }

4. LOGOUT
   Mobile ──POST /api/auth/logout { refreshToken }──> Gateway ──> Identity
   Identity revokes refresh token
   Mobile clears local storage
```

### 5.3 Gateway JWT Validation

```csharp
// JiApp.Gateway validates JWT before forwarding
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.Authority = "http://identity:5001";
        options.Audience = "jiapp-gateway";
    });
```

### 5.4 Service Trust Model

- **Dev**: YtDownloader trusts `X-User-Id`/`X-Username` headers from Gateway (no JWT validation)
- **Prod**: YtDownloader also validates JWT independently (defense-in-depth)

### 5.5 Mobile Token Refresh Interceptor

Replace current credential-based silent re-login with refresh token flow:

```typescript
// apiClient.ts response interceptor (simplified)
if (error.response?.status === 401 && !config._isRetry) {
  const refreshToken = await getRefreshToken();
  if (refreshToken) {
    try {
      const { accessToken, refreshToken: newRefresh } =
        await authService.refreshToken(refreshToken);
      await saveToken(accessToken);
      await saveRefreshToken(newRefresh);
      config._isRetry = true;
      config.headers.Authorization = `Bearer ${accessToken}`;
      return apiClient.request(config);
    } catch { /* refresh failed */ }
  }
  await clearAllAuth();
  // redirect to login
}
```

---

## 6. API Contract (via Gateway)

All requests go through the Gateway at `https://{host}:5000/api/v1/`.

### Identity Endpoints (routed to JiApp.Identity)

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | /api/v1/auth/register | No | Register new user |
| POST | /api/v1/auth/login | No | Login, returns access + refresh tokens |
| POST | /api/v1/auth/refresh | No | Refresh token rotation |
| POST | /api/v1/auth/logout | No | Revoke refresh token |
| GET | /api/v1/auth/me | Yes | Get current user info |

### YT Downloader Endpoints (routed to JiApp.YtDownloader)

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | /api/v1/yt/search | Yes | Search YouTube videos |
| GET | /api/v1/yt/search/history | Yes | Get search history |
| DELETE | /api/v1/yt/search/{id} | Yes | Archive search entry |
| POST | /api/v1/yt/downloads/mp3 | Yes | Request MP3 download |
| GET | /api/v1/yt/downloads/mp3/file/{id} | No | Stream MP3 file (temp URL) |
| GET | /api/v1/yt/downloads/history | Yes | Get download history |
| DELETE | /api/v1/yt/downloads/{id} | Yes | Archive download entry |
| GET | /api/v1/yt/history | Yes | Combined search + download history |
| GET | /api/v1/yt/preview/{videoId} | Yes | Stream audio preview |

### Response Changes

Login response changes from:
```json
{ "id": 123, "displayName": "Jakub", "token": "eyJ..." }
```
to:
```json
{
  "userId": 123,
  "displayName": "Jakub",
  "accessToken": "eyJ...",
  "refreshToken": "a1b2c3...",
  "expiresIn": 900
}
```

---

## 7. Phase Roadmap

| Phase | Name | Goal | Effort |
|-------|------|------|--------|
| 0 | Foundation | Docker Compose, Identity Server, Gateway skeleton, mobile shell structure | 4-5 days |
| 1 | Service Extraction | Extract YT Downloader into standalone microservice, migrate mobile to refresh tokens | 4-5 days |
| 2 | Mobile Shell | Implement module loader, dynamic navigation, settings at shell level | 2-3 days |
| 3 | Hardening | Health checks, structured logging, error handling, rate limiting at gateway | 2-3 days |
| 4 | Module PoC | Add a second module (placeholder) to prove the pattern works end-to-end | 1-2 days |

**Total estimated effort: ~13-18 days**

### Phase Dependencies

```
Phase 0 (Foundation)
  ├── Phase 1 (Service Extraction)
  │     └── Phase 2 (Mobile Shell)
  │           └── Phase 3 (Hardening)
  │                 └── Phase 4 (Module PoC)
```

---

## 8. Migration Risk Assessment

| Risk | Impact | Mitigation |
|------|--------|------------|
| Breaking existing mobile app | High | Keep monolith running until mobile is fully migrated. Test each phase independently. |
| Token refresh complexity | High | Implement carefully with rotation. Test: expired refresh, concurrent refresh, network failure. |
| yt-dlp/ffmpeg in Docker | Medium | Use base image with these tools pre-installed. Test download flow in container before switching. |
| Data migration (SQLite → PostgreSQL) | Medium | Write migration script. Keep old DB as backup. |
| Gateway SPOF | Low | YARP is battle-tested. Acceptable for local dev. |
| Module loading startup time | Low | Navigators are lazy-loaded. Registry is synchronous but lightweight. |
