using JiApp.Scheduler.Features.Common;
using JiApp.Scheduler.Features.Services.UpdateService;

namespace JiApp.Scheduler.Tests.Features.Services.UpdateService;

public sealed class UpdateServiceValidatorTests
{
    private sealed class Fixture
    {
        public UpdateServiceValidator Sut => new();

        public static Fixture Init() => new();
    }

    [Fact]
    public void Validate_WithEmptyName_ReturnsError()
    {
        var fixture = Fixture.Init();
        var request = new UpdateServiceRequest("", "MensHaircut", 30, new PriceRequest(100));

        var result = fixture.Sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithInvalidCategory_ReturnsError()
    {
        var fixture = Fixture.Init();
        var request = new UpdateServiceRequest("Haircut", "InvalidCategory", 30, new PriceRequest(100));

        var result = fixture.Sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithValidRequest_IsValid()
    {
        var fixture = Fixture.Init();
        var request = new UpdateServiceRequest("Haircut", "MensHaircut", 30, new PriceRequest(100));

        var result = fixture.Sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }
}
