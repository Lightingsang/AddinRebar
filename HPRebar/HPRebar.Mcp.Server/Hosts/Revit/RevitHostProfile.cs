using HPRebar.Mcp.Server.Hosts;

namespace HPRebar.Mcp.Server.Hosts.Revit;

/// <summary>
///     The Revit profile as this exe registers it: the engine's Revit defaults plus this assembly, which
///     is where the seed library and the Revit tool classes are embedded.
/// </summary>
public static class RevitHostProfile
{
    public static readonly HostProfile Instance = HostProfile.Revit.WithHostAssembly(typeof(RevitHostProfile).Assembly);
}
