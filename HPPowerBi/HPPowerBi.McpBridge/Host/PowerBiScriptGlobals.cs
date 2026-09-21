using System;
using System.Threading;
using HPRebar.McpBridge.Core.Scripting;
using Microsoft.AnalysisServices.AdomdClient;
using Microsoft.AnalysisServices.Tabular;

namespace HPPowerBi.McpBridge.Host;

/// <summary>
///     Global variables injected into Roslyn C# scripts executed via execute_powerbi_code.
///     Script contracts: HostScriptContracts.PowerBiGlobals = { "model", "server", "adomd", "ct", "log", "progress", "args" }.
/// </summary>
public sealed class PowerBiScriptGlobals
{
    public Model model { get; set; } = null!;
    public Server server { get; set; } = null!;
    public AdomdConnection adomd { get; set; } = null!;
    public CancellationToken ct { get; set; }
    public Action<string> log { get; set; } = null!;
    public Action<int, int?, string?> progress { get; set; } = null!;
    public ScriptArgs args { get; set; } = null!;
}
