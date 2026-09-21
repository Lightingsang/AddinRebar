using System;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.McpBridge.Core.Pipe;
using Serilog;

namespace HPExcel.McpBridge.Safety;

/// <summary>
///     3-Layer UI gating guard for the HPExcel MCP ecosystem:
///     - Tier R: Requires IsExecutionEnabled.
///     - Tier W: Requires IsExecutionEnabled + IsWriteEnabled.
///     - Tier D: Requires IsExecutionEnabled + IsWriteEnabled + IsDestructiveEnabled.
///     Disabling an outer gate automatically disables inner gates.
/// </summary>
public sealed class ExcelSafetyGuard
{
    private readonly object _sync = new();
    private bool _isExecutionEnabled;
    private bool _isWriteEnabled;
    private bool _isDestructiveEnabled;

    public const string ExecutionDisabledMessage =
        "Code execution is disabled. Ask the user to tick 'Allow AI execution' in the HPExcel MCP Bridge window.";

    public const string WriteDisabledMessage =
        "Write operations are disabled. Ask the user to tick 'Allow write operations' in the HPExcel MCP Bridge window.";

    public const string DestructiveDisabledMessage =
        "Destructive operations (sheet deletion, clearing cells, running macros) are disabled. Ask the user to tick 'Allow destructive operations' in the HPExcel MCP Bridge window.";

    public (bool Execution, bool Write, bool Destructive) GetState()
    {
        lock (_sync) return (_isExecutionEnabled, _isWriteEnabled, _isDestructiveEnabled);
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
                        _isWriteEnabled = false;
                        _isDestructiveEnabled = false;
                    }
                    changed = true;
                }
            }
            if (changed)
            {
                Log.Information("Excel execution gate: {State}", value ? "ENABLED" : "disabled");
                StateChanged?.Invoke();
            }
        }
    }

    public bool IsWriteEnabled
    {
        get { lock (_sync) return _isWriteEnabled; }
        set
        {
            bool changed = false;
            lock (_sync)
            {
                if (value && !_isExecutionEnabled) value = false;
                if (_isWriteEnabled != value)
                {
                    _isWriteEnabled = value;
                    if (!value) _isDestructiveEnabled = false;
                    changed = true;
                }
            }
            if (changed)
            {
                Log.Information("Excel write gate: {State}", value ? "ENABLED" : "disabled");
                StateChanged?.Invoke();
            }
        }
    }

    public bool IsDestructiveEnabled
    {
        get { lock (_sync) return _isDestructiveEnabled; }
        set
        {
            bool changed = false;
            lock (_sync)
            {
                if (value && (!_isExecutionEnabled || !_isWriteEnabled)) value = false;
                if (_isDestructiveEnabled != value)
                {
                    _isDestructiveEnabled = value;
                    changed = true;
                }
            }
            if (changed)
            {
                Log.Information("Excel destructive gate: {State}", value ? "ENABLED" : "disabled");
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

    public void EnsureWriteAllowed()
    {
        lock (_sync)
        {
            if (!_isExecutionEnabled)
                throw new BridgeRequestException(BridgeErrorCode.ExecutionDisabled, ExecutionDisabledMessage);
            if (!_isWriteEnabled)
                throw new BridgeRequestException(BridgeErrorCode.ExecutionDisabled, WriteDisabledMessage);
        }
    }

    public void EnsureDestructiveAllowed()
    {
        lock (_sync)
        {
            if (!_isExecutionEnabled)
                throw new BridgeRequestException(BridgeErrorCode.ExecutionDisabled, ExecutionDisabledMessage);
            if (!_isWriteEnabled)
                throw new BridgeRequestException(BridgeErrorCode.ExecutionDisabled, WriteDisabledMessage);
            if (!_isDestructiveEnabled)
                throw new BridgeRequestException(BridgeErrorCode.ExecutionDisabled, DestructiveDisabledMessage);
        }
    }

    public void EnsureTierAllowed(ExcelTier tier)
    {
        switch (tier)
        {
            case ExcelTier.Destructive:
                EnsureDestructiveAllowed();
                break;
            case ExcelTier.Write:
                EnsureWriteAllowed();
                break;
            default:
                EnsureExecutionAllowed();
                break;
        }
    }
}
