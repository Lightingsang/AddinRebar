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
        // The AEC engine facade (AecTools) the AEC seeds call; HPAutoCad.Aec ships beside the bridge.
        "HPAutoCad.Aec",
    };

    /// <summary>
    ///     Default `using`s of a Navisworks script. <c>Autodesk.Navisworks.Api.DocumentParts</c> holds the
    ///     document collections (<c>DocumentSelectionSets</c>, <c>DocumentModels</c>); Clash and Timeliner
    ///     live in their own assemblies/namespaces. Deliberately without <c>ApplicationParts</c> (bridge-only
    ///     GUI plumbing), <c>Plugins</c>, and the <c>Interop</c>/<c>ComApi</c>/<c>Automation</c>/<c>Data</c>
    ///     namespaces the guard denies.
    /// </summary>
    public static readonly string[] NavisImports =
    {
        "System", "System.Linq", "System.Collections.Generic",
        "Autodesk.Navisworks.Api", "Autodesk.Navisworks.Api.DocumentParts",
        "Autodesk.Navisworks.Api.Clash", "Autodesk.Navisworks.Api.Timeliner",
        "HPRebar.McpBridge.Core.Scripting",
    };

    /// <summary>Global names a Revit script may use (`doc`, `uidoc`, …).</summary>
    public static readonly string[] RevitGlobals = { "doc", "uidoc", "app", "uiapp", "ct", "log", "progress", "args" };

    /// <summary>Global names an AutoCAD script may use; `tr` is the bridge's outermost transaction, `units` converts mm ↔ drawing units.</summary>
    public static readonly string[] AutocadGlobals = { "doc", "db", "ed", "app", "tr", "units", "ct", "log", "progress", "args" };

    /// <summary>
    ///     Global names a Navisworks script may use: `doc` is the active <c>Document</c>, `app` a small wrapper
    ///     over the static <c>Application</c> (version, documents, clash module present), `units` converts
    ///     mm ↔ <c>Document.Units</c>. The bridge owns the only transaction, so there is no `tr`.
    /// </summary>
    public static readonly string[] NavisGlobals = { "doc", "app", "units", "ct", "log", "progress", "args" };

    /// <summary>
    ///     Longest Navisworks run once the user allowed heavy operations (a clash run or a file append cannot be
    ///     interrupted). The bridge clamps to it at run time and the server profile advertises it; one constant so
    ///     the net48 and net10 sides cannot drift.
    /// </summary>
    public const int NavisHeavyMaxTimeoutSeconds = 600;

    /// <summary>
    ///     Default `using`s of an ETABS script. The API is the managed <c>ETABSv1.dll</c> wrapper (namespace
    ///     <c>ETABSv1</c>: <c>cSapModel</c>, <c>cOAPI</c>, <c>eUnits</c>, …), not the generic <c>CSiAPIv1</c>
    ///     twin. Nothing from <c>System.Runtime.InteropServices</c>: the COM boundary lives inside the wrapper.
    /// </summary>
    public static readonly string[] EtabsImports =
    {
        "System", "System.Linq", "System.Collections.Generic",
        "ETABSv1",
        "HPRebar.McpBridge.Core.Scripting",
    };

    /// <summary>
    ///     Global names an ETABS script may use: `sapModel` is the attached <c>cSapModel</c>, `etabs` the
    ///     <c>cOAPI</c> root, `units` reports the unit system the bridge forces for the run (kN, mm, °C).
    ///     Attaching is the bridge's job, so there is no `helper` and no transaction global — ETABS has none.
    /// </summary>
    public static readonly string[] EtabsGlobals = { "sapModel", "etabs", "units", "ct", "log", "progress", "args" };

    /// <summary>
    ///     Longest ETABS run once the user allowed destructive operations (an analysis run, a file open/save
    ///     cannot be interrupted). Shared by the bridge clamp and the server profile like the Navisworks constant.
    /// </summary>
    public const int EtabsHeavyMaxTimeoutSeconds = 600;
}
