namespace HPGrasshopper.E2E.Tests.Contracts;

using System;
using System.Collections.Generic;
using System.Text.Json;

/// <summary>
/// Domain models used for opaque-box E2E validation of the HPGrasshopper MCP subsystem.
/// </summary>
public sealed record GrasshopperInfoRecord(
    bool IsConnected,
    string? ActiveDefinitionName,
    string? ActiveDefinitionPath,
    int ComponentsCount,
    int WiresCount,
    string? SolverStatus,
    string? GrasshopperVersion,
    string? RevitVersion,
    bool HasUnsavedChanges,
    bool IsExecutionEnabled,
    bool IsLiveCanvasAttached,
    bool IsRhinoInsideActive);

public sealed record GrasshopperComponentSpec(
    Guid Id,
    string Name,
    string NickName,
    string Category,
    string SubCategory,
    double X,
    double Y,
    IReadOnlyList<string> InPorts,
    IReadOnlyList<string> OutPorts,
    Dictionary<string, object?> Properties,
    IReadOnlyList<string>? Messages = null,
    bool Selected = false);

public sealed record GrasshopperWireSpec(
    Guid SourceComponentId,
    int SourcePortIndex,
    Guid TargetComponentId,
    int TargetPortIndex);

public sealed record GrasshopperCanvasSpec(
    Guid Uuid,
    string Name,
    string Description,
    List<GrasshopperComponentSpec> Components,
    List<GrasshopperWireSpec> Wires,
    bool HasUnsavedChanges = false)
{
    public string ToJson() => JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });

    public static GrasshopperCanvasSpec? FromJson(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<GrasshopperCanvasSpec>(json);
        }
        catch (JsonException)
        {
            // Corrupted JSON archives gracefully return null fallback
            return null;
        }
    }
}

public sealed record GrasshopperExecutionRequest(
    string Code,
    string Transaction = "auto",
    bool DryRun = false,
    int TimeoutSeconds = 60,
    string? ArgsJson = null);

public sealed record GrasshopperExecutionResult(
    bool Success,
    string? OutputJson,
    bool RolledBack,
    IReadOnlyList<long> CreatedElementIds,
    IReadOnlyList<string> Logs,
    long DurationMs,
    string? ErrorMessage = null,
    int? ErrorCode = null);

public sealed record GrasshopperScriptQualityResult(
    bool IsValid,
    int LineCount,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings);

public sealed record GrasshopperPlayerResult(
    bool Success,
    long DurationMs,
    int InputsApplied,
    Dictionary<string, object> Outputs,
    string? Error = null);

public sealed record GrasshopperImageExportResult(
    bool Success,
    int Width,
    int Height,
    string Mode,
    string OutputPath,
    string Base64,
    string? Error = null);
