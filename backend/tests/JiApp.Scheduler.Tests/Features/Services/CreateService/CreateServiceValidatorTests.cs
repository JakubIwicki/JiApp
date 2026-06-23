using JiApp.Scheduler.Features.Common;
using JiApp.Scheduler.Features.Services.CreateService;

namespace JiApp.Scheduler.Tests.Features.Services.CreateService;

public sealed class CreateServiceValidatorTests
{
    private sealed class Fixture
    {
        public CreateServiceValidator Sut => new();

        public static Fixture Init() => new();
    }

    [Fact]
    public void Validate_WithEmptyName_ReturnsError()
    {
        var fixture = Fixture.Init();
        var request = new CreateServiceRequest(1, "", "MensHaircut", 30, new PriceRequest(100));

        var result = fixture.Sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithInvalidCategory_ReturnsError()
    {
        var fixture = Fixture.Init();
        var request = new CreateServiceRequest(1, "Haircut", "InvalidCategory", 30, new PriceRequest(100));

        var result = fixture.Sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithZeroDuration_ReturnsError()
    {
        var fixture = Fixture.Init();
        var request = new CreateServiceRequest(1, "Haircut", "MensHaircut", 0, new PriceRequest(100));

        var result = fixture.Sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithValidRequest_IsValid()
    {
        var fixture = Fixture.Init();
        var request = new CreateServiceRequest(1, "Haircut", "MensHaircut", 30, new PriceRequest(100));

        var result = fixture.Sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }
}
