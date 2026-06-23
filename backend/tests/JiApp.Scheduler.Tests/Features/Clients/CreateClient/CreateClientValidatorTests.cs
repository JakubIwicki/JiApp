using JiApp.Scheduler.Features.Clients.CreateClient;

namespace JiApp.Scheduler.Tests.Features.Clients.CreateClient;

public sealed class CreateClientValidatorTests
{
    private sealed class Fixture
    {
        public CreateClientValidator Sut => new();

        public static Fixture Init() => new();
    }

    [Fact]
    public void Validate_WithEmptyName_ReturnsError()
    {
        var fixture = Fixture.Init();
        var request = new CreateClientRequest(1L, "", null, null);

        var result = fixture.Sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithTooLongName_ReturnsError()
    {
        var fixture = Fixture.Init();
        var request = new CreateClientRequest(1L, new string('a', 201), null, null);

        var result = fixture.Sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithValidRequest_IsValid()
    {
        var fixture = Fixture.Init();
        var request = new CreateClientRequest(1L, "John Doe", "+48123456789", "Regular client");

        var result = fixture.Sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithNullPhone_IsValid()
    {
        var fixture = Fixture.Init();
        var request = new CreateClientRequest(1L, "John Doe", null, null);

        var result = fixture.Sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithInvalidPhone_ReturnsError()
    {
        var fixture = Fixture.Init();
        var request = new CreateClientRequest(1L, "John Doe", "abc", null);

        var result = fixture.Sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithPhoneMissingPlusPrefix_IsValid()
    {
        var fixture = Fixture.Init();
        var request = new CreateClientRequest(1L, "John Doe", "48123456789", null);

        var result = fixture.Sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithTooShortPhone_ReturnsError()
    {
        var fixture = Fixture.Init();
        var request = new CreateClientRequest(1L, "John Doe", "123", null);

        var result = fixture.Sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithHtmlTagInName_ReturnsError()
    {
        var fixture = Fixture.Init();
        var request = new CreateClientRequest(1L, "<script>alert('xss')</script>", null, null);

        var result = fixture.Sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithHtmlTagsInNotes_ReturnsError()
    {
        var fixture = Fixture.Init();
        var request = new CreateClientRequest(1L, "John Doe", null, "<script>alert('xss')</script>");

        var result = fixture.Sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }
}
