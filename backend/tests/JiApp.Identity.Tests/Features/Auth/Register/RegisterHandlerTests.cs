using JiApp.Common.Models;
using JiApp.Common.Abstractions;
using JiApp.Identity.Features.Auth.Register;
using JiApp.Identity.Tests.Mocks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.Identity.Tests.Features.Auth.Register;

public sealed class RegisterHandlerTests
{
    private sealed class Fixture
    {
        public MockUserManager UserManagerDouble { get; } = MockUserManager.GetSuccessful();
        public MockUserAccessService AccessServiceDouble { get; } = MockUserAccessService.GetSuccessful();
        public RecordingLogger<RegisterHandler> Logger { get; } = new();

        public RegisterHandler Sut { get; }

        public Fixture()
        {
            Sut = new RegisterHandler(UserManagerDouble, AccessServiceDouble.Object, Logger);
        }

        public Fixture WithSuccessfulCreate(long userId = 7)
        {
            UserManagerDouble.WithCreateAsync("Password1", IdentityResult.Success,
                callback: user => user.Id = userId);
            return this;
        }

        public Fixture WithFailingCreate(string errorDescription)
        {
            UserManagerDouble.WithCreateAsync("weak",
                IdentityResult.Failed(new IdentityError { Description = errorDescription }));
            return this;
        }

        public Fixture WithCreateFailingMultiple()
        {
            UserManagerDouble.WithCreateAsync("weak",
                IdentityResult.Failed(
                    new IdentityError { Description = "Passwords must have at least one uppercase ('A'-'Z')." },
                    new IdentityError { Description = "Passwords must have at least one digit ('0'-'9')." }));
            return this;
        }

        public Fixture WithUniqueConstraintViolation()
        {
            UserManagerDouble.WithCreateThrowsUniqueConstraint();
            return this;
        }

        public Fixture WithFailingDefaultRoleAssignment(long userId)
        {
            AccessServiceDouble.WithFailingDefaultRoleAssignment(userId,
                new InvalidOperationException("DB unavailable"));
            return this;
        }
    }

    [Fact]
    public async Task HandleAsync_ReturnsSuccess_ForValidRegistration()
    {
        var fixture = new Fixture().WithSuccessfulCreate();

        var result = await fixture.Sut.HandleAsync(
            new RegisterRequest("newuser", "new@test.com", "Password1", "New User"), CancellationToken.None);

        AssertSuccess(result);
    }

    [Fact]
    public async Task HandleAsync_AssignsDefaultRole_OnSuccessfulRegistration()
    {
        const long createdUserId = 7;
        var fixture = new Fixture().WithSuccessfulCreate(createdUserId);

        await fixture.Sut.HandleAsync(
            new RegisterRequest("newuser", "new@test.com", "Password1", "New User"), CancellationToken.None);

        fixture.AccessServiceDouble.VerifyAssignedDefaultRole(createdUserId);
    }

    [Fact]
    public async Task HandleAsync_DoesNotAssignRole_WhenRegistrationFails()
    {
        var fixture = new Fixture().WithFailingCreate("Passwords must have at least one uppercase ('A'-'Z').");

        await fixture.Sut.HandleAsync(
            new RegisterRequest("newuser", "new@test.com", "weak", "New User"), CancellationToken.None);

        fixture.AccessServiceDouble.VerifyAssignedDefaultRole_NotCalled();
    }

    [Fact]
    public async Task HandleAsync_ReturnsGenericFailure_OnUniqueConstraintViolation()
    {
        var fixture = new Fixture().WithUniqueConstraintViolation();

        var result = await fixture.Sut.HandleAsync(
            new RegisterRequest("existinguser", "existing@test.com", "Password1", "New User"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Registration failed");
    }

    [Fact]
    public async Task HandleAsync_ReturnsGenericMessage_WhenCreateFails()
    {
        var fixture = new Fixture().WithFailingCreate("Passwords must have at least one uppercase ('A'-'Z').");

        var result = await fixture.Sut.HandleAsync(
            new RegisterRequest("newuser", "new@test.com", "weak", "New User"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Registration failed");
    }

    [Fact]
    public async Task HandleAsync_ReturnsGenericMessage_AndLogsAllErrors_WhenCreateFailsMultiple()
    {
        var fixture = new Fixture().WithCreateFailingMultiple();

        var result = await fixture.Sut.HandleAsync(
            new RegisterRequest("newuser", "new@test.com", "weak", "New User"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Registration failed");
        result.Error.Should().NotContain("uppercase");
        result.Error.Should().NotContain("digit");
        fixture.Logger.Messages.Should().Contain(m => m.Contains("uppercase") && m.Contains("digit"));
    }

    [Fact]
    public async Task HandleAsync_ReturnsGenericMessage_AndLogsRealError_WhenUsernameAlreadyTaken()
    {
        // The UserValidator leak (DuplicateUserName -> "Username 'alice' is already taken.")
        // must not reach the response: both paths — IdentityResult errors and the DB
        // unique-constraint violation — agree on the generic message.
        var fixture = new Fixture().WithFailingCreate("Username 'alice' is already taken.");

        var result = await fixture.Sut.HandleAsync(
            new RegisterRequest("alice", "new@test.com", "weak", "Alice"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Registration failed");
        result.Error.Should().NotContain("alice");
        fixture.Logger.Messages.Should().Contain(m => m.Contains("already taken"));
    }

    [Fact]
    public async Task HandleAsync_CompensatesUserDeletion_WhenDefaultRoleAssignmentFails()
    {
        const long createdUserId = 9;
        var fixture = new Fixture()
            .WithSuccessfulCreate(createdUserId)
            .WithFailingDefaultRoleAssignment(createdUserId);

        var result = await fixture.Sut.HandleAsync(
            new RegisterRequest("newuser", "new@test.com", "Password1", "New User"), CancellationToken.None);

        fixture.UserManagerDouble.VerifyDeletedUser(createdUserId);
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Registration failed");
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        private readonly List<string> _messages = [];

        public IReadOnlyList<string> Messages => _messages;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
            => _messages.Add(formatter(state, exception));

        public bool IsEnabled(LogLevel logLevel) => true;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    }
}
