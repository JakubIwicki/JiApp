using JiApp.Identity.Features.Auth.UpdateProfile;

namespace JiApp.Identity.Tests.Features.Auth.UpdateProfile;

public sealed class UpdateProfileValidatorTests : ValidatorTestBase
{
    private sealed class Fixture
    {
        public UpdateProfileValidator Sut => new();

        public static Fixture Init() => new();
    }

    [Fact]
    public void Validate_WithEmptyDisplayName_ReturnsError()
    {
        var fixture = Fixture.Init();
        var request = new UpdateProfileRequest("", "test@test.com");

        var result = fixture.Sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateProfileRequest.DisplayName));
    }

    [Fact]
    public void Validate_WithDisplayNameExceedingMaxLength_ReturnsError()
    {
        var fixture = Fixture.Init();
        var request = new UpdateProfileRequest(new string('A', 51), "test@test.com");

        var result = fixture.Sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateProfileRequest.DisplayName));
    }

    [Fact]
    public void Validate_WithEmptyEmail_ReturnsError()
    {
        var fixture = Fixture.Init();
        var request = new UpdateProfileRequest("Test User", "");

        var result = fixture.Sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateProfileRequest.Email));
    }

    [Fact]
    public void Validate_WithInvalidEmailFormat_ReturnsError()
    {
        var fixture = Fixture.Init();
        var request = new UpdateProfileRequest("Test User", "not-an-email");

        var result = fixture.Sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateProfileRequest.Email));
    }

    [Fact]
    public void Validate_WithValidRequest_IsValid()
    {
        var fixture = Fixture.Init();
        var request = new UpdateProfileRequest("Test User", "test@test.com");

        var result = fixture.Sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }
}
