using System;

namespace HPRebar.Mcp.Contracts.JsonRpc;

/// <summary>
///     Method names on the server ↔ bridge pipe. Requests flow server → bridge, notifications bridge → server.
///     Names are <c>{host}.{suffix}</c>: the Revit constants are kept verbatim for the deployed Revit
///     bridge, and a bridge dispatches on the suffix so either prefix is accepted.
/// </summary>
public static class JsonRpcMethods
{
    public const string Ping = "revit.ping";
    public const string Context = "revit.context";
    public const string Inspect = "revit.inspect";
    public const string Execute = "revit.execute";
    public const string Cancel = "revit.cancel";
    public const string Analyze = "revit.analyze";

    public const string ProgressNotification = "revit.progress";
    public const string LogNotification = "revit.log";
    public const string StatusNotification = "revit.status";

    // ---- host-neutral suffixes --------------------------------------------------------------------

    public const string PingSuffix = "ping";
    public const string ContextSuffix = "context";
    public const string InspectSuffix = "inspect";
    public const string ExecuteSuffix = "execute";
    public const string CancelSuffix = "cancel";
    public const string AnalyzeSuffix = "analyze";
    public const string ProgressSuffix = "progress";
    public const string LogSuffix = "log";
    public const string StatusSuffix = "status";

    public const string RevitPrefix = "revit.";
    public const string AutocadPrefix = "autocad.";
    public const string NavisPrefix = "navis.";
    public const string EtabsPrefix = "etabs.";
    public const string Civil3dPrefix = "civil3d.";
    public const string Sap2000Prefix = "sap2000.";
    public const string PowerBiPrefix = "powerbi.";
    public const string ExcelPrefix = "excel.";
    public const string RobotPrefix = "robot.";
    public const string TeklaPrefix = "tekla.";

    /// <summary>Builds <c>{prefix}{suffix}</c>; the prefix must end with a dot, otherwise the suffix could not be split off again.</summary>
    public static string For(string prefix, string suffix)
    {
        if (string.IsNullOrEmpty(prefix) || prefix[prefix.Length - 1] != '.') throw new ArgumentException("method prefix must end with '.'", nameof(prefix));
        if (string.IsNullOrEmpty(suffix) || suffix.IndexOf('.') >= 0) throw new ArgumentException("method suffix must be a single segment", nameof(suffix));

        return prefix + suffix;
    }

    /// <summary>The part after the first dot, or the whole name when there is none: <c>autocad.execute</c> → <c>execute</c>.</summary>
    public static string Suffix(string? method)
    {
        if (string.IsNullOrEmpty(method)) return string.Empty;

        var dot = method!.IndexOf('.');

        return dot < 0 ? method : method.Substring(dot + 1);
    }

    /// <summary>True when the method is a progress notification of any host.</summary>
    public static bool IsProgress(string? method) => string.Equals(Suffix(method), ProgressSuffix, StringComparison.Ordinal);

    public static bool IsStatus(string? method) => string.Equals(Suffix(method), StatusSuffix, StringComparison.Ordinal);

    public static bool IsLog(string? method) => string.Equals(Suffix(method), LogSuffix, StringComparison.Ordinal);
}
