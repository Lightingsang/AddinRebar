namespace HPRebar.Mcp.Contracts;

/// <summary>
///     The script-facing contract each host bridge promises: the default imports a script sees and the
///     globals it may name. Kept here, as plain strings, so the bridge (compiler options), the server
///     (tool descriptions) and the tests (seed compile checks) read one list — a wrapper that drifts from
///     the bridge's real imports passes a test and fails inside the host.
/// </summary>
public static class HostScriptContracts
{
    /// <summary>Default `using`s of a Revit script, in the order the bridge applies them.</summary>
    public static readonly string[] RevitImports =
    {
        "System", "System.Linq", "System.Collections.Generic",
        "Autodesk.Revit.DB", "Autodesk.Revit.UI", "Autodesk.Revit.DB.Structure",
        "HPRebar.McpBridge.Core.Scripting",
    };

    /// <summary>
    ///     Default `using`s of an AutoCAD script. Deliberately without <c>Autodesk.AutoCAD.Runtime</c>
    ///     (its <c>Exception</c> collides with <c>System.Exception</c>) and without
    ///     <c>Autodesk.AutoCAD.ApplicationServices.Core</c> (its <c>Application</c> collides with the
    ///     one in <c>ApplicationServices</c>).
    /// </summary>
    public static readonly string[] AutocadImports =
    {
        "System", "System.Linq", "System.Collections.Generic",
        "Autodesk.AutoCAD.ApplicationServices", "Autodesk.AutoCAD.DatabaseServices",
        "Autodesk.AutoCAD.EditorInput", "Autodesk.AutoCAD.Geometry", "Autodesk.AutoCAD.Colors",
        "HPRebar.McpBridge.Core.Scripting",
    };

    /// <summary>Global names a Revit script may use (`doc`, `uidoc`, …).</summary>
    public static readonly string[] RevitGlobals = { "doc", "uidoc", "app", "uiapp", "ct", "log", "progress", "args" };

    /// <summary>Global names an AutoCAD script may use; `tr` is the bridge's outermost transaction, `units` converts mm ↔ drawing units.</summary>
    public static readonly string[] AutocadGlobals = { "doc", "db", "ed", "app", "tr", "units", "ct", "log", "progress", "args" };
}
