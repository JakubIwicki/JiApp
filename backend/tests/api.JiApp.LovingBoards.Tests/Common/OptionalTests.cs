using System.Text.Json;
using api.JiApp.LovingBoards.Features.Items.UpdateItem;

namespace api.JiApp.LovingBoards.Tests.Common;

public sealed class OptionalTests
{
    private static readonly JsonSerializerOptions CamelCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Fact]
    public void Deserialize_PresentValue_SetsValue()
    {
        var json = """{"title":"Milk"}""";

        var request = JsonSerializer.Deserialize<UpdateItemRequest>(json, CamelCaseOptions);

        request!.Title.IsSet.Should().BeTrue();
        request.Title.Value.Should().Be("Milk");
    }

    [Fact]
    public void Deserialize_ExplicitNull_SetsNullValue()
    {
        var json = """{"quantity":null}""";

        var request = JsonSerializer.Deserialize<UpdateItemRequest>(json, CamelCaseOptions);

        request!.Quantity.IsSet.Should().BeTrue();
        request.Quantity.Value.Should().BeNull();
    }

    [Fact]
    public void Deserialize_AbsentProperty_IsUnset()
    {
        var json = """{"title":"Milk"}""";

        var request = JsonSerializer.Deserialize<UpdateItemRequest>(json, CamelCaseOptions);

        request!.Quantity.IsSet.Should().BeFalse();
        request.Category.IsSet.Should().BeFalse();
        request.IsRecurring.IsSet.Should().BeFalse();
    }

    [Fact]
    public void RoundTrip_SetValue_PreservesValue()
    {
        var request = new UpdateItemRequest(
            Title: new Optional<string>("Milk"),
            Quantity: new Optional<string?>("2L"),
            Category: new Optional<string?>("Dairy"),
            Note: new Optional<string?>("note"),
            AssigneeUserId: new Optional<long?>(2L),
            ExpiryDate: new Optional<DateTime?>(new DateTime(2030, 1, 1)),
            IsRecurring: new Optional<bool>(false));

        var json = JsonSerializer.Serialize(request, CamelCaseOptions);

        var roundTripped = JsonSerializer.Deserialize<UpdateItemRequest>(json, CamelCaseOptions);
        roundTripped!.Title.IsSet.Should().BeTrue();
        roundTripped.Title.Value.Should().Be("Milk");
        roundTripped.Quantity.Value.Should().Be("2L");
        roundTripped.IsRecurring.Value.Should().BeFalse();
    }

    [Fact]
    public void RoundTrip_SetNull_PreservesNull()
    {
        var request = new UpdateItemRequest(
            Title: new Optional<string>("Milk"),
            Quantity: new Optional<string?>(null),
            Category: new Optional<string?>("Dairy"),
            Note: new Optional<string?>("note"),
            AssigneeUserId: new Optional<long?>(2L),
            ExpiryDate: new Optional<DateTime?>(new DateTime(2030, 1, 1)),
            IsRecurring: new Optional<bool>(false));

        var json = JsonSerializer.Serialize(request, CamelCaseOptions);

        json.Should().Contain("\"quantity\":null");
        var roundTripped = JsonSerializer.Deserialize<UpdateItemRequest>(json, CamelCaseOptions);
        roundTripped!.Quantity.IsSet.Should().BeTrue();
        roundTripped.Quantity.Value.Should().BeNull();
    }

    [Fact]
    public void Serialize_Unset_ThrowsNotSupportedException()
    {
        var request = new UpdateItemRequest();

        var act = () => JsonSerializer.Serialize(request, CamelCaseOptions);

        act.Should().Throw<NotSupportedException>()
            .WithMessage("*inbound-only PATCH contract*");
    }
}
