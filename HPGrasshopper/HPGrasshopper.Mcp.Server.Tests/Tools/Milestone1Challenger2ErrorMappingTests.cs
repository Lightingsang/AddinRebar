namespace HPGrasshopper.Mcp.Server.Tests.Tools;

using System.Text.Json;
using HPGrasshopper.Mcp.Server.Tools;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Server.Services;
using ModelContextProtocol.Protocol;
using Xunit;

/// <summary>
///     Adversarial stress and edge-case verification for GrasshopperToolResult (Finding R2).
///     Empirically validates:
///     1. Failure payload detection (success=false, isError=true, mixed, complex).
///     2. Non-failure payload preservation (success=true, empty object, primitives, arrays).
///     3. Exact round-trip JSON diagnostic preservation without corruption or truncation.
/// </summary>
public sealed class Milestone1Challenger2ErrorMappingTests
{
    private readonly ResultFormatter _formatter = new();

    [Theory]
    [InlineData("{\"success\": false}", true)]
    [InlineData("{\"success\": false, \"isError\": false}", true)]
    [InlineData("{\"success\": false, \"isError\": true}", true)]
    [InlineData("{\"success\": true, \"isError\": true}", true)]
    [InlineData("{\"isError\": true}", true)]
    [InlineData("{\"isError\": true, \"message\": \"failed\"}", true)]
    [InlineData("{\"success\": false, \"errors\": [\"e1\", \"e2\"]}", true)]
    public void IsFailure_AdversarialFailurePayloads_ReturnsTrue(string rawJson, bool expected)
    {
        var element = JsonDocument.Parse(rawJson).RootElement;
        Assert.Equal(expected, GrasshopperToolResult.IsFailure(element));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"success\": true}")]
    [InlineData("{\"isError\": false}")]
    [InlineData("{\"success\": true, \"isError\": false}")]
    [InlineData("{\"result\": 42, \"name\": \"ok\"}")]
    [InlineData("{\"success\": \"false\"}")]
    [InlineData("{\"isError\": \"true\"}")]
    [InlineData("{\"success\": null}")]
    [InlineData("{\"isError\": null}")]
    public void IsFailure_AdversarialNonFailureOrMalformedPayloads_ReturnsFalse(string rawJson)
    {
        var element = JsonDocument.Parse(rawJson).RootElement;
        Assert.False(GrasshopperToolResult.IsFailure(element));
    }

    [Theory]
    [InlineData("\"plain text\"")]
    [InlineData("12345")]
    [InlineData("true")]
    [InlineData("false")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("[1, 2, 3]")]
    [InlineData("[{\"success\": false}]")]
    public void IsFailure_PrimitivesAndArrays_ReturnsFalse(string rawJson)
    {
        var element = JsonDocument.Parse(rawJson).RootElement;
        Assert.False(GrasshopperToolResult.IsFailure(element));
    }

    [Fact]
    public void From_WhenSuccessFalseWithErrorsAndIsErrorTrue_SetsIsErrorAndPreservesFullJson()
    {
        var payload = new
        {
            success = false,
            isError = true,
            error = "Component execution exception",
            errors = new[] { "Input port 0 missing wire", "Divide by zero in Math.Div" },
            warnings = new[] { "Type coercion from string to number" },
            context = new { definitionGuid = "6ba7b810-9dad-11d1-80b4-00c04fd430c8", iteration = 4 },
            rolledBack = true
        };

        var element = JsonSerializer.SerializeToElement(payload);
        var result = GrasshopperToolResult.From(element, _formatter);

        Assert.True(result.IsError);
        Assert.Single(result.Content);
        var textBlock = Assert.IsType<TextContentBlock>(result.Content[0]);

        using var doc = JsonDocument.Parse(textBlock.Text);
        var root = doc.RootElement;
        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.True(root.GetProperty("isError").GetBoolean());
        Assert.Equal("Component execution exception", root.GetProperty("error").GetString());
        Assert.Equal(2, root.GetProperty("errors").GetArrayLength());
        Assert.Equal("Input port 0 missing wire", root.GetProperty("errors")[0].GetString());
        Assert.Equal("Divide by zero in Math.Div", root.GetProperty("errors")[1].GetString());
        Assert.True(root.GetProperty("rolledBack").GetBoolean());
        Assert.Equal(4, root.GetProperty("context").GetProperty("iteration").GetInt32());
    }

    [Fact]
    public void From_EmptyObject_ReturnsNoErrorAndPreservesContent()
    {
        var element = JsonDocument.Parse("{}").RootElement;
        var result = GrasshopperToolResult.From(element, _formatter);

        Assert.False(result.IsError);
        Assert.Single(result.Content);
        var textBlock = Assert.IsType<TextContentBlock>(result.Content[0]);
        Assert.Equal("{}", textBlock.Text.Trim());
    }

    [Fact]
    public void From_PrimitiveValues_ReturnsNoErrorAndPreservesValue()
    {
        var strElem = JsonSerializer.SerializeToElement("valid output text");
        var strResult = GrasshopperToolResult.From(strElem, _formatter);
        Assert.False(strResult.IsError);
        Assert.Contains("valid output text", Assert.IsType<TextContentBlock>(strResult.Content[0]).Text);

        var numElem = JsonSerializer.SerializeToElement(42.5);
        var numResult = GrasshopperToolResult.From(numElem, _formatter);
        Assert.False(numResult.IsError);
        Assert.Contains("42.5", Assert.IsType<TextContentBlock>(numResult.Content[0]).Text);

        var boolElem = JsonSerializer.SerializeToElement(false);
        var boolResult = GrasshopperToolResult.From(boolElem, _formatter);
        Assert.False(boolResult.IsError);
        Assert.Contains("false", Assert.IsType<TextContentBlock>(boolResult.Content[0]).Text);
    }

    [Fact]
    public void From_ArrayValues_ReturnsNoErrorAndPreservesArray()
    {
        var arrElem = JsonSerializer.SerializeToElement(new[] { 10, 20, 30 });
        var result = GrasshopperToolResult.From(arrElem, _formatter);

        Assert.False(result.IsError);
        Assert.Single(result.Content);
        var textBlock = Assert.IsType<TextContentBlock>(result.Content[0]);
        Assert.Contains("[10,20,30]", textBlock.Text.Replace(" ", ""));
    }

    [Fact]
    public void From_UnicodeAndSpecialCharacters_PreservesExactDiagnostics()
    {
        var payload = new
        {
            success = false,
            error = "Lỗi tính toán: Cốt thép D20 không thỏa mãn điều kiện khoảng hở",
            symbol = "Ø20@150",
            quotes = "Text with \"nested quotes\" and \n newline"
        };

        var element = JsonSerializer.SerializeToElement(payload);
        var result = GrasshopperToolResult.From(element, _formatter);

        Assert.True(result.IsError);
        var textBlock = Assert.IsType<TextContentBlock>(result.Content[0]);
        Assert.Contains("Lỗi tính toán: Cốt thép D20", textBlock.Text);
        Assert.Contains("Ø20@150", textBlock.Text);
        Assert.Contains("nested quotes", textBlock.Text);
    }
}
