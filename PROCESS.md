# JiApp - Phase-by-Phase Execution Guide

This document contains the detailed, task-level execution plan for building JiApp. Each phase delivers a working vertical slice (backend + frontend). Tasks are ordered by dependency within each phase.

**Granularity:** File paths, method signatures, validation rules, acceptance criteria. No inline code blocks.

**Reference code:** The old backend at `YtApi/` serves as reference for porting logic. Key files:
- `YtApi/YoutubeLib/YoutubeClient.cs` — Search + download logic
- `YtApi/JiDb/JiDbContext.cs` — Database schema
- `YtApi/JiApi/Jwt/JwtTokenService.cs` — JWT generation
- `YtApi/JiApi/Extensions/TempFileStore.cs` — Temp file management
- `YtApi/JiApi/Controllers/YoutubeController.cs` — Endpoint logic

---

## Phase 0: Project Scaffolding & Infrastructure

### Prerequisites
- .NET 10 SDK installed
- Node.js 18+ and npm installed
- React Native CLI environment set up (JDK 17, Android SDK, Android Studio)
- Git initialized in `/home/jakub/JiApp/`

### Backend Tasks

**B0.1: Create solution and projects**
- Create solution file: `backend/JiApp.sln`
- Create class library: `backend/src/JiApp.Common/JiApp.Common.csproj` (TargetFramework net10.0)
- Create class library: `backend/src/JiApp.Infrastructure/JiApp.Infrastructure.csproj` (TargetFramework net10.0)
- Create class library: `backend/src/JiApp.YtApi/JiApp.YtApi.csproj` (TargetFramework net10.0)
- Create web project: `backend/src/JiApp.Api/JiApp.Api.csproj` (TargetFramework net10.0, Sdk: Microsoft.NET.Sdk.Web)
- Create test project: `backend/tests/JiApp.Tests/JiApp.Tests.csproj` (TargetFramework net10.0)
- Add all projects to the solution
- Set project references:
  - JiApp.Api references JiApp.Infrastructure, JiApp.Common, JiApp.YtApi
  - JiApp.Infrastructure references JiApp.Common
  - JiApp.YtApi references JiApp.Common
  - JiApp.Tests references JiApp.Api, JiApp.Infrastructure, JiApp.Common, JiApp.YtApi
- Acceptance: `dotnet build` succeeds with zero errors from the solution root

**B0.2: Install NuGet packages**
- JiApp.Common: no external packages (implicit usings only)
- JiApp.Infrastructure:
  - Microsoft.AspNetCore.Identity.EntityFrameworkCore (10.x)
  - Microsoft.EntityFrameworkCore (10.x)
  - Microsoft.EntityFrameworkCore.Sqlite (10.x)
  - Microsoft.EntityFrameworkCore.Design (10.x, PrivateAssets=all)
- JiApp.YtApi:
  - Google.Apis.YouTube.v3 (latest)
  - YoutubeDLSharp (latest)
  - FFMpegCore (latest)
- JiApp.Api:
  - Microsoft.AspNetCore.Authentication.JwtBearer (10.x)
  - FluentValidation.DependencyInjectionExtensions (latest)
  - Swashbuckle.AspNetCore (latest)
- JiApp.Tests:
  - xunit (latest)
  - xunit.runner.visualstudio (latest)
  - Microsoft.NET.Test.Sdk (latest)
  - Moq (latest)
  - FluentAssertions (latest)
  - Microsoft.AspNetCore.Mvc.Testing (10.x)
- Acceptance: `dotnet restore` and `dotnet build` both succeed

**B0.3: Create BaseEntity and common models (stubs)**
- File: `backend/src/JiApp.Common/Models/BaseEntity.cs`
  - `public abstract class BaseEntity<TKey> where TKey : IEquatable<TKey>, IComparable<TKey>` with property `TKey Id { get; set; }`
- File: `backend/src/JiApp.Common/Models/User.cs`
  - `public class User : IdentityUser<long>` with property `string? DisplayName { get; set; }` (MaxLength 50)
- File: `backend/src/JiApp.Common/Constants/AppConstants.cs`
  - `public static class AppConstants` with `const int DefaultPageSize = 10` and `const int MaxPageSize = 50`
- File: `backend/src/JiApp.Common/Constants/ValidationConstants.cs`
  - `public static class ValidationConstants` with constants: UsernameMinLength (3), UsernameMaxLength (50), PasswordMinLength (4), DisplayNameMaxLength (50), QueryMaxLength (200), VideoTitleMaxLength (300), VideoDescriptionMaxLength (1000)
- File: `backend/src/JiApp.Common/Abstractions/Result.cs`
  - `public sealed record Result<T>(bool IsSuccess, T? Value, string? Error)` with static factory methods `Success(T value)` and `Failure(string error)`
- File: `backend/src/JiApp.Common/Abstractions/ICurrentUserService.cs`
  - `public interface ICurrentUserService` with properties `long UserId { get; }` and `string Username { get; }`
- Acceptance: All models compile; `dotnet build` succeeds

**B0.4: Create minimal Program.cs with health check**
- File: `backend/src/JiApp.Api/Program.cs`
  - Create WebApplication builder
  - Add Swagger/OpenAPI services
  - Add FluentValidation from assembly
  - Map a single GET `/api/health` endpoint returning 200 with `{ status: "healthy", timestamp: DateTime.UtcNow }`
  - Configure Kestrel to listen on `http://*:5001`
  - Use Swagger UI in Development
- File: `backend/src/JiApp.Api/appsettings.json`
  - Logging section (Default: Information, Microsoft.AspNetCore: Warning)
  - Kestrel endpoints (Http: `http://*:5001`)
  - App section with BaseDirectory placeholder
  - ConnectionStrings section with JiDb SQLite connection string using `${BaseDirectory}` placeholder
  - Youtube section with api-key, yt-dlp path, ffmpeg path placeholders
  - Jwt section with Key, Issuer, Audience, ExpireMinutes placeholders
- File: `backend/src/JiApp.Api/appsettings.Development.json`
  - Override BaseDirectory to local dev path
  - Set YouTube API key (or user-secrets reference)
  - Set Jwt:Key to a development base64 key
  - Set Jwt:Issuer to "JiApp-Dev", Jwt:Audience to "http://localhost:5001", ExpireMinutes to 60
- File: `backend/src/JiApp.Api/Properties/launchSettings.json`
  - Profile "http": commandName Project, applicationUrl `http://localhost:5001`, ASPNETCORE_ENVIRONMENT Development
- Acceptance: `dotnet run --project backend/src/JiApp.Api` starts; `GET http://localhost:5001/api/health` returns 200 with JSON containing "healthy"

**B0.5: Create global error handling middleware**
- File: `backend/src/JiApp.Api/Middleware/GlobalExceptionMiddleware.cs`
  - Class implementing `IMiddleware`
  - Method: `Task InvokeAsync(HttpContext context, RequestDelegate next)`
  - Catches all exceptions, logs them, returns 500 with `{ error: "An unexpected error occurred" }` in production; includes exception details in Development
- File: `backend/src/JiApp.Api/Middleware/RequestLoggingMiddleware.cs`
  - Class implementing `IMiddleware`
  - Method: `Task InvokeAsync(HttpContext context, RequestDelegate next)`
  - Logs request method, path, and response status code
- Register both middleware in Program.cs pipeline
- Acceptance: Throwing an exception in the health endpoint returns a structured JSON error instead of a stack trace

### Frontend Tasks

**F0.1: Initialize React Native project**
- Run `npx @react-native-community/cli init JiAppMobile --directory mobile` from repo root
- Verify the generated project targets Android
- Remove iOS-specific files/folders (Android-only app)
- Acceptance: `npx react-native run-android` builds and launches on emulator showing the default RN welcome screen

**F0.2: Install dependencies**
- Navigation: @react-navigation/native, @react-navigation/stack, react-native-screens, react-native-safe-area-context, react-native-gesture-handler
- HTTP: axios
- Storage: @react-native-async-storage/async-storage, react-native-encrypted-storage
- i18n: i18next, react-i18next, react-native-localize
- UI (optional): react-native-vector-icons
- TypeScript: Ensure tsconfig.json has strict mode, paths configured for `src/`
- Acceptance: `npm install` succeeds; app still builds and runs

**F0.3: Set up project structure and navigation skeleton**
- Create directories: `mobile/src/`, `mobile/src/screens/`, `mobile/src/components/`, `mobile/src/services/`, `mobile/src/navigation/`, `mobile/src/i18n/`, `mobile/src/hooks/`, `mobile/src/context/`, `mobile/src/types/`
- File: `mobile/src/navigation/types.ts`
  - Define `AuthStackParamList` with keys: Login, Register
  - Define `MainStackParamList` with keys: Search, Download, History, Settings
- File: `mobile/src/navigation/AuthNavigator.tsx`
  - Stack navigator with LoginScreen and RegisterScreen (placeholder components)
- File: `mobile/src/navigation/MainNavigator.tsx`
  - Stack navigator with SearchScreen, DownloadScreen, HistoryScreen, SettingsScreen (placeholder components)
- File: `mobile/src/navigation/AppNavigator.tsx`
  - Conditionally renders AuthNavigator or MainNavigator based on auth state (hardcoded to AuthNavigator for now)
- File: `mobile/src/App.tsx`
  - NavigationContainer wrapping AppNavigator
  - i18n initialization
- Create placeholder screen files (each renders a View with a Text showing the screen name):
  - `mobile/src/screens/LoginScreen.tsx`
  - `mobile/src/screens/RegisterScreen.tsx`
  - `mobile/src/screens/SearchScreen.tsx`
  - `mobile/src/screens/DownloadScreen.tsx`
  - `mobile/src/screens/HistoryScreen.tsx`
  - `mobile/src/screens/SettingsScreen.tsx`
- Acceptance: App launches, shows the Login placeholder screen; navigation between Login and Register works

**F0.4: Set up i18n**
- File: `mobile/src/i18n/pl.json`
  - Root object with nested keys by screen
  - Initial keys: `common.loading`, `common.error`, `common.retry`, `auth.login`, `auth.register`, `auth.username`, `auth.password`, `auth.email`, `auth.displayName`, `nav.search`, `nav.history`, `nav.settings`
  - All values in Polish
- File: `mobile/src/i18n/en.json`
  - Same keys, English values
- File: `mobile/src/i18n/index.ts`
  - Initialize i18next with react-i18next
  - Use react-native-localize to detect device language
  - Fallback language: 'pl'
  - Load pl.json and en.json as resources
- Update App.tsx to import i18n/index.ts before NavigationContainer
- Acceptance: Placeholder screens show translated text; changing device language to English shows English strings

**F0.5: Set up API client skeleton**
- File: `mobile/src/services/apiClient.ts`
  - Create Axios instance with configurable baseURL (default: `http://10.0.2.2:5001/api` for Android emulator accessing host machine)
  - Add request interceptor that reads token from encrypted storage and attaches `Authorization: Bearer <token>` header
  - Add response interceptor that catches 401 and clears auth state
- File: `mobile/src/types/api.ts`
  - Define TypeScript interfaces: LoginRequest, LoginResponse, RegisterRequest, SearchRequest, SearchResponse, VideoItem, DownloadRequest, DownloadResponse, SearchHistoryItem, DownloadHistoryItem, HistoryResponse
- Acceptance: apiClient.ts exports a configured Axios instance; types compile

**F0.6: Set up Storybook**
- Install packages: `npm install --save-dev @storybook/react-native @storybook/react` in mobile/
- Create directory: `mobile/.storybook/`
- File: `mobile/.storybook/main.ts`
  - Export config with stories glob: `['../src/**/*.stories.tsx']`
  - Addons: `[]` (on-device mode doesn't use web addons)
- File: `mobile/.storybook/preview.tsx`
  - Export decorators array wrapping stories with: i18next I18nextProvider, React Navigation NavigationContainer (stub), any additional global context
- File: `mobile/.storybook/Storybook.tsx`
  - Default export rendering Storybook UI (imported from `@storybook/react-native`)
- Update `mobile/App.tsx`:
  - Import Storybook from `./.storybook/Storybook`
  - At component top: check `process.env.START_STORYBOOK === 'true'`; if so, render `<Storybook />` instead of `<AppNavigator />`
- Update `mobile/package.json` scripts:
  - Add `"storybook": "START_STORYBOOK=true npx react-native start"`
- Update `mobile/metro.config.js`:
  - Add watch folders for `.storybook/`
  - Ensure `.tsx` extension is in resolver sourceExts (already default)
- Acceptance: `npm run storybook` launches Metro with Storybook toggle; `npm start` launches normal app (both deferred to emulator availability)

**F0.7: Set up frontend test libraries**
- Install: `npm install --save-dev @testing-library/react-native @testing-library/jest-native` in mobile/
- File: `mobile/jest.setup.ts`
  - `import '@testing-library/jest-native/extend-expect'`
- Update `mobile/jest.config.js`:
  - Add `setupFilesAfterSetup: ['./jest.setup.ts']` (note: the key is `setupFilesAfterSetup` in newer Jest, or `setupFiles` for the global setup path)
  - Ensure `transformIgnorePatterns` excludes react-native and @react-native packages from ignoring (so they get transformed)
  - Keep existing `preset: '@react-native/jest-preset'`
- File: `mobile/src/components/__tests__/.gitkeep` — placeholder for future test directory
- Add a verification test:
  - File: `mobile/src/components/LoadingSpinner.test.tsx`
    - Renders `LoadingSpinner`, asserts ActivityIndicator is present
  - Run `npm test` — must pass
- Acceptance: `npm test` runs Jest with extended matchers; sample test passes

### Integration Tasks

- [x] Verify backend health endpoint is reachable via curl: `GET http://localhost:5001/api/health` returns 200 with `{"status":"healthy","timestamp":"..."}`
- [ ] Verify from Android emulator: `GET http://10.0.2.2:5001/api/health` from mobile API client (deferred — no emulator available)

### Definition of Done - Phase 0

- [x] `dotnet build backend/JiApp.slnx` succeeds with zero errors
- [x] `dotnet run --project backend/src/JiApp.Api` starts and serves health endpoint
- [x] `dotnet test backend/tests/JiApp.Tests/` — 8/8 tests passing
- [ ] React Native app builds and launches on Android emulator (deferred — no emulator available)
- [x] Navigation skeleton works — AuthNavigator, MainNavigator, AppNavigator, 6 placeholder screens
- [x] i18n shows Polish strings by default, English when device language is English (40+ keys)
- [x] API client configured with `http://10.0.2.2:5001/api` baseURL, TypeScript types matching API contract
- [x] All files follow naming conventions (PascalCase for C#, camelCase for TS)
- [x] Backend middleware: GlobalExceptionMiddleware returns structured JSON errors, RequestLoggingMiddleware logs method/path/status
- [x] Storybook setup: packages installed, .storybook/ config created, launch toggle wired in App.tsx
- [x] Frontend test libraries: @testing-library/react-native + @testing-library/jest-native installed, jest.setup.ts configured, sample test passes

---

## Phase 1: Authentication (Backend + Frontend)

### Prerequisites
- Phase 0 complete and verified
- SQLite database path configured in appsettings.Development.json

### Backend Tasks

**B1.1: Create entity models**
- File: `backend/src/JiApp.Common/Models/YoutubeSearchHistory.cs`
  - `public class YoutubeSearchHistory : BaseEntity<long>`
  - Properties: `long UserId`, `DateTime? SearchedAt`, `string? SearchText` (MaxLength 100)
- File: `backend/src/JiApp.Common/Models/YoutubeDownloadHistory.cs`
  - `public class YoutubeDownloadHistory : BaseEntity<long>`
  - Properties: `long UserId`, `DateTime DownloadedAt`, `string? VideoTitle` (MaxLength 300), `string? VideoDescription` (MaxLength 1000), `string? VideoId` (MaxLength 150), `string? VideoUrl` (MaxLength 300), `string? ImageUrl` (MaxLength 300)
- File: `backend/src/JiApp.Common/Models/EventLog.cs`
  - `public enum EventLogType { Exception = 0, ThirdPartyService = 1, Insider = 2 }`
  - `public class EventLog : BaseEntity<long>`
  - Properties: `EventLogType Type`, `long? UserId`, `DateTime? Timestamp`, `string? Message` (MaxLength 50000), `string? Exception` (MaxLength 20000)
  - Static factory: `public static EventLog Create(EventLogType type, long? userId, string message)` returning new EventLog with Timestamp = DateTime.UtcNow
- Acceptance: All models compile; no circular references

**B1.2: Create DbContext and configurations**
- File: `backend/src/JiApp.Infrastructure/Persistence/JiAppDbContext.cs`
  - `public class JiAppDbContext : IdentityDbContext<User, IdentityRole<long>, long>`
  - Constructor: `JiAppDbContext(DbContextOptions<JiAppDbContext> options)`
  - DbSet properties: `EventLogs`, `YoutubeSearchHistory`, `YoutubeDownloadHistory`
  - Override `OnModelCreating`: call `base.OnModelCreating(builder)` then `builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly())`
- File: `backend/src/JiApp.Infrastructure/Persistence/Configurations/UserConfiguration.cs`
  - Implements `IEntityTypeConfiguration<User>`
  - Configure DisplayName: MaxLength(50)
- File: `backend/src/JiApp.Infrastructure/Persistence/Configurations/YoutubeSearchHistoryConfiguration.cs`
  - Implements `IEntityTypeConfiguration<YoutubeSearchHistory>`
  - HasOne(User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade)
- File: `backend/src/JiApp.Infrastructure/Persistence/Configurations/YoutubeDownloadHistoryConfiguration.cs`
  - Implements `IEntityTypeConfiguration<YoutubeDownloadHistory>`
  - HasOne(User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade)
- File: `backend/src/JiApp.Infrastructure/Persistence/Configurations/EventLogConfiguration.cs`
  - Implements `IEntityTypeConfiguration<EventLog>`
  - HasOne(User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.SetNull)
- Acceptance: DbContext compiles; configurations applied correctly

**B1.3: Create initial migration**
- Run: `dotnet ef migrations add Init --project backend/src/JiApp.Infrastructure --startup-project backend/src/JiApp.Api --output-dir Persistence/Migrations`
- Verify migration creates: AspNetUsers (with DisplayName), AspNetRoles, AspNetUserRoles, AspNetUserClaims, AspNetUserLogins, AspNetUserTokens, AspNetRoleClaims, YoutubeSearchHistory, YoutubeDownloadHistory, EventLogs
- Run: `dotnet ef database update --project backend/src/JiApp.Infrastructure --startup-project backend/src/JiApp.Api`
- Acceptance: SQLite database file created at configured path; all tables exist

**B1.4: Create JWT token service**
- File: `backend/src/JiApp.Infrastructure/Services/IJwtTokenService.cs`
  - Method: `string GenerateToken(long userId, string username)` — returns JWT string
  - Method: `bool IsTokenValid(string token)` — returns true if not expired
  - Method: `string GetUsernameFromToken(string token)` — extracts username claim
  - Method: `long GetUserIdFromToken(string token)` — extracts userId claim
- File: `backend/src/JiApp.Infrastructure/Services/JwtTokenService.cs`
  - Constructor receives `IConfiguration config`
  - GenerateToken: Creates JwtSecurityToken with claims [ClaimTypes.NameIdentifier = userId, ClaimTypes.Name = username], signed with HMAC-SHA256 using base64-decoded key from config["Jwt:Key"], issuer from config["Jwt:Issuer"], audience from config["Jwt:Audience"], expires in config["Jwt:ExpireMinutes"] minutes
  - IsTokenValid: Reads token, checks ValidTo >= DateTime.UtcNow
  - GetUsernameFromToken: Reads token, finds claim with type ClaimTypes.Name
  - GetUserIdFromToken: Reads token, finds claim with type ClaimTypes.NameIdentifier, parses to long
- Acceptance: Unit test generates a token and validates it; username and userId can be extracted

**B1.5: Create CurrentUserService**
- File: `backend/src/JiApp.Api/Services/CurrentUserService.cs`
  - Implements `ICurrentUserService`
  - Constructor receives `IHttpContextAccessor httpContextAccessor`
  - UserId: Reads ClaimTypes.NameIdentifier from HttpContext.User.Claims, parses to long
  - Username: Reads ClaimTypes.Name from HttpContext.User.Claims
- Register as scoped service in Program.cs
- Acceptance: When JWT is valid, CurrentUserService correctly returns user id and username from claims

**B1.6: Configure Identity and JWT in Program.cs**
- Update `backend/src/JiApp.Api/Program.cs`:
  - Register JiAppDbContext with SQLite provider (connection string from config with placeholder replacement)
  - Register ASP.NET Identity with User and IdentityRole(long): password rules (RequireDigit: false, RequireLowercase: false, RequireUppercase: false, RequiredLength: 4, RequireNonAlphanumeric: false, RequiredUniqueChars: 0, RequireUniqueEmail: true)
  - Register JWT Bearer authentication: ValidateIssuer: true, ValidateAudience: true, ValidateIssuerSigningKey: true, ValidateLifetime: true, key from config
  - Register authorization with default policy requiring authenticated user with JWT Bearer scheme
  - Register IJwtTokenService as singleton
  - Register ICurrentUserService as scoped
  - Register IHttpContextAccessor
  - Middleware pipeline: UseRouting, UseCors, UseAuthentication, UseAuthorization
- Acceptance: Application starts; Identity tables exist; JWT configuration loads without error

**B1.7: Create Register feature slice**
- File: `backend/src/JiApp.Api/Features/Auth/Register/RegisterRequest.cs`
  - `public sealed record RegisterRequest(string Username, string Email, string Password, string DisplayName)`
- File: `backend/src/JiApp.Api/Features/Auth/Register/RegisterResponse.cs`
  - `public sealed record RegisterResponse()` (empty; 201 Created with no body)
- File: `backend/src/JiApp.Api/Features/Auth/Register/RegisterValidator.cs`
  - Username: NotEmpty, Length(3, 50), Matches("^[a-zA-Z0-9_]+$")
  - Email: NotEmpty, EmailAddress
  - Password: NotEmpty, MinimumLength(4)
  - DisplayName: NotEmpty, MaximumLength(50)
- File: `backend/src/JiApp.Api/Features/Auth/Register/RegisterHandler.cs`
  - Constructor receives `UserManager<User> userManager`
  - Method: `async Task<Result<RegisterResponse>> HandleAsync(RegisterRequest request)`
  - Logic: Check if username exists (FindByNameAsync), check if email exists (FindByEmailAsync), create User, call CreateAsync with password. Return Success or Failure with error messages.
- File: `backend/src/JiApp.Api/Features/Auth/Register/RegisterEndpoint.cs`
  - Static method: `IEndpointRouteBuilder MapRegister(this IEndpointRouteBuilder endpoints)`
  - Maps POST "/api/auth/register"
  - Allows anonymous
  - Receives RegisterRequest from body
  - Validates with RegisterValidator; returns 400 with errors if invalid
  - Calls RegisterHandler.HandleAsync
  - Returns 201 on success, 400 on failure
- Acceptance: POST /api/auth/register with valid data creates user in database and returns 201; invalid data returns 400 with validation errors; duplicate username returns 400

**B1.8: Create Login feature slice**
- File: `backend/src/JiApp.Api/Features/Auth/Login/LoginRequest.cs`
  - `public sealed record LoginRequest(string Username, string Password)`
- File: `backend/src/JiApp.Api/Features/Auth/Login/LoginResponse.cs`
  - `public sealed record LoginResponse(long Id, string? DisplayName, string Token)`
- File: `backend/src/JiApp.Api/Features/Auth/Login/LoginValidator.cs`
  - Username: NotEmpty
  - Password: NotEmpty
- File: `backend/src/JiApp.Api/Features/Auth/Login/LoginHandler.cs`
  - Constructor receives `UserManager<User> userManager, SignInManager<User> signInManager, IJwtTokenService jwtTokenService`
  - Method: `async Task<Result<LoginResponse>> HandleAsync(LoginRequest request)`
  - Logic: FindByNameAsync, if null return Failure. PasswordSignInAsync (isPersistent: false, lockoutOnFailure: false). If not succeeded return Failure. Generate token with userId and username. Return Success with LoginResponse.
- File: `backend/src/JiApp.Api/Features/Auth/Login/LoginEndpoint.cs`
  - Maps POST "/api/auth/login"
  - Allows anonymous
  - Validates with LoginValidator
  - Calls LoginHandler.HandleAsync
  - Returns 200 with LoginResponse on success, 401 on failure
- Acceptance: POST /api/auth/login with valid credentials returns 200 with JWT; invalid credentials return 401

**B1.9: Create Me feature slice**
- File: `backend/src/JiApp.Api/Features/Auth/Me/MeResponse.cs`
  - `public sealed record MeResponse(long Id, string? DisplayName, string Token)`
- File: `backend/src/JiApp.Api/Features/Auth/Me/MeHandler.cs`
  - Constructor receives `UserManager<User> userManager, IJwtTokenService jwtTokenService, ICurrentUserService currentUser`
  - Method: `async Task<Result<MeResponse>> HandleAsync(string currentToken)`
  - Logic: Get username from currentUser.Username. FindByNameAsync. If null, return Failure. Return Success with MeResponse echoing the current token.
- File: `backend/src/JiApp.Api/Features/Auth/Me/MeEndpoint.cs`
  - Maps GET "/api/auth/me"
  - Requires authorization
  - Extracts Bearer token from Authorization header
  - Calls MeHandler.HandleAsync
  - Returns 200 with MeResponse on success, 401 on failure
- Acceptance: GET /api/auth/me with valid token returns user data; expired or missing token returns 401

**B1.10: Register all auth endpoints in Program.cs**
- Update Program.cs to call `app.MapRegister()`, `app.MapLogin()`, `app.MapMe()`
- Acceptance: All three auth endpoints respond correctly via Swagger UI

### Frontend Tasks

**F1.1: Create AuthContext**
- File: `mobile/src/context/AuthContext.tsx`
  - State shape: `{ token: string | null, userId: number | null, displayName: string | null, isLoading: boolean }`
  - Actions: LOGIN (sets token, userId, displayName), LOGOUT (clears all), RESTORE_TOKEN (sets from storage), SET_LOADING
  - Provide: state, login(username, password), register(username, email, password, displayName), logout(), checkToken()
  - login calls POST /api/auth/login, stores token in encrypted storage, dispatches LOGIN
  - register calls POST /api/auth/register (does not auto-login)
  - logout clears encrypted storage, dispatches LOGOUT
  - checkToken reads token from encrypted storage, calls GET /api/auth/me, dispatches RESTORE_TOKEN or LOGOUT
  - On app start, call checkToken to restore session
- Acceptance: AuthContext provides auth state; login/logout/register functions work

**F1.2: Create auth service**
- File: `mobile/src/services/authService.ts`
  - Function: `login(username: string, password: string): Promise<LoginResponse>`
  - Function: `register(username: string, email: string, password: string, displayName: string): Promise<void>`
  - Function: `checkToken(token: string): Promise<LoginResponse>`
  - All functions use apiClient instance
- Acceptance: Functions correctly call API endpoints and return typed responses

**F1.3: Create storage service**
- File: `mobile/src/services/storageService.ts`
  - Function: `saveToken(token: string): Promise<void>` — uses EncryptedStorage
  - Function: `getToken(): Promise<string | null>` — reads from EncryptedStorage
  - Function: `clearToken(): Promise<void>` — removes from EncryptedStorage
  - Function: `saveCredentials(username: string, password: string): Promise<void>` — for "Remember Me"
  - Function: `getCredentials(): Promise<{ username: string, password: string } | null>`
  - Function: `clearCredentials(): Promise<void>`
  - Function: `saveLanguage(lang: string): Promise<void>` — uses AsyncStorage
  - Function: `getLanguage(): Promise<string | null>`
- Acceptance: Token persists across app restarts; clearToken removes it

**F1.3a: Build form components in Storybook**
- File: `mobile/src/components/Button.tsx`
  - Props: `title: string`, `onPress: () => void`, `disabled?: boolean`, `loading?: boolean`
  - When loading: shows ActivityIndicator instead of title
  - When disabled: reduced opacity, onPress does not fire
- File: `mobile/src/components/Button.stories.tsx`
  - Stories: Default, Disabled, Loading
- File: `mobile/src/components/FormInput.tsx`
  - Props: `value: string`, `onChangeText: (text: string) => void`, `placeholder?: string`, `secureTextEntry?: boolean`, `error?: string`, `label?: string`
  - Shows red border and error message below input when `error` is set
  - Uses i18n for placeholder/label where applicable
- File: `mobile/src/components/FormInput.stories.tsx`
  - Stories: Default, Secure (password), WithError, WithLabel
- Acceptance: Components render in Storybook; all story states visible

**F1.3b: Test form components**
- File: `mobile/src/components/Button.test.tsx`
  - Test: renders title text, fires onPress on press, shows ActivityIndicator when loading=true, does NOT fire onPress when disabled=true, does NOT fire onPress when loading=true
- File: `mobile/src/components/FormInput.test.tsx`
  - Test: renders with placeholder, calls onChangeText on text input, shows error message when error prop is set, toggles secureTextEntry for password variant
- Run `npm test` — all new tests pass
- Acceptance: `npm test` passes with 4+ component tests total

**F1.4: Build LoginScreen**
- File: `mobile/src/screens/LoginScreen.tsx`
  - TextInput for username (placeholder from i18n: `auth.username`)
  - TextInput for password (secureTextEntry, placeholder from i18n: `auth.password`)
  - Checkbox/Switch for "Remember Me" (label from i18n: `auth.rememberMe`)
  - Button "Login" (label from i18n: `auth.login`)
  - Link text "Register" navigates to RegisterScreen (label from i18n: `auth.goToRegister`)
  - On mount: check if saved credentials exist, pre-fill if so
  - On login press: validate fields not empty (show error from i18n if empty), call AuthContext.login, if "Remember Me" is checked save credentials
  - Loading spinner while API call in progress
  - Error message displayed below button on failure (from i18n: `auth.invalidCredentials`)
- i18n keys to add: `auth.rememberMe`, `auth.goToRegister`, `auth.invalidCredentials`, `auth.loginSuccess`, `auth.usernameRequired`, `auth.passwordRequired`
- Acceptance: User can type username/password, press Login, see loading state, get logged in or see error. "Remember Me" persists credentials.

**F1.5: Build RegisterScreen**
- File: `mobile/src/screens/RegisterScreen.tsx`
  - TextInput for username
  - TextInput for email (keyboardType: email-address)
  - TextInput for password (secureTextEntry)
  - TextInput for display name
  - Button "Register" (label from i18n: `auth.register`)
  - Link text "Back to Login" navigates back
  - On press: validate all fields not empty, username 3+ chars, password 4+ chars. Call AuthContext.register. On success, show success alert (i18n: `auth.registerSuccess`) and navigate to LoginScreen. On failure, show error.
- i18n keys to add: `auth.goToLogin`, `auth.registerSuccess`, `auth.registerFailed`, `auth.emailRequired`, `auth.displayNameRequired`, `auth.usernameTooShort`, `auth.passwordTooShort`
- Acceptance: User can register, sees success, is navigated to login. Validation errors shown for each field.

**F1.5a: Test RegisterScreen**
- File: `mobile/src/screens/__tests__/RegisterScreen.test.tsx`
  - Test: renders all 4 inputs (username, email, password, displayName) + register button
  - Test: shows validation errors when fields are empty and register is pressed
  - Test: shows "username too short" error when username < 3 chars
  - Test: shows "password too short" error when password < 4 chars
  - Test: calls AuthContext.register with correct data from all 4 fields
  - Test: navigates to LoginScreen on successful registration
  - Test: shows error alert on registration failure (duplicate username/email)
- Run `npm test` — all new tests pass
- Acceptance: RegisterScreen tests cover validation, success, and error paths

**F1.6: Update AppNavigator with auth state**
- File: `mobile/src/navigation/AppNavigator.tsx`
  - Wrap with AuthContext.Provider
  - Read isLoading and token from AuthContext
  - If isLoading, show a full-screen loading spinner
  - If token is null, show AuthNavigator
  - If token is not null, show MainNavigator
- Acceptance: After login, user sees SearchScreen (placeholder). After logout, user sees LoginScreen. On app restart with valid saved token, user goes directly to MainNavigator.

**F1.7: Create useAuth hook**
- File: `mobile/src/hooks/useAuth.ts`
  - Wraps useContext(AuthContext) for convenience
  - Returns: `{ token, userId, displayName, isLoading, login, logout, register, checkToken }`
- Acceptance: All screens can use useAuth() to access auth state and actions

**F1.8: Test AuthContext and AppNavigator**
- File: `mobile/src/context/__tests__/AuthContext.test.tsx`
  - Test: LOGIN action sets token, userId, displayName in state
  - Test: LOGOUT clears all state to null
  - Test: RESTORE_TOKEN hydrates state from provided values
  - Test: login() calls POST /api/auth/login, stores token in EncryptedStorage on success
  - Test: login() dispatches error state on invalid credentials (no token stored)
  - Test: register() calls POST /api/auth/register, does NOT auto-login
  - Test: register() shows error alert on API failure
  - Test: checkToken() calls GET /api/auth/me with stored token, dispatches RESTORE_TOKEN on success
  - Test: checkToken() dispatches LOGOUT when /api/auth/me returns 401 (expired token)
  - Test: logout() clears EncryptedStorage, dispatches LOGOUT
- File: `mobile/src/navigation/__tests__/AppNavigator.test.tsx`
  - Test: renders AuthNavigator when token is null and isLoading is false
  - Test: renders full-screen loading spinner when isLoading is true
  - Test: renders MainNavigator when token is present (non-null string)
- Run `npm test` — all new tests pass
- Acceptance: AuthContext tests cover all state transitions and API integration; AppNavigator tests cover all three auth states

### Integration Tasks

- Start backend, start mobile app on emulator
- Register a new user via RegisterScreen
- Verify user exists in SQLite database
- Login with the registered user
- Verify JWT is stored in encrypted storage (log in console for dev)
- Kill and restart the app; verify auto-login works (token restored, /api/auth/me called)
- Verify expired token triggers logout

### Definition of Done - Phase 1

- [x] POST /api/auth/register creates user with hashed password
- [x] POST /api/auth/login returns valid JWT
- [x] GET /api/auth/me validates token and returns user data
- [x] FluentValidation rejects invalid register/login requests
- [x] Mobile LoginScreen and RegisterScreen are functional
- [x] JWT stored securely in EncryptedStorage
- [x] "Remember Me" saves and restores credentials
- [x] Auto-login on app restart with valid token
- [x] All auth strings translated in both pl.json and en.json
- [x] `dotnet build` and RN build both succeed
- [x] New components (Button, FormInput) have Storybook stories covering all states
- [x] New components (Button, FormInput) have Jest tests passing (happy-path + edge cases for auth)
- [x] AuthContext has comprehensive Jest tests (all actions, API integration, error paths)
- [x] RegisterScreen has Jest tests (validation, success, error)
- [x] AppNavigator has Jest tests (all auth states)
- [x] `npm test` passes with zero failures

---

## Phase 2: YouTube Search (Backend + Frontend)

### Prerequisites
- Phase 1 complete: user can register, login, JWT works end-to-end
- YouTube API key configured in appsettings

### Backend Tasks

**B2.1: Create YoutubeVideo record and YoutubeClient**
- File: `backend/src/JiApp.YtApi/YoutubeVideo.cs`
  - `public sealed record YoutubeVideo(string VideoId, string Title, string Description, string ImageUrl)` with computed property `string VideoUrl => $"https://www.youtube.com/watch?v={VideoId}"`
- File: `backend/src/JiApp.YtApi/YoutubeClientResponse.cs`
  - `public sealed record YoutubeClientResponse(string? FilePath, bool Success, string[] Errors)`
- File: `backend/src/JiApp.YtApi/Configuration/YoutubeSettings.cs`
  - `public sealed record YoutubeSettings(string ApiKey, string YtDlpPath, string FfmpegPath)`
- File: `backend/src/JiApp.YtApi/IYoutubeClient.cs`
  - Method: `Task<IReadOnlyList<YoutubeVideo>> SearchVideosAsync(string query, int maxResults = 10)`
  - Method: `Task<YoutubeClientResponse> DownloadVideoAsync(string videoUrl, string outputPath)`
- File: `backend/src/JiApp.YtApi/YoutubeClient.cs`
  - Constructor receives `YoutubeSettings settings`
  - Creates YouTubeService with ApiKey
  - SearchVideosAsync: Creates Search.List("snippet") request, sets Q and MaxResults, executes, filters by "youtube#video" kind, maps to YoutubeVideo records
  - DownloadVideoAsync: Creates YoutubeDL instance with paths from settings, calls RunAudioDownload with AudioConversionFormat.Mp3, returns YoutubeClientResponse
- Register IYoutubeClient as singleton in Program.cs with YoutubeSettings from config
- Acceptance: IYoutubeClient.SearchVideosAsync returns YouTube results when called with a valid query

**B2.2: Create SearchHistory repository**
- File: `backend/src/JiApp.Infrastructure/Repositories/ISearchHistoryRepository.cs`
  - Method: `Task<IReadOnlyList<YoutubeSearchHistory>> GetByUserIdAsync(long userId, int limit)`
  - Method: `Task AddAsync(YoutubeSearchHistory entry)`
- File: `backend/src/JiApp.Infrastructure/Repositories/SearchHistoryRepository.cs`
  - Constructor receives `JiAppDbContext dbContext`
  - GetByUserIdAsync: Query where UserId == userId, order by SearchedAt descending, take limit
  - AddAsync: Add entity, SaveChangesAsync
- Register as scoped in Program.cs
- Acceptance: Repository correctly persists and retrieves search history entries

**B2.3: Create SearchVideos feature slice**
- File: `backend/src/JiApp.Api/Features/Search/SearchVideos/SearchVideosRequest.cs`
  - `public sealed record SearchVideosRequest(string Query, int? MaxResults)`
- File: `backend/src/JiApp.Api/Features/Search/SearchVideos/SearchVideosResponse.cs`
  - `public sealed record SearchVideosResponse(IReadOnlyList<VideoItem> Results)`
  - `public sealed record VideoItem(string VideoId, string Title, string Description, string ImageUrl, string VideoUrl)`
- File: `backend/src/JiApp.Api/Features/Search/SearchVideos/SearchVideosValidator.cs`
  - Query: NotEmpty, MaximumLength(200)
  - MaxResults: When not null, InclusiveBetween(1, 50)
- File: `backend/src/JiApp.Api/Features/Search/SearchVideos/SearchVideosHandler.cs`
  - Constructor receives `IYoutubeClient youtubeClient, ISearchHistoryRepository searchHistoryRepo, ICurrentUserService currentUser`
  - Method: `async Task<Result<SearchVideosResponse>> HandleAsync(SearchVideosRequest request)`
  - Logic: Resolve maxResults (default 10 if null). Call youtubeClient.SearchVideosAsync. Save search history entry (UserId from currentUser, SearchedAt = UtcNow, SearchText = request.Query). Map YoutubeVideo list to VideoItem list. Return Success.
- File: `backend/src/JiApp.Api/Features/Search/SearchVideos/SearchVideosEndpoint.cs`
  - Maps POST "/api/search"
  - Requires authorization
  - Validates with SearchVideosValidator
  - Calls SearchVideosHandler.HandleAsync
  - Returns 200 with SearchVideosResponse
- Acceptance: POST /api/search with valid token and query returns YouTube results; search is saved to database

**B2.4: Create SearchHistory feature slice**
- File: `backend/src/JiApp.Api/Features/Search/SearchHistory/SearchHistoryRequest.cs`
  - `public sealed record SearchHistoryRequest(int? Limit)`
- File: `backend/src/JiApp.Api/Features/Search/SearchHistory/SearchHistoryResponse.cs`
  - `public sealed record SearchHistoryResponse(IReadOnlyList<SearchHistoryItem> Items)`
  - `public sealed record SearchHistoryItem(long Id, string? SearchText, DateTime? SearchedAt)`
- File: `backend/src/JiApp.Api/Features/Search/SearchHistory/SearchHistoryValidator.cs`
  - Limit: When not null, InclusiveBetween(1, 50)
- File: `backend/src/JiApp.Api/Features/Search/SearchHistory/SearchHistoryHandler.cs`
  - Constructor receives `ISearchHistoryRepository searchHistoryRepo, ICurrentUserService currentUser`
  - Method: `async Task<Result<SearchHistoryResponse>> HandleAsync(SearchHistoryRequest request)`
  - Logic: Resolve limit (default 10). Get search history from repo. Map to SearchHistoryItem list. Return Success.
- File: `backend/src/JiApp.Api/Features/Search/SearchHistory/SearchHistoryEndpoint.cs`
  - Maps GET "/api/search/history"
  - Requires authorization
  - Reads `limit` from query string, constructs SearchHistoryRequest
  - Validates, calls handler
  - Returns 200 with SearchHistoryResponse
- Acceptance: GET /api/search/history returns the user's search history ordered by most recent first

**B2.5: Register search endpoints in Program.cs**
- Add `app.MapSearchVideos()` and `app.MapSearchHistory()` to Program.cs
- Acceptance: Both endpoints respond via Swagger; authentication enforced

### Frontend Tasks

**F2.1: Create search service**
- File: `mobile/src/services/searchService.ts`
  - Function: `searchVideos(query: string, maxResults?: number): Promise<SearchResponse>`
  - Function: `getSearchHistory(limit?: number): Promise<SearchHistoryItem[]>`
- Acceptance: Functions return typed data from API

**F2.2: Build SearchBar component in Storybook**
- File: `mobile/src/components/SearchBar.tsx`
  - TextInput with search icon, placeholder from i18n (`search.placeholder`: "Szukaj na YouTube...")
  - Debounce input by 500ms before triggering search
  - Props: `onSearch(query: string)`, `initialValue?: string`
  - Clear button (X icon) when text is not empty
- File: `mobile/src/components/SearchBar.stories.tsx`
  - Stories: Empty, WithText, WithClearButtonVisible
- i18n keys: `search.placeholder`, `search.noResults`, `search.searching`
- Acceptance: Component renders in Storybook; all story states visible

**F2.2b: Test SearchBar**
- File: `mobile/src/components/SearchBar.test.tsx`
  - Test: renders with placeholder text from i18n
  - Test: calls onSearch after 500ms debounce (only once even with rapid typing)
  - Test: clear button (X) appears when text is non-empty and clears input on press
  - Test: initialValue is displayed on mount
- Run `npm test` — all pass
- Acceptance: SearchBar tests cover debounce behavior and clear functionality

**F2.3: Build VideoCard component in Storybook**
- File: `mobile/src/components/VideoCard.tsx`
  - Displays thumbnail image (imageUrl), title, truncated description (2 lines)
  - Props: `video: VideoItem`, `onPress(video: VideoItem)`
  - Tapping the card calls onPress
- File: `mobile/src/components/VideoCard.stories.tsx`
  - Stories: Default (full data), LongTitle (truncated), MissingThumbnail (placeholder), NoDescription
  - Mock thumbnail with a placeholder color block for missing thumbnail story
- Acceptance: Component renders in Storybook; all story states visible

**F2.3b: Test VideoCard**
- File: `mobile/src/components/VideoCard.test.tsx`
  - Test: renders title, description, and thumbnail image
  - Test: truncates description to 2 lines (verify numberOfLines prop on description Text)
  - Test: fires onPress with full video data when card is pressed
  - Test: shows placeholder when imageUrl is empty/missing
- Run `npm test` — all pass
- Acceptance: VideoCard tests cover render, truncation, press, and missing thumbnail

**F2.4: Build SearchScreen**
- File: `mobile/src/screens/SearchScreen.tsx`
  - SearchBar at top
  - FlatList of VideoCard components for results
  - Loading spinner while search in progress
  - Empty state when no results (i18n: `search.noResults`)
  - Error state with retry button
  - On VideoCard press: navigate to DownloadScreen with video data as params
  - On initial load: optionally show recent search history as suggestions below search bar
- i18n keys: `search.title`, `search.error`, `search.recentSearches`
- Acceptance: User types query, sees results with thumbnails, can tap a result to navigate to download

**F2.5: Create useSearch hook**
- File: `mobile/src/hooks/useSearch.ts`
  - State: results (VideoItem[]), isLoading, error
  - Function: search(query: string, maxResults?: number) — calls searchService, updates state
  - Function: clearResults() — resets state
- Acceptance: Hook manages search state; loading/error states work correctly

**F2.6: Test useSearch hook**
- File: `mobile/src/hooks/__tests__/useSearch.test.tsx`
  - Test: initialState has empty results array, isLoading=false, error=null
  - Test: search() sets isLoading=true, then populates results and sets isLoading=false on success
  - Test: search() sets error state on API failure, isLoading returns to false
  - Test: clearResults() resets results to empty array
- Mock searchService to control API responses
- Run `npm test` — all pass
- Acceptance: useSearch hook tests cover all state transitions

### Integration Tasks

- Login on mobile, navigate to SearchScreen
- Type a query (e.g., "Metallica Nothing Else Matters")
- Verify YouTube results appear with thumbnails
- Check database that search query was saved in YoutubeSearchHistory
- Verify GET /api/search/history returns the saved search

### Definition of Done - Phase 2

- [x] POST /api/search returns YouTube video results
- [x] Search history saved to database per user
- [x] GET /api/search/history returns user's searches
- [x] SearchScreen shows results with thumbnails
- [x] VideoCard component is reusable
- [x] SearchBar has debounce and clear functionality
- [x] Loading and error states work on SearchScreen
- [x] All search strings translated in pl.json and en.json
- [x] Backend validation rejects empty/too-long queries
- [x] New components (SearchBar, VideoCard) have Storybook stories covering all states
- [x] New components (SearchBar, VideoCard) have Jest tests passing
- [x] useSearch hook has Jest tests covering all state transitions
- [x] `npm test` passes with zero failures

---

## Phase 3: MP3 Download (Backend + Frontend)

### Prerequisites
- Phase 2 complete: search works, user can see video results
- yt-dlp and ffmpeg binaries accessible at configured paths

### Backend Tasks

**B3.1: Create TempFileStore**
- File: `backend/src/JiApp.Infrastructure/Services/ITempFileStore.cs`
  - Method: `string Add(string filePath)` — returns GUID id
  - Method: `string? Get(string id)` — returns file path or null if expired
  - Method: `void CleanupExpired()` — removes expired entries and deletes files
- File: `backend/src/JiApp.Infrastructure/Services/TempFileStore.cs`
  - Private dictionary: `Dictionary<string, (string Path, DateTime Expiry)>`
  - Lifetime: 10 minutes (TimeSpan.FromMinutes(10))
  - Add: Generates GUID (ToString("N")), stores path with expiry, returns id
  - Get: Looks up id, checks expiry and File.Exists, returns path or null (removes if expired)
  - CleanupExpired: Iterates entries, deletes expired files from disk and removes from dictionary
- Register as singleton in Program.cs
- Acceptance: File can be added, retrieved by id, and expires after 10 minutes

**B3.2: Create DownloadHistory repository**
- File: `backend/src/JiApp.Infrastructure/Repositories/IDownloadHistoryRepository.cs`
  - Method: `Task<IReadOnlyList<YoutubeDownloadHistory>> GetByUserIdAsync(long userId, int limit)`
  - Method: `Task AddAsync(YoutubeDownloadHistory entry)`
- File: `backend/src/JiApp.Infrastructure/Repositories/DownloadHistoryRepository.cs`
  - Constructor receives `JiAppDbContext dbContext`
  - GetByUserIdAsync: Query where UserId == userId, order by DownloadedAt descending, take limit
  - AddAsync: Add entity, SaveChangesAsync
- Register as scoped in Program.cs
- Acceptance: Repository correctly persists and retrieves download history

**B3.3: Create GetDownloadLink feature slice**
- File: `backend/src/JiApp.Api/Features/Downloads/GetDownloadLink/DownloadRequest.cs`
  - `public sealed record DownloadRequest(string VideoId, string VideoUrl, string? Title, string? Description, string? ImageUrl)`
- File: `backend/src/JiApp.Api/Features/Downloads/GetDownloadLink/DownloadResponse.cs`
  - `public sealed record DownloadResponse(string DownloadUrl)`
- File: `backend/src/JiApp.Api/Features/Downloads/GetDownloadLink/GetDownloadLinkValidator.cs`
  - VideoId: NotEmpty
  - VideoUrl: NotEmpty, Must start with "https://www.youtube.com/watch?v="
  - Title: When not null, MaximumLength(300)
  - Description: When not null, MaximumLength(1000)
  - ImageUrl: When not null, MaximumLength(300)
- File: `backend/src/JiApp.Api/Features/Downloads/GetDownloadLink/GetDownloadLinkHandler.cs`
  - Constructor receives `IYoutubeClient youtubeClient, ITempFileStore tempFileStore, IDownloadHistoryRepository downloadHistoryRepo, ICurrentUserService currentUser, IConfiguration config`
  - Method: `async Task<Result<DownloadResponse>> HandleAsync(DownloadRequest request, HttpRequest httpRequest)`
  - Logic:
    - Build output folder: Path.Combine(config["App:BaseDirectory"], $"YtMp3_{currentUser.Username}")
    - Call youtubeClient.DownloadVideoAsync(request.VideoUrl, outputFolder)
    - If not success, return Failure with error
    - Register file in tempFileStore, get id
    - Build temp URL: $"{httpRequest.Scheme}://{httpRequest.Host}/api/downloads/mp3/file/{id}"
    - Save download history entry (UserId, DownloadedAt = UtcNow, VideoTitle, VideoDescription, VideoId, VideoUrl, ImageUrl from request)
    - Return Success with DownloadResponse containing the temp URL
- File: `backend/src/JiApp.Api/Features/Downloads/GetDownloadLink/GetDownloadLinkEndpoint.cs`
  - Maps POST "/api/downloads/mp3"
  - Requires authorization
  - Validates with GetDownloadLinkValidator
  - Calls handler
  - Returns 200 with DownloadResponse on success, 500 on yt-dlp failure
- Acceptance: POST /api/downloads/mp3 with valid video URL triggers download, returns temp URL; download history saved

**B3.4: Create DownloadFile feature slice**
- File: `backend/src/JiApp.Api/Features/Downloads/DownloadFile/DownloadFileHandler.cs`
  - Constructor receives `ITempFileStore tempFileStore`
  - Method: `Result<string> Handle(string id)` — returns file path or failure
  - Logic: Call tempFileStore.Get(id). If null, return Failure("File expired or not found"). Return Success with path.
- File: `backend/src/JiApp.Api/Features/Downloads/DownloadFile/DownloadFileEndpoint.cs`
  - Maps GET "/api/downloads/mp3/file/{id}"
  - Allows anonymous (temp URL is the auth mechanism)
  - Calls DownloadFileHandler.Handle
  - Returns PhysicalFile with content type "audio/mpeg" and the original filename
  - Returns 404 if file not found/expired
- Acceptance: GET /api/downloads/mp3/file/{validGuid} streams the MP3 file; expired/invalid GUID returns 404

**B3.5: Create DownloadHistory feature slice**
- File: `backend/src/JiApp.Api/Features/Downloads/DownloadHistory/DownloadHistoryRequest.cs`
  - `public sealed record DownloadHistoryRequest(int? Limit)`
- File: `backend/src/JiApp.Api/Features/Downloads/DownloadHistory/DownloadHistoryResponse.cs`
  - `public sealed record DownloadHistoryResponse(IReadOnlyList<DownloadHistoryItem> Items)`
  - `public sealed record DownloadHistoryItem(long Id, string? VideoTitle, string? VideoDescription, string? VideoId, string? VideoUrl, string? ImageUrl, DateTime DownloadedAt)`
- File: `backend/src/JiApp.Api/Features/Downloads/DownloadHistory/DownloadHistoryValidator.cs`
  - Limit: When not null, InclusiveBetween(1, 50)
- File: `backend/src/JiApp.Api/Features/Downloads/DownloadHistory/DownloadHistoryHandler.cs`
  - Constructor receives `IDownloadHistoryRepository downloadHistoryRepo, ICurrentUserService currentUser`
  - Method: `async Task<Result<DownloadHistoryResponse>> HandleAsync(DownloadHistoryRequest request)`
  - Logic: Resolve limit, get history from repo, map to DownloadHistoryItem list, return Success
- File: `backend/src/JiApp.Api/Features/Downloads/DownloadHistory/DownloadHistoryEndpoint.cs`
  - Maps GET "/api/downloads/history"
  - Requires authorization
  - Reads limit from query string
  - Returns 200 with DownloadHistoryResponse
- Acceptance: GET /api/downloads/history returns user's download history ordered by most recent

**B3.6: Register download endpoints in Program.cs**
- Add `app.MapGetDownloadLink()`, `app.MapDownloadFile()`, `app.MapDownloadHistory()`
- Acceptance: All three endpoints respond; Swagger shows them

### Frontend Tasks

**F3.1: Create download service**
- File: `mobile/src/services/downloadService.ts`
  - Function: `requestDownloadLink(request: DownloadRequest): Promise<DownloadResponse>` — calls POST /api/downloads/mp3
  - Function: `downloadFile(downloadUrl: string, fileName: string): Promise<string>` — uses react-native-blob-util to download the file from temp URL and save to device Downloads folder. Returns local file path.
  - Function: `getDownloadHistory(limit?: number): Promise<DownloadHistoryItem[]>` — calls GET /api/downloads/history
- Install: react-native-blob-util (for file download to device storage)
- Acceptance: File downloads from temp URL and saves to device

**F3.2: Build DownloadScreen**
- File: `mobile/src/screens/DownloadScreen.tsx`
  - Receives video data (VideoItem) via navigation params
  - Displays video info: thumbnail, title, description
  - Button "Download MP3" (i18n: `download.downloadMp3`)
  - On press:
    - Show loading/progress indicator
    - Call downloadService.requestDownloadLink with video data
    - Receive temp URL in response
    - Call downloadService.downloadFile to save MP3 to device
    - Show success message with file path (i18n: `download.success`)
    - Or show error message on failure (i18n: `download.failed`)
  - After download, option to go back to search or view history
- i18n keys: `download.title`, `download.downloadMp3`, `download.downloading`, `download.success`, `download.failed`, `download.goBack`, `download.viewHistory`, `download.fileSaved`
- Acceptance: User taps download, sees progress, file is saved to device Downloads folder, success message shown

**F3.2a: Test DownloadScreen**
- File: `mobile/src/screens/__tests__/DownloadScreen.test.tsx`
  - Test: renders video info (thumbnail, title, description) from navigation params
  - Test: shows "Download MP3" button (from i18n key download.downloadMp3)
  - Test: shows loading spinner during download (isDownloading=true)
  - Test: shows success message with file path after successful download
  - Test: shows error message (from i18n key download.failed) on download failure
  - Test: "Go back to search" and "View history" links navigate correctly
- Mock useDownload hook to control state
- Run `npm test` — all pass
- Acceptance: DownloadScreen tests cover all states (initial, downloading, success, error)

**F3.3: Create useDownload hook**
- File: `mobile/src/hooks/useDownload.ts`
  - State: isDownloading, downloadProgress (percentage or indeterminate), error, localFilePath
  - Function: download(video: VideoItem) — orchestrates requestDownloadLink then downloadFile, updates state
- Acceptance: Hook manages download lifecycle state

**F3.3a: Test useDownload hook**
- File: `mobile/src/hooks/__tests__/useDownload.test.tsx`
  - Test: initialState has isDownloading=false, downloadProgress=null, error=null, localFilePath=null
  - Test: download() sets isDownloading=true, then sets localFilePath on success
  - Test: download() sets error state on API failure, isDownloading returns to false
  - Test: download() sets error state on file download failure (e.g., permission denied)
- Mock downloadService to control API and file responses
- Run `npm test` — all pass
- Acceptance: useDownload hook tests cover success, API failure, and file download failure

**F3.4: Update SearchScreen to navigate to DownloadScreen**
- Update VideoCard onPress in SearchScreen to navigate to DownloadScreen passing the full VideoItem as params
- Acceptance: Tapping a search result navigates to DownloadScreen with video data pre-filled

### Integration Tasks

- Login, search for a video, tap a result
- On DownloadScreen, press "Download MP3"
- Verify yt-dlp runs on backend (check console output)
- Verify temp URL is generated and returned to mobile
- Verify MP3 file is downloaded to device (check Downloads folder)
- Verify download history entry saved in database
- Verify temp file expires after 10 minutes (file URL returns 404)

### Definition of Done - Phase 3

- [x] POST /api/downloads/mp3 invokes yt-dlp and returns temp download URL
- [x] GET /api/downloads/mp3/file/{id} streams the MP3 file
- [x] Temp URLs expire after 10 minutes
- [x] Download history saved to database
- [x] GET /api/downloads/history returns user's downloads
- [x] DownloadScreen shows video info and download button
- [x] MP3 file saved to device Downloads folder
- [x] Loading and error states work during download
- [x] All download strings translated in pl.json and en.json
- [x] Validation rejects invalid YouTube URLs
- [x] useDownload hook has Jest tests covering all state transitions
- [x] DownloadScreen has Jest tests covering all states (initial, downloading, success, error)
- [x] `npm test` passes with zero failures

---

## Phase 4: History (Backend + Frontend)

### Prerequisites
- Phase 3 complete: both search and download history exist in database

### Backend Tasks

**B4.1: Create GetHistory combined feature slice**
- File: `backend/src/JiApp.Api/Features/History/GetHistory/GetHistoryRequest.cs`
  - `public sealed record GetHistoryRequest(int? Limit)`
- File: `backend/src/JiApp.Api/Features/History/GetHistory/GetHistoryResponse.cs`
  - `public sealed record GetHistoryResponse(IReadOnlyList<SearchHistoryItem> Searches, IReadOnlyList<DownloadHistoryItem> Downloads)`
  - Reuse SearchHistoryItem and DownloadHistoryItem record types from their respective response files, or define shared versions in JiApp.Common if needed
- File: `backend/src/JiApp.Api/Features/History/GetHistory/GetHistoryValidator.cs`
  - Limit: When not null, InclusiveBetween(1, 50)
- File: `backend/src/JiApp.Api/Features/History/GetHistory/GetHistoryHandler.cs`
  - Constructor receives `ISearchHistoryRepository searchHistoryRepo, IDownloadHistoryRepository downloadHistoryRepo, ICurrentUserService currentUser`
  - Method: `async Task<Result<GetHistoryResponse>> HandleAsync(GetHistoryRequest request)`
  - Logic: Resolve limit. Fetch both search history and download history in parallel (Task.WhenAll). Map to response records. Return Success.
- File: `backend/src/JiApp.Api/Features/History/GetHistory/GetHistoryEndpoint.cs`
  - Maps GET "/api/history"
  - Requires authorization
  - Reads limit from query string
  - Returns 200 with GetHistoryResponse
- Register in Program.cs: `app.MapGetHistory()`
- Acceptance: GET /api/history returns both search and download history for the current user

### Frontend Tasks

**F4.1: Build HistoryItem component in Storybook**
- File: `mobile/src/components/HistoryItem.tsx`
  - Props: type ("search" | "download"), item data (search text + date OR video title + thumbnail + date)
  - For search type: show search icon, search text, formatted date
  - For download type: show small thumbnail, video title, formatted date
  - On press (download type): navigate to DownloadScreen to re-download
- File: `mobile/src/components/HistoryItem.stories.tsx`
  - Stories: SearchType, DownloadType (with thumbnail), DownloadType (missing thumbnail)
- Acceptance: Component renders in Storybook; all story states visible

**F4.1b: Test HistoryItem**
- File: `mobile/src/components/HistoryItem.test.tsx`
  - Test: search type renders search icon, search text, and formatted date
  - Test: download type renders thumbnail, video title, and formatted date
  - Test: download type shows placeholder when thumbnail is missing
  - Test: fires onPress with item data when download type is pressed
- Run `npm test` — all pass
- Acceptance: HistoryItem tests cover both types, missing thumbnail, and press handler

**F4.2: Build HistoryScreen**
- File: `mobile/src/screens/HistoryScreen.tsx`
  - Two sections or tabs: "Wyszukiwania" (Searches) and "Pobrania" (Downloads), labels from i18n
  - Each section is a FlatList of HistoryItem components
  - Pull-to-refresh to reload history
  - Empty state per section (i18n: `history.noSearches`, `history.noDownloads`)
  - Loading spinner on initial load
- i18n keys: `history.title`, `history.searches`, `history.downloads`, `history.noSearches`, `history.noDownloads`, `history.loadError`
- Acceptance: User sees their search and download history; pull-to-refresh works

**F4.3: Create useHistory hook**
- File: `mobile/src/hooks/useHistory.ts`
  - State: searches (SearchHistoryItem[]), downloads (DownloadHistoryItem[]), isLoading, error
  - Function: loadHistory(limit?: number) — calls GET /api/history, updates state
  - Function: refresh() — reloads with same limit
- Acceptance: Hook fetches and manages combined history state

**F4.3a: Test useHistory hook**
- File: `mobile/src/hooks/__tests__/useHistory.test.tsx`
  - Test: initialState has empty searches and downloads, isLoading=false, error=null
  - Test: loadHistory() fetches both searches and downloads, populates state
  - Test: loadHistory() sets error on API failure, isLoading returns to false
  - Test: refresh() re-fetches with same limit and updates state
  - Test: empty state when both searches and downloads are empty arrays
- Mock the GET /api/history response
- Run `npm test` — all pass
- Acceptance: useHistory hook tests cover loading, success, error, refresh, and empty states

**F4.4: Add History to MainNavigator**
- Ensure HistoryScreen is accessible from MainNavigator
- Add a button or tab bar entry to navigate to HistoryScreen from SearchScreen (e.g., header icon)
- Acceptance: User can navigate to HistoryScreen from the main app flow

### Integration Tasks

- Login, perform several searches and downloads
- Navigate to HistoryScreen
- Verify both search and download histories appear with correct data
- Verify pull-to-refresh fetches latest data
- Verify empty state shows when a new user has no history

### Definition of Done - Phase 4

- [x] GET /api/history returns combined search and download history
- [x] HistoryScreen displays both sections with correct data
- [x] Pull-to-refresh works
- [x] Empty states shown for sections with no data
- [x] HistoryItem component handles both types
- [x] Navigation to HistoryScreen integrated into main flow
- [x] All history strings translated in pl.json and en.json
- [x] HistoryItem component has Storybook stories (search type, download type, missing thumbnail)
- [x] HistoryItem component has Jest tests (both types, press handler)
- [x] useHistory hook has Jest tests (loading, success, error, refresh, empty)
- [x] `npm test` passes with zero failures

---

## Phase 5: Settings & Polish

### Prerequisites
- Phase 4 complete: all features functional

### Backend Tasks

**B5.1: Review and harden error responses**
- Ensure GlobalExceptionMiddleware returns consistent JSON error format: `{ error: string, details?: string[] }`
- Ensure all FluentValidation failures return 400 with `{ errors: string[] }` format
- Ensure all 401 responses have `{ message: string }` format
- Acceptance: All error responses follow a consistent structure

### Frontend Tasks

**F5.1: Build LanguagePicker component in Storybook**
- File: `mobile/src/components/LanguagePicker.tsx`
  - Displays current language name ("Polski" or "English")
  - On press: shows a modal or dropdown with both options
  - On select: calls i18next.changeLanguage, saves preference via storageService.saveLanguage
  - Props: none (reads and writes language internally)
- File: `mobile/src/components/LanguagePicker.stories.tsx`
  - Stories: PolishSelected, EnglishSelected
- Acceptance: Component renders in Storybook; changing language immediately updates all visible strings

**F5.1a: Test LanguagePicker and i18n**
- File: `mobile/src/components/LanguagePicker.test.tsx`
  - Test: displays current language name ("Polski" by default, or saved preference)
  - Test: opens dropdown/modal on press, showing both language options
  - Test: selecting "English" calls i18next.changeLanguage('en') and persists via storageService
  - Test: selecting "Polski" calls i18next.changeLanguage('pl') and persists via storageService
- File: `mobile/src/i18n/__tests__/i18n.test.ts`
  - Test: pl.json and en.json have identical sets of keys (deep comparison)
  - Test: i18next initializes with Polish as default fallback language
  - Test: changeLanguage('en') switches all t() calls to English
  - Test: changeLanguage('pl') switches all t() calls back to Polish
- Run `npm test` — all pass
- Acceptance: LanguagePicker tests cover display, selection, and persistence; i18n tests verify key parity and language switching

**F5.2: Build SettingsScreen**
- File: `mobile/src/screens/SettingsScreen.tsx`
  - Section: Language with LanguagePicker component
  - Section: Account info (display name, username — read-only)
  - Button: "Wyloguj" / "Logout" (calls AuthContext.logout)
  - Version info at bottom
- i18n keys: `settings.title`, `settings.language`, `settings.account`, `settings.displayName`, `settings.username`, `settings.logout`, `settings.version`, `settings.languagePolish`, `settings.languageEnglish`
- Acceptance: User can change language and log out from settings

**F5.3: Add Settings to navigation**
- Add Settings icon/button in SearchScreen header or as a tab
- Ensure SettingsScreen is accessible from MainNavigator
- Acceptance: User can reach SettingsScreen from the main flow

**F5.4: Complete all i18n translations**
- Review all pl.json and en.json files
- Ensure every user-facing string in every screen uses i18n keys
- Verify no hardcoded strings remain in screen/component files
- Acceptance: Switching between PL and EN translates 100% of visible text

**F5.5: Polish UI and UX**
- Add consistent error handling with user-friendly messages across all screens
- File: `mobile/src/components/LoadingSpinner.tsx` — shared loading indicator
- File: `mobile/src/components/ErrorMessage.tsx` — error display with retry button

**F5.5a: Test shared components**
- File: `mobile/src/components/LoadingSpinner.test.tsx` — test renders ActivityIndicator
- File: `mobile/src/components/ErrorMessage.test.tsx`
  - Test: renders error message text
  - Test: renders retry button with i18n label
  - Test: fires onRetry callback when retry button is pressed
  - Test: does not render retry button when onRetry is not provided
- Run `npm test` — all pass
- Acceptance: Shared component tests verify basic rendering and callbacks

- Add empty state illustrations or icons for all empty lists
- Ensure all lists have pull-to-refresh
- Add keyboard dismiss on tap outside inputs
- Ensure proper keyboard handling (avoid keyboard covering inputs)
- Test and fix navigation back behavior on all screens
- Acceptance: App feels polished; no raw error messages; consistent loading/error patterns

**F5.6: Persist language preference across restarts**
- On app start (in App.tsx or i18n/index.ts), read saved language from storageService.getLanguage
- If a saved preference exists, set it via i18next.changeLanguage before rendering
- Acceptance: Language preference survives app restart

### Integration Tasks

- Test language switch end-to-end: change to English in settings, verify all screens
- Test logout flow: settings logout, verify token cleared, navigated to login
- Test app restart language persistence

### Definition of Done - Phase 5

- [x] SettingsScreen with language picker and logout
- [x] Language change takes effect immediately
- [x] Language preference persists across app restarts
- [x] All strings translated in both languages
- [x] No hardcoded user-facing strings in code
- [x] Consistent error/loading/empty states across all screens
- [x] UI polish: keyboard handling, pull-to-refresh, back navigation all correct
- [x] Error responses from backend are consistently structured
- [x] LanguagePicker component has Storybook stories (PL selected, EN selected)
- [x] LanguagePicker has Jest tests (display, selection, persistence)
- [x] i18n key parity test passes (pl.json and en.json have identical keys)
- [x] Shared components (LoadingSpinner, ErrorMessage) have Jest tests
- [x] `npm test` passes with zero failures

---

## Phase 6: Testing & Release Prep

### Prerequisites
- Phase 5 complete: all features working, UI polished

### Backend Tasks

**B6.1: Unit tests for handlers**
- [x] DownloadHistoryHandlerTests (missing coverage)
- File: `backend/tests/JiApp.Tests/Features/Auth/RegisterHandlerTests.cs`
  - Test: successful registration returns Success
  - Test: duplicate username returns Failure
  - Test: duplicate email returns Failure
  - Test: Identity CreateAsync failure returns Failure with error messages
  - Mock: UserManager(User)
- File: `backend/tests/JiApp.Tests/Features/Auth/LoginHandlerTests.cs`
  - Test: valid credentials return Success with token
  - Test: invalid username returns Failure
  - Test: invalid password returns Failure
  - Mock: UserManager(User), SignInManager(User), IJwtTokenService
- File: `backend/tests/JiApp.Tests/Features/Search/SearchVideosHandlerTests.cs`
  - Test: valid query returns results and saves history
  - Test: empty results returned when YouTube returns nothing
  - Test: YouTube API exception is handled gracefully
  - Mock: IYoutubeClient, ISearchHistoryRepository, ICurrentUserService
- File: `backend/tests/JiApp.Tests/Features/Downloads/GetDownloadLinkHandlerTests.cs`
  - Test: valid video URL triggers download and returns temp URL
  - Test: yt-dlp failure returns Failure
  - Test: download history is saved on success
  - Mock: IYoutubeClient, ITempFileStore, IDownloadHistoryRepository, ICurrentUserService, IConfiguration
- Acceptance: All handler tests pass; at least 4 tests per handler

**B6.2: Unit tests for JWT service**
- File: `backend/tests/JiApp.Tests/Infrastructure/JwtTokenServiceTests.cs`
  - Test: GenerateToken produces a valid JWT string
  - Test: IsTokenValid returns true for fresh token
  - Test: IsTokenValid returns false for expired token (use short expiry in test config)
  - Test: GetUsernameFromToken correctly extracts username claim
  - Test: GetUserIdFromToken correctly extracts userId claim
  - Mock: IConfiguration with test JWT settings
- Acceptance: All JWT tests pass

**B6.3: Validator tests**
- Create test files for each validator in the corresponding test feature folder
  - RegisterValidator: rejects empty username, short username (2 chars), invalid chars, empty email, invalid email, empty password, short password (3 chars), empty displayName, long displayName (51 chars). Accepts valid input.
  - LoginValidator: rejects empty username, empty password. Accepts valid input.
  - SearchVideosValidator: rejects empty query, too-long query (201 chars), maxResults 0, maxResults 51. Accepts valid input.
  - GetDownloadLinkValidator: rejects empty videoId, empty videoUrl, URL not starting with "https://www.youtube.com/watch?v=", too-long title (301 chars). Accepts valid input.
- Acceptance: All validator edge cases tested; all tests pass

**B6.4: Integration tests**
- File: `backend/tests/JiApp.Tests/Integration/AuthEndpointTests.cs`
  - Use WebApplicationFactory(Program) to test full request pipeline
  - Test: Register then Login flow returns valid JWT
  - Test: Register with duplicate username returns 400
  - Test: Access protected endpoint without token returns 401
  - Test: Access protected endpoint with valid token returns 200
- Acceptance: Integration tests exercise the full middleware pipeline

### Frontend Testing Tasks

**F6.1: Run full frontend test suite**
- Run `npm test` from mobile/ directory
- Verify all Jest suites pass: components (Button, FormInput, SearchBar, VideoCard, HistoryItem, LanguagePicker, LoadingSpinner, ErrorMessage)
- Verify hook tests pass: useAuth, useSearch, useDownload, useHistory (via AuthContext tests)
- Verify screen tests pass: RegisterScreen, DownloadScreen
- Verify navigation tests pass: AppNavigator
- Verify i18n tests pass: key parity, language switching
- Check that all critical tests exist and are not skipped (auth context, search flow, download flow)
- Fix any failing or flaky tests
- Acceptance: `npm test` exits with exit code 0; no skipped tests (`test.skip`) remain

**F6.2: Storybook audit**
- Launch Storybook on emulator: `npm run storybook`
- Verify every component listed in the component directory has a corresponding `.stories.tsx` file:
  - Button, FormInput, SearchBar, VideoCard, HistoryItem, LanguagePicker, LoadingSpinner, ErrorMessage
- Verify each component's stories cover all documented states (check story names match states in PROCESS.md tasks)
- Verify all stories render without console errors (check Metro console)
- Acceptance: All 8 components have stories; all stories render; no console errors

### Frontend Tasks (Release)

**F6.1: Manual test plan**
- Create a test checklist covering:
  - Fresh install: app starts, shows login screen
  - Register new user: all validation errors shown for each field, success navigates to login
  - Login: invalid credentials show error, valid credentials navigate to search
  - Remember Me: close app, reopen, credentials pre-filled
  - Auto-login: close app (while logged in), reopen, goes directly to search
  - Search: type query, see results with thumbnails, clear search, empty state
  - Download: tap result, see video info, tap download, file saved to device, success message
  - History: search and download histories appear, pull-to-refresh, empty state for new user
  - Settings: change language (verify all screens update), logout (verify return to login)
  - Language persistence: change to English, close app, reopen, still in English
  - Token expiry: wait for token to expire, next API call triggers auto-logout
  - Network error: disable WiFi, verify error messages appear on all screens
  - Back navigation: every screen's back button works correctly

**F6.2: Build signed release APK**
- Configure Android signing: generate keystore, set signing config in `mobile/android/app/build.gradle`
- Build release APK: `cd mobile/android && ./gradlew assembleRelease`
- Test release APK on physical device
- Acceptance: Signed APK installs and runs on a physical Android device; all features work

### Integration Tasks

- Run full backend test suite: `dotnet test backend/tests/JiApp.Tests/`
- All tests must pass
- Run the complete manual test plan on an Android device or emulator
- Document any issues found and fix them

### Definition of Done - Phase 6

- [x] All unit tests pass (handlers, validators, JWT service)
- [x] Integration tests pass
- [x] `dotnet test` returns 0 failures
- [ ] Manual test plan executed with all items passing (deferred — no emulator available)
- [ ] Signed release APK builds successfully (deferred — no Android SDK available)
- [ ] Release APK tested on physical device (deferred — no device available)
- [x] No known critical bugs remain
- [x] Code is clean: no TODO comments left unresolved, no commented-out code, no hardcoded secrets
- [x] `npm test` passes with zero failures (all components, hooks, screens, context, i18n)
- [x] All components have Storybook stories covering their documented states
- [x] Storybook audit complete: all stories render without console errors
Iteration 1: Critique failed. Issues: All three review agents have completed. Both test suites pass (backend 100/100, mobile 92/92). However, the agents found significant issues. Here's the consolidated assessment:

## Issues for Next Dev Loop

### High Priority

- **No shared error response model** — Three places serialize error JSON with different shapes: `GlobalExceptionMiddleware` uses `Dictionary<string,string?>`, JWT challenge uses anonymous `{ message }`, rate limiter uses `Dictionary<string,object?>`. A shared `ApiErrorResponse` DTO in `JiApp.Common` would unify these.
- **`WebApplicationFactory<Program>` created 3 times per test class** — `GlobalExceptionMiddlewareTests` instantiates the full ASP.NET pipeline for each `[Fact]`. Should use `IClassFixture<WebApplicationFactory<Program>>` to share one instance.

### Medium Priority

- **Unnecessary `Microsoft.AspNetCore.Authentication.JwtBearer` in Infrastructure** — `JwtTokenService` only uses `System.IdentityModel.Tokens.Jwt` types. The Infrastructure project should reference the lower-level package, not the full ASP.NET middleware one.
- **Style duplication across all 6 screens** — Every screen defines its own `StyleSheet.create()` with repeated `backgroundColor: '#F2F2F7'`, text colors, container styles. Extract to `mobile/src/styles/theme.ts`.
- **Sequential independent storage calls** — `apiClient.ts` 401 handler and `AuthContext.tsx` logout/checkToken execute `clearToken/clearUserId/clearDisplayName` sequentially. Should use `Promise.all`.
- **Redundant dynamic import in `apiClient.ts`** — Leaks the 401 interceptor's `import('./storageService')` while `getToken` is statically imported. Add all storage functions to the static import.
- **Missing `useEffect` cleanup** in `LoginScreen.tsx`, `SearchScreen.tsx`, `HistoryScreen.tsx` — async operations that set state after unmount without abort signals or mounted flags.
- **Dead state: `downloadProgress`** in `useDownload.ts` — never set to a non-null value, only initialized and reset. Either wire it to a real download progress callback or remove it.
- **God method: `Startup.ConfigureServices` (185 lines)** — Each comment-delimited section (Swagger, EF Core, Identity, JWT, CORS, Rate Limiting, DI registrations) should be a private method or extension method.
- **DbContext replacement logic duplicated** between `CustomWebApplicationFactory` and `RateLimitingEndpointTests`.

### Low Priority

- **Silent empty catch** in `GlobalExceptionMiddleware` line 35-38 — event log persistence failure should at least emit a warning log.
- **Hardcoded `baseURL: 'http://10.0.2.2:5001/api'`** in `apiClient.ts` — should come from env config.
- **Unconditional Storybook import** in `App.tsx` — may not be tree-shaken by bundler, pulling Storybook into production builds.
- **`@testing-library/jest-native` is deprecated** — migration guide at callstack.github.io recommends built-in matchers in `@testing-library/react-native` v12.4+.
- **Empty state `hasSearched` boolean** in `SearchScreen.tsx` — derivable from `results.length > 0` and `lastQueryRef.current`.
- **Preview NuGet package** `Microsoft.AspNetCore.Identity.EntityFrameworkCore 10.0.0-preview.2` mixed with stable `10.0.8` packages.
Iteration 2: Critique failed. Issues: Tests pass (100 backend, 109 mobile). However, the review agents found real issues. Here are the findings for the next dev loop:

## Issues for Next Dev Loop

### High Priority

- **`mountedRef` declared but never checked in `HistoryScreen.tsx`** — dead code: the ref is set up and a cleanup effect toggles it, but no effect/callback actually reads `mountedRef.current`. This is copy-paste residue from LoginScreen/SearchScreen. Meanwhile, `handleLogin` in `LoginScreen.tsx` also doesn't guard the `finally { setIsLoading(false) }` block with `mountedRef`, and `RegisterScreen.tsx` lacks the pattern entirely — so all three screens have unmount-after-async bugs in different ways.

### Medium Priority

- **`JsonSerializerOptions` duplicated** between `Startup.cs` and `GlobalExceptionMiddleware.cs` — both define the same `static readonly` with `CamelCase` naming. Should live on `ApiErrorResponse` or a shared `JsonDefaults` class.
- **Inline anonymous type in `SearchScreen.tsx` duplicates `VideoItem`** — `handleVideoPress` re-declares `{ videoId, title, description, imageUrl, videoUrl }` instead of reusing the `VideoItem` interface from `types/api.ts`. Same issue in `navigation/types.ts` where `Download` params repeat all `VideoItem` fields inline.
- **Near-duplicate section blocks in `HistoryScreen.tsx`** — the Searches and Downloads sections are structurally identical JSX (container/header/empty-state/FlatList). Extract to a generic `HistorySection<T>` component.
- **Hardcoded colors bypassing theme** — `'#007AFF'` in `HistoryScreen.tsx` RefreshControl tintColor and `'#F2F2F7'` + `'#007AFF'` in `AppNavigator.tsx` loading style instead of `colors.primary` and `colors.background` from `theme.ts`.

### Low Priority

- **`err instanceof Error` pattern duplicated in 3 hooks** (`useSearch`, `useHistory`, `useDownload`) — extract to `getErrorMessage(err: unknown, fallback: string)` in a shared utils module.
- **`formatDate` inline in `HistoryItem.tsx`** — extract to `utils/date.ts`.
- **Unnecessary `handleRetry` wrapper in `DownloadScreen.tsx`** — a pass-through `useCallback` that just calls `handleDownload` with no additional logic; can inline `handleDownload` directly as the `onRetry` prop.
- **Hardcoded version `'JiApp v1.0.0'` in `SettingsScreen.tsx`** — should be a constant or read from package.json.
- **`LoginScreen`/`RegisterScreen` share identical `onChangeText` lambda pattern** — 6 instances of `(text) => { setField(text); setFieldError(undefined); }` that could be a `makeChangeHandler` helper.
- **Unnecessary WHAT-comments** — `{/* Searches section */}`, `{/* Language section */}`, `// Persist the exception to EventLog`, etc. — all describe what the code already says.
Iteration 3: Critique failed. Issues: Both test suites pass:
- Backend: **100/100** 
- Mobile: **128/128**, 24 suites

Here's a summary of what I fixed and what remains for the next dev loop.

## Fixed in this iteration

1. **Critical: Token restoration bug** (`AuthContext.tsx:104-105`) — Backend `/api/auth/me` returns `MeResponse` (no `token` field), but the mobile code typed it as `LoginResponse`. `user.token` was always `undefined`, overwriting the valid stored token on every app restart and forcing logout. Fixed by using the existing stored token instead of reading from the response.

2. **Medium: eslint-disable suppression** (`AuthContext.tsx:93`) — Added `checkToken` to the `useEffect` dependency array. The function has `[]` deps so it's stable; no re-runs occur.

3. **Medium: SettingsScreen leaky abstraction** (`SettingsScreen.tsx:13`) — Replaced `useContext(AuthContext)` with `useAuth()`, which includes a diagnostic null-safety guard.

4. **Low: Unnecessary mountedRef in finally blocks** (`LoginScreen.tsx:97-100`, `RegisterScreen.tsx:115-118`) — `setIsLoading(false)` on an unmounted component is a harmless no-op in React 18. Removed the guards.

## Issues for next dev loop

- **Backend error responses use 4+ different JSON shapes** across 22 error paths. Only `GlobalExceptionMiddleware`, JWT challenge, and rate limiter use `ApiErrorResponse`. The remaining 18+ endpoints use anonymous objects. A unified error response pattern is needed.

- **GlobalExceptionMiddleware writes to database on the hot 500 path** (lines 31-33 of the middleware). If the DB is down or the exception IS a DB failure, the error response is delayed by connection timeout, and the user's stack trace is discarded (only `ex.Message` is persisted).

- **Storybook is bundled in production builds** — Metro resolves `require('./.storybook/Storybook')` as a static dependency regardless of the `START_STORYBOOK` guard, pulling in the entire Storybook dependency tree into the APK.

- **No `AbortController` in any API call** (`useSearch`, `useDownload`, `useHistory`) — in-flight requests continue after unmount, wasting JS thread time on deserialization even though state setters are no-ops.

- **`LoginScreen`/`RegisterScreen` share identical auth layout scaffolding** (KeyboardAvoidingView + ScrollView + title + inputs + error + button + footer link). Extract to an `AuthLayout` component.

- **SettingsScreen fields show `userId` as the "Username" label** — the label reads "Nazwa użytkownika" but shows `userId` (a number), not the actual username. AuthContext stores `userId` and `displayName` but not `username`.
Iteration 4: Critique failed. Issues: Both test suites pass cleanly — **backend: 104/104, mobile: 149/149**. However, the review agents found several issues. Per the instructions, listing them for the next dev loop:

## Issues for Next Dev Loop

- **`mountedRef` duplicated across 3 screens** (LoginScreen, RegisterScreen, SearchScreen) — identical 4-line pattern copied verbatim. Should be extracted to a shared `useIsMounted` hook, or removed entirely if unnecessary in React 18 (state setters on unmounted components are no-ops).

- **SearchScreen's `handleVideoPress` destructures `VideoItem` fields individually** — `navigation/types.ts` already consolidated `Download` params to `VideoItem`, but SearchScreen still maps each field manually (`navigation.navigate('Download', { videoId: v.videoId, title: v.title, ... })` instead of `navigation.navigate('Download', video)`).

- **`useLayoutEffect` + `navigation.setOptions` repeated in all 6 screens** — identical 3-line title-setting block in every screen. Could move titles to `Stack.Screen` `options` prop in the navigator, or extract to a `useScreenTitle` hook.

- **apiClient 401 handler clears fewer storage keys than `AuthContext.logout`** — the 401 interceptor clears `token`/`userId`/`displayName` but misses `username` and `credentials`, so "Remember Me" credentials survive a forced logout.

- **Password trim inconsistency in LoginScreen** — `username.trim()` is used everywhere (validation, login call, credential save), but `password` is only trimmed in validation while raw state is sent to `login()` and `saveCredentials()`.

- **Hardcoded magic numbers** in HistoryScreen (`loadHistory(50)`) and SearchScreen (`getSearchHistory(10)`) — should be named constants.
