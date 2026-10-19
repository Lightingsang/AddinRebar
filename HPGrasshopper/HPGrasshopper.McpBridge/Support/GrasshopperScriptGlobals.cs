namespace HPGrasshopper.McpBridge.Support;

using System;
using System.Threading;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using HPRebar.McpBridge.Core.Scripting;
using Rhino;

/// <summary>
///     Globals exposed to Grasshopper Roslyn C# scripts executed via execute_grasshopper_code.
/// </summary>
public sealed class GrasshopperScriptGlobals
{
    public GH_Document? doc { get; }
    public GH_Canvas? canvas { get; }
    public RhinoDoc? rhinoDoc { get; }
    public Document? revitDoc { get; }
    public UIDocument? uidoc { get; }
    public UIApplication? uiapp { get; }
    public CancellationToken ct { get; }
    public Action<string?> log { get; }
    public Action<int, int?, string?> progress { get; }
    public ScriptArgs args { get; }
    public bool dryRun { get; }
    public GrasshopperScriptGlobals globals => this;

    public GrasshopperScriptGlobals(
        GH_Document? ghDocument,
        GH_Canvas? ghCanvas,
        RhinoDoc? activeRhinoDoc,
        Document? activeRevitDoc,
        UIDocument? activeUiDoc,
        UIApplication? activeUiApp,
        CancellationToken cancellationToken,
        Action<string?> logAction,
        Action<int, int?, string?> progressAction,
        ScriptArgs scriptArguments,
        bool isDryRun = false)
    {
        doc = ghDocument;
        canvas = ghCanvas;
        rhinoDoc = activeRhinoDoc;
        revitDoc = activeRevitDoc;
        uidoc = activeUiDoc;
        uiapp = activeUiApp;
        ct = cancellationToken;
        log = logAction;
        progress = progressAction;
        args = scriptArguments;
        dryRun = isDryRun;
    }
}
