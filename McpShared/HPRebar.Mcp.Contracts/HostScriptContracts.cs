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

    /// <summary>
    ///     Default `using`s of a Civil 3D script: the AutoCAD set (Civil 3D is an AutoCAD vertical, every Civil
    ///     object is an AutoCAD <c>Entity</c> read through <c>tr</c>) plus the Civil API namespaces of
    ///     <c>AeccDbMgd.dll</c> — <c>CivilApplication</c>/<c>CivilDocument</c>, the alignment/surface/corridor/
    ///     pipe/parcel/COGO classes, their styles and the drawing settings (units, zone). Deliberately without
    ///     <c>Autodesk.Civil.DataShortcuts</c> (state outside the drawing), <c>Autodesk.Civil.AeccUiMgd</c>
    ///     (dialogs) and the <c>Autodesk.AECC.Interop</c> COM wrappers, all of which the guard denies; and without
    ///     the AutoCAD AEC engine facade, which does not ship with this bridge.
    /// </summary>
    public static readonly string[] Civil3dImports =
    {
        "System", "System.Linq", "System.Collections.Generic",
        "Autodesk.AutoCAD.ApplicationServices", "Autodesk.AutoCAD.DatabaseServices",
        "Autodesk.AutoCAD.EditorInput", "Autodesk.AutoCAD.Geometry", "Autodesk.AutoCAD.Colors",
        "Autodesk.Civil", "Autodesk.Civil.ApplicationServices", "Autodesk.Civil.DatabaseServices",
        "Autodesk.Civil.DatabaseServices.Styles", "Autodesk.Civil.Settings",
        "HPRebar.McpBridge.Core.Scripting",
    };

    /// <summary>
    ///     Global names a Civil 3D script may use: the AutoCAD set plus `civil`, the active <c>CivilDocument</c>
    ///     (null when the drawing is not a Civil document). `units` converts mm ↔ the Civil drawing unit
    ///     (Meters or Feet from the drawing settings, not INSUNITS); stations and elevations stay in drawing units.
    /// </summary>
    public static readonly string[] Civil3dGlobals = { "doc", "db", "ed", "app", "tr", "units", "civil", "ct", "log", "progress", "args" };

    /// <summary>
    ///     Default `using`s of a SAP2000 script. The API is the managed <c>SAP2000v1.dll</c> wrapper (namespace
    ///     <c>SAP2000v1</c>: <c>cSapModel</c>, <c>cOAPI</c>, <c>eUnits</c>, …).
    /// </summary>
    public static readonly string[] Sap2000Imports =
    {
        "System", "System.Linq", "System.Collections.Generic",
        "SAP2000v1",
        "HPRebar.McpBridge.Core.Scripting",
    };

    /// <summary>
    ///     Global names a SAP2000 script may use: `sapModel` is the attached <c>cSapModel</c>, `sap` the
    ///     <c>cOAPI</c> root, `units` reports the unit system the bridge forces for the run (kN, m, °C).
    ///     Attaching is the bridge's job, so there is no `helper` and no transaction global — SAP2000 has none.
    /// </summary>
    public static readonly string[] Sap2000Globals = { "sapModel", "sap", "units", "ct", "log", "progress", "args" };

    /// <summary>
    ///     Longest SAP2000 run once the user allowed destructive operations (an analysis run, a file open/save
    ///     cannot be interrupted). Shared by the bridge clamp and the server profile like the ETABS constant.
    /// </summary>
    public const int Sap2000HeavyMaxTimeoutSeconds = 600;

    /// <summary>
    ///     Default `using`s of a Power BI script.
    ///     AMO-TOM (<c>Microsoft.AnalysisServices.Tabular</c>) and ADOMD.NET (<c>Microsoft.AnalysisServices.AdomdClient</c>).
    /// </summary>
    public static readonly string[] PowerBiImports =
    {
        "System", "System.Linq", "System.Collections.Generic", "System.Data",
        "Microsoft.AnalysisServices.Tabular", "Microsoft.AnalysisServices.AdomdClient",
        "HPRebar.McpBridge.Core.Scripting",
    };

    /// <summary>
    ///     Global names a Power BI script may use: `model` is the active TOM <c>Model</c>, `server` is the TOM
    ///     <c>Server</c> connected to local Analysis Services, `adomd` is the active <c>AdomdConnection</c> for DAX queries.
    /// </summary>
    public static readonly string[] PowerBiGlobals = { "model", "server", "adomd", "ct", "log", "progress", "args" };

    /// <summary>Longest Power BI run timeout allowed.</summary>
    public const int PowerBiHeavyMaxTimeoutSeconds = 600;

    /// <summary>
    ///     Default `using`s of an Excel script.
    ///     Includes Microsoft.Office.Interop.Excel, ClosedXML.Excel, and bridge scripting.
    /// </summary>
    public static readonly string[] ExcelImports =
    {
        "System", "System.Linq", "System.Collections.Generic",
        "Microsoft.Office.Interop.Excel",
        "ClosedXML.Excel",
        "HPRebar.McpBridge.Core.Scripting",
    };

    /// <summary>
    ///     Global names an Excel script may use: `excel` is the attached Excel.Application,
    ///     `workbook` is the active Workbook (or null), `sheet` is the active Worksheet (or null),
    ///     `ct` is the cancellation token, `log` writes to output, `progress` reports steps, `args` carries parameters.
    /// </summary>
    public static readonly string[] ExcelGlobals = { "excel", "workbook", "sheet", "ct", "log", "progress", "args" };

    /// <summary>Longest Excel run timeout allowed (seconds).</summary>
    public const int ExcelHeavyMaxTimeoutSeconds = 600;

    /// <summary>
    ///     Default `using`s of a Robot Structural Analysis script. The API is the COM interop wrapper
    ///     <c>Interop.RobotOM.dll</c> (namespace <c>RobotOM</c>: <c>RobotApplication</c>, <c>RobotStructure</c>, …).
    /// </summary>
    public static readonly string[] RobotImports =
    {
        "RobotOM", "System", "System.Collections.Generic", "System.Linq",
        "HPRebar.McpBridge.Core.Scripting",
    };

    /// <summary>
    ///     Global names a Robot Structural Analysis script may use: `robot` is the attached <c>RobotApplication</c>,
    ///     `structure` the active <c>RobotStructure</c>, `units` reports the unit system the bridge standardizes
    ///     (Meter for length, kN for force, kN·m for moment, MPa for stress).
    /// </summary>
    public static readonly string[] RobotGlobals = { "robot", "structure", "units", "ct", "log", "progress", "args" };

    /// <summary>
    ///     Longest Robot Structural Analysis run once the user allowed heavy operations (e.g. structural FEA calculations
    ///     via CalcEngine.Calculate() or batch object deletions). The bridge clamps to it and the server profile advertises it.
    /// </summary>
    public const int RobotHeavyMaxTimeoutSeconds = 300;

    /// <summary>
    ///     Default `using`s of a Tekla Structures script. Covers the core Tekla Open API namespaces:
    ///     general structures, model objects (Beam, Column, ContourPlate, RebarGroup), geometry (Point, Vector),
    ///     and catalog definitions.
    /// </summary>
    public static readonly string[] TeklaImports =
    {
        "System", "System.Linq", "System.Collections.Generic",
        "Tekla.Structures", "Tekla.Structures.Model",
        "Tekla.Structures.Geometry3d", "Tekla.Structures.Catalogs",
        "HPRebar.McpBridge.Core.Scripting",
    };

    /// <summary>
    ///     Global names a Tekla Structures script may use: `model` is the active Tekla Model instance,
    ///     `ct` is cooperative cancellation, `log` writes output, `progress` reports steps, and `args` carries parameters.
    /// </summary>
    public static readonly string[] TeklaGlobals = { "model", "ct", "log", "progress", "args" };

    /// <summary>
    ///     Longest Tekla Structures run allowed when heavy operations are enabled (e.g. batch model updates,
    ///     IFC export, drawing generation).
    /// </summary>
    public const int TeklaHeavyMaxTimeoutSeconds = 600;
}
