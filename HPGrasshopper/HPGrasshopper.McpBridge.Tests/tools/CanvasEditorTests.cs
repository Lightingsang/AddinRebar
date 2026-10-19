namespace HPGrasshopper.McpBridge.Tests.Tools;

using System;
using System.Text.Json;
using HPGrasshopper.McpBridge.Tools;
using Xunit;

public sealed class CanvasEditorTests
{
    private readonly CanvasEditor _editor = new();

    [Fact]
    public void ExecuteBatch_NullDocument_ReturnsCleanFailure()
    {
        using var jsonDoc = JsonDocument.Parse("{}");
        var result = _editor.ExecuteBatch(null, jsonDoc.RootElement, false);
        var json = JsonSerializer.Serialize(result);
        using var parsed = JsonDocument.Parse(json);

        Assert.False(parsed.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("No active Grasshopper document is open.", parsed.RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public void Preflight_InvalidCoordinatesNaN_ReturnsValidationError()
    {
        using var jsonDoc = JsonDocument.Parse("""
        {
            "componentsToAdd": [
                { "type": "Addition", "x": "NaN", "y": 100.0 }
            ]
        }
        """);

        var preflight = CanvasBatchValidator.Preflight(jsonDoc.RootElement);
        Assert.False(preflight.IsValid);
        Assert.NotEmpty(preflight.Errors);
        Assert.Contains(preflight.Errors, e => e.Operation == "componentsToAdd" && e.Reason.Contains("finite number"));
    }

    [Fact]
    public void Preflight_MissingType_ReturnsValidationError()
    {
        using var jsonDoc = JsonDocument.Parse("""
        {
            "componentsToAdd": [
                { "x": 100.0, "y": 100.0 }
            ]
        }
        """);

        var preflight = CanvasBatchValidator.Preflight(jsonDoc.RootElement);
        Assert.False(preflight.IsValid);
        Assert.Contains(preflight.Errors, e => e.Operation == "componentsToAdd" && e.Reason.Contains("type"));
    }

    [Fact]
    public void Preflight_DuplicateTempId_ReturnsValidationError()
    {
        using var jsonDoc = JsonDocument.Parse("""
        {
            "componentsToAdd": [
                { "type": "Addition", "tempId": "node1" },
                { "type": "NumberSlider", "tempId": "node1" }
            ]
        }
        """);

        var preflight = CanvasBatchValidator.Preflight(jsonDoc.RootElement);
        Assert.False(preflight.IsValid);
        Assert.Contains(preflight.Errors, e => e.Reason.Contains("Duplicate tempId 'node1'"));
    }

    [Fact]
    public void Preflight_InvalidGuidToRemove_ReturnsValidationError()
    {
        using var jsonDoc = JsonDocument.Parse("""
        {
            "componentsToRemove": [
                "not-a-guid"
            ]
        }
        """);

        var preflight = CanvasBatchValidator.Preflight(jsonDoc.RootElement, findObject: _ => null);
        Assert.False(preflight.IsValid);
        Assert.Contains(preflight.Errors, e => e.Operation == "componentsToRemove" && e.Reason.Contains("Invalid GUID format"));
    }

    [Fact]
    public void Preflight_NonExistentComponentToRemove_ReturnsValidationError()
    {
        var randomGuid = Guid.NewGuid().ToString("D");
        using var jsonDoc = JsonDocument.Parse($$"""
        {
            "componentsToRemove": [
                "{{randomGuid}}"
            ]
        }
        """);

        var preflight = CanvasBatchValidator.Preflight(jsonDoc.RootElement, findObject: _ => null);
        Assert.False(preflight.IsValid);
        Assert.Contains(preflight.Errors, e => e.Operation == "componentsToRemove" && e.Reason.Contains("does not exist on canvas"));
    }

    [Fact]
    public void Preflight_NonExistentWireTarget_ReturnsValidationError()
    {
        using var jsonDoc = JsonDocument.Parse("""
        {
            "wiresToConnect": [
                { "source": "compA", "target": "compB" }
            ]
        }
        """);

        var preflight = CanvasBatchValidator.Preflight(jsonDoc.RootElement, findObject: _ => null);
        Assert.False(preflight.IsValid);
        Assert.Contains(preflight.Errors, e => e.Operation == "wiresToConnect" && e.Reason.Contains("Source 'compA' not found"));
    }

    [Fact]
    public void Preflight_NegativePortIndex_ReturnsValidationError()
    {
        using var jsonDoc = JsonDocument.Parse("""
        {
            "componentsToAdd": [
                { "type": "Addition", "tempId": "nodeA" },
                { "type": "NumberSlider", "tempId": "nodeB" }
            ],
            "wiresToConnect": [
                { "source": "nodeA", "sourceOutput": -1, "target": "nodeB", "targetInput": 0 }
            ]
        }
        """);

        var preflight = CanvasBatchValidator.Preflight(jsonDoc.RootElement, findObject: _ => null);
        Assert.False(preflight.IsValid);
        Assert.Contains(preflight.Errors, e => e.Operation == "wiresToConnect" && e.Reason.Contains("sourceOutput port index must be non-negative"));
    }

    [Fact]
    public void Preflight_UnknownComponentType_RejectsWhenValidatorProvided()
    {
        using var jsonDoc = JsonDocument.Parse("""
        {
            "componentsToAdd": [
                { "type": "NonExistentComponentTypeXYZ" }
            ]
        }
        """);

        var preflight = CanvasBatchValidator.Preflight(jsonDoc.RootElement, canCreateObject: _ => false);
        Assert.False(preflight.IsValid);
        Assert.Contains(preflight.Errors, e => e.Operation == "componentsToAdd" && e.Reason.Contains("Unknown component type"));
    }

    [Fact]
    public void Preflight_ValidBatch_ReturnsSuccess()
    {
        using var jsonDoc = JsonDocument.Parse("""
        {
            "componentsToAdd": [
                { "type": "Addition", "tempId": "addNode", "x": 100.0, "y": 200.0 },
                { "type": "NumberSlider", "tempId": "sliderNode", "x": 50.0, "y": 50.0 }
            ],
            "wiresToConnect": [
                { "source": "sliderNode", "sourceOutput": 0, "target": "addNode", "targetInput": 0 }
            ]
        }
        """);

        var preflight = CanvasBatchValidator.Preflight(jsonDoc.RootElement, findObject: _ => null, canCreateObject: _ => true);
        Assert.True(preflight.IsValid);
        Assert.Empty(preflight.Errors);
    }

    [Fact]
    public void CreatedComponentInfo_RecordProperties_CanBeInstantiated()
    {
        var info = new CreatedComponentInfo(
            0,
            "tempNode",
            Guid.NewGuid().ToString("D"),
            "Number Slider",
            "Num",
            "NumberSlider");

        Assert.Equal(0, info.Index);
        Assert.Equal("tempNode", info.TempId);
        Assert.Equal("Number Slider", info.Name);
        Assert.Equal("Num", info.NickName);
        Assert.Equal("NumberSlider", info.Type);
    }

    [Fact]
    public void Preflight_NonNumericPortIndex_ReturnsValidationError()
    {
        using var jsonDoc = JsonDocument.Parse("""
        {
            "componentsToAdd": [
                { "type": "Addition", "tempId": "nodeA" },
                { "type": "NumberSlider", "tempId": "nodeB" }
            ],
            "wiresToConnect": [
                { "source": "nodeA", "sourceOutput": "invalid_port", "target": "nodeB", "targetInput": 0 }
            ]
        }
        """);

        var preflight = CanvasBatchValidator.Preflight(jsonDoc.RootElement, findObject: _ => null);
        Assert.False(preflight.IsValid);
        Assert.Contains(preflight.Errors, e => e.Operation == "wiresToConnect" && e.Reason.Contains("sourceOutput port index must be a valid integer"));
    }

    [Fact]
    public void Preflight_NullPortIndex_ReturnsValidationError()
    {
        using var jsonDoc = JsonDocument.Parse("""
        {
            "componentsToAdd": [
                { "type": "Addition", "tempId": "nodeA" },
                { "type": "NumberSlider", "tempId": "nodeB" }
            ],
            "wiresToConnect": [
                { "source": "nodeA", "sourceOutput": null, "target": "nodeB", "targetInput": null }
            ]
        }
        """);

        var preflight = CanvasBatchValidator.Preflight(jsonDoc.RootElement, findObject: _ => null);
        Assert.False(preflight.IsValid);
        Assert.Contains(preflight.Errors, e => e.Operation == "wiresToConnect" && e.Reason.Contains("sourceOutput port index must be a valid integer"));
        Assert.Contains(preflight.Errors, e => e.Operation == "wiresToConnect" && e.Reason.Contains("targetInput port index must be a valid integer"));
    }
}
