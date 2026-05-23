using System.Text.Json;
using FluentAssertions;
using JiApp.Common.Abstractions;
using Xunit;

namespace JiApp.Tests.Common;

public class ApiErrorResponseTests
{
    [Fact]
    public void Serialize_WithAllProperties_ProducesCamelCasePropertyNames()
    {
        var response = new ApiErrorResponse("Test error", "Details here", "60");

        var json = JsonSerializer.Serialize(response, ApiErrorResponse.JsonOptions);

        json.Should().Contain("\"error\":");
        json.Should().Contain("\"details\":");
        json.Should().Contain("\"retryAfterSeconds\":");
        json.Should().NotContain("\"Error\":");
        json.Should().NotContain("\"Details\":");
        json.Should().NotContain("\"RetryAfterSeconds\":");
    }

    [Fact]
    public void Deserialize_FromCamelCaseJson_PopulatesAllProperties()
    {
        const string json = """{"error":"Test","details":"Detail","retryAfterSeconds":"30"}""";

        var response = JsonSerializer.Deserialize<ApiErrorResponse>(json, ApiErrorResponse.JsonOptions);

        response.Should().NotBeNull();
        response.Error.Should().Be("Test");
        response.Details.Should().Be("Detail");
        response.RetryAfterSeconds.Should().Be("30");
    }

    [Fact]
    public void Serialize_WithOnlyError_NullPropertiesArePresentAsNull()
    {
        var response = new ApiErrorResponse("Only error");

        var json = JsonSerializer.Serialize(response, ApiErrorResponse.JsonOptions);

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("error").GetString().Should().Be("Only error");
        doc.RootElement.GetProperty("details").GetString().Should().BeNull();
        doc.RootElement.GetProperty("retryAfterSeconds").GetString().Should().BeNull();
    }
}