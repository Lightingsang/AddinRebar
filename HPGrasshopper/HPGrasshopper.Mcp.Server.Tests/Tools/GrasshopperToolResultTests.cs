namespace HPGrasshopper.Mcp.Server.Tests.Tools;

using System.Text.Json;
using HPGrasshopper.Mcp.Server.Tools;
using HPRebar.Mcp.Server.Services;
using ModelContextProtocol.Protocol;
using Xunit;

public sealed class GrasshopperToolResultTests
{
    private readonly ResultFormatter _formatter = new();

    [Fact]
    public void From_WhenSuccessFalse_ReturnsErrorWithDiagnosticJson()
    {
        var json = JsonSerializer.SerializeToElement(new
        {
            success = false,
            error = "Definition file not found: missing.gh",
            rolledBack = true
        });

        var result = GrasshopperToolResult.From(json, _formatter);

        Assert.True(result.IsError);
        Assert.Single(result.Content);
        var textBlock = Assert.IsType<TextContentBlock>(result.Content[0]);
        Assert.Contains("missing.gh", textBlock.Text);
        Assert.Contains("\"success\":false", textBlock.Text.Replace(" ", ""));
        Assert.Contains("\"rolledBack\":true", textBlock.Text.Replace(" ", ""));
    }

    [Fact]
    public void From_WhenIsErrorTrue_ReturnsErrorWithDiagnosticJson()
    {
        var json = JsonSerializer.SerializeToElement(new
        {
            isError = true,
            message = "Execution timed out without host tick.",
            rolledBack = true
        });

        var result = GrasshopperToolResult.From(json, _formatter);

        Assert.True(result.IsError);
        Assert.Single(result.Content);
        var textBlock = Assert.IsType<TextContentBlock>(result.Content[0]);
        Assert.Contains("Execution timed out", textBlock.Text);
    }

    [Fact]
    public void From_WhenSuccessTrue_ReturnsSuccessResult()
    {
        var json = JsonSerializer.SerializeToElement(new
        {
            success = true,
            outputs = new { Area = 120.5 },
            durationMs = 45
        });

        var result = GrasshopperToolResult.From(json, _formatter);

        Assert.False(result.IsError);
        Assert.Single(result.Content);
        var textBlock = Assert.IsType<TextContentBlock>(result.Content[0]);
        Assert.Contains("120.5", textBlock.Text);
    }

    [Fact]
    public void From_WhenNonObject_ReturnsSuccessResult()
    {
        var json = JsonSerializer.SerializeToElement("simple string output");

        var result = GrasshopperToolResult.From(json, _formatter);

        Assert.False(result.IsError);
        Assert.Single(result.Content);
    }

    [Fact]
    public void From_WhenComplexDiagnostics_PreservesFullJsonStructure()
    {
        var json = JsonSerializer.SerializeToElement(new
        {
            success = false,
            error = "Preflight validation failed",
            errors = new[] { "Wire cyclic dependency", "Port out of range" },
            warnings = new[] { "Unconnected input port" },
            inputsApplied = 3,
            rolledBack = true
        });

        var result = GrasshopperToolResult.From(json, _formatter);

        Assert.True(result.IsError);
        var textBlock = Assert.IsType<TextContentBlock>(result.Content[0]);
        Assert.Contains("Wire cyclic dependency", textBlock.Text);
        Assert.Contains("Port out of range", textBlock.Text);
        Assert.Contains("Unconnected input port", textBlock.Text);
        Assert.Contains("\"inputsApplied\":3", textBlock.Text.Replace(" ", ""));
    }

    [Theory]
    [InlineData("{\"success\": false}", true)]
    [InlineData("{\"isError\": true}", true)]
    [InlineData("{\"success\": true}", false)]
    [InlineData("{\"isError\": false}", false)]
    [InlineData("{\"other\": 123}", false)]
    public void IsFailure_DetectsExpectedFailureStates(string rawJson, bool expectedFailure)
    {
        var element = JsonDocument.Parse(rawJson).RootElement;
        Assert.Equal(expectedFailure, GrasshopperToolResult.IsFailure(element));
    }
}
