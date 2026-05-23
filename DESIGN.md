# Project Specification: JiApp

## 1. Overview
**JiApp** is a high-performance, full-stack YouTube-to-MP3 downloader application. The system consists of a .NET 10 backend API and a React Native mobile application primarily targeting Android. It enables users to search YouTube, manage their search history via a local database, and download audio directly to their mobile devices.

## 2. Tech Stack
- **Backend:** ASP.NET Core API (.NET 10)
- **Frontend:** React Native (Targeting Android)
- **Database:** SQLite (Entity Framework Core)
- **Media Processing:** `yt-dlp.exe` and `ffmpeg.exe`
- **Development OS:** Windows 11

## 3. Architecture & Design Patterns
### 3.1 Vertical Slice Architecture (VSA)
The project is organized by **Features** rather than technical layers. Each feature folder is self-contained.
**Example Structure:**
`Features / Downloads / GetDownloadLink /`
- `GetDownloadLinkEndpoint.cs` (Minimal API Mapping)
- `GetDownloadLinkHandler.cs` (Business Logic)
- `GetDownloadLinkValidator.cs` (FluentValidation)
- `DownloadRequest.cs` (Public sealed record)
- `DownloadResponse.cs` (Public sealed record)

### 3.2 "Zero-Trust" Logic & Validation
- **Strict Immutability:** All Request/Response DTOs must be `public sealed record`.
- **Verify Everything:** No input is trusted. Every request must be validated via `FluentValidation` before processing.
- **Minimal APIs:** Use Minimal APIs over Controllers to maintain high cohesion within vertical slices and reduce overhead.

## 4. Project Naming Conventions
The solution uses a modular naming approach:
- `JiApp.Api`: The entry point and Minimal API definitions.
- `JiApp.Infrastructure`: SQLite DbContext, Repository implementations, and external process wrappers (`yt-dlp`).
- `JiApp.Common`: Shared abstractions, "Zero-Trust" base records, and global constants.
- `JiApp.YtApi`: Specialized logic for interacting with YouTube data.

## 5. Key Functionalities (MVP)
1. **Authentication:** Login page with "Remember Me" (encrypted password saving).
2. **YouTube Search:** Real-time querying of YouTube via the API.
3. **History:** SQLite integration to log and retrieve search/download history.
4. **MP3 Extraction:** - Backend invokes `yt-dlp.exe` to fetch streams.
   - `ffmpeg.exe` handles the conversion to high-quality MP3.
   - The API streams the result or provides a secure download link to the mobile client.

## 6. Implementation Notes for Windows 11
- Ensure `yt-dlp.exe` and `ffmpeg.exe` are bundled in the deployment directory or managed via environment variables.
- Use `System.Diagnostics.Process` with `RedirectStandardOutput` for process orchestration.

## 7. Frontend Specifics (React Native)
- Focus on Android-specific file system permissions for saving downloads.
- Implementation of a secure storage mechanism for user credentials.
"""