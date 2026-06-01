# JiApp Deployment Guide

## Backend (ASP.NET Core 10)

### Required Environment Variables

Set these before starting the API in production:

| Variable | Config Path | Description |
|---|---|---|
| `JWT_KEY` | `Jwt:Key` | Base64-encoded HMAC-SHA256 key (min 32 bytes before encoding) |
| `JWT_AUDIENCE` | `Jwt:Audience` | JWT `aud` claim — typically the production server URL |
| `CERT_PATH` | `Kestrel:Endpoints:Https:Certificate:Path` | Path to HTTPS PFX/PKCS#12 certificate file |
| `CERT_PASSWORD` | `Kestrel:Endpoints:Https:Certificate:Password` | Password for the PFX certificate |
| `Youtube__api-key` | `Youtube:api-key` | Google YouTube Data API v3 key |

Note: double-underscore (`__`) maps to colon (`:`) in ASP.NET Core's environment variable provider. Alternatively, use `Youtube:api-key` syntax if your process manager supports colons.

### Runtime

```sh
ASPNETCORE_ENVIRONMENT=Production dotnet run --project backend/src/JiApp.Api
```

- Listens on HTTP `*:6701` and HTTPS `*:6703`.
- SQLite database is created at `{AppContext.BaseDirectory}/JiApp.db` unless overridden via `ConnectionStrings:JiDb`.
- Swagger is disabled. HSTS is enabled. Security headers (X-Content-Type-Options, X-Frame-Options, Referrer-Policy) are always applied.

### Startup Validation

Startup will refuse with a clear error if:
- `Jwt:Key` is empty or still contains the `${` placeholder
- `Youtube:api-key` is empty or still contains the `${` placeholder

## Mobile (React Native Android)

### Build-Time Environment Variable

| Variable | Description |
|---|---|
| `JIAPP_API_URL` | Backend API base URL (e.g., `https://api.example.com/api`) |

This variable is inlined at build time via `babel-plugin-transform-inline-environment-variables`. It must be set when running the Metro bundler or building the release APK.

### Release APK

```sh
# Generate keystore and build the release APK
./build-apk.sh --release

# Or manually:
cd mobile
JIAPP_API_URL=https://api.example.com/api \
  npx react-native build-android --mode=release
```

The release build **requires** `android/app/keystore.properties` to exist. If missing, the build fails with a clear error. Use `build-apk.sh --release` to generate both the keystore and the properties file automatically.

### Network Security

- **Debug builds** trust user-installed CA certificates (for proxy debugging) and a custom CA for the emulator loopback address.
- **Release builds** use the system CA store only. Ensure the production server uses a certificate from a trusted CA.

### ProGuard

ProGuard is disabled for release builds (`enableProguardInReleaseBuilds = false`). To enable it, set `enableProguardInReleaseBuilds = true` in `android/app/build.gradle` and configure rules in `proguard-rules.pro`.

## Verification Checklist

- [ ] Backend starts without errors with `ASPNETCORE_ENVIRONMENT=Production`
- [ ] `GET /api/health` returns 200
- [ ] Registration and login work against the production backend
- [ ] JWT tokens are issued with the production issuer and audience
- [ ] HTTPS certificate is valid and trusted by client devices
- [ ] Release APK installs on a device and connects to the production API
- [ ] Release APK is signed with the production keystore (verify with `jarsigner -verify`)
