namespace HPDynamo.E2E.Tests.Contracts;

using System;
using System.Collections.Generic;
using System.Text.Json;

/// <summary>
/// Domain models used for opaque-box E2E validation of the HPDynamo MCP subsystem.
/// </summary>
public sealed record DynamoInfoRecord(
    bool IsConnected,
    string? ActiveWorkspaceName,
    string? ActiveWorkspacePath,
    int NodesCount,
    int WiresCount,
    string? DynamoVersion,
    string? RevitVersion,
    bool HasUnsavedChanges,
    bool IsExecutionEnabled,
    string? RunType,
    bool IsLiveCanvasAttached);

public sealed record DynamoNodeSpec(
    Guid Id,
    string Name,
    string NodeType,
    double X,
    double Y,
    IReadOnlyList<string> InPorts,
    IReadOnlyList<string> OutPorts,
    Dictionary<string, object?> Properties);

public sealed record DynamoWireSpec(
    Guid SourceNodeId,
    int SourcePortIndex,
    Guid TargetNodeId,
    int TargetPortIndex);

public sealed record DynamoGraphSpec(
    Guid Uuid,
    string Name,
    string Description,
    List<DynamoNodeSpec> Nodes,
    List<DynamoWireSpec> Wires,
    bool HasUnsavedChanges = false)
{
    public string ToJson() => JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });

    public static DynamoGraphSpec? FromJson(string json)
    {
        return JsonSerializer.Deserialize<DynamoGraphSpec>(json);
    }
}

public sealed record DynamoExecutionRequest(
    string Code,
    string ScriptType = "csharp",
    bool DryRun = false,
    int TimeoutSeconds = 60,
    string? ArgsJson = null);

public sealed record DynamoExecutionResult(
    bool Success,
    string? OutputJson,
    bool RolledBack,
    IReadOnlyList<long> CreatedElementIds,
    IReadOnlyList<string> Logs,
    long DurationMs,
    string? ErrorMessage = null,
    int? ErrorCode = null);

public sealed record DynamoScriptQualityResult(
    bool IsValid,
    int LineCount,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings);
