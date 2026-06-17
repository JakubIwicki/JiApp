# Backend Test Refactoring — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Refactor all 58 backend test files into a unified Fixture/With_xxx/Sut + AAA pattern with shared base classes, TestDb, MockObject infrastructure, and custom assertions.

**Architecture:** New `JiApp.Testing.Common` shared project with base classes (`HandlerTestBase`, `ValidatorTestBase`, `IntegrationTestBase`), `TestDb` wrapper, `MockObject<T>` abstract base, `MockCurrentUserService`, and static assertion helpers. All 5 test projects gain a `ProjectReference` to it. Each test file gets refactored to the unified pattern: nested Fixture with `Init()` + `With_()` builder methods + `Sut` property, AAA via blank lines, explicit `StoreInDb()`/`RemoveFromDb()` calls.

**Tech Stack:** xUnit 2.9.3, FluentAssertions 8.10.0, Moq 4.20.72, SQLite in-memory (Microsoft.EntityFrameworkCore.Sqlite 10.0.8)

---

### Task 1: Create JiApp.Testing.Common project

**Files:**
- Create: `tests/JiApp.Testing.Common/JiApp.Testing.Common.csproj`
- Create: `tests/JiApp.Testing.Common/Usings.cs`
- Modify: `JiApp.sln`

- [ ] **Step 1: Create project directory and csproj**

```bash
mkdir -p tests/JiApp.Testing.Common
```

```xml
<!-- tests/JiApp.Testing.Common/JiApp.Testing.Common.csproj -->
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <IsPackable>false</IsPackable>
    <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="FluentAssertions" Version="8.10.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.8" />
    <PackageReference Include="Moq" Version="4.20.72" />
    <PackageReference Include="xunit" Version="2.9.3" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\JiApp.Common\JiApp.Common.csproj" />
    <ProjectReference Include="..\..\src\JiApp.Scheduler\JiApp.Scheduler.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Create Usings.cs**

```csharp
// tests/JiApp.Testing.Common/Usings.cs
global using FluentAssertions;
global using JiApp.Common.Abstractions;
global using Moq;
global using Xunit;
```

- [ ] **Step 3: Add project to solution**

```bash
dotnet sln JiApp.sln add tests/JiApp.Testing.Common/JiApp.Testing.Common.csproj --solution-folder tests
```

- [ ] **Step 4: Build to verify**

```bash
dotnet build tests/JiApp.Testing.Common/
```

Expected: Build succeeds (project references resolve, no code yet).

- [ ] **Step 5: Commit**

```bash
git add tests/JiApp.Testing.Common/ JiApp.sln
git commit -m "feat: scaffold JiApp.Testing.Common shared test project

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 2: TestDb wrapper

**Files:**
- Create: `tests/JiApp.Testing.Common/Data/TestDb.cs`

- [ ] **Step 1: Write TestDb**

```csharp
// tests/JiApp.Testing.Common/Data/TestDb.cs
using Microsoft.EntityFrameworkCore;

namespace JiApp.Testing.Common.Data;

public sealed class TestDb
{
    private readonly DbContext _db;

    public TestDb(DbContext db) => _db = db;

    public void Store<T>(T entity) where T : class
    {
        _db.Set<T>().Add(entity);
        _db.SaveChanges();
        _db.ChangeTracker.Clear();
    }

    public void StoreAll<T>(params T[] entities) where T : class
    {
        _db.Set<T>().AddRange(entities);
        _db.SaveChanges();
        _db.ChangeTracker.Clear();
    }

    public void Remove<T>(T entity) where T : class
    {
        _db.Set<T>().Remove(entity);
        _db.SaveChanges();
        _db.ChangeTracker.Clear();
    }

    public T? Find<T>(object id) where T : class =>
        _db.Set<T>().Find(id);

    public IQueryable<T> Query<T>() where T : class =>
        _db.Set<T>();
}
```

- [ ] **Step 2: Build to verify**

```bash
dotnet build tests/JiApp.Testing.Common/
```

Expected: BUILD SUCCESS

- [ ] **Step 3: Commit**

```bash
git add tests/JiApp.Testing.Common/Data/TestDb.cs
git commit -m "feat: add TestDb wrapper with Store/Remove/StoreAll

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 3: MockObject<T> and MockCurrentUserService

**Files:**
- Create: `tests/JiApp.Testing.Common/Mocking/MockObject.cs`
- Create: `tests/JiApp.Testing.Common/Mocking/MockCurrentUserService.cs`

- [ ] **Step 1: Write MockObject<T>**

```csharp
// tests/JiApp.Testing.Common/Mocking/MockObject.cs
namespace JiApp.Testing.Common.Mocking;

public abstract class MockObject<T> where T : class
{
    protected readonly Mock<T> Mock = new();

    public static implicit operator T(MockObject<T> mock) => mock.Mock.Object;
}
```

- [ ] **Step 2: Write MockCurrentUserService**

```csharp
// tests/JiApp.Testing.Common/Mocking/MockCurrentUserService.cs
using JiApp.Common.Abstractions;

namespace JiApp.Testing.Common.Mocking;

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

- [ ] **Step 3: Build to verify**

```bash
dotnet build tests/JiApp.Testing.Common/
```

Expected: BUILD SUCCESS

- [ ] **Step 4: Commit**

```bash
git add tests/JiApp.Testing.Common/Mocking/
git commit -m "feat: add MockObject<T> base and MockCurrentUserService

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 4: Custom assertions

**Files:**
- Create: `tests/JiApp.Testing.Common/Assertions/ResultAssertions.cs`
- Create: `tests/JiApp.Testing.Common/Assertions/EntityAssertions.cs`

- [ ] **Step 1: Write ResultAssertions**

```csharp
// tests/JiApp.Testing.Common/Assertions/ResultAssertions.cs
using JiApp.Common.Abstractions;

namespace JiApp.Testing.Common.Assertions;

public static class ResultAssertions
{
    public static void AssertSuccess<T>(Result<T> result) =>
        result.IsSuccess.Should().BeTrue();

    public static void AssertSuccessWithValue<T>(Result<T> result, T expected) =>
        result.Value.Should().Be(expected);

    public static void AssertFailure<T>(Result<T> result, string category)
    {
        result.IsSuccess.Should().BeFalse();
        result.ErrorCategory.Should().Be(category);
    }

    public static void AssertFailureWithMessage<T>(Result<T> result, string category, string message)
    {
        result.IsSuccess.Should().BeFalse();
        result.ErrorCategory.Should().Be(category);
        result.Error.Should().Be(message);
    }

    public static void AssertNotFound<T>(Result<T> result) =>
        AssertFailure(result, ResultCategories.NotFound);

    public static void AssertAccessDenied<T>(Result<T> result) =>
        AssertFailure(result, ResultCategories.AccessDenied);

    public static void AssertValidationFailure<T>(Result<T> result) =>
        AssertFailure(result, ResultCategories.Validation);

    public static void AssertConflict<T>(Result<T> result) =>
        AssertFailure(result, ResultCategories.Conflict);
}
```

- [ ] **Step 2: Write EntityAssertions**

```csharp
// tests/JiApp.Testing.Common/Assertions/EntityAssertions.cs
using Microsoft.EntityFrameworkCore;

namespace JiApp.Testing.Common.Assertions;

public static class EntityAssertions
{
    public static void AssertEntityExists<T>(DbContext db, long id) where T : class
    {
        var entity = db.Set<T>().Find(id);
        entity.Should().NotBeNull();
    }

    public static void AssertEntityCount<T>(DbContext db, int expected) where T : class =>
        db.Set<T>().Count().Should().Be(expected);
}
```

- [ ] **Step 3: Build to verify**

```bash
dotnet build tests/JiApp.Testing.Common/
```

Expected: BUILD SUCCESS

- [ ] **Step 4: Commit**

```bash
git add tests/JiApp.Testing.Common/Assertions/
git commit -m "feat: add ResultAssertions and EntityAssertions helpers

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 5: Base classes

**Files:**
- Create: `tests/JiApp.Testing.Common/Bases/HandlerTestBase.cs`
- Create: `tests/JiApp.Testing.Common/Bases/ValidatorTestBase.cs`
- Create: `tests/JiApp.Testing.Common/Bases/IntegrationTestBase.cs`

- [ ] **Step 1: Write HandlerTestBase**

```csharp
// tests/JiApp.Testing.Common/Bases/HandlerTestBase.cs
using JiApp.Testing.Common.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using JiApp.Scheduler.Persistence;

namespace JiApp.Testing.Common.Bases;

public abstract class HandlerTestBase : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SchedulerDbContext _dbContext;

    protected TestDb Db { get; }

    protected HandlerTestBase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<SchedulerDbContext>()
            .UseSqlite(_connection)
            .Options;
        _dbContext = new SchedulerDbContext(options);
        _dbContext.Database.EnsureCreated();
        Db = new TestDb(_dbContext);
    }

    protected void StoreInDb<T>(T entity) where T : class => Db.Store(entity);
    protected void RemoveFromDb<T>(T entity) where T : class => Db.Remove(entity);

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }
}
```

- [ ] **Step 2: Write ValidatorTestBase**

```csharp
// tests/JiApp.Testing.Common/Bases/ValidatorTestBase.cs
namespace JiApp.Testing.Common.Bases;

public abstract class ValidatorTestBase
{
}
```

- [ ] **Step 3: Write IntegrationTestBase**

```csharp
// tests/JiApp.Testing.Common/Bases/IntegrationTestBase.cs
namespace JiApp.Testing.Common.Bases;

public abstract class IntegrationTestBase
{
}
```

- [ ] **Step 4: Build to verify**

```bash
dotnet build tests/JiApp.Testing.Common/
```

Expected: BUILD SUCCESS

- [ ] **Step 5: Commit**

```bash
git add tests/JiApp.Testing.Common/Bases/
git commit -m "feat: add HandlerTestBase, ValidatorTestBase, IntegrationTestBase

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 6: Add JiApp.Testing.Common reference to all test projects

**Files:**
- Modify: `tests/JiApp.Scheduler.Tests/JiApp.Scheduler.Tests.csproj`
- Modify: `tests/JiApp.Identity.Tests/JiApp.Identity.Tests.csproj`
- Modify: `tests/JiApp.YtDownloader.Tests/JiApp.YtDownloader.Tests.csproj`
- Modify: `tests/JiApp.Gateway.Tests/JiApp.Gateway.Tests.csproj`
- Modify: `tests/JiApp.ImageTools.Tests/JiApp.ImageTools.Tests.csproj`
- Modify: `tests/JiApp.Scheduler.Tests/Usings.cs`
- Modify: `tests/JiApp.Identity.Tests/Usings.cs`
- Modify: `tests/JiApp.YtDownloader.Tests/Usings.cs`
- Modify: `tests/JiApp.Gateway.Tests/Usings.cs`
- Modify: `tests/JiApp.ImageTools.Tests/Usings.cs`

- [ ] **Step 1: Add ProjectReference to each test .csproj**

Add this to each test `.csproj` inside `<ItemGroup>` (alongside existing `ProjectReference` entries):

```xml
<ProjectReference Include="..\JiApp.Testing.Common\JiApp.Testing.Common.csproj" />
```

Files to edit:
- `tests/JiApp.Scheduler.Tests/JiApp.Scheduler.Tests.csproj`
- `tests/JiApp.Identity.Tests/JiApp.Identity.Tests.csproj`
- `tests/JiApp.YtDownloader.Tests/JiApp.YtDownloader.Tests.csproj`
- `tests/JiApp.Gateway.Tests/JiApp.Gateway.Tests.csproj`
- `tests/JiApp.ImageTools.Tests/JiApp.ImageTools.Tests.csproj`

- [ ] **Step 2: Add global usings to each test project's Usings.cs**

For Scheduler (`tests/JiApp.Scheduler.Tests/Usings.cs`), append:
```csharp
global using JiApp.Testing.Common.Assertions;
global using JiApp.Testing.Common.Bases;
global using JiApp.Testing.Common.Data;
global using JiApp.Testing.Common.Mocking;
```

For Identity (`tests/JiApp.Identity.Tests/Usings.cs`), append:
```csharp
global using JiApp.Testing.Common.Assertions;
global using JiApp.Testing.Common.Bases;
global using JiApp.Testing.Common.Data;
global using JiApp.Testing.Common.Mocking;
```

For YtDownloader (`tests/JiApp.YtDownloader.Tests/Usings.cs`), append:
```csharp
global using JiApp.Testing.Common.Assertions;
global using JiApp.Testing.Common.Bases;
global using JiApp.Testing.Common.Data;
global using JiApp.Testing.Common.Mocking;
```

For Gateway (`tests/JiApp.Gateway.Tests/Usings.cs`), append:
```csharp
global using JiApp.Testing.Common.Assertions;
global using JiApp.Testing.Common.Bases;
global using JiApp.Testing.Common.Data;
global using JiApp.Testing.Common.Mocking;
```

For ImageTools (`tests/JiApp.ImageTools.Tests/Usings.cs`), append:
```csharp
global using JiApp.Testing.Common.Assertions;
global using JiApp.Testing.Common.Bases;
global using JiApp.Testing.Common.Data;
global using JiApp.Testing.Common.Mocking;
```

- [ ] **Step 3: Build all test projects**

```bash
dotnet build tests/
```

Expected: All projects build, references resolve.

- [ ] **Step 4: Commit**

```bash
git add tests/JiApp.Scheduler.Tests/JiApp.Scheduler.Tests.csproj \
        tests/JiApp.Identity.Tests/JiApp.Identity.Tests.csproj \
        tests/JiApp.YtDownloader.Tests/JiApp.YtDownloader.Tests.csproj \
        tests/JiApp.Gateway.Tests/JiApp.Gateway.Tests.csproj \
        tests/JiApp.ImageTools.Tests/JiApp.ImageTools.Tests.csproj \
        tests/JiApp.Scheduler.Tests/Usings.cs \
        tests/JiApp.Identity.Tests/Usings.cs \
        tests/JiApp.YtDownloader.Tests/Usings.cs \
        tests/JiApp.Gateway.Tests/Usings.cs \
        tests/JiApp.ImageTools.Tests/Usings.cs
git commit -m "feat: wire JiApp.Testing.Common into all test projects

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 7: Refactor Scheduler handler tests (6 files)

**Files to refactor:**
- `tests/JiApp.Scheduler.Tests/Features/Appointments/AppointmentHandlerTests.cs`
- `tests/JiApp.Scheduler.Tests/Features/Boards/BoardHandlerTests.cs`
- `tests/JiApp.Scheduler.Tests/Features/Clients/ClientHandlerTests.cs`
- `tests/JiApp.Scheduler.Tests/Features/Expenses/ExpenseHandlerTests.cs`
- `tests/JiApp.Scheduler.Tests/Features/Services/ServiceHandlerTests.cs`
- `tests/JiApp.Scheduler.Tests/Features/DayTotals/DayTotalsHandlerTests.cs`

**Pattern to apply:**

```csharp
public sealed class AppointmentHandlerTests : HandlerTestBase
{
    private sealed class Fixture
    {
        private Fixture()
        {
            CurrentUser = MockCurrentUserService.GetSuccessful();
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
}
```

**Refactoring steps per file:**
1. Change base class to `HandlerTestBase`
2. Keep nested `Fixture` but remove `IDisposable`, SQLite boilerplate, and `_db` field — use `Db`, `StoreInDb()`, `RemoveFromDb()` from base
3. Replace raw `Mock<ICurrentUserService>` with `MockCurrentUserService`
4. Replace `Build()` or expression-bodied Sut properties with single `Sut` property (singular)
5. Add `private static Fixture.Init()` + `With_()` methods that take domain values
6. Default all deps to successful in Fixture constructor
7. Replace `_db.Boards.Add(x); _db.SaveChanges(); _db.ChangeTracker.Clear()` with `StoreInDb(x)`
8. Replace assertion boilerplate with `AssertSuccess()` / `AssertFailure()` / etc.
9. Ensure test names are PascalCase with underscores
10. Ensure AAA is blank-line separated, no `// Arrange` comments

- [ ] **Step 1: Refactor AppointmentHandlerTests.cs**
- [ ] **Step 2: Refactor BoardHandlerTests.cs**
- [ ] **Step 3: Refactor ClientHandlerTests.cs**
- [ ] **Step 4: Refactor ExpenseHandlerTests.cs**
- [ ] **Step 5: Refactor ServiceHandlerTests.cs**
- [ ] **Step 6: Refactor DayTotalsHandlerTests.cs**
- [ ] **Step 7: Build and run Scheduler tests**

```bash
dotnet test tests/JiApp.Scheduler.Tests/ --no-build
```

Expected: All tests pass

- [ ] **Step 8: Commit**

```bash
git add tests/JiApp.Scheduler.Tests/Features/Appointments/AppointmentHandlerTests.cs \
        tests/JiApp.Scheduler.Tests/Features/Boards/BoardHandlerTests.cs \
        tests/JiApp.Scheduler.Tests/Features/Clients/ClientHandlerTests.cs \
        tests/JiApp.Scheduler.Tests/Features/Expenses/ExpenseHandlerTests.cs \
        tests/JiApp.Scheduler.Tests/Features/Services/ServiceHandlerTests.cs \
        tests/JiApp.Scheduler.Tests/Features/DayTotals/DayTotalsHandlerTests.cs
git commit -m "refactor: Scheduler handler tests to unified Fixture/Sut/AAA pattern

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 8: Refactor Scheduler report handler tests (2 files)

**Files:**
- `tests/JiApp.Scheduler.Tests/Features/Clients/ClientReportHandlerTests.cs`
- `tests/JiApp.Scheduler.Tests/Features/Revenues/RevenueReportHandlerTests.cs`

Same pattern as Task 7. Each file:
- Inherit `HandlerTestBase`
- Nested `Fixture` with `Init()` + `With_()` methods
- `Sut` property (singular)
- `StoreInDb()` for explicit data seeding
- `AssertSuccess()` / `AssertFailure()` for assertions

- [ ] **Step 1: Refactor ClientReportHandlerTests.cs**
- [ ] **Step 2: Refactor RevenueReportHandlerTests.cs**
- [ ] **Step 3: Run Scheduler tests**

```bash
dotnet test tests/JiApp.Scheduler.Tests/ --no-build
```

Expected: All tests pass

- [ ] **Step 4: Commit**

```bash
git add tests/JiApp.Scheduler.Tests/Features/Clients/ClientReportHandlerTests.cs \
        tests/JiApp.Scheduler.Tests/Features/Revenues/RevenueReportHandlerTests.cs
git commit -m "refactor: Scheduler report handler tests to unified pattern

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 9: Refactor Scheduler validator tests (14 files)

**Files:**
- `tests/JiApp.Scheduler.Tests/Features/Appointments/CreateAppointmentValidatorTests.cs`
- `tests/JiApp.Scheduler.Tests/Features/Appointments/UpdateAppointmentValidatorTests.cs`
- `tests/JiApp.Scheduler.Tests/Features/Appointments/UpdateAppointmentStatusValidatorTests.cs`
- `tests/JiApp.Scheduler.Tests/Features/Boards/CreateBoardValidatorTests.cs`
- `tests/JiApp.Scheduler.Tests/Features/Boards/UpdateBoardValidatorTests.cs`
- `tests/JiApp.Scheduler.Tests/Features/Boards/AddBoardMemberValidatorTests.cs`
- `tests/JiApp.Scheduler.Tests/Features/Clients/CreateClientValidatorTests.cs`
- `tests/JiApp.Scheduler.Tests/Features/Clients/UpdateClientValidatorTests.cs`
- `tests/JiApp.Scheduler.Tests/Features/Services/CreateServiceValidatorTests.cs`
- `tests/JiApp.Scheduler.Tests/Features/Services/UpdateServiceValidatorTests.cs`
- `tests/JiApp.Scheduler.Tests/Features/Expenses/CreateExpenseValidatorTests.cs`
- `tests/JiApp.Scheduler.Tests/Features/Expenses/UpdateExpenseValidatorTests.cs`
- `tests/JiApp.Scheduler.Tests/Features/Clients/ClientReportValidatorTests.cs`
- `tests/JiApp.Scheduler.Tests/Features/Revenues/RevenueReportValidatorTests.cs`

**Pattern:**

```csharp
public sealed class CreateAppointmentValidatorTests : ValidatorTestBase
{
    private sealed class Fixture
    {
        private Fixture() { }

        public CreateAppointmentValidator Sut => new();

        public static Fixture Init() => new();
    }

    [Fact]
    public void Validate_WithValidRequest_Passes()
    {
        var fixture = Fixture.Init();
        var request = new CreateAppointmentRequest(/* valid data */);

        var sut = fixture.Sut;

        var result = sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithMissingServiceId_Fails()
    {
        var fixture = Fixture.Init();
        var request = new CreateAppointmentRequest(ServiceId: 0, /* ... */);

        var sut = fixture.Sut;

        var result = sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }
}
```

- [ ] **Step 1: Refactor all 14 validator test files**
- [ ] **Step 2: Run Scheduler tests**

```bash
dotnet test tests/JiApp.Scheduler.Tests/ --no-build
```

Expected: All tests pass

- [ ] **Step 3: Commit**

```bash
git add tests/JiApp.Scheduler.Tests/Features/
git commit -m "refactor: Scheduler validator tests to unified Fixture/ValidatorTestBase pattern

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 10: Refactor remaining Scheduler test files (4 files)

**Files:**
- `tests/JiApp.Scheduler.Tests/BoardAccessGuardTests.cs`
- `tests/JiApp.Scheduler.Tests/ModuleAuthorizationPolicyTests.cs`
- `tests/JiApp.Scheduler.Tests/SchedulerSettingsTests.cs`
- `tests/JiApp.Scheduler.Tests/Features/Expenses/UpdateExpenseHandlerXssTests.cs`

**Pattern for BoardAccessGuardTests:** Inherit `HandlerTestBase`, use Fixture + Sut + StoreInDb().

**Pattern for ModuleAuthorizationPolicyTests:** Inherit `ValidatorTestBase`, Fixture + Sut.

**Pattern for SchedulerSettingsTests:** Inherit `ValidatorTestBase`, direct assertion (no DB).

**Pattern for XssTests:** Inherit `HandlerTestBase`, Fixture + Sut + StoreInDb().

- [ ] **Step 1: Refactor BoardAccessGuardTests.cs**
- [ ] **Step 2: Refactor ModuleAuthorizationPolicyTests.cs**
- [ ] **Step 3: Refactor SchedulerSettingsTests.cs**
- [ ] **Step 4: Refactor UpdateExpenseHandlerXssTests.cs**
- [ ] **Step 5: Run all Scheduler tests**

```bash
dotnet test tests/JiApp.Scheduler.Tests/
```

Expected: All 20 test files pass

- [ ] **Step 6: Commit**

```bash
git add tests/JiApp.Scheduler.Tests/
git commit -m "refactor: remaining Scheduler test files to unified pattern

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 11: Refactor Scheduler domain tests (6 files)

**Files:**
- `tests/JiApp.Scheduler.Tests/Domain/AppointmentTests.cs`
- `tests/JiApp.Scheduler.Tests/Domain/ServiceTests.cs`
- `tests/JiApp.Scheduler.Tests/Domain/BoardTests.cs`
- `tests/JiApp.Scheduler.Tests/Domain/ClientTests.cs`
- `tests/JiApp.Scheduler.Tests/Domain/ExpenseTests.cs`
- `tests/JiApp.Scheduler.Tests/Domain/PriceTests.cs`

**Pattern:** Domain tests are pure logic — no DB, no mocks. Testing value objects and entity methods directly.

```csharp
public sealed class AppointmentTests
{
    private sealed class Fixture
    {
        private Fixture() { }

        public static Fixture Init() => new();
    }

    [Fact]
    public void Constructor_WithValidParameters_SetsProperties()
    {
        var fixture = Fixture.Init();

        var appointment = new Appointment(
            boardId: 1L, clientId: 2L, serviceId: 3L,
            startTime: DateTime.UtcNow, duration: TimeSpan.FromHours(1));

        appointment.BoardId.Should().Be(1L);
        appointment.ClientId.Should().Be(2L);
    }
}
```

- [ ] **Step 1: Refactor all 6 domain test files**
- [ ] **Step 2: Run Scheduler tests**

```bash
dotnet test tests/JiApp.Scheduler.Tests/ --no-build
```

Expected: All tests pass

- [ ] **Step 3: Commit**

```bash
git add tests/JiApp.Scheduler.Tests/Domain/
git commit -m "refactor: Scheduler domain tests to unified Fixture/Sut pattern

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 12: Identity handler tests (5 files)

**Files:**
- `tests/JiApp.Identity.Tests/Features/Auth/Login/LoginHandlerTests.cs`
- `tests/JiApp.Identity.Tests/Features/Auth/Register/RegisterHandlerTests.cs`
- `tests/JiApp.Identity.Tests/Features/Auth/Refresh/RefreshHandlerTests.cs`
- `tests/JiApp.Identity.Tests/Features/Auth/Me/MeHandlerTests.cs`
- `tests/JiApp.Identity.Tests/Features/Auth/Logout/LogoutHandlerTests.cs`

**Pattern:**

```csharp
public sealed class LoginHandlerTests
{
    private sealed class Fixture
    {
        private Fixture()
        {
            CurrentUser = MockCurrentUserService.GetSuccessful();
            UserManager = new Mock<UserManager<User>>(/* ... */);
            JwtTokenService = new Mock<IJwtTokenService>();
            // all deps default to successful
        }

        public MockCurrentUserService CurrentUser { get; private set; }
        public Mock<UserManager<User>> UserManager { get; }
        public Mock<IJwtTokenService> JwtTokenService { get; }

        public LoginHandler Sut => new(UserManager.Object, JwtTokenService.Object, CurrentUser);

        public static Fixture Init() => new();

        public Fixture WithExistingUser(User user)
        {
            UserManager.Setup(x => x.FindByEmailAsync(user.Email))
                .ReturnsAsync(user);
            return this;
        }

        public Fixture WithCurrentUser(User user)
        {
            CurrentUser.WithReturning(user.Id);
            return this;
        }
    }

    [Fact]
    public async Task HandleAsync_WithValidCredentials_ReturnsSuccess()
    {
        var user = new User { Id = 1L, Email = "test@test.com" };
        var fixture = Fixture.Init().WithExistingUser(user);

        var sut = fixture.Sut;

        var result = await sut.HandleAsync(new LoginRequest(user.Email, "password"), CancellationToken.None);

        AssertSuccess(result);
    }
}
```

Identity tests don't use SQLite — they're pure mock-based. No `HandlerTestBase` needed. Fixture stands alone (no DB, no `IDisposable`).

- [ ] **Step 1: Refactor LoginHandlerTests.cs**
- [ ] **Step 2: Refactor RegisterHandlerTests.cs**
- [ ] **Step 3: Refactor RefreshHandlerTests.cs**
- [ ] **Step 4: Refactor MeHandlerTests.cs**
- [ ] **Step 5: Refactor LogoutHandlerTests.cs** (currently legacy pattern with constructor-level `_sut` field)
- [ ] **Step 6: Run Identity tests**

```bash
dotnet test tests/JiApp.Identity.Tests/
```

Expected: All tests pass

- [ ] **Step 7: Commit**

```bash
git add tests/JiApp.Identity.Tests/
git commit -m "refactor: Identity handler tests to unified Fixture/Sut pattern

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 12: Refactor Identity validator tests (4 files)

**Files:**
- `tests/JiApp.Identity.Tests/Features/Auth/Login/LoginValidatorTests.cs`
- `tests/JiApp.Identity.Tests/Features/Auth/Register/RegisterValidatorTests.cs`
- `tests/JiApp.Identity.Tests/Features/Auth/Refresh/RefreshValidatorTests.cs`
- `tests/JiApp.Identity.Tests/Features/Auth/Logout/LogoutValidatorTests.cs`

**Pattern:** Same as Scheduler validator tests — `ValidatorTestBase`, nested `Fixture` with `Init()` + `Sut`.

- [ ] **Step 1: Refactor all 4 validator test files**
- [ ] **Step 2: Run Identity tests**

```bash
dotnet test tests/JiApp.Identity.Tests/
```

Expected: All tests pass

- [ ] **Step 3: Commit**

```bash
git add tests/JiApp.Identity.Tests/Features/Auth/*/ValidatorTests.cs
git commit -m "refactor: Identity validator tests to unified pattern

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 13: Refactor Identity service tests (2 files)

**Files:**
- `tests/JiApp.Identity.Tests/JwtTokenServiceTests.cs`
- `tests/JiApp.Identity.Tests/RefreshTokenServiceTests.cs`

**Pattern:** Service tests are closer to handler tests — they have dependencies. Use nested `Fixture` with `Init()` + `Sut`, no DB.

- [ ] **Step 1: Refactor JwtTokenServiceTests.cs**
- [ ] **Step 2: Refactor RefreshTokenServiceTests.cs**
- [ ] **Step 3: Run Identity tests**

```bash
dotnet test tests/JiApp.Identity.Tests/ --no-build
```

Expected: All tests pass

- [ ] **Step 4: Commit**

```bash
git add tests/JiApp.Identity.Tests/JwtTokenServiceTests.cs \
        tests/JiApp.Identity.Tests/RefreshTokenServiceTests.cs
git commit -m "refactor: Identity service tests to unified Fixture/Sut pattern

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 14: Refactor YtDownloader tests (7 files)

**Files:**
- `tests/JiApp.YtDownloader.Tests/Features/SearchVideos/SearchVideosHandlerTests.cs`
- `tests/JiApp.YtDownloader.Tests/Features/GetDownloadLink/GetDownloadLinkHandlerTests.cs`
- `tests/JiApp.YtDownloader.Tests/Features/StreamPreview/StreamPreviewHandlerTests.cs`
- `tests/JiApp.YtDownloader.Tests/Features/SearchVideos/SearchVideosValidatorTests.cs`
- `tests/JiApp.YtDownloader.Tests/ModuleAuthorizationPolicyTests.cs`
- `tests/JiApp.YtDownloader.Tests/YoutubeClientValidationTests.cs`
- `tests/JiApp.YtDownloader.Tests/HealthEndpointTests.cs`
- `tests/JiApp.YtDownloader.Tests/SettingsTests.cs`

**Handler test pattern (replaces static `CreateHandler()` factory):**

```csharp
public sealed class SearchVideosHandlerTests
{
    private sealed class Fixture
    {
        private Fixture()
        {
            CurrentUser = MockCurrentUserService.GetSuccessful();
            YoutubeClient = new Mock<IYoutubeClient>();
            HistoryRepo = new Mock<ISearchHistoryRepository>();
        }

        public MockCurrentUserService CurrentUser { get; private set; }
        public Mock<IYoutubeClient> YoutubeClient { get; }
        public Mock<ISearchHistoryRepository> HistoryRepo { get; }

        public SearchVideosHandler Sut => new(
            YoutubeClient.Object, HistoryRepo.Object, CurrentUser,
            new MemoryCache(new MemoryCacheOptions()),
            new Mock<ILogger<SearchVideosHandler>>().Object);

        public static Fixture Init() => new();

        public Fixture WithCurrentUser(User user)
        {
            CurrentUser.WithReturning(user.Id);
            return this;
        }
    }

    [Fact]
    public async Task HandleAsync_PropagatesCancellationTokenToYoutubeClient()
    {
        var fixture = Fixture.Init();

        var sut = fixture.Sut;
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await sut.HandleAsync(new SearchVideosRequest("test", null), cts.Token);

        fixture.YoutubeClient.Verify(
            x => x.SearchAsync("test", It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
```

- [ ] **Step 1: Refactor SearchVideosHandlerTests.cs** (currently Style C — factory method)
- [ ] **Step 2: Refactor GetDownloadLinkHandlerTests.cs** (currently Style C — factory method)
- [ ] **Step 3: Refactor StreamPreviewHandlerTests.cs** (currently Style D — inline construction)
- [ ] **Step 4: Refactor SearchVideosValidatorTests.cs**
- [ ] **Step 5: Refactor ModuleAuthorizationPolicyTests.cs**
- [ ] **Step 6: Refactor YoutubeClientValidationTests.cs**
- [ ] **Step 7: Refactor HealthEndpointTests.cs**
- [ ] **Step 8: Refactor SettingsTests.cs**
- [ ] **Step 9: Run YtDownloader tests**

```bash
dotnet test tests/JiApp.YtDownloader.Tests/
```

Expected: All tests pass

- [ ] **Step 10: Commit**

```bash
git add tests/JiApp.YtDownloader.Tests/
git commit -m "refactor: YtDownloader tests to unified Fixture/Sut pattern

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 15: Refactor Gateway tests (5 files)

**Files:**
- `tests/JiApp.Gateway.Tests/Integration/GatewayIntegrationTests.cs`
- `tests/JiApp.Gateway.Tests/Configuration/GatewaySettingsTests.cs`
- `tests/JiApp.Gateway.Tests/RateLimiting/RateLimitPolicySelectorTests.cs`
- `tests/JiApp.Gateway.Tests/RateLimiting/RateLimitPolicyServiceTests.cs`
- `tests/JiApp.Gateway.Tests/Integration/GatewayWebApplicationFactory.cs`

**Integration test pattern:**

```csharp
public sealed class GatewayIntegrationTests : IntegrationTestBase
{
    private sealed class Fixture
    {
        private Fixture() { }

        public HttpClient Sut { get; }

        public static Fixture Init(HttpClient client) => new() { Sut = client };
    }

    [Fact]
    public async Task GetHealth_ReturnsOk()
    {
        var fixture = Fixture.Init(Client);

        var response = await fixture.Sut.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

**Rate limiting / config test pattern:** Nested `Fixture` with `Init()` + `Sut`, no base class needed.

- [ ] **Step 1: Refactor GatewayIntegrationTests.cs**
- [ ] **Step 2: Refactor GatewaySettingsTests.cs**
- [ ] **Step 3: Refactor RateLimitPolicySelectorTests.cs**
- [ ] **Step 4: Refactor RateLimitPolicyServiceTests.cs**
- [ ] **Step 5: Review GatewayWebApplicationFactory.cs** (may need adjustment for IntegrationTestBase)
- [ ] **Step 6: Run Gateway tests**

```bash
dotnet test tests/JiApp.Gateway.Tests/
```

Expected: All tests pass

- [ ] **Step 7: Commit**

```bash
git add tests/JiApp.Gateway.Tests/
git commit -m "refactor: Gateway tests to unified Fixture/Sut pattern

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 16: Refactor ImageTools tests (3 files)

**Files:**
- `tests/JiApp.ImageTools.Tests/ImageToolsSmokeTests.cs`
- `tests/JiApp.ImageTools.Tests/StartupTests.cs`
- `tests/JiApp.ImageTools.Tests/ImageToolsSettingsTests.cs`

**Pattern:** ImageTools tests are simpler — no DB, no Moq. Use nested `Fixture` with `Init()` + `Sut` where applicable.

- [ ] **Step 1: Refactor ImageToolsSmokeTests.cs**
- [ ] **Step 2: Refactor StartupTests.cs**
- [ ] **Step 3: Refactor ImageToolsSettingsTests.cs**
- [ ] **Step 4: Run ImageTools tests**

```bash
dotnet test tests/JiApp.ImageTools.Tests/
```

Expected: All tests pass

- [ ] **Step 5: Commit**

```bash
git add tests/JiApp.ImageTools.Tests/
git commit -m "refactor: ImageTools tests to unified Fixture/Sut pattern

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 17: Final verification

- [ ] **Step 1: Build everything**

```bash
dotnet build tests/
```

Expected: BUILD SUCCESS — all 6 test projects compile

- [ ] **Step 2: Run full test suite**

```bash
dotnet test tests/ --no-build
```

Expected: All tests pass, zero failures

- [ ] **Step 3: Verify no production code touched**

```bash
git diff --stat origin/main -- src/
```

Expected: No diff (empty — only test files and `.csproj`/`.sln` changed)

- [ ] **Step 4: Spot-check style compliance on 3 representative files**

Open and verify against conventions table:
1. One handler test (e.g., `AppointmentHandlerTests.cs`)
2. One validator test (e.g., `CreateAppointmentValidatorTests.cs`)
3. One mock-based handler test (e.g., `LoginHandlerTests.cs`)

Check: `Fixtures Init()` + `With_()` builder methods | `Sut` property (singular) | `StoreInDb()` explicit | `AssertSuccess()` / `AssertFailure()` | AAA via blank lines | PascalCase with underscores | `public sealed class` | No raw `.Setup()` in tests

- [ ] **Step 5: Final commit (if any remaining unstaged changes)**

```bash
git add -A
git commit -m "refactor: final polish and verification of test refactoring

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

## Conventions Reference

| Aspect | Convention |
|---|---|
| Test class modifier | `public sealed class` |
| Test method naming | `PascalCase_With_Underscores` |
| Sut property | Always `Sut`, singular |
| AAA | Blank-line separation, no `// Arrange` comments |
| `With_` methods | Take domain values, no logic, pure setters |
| Entity factories | `CreateSomeXxx()` — per-file, creates instances, does NOT persist |
| DB persistence | `StoreInDb()` / `RemoveFromDb()` — explicit calls in test body |
| Mock setup | Never raw `.Setup()` in tests — always via `MockObject` wrappers |
| Mock verify | `fixture.xxx.Mock.Verify(...)` — only in assertions |
| All deps default to success | Only override deviations |
| `ResultCategories.AccessDenied` | Not `Unauthorized` — matches `JiApp.Common` constant |
