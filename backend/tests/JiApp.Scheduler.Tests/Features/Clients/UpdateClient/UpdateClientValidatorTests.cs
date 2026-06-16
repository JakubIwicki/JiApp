using JiApp.Scheduler.Features.Clients.UpdateClient;

namespace JiApp.Scheduler.Tests.Features.Clients.UpdateClient;

public sealed class UpdateClientValidatorTests
{
    private readonly UpdateClientValidator _sut = new();

    [Fact]
    public void Validate_WithEmptyName_ReturnsError()
    {
        var request = new UpdateClientRequest("", null, null);

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithTooLongName_ReturnsError()
    {
        var request = new UpdateClientRequest(new string('a', 201), null, null);

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithValidRequest_IsValid()
    {
        var request = new UpdateClientRequest("John Doe", "+48123456789", "Regular client");

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithNullPhone_IsValid()
    {
        var request = new UpdateClientRequest("John Doe", null, null);

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithInvalidPhone_ReturnsError()
    {
        var request = new UpdateClientRequest("John Doe", "abc", null);

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithPhoneMissingPlusPrefix_IsValid()
    {
        var request = new UpdateClientRequest("John Doe", "48123456789", null);

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithTooShortPhone_ReturnsError()
    {
        var request = new UpdateClientRequest("John Doe", "123", null);

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithHtmlTagInName_ReturnsError()
    {
        var request = new UpdateClientRequest("<script>alert('xss')</script>", null, null);

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }
}