namespace HPGrasshopper.McpBridge.Tests.Execution;

using System.Collections.Generic;
using System.Text.Json;
using HPGrasshopper.McpBridge.Execution;
using HPGrasshopper.McpBridge.Tools;
using Xunit;

public sealed class FindingN8DiagnosticsAndCamelCaseTests
{
    [Fact]
    public void MapToExecuteResult_SerializesDtoPropertiesWithStrictCamelCase()
    {
        var sampleResult = new
        {
            Success = false,
            Outputs = new Dictionary<string, object?> { ["Area"] = 100 },
            Errors = new[] { "Calculation error" },
            Warnings = new[] { "Low precision" },
            RejectedInputs = new[] { new PlayerInputFailure("ParamX", "Bad type") },
            DurationMs = 125,
            Error = "Solve failed"
        };

        var execResult = GrasshopperExecutionEngine.MapToExecuteResult(sampleResult);

        Assert.True(execResult.IsError);
        var json = execResult.Value!.Value;

        Assert.True(json.TryGetProperty("success", out _));
        Assert.False(json.TryGetProperty("Success", out _));

        Assert.True(json.TryGetProperty("errors", out var errs));
        Assert.False(json.TryGetProperty("Errors", out _));
        Assert.Single(errs.EnumerateArray());

        Assert.True(json.TryGetProperty("warnings", out var warns));
        Assert.False(json.TryGetProperty("Warnings", out _));
        Assert.Single(warns.EnumerateArray());

        Assert.True(json.TryGetProperty("rejectedInputs", out var rejs));
        Assert.False(json.TryGetProperty("RejectedInputs", out _));
        Assert.Single(rejs.EnumerateArray());

        Assert.True(json.TryGetProperty("durationMs", out _));
        Assert.False(json.TryGetProperty("DurationMs", out _));
    }

    [Fact]
    public void MergeOutcomeValue_PreservesDiagnosticsOnRevitError()
    {
        var lastRun = new
        {
            success = false,
            errors = new[] { "Solve error in Component A" },
            warnings = new[] { "Warning in Component B" },
            rejectedInputs = new[] { new PlayerInputFailure("P1", "NotFound") },
            durationMs = 85,
            error = "Component failed"
        };

        var merged = GrasshopperExecutionEngine.MergeOutcomeValue(lastRun, rolledBack: true, fallbackError: "Transaction aborted");

        Assert.True(merged.TryGetProperty("errors", out var errs));
        Assert.Single(errs.EnumerateArray());

        Assert.True(merged.TryGetProperty("warnings", out var warns));
        Assert.Single(warns.EnumerateArray());

        Assert.True(merged.TryGetProperty("rejectedInputs", out var rejs));
        Assert.Single(rejs.EnumerateArray());

        Assert.True(merged.TryGetProperty("rolledBack", out var rb));
        Assert.True(rb.GetBoolean());

        Assert.True(merged.TryGetProperty("durationMs", out var dur));
        Assert.Equal(85, dur.GetInt64());
    }
}
