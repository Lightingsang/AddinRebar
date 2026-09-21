using System;
using System.Text.RegularExpressions;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.McpBridge.Core.Pipe;
using Serilog;

namespace HPPowerBi.McpBridge.Safety;

/// <summary>
///     3-layer safety guard for Power BI operations:
///     1. Human opt-in flags (ExecutionEnabled, MutationEnabled).
///     2. DAX and script pattern validation (blocks XMLA, DDL, session kills).
///     3. Mutation detection to gate model-modifying operations behind explicit user opt-in.
/// </summary>
public sealed class PbiSafetyGuard
{
    private volatile bool _isExecutionEnabled;
    private volatile bool _isMutationEnabled;

    public const string ExecutionDisabledMessage =
        "Code execution is disabled. Ask the user to tick 'Allow AI DAX / code execution' in the HPPowerBi MCP Bridge window.";

    public const string MutationDisabledMessage =
        "Model mutation is disabled. Ask the user to tick 'Allow AI model modification' in the HPPowerBi MCP Bridge window.";

    public bool IsExecutionEnabled
    {
        get => _isExecutionEnabled;
        set
        {
            if (_isExecutionEnabled == value) return;
            _isExecutionEnabled = value;
            if (!value) _isMutationEnabled = false; // Disabling execution automatically turns off mutation
            Log.Information("Power BI execution gate {State}", value ? "ENABLED" : "disabled");
            StateChanged?.Invoke();
        }
    }

    public bool IsMutationEnabled
    {
        get => _isMutationEnabled;
        set
        {
            if (value && !_isExecutionEnabled) value = false;
            if (_isMutationEnabled == value) return;
            _isMutationEnabled = value;
            Log.Information("Power BI mutation gate {State}", value ? "ENABLED" : "disabled");
            StateChanged?.Invoke();
        }
    }

    public event Action? StateChanged;

    /// <summary>
    ///     Ensures that general execution (read or write) is enabled.
    ///     Throws BridgeRequestException(-32001) if disabled.
    /// </summary>
    public void EnsureExecutionAllowed()
    {
        if (!_isExecutionEnabled)
            throw new BridgeRequestException(BridgeErrorCode.ExecutionDisabled, ExecutionDisabledMessage);
    }

    /// <summary>
    ///     Ensures that structural model mutation is enabled.
    ///     Throws BridgeRequestException(-32001) if disabled.
    /// </summary>
    public void EnsureMutationAllowed()
    {
        EnsureExecutionAllowed();

        if (!_isMutationEnabled)
            throw new BridgeRequestException(BridgeErrorCode.ExecutionDisabled, MutationDisabledMessage);
    }

    /// <summary>
    ///     Strips leading whitespace, single-line comments (-- or //), and block comments (/* ... */).
    /// </summary>
    public static string StripLeadingComments(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var span = input.AsSpan().TrimStart();
        while (!span.IsEmpty)
        {
            if (span.StartsWith("/*", StringComparison.Ordinal))
            {
                var endComment = span.IndexOf("*/", StringComparison.Ordinal);
                if (endComment >= 0)
                {
                    span = span.Slice(endComment + 2).TrimStart();
                    continue;
                }

                return string.Empty;
            }

            if (span.StartsWith("//", StringComparison.Ordinal) || span.StartsWith("--", StringComparison.Ordinal))
            {
                var newlineIdx = span.IndexOfAny('\r', '\n');
                if (newlineIdx >= 0)
                {
                    span = span.Slice(newlineIdx).TrimStart();
                    continue;
                }

                return string.Empty;
            }

            break;
        }

        return span.ToString();
    }

    /// <summary>
    ///     Validates that a DAX query does not contain forbidden XMLA, TMSL, DDL, or admin commands.
    /// </summary>
    public static bool ValidateDaxQuery(string? daxQuery, out string? errorMessage)
    {
        errorMessage = null;
        if (string.IsNullOrWhiteSpace(daxQuery))
        {
            errorMessage = "DAX query cannot be empty.";
            return false;
        }

        var stripped = StripLeadingComments(daxQuery);
        if (string.IsNullOrWhiteSpace(stripped))
        {
            errorMessage = "DAX query cannot be empty.";
            return false;
        }

        // 1. Block XMLA envelope commands
        if (stripped.StartsWith("<", StringComparison.Ordinal))
        {
            errorMessage = "Raw XMLA commands (<Batch>, <Create>, <Alter>, etc.) are not allowed in DAX evaluator.";
            return false;
        }

        // 2. Block raw TMSL JSON commands
        if (stripped.StartsWith("{", StringComparison.Ordinal))
        {
            errorMessage = "Raw TMSL JSON commands are not allowed in DAX evaluator.";
            return false;
        }

        // 3. Block administrative session kill commands
        if (Regex.IsMatch(stripped, @"\bKILL\s+SPID\b", RegexOptions.IgnoreCase) ||
            Regex.IsMatch(stripped, @"\bDISCOVER_TRACE\b", RegexOptions.IgnoreCase))
        {
            errorMessage = "Administrative KILL and trace commands are forbidden.";
            return false;
        }

        return true;
    }

    /// <summary>
    ///     Detects whether a C# script contains patterns indicating model mutation.
    /// </summary>
    public static bool IsMutationScript(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;

        // Common mutation calls on TOM Model
        return Regex.IsMatch(code, @"\b(SaveChanges|Measures\.Add|Measures\.Remove|Relationships\.Add|Relationships\.Remove|Tables\.Add|Tables\.Remove|Columns\.Add|Columns\.Remove)\b");
    }
}
