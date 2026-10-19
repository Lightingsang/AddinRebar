namespace HPDynamo.McpBridge.Tests.Execution;

using System;
using System.Text.Json;
using System.Threading;
using HPDynamo.McpBridge.Execution;
using Xunit;

public sealed class DesignScriptRunnerTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void ExecuteDesignScript_EmptyOrWhitespaceCode_ThrowsArgumentException(string code)
    {
        var runner = new HeadlessGraphRunner();

        var ex = Assert.Throws<ArgumentException>(() =>
            runner.ExecuteDesignScript(null!, code, null, false, CancellationToken.None, _ => { }));

        Assert.Contains("DesignScript code cannot be empty", ex.Message);
    }

    [Fact]
    public void FormatParameterOverrides_ValidJsonArgs_ProducesSerializedOverrides()
    {
        using var doc = JsonDocument.Parse("""
            {
                "width": 250.0,
                "height": 500.0,
                "name": "Beam_B1"
            }
            """);

        var formattedJson = HeadlessGraphRunner.FormatParameterOverrides(doc.RootElement);

        Assert.NotNull(formattedJson);
        using var resultDoc = JsonDocument.Parse(formattedJson);
        var array = resultDoc.RootElement;
        Assert.Equal(JsonValueKind.Array, array.ValueKind);
        Assert.Equal(3, array.GetArrayLength());

        var first = array[0];
        Assert.Equal("width", first.GetProperty("Name").GetString());
        Assert.Equal("250.0", first.GetProperty("Value").GetString());
    }

    [Fact]
    public void ExtractNodeOutputs_NullController_ReturnsEmptyDictionary()
    {
        var outputs = HeadlessGraphRunner.ExtractNodeOutputs(null, null!);

        Assert.NotNull(outputs);
        Assert.Empty(outputs);
    }

    [Fact]
    public void ExecuteDesignScript_ValidScriptWithoutUiApp_PassesValidationAndThrowsArgumentNullForUiApp()
    {
        var runner = new HeadlessGraphRunner();
        const string script = """
            pt1 = Point.ByCoordinates(0, 0, 0);
            pt2 = Point.ByCoordinates(10, 0, 0);
            line = Line.ByStartPointEndPoint(pt1, pt2);
            """;

        var ex = Assert.Throws<ArgumentNullException>(() =>
            runner.ExecuteDesignScript(null, script, null, false, CancellationToken.None, _ => { }));

        Assert.Equal("uiapp", ex.ParamName);
        Assert.Contains("UIApplication is required", ex.Message);
    }
}
