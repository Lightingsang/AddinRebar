using System;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.McpBridge.Core.Pipe;
using Serilog;

namespace HPRobot.McpBridge.Safety;

/// <summary>
///     Safety guard controlling permission gates for the HPRobot MCP ecosystem:
///     - AllowExecution: Master switch enabling MCP script execution (Tier R and Tier W).
///     - AllowHeavyOperations: Gating switch for destructive element deletions and FEA solver execution (Tier D).
///     Disabling the master execution gate automatically disables the heavy operations gate.
/// </summary>
public sealed class RobotSafetyGuard
{
    private readonly object _sync = new();
    private bool _isExecutionEnabled;
    private bool _isHeavyOperationsEnabled;

    public const string ExecutionDisabledMessage =
        "Code execution is disabled. Ask the user to tick 'Allow AI execution' in the HPRobot MCP Bridge window.";

    public const string HeavyOperationsDisabledMessage =
        "Heavy operations (FEA calculations, element deletion, model clear) are disabled. Ask the user to tick 'Allow heavy operations / calculations' in the HPRobot MCP Bridge window.";

    public (bool Execution, bool HeavyOperations) GetState()
    {
        lock (_sync) return (_isExecutionEnabled, _isHeavyOperationsEnabled);
    }

    public bool IsExecutionEnabled
    {
        get { lock (_sync) return _isExecutionEnabled; }
        set
        {
            bool changed = false;
            lock (_sync)
            {
                if (_isExecutionEnabled != value)
                {
                    _isExecutionEnabled = value;
                    if (!value)
                    {
                        _isHeavyOperationsEnabled = false;
                    }
                    changed = true;
                }
            }
            if (changed)
            {
                Log.Information("Robot execution gate: {State}", value ? "ENABLED" : "disabled");
                StateChanged?.Invoke();
            }
        }
    }

    public bool IsHeavyOperationsEnabled
    {
        get { lock (_sync) return _isHeavyOperationsEnabled; }
        set
        {
            bool changed = false;
            lock (_sync)
            {
                if (value && !_isExecutionEnabled)
                {
                    value = false;
                }
                if (_isHeavyOperationsEnabled != value)
                {
                    _isHeavyOperationsEnabled = value;
                    changed = true;
                }
            }
            if (changed)
            {
                Log.Information("Robot heavy operations gate: {State}", value ? "ENABLED" : "disabled");
                StateChanged?.Invoke();
            }
        }
    }

    public event Action? StateChanged;

    public void EnsureExecutionAllowed()
    {
        lock (_sync)
        {
            if (!_isExecutionEnabled)
                throw new BridgeRequestException(BridgeErrorCode.ExecutionDisabled, ExecutionDisabledMessage);
        }
    }

    public void EnsureHeavyOperationsAllowed()
    {
        lock (_sync)
        {
            if (!_isExecutionEnabled)
                throw new BridgeRequestException(BridgeErrorCode.ExecutionDisabled, ExecutionDisabledMessage);
            if (!_isHeavyOperationsEnabled)
                throw new BridgeRequestException(BridgeErrorCode.ExecutionDisabled, HeavyOperationsDisabledMessage);
        }
    }

    public void EnsureTierAllowed(RobotTier tier)
    {
        switch (tier)
        {
            case RobotTier.DeleteHeavy:
                EnsureHeavyOperationsAllowed();
                break;
            default:
                EnsureExecutionAllowed();
                break;
        }
    }
}
