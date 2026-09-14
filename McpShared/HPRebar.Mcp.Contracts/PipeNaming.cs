using System;

namespace HPRebar.Mcp.Contracts;

/// <summary>
///     Single source of the named-pipe name so a server and its bridge can never disagree on it.
///     One pipe per host application and major version: a server built for AutoCAD 2026 talks only to
///     a bridge loaded inside AutoCAD 2026, and never to the Revit bridge running on the same machine.
/// </summary>
public static class PipeNaming
{
    /// <summary>Historical Revit prefix; kept verbatim because the deployed Revit bridge listens on it.</summary>
    public const string Prefix = "hprebar-mcp-r";

    public const string RevitHost = "revit";

    public const string AutocadHost = "autocad";

    /// <summary>Revit pipe, e.g. <c>hprebar-mcp-r2026</c>. Unchanged since the first release.</summary>
    public static string For(int revitVersion) => Prefix + revitVersion;

    /// <summary>
    ///     Pipe for any host: <c>hprebar-mcp-r2026</c> for Revit, <c>hpautocad-mcp-2026</c> for AutoCAD.
    ///     Unknown hosts get <c>hp{host}-mcp-{version}</c> so a third host needs no change here.
    /// </summary>
    public static string For(string host, int version)
    {
        if (string.IsNullOrWhiteSpace(host)) throw new ArgumentException("host is required", nameof(host));

        var key = host.Trim().ToLowerInvariant();

        return key switch
        {
            RevitHost => For(version),
            AutocadHost => "hpautocad-mcp-" + version,
            _ => "hp" + key + "-mcp-" + version,
        };
    }
}
