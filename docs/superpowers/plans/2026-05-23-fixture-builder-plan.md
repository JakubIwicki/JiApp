# FixtureBuilder Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Introduce a FixtureBuilder fluent pattern that replaces inline `.Setup()` calls in handler tests with `With_*` builder methods.

**Architecture:** Each handler gets a co-located Fixture class and Context record. The Fixture wraps existing `Mocks/*.GetSuccessful()` factories internally, exposes fluent `With_*` methods for Moq `.Setup()` coverage, and materializes the handler via `.Build()` which returns a Context record containing the handler and all mock objects. Test files are refactored to use the new pattern. Existing `Mocks/*.cs` factory files are unchanged.

**Tech Stack:** C# 12, xUnit, Moq, FluentAssertions, .NET 10

---

## File Structure

### Created (9 new files in `backend/tests/JiApp.Tests/Fixtures/`)

| File | Responsibility |
|------|---------------|
| `Fixtures/LoginHandlerFixture.cs` | Fixture + Context for LoginHandler |
| `Fixtures/RegisterHandlerFixture.cs` | Fixture + Context for RegisterHandler |
| `Fixtures/MeHandlerFixture.cs` | Fixture + Context for MeHandler |
| `Fixtures/DownloadFileHandlerFixture.cs` | Fixture + Context for DownloadFileHandler |
| `Fixtures/DownloadHistoryHandlerFixture.cs` | Fixture + Context for DownloadHistoryHandler |
| `Fixtures/SearchHistoryHandlerFixture.cs` | Fixture + Context for SearchHistoryHandler |
| `Fixtures/GetHistoryHandlerFixture.cs` | Fixture + Context for GetHistoryHandler |
| `Fixtures/SearchVideosHandlerFixture.cs` | Fixture + Context for SearchVideosHandler |
| `Fixtures/GetDownloadLinkHandlerFixture.cs` | Fixture + Context for GetDownloadLinkHandler |

### Modified (9 test files — same paths, imports changed, constructor removed, per-test logic uses fixtures)

### Unchanged

All files in `Mocks/`, all handler source files in `Features/`.

---

### Task 1: Create LoginHandlerFixture

**Files:**
- Create: `/home/jakub/JiApp/backend/tests/JiApp.Tests/Fixtures/LoginHandlerFixture.cs`

- [ ] **Step 1: Write the fixture file**

```csharp
using JiApp.Api.Features.Auth.Login;
using JiApp.Common.Models;
using JiApp.Infrastructure.Services;
using JiApp.Tests.Mocks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.Tests.Fixtures;

public sealed class LoginHandlerFixture
{
    private readonly Mock<UserManager<User>> _userManagerMock = UserManagerMock.GetSuccessful();
    private readonly Mock<SignInManager<User>> _signInManagerMock;
    private readonly Mock<IJwtTokenService> _jwtTokenServiceMock = JwtTokenServiceMock.GetSuccessful();

    public LoginHandlerFixture()
    {
        _signInManagerMock = SignInManagerMock.GetSuccessful(_userManagerMock);
    }

    public LoginHandlerFixture WithFindByNameAsync(string username, User? user)
    {
        _userManagerMock.Setup(x => x.FindByNameAsync(username)).ReturnsAsync(user);
        return this;
    }

    public LoginHandlerFixture WithAnyFindByNameAsync(User? user)
    {
        _userManagerMock.Setup(x => x.FindByNameAsync(It.IsAny<string>())).ReturnsAsync(user);
        return this;
    }

    public LoginHandlerFixture WithCheckPasswordSignInAsync(User user, string password, SignInResult result)
    {
        _signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(user, password, true)).ReturnsAsync(result);
        return this;
    }

    public LoginHandlerFixture WithGenerateToken(long userId, string username, string token)
    {
        _jwtTokenServiceMock.Setup(x => x.GenerateToken(userId, username)).Returns(token);
        return this;
    }

    public LoginHandlerContext Build()
    {
        var handler = new LoginHandler(
            _signInManagerMock.Object,
            _jwtTokenServiceMock.Object,
            LoggerMock.Of<LoginHandler>());
        return new LoginHandlerContext(
            handler,
            _userManagerMock,
            _signInManagerMock,
            _jwtTokenServiceMock);
    }
}

public sealed record LoginHandlerContext(
    LoginHandler Handler,
    Mock<UserManager<User>> UserManagerMock,
    Mock<SignInManager<User>> SignInManagerMock,
    Mock<IJwtTokenService> JwtTokenServiceMock);
```

- [ ] **Step 2: Verify file compiles**

Run: `dotnet build backend/JiApp.sln`
Expected: Build succeeds (test project compiles the new fixture, even though no tests use it yet).

- [ ] **Step 3: Commit**

```bash
git add backend/tests/JiApp.Tests/Fixtures/LoginHandlerFixture.cs
git commit -m "feat(tests): add LoginHandlerFixture + LoginHandlerContext"
```

---

### Task 2: Refactor LoginHandlerTests to use fixture

**Files:**
- Modify: `/home/jakub/JiApp/backend/tests/JiApp.Tests/Features/Auth/LoginHandlerTests.cs` (full rewrite)

- [ ] **Step 1: Replace the test file content**

Replace the entire file with:

```csharp
using System.Threading.Tasks;
using FluentAssertions;
using JiApp.Api.Features.Auth.Login;
using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Tests.Fixtures;
using Microsoft.AspNetCore.Identity;
using Xunit;

namespace JiApp.Tests.Features.Auth;

public class LoginHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithValidCredentials_ReturnsSuccessWithToken()
    {
        var user = new User { Id = 1, UserName = "testuser", DisplayName = "Test User" };
        var ctx = new LoginHandlerFixture()
            .WithFindByNameAsync("testuser", user)
            .WithCheckPasswordSignInAsync(user, "correctpassword", SignInResult.Success)
            .WithGenerateToken(1, "testuser", "jwt-token-123")
            .Build();

        var result = await ctx.Handler.HandleAsync(new LoginRequest("testuser", "correctpassword"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(1);
        result.Value.DisplayName.Should().Be("Test User");
        result.Value.Token.Should().Be("jwt-token-123");
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_WithInvalidUsername_ReturnsFailure()
    {
        var ctx = new LoginHandlerFixture()
            .WithAnyFindByNameAsync(null)
            .Build();

        var result = await ctx.Handler.HandleAsync(new LoginRequest("nonexistent", "password"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid username or password");
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_WithInvalidPassword_ReturnsFailure()
    {
        var user = new User { Id = 1, UserName = "testuser", DisplayName = "Test User" };
        var ctx = new LoginHandlerFixture()
            .WithFindByNameAsync("testuser", user)
            .WithCheckPasswordSignInAsync(user, "wrongpassword", SignInResult.Failed)
            .Build();

        var result = await ctx.Handler.HandleAsync(new LoginRequest("testuser", "wrongpassword"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid username or password");
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_WhenAccountLocked_ReturnsFailureWithLockedMessage()
    {
        var user = new User { Id = 1, UserName = "testuser", DisplayName = "Test User" };
        var ctx = new LoginHandlerFixture()
            .WithFindByNameAsync("testuser", user)
            .WithCheckPasswordSignInAsync(user, "password", SignInResult.LockedOut)
            .Build();

        var result = await ctx.Handler.HandleAsync(new LoginRequest("testuser", "password"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Account is locked. Please try again later.");
        result.Value.Should().BeNull();
    }
}
```

Removed imports: `JiApp.Infrastructure.Services`, `JiApp.Tests.Mocks`, `Microsoft.Extensions.Logging`, `Moq`
Added import: `JiApp.Tests.Fixtures`

- [ ] **Step 2: Build and run LoginHandler tests**

Run: `dotnet test backend/tests/JiApp.Tests/ --filter "FullyQualifiedName~LoginHandlerTests" --no-restore`
Expected: 4 passed, 0 failed

- [ ] **Step 3: Commit**

```bash
git add backend/tests/JiApp.Tests/Features/Auth/LoginHandlerTests.cs
git commit -m "refactor(tests): use LoginHandlerFixture in LoginHandlerTests"
```

---

### Task 3: Create RegisterHandlerFixture

**Files:**
- Create: `/home/jakub/JiApp/backend/tests/JiApp.Tests/Fixtures/RegisterHandlerFixture.cs`

- [ ] **Step 1: Write the fixture file**

```csharp
using JiApp.Api.Features.Auth.Register;
using JiApp.Common.Models;
using JiApp.Tests.Mocks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.Tests.Fixtures;

public sealed class RegisterHandlerFixture
{
    private readonly Mock<UserManager<User>> _userManagerMock = UserManagerMock.GetSuccessful();

    public RegisterHandlerFixture WithFindByNameAsync(string username, User? user)
    {
        _userManagerMock.Setup(x => x.FindByNameAsync(username)).ReturnsAsync(user);
        return this;
    }

    public RegisterHandlerFixture WithAnyFindByNameAsync(User? user)
    {
        _userManagerMock.Setup(x => x.FindByNameAsync(It.IsAny<string>())).ReturnsAsync(user);
        return this;
    }

    public RegisterHandlerFixture WithFindByEmailAsync(string email, User? user)
    {
        _userManagerMock.Setup(x => x.FindByEmailAsync(email)).ReturnsAsync(user);
        return this;
    }

    public RegisterHandlerFixture WithAnyFindByEmailAsync(User? user)
    {
        _userManagerMock.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
        return this;
    }

    public RegisterHandlerFixture WithAnyCreateAsync(IdentityResult result)
    {
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>())).ReturnsAsync(result);
        return this;
    }

    public RegisterHandlerContext Build()
    {
        var handler = new RegisterHandler(
            _userManagerMock.Object,
            LoggerMock.Of<RegisterHandler>());
        return new RegisterHandlerContext(handler, _userManagerMock);
    }
}

public sealed record RegisterHandlerContext(
    RegisterHandler Handler,
    Mock<UserManager<User>> UserManagerMock);
```

- [ ] **Step 2: Build**

Run: `dotnet build backend/JiApp.sln`
Expected: Build succeeds

- [ ] **Step 3: Commit**

```bash
git add backend/tests/JiApp.Tests/Fixtures/RegisterHandlerFixture.cs
git commit -m "feat(tests): add RegisterHandlerFixture + RegisterHandlerContext"
```

---

### Task 4: Refactor RegisterHandlerTests to use fixture

**Files:**
- Modify: `/home/jakub/JiApp/backend/tests/JiApp.Tests/Features/Auth/RegisterHandlerTests.cs` (full rewrite)

- [ ] **Step 1: Replace the test file content**

Replace the entire file with:

```csharp
using System.Threading.Tasks;
using FluentAssertions;
using JiApp.Api.Features.Auth.Register;
using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Tests.Fixtures;
using Microsoft.AspNetCore.Identity;
using Xunit;

namespace JiApp.Tests.Features.Auth;

public class RegisterHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithValidRequest_ReturnsSuccess()
    {
        var ctx = new RegisterHandlerFixture()
            .WithAnyFindByNameAsync(null)
            .WithAnyFindByEmailAsync(null)
            .WithAnyCreateAsync(IdentityResult.Success)
            .Build();

        var result = await ctx.Handler.HandleAsync(
            new RegisterRequest("testuser", "test@example.com", "password", "Test User"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_WithDuplicateUsername_ReturnsFailure()
    {
        var existingUser = new User { UserName = "existing", Email = "existing@example.com" };
        var ctx = new RegisterHandlerFixture()
            .WithFindByNameAsync("existing", existingUser)
            .Build();

        var result = await ctx.Handler.HandleAsync(
            new RegisterRequest("existing", "new@example.com", "password", "Existing User"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Username already taken");
    }

    [Fact]
    public async Task HandleAsync_WithDuplicateEmail_ReturnsFailure()
    {
        var existingUser = new User { UserName = "other", Email = "test@example.com" };
        var ctx = new RegisterHandlerFixture()
            .WithAnyFindByNameAsync(null)
            .WithFindByEmailAsync("test@example.com", existingUser)
            .Build();

        var result = await ctx.Handler.HandleAsync(
            new RegisterRequest("newuser", "test@example.com", "password", "New User"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Email already taken");
    }
}
```

Removed imports: `System`, `JiApp.Tests.Mocks`, `Microsoft.Extensions.Logging`, `Moq`
Added import: `JiApp.Tests.Fixtures`

- [ ] **Step 2: Build and run RegisterHandler tests**

Run: `dotnet test backend/tests/JiApp.Tests/ --filter "FullyQualifiedName~RegisterHandlerTests" --no-restore`
Expected: 3 passed, 0 failed

- [ ] **Step 3: Commit**

```bash
git add backend/tests/JiApp.Tests/Features/Auth/RegisterHandlerTests.cs
git commit -m "refactor(tests): use RegisterHandlerFixture in RegisterHandlerTests"
```

---

### Task 5: Create MeHandlerFixture

**Files:**
- Create: `/home/jakub/JiApp/backend/tests/JiApp.Tests/Fixtures/MeHandlerFixture.cs`

- [ ] **Step 1: Write the fixture file**

```csharp
using JiApp.Api.Features.Auth.Me;
using JiApp.Common.Models;
using JiApp.Infrastructure.Services;
using JiApp.Tests.Mocks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.Tests.Fixtures;

public sealed class MeHandlerFixture
{
    private readonly Mock<UserManager<User>> _userManagerMock = UserManagerMock.GetSuccessful();
    private readonly Mock<ICurrentUserService> _currentUserMock;

    public MeHandlerFixture()
    {
        _currentUserMock = CurrentUserServiceMock.GetSuccessful();
    }

    public MeHandlerFixture WithUserId(long userId)
    {
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
        return this;
    }

    public MeHandlerFixture WithFindByIdAsync(string userId, User? user)
    {
        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
        return this;
    }

    public MeHandlerContext Build()
    {
        var handler = new MeHandler(
            _userManagerMock.Object,
            _currentUserMock.Object,
            LoggerMock.Of<MeHandler>());
        return new MeHandlerContext(handler, _userManagerMock, _currentUserMock);
    }
}

public sealed record MeHandlerContext(
    MeHandler Handler,
    Mock<UserManager<User>> UserManagerMock,
    Mock<ICurrentUserService> CurrentUserMock);
```

- [ ] **Step 2: Build**

Run: `dotnet build backend/JiApp.sln`
Expected: Build succeeds

- [ ] **Step 3: Commit**

```bash
git add backend/tests/JiApp.Tests/Fixtures/MeHandlerFixture.cs
git commit -m "feat(tests): add MeHandlerFixture + MeHandlerContext"
```

---

### Task 6: Refactor MeHandlerTests to use fixture

**Files:**
- Modify: `/home/jakub/JiApp/backend/tests/JiApp.Tests/Features/Auth/MeHandlerTests.cs` (full rewrite)

- [ ] **Step 1: Replace the test file content**

Replace the entire file with:

```csharp
using System.Globalization;
using System.Threading.Tasks;
using FluentAssertions;
using JiApp.Api.Features.Auth.Me;
using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Tests.Fixtures;
using Xunit;

namespace JiApp.Tests.Features.Auth;

public class MeHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithValidToken_ReturnsUserData()
    {
        const long userId = 42L;
        var user = new User { Id = userId, UserName = "janek", DisplayName = "Jan Kowalski" };
        var ctx = new MeHandlerFixture()
            .WithUserId(userId)
            .WithFindByIdAsync(userId.ToString(CultureInfo.InvariantCulture), user)
            .Build();

        var result = await ctx.Handler.HandleAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(42);
        result.Value.DisplayName.Should().Be("Jan Kowalski");
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_WhenUserNotFound_ReturnsFailure()
    {
        const long userId = 999L;
        var ctx = new MeHandlerFixture()
            .WithUserId(userId)
            .WithFindByIdAsync(userId.ToString(CultureInfo.InvariantCulture), null)
            .Build();

        var result = await ctx.Handler.HandleAsync();

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("User not found");
        result.Value.Should().BeNull();
    }
}
```

Removed imports: `System`, `JiApp.Tests.Mocks`, `Microsoft.AspNetCore.Identity`, `Microsoft.Extensions.Logging`, `Moq`
Added import: `JiApp.Tests.Fixtures`

- [ ] **Step 2: Build and run MeHandler tests**

Run: `dotnet test backend/tests/JiApp.Tests/ --filter "FullyQualifiedName~MeHandlerTests" --no-restore`
Expected: 2 passed, 0 failed

- [ ] **Step 3: Commit**

```bash
git add backend/tests/JiApp.Tests/Features/Auth/MeHandlerTests.cs
git commit -m "refactor(tests): use MeHandlerFixture in MeHandlerTests"
```

---

### Task 7: Create DownloadFileHandlerFixture

**Files:**
- Create: `/home/jakub/JiApp/backend/tests/JiApp.Tests/Fixtures/DownloadFileHandlerFixture.cs`

- [ ] **Step 1: Write the fixture file**

```csharp
using JiApp.Api.Features.Downloads.DownloadFile;
using JiApp.Infrastructure.Services;
using JiApp.Tests.Mocks;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.Tests.Fixtures;

public sealed class DownloadFileHandlerFixture
{
    private readonly Mock<ITempFileStore> _tempFileStoreMock = TempFileStoreMock.GetSuccessful();
    private readonly Mock<ICurrentUserService> _currentUserMock;

    public DownloadFileHandlerFixture()
    {
        _currentUserMock = CurrentUserServiceMock.GetSuccessful();
    }

    public DownloadFileHandlerFixture WithUserId(long userId)
    {
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
        return this;
    }

    public DownloadFileHandlerFixture WithGet(string id, long userId, string? filePath)
    {
        _tempFileStoreMock.Setup(x => x.Get(id, userId)).Returns(filePath);
        return this;
    }

    public DownloadFileHandlerContext Build()
    {
        var handler = new DownloadFileHandler(
            _tempFileStoreMock.Object,
            _currentUserMock.Object,
            LoggerMock.Of<DownloadFileHandler>());
        return new DownloadFileHandlerContext(handler, _tempFileStoreMock, _currentUserMock);
    }
}

public sealed record DownloadFileHandlerContext(
    DownloadFileHandler Handler,
    Mock<ITempFileStore> TempFileStoreMock,
    Mock<ICurrentUserService> CurrentUserMock);
```

- [ ] **Step 2: Build**

Run: `dotnet build backend/JiApp.sln`
Expected: Build succeeds

- [ ] **Step 3: Commit**

```bash
git add backend/tests/JiApp.Tests/Fixtures/DownloadFileHandlerFixture.cs
git commit -m "feat(tests): add DownloadFileHandlerFixture + DownloadFileHandlerContext"
```

---

### Task 8: Refactor DownloadFileHandlerTests to use fixture

**Files:**
- Modify: `/home/jakub/JiApp/backend/tests/JiApp.Tests/Features/Downloads/DownloadFileHandlerTests.cs` (full rewrite)

- [ ] **Step 1: Replace the test file content**

Replace the entire file with:

```csharp
using FluentAssertions;
using JiApp.Api.Features.Downloads.DownloadFile;
using JiApp.Tests.Fixtures;
using Xunit;

namespace JiApp.Tests.Features.Downloads;

public class DownloadFileHandlerTests
{
    [Fact]
    public void Handle_WithValidIdAndOwnedFile_ReturnsFilePath()
    {
        var ctx = new DownloadFileHandlerFixture()
            .WithGet("valid-id", 1L, "/tmp/ji_app/YtMp3_1/song.mp3")
            .Build();

        var result = ctx.Handler.Handle("valid-id");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("/tmp/ji_app/YtMp3_1/song.mp3");
    }

    [Fact]
    public void Handle_WithExpiredId_ReturnsFailure()
    {
        var ctx = new DownloadFileHandlerFixture()
            .WithGet("expired-id", 1L, null)
            .Build();

        var result = ctx.Handler.Handle("expired-id");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Handle_WhenFileNotOwnedByUser_ReturnsFailure()
    {
        var ctx = new DownloadFileHandlerFixture()
            .WithGet("other-user-file", 1L, null)
            .Build();

        var result = ctx.Handler.Handle("other-user-file");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }
}
```

Removed imports: `JiApp.Infrastructure.Services`, `JiApp.Tests.Mocks`, `Microsoft.Extensions.Logging`, `Moq`
Added import: `JiApp.Tests.Fixtures`
Removed: constructor + field-level mocks

- [ ] **Step 2: Build and run DownloadFileHandler tests**

Run: `dotnet test backend/tests/JiApp.Tests/ --filter "FullyQualifiedName~DownloadFileHandlerTests" --no-restore`
Expected: 3 passed, 0 failed

- [ ] **Step 3: Commit**

```bash
git add backend/tests/JiApp.Tests/Features/Downloads/DownloadFileHandlerTests.cs
git commit -m "refactor(tests): use DownloadFileHandlerFixture in DownloadFileHandlerTests"
```

---

### Task 9: Create DownloadHistoryHandlerFixture

**Files:**
- Create: `/home/jakub/JiApp/backend/tests/JiApp.Tests/Fixtures/DownloadHistoryHandlerFixture.cs`

- [ ] **Step 1: Write the fixture file**

```csharp
using JiApp.Api.Features.Downloads.DownloadHistory;
using JiApp.Common.Models;
using JiApp.Infrastructure.Repositories;
using JiApp.Infrastructure.Services;
using JiApp.Tests.Mocks;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.Tests.Fixtures;

public sealed class DownloadHistoryHandlerFixture
{
    private readonly Mock<IDownloadHistoryRepository> _downloadHistoryRepoMock = DownloadHistoryRepositoryMock.GetSuccessful();
    private readonly Mock<ICurrentUserService> _currentUserMock;

    public DownloadHistoryHandlerFixture()
    {
        _currentUserMock = CurrentUserServiceMock.GetSuccessful();
    }

    public DownloadHistoryHandlerFixture WithGetByUserIdAsync(long userId, int limit, IReadOnlyList<YoutubeDownloadHistory> result, int offset = 0)
    {
        _downloadHistoryRepoMock.Setup(x => x.GetByUserIdAsync(userId, limit, offset)).ReturnsAsync(result);
        return this;
    }

    public DownloadHistoryHandlerContext Build()
    {
        var handler = new DownloadHistoryHandler(
            _downloadHistoryRepoMock.Object,
            _currentUserMock.Object,
            LoggerMock.Of<DownloadHistoryHandler>());
        return new DownloadHistoryHandlerContext(handler, _downloadHistoryRepoMock, _currentUserMock);
    }
}

public sealed record DownloadHistoryHandlerContext(
    DownloadHistoryHandler Handler,
    Mock<IDownloadHistoryRepository> DownloadHistoryRepoMock,
    Mock<ICurrentUserService> CurrentUserMock);
```

- [ ] **Step 2: Build**

Run: `dotnet build backend/JiApp.sln`
Expected: Build succeeds

- [ ] **Step 3: Commit**

```bash
git add backend/tests/JiApp.Tests/Fixtures/DownloadHistoryHandlerFixture.cs
git commit -m "feat(tests): add DownloadHistoryHandlerFixture + DownloadHistoryHandlerContext"
```

---

### Task 10: Refactor DownloadHistoryHandlerTests to use fixture

**Files:**
- Modify: `/home/jakub/JiApp/backend/tests/JiApp.Tests/Features/Downloads/DownloadHistoryHandlerTests.cs` (full rewrite)

- [ ] **Step 1: Replace the test file content**

Replace the entire file with:

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using JiApp.Api.Features.Downloads.DownloadHistory;
using JiApp.Common.Models;
using JiApp.Tests.Fixtures;
using Xunit;

namespace JiApp.Tests.Features.Downloads;

public class DownloadHistoryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithHistory_ReturnsUserDownloadHistory()
    {
        var history = new List<YoutubeDownloadHistory>
        {
            new()
            {
                Id = 1,
                UserId = 1L,
                VideoTitle = "test video one",
                VideoId = "abc123",
                VideoUrl = "https://youtube.com/watch?v=abc123",
                DownloadedAt = new DateTime(2026, 5, 20, 10, 0, 0, DateTimeKind.Utc)
            },
            new()
            {
                Id = 2,
                UserId = 1L,
                VideoTitle = "test video two",
                VideoId = "def456",
                VideoUrl = "https://youtube.com/watch?v=def456",
                DownloadedAt = new DateTime(2026, 5, 20, 9, 0, 0, DateTimeKind.Utc)
            }
        }.AsReadOnly();

        var ctx = new DownloadHistoryHandlerFixture()
            .WithGetByUserIdAsync(1L, 10, history)
            .Build();

        var result = await ctx.Handler.HandleAsync(new DownloadHistoryRequest(null));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items[0].Id.Should().Be(1);
        result.Value.Items[0].VideoTitle.Should().Be("test video one");
        result.Value.Items[0].VideoId.Should().Be("abc123");
        result.Value.Items[0].VideoUrl.Should().Be("https://youtube.com/watch?v=abc123");
        result.Value.Items[1].Id.Should().Be(2);
        result.Value.Items[1].VideoTitle.Should().Be("test video two");
        result.Value.Items[1].VideoId.Should().Be("def456");
        result.Value.Items[1].VideoUrl.Should().Be("https://youtube.com/watch?v=def456");
    }

    [Fact]
    public async Task HandleAsync_WithLimit_ReturnsLimitedHistory()
    {
        var history = new List<YoutubeDownloadHistory>
        {
            new()
            {
                Id = 1,
                UserId = 1L,
                VideoTitle = "only one",
                VideoId = "xyz789",
                VideoUrl = "https://youtube.com/watch?v=xyz789",
                DownloadedAt = DateTime.UtcNow
            }
        }.AsReadOnly();

        var ctx = new DownloadHistoryHandlerFixture()
            .WithGetByUserIdAsync(1L, 5, history)
            .Build();

        var result = await ctx.Handler.HandleAsync(new DownloadHistoryRequest(5));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task HandleAsync_WithEmptyHistory_ReturnsEmptyList()
    {
        var emptyHistory = new List<YoutubeDownloadHistory>().AsReadOnly();

        var ctx = new DownloadHistoryHandlerFixture()
            .WithGetByUserIdAsync(1L, 10, emptyHistory)
            .Build();

        var result = await ctx.Handler.HandleAsync(new DownloadHistoryRequest(null));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().BeEmpty();
    }
}
```

Removed imports: `JiApp.Infrastructure.Repositories`, `JiApp.Tests.Mocks`, `Microsoft.Extensions.Logging`, `Moq`
Added import: `JiApp.Tests.Fixtures`
Removed: constructor + field-level mocks

- [ ] **Step 2: Build and run DownloadHistoryHandler tests**

Run: `dotnet test backend/tests/JiApp.Tests/ --filter "FullyQualifiedName~DownloadHistoryHandlerTests" --no-restore`
Expected: 3 passed, 0 failed

- [ ] **Step 3: Commit**

```bash
git add backend/tests/JiApp.Tests/Features/Downloads/DownloadHistoryHandlerTests.cs
git commit -m "refactor(tests): use DownloadHistoryHandlerFixture in DownloadHistoryHandlerTests"
```

---

### Task 11: Create SearchHistoryHandlerFixture

**Files:**
- Create: `/home/jakub/JiApp/backend/tests/JiApp.Tests/Fixtures/SearchHistoryHandlerFixture.cs`

- [ ] **Step 1: Write the fixture file**

```csharp
using JiApp.Api.Features.Search.SearchHistory;
using JiApp.Common.Models;
using JiApp.Infrastructure.Repositories;
using JiApp.Infrastructure.Services;
using JiApp.Tests.Mocks;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.Tests.Fixtures;

public sealed class SearchHistoryHandlerFixture
{
    private readonly Mock<ISearchHistoryRepository> _searchHistoryRepoMock = SearchHistoryRepositoryMock.GetSuccessful();
    private readonly Mock<ICurrentUserService> _currentUserMock;

    public SearchHistoryHandlerFixture()
    {
        _currentUserMock = CurrentUserServiceMock.GetSuccessful();
    }

    public SearchHistoryHandlerFixture WithGetByUserIdAsync(long userId, int limit, IReadOnlyList<YoutubeSearchHistory> result, int offset = 0)
    {
        _searchHistoryRepoMock.Setup(x => x.GetByUserIdAsync(userId, limit, offset)).ReturnsAsync(result);
        return this;
    }

    public SearchHistoryHandlerContext Build()
    {
        var handler = new SearchHistoryHandler(
            _searchHistoryRepoMock.Object,
            _currentUserMock.Object,
            LoggerMock.Of<SearchHistoryHandler>());
        return new SearchHistoryHandlerContext(handler, _searchHistoryRepoMock, _currentUserMock);
    }
}

public sealed record SearchHistoryHandlerContext(
    SearchHistoryHandler Handler,
    Mock<ISearchHistoryRepository> SearchHistoryRepoMock,
    Mock<ICurrentUserService> CurrentUserMock);
```

- [ ] **Step 2: Build**

Run: `dotnet build backend/JiApp.sln`
Expected: Build succeeds

- [ ] **Step 3: Commit**

```bash
git add backend/tests/JiApp.Tests/Fixtures/SearchHistoryHandlerFixture.cs
git commit -m "feat(tests): add SearchHistoryHandlerFixture + SearchHistoryHandlerContext"
```

---

### Task 12: Refactor SearchHistoryHandlerTests to use fixture

**Files:**
- Modify: `/home/jakub/JiApp/backend/tests/JiApp.Tests/Features/Search/SearchHistoryHandlerTests.cs` (full rewrite)

- [ ] **Step 1: Replace the test file content**

Replace the entire file with:

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using JiApp.Api.Features.Search.SearchHistory;
using JiApp.Common.Models;
using JiApp.Tests.Fixtures;
using Xunit;

namespace JiApp.Tests.Features.Search;

public class SearchHistoryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithHistory_ReturnsUserSearchHistory()
    {
        var history = new List<YoutubeSearchHistory>
        {
            new()
            {
                Id = 1,
                UserId = 1L,
                SearchText = "first query",
                SearchedAt = new DateTime(2026, 5, 20, 10, 0, 0, DateTimeKind.Utc)
            },
            new()
            {
                Id = 2,
                UserId = 1L,
                SearchText = "second query",
                SearchedAt = new DateTime(2026, 5, 20, 9, 0, 0, DateTimeKind.Utc)
            }
        }.AsReadOnly();

        var ctx = new SearchHistoryHandlerFixture()
            .WithGetByUserIdAsync(1L, 10, history)
            .Build();

        var result = await ctx.Handler.HandleAsync(new SearchHistoryRequest(null));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items[0].Id.Should().Be(1);
        result.Value.Items[0].SearchText.Should().Be("first query");
        result.Value.Items[1].Id.Should().Be(2);
        result.Value.Items[1].SearchText.Should().Be("second query");
    }

    [Fact]
    public async Task HandleAsync_WithLimit_ReturnsLimitedHistory()
    {
        var history = new List<YoutubeSearchHistory>
        {
            new()
            {
                Id = 1,
                UserId = 1L,
                SearchText = "only one",
                SearchedAt = DateTime.UtcNow
            }
        }.AsReadOnly();

        var ctx = new SearchHistoryHandlerFixture()
            .WithGetByUserIdAsync(1L, 5, history)
            .Build();

        var result = await ctx.Handler.HandleAsync(new SearchHistoryRequest(5));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task HandleAsync_WithEmptyHistory_ReturnsEmptyList()
    {
        var emptyHistory = new List<YoutubeSearchHistory>().AsReadOnly();

        var ctx = new SearchHistoryHandlerFixture()
            .WithGetByUserIdAsync(1L, 10, emptyHistory)
            .Build();

        var result = await ctx.Handler.HandleAsync(new SearchHistoryRequest(null));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().BeEmpty();
    }
}
```

Removed imports: `JiApp.Infrastructure.Repositories`, `JiApp.Tests.Mocks`, `Moq`
Added import: `JiApp.Tests.Fixtures`
Removed: constructor + field-level mocks

- [ ] **Step 2: Build and run SearchHistoryHandler tests**

Run: `dotnet test backend/tests/JiApp.Tests/ --filter "FullyQualifiedName~SearchHistoryHandlerTests" --no-restore`
Expected: 3 passed, 0 failed

- [ ] **Step 3: Commit**

```bash
git add backend/tests/JiApp.Tests/Features/Search/SearchHistoryHandlerTests.cs
git commit -m "refactor(tests): use SearchHistoryHandlerFixture in SearchHistoryHandlerTests"
```

---

### Task 13: Create GetHistoryHandlerFixture

**Files:**
- Create: `/home/jakub/JiApp/backend/tests/JiApp.Tests/Fixtures/GetHistoryHandlerFixture.cs`

- [ ] **Step 1: Write the fixture file**

```csharp
using JiApp.Api.Features.History.GetHistory;
using JiApp.Common.Models;
using JiApp.Infrastructure.Repositories;
using JiApp.Infrastructure.Services;
using JiApp.Tests.Mocks;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.Tests.Fixtures;

public sealed class GetHistoryHandlerFixture
{
    private readonly Mock<ISearchHistoryRepository> _searchHistoryRepoMock = SearchHistoryRepositoryMock.GetSuccessful();
    private readonly Mock<IDownloadHistoryRepository> _downloadHistoryRepoMock = DownloadHistoryRepositoryMock.GetSuccessful();
    private readonly Mock<ICurrentUserService> _currentUserMock;

    public GetHistoryHandlerFixture()
    {
        _currentUserMock = CurrentUserServiceMock.GetSuccessful();
    }

    public GetHistoryHandlerFixture WithSearchGetByUserIdAsync(long userId, int limit, IReadOnlyList<YoutubeSearchHistory> result, int offset = 0)
    {
        _searchHistoryRepoMock.Setup(x => x.GetByUserIdAsync(userId, limit, offset)).ReturnsAsync(result);
        return this;
    }

    public GetHistoryHandlerFixture WithSearchGetByUserIdAsync_Throws(long userId, int limit, Exception exception, int offset = 0)
    {
        _searchHistoryRepoMock.Setup(x => x.GetByUserIdAsync(userId, limit, offset)).ThrowsAsync(exception);
        return this;
    }

    public GetHistoryHandlerFixture WithDownloadGetByUserIdAsync(long userId, int limit, IReadOnlyList<YoutubeDownloadHistory> result, int offset = 0)
    {
        _downloadHistoryRepoMock.Setup(x => x.GetByUserIdAsync(userId, limit, offset)).ReturnsAsync(result);
        return this;
    }

    public GetHistoryHandlerFixture WithDownloadGetByUserIdAsync_Throws(long userId, int limit, Exception exception, int offset = 0)
    {
        _downloadHistoryRepoMock.Setup(x => x.GetByUserIdAsync(userId, limit, offset)).ThrowsAsync(exception);
        return this;
    }

    public GetHistoryHandlerContext Build()
    {
        var handler = new GetHistoryHandler(
            _searchHistoryRepoMock.Object,
            _downloadHistoryRepoMock.Object,
            _currentUserMock.Object,
            LoggerMock.Of<GetHistoryHandler>());
        return new GetHistoryHandlerContext(handler, _searchHistoryRepoMock, _downloadHistoryRepoMock, _currentUserMock);
    }
}

public sealed record GetHistoryHandlerContext(
    GetHistoryHandler Handler,
    Mock<ISearchHistoryRepository> SearchHistoryRepoMock,
    Mock<IDownloadHistoryRepository> DownloadHistoryRepoMock,
    Mock<ICurrentUserService> CurrentUserMock);
```

- [ ] **Step 2: Build**

Run: `dotnet build backend/JiApp.sln`
Expected: Build succeeds

- [ ] **Step 3: Commit**

```bash
git add backend/tests/JiApp.Tests/Fixtures/GetHistoryHandlerFixture.cs
git commit -m "feat(tests): add GetHistoryHandlerFixture + GetHistoryHandlerContext"
```

---

### Task 14: Refactor GetHistoryHandlerTests to use fixture

**Files:**
- Modify: `/home/jakub/JiApp/backend/tests/JiApp.Tests/Features/History/GetHistoryHandlerTests.cs` (full rewrite)

- [ ] **Step 1: Replace the test file content**

Replace the entire file with:

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using JiApp.Api.Features.History.GetHistory;
using JiApp.Common.Models;
using JiApp.Tests.Fixtures;
using Xunit;

namespace JiApp.Tests.Features.History;

public class GetHistoryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithHistory_ReturnsBothSearchesAndDownloads()
    {
        var searches = new List<YoutubeSearchHistory>
        {
            new()
            {
                Id = 1,
                UserId = 1L,
                SearchText = "test query",
                SearchedAt = new DateTime(2026, 5, 20, 10, 0, 0, DateTimeKind.Utc)
            }
        }.AsReadOnly();

        var downloads = new List<YoutubeDownloadHistory>
        {
            new()
            {
                Id = 1,
                UserId = 1L,
                VideoTitle = "Test Video",
                VideoId = "abc123",
                VideoUrl = "https://youtube.com/watch?v=abc123",
                DownloadedAt = new DateTime(2026, 5, 20, 9, 0, 0, DateTimeKind.Utc)
            }
        }.AsReadOnly();

        var ctx = new GetHistoryHandlerFixture()
            .WithSearchGetByUserIdAsync(1L, 10, searches)
            .WithDownloadGetByUserIdAsync(1L, 10, downloads)
            .Build();

        var result = await ctx.Handler.HandleAsync(new GetHistoryRequest(null));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Searches.Should().HaveCount(1);
        result.Value.Searches[0].SearchText.Should().Be("test query");
        result.Value.Downloads.Should().HaveCount(1);
        result.Value.Downloads[0].VideoTitle.Should().Be("Test Video");
    }

    [Fact]
    public async Task HandleAsync_WithLimit_RespectsLimitParameter()
    {
        var searches = new List<YoutubeSearchHistory>
        {
            new()
            {
                Id = 1,
                UserId = 1L,
                SearchText = "limited result",
                SearchedAt = DateTime.UtcNow
            }
        }.AsReadOnly();

        var downloads = new List<YoutubeDownloadHistory>
        {
            new()
            {
                Id = 1,
                UserId = 1L,
                VideoTitle = "Limited Download",
                VideoId = "xyz789",
                DownloadedAt = DateTime.UtcNow
            }
        }.AsReadOnly();

        var ctx = new GetHistoryHandlerFixture()
            .WithSearchGetByUserIdAsync(1L, 5, searches)
            .WithDownloadGetByUserIdAsync(1L, 5, downloads)
            .Build();

        var result = await ctx.Handler.HandleAsync(new GetHistoryRequest(5));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Searches.Should().HaveCount(1);
        result.Value!.Downloads.Should().HaveCount(1);
    }

    [Fact]
    public async Task HandleAsync_WithEmptyHistory_ReturnsEmptyLists()
    {
        var emptySearches = new List<YoutubeSearchHistory>().AsReadOnly();
        var emptyDownloads = new List<YoutubeDownloadHistory>().AsReadOnly();

        var ctx = new GetHistoryHandlerFixture()
            .WithSearchGetByUserIdAsync(1L, 10, emptySearches)
            .WithDownloadGetByUserIdAsync(1L, 10, emptyDownloads)
            .Build();

        var result = await ctx.Handler.HandleAsync(new GetHistoryRequest(null));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Searches.Should().BeEmpty();
        result.Value!.Downloads.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_WhenSearchHistoryFails_ReturnsPartialDownloadHistory()
    {
        var downloads = new List<YoutubeDownloadHistory>
        {
            new()
            {
                Id = 1,
                UserId = 1L,
                VideoTitle = "Partial Video",
                VideoId = "abc123",
                DownloadedAt = DateTime.UtcNow
            }
        }.AsReadOnly();

        var ctx = new GetHistoryHandlerFixture()
            .WithSearchGetByUserIdAsync_Throws(1L, 10, new InvalidOperationException("Database connection failed"))
            .WithDownloadGetByUserIdAsync(1L, 10, downloads)
            .Build();

        var result = await ctx.Handler.HandleAsync(new GetHistoryRequest(null));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Searches.Should().BeEmpty();
        result.Value.Downloads.Should().HaveCount(1);
    }

    [Fact]
    public async Task HandleAsync_WhenBothRepositoriesFail_ReturnsFailure()
    {
        var ctx = new GetHistoryHandlerFixture()
            .WithSearchGetByUserIdAsync_Throws(1L, 10, new InvalidOperationException("Database connection failed"))
            .WithDownloadGetByUserIdAsync_Throws(1L, 10, new InvalidOperationException("Database connection failed"))
            .Build();

        var result = await ctx.Handler.HandleAsync(new GetHistoryRequest(null));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }
}
```

Removed imports: `JiApp.Infrastructure.Repositories`, `JiApp.Tests.Mocks`, `Moq`
Added import: `JiApp.Tests.Fixtures`
Removed: constructor + field-level mocks

- [ ] **Step 2: Build and run GetHistoryHandler tests**

Run: `dotnet test backend/tests/JiApp.Tests/ --filter "FullyQualifiedName~GetHistoryHandlerTests" --no-restore`
Expected: 5 passed, 0 failed

- [ ] **Step 3: Commit**

```bash
git add backend/tests/JiApp.Tests/Features/History/GetHistoryHandlerTests.cs
git commit -m "refactor(tests): use GetHistoryHandlerFixture in GetHistoryHandlerTests"
```

---

### Task 15: Create SearchVideosHandlerFixture

**Files:**
- Create: `/home/jakub/JiApp/backend/tests/JiApp.Tests/Fixtures/SearchVideosHandlerFixture.cs`

- [ ] **Step 1: Write the fixture file**

```csharp
using JiApp.Api.Features.Search.SearchVideos;
using JiApp.Infrastructure.Repositories;
using JiApp.Infrastructure.Services;
using JiApp.Tests.Mocks;
using JiApp.YtApi;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.Tests.Fixtures;

public sealed class SearchVideosHandlerFixture
{
    private readonly Mock<IYoutubeClient> _youtubeClientMock = YoutubeClientMock.GetSuccessful();
    private readonly Mock<ISearchHistoryRepository> _searchHistoryRepoMock = SearchHistoryRepositoryMock.GetSuccessful();
    private readonly Mock<ICurrentUserService> _currentUserMock;

    public SearchVideosHandlerFixture()
    {
        _currentUserMock = CurrentUserServiceMock.GetSuccessful();
    }

    public SearchVideosHandlerFixture WithSearchVideosAsync(string query, int maxResults, IReadOnlyList<YoutubeVideo> result)
    {
        _youtubeClientMock.Setup(x => x.SearchVideosAsync(query, maxResults)).ReturnsAsync(result);
        return this;
    }

    public SearchVideosHandlerFixture WithAnySearchVideosAsync(IReadOnlyList<YoutubeVideo> result)
    {
        _youtubeClientMock.Setup(x => x.SearchVideosAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(result);
        return this;
    }

    public SearchVideosHandlerFixture WithSearchVideosAsync_Throws(Exception exception)
    {
        _youtubeClientMock.Setup(x => x.SearchVideosAsync(It.IsAny<string>(), It.IsAny<int>())).ThrowsAsync(exception);
        return this;
    }

    public SearchVideosHandlerContext Build()
    {
        var handler = new SearchVideosHandler(
            _youtubeClientMock.Object,
            _searchHistoryRepoMock.Object,
            _currentUserMock.Object,
            LoggerMock.Of<SearchVideosHandler>());
        return new SearchVideosHandlerContext(handler, _youtubeClientMock, _searchHistoryRepoMock, _currentUserMock);
    }
}

public sealed record SearchVideosHandlerContext(
    SearchVideosHandler Handler,
    Mock<IYoutubeClient> YoutubeClientMock,
    Mock<ISearchHistoryRepository> SearchHistoryRepoMock,
    Mock<ICurrentUserService> CurrentUserMock);
```

- [ ] **Step 2: Build**

Run: `dotnet build backend/JiApp.sln`
Expected: Build succeeds

- [ ] **Step 3: Commit**

```bash
git add backend/tests/JiApp.Tests/Fixtures/SearchVideosHandlerFixture.cs
git commit -m "feat(tests): add SearchVideosHandlerFixture + SearchVideosHandlerContext"
```

---

### Task 16: Refactor SearchVideosHandlerTests to use fixture

**Files:**
- Modify: `/home/jakub/JiApp/backend/tests/JiApp.Tests/Features/Search/SearchVideosHandlerTests.cs` (full rewrite)

- [ ] **Step 1: Replace the test file content**

Replace the entire file with:

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Google;
using JiApp.Api.Features.Search.SearchVideos;
using JiApp.Common.Models;
using JiApp.Tests.Fixtures;
using JiApp.YtApi;
using Xunit;

namespace JiApp.Tests.Features.Search;

public class SearchVideosHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithValidQuery_ReturnsResultsAndSavesHistory()
    {
        var videos = new List<YoutubeVideo>
        {
            new("vid1", "Test Title", "Test Description", "https://img.url/1", "Test Channel"),
            new("vid2", "Another Title", "Another Description", "https://img.url/2", "Another Channel")
        }.AsReadOnly();

        var ctx = new SearchVideosHandlerFixture()
            .WithSearchVideosAsync("test query", 10, videos)
            .Build();

        var result = await ctx.Handler.HandleAsync(new SearchVideosRequest("test query", null));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Results.Should().HaveCount(2);

        var first = result.Value.Results[0];
        first.VideoId.Should().Be("vid1");
        first.Title.Should().Be("Test Title");
        first.VideoUrl.Should().Be("https://www.youtube.com/watch?v=vid1");

        ctx.SearchHistoryRepoMock.Verify(x => x.AddAsync(It.Is<YoutubeSearchHistory>(h =>
            h.UserId == 1L &&
            h.SearchText == "test query" &&
            h.SearchedAt.HasValue)), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithMaxResults_ReturnsLimitedResults()
    {
        var videos = new List<YoutubeVideo>
        {
            new("vid1", "Title", "Desc", "https://img.url/1", "Channel")
        }.AsReadOnly();

        var ctx = new SearchVideosHandlerFixture()
            .WithSearchVideosAsync("query", 5, videos)
            .Build();

        var result = await ctx.Handler.HandleAsync(new SearchVideosRequest("query", 5));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Results.Should().HaveCount(1);
    }

    [Fact]
    public async Task HandleAsync_WithEmptyResults_ReturnsEmptyList()
    {
        var emptyList = new List<YoutubeVideo>().AsReadOnly();

        var ctx = new SearchVideosHandlerFixture()
            .WithSearchVideosAsync("empty query", 10, emptyList)
            .Build();

        var result = await ctx.Handler.HandleAsync(new SearchVideosRequest("empty query", null));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Results.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_WhenYouTubeApiThrows_ReturnsFailure()
    {
        var ctx = new SearchVideosHandlerFixture()
            .WithSearchVideosAsync_Throws(new GoogleApiException("youtube", "API error"))
            .Build();

        var result = await ctx.Handler.HandleAsync(new SearchVideosRequest("query", null));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }
}
```

Note: The Verify call in the first test now uses `ctx.SearchHistoryRepoMock` (from the context) instead of a field-level mock. The import for `Moq` is still needed because `It.Is<>` and `Times.Once` are Moq types. But looking at the imports... `Times` is from Moq, but the test doesn't directly use `Times` — wait, it does use `Times.Once`. But actually, `Moq` namespace provides `It` and `Times` as extension methods. However, for Moq 4.x, `It.Is<T>()` is in the `Moq` namespace. So I still need `using Moq;`.

Wait, let me look at the current test imports:
```csharp
using Moq;
```

In the refactored version, we still need `Moq` because of `It.Is<YoutubeSearchHistory>(...)` and `Times.Once` in the Verify call. Let me keep it.

Also, the `It.Is<T>()` syntax requires `using Moq;` and is used inside ctx.SearchHistoryRepoMock.Verify.

Let me also check: do we need JiApp.Tests.Mocks? No, because we no longer create mocks directly.

So the imports for this file should be:
```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Google;
using JiApp.Api.Features.Search.SearchVideos;
using JiApp.Common.Models;
using JiApp.Tests.Fixtures;
using JiApp.YtApi;
using Moq;
using Xunit;
```

And we removed: `JiApp.Infrastructure.Repositories`, `JiApp.Tests.Mocks`
And added: `JiApp.Tests.Fixtures`

The `Moq` import is kept for the Verify call with `It.Is<>` and `Times.Once`.

- [ ] **Step 2: Build and run SearchVideosHandler tests**

Run: `dotnet test backend/tests/JiApp.Tests/ --filter "FullyQualifiedName~SearchVideosHandlerTests" --no-restore`
Expected: 4 passed, 0 failed

- [ ] **Step 3: Commit**

```bash
git add backend/tests/JiApp.Tests/Features/Search/SearchVideosHandlerTests.cs
git commit -m "refactor(tests): use SearchVideosHandlerFixture in SearchVideosHandlerTests"
```

---

### Task 17: Create GetDownloadLinkHandlerFixture

**Files:**
- Create: `/home/jakub/JiApp/backend/tests/JiApp.Tests/Fixtures/GetDownloadLinkHandlerFixture.cs`

- [ ] **Step 1: Write the fixture file**

```csharp
using JiApp.Api.Configuration;
using JiApp.Api.Features.Downloads.GetDownloadLink;
using JiApp.Infrastructure.Repositories;
using JiApp.Infrastructure.Services;
using JiApp.Tests.Mocks;
using JiApp.YtApi;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.Tests.Fixtures;

public sealed class GetDownloadLinkHandlerFixture
{
    private readonly Mock<IYoutubeClient> _youtubeClientMock = YoutubeClientMock.GetSuccessful();
    private readonly Mock<ITempFileStore> _tempFileStoreMock = TempFileStoreMock.GetSuccessful();
    private readonly Mock<IDownloadHistoryRepository> _downloadHistoryRepoMock = DownloadHistoryRepositoryMock.GetSuccessful();
    private readonly Mock<ICurrentUserService> _currentUserMock;
    private readonly Settings _settings;

    public GetDownloadLinkHandlerFixture()
    {
        _currentUserMock = CurrentUserServiceMock.GetWithUsername(1L, "testuser");
        _settings = new Settings
        {
            App = new Settings.AppSettings { BaseDirectory = "/tmp/ji_app" }
        };
    }

    public GetDownloadLinkHandlerFixture WithDownloadVideoAsync(string videoId, string outputPath, YoutubeClientResponse response)
    {
        _youtubeClientMock.Setup(x => x.DownloadVideoAsync(videoId, outputPath)).ReturnsAsync(response);
        return this;
    }

    public GetDownloadLinkHandlerFixture WithAnyDownloadVideoAsync(YoutubeClientResponse response)
    {
        _youtubeClientMock.Setup(x => x.DownloadVideoAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(response);
        return this;
    }

    public GetDownloadLinkHandlerFixture WithTempFileStoreAdd(string filePath, long userId, string tempId)
    {
        _tempFileStoreMock.Setup(x => x.Add(filePath, userId)).Returns(tempId);
        return this;
    }

    public GetDownloadLinkHandlerContext Build()
    {
        var handler = new GetDownloadLinkHandler(
            _youtubeClientMock.Object,
            _tempFileStoreMock.Object,
            _downloadHistoryRepoMock.Object,
            _currentUserMock.Object,
            _settings,
            LoggerMock.Of<GetDownloadLinkHandler>());
        return new GetDownloadLinkHandlerContext(
            handler,
            _youtubeClientMock,
            _tempFileStoreMock,
            _downloadHistoryRepoMock,
            _currentUserMock,
            _settings);
    }
}

public sealed record GetDownloadLinkHandlerContext(
    GetDownloadLinkHandler Handler,
    Mock<IYoutubeClient> YoutubeClientMock,
    Mock<ITempFileStore> TempFileStoreMock,
    Mock<IDownloadHistoryRepository> DownloadHistoryRepoMock,
    Mock<ICurrentUserService> CurrentUserMock,
    Settings Settings);
```

- [ ] **Step 2: Build**

Run: `dotnet build backend/JiApp.sln`
Expected: Build succeeds

- [ ] **Step 3: Commit**

```bash
git add backend/tests/JiApp.Tests/Fixtures/GetDownloadLinkHandlerFixture.cs
git commit -m "feat(tests): add GetDownloadLinkHandlerFixture + GetDownloadLinkHandlerContext"
```

---

### Task 18: Refactor GetDownloadLinkHandlerTests to use fixture

**Files:**
- Modify: `/home/jakub/JiApp/backend/tests/JiApp.Tests/Features/Downloads/GetDownloadLinkHandlerTests.cs` (full rewrite)

- [ ] **Step 1: Replace the test file content**

Replace the entire file with:

```csharp
using System.Threading.Tasks;
using FluentAssertions;
using JiApp.Api.Features.Downloads.GetDownloadLink;
using JiApp.Common.Models;
using JiApp.Tests.Fixtures;
using JiApp.YtApi;
using Moq;
using Xunit;

namespace JiApp.Tests.Features.Downloads;

public class GetDownloadLinkHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithValidRequest_ReturnsTempId()
    {
        const string downloadedFilePath = "/tmp/ji_app/YtMp3_1/song.mp3";
        const string tempFileId = "abc123def456";

        var ctx = new GetDownloadLinkHandlerFixture()
            .WithAnyDownloadVideoAsync(new YoutubeClientResponse(downloadedFilePath, true, []))
            .WithTempFileStoreAdd(downloadedFilePath, 1L, tempFileId)
            .Build();

        var request = new DownloadRequest(
            "dQw4w9WgXcQ",
            "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
            "Test Title",
            "Test Description",
            "https://img.url/thumb.jpg");

        var result = await ctx.Handler.HandleAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.TempId.Should().Be(tempFileId);
    }

    [Fact]
    public async Task HandleAsync_WhenYtDlpFails_ReturnsFailure()
    {
        var ctx = new GetDownloadLinkHandlerFixture()
            .WithAnyDownloadVideoAsync(new YoutubeClientResponse(null, false, ["Download failed"]))
            .Build();

        var request = new DownloadRequest(
            "dQw4w9WgXcQ",
            "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
            null, null, null);

        var result = await ctx.Handler.HandleAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task HandleAsync_SavesDownloadHistory_OnSuccess()
    {
        const string downloadedFilePath = "/tmp/ji_app/YtMp3_1/song.mp3";
        const string tempFileId = "abc123def456";

        var ctx = new GetDownloadLinkHandlerFixture()
            .WithAnyDownloadVideoAsync(new YoutubeClientResponse(downloadedFilePath, true, []))
            .WithTempFileStoreAdd(downloadedFilePath, 1L, tempFileId)
            .Build();

        var request = new DownloadRequest(
            "dQw4w9WgXcQ",
            "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
            "Test Title",
            "Test Description",
            "https://img.url/thumb.jpg");

        await ctx.Handler.HandleAsync(request);

        ctx.DownloadHistoryRepoMock.Verify(x => x.AddAsync(It.Is<YoutubeDownloadHistory>(h =>
            h.UserId == 1L &&
            h.VideoId == "dQw4w9WgXcQ" &&
            h.VideoTitle == "Test Title" &&
            h.VideoDescription == "Test Description" &&
            h.VideoUrl == "https://www.youtube.com/watch?v=dQw4w9WgXcQ" &&
            h.ImageUrl == "https://img.url/thumb.jpg" &&
            h.DownloadedAt.Kind == DateTimeKind.Utc)), Times.Once);
    }
}
```

Removed imports: `JiApp.Infrastructure.Repositories`, `JiApp.Infrastructure.Services`, `JiApp.Tests.Mocks`, `Microsoft.Extensions.Configuration`, `System` (partial — `DateTimeKind` is from System but `DownloadedAt.Kind` works with implicit using since the project likely has `<ImplicitUsings>enable</ImplicitUsings>`... let me check).

Actually, let me check if the project has implicit usings enabled. It's a .NET SDK-style project and uses `System.Threading.Tasks` explicitly. Let me look at the project file.

The test csproj has:
```xml
<Project Sdk="Microsoft.NET.Sdk">
```

No `<ImplicitUsings>` specified. But I see the test files use `System.Threading.Tasks` as an explicit import. For .NET 8+, the default for `Microsoft.NET.Sdk.Web` projects is implicit usings, but for `Microsoft.NET.Sdk` (class library), it might not be.

Looking at the test file, they have `using System;` in some files. Let me check the `using System;` in the current GetDownloadLinkHandlerTests - line 1 is `using System;`.

Actually, looking at the `DateTimeKind.UtcUsed` in the test (line `h.DownloadedAt.Kind == DateTimeKind.Utc`), `DateTimeKind` is in `System` namespace. So I need `using System;`. But the handler fixture and context don't need it.

Hmm, let me look at the refactored GetDownloadLinkHandlerTests more carefully. The third test uses `DateTimeKind.Utc`, so I need `using System;`. Let me add that import.

Wait, the original test file also uses `System.Collections.Generic` which isn't used anymore (since we moved list creation into the fixture methods). Let me check the refactored version - do any of the tests use `System.Collections.Generic`? No, the lists are inside the fixture methods. `System.Threading.Tasks` is used by the async tests. So the imports should be:

```csharp
using System;
using System.Threading.Tasks;
using FluentAssertions;
using JiApp.Api.Features.Downloads.GetDownloadLink;
using JiApp.Common.Models;
using JiApp.Tests.Fixtures;
using JiApp.YtApi;
using Moq;
using Xunit;
```

Let me use that.

- [ ] **Step 2: Build and run GetDownloadLinkHandler tests**

Run: `dotnet test backend/tests/JiApp.Tests/ --filter "FullyQualifiedName~GetDownloadLinkHandlerTests" --no-restore`
Expected: 3 passed, 0 failed

- [ ] **Step 3: Commit**

```bash
git add backend/tests/JiApp.Tests/Features/Downloads/GetDownloadLinkHandlerTests.cs
git commit -m "refactor(tests): use GetDownloadLinkHandlerFixture in GetDownloadLinkHandlerTests"
```

---

### Task 19: Run full test suite to verify nothing is broken

- [ ] **Step 1: Run all tests**

Run: `dotnet test backend/JiApp.sln --no-restore`
Expected: All tests pass (including integration tests in `Integration/` and any other tests)

- [ ] **Step 2: Final commit if needed**

No changes expected at this point (all changes already committed in previous steps).

---

## Verification Checklist

- [ ] All 9 fixture files compile successfully
- [ ] All 9 test files compile successfully
- [ ] All LoginHandlerTests pass (4 tests)
- [ ] All RegisterHandlerTests pass (3 tests)
- [ ] All MeHandlerTests pass (2 tests)
- [ ] All DownloadFileHandlerTests pass (3 tests)
- [ ] All DownloadHistoryHandlerTests pass (3 tests)
- [ ] All SearchHistoryHandlerTests pass (3 tests)
- [ ] All GetHistoryHandlerTests pass (5 tests)
- [ ] All SearchVideosHandlerTests pass (4 tests)
- [ ] All GetDownloadLinkHandlerTests pass (3 tests)
- [ ] Full `dotnet test` suite passes
- [ ] No new `Moq` usage patterns introduced (only `Setup` calls wrapped in fixtures, `Verify` calls in tests via context mocks)
- [ ] Existing `Mocks/*.cs` files are unmodified
- [ ] No handler source files are modified
