using JiApp.Scheduler.Features.Common;
using JiApp.Scheduler.Features.Services.CreateService;

namespace JiApp.Scheduler.Tests.Features.Services.CreateService;

public sealed class CreateServiceValidatorTests
{
    private readonly CreateServiceValidator _sut = new();

    [Fact]
    public void Validate_WithEmptyName_ReturnsError()
    {
        var request = new CreateServiceRequest(1, "", "MensHaircut", 30, new PriceRequest(100));

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithInvalidCategory_ReturnsError()
    {
        var request = new CreateServiceRequest(1, "Haircut", "InvalidCategory", 30, new PriceRequest(100));

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithZeroDuration_ReturnsError()
    {
        var request = new CreateServiceRequest(1, "Haircut", "MensHaircut", 0, new PriceRequest(100));

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithValidRequest_IsValid()
    {
        var request = new CreateServiceRequest(1, "Haircut", "MensHaircut", 30, new PriceRequest(100));

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }
}