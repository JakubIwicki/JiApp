# JiApp - Project Roadmap & Architecture Vision

## 1. Project Overview

JiApp is a YouTube-to-MP3 downloader consisting of a .NET 10 backend API and a React Native Android mobile application. Users can register, log in, search YouTube videos, view search/download history, and download audio as MP3 files directly to their mobile device.

This project is a full rewrite of the legacy `YtApi/` backend, reorganized as a monorepo with Vertical Slice Architecture on the backend and React Native CLI on the frontend.

### Key Decisions

- **Full rewrite** — the old `YtApi/` directory is reference-only
- **Monorepo layout** — `backend/`, `mobile/`, `docs/` at root
- **.NET 10** with Vertical Slice Architecture, Minimal APIs, FluentValidation, sealed records
- **React Native CLI** targeting Android only (no Expo)
- **i18n** — Polish (default) + English, wired in Phase 0, strings translated per-phase, settings screen in Phase 5
- **SQLite** via Entity Framework Core with ASP.NET Identity (long key)
- **YouTube integration** — Google.Apis.YouTube.v3 for search; YoutubeDLSharp + FFMpegCore for download
- **Local hosting** for now; architecture must be cloud-ready (no hard-coded paths, config-driven)

### Tech Stack

| Layer | Technology |
|-------|-----------|
| Backend Framework | ASP.NET Core (.NET 10) |
| Backend Pattern | Vertical Slice Architecture, Minimal APIs |
| Validation | FluentValidation |
| Database | SQLite via EF Core |
| Identity | ASP.NET Core Identity with JWT Bearer |
| YouTube Search | Google.Apis.YouTube.v3 |
| Media Download | YoutubeDLSharp (yt-dlp) + FFMpegCore (ffmpeg) |
| Frontend | React Native CLI (Android) |
| Navigation | React Navigation (Stack) |
| State Management | React Context + useReducer |
| HTTP Client | Axios |
| Secure Storage | react-native-encrypted-storage |
| Preferences | @react-native-async-storage/async-storage |
| i18n | i18next + react-i18next + react-native-localize |
| Testing (Backend) | xUnit, Moq, FluentAssertions |
| Storybook | @storybook/react-native (on-device, toggleable) |
| Testing (Frontend) | Jest, @testing-library/react-native, @testing-library/jest-native |

---

## 2. Architecture Overview

### 2.1 Backend (Vertical Slice Architecture)

Each feature is a self-contained folder containing:
- **Endpoint** — Minimal API route mapping
- **Handler** — Business logic
- **Validator** — FluentValidation rules
- **Request** — `public sealed record` DTO for input
- **Response** — `public sealed record` DTO for output

No controllers. Minimal APIs only. Cross-cutting concerns (auth middleware, global error handling, logging) live in the API project's infrastructure.

### 2.2 Frontend (React Native CLI)

Standard React Native CLI project targeting Android. Navigation via React Navigation (stack navigator). State management via React Context + useReducer for auth state; no Redux. API communication via Axios. Secure token storage via react-native-encrypted-storage. i18n via react-native-localize + i18next.

### 2.3 Database

SQLite via EF Core. ASP.NET Identity with `long` primary keys. Three custom tables: YoutubeSearchHistory, YoutubeDownloadHistory, EventLogs. All history tables have a foreign key to AspNetUsers with cascade delete. EventLogs have SetNull on user deletion.

---

## 3. Monorepo Structure

```
JiApp/
  PLAN.md
  PROCESS.md
  DESIGN.md
  README.md
  .gitignore
  backend/
    JiApp.sln
    src/
      JiApp.Api/
        JiApp.Api.csproj
        Program.cs
        appsettings.json
        appsettings.Development.json
        Properties/
          launchSettings.json
        Middleware/
          GlobalExceptionMiddleware.cs
          RequestLoggingMiddleware.cs
        Features/
          Auth/
            Register/
              RegisterEndpoint.cs
              RegisterHandler.cs
              RegisterValidator.cs
              RegisterRequest.cs
              RegisterResponse.cs
            Login/
              LoginEndpoint.cs
              LoginHandler.cs
              LoginValidator.cs
              LoginRequest.cs
              LoginResponse.cs
            Me/
              MeEndpoint.cs
              MeHandler.cs
              MeResponse.cs
          Search/
            SearchVideos/
              SearchVideosEndpoint.cs
              SearchVideosHandler.cs
              SearchVideosValidator.cs
              SearchVideosRequest.cs
              SearchVideosResponse.cs
            SearchHistory/
              SearchHistoryEndpoint.cs
              SearchHistoryHandler.cs
              SearchHistoryValidator.cs
              SearchHistoryRequest.cs
              SearchHistoryResponse.cs
          Downloads/
            GetDownloadLink/
              GetDownloadLinkEndpoint.cs
              GetDownloadLinkHandler.cs
              GetDownloadLinkValidator.cs
              DownloadRequest.cs
              DownloadResponse.cs
            DownloadFile/
              DownloadFileEndpoint.cs
              DownloadFileHandler.cs
            DownloadHistory/
              DownloadHistoryEndpoint.cs
              DownloadHistoryHandler.cs
              DownloadHistoryValidator.cs
              DownloadHistoryRequest.cs
              DownloadHistoryResponse.cs
          History/
            GetHistory/
              GetHistoryEndpoint.cs
              GetHistoryHandler.cs
              GetHistoryValidator.cs
              GetHistoryRequest.cs
              GetHistoryResponse.cs
      JiApp.Infrastructure/
        JiApp.Infrastructure.csproj
        Persistence/
          JiAppDbContext.cs
          Configurations/
            UserConfiguration.cs
            YoutubeSearchHistoryConfiguration.cs
            YoutubeDownloadHistoryConfiguration.cs
            EventLogConfiguration.cs
          Migrations/
        Repositories/
          ISearchHistoryRepository.cs
          SearchHistoryRepository.cs
          IDownloadHistoryRepository.cs
          DownloadHistoryRepository.cs
          IEventLogRepository.cs
          EventLogRepository.cs
        Services/
          IJwtTokenService.cs
          JwtTokenService.cs
          ITempFileStore.cs
          TempFileStore.cs
      JiApp.Common/
        JiApp.Common.csproj
        Models/
          User.cs
          YoutubeSearchHistory.cs
          YoutubeDownloadHistory.cs
          EventLog.cs
          BaseEntity.cs
        Constants/
          AppConstants.cs
          ValidationConstants.cs
        Abstractions/
          ICurrentUserService.cs
          Result.cs
      JiApp.YtApi/
        JiApp.YtApi.csproj
        IYoutubeClient.cs
        YoutubeClient.cs
        YoutubeVideo.cs
        YoutubeClientResponse.cs
        Configuration/
          YoutubeSettings.cs
    tests/
      JiApp.Tests/
        JiApp.Tests.csproj
        Features/
          Auth/
            RegisterHandlerTests.cs
            LoginHandlerTests.cs
          Search/
            SearchVideosHandlerTests.cs
          Downloads/
            GetDownloadLinkHandlerTests.cs
        Infrastructure/
          JwtTokenServiceTests.cs
  mobile/
    package.json
    tsconfig.json
    babel.config.js
    metro.config.js
    index.js
    android/
    src/
      App.tsx
      screens/
        LoginScreen.tsx
        RegisterScreen.tsx
        SearchScreen.tsx
        DownloadScreen.tsx
        HistoryScreen.tsx
        SettingsScreen.tsx
      components/
        VideoCard.tsx
        SearchBar.tsx
        HistoryItem.tsx
        LoadingSpinner.tsx
        ErrorMessage.tsx
        LanguagePicker.tsx
      services/
        apiClient.ts
        authService.ts
        storageService.ts
        downloadService.ts
        searchService.ts
      navigation/
        AppNavigator.tsx
        AuthNavigator.tsx
        MainNavigator.tsx
        types.ts
      i18n/
        index.ts
        pl.json
        en.json
      hooks/
        useAuth.ts
        useSearch.ts
        useDownload.ts
        useHistory.ts
      context/
        AuthContext.tsx
      types/
        api.ts
        navigation.ts
  docs/
    API.md
    SETUP.md
```

---

## 4. Backend Project Descriptions

### 4.1 JiApp.Api

The entry point and Minimal API host. Contains `Program.cs` with all service registrations (DI, Identity, JWT, EF Core, CORS, Swagger). Contains the `Features/` folder with all vertical slices. Each feature folder has an Endpoint file that maps a Minimal API route and delegates to the Handler. Also contains global middleware for exception handling and request logging.

**Dependencies:** JiApp.Infrastructure, JiApp.Common, JiApp.YtApi

### 4.2 JiApp.Infrastructure

Database layer. Contains `JiAppDbContext` (inherits `IdentityDbContext<User, IdentityRole<long>, long>`), all EF Core configurations, migrations, repository implementations. Also contains cross-cutting services: JwtTokenService, TempFileStore.

**Dependencies:** JiApp.Common, Microsoft.AspNetCore.Identity.EntityFrameworkCore, Microsoft.EntityFrameworkCore.Sqlite

### 4.3 JiApp.Common

Shared abstractions and models. Contains all entity classes (User, YoutubeSearchHistory, YoutubeDownloadHistory, EventLog, BaseEntity). Contains constants, validation constants, and abstractions like ICurrentUserService and a Result<T> type for handler returns.

**Dependencies:** Microsoft.AspNetCore.Identity (for IdentityUser<long> base only)

### 4.4 JiApp.YtApi

YouTube-specific integration. Wraps Google.Apis.YouTube.v3 for search and YoutubeDLSharp + FFMpegCore for MP3 download. Contains IYoutubeClient interface, YoutubeClient implementation, YoutubeVideo record, YoutubeClientResponse record, and YoutubeSettings configuration record.

**Dependencies:** JiApp.Common, Google.Apis.YouTube.v3, YoutubeDLSharp, FFMpegCore

---

## 5. Frontend Architecture

### 5.1 Navigation

- **AuthNavigator** (Stack): LoginScreen -> RegisterScreen
- **MainNavigator** (Stack): SearchScreen -> DownloadScreen, HistoryScreen, SettingsScreen
- **AppNavigator**: Switches between AuthNavigator and MainNavigator based on auth state

### 5.2 State Management

- **AuthContext** (React Context + useReducer): Holds user token, user info, isAuthenticated, isLoading. Actions: LOGIN, LOGOUT, RESTORE_TOKEN.
- No global state library; each screen manages its own local state via useState/useReducer.

### 5.3 i18n

- react-native-localize detects device locale
- i18next with react-i18next for translation hooks
- Two JSON files: `pl.json` (default), `en.json`
- All user-facing strings referenced via translation keys from day one (Phase 0 setup)
- Language switch UI in SettingsScreen (Phase 5)
- Default/fallback language: Polish

### 5.4 Storage

- **react-native-encrypted-storage**: JWT token, saved credentials (for "Remember Me")
- **AsyncStorage**: Non-sensitive preferences (language, theme)

### 5.5 API Client

- Axios instance with baseURL from environment config
- Request interceptor attaches Bearer token from encrypted storage
- Response interceptor handles 401 by clearing auth state and redirecting to login

### 5.6 Component Development with Storybook

Components are developed in isolation via `@storybook/react-native` before being wired into screens. Storybook runs on-device (or emulator), toggleable via `START_STORYBOOK=true` environment variable at the Metro bundler level.

**Configuration:**
- **Directory:** `mobile/.storybook/` containing `main.ts` (story discovery) and `preview.tsx` (global decorators, providers)
- **Story files:** Colocated with components as `ComponentName.stories.tsx`
- **Metro:** Updated resolver to pick up `.storybook/` and story files
- **Launch toggle:** `App.tsx` checks `START_STORYBOOK` env var; when set, renders Storybook UI instead of AppNavigator

**Workflow:**
1. Write stories for each component state (default, loading, disabled, error, empty)
2. Verify rendering on emulator/device via Metro hot reload
3. Wire component into target screen only after all states look correct in isolation

### 5.7 Frontend Testing

Jest is the test runner (pre-installed via `@react-native/jest-preset`). Component and hook testing uses `@testing-library/react-native` for render assertions and `@testing-library/jest-native` for extended matchers.

**Test Organization:**
- Tests colocated with components: `Button.test.tsx` next to `Button.tsx`
- Test files in `__tests__/` for hook and utility tests
- Jest config already present in `jest.config.js`; setup file at `jest.setup.ts` for matcher registration

**Coverage Strategy:**
- **Auth and core features:** Comprehensive — loading, error, edge cases, success states
- **Presentational components (LoadingSpinner, ErrorMessage, VideoCard):** Happy-path render + key prop variants
- **Hooks (useAuth, useSearch, useDownload, useHistory):** Test via `renderHook` from `@testing-library/react-native`
- `npm test` runs the full suite (already configured)

---

## 6. Database Schema

### 6.1 AspNetUsers (ASP.NET Identity, managed)

| Column | Type | Constraints |
|--------|------|-------------|
| Id | long | PK, autoincrement |
| DisplayName | TEXT | MaxLength 50, nullable |
| UserName | TEXT | MaxLength 256, nullable |
| NormalizedUserName | TEXT | MaxLength 256, unique index |
| Email | TEXT | MaxLength 256, nullable |
| NormalizedEmail | TEXT | MaxLength 256, index |
| EmailConfirmed | INTEGER | bool |
| PasswordHash | TEXT | nullable |
| SecurityStamp | TEXT | nullable |
| ConcurrencyStamp | TEXT | nullable |
| PhoneNumber | TEXT | nullable |
| PhoneNumberConfirmed | INTEGER | bool |
| TwoFactorEnabled | INTEGER | bool |
| LockoutEnd | TEXT | DateTimeOffset, nullable |
| LockoutEnabled | INTEGER | bool |
| AccessFailedCount | INTEGER | |

### 6.2 YoutubeSearchHistory

| Column | Type | Constraints |
|--------|------|-------------|
| Id | long | PK, autoincrement |
| UserId | long | FK -> AspNetUsers.Id, ON DELETE CASCADE, indexed |
| SearchedAt | DateTime | nullable |
| SearchText | TEXT | MaxLength 100 |

### 6.3 YoutubeDownloadHistory

| Column | Type | Constraints |
|--------|------|-------------|
| Id | long | PK, autoincrement |
| UserId | long | FK -> AspNetUsers.Id, ON DELETE CASCADE, indexed |
| DownloadedAt | DateTime | NOT NULL |
| VideoTitle | TEXT | MaxLength 300, nullable |
| VideoDescription | TEXT | MaxLength 1000, nullable |
| VideoId | TEXT | MaxLength 150, nullable |
| VideoUrl | TEXT | MaxLength 300, nullable |
| ImageUrl | TEXT | MaxLength 300, nullable |

### 6.4 EventLogs

| Column | Type | Constraints |
|--------|------|-------------|
| Id | long | PK, autoincrement |
| Type | INTEGER | enum (0=Exception, 1=ThirdPartyService, 2=Insider) |
| UserId | long? | FK -> AspNetUsers.Id, ON DELETE SET NULL, nullable, indexed |
| Timestamp | DateTime | nullable |
| Message | TEXT | MaxLength 50000, nullable |
| Exception | TEXT | MaxLength 20000, nullable |

### 6.5 Identity Tables (managed by ASP.NET Identity)

- AspNetRoles (Id long, Name, NormalizedName, ConcurrencyStamp)
- AspNetRoleClaims (Id int, RoleId long FK, ClaimType, ClaimValue)
- AspNetUserClaims (Id int, UserId long FK, ClaimType, ClaimValue)
- AspNetUserLogins (LoginProvider, ProviderKey PK composite, ProviderDisplayName, UserId long FK)
- AspNetUserRoles (UserId, RoleId composite PK)
- AspNetUserTokens (UserId, LoginProvider, Name composite PK, Value)

### 6.6 Relationships

- User 1:N YoutubeSearchHistory (cascade delete)
- User 1:N YoutubeDownloadHistory (cascade delete)
- User 1:N EventLogs (set null on delete)

---

## 7. API Contract

All endpoints are under `/api`. All authenticated endpoints require `Authorization: Bearer <token>` header.

### 7.1 POST /api/auth/register

- **Auth required:** No
- **Request body:**
  - username: string, required, 3-50 chars, alphanumeric + underscore
  - email: string, required, valid email format
  - password: string, required, minimum 4 chars
  - displayName: string, required, 1-50 chars
- **Success:** 201 Created (empty body)
- **Errors:** 400 `{ errors: string[] }` — validation failures, username/email taken

### 7.2 POST /api/auth/login

- **Auth required:** No
- **Request body:**
  - username: string, required
  - password: string, required
- **Success:** 200 OK `{ id: number, displayName: string, token: string }`
- **Errors:** 400 validation failures; 401 `{ message: string }` — invalid credentials

### 7.3 GET /api/auth/me

- **Auth required:** Yes
- **Request:** No body, token in header
- **Success:** 200 OK `{ id: number, displayName: string, token: string }`
- **Errors:** 401 `{ validToken: false, message: string }`

### 7.4 POST /api/search

- **Auth required:** Yes
- **Request body:**
  - query: string, required, 1-200 chars
  - maxResults: int, optional, 1-50 (default 10)
- **Success:** 200 OK `{ results: [{ videoId, title, description, imageUrl, videoUrl }] }`
- **Errors:** 400 validation; 401 unauthorized; 500 YouTube API error

### 7.5 GET /api/search/history?limit={limit}

- **Auth required:** Yes
- **Query params:** limit: int, optional, 1-50 (default 10)
- **Success:** 200 OK `{ items: [{ id, searchText, searchedAt }] }`
- **Errors:** 401 unauthorized

### 7.6 POST /api/downloads/mp3

- **Auth required:** Yes
- **Request body:**
  - videoId: string, required
  - videoUrl: string, required, must start with "https://www.youtube.com/watch?v="
  - title: string, optional, max 300 chars
  - description: string, optional, max 1000 chars
  - imageUrl: string, optional, max 300 chars
- **Success:** 200 OK `{ downloadUrl: string }`
- **Errors:** 400 validation; 401 unauthorized; 500 yt-dlp/ffmpeg error

### 7.7 GET /api/downloads/mp3/file/{id}

- **Auth required:** No (temp URL with GUID, expires after 10 minutes)
- **Path params:** id: string (GUID)
- **Success:** 200 OK, Content-Type: audio/mpeg, file stream
- **Errors:** 404 file expired or not found

### 7.8 GET /api/downloads/history?limit={limit}

- **Auth required:** Yes
- **Query params:** limit: int, optional, 1-50 (default 10)
- **Success:** 200 OK `{ items: [{ id, videoTitle, videoDescription, videoId, videoUrl, imageUrl, downloadedAt }] }`
- **Errors:** 401 unauthorized

### 7.9 GET /api/history?limit={limit}

- **Auth required:** Yes
- **Query params:** limit: int, optional, 1-50 (default 10)
- **Success:** 200 OK `{ searches: [...], downloads: [...] }`
- **Errors:** 401 unauthorized

### 7.10 GET /api/health

- **Auth required:** No
- **Success:** 200 OK `{ status: "healthy", timestamp: "ISO 8601" }`

---

## 8. Phase Roadmap

| Phase | Name | Goal | Effort |
|-------|------|------|--------|
| 0 | Scaffolding | Monorepo setup, all projects created, RN init, i18n skeleton, health endpoint | 2-3 days |
| 1 | Authentication | Register, login, JWT, secure token storage on mobile | 4-5 days |
| 2 | YouTube Search | Search YouTube, display results with thumbnails, save search history | 3-4 days |
| 3 | MP3 Download | Download videos as MP3, save to device, track download history | 4-5 days |
| 4 | History | Combined history view for searches and downloads | 2-3 days |
| 5 | Settings & Polish | Language settings, UI polish, error handling consistency | 2-3 days |
| 6 | Testing & Release | Unit tests, integration tests, manual test plan, signed APK | 3-4 days |

**Total estimated effort: ~20-27 days**

### Phase Dependencies

```
Phase 0 (Foundation)
  └── Phase 1 (Auth)
        └── Phase 2 (Search)
              └── Phase 3 (Download)
                    └── Phase 4 (History)
                          └── Phase 5 (Polish)
                                └── Phase 6 (Testing)
```

Each phase builds on the previous. No phase can start until its predecessor is complete.

### Phase 6 Frontend Testing Scope

- All component stories render without errors (Storybook audit)
- Jest suite passes with coverage for auth, search, and download logic
- `npm test` returns zero failures
- Manual test plan includes frontend verification on emulator/device

---

## 9. Non-Functional Requirements

### 9.1 Security

- All passwords hashed via ASP.NET Identity (PBKDF2)
- JWT tokens with HMAC-SHA256 signing, 30-minute expiry
- All YouTube/Download endpoints require valid JWT
- Temp download URLs use GUIDs, expire after 10 minutes, auto-cleanup
- No API keys or secrets in source code; all via appsettings.json / environment variables / user-secrets
- CORS restricted to known origins in production
- Input validation on every endpoint via FluentValidation
- Mobile: JWT stored in encrypted storage, not AsyncStorage

### 9.2 Performance

- SQLite is sufficient for single-user/low-concurrency local deployment
- Download files cleaned up via TempFileStore expiry (10-minute window)
- YouTube search results capped at 50 to avoid API quota exhaustion
- yt-dlp process runs async; API does not block thread pool

### 9.3 Cloud Readiness

- All file paths, connection strings, and external tool paths are configuration-driven (appsettings.json)
- No hard-coded Windows paths; use Path.Combine and environment-aware configuration
- TempFileStore could be replaced with blob storage interface
- DbContext can be swapped to PostgreSQL/SQL Server by changing provider
- CORS, JWT issuer/audience, and Kestrel endpoints all config-driven
- Structured logging ready for Serilog/Application Insights swap
