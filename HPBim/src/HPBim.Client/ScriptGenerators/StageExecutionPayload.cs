namespace HPBim.Client.ScriptGenerators;

using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;

/// <summary>
///     Represents an executable C# script payload for a pipeline stage with its data arguments.
/// </summary>
public sealed record StageExecutionPayload
{
    public int StageIndex { get; init; }
    public string StageName { get; init; } = string.Empty;
    public string ScriptCode { get; init; } = string.Empty;
    public object? Args { get; init; }
    public string TransactionMode { get; init; } = TransactionModes.Auto;
    public string Label { get; init; } = string.Empty;

    public ExecuteRequest ToExecuteRequest(bool dryRun = false, int timeoutSeconds = 120) =>
        new(
            Code: ScriptCode,
            Transaction: TransactionMode,
            DryRun: dryRun,
            TimeoutSeconds: timeoutSeconds,
            Label: string.IsNullOrWhiteSpace(Label) ? $"Stage {StageIndex}: {StageName}" : Label,
            Args: BridgeJson.ToElement(Args));
}
