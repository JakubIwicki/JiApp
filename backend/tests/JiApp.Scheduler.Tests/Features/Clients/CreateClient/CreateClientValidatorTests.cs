using JiApp.Scheduler.Features.Clients.CreateClient;

namespace JiApp.Scheduler.Tests.Features.Clients.CreateClient;

public sealed class CreateClientValidatorTests
{
    private readonly CreateClientValidator _sut = new();

    [Fact]
    public void Validate_WithEmptyName_ReturnsError()
    {
        var request = new CreateClientRequest(1L, "", null, null);

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithTooLongName_ReturnsError()
    {
        var request = new CreateClientRequest(1L, new string('a', 201), null, null);

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithValidRequest_IsValid()
    {
        var request = new CreateClientRequest(1L, "John Doe", "+48123456789", "Regular client");

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithNullPhone_IsValid()
    {
        var request = new CreateClientRequest(1L, "John Doe", null, null);

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithInvalidPhone_ReturnsError()
    {
        var request = new CreateClientRequest(1L, "John Doe", "abc", null);

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithPhoneMissingPlusPrefix_IsValid()
    {
        var request = new CreateClientRequest(1L, "John Doe", "48123456789", null);

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithTooShortPhone_ReturnsError()
    {
        var request = new CreateClientRequest(1L, "John Doe", "123", null);

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }
}