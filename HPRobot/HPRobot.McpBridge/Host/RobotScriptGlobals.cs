using System;
using System.Threading;
using HPRebar.McpBridge.Core.Scripting;
using RobotOM;

namespace HPRobot.McpBridge.Host;

/// <summary>
///     Globals exposed to C# Roslyn scripts executed against Autodesk Robot Structural Analysis.
///     Contracts: robot, structure, units, args, log, progress, ct.
/// </summary>
public sealed class RobotScriptGlobals
{
    /// <summary>Active RobotOM.IRobotApplication COM root or null if disconnected.</summary>
    public IRobotApplication? robot { get; set; }

    /// <summary>Active RobotOM.IRobotStructure or null.</summary>
    public IRobotStructure? structure { get; set; }

    /// <summary>Active RobotOM.IRobotUnitMngr or null.</summary>
    public IRobotUnitMngr? units { get; set; }

    /// <summary>Parameters passed to the script.</summary>
    public ScriptArgs args { get; set; } = ScriptArgs.Empty;

    /// <summary>Script log function.</summary>
    public Action<string> log { get; set; } = _ => { };

    /// <summary>Script progress reporter (current, total, message).</summary>
    public Action<int, int?, string?> progress { get; set; } = (_, _, _) => { };

    /// <summary>Cancellation token for the execution.</summary>
    public CancellationToken ct { get; set; }
}
