namespace HPRebar.Mcp.Server.Tools.Registry;

/// <summary>
///     Description fragments shared by the registry meta tools. `[Description]` attributes are compile-time
///     constants, so the host cannot be read from the profile there; every host's vocabulary is named once
///     here and the profile-driven parts (`search_tools` hint, registrar text) stay in code.
/// </summary>
internal static class RegistryToolText
{
    public const string Category =
        "One of the host's categories (search_tools lists them): Architecture | Structure | MEP | Annotation | View | Data | Generic for Revit, " +
        "Drawing | Layer | Block | Annotation | Layout | Data | Generic for AutoCAD";

    public const string Transaction =
        "auto (the bridge wraps the run in a transaction) | manual (Revit: the code opens its own Transaction; AutoCAD: same as auto) | none (read-only)";
}
