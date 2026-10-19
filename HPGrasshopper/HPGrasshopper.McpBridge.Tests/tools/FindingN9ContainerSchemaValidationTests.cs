namespace HPGrasshopper.McpBridge.Tests.Tools;

using System;
using System.Text.Json;
using HPGrasshopper.McpBridge.Tools;
using Xunit;

public sealed class FindingN9ContainerSchemaValidationTests
{
    [Theory]
    [InlineData("{\"componentsToAdd\": {\"type\": \"Addition\"}}", "componentsToAdd")]
    [InlineData("{\"componentsToRemove\": \"6ba7b810-9dad-11d1-80b4-00c04fd430c8\"}", "componentsToRemove")]
    [InlineData("{\"wiresToConnect\": {\"source\": \"A\", \"target\": \"B\"}}", "wiresToConnect")]
    [InlineData("{\"wiresToDisconnect\": 12345}", "wiresToDisconnect")]
    public void Preflight_WhenContainerIsNotJsonArray_FailsWithStructuredError(string rawJson, string expectedContainer)
    {
        using var jsonDoc = JsonDocument.Parse(rawJson);

        var preflight = CanvasBatchValidator.Preflight(jsonDoc.RootElement);

        Assert.False(preflight.IsValid);
        Assert.Contains(preflight.Errors, e =>
            e.Operation == expectedContainer &&
            e.Reason.Contains("must be a JSON array", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Preflight_WhenComponentsToAddContainsNonObject_FailsWithStructuredError()
    {
        using var jsonDoc = JsonDocument.Parse("{\"componentsToAdd\": [\"string-item\"]}");

        var preflight = CanvasBatchValidator.Preflight(jsonDoc.RootElement);

        Assert.False(preflight.IsValid);
        Assert.Contains(preflight.Errors, e =>
            e.Operation == "componentsToAdd" &&
            e.Reason.Contains("must be a JSON object", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Preflight_WhenComponentsToRemoveContainsNonString_FailsWithStructuredError()
    {
        using var jsonDoc = JsonDocument.Parse("{\"componentsToRemove\": [12345]}");

        var preflight = CanvasBatchValidator.Preflight(jsonDoc.RootElement);

        Assert.False(preflight.IsValid);
        Assert.Contains(preflight.Errors, e =>
            e.Operation == "componentsToRemove" &&
            e.Reason.Contains("must be a GUID string", StringComparison.OrdinalIgnoreCase));
    }
}
