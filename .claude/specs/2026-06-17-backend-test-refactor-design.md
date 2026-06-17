# Backend Test Refactoring — Design Spec

**Date:** 2026-06-17
**Branch:** clean/backend-tests (from main)
**Scope:** ~58 test files across 5 test projects

---

## Goal

Refactor all backend tests to a unified, semantic style: **Fixture / With_xxx / Sut + AAA** with custom base classes and custom assertions. Tests should speak for themselves — reading a test method body should read like a sentence.

---

## 1. Shared Project

Create `tests/JiApp.Testing.Common/JiApp.Testing.Common.csproj` — referenced by all 5 test projects.

```
tests/JiApp.Testing.Common/
├── JiApp.Testing.Common.csproj
├── Bases/
│   ├── HandlerTestBase.cs
│   ├── ValidatorTestBase.cs
│   └── IntegrationTestBase.cs
├── Mocking/
│   ├── MockObject.cs
│   └── MockCurrentUserService.cs
├── Data/
│   └── TestDb.cs
└── Assertions/
    ├── ResultAssertions.cs
    └── EntityAssertions.cs
```

---

## 2. Base Classes

### 2.1 `HandlerTestBase` — DB-backed handler tests

```csharp
public abstract class HandlerTestBase
{
    protected TestDb Db { get; }   // fresh empty DB per test

    protected void StoreInDb<T>(T entity) where T : class => Db.Store(entity);
    protected void RemoveFromDb<T>(T entity) where T : class => Db.Remove(entity);
}
```

- Fresh SQLite `:memory:` database created in the base constructor
- No `IDisposable` — xUnit handles cleanup via finalization for in-memory DBs
- `Db` is always empty at test start — data existence is explicit

### 2.2 `ValidatorTestBase` — lightweight, no DB

```csharp
public abstract class ValidatorTestBase
{
    // shared assertion helpers only; no infrastructure
}
```

### 2.3 `IntegrationTestBase` — `WebApplicationFactory` boilerplate

```csharp
public abstract class IntegrationTestBase : IClassFixture<GatewayWebApplicationFactory>
{
    protected HttpClient Client { get; }
    // ...
}
```

---

## 3. TestDb

Wraps `DbContext`, hides the `Add → SaveChanges → ChangeTracker.Clear()` triplet.

```csharp
public class TestDb
{
    public void Store<T>(T entity) where T : class { ... }
    public void StoreAll<T>(params T[] entities) where T : class { ... }
    public void Remove<T>(T entity) where T : class { ... }
    public T? Find<T>(object id) where T : class { ... }
    public IQueryable<T> Query<T>() where T : class { ... }
}
```

---

## 4. Mocking Infrastructure

### 4.1 `MockObject<T>` — abstract base

```csharp
public abstract class MockObject<T> where T : class
{
    protected readonly Mock<T> Mock = new();

    public static implicit operator T(MockObject<T> mock) => mock.Mock.Object;
}
```

- Implicit conversion means `MockCurrentUserService` can be passed anywhere `ICurrentUserService` is expected
- `.Mock` is accessible for `.Verify()` in assertions

### 4.2 `MockCurrentUserService`

```csharp
public sealed class MockCurrentUserService : MockObject<ICurrentUserService>
{
    public MockCurrentUserService WithReturning(long userId)
    {
        Mock.Setup(x => x.UserId).Returns(userId);
        return this;
    }

    public static MockCurrentUserService GetSuccessful() =>
        new MockCurrentUserService().WithReturning(1L);
}
```

### 4.3 Mock handler pattern (dependency doubles)

When a handler is a dependency of another test, a file-local static factory provides test doubles:

```csharp
private static class MockAppointmentHandler
{
    public static IAppointmentHandler GetSuccessful() => ...
    public static IAppointmentHandler GetFailed(string category) => ...
}
```

---

## 5. Fixture Pattern

### Rules
- **Nested `private sealed class Fixture`** in each test class
- **Private constructor** + `public static Fixture Init()` factory
- **Defaults to successful** — all dependencies initialized to happy path in ctor
- **`With_` takes domain values** (not mocks) — no logic inside `With_`, it's a pure setter that translates domain → mock setup
- **Sut property** always named `Sut`

### Template

```csharp
private sealed class Fixture
{
    private Fixture()
    {
        CurrentUser = MockCurrentUserService.GetSuccessful();
        // all deps default to happy path
    }

    public MockCurrentUserService CurrentUser { get; private set; }

    public AppointmentHandler Sut => new(Db, CurrentUser);

    public static Fixture Init() => new();

    public Fixture WithCurrentUser(User user)
    {
        CurrentUser.WithReturning(user.Id);
        return this;
    }
}
```

---

## 6. Test Method Template

### Structure: Arrange / Act / Assert via blank lines

```csharp
public sealed class AppointmentHandlerTests : HandlerTestBase
{
    private sealed class Fixture { /* as above */ }

    // Entity factories — file-local, not on base
    private static Board CreateSomeBoard(Action<Board>? configure = null)
    {
        var board = new Board { Name = "Test Board" };
        configure?.Invoke(board);
        return board;
    }

    [Fact]
    public async Task CreateAppointment_WithValidData_ReturnsAppointmentId()
    {
        var fixture = Fixture.Init();

        var board = CreateSomeBoard();
        StoreInDb(board);

        var sut = fixture.Sut;

        var result = await sut.HandleAsync(request, CancellationToken.None);

        AssertSuccess(result);
        result.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateAppointment_WithUnauthenticatedUser_ReturnsUnauthorized()
    {
        var unauthenticatedUser = new User { Id = 0 };
        var fixture = Fixture.Init().WithCurrentUser(unauthenticatedUser);

        var sut = fixture.Sut;

        var result = await sut.HandleAsync(request, CancellationToken.None);

        AssertFailure(result, ResultCategories.Unauthorized);
    }
}
```

---

## 7. Custom Assertions

```csharp
public static class ResultAssertions
{
    public static void AssertSuccess<T>(Result<T> result) =>
        result.IsSuccess.Should().BeTrue();

    public static void AssertSuccessWithValue<T>(Result<T> result, T expected) =>
        result.Value.Should().Be(expected);

    public static void AssertFailure<T>(Result<T> result, string category) =>
        result.ErrorCategory.Should().Be(category);

    public static void AssertFailureWithMessage<T>(Result<T> result, string category, string message) { ... }

    public static void AssertNotFound<T>(Result<T> result) =>
        AssertFailure(result, ResultCategories.NotFound);
}

public static class EntityAssertions
{
    public static void AssertEntityExists<T>(DbContext db, long id) where T : class => ...
    public static void AssertEntityCount<T>(DbContext db, int expected) where T : class => ...
}
```

---

## 8. Conventions Summary

| Aspect | Convention |
|---|---|
| Test class modifier | `public sealed class` |
| Test method naming | `PascalCase_With_Underscores` (e.g., `CreateAppointment_WithValidData_ReturnsAppointmentId`) |
| Sut property | Always `Sut`, singular |
| AAA | Blank-line separation, no comments |
| `With_` methods | Take domain values, no logic, pure setters |
| Entity factories | `CreateSomeXxx()` — per-file, creates instances, does NOT persist |
| DB persistence | `StoreInDb()` / `RemoveFromDb()` — explicit calls in test body |
| Mock setup | Never raw `.Setup()` in tests — always via `MockObject` wrappers |
| Mock verify | `fixture.CurrentUser.Mock.Verify(...)` — only in assertions |
| All deps default to success | Only override deviations |

---

## 9. File Inventory

### New files (in `JiApp.Testing.Common`)
- `JiApp.Testing.Common.csproj`
- `Bases/HandlerTestBase.cs`
- `Bases/ValidatorTestBase.cs`
- `Bases/IntegrationTestBase.cs`
- `Mocking/MockObject.cs`
- `Mocking/MockCurrentUserService.cs`
- `Data/TestDb.cs`
- `Assertions/ResultAssertions.cs`
- `Assertions/EntityAssertions.cs`

### Modified files (all 5 test projects)
- All `.csproj` — add `<ProjectReference>` to `JiApp.Testing.Common`
- ~58 test `.cs` files — refactored to unified pattern

---

## 10. Verification

1. `dotnet build tests/` — all projects compile
2. `dotnet test tests/` — all tests pass with identical coverage
3. Spot-check 3 representative test files for style compliance
4. `git diff --stat` — confirm no production code touched
