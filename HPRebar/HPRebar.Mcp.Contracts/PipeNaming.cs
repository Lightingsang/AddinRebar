namespace HPRebar.Mcp.Contracts;

/// <summary>
///     Single source of the named-pipe name so the server and the bridge can never disagree on it.
///     One pipe per Revit major version: a server configured for 2026 talks only to a bridge loaded
///     inside Revit 2026.
/// </summary>
public static class PipeNaming
{
    public const string Prefix = "hprebar-mcp-r";

    public static string For(int revitVersion) => Prefix + revitVersion;
}
