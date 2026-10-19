namespace HPGrasshopper.E2E.Tests.Harness;

using System;
using System.Collections.Generic;
using System.Threading;
using HPGrasshopper.E2E.Tests.Contracts;
using HPRebar.McpBridge.Core.Scripting;

/// <summary>
/// Execution globals exposed to Roslyn C# scripts in Grasshopper E2E test harness.
/// </summary>
public sealed class GrasshopperTestGlobals
{
    public GrasshopperCanvasSpec? doc { get; set; }
    public GrasshopperCanvasSpec? canvas { get; set; }
    public object? rhinoDoc { get; set; }
    public object? revitDoc { get; set; }
    public CancellationToken ct { get; set; }
    public Action<string?> log { get; set; } = _ => { };
    public Action<int, int?, string?> progress { get; set; } = (_, _, _) => { };
    public ScriptArgs args { get; set; } = new(null);
    public bool dryRun { get; set; }
    public List<string> Logs { get; } = new();
    public GrasshopperStateLedger Ledger { get; set; } = null!;
    public long CreateElement() => Ledger?.CreateElement() ?? 0;
}
