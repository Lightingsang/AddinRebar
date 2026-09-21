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

    /// <summary>Navisworks Manage; pipe <c>hpnavis-mcp-{version}</c> — the same name the generic branch below produces.</summary>
    public const string NavisHost = "navis";

    /// <summary>
    ///     CSI ETABS; pipe <c>hpetabs-mcp-{version}</c> where the version is CSI's own major number (22 for ETABS 22),
    ///     not a year — again the name the generic branch below produces.
    /// </summary>
    public const string EtabsHost = "etabs";

    /// <summary>
    ///     Autodesk Civil 3D — an AutoCAD vertical that runs on the same acad.exe (R25.1 for 2026). Its own pipe
    ///     <c>hpcivil3d-mcp-{version}</c> so AutoCAD 2026 and Civil 3D 2026 can serve at the same time; again the
    ///     name the generic branch below produces.
    /// </summary>
    public const string Civil3dHost = "civil3d";

    /// <summary>
    ///     CSI SAP2000; pipe <c>hpsap2000-mcp-{version}</c> where the version is CSI's own major number (27 for SAP2000 27).
    /// </summary>
    public const string Sap2000Host = "sap2000";

    /// <summary>
    ///     Microsoft Power BI; pipe <c>hppowerbi-mcp-{version}</c> (e.g. 2026).
    /// </summary>
    public const string PowerBiHost = "powerbi";

    /// <summary>
    ///     Microsoft Excel; pipe <c>hpexcel-mcp-{version}</c> (e.g. 2026).
    /// </summary>
    public const string ExcelHost = "excel";

    /// <summary>
    ///     Autodesk Robot Structural Analysis Professional; pipe <c>hprobot-mcp-{version}</c> (e.g. 2026).
    /// </summary>
    public const string RobotHost = "robot";

    /// <summary>
    ///     Trimble Tekla Structures; pipe <c>hptekla-mcp-{version}</c> (e.g. 2025).
    /// </summary>
    public const string TeklaHost = "tekla";

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
            NavisHost => "hpnavis-mcp-" + version,
            EtabsHost => "hpetabs-mcp-" + version,
            Civil3dHost => "hpcivil3d-mcp-" + version,
            Sap2000Host => "hpsap2000-mcp-" + version,
            PowerBiHost => "hppowerbi-mcp-" + version,
            ExcelHost => "hpexcel-mcp-" + version,
            RobotHost => "hprobot-mcp-" + version,
            TeklaHost => "hptekla-mcp-" + version,
            _ => "hp" + key + "-mcp-" + version,
        };
    }
}
