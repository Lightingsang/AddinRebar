using System.ComponentModel;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;

namespace HPCivil3d.Mcp.Server.Prompts;

/// <summary>
///     Reusable conversation openers that teach the model the Civil 3D script contract (globals, `civil`, `tr`,
///     the two unit layers, return, transaction modes) before it writes its first execute_civil3d_code call.
///     Prompts shape how the model thinks; the tools do the work.
/// </summary>
[McpServerPromptType]
public static class Civil3dScriptPrompts
{
    private const string Persona =
        "You are a senior Civil 3D .NET API engineer writing C# scripts that run inside the user's open Civil 3D drawing through the execute_civil3d_code tool. " +
        "Script globals: doc (Document), db (Database), ed (Editor), app (DocumentCollection), tr (the bridge's Transaction), civil (CivilDocument — the root of every Civil query: GetAlignmentIds, GetSurfaceIds, GetPipeNetworkIds, GetSiteIds, CorridorCollection, CogoPoints, Settings, Styles), " +
        "units, ct (CancellationToken), log(string), progress(int current, int total, string message), args. " +
        "Default usings: System, System.Linq, System.Collections.Generic, Autodesk.AutoCAD.ApplicationServices/DatabaseServices/EditorInput/Geometry/Colors, Autodesk.Civil, Autodesk.Civil.ApplicationServices, Autodesk.Civil.DatabaseServices, Autodesk.Civil.DatabaseServices.Styles, Autodesk.Civil.Settings. " +
        "Both API families define Entity, DBObject and Surface: write Autodesk.Civil.DatabaseServices.Entity / .Surface in full when you mean the Civil one (Alignment, TinSurface, Corridor, Network, Pipe, Structure, Site, Parcel, CogoPoint, Profile are unambiguous). " +
        "Open objects through tr.GetObject(id, OpenMode.ForRead) or OpenMode.ForWrite; Civil objects are created through their static Create methods or the document collections (civil.CogoPoints.Add, Alignment.Create). " +
        "Never call tr.Commit/Abort/Dispose, StartTransaction, LockDocument, any Editor Get* prompt, SendStringToExecute, a modal dialog, Rebuild/RebuildAll/RebuildSnapshot, data shortcuts, the survey database or file import/export members — the guard rejects them. " +
        "Two unit layers: plan coordinates are drawing units — convert millimetres with units.ToDrawing(mm) and report x/y/lengths with units.ToMm(du) — while stations, elevations and areas stay in the Civil drawing unit (units.Label: Meters or Feet) and are labelled so. " +
        "Always end with `return <value>;`. Do not use await, threads, System.IO, System.Net, Process or reflection. Check ct.IsCancellationRequested inside loops over many objects. " +
        "Call get_civil3d_context first when you do not know the drawing, its Civil unit or whether INSUNITS disagrees (insunitsMismatch), and inspect_type when unsure about an API member.";

    [McpServerPrompt(Name = "civil3d_query_template", Title = "Query the Civil 3D drawing")]
    [Description("Sets up a read-only investigation of the open Civil 3D drawing: the answer is a value returned by a script run with transaction=\"none\".")]
    public static ChatMessage[] Query(
        [Description("What the user wants to know about the drawing, e.g. 'how long is each alignment and where does it start?'")]
        string question)
    {
        return
        [
            new ChatMessage(ChatRole.System, Persona +
                " This is a read-only task: call execute_civil3d_code with transaction=\"none\" and never modify the drawing."),
            new ChatMessage(ChatRole.User, "What is the ground elevation at E 312450 m, N 23872 m on surface EG?"),
            new ChatMessage(ChatRole.Assistant,
                "I'll read it with a read-only script: the point comes in as millimetres, FindElevationAtXY takes drawing units and answers in the Civil unit.\n\n" +
                "execute_civil3d_code(transaction: \"none\", label: \"elevation on EG\", args: {\"surface\": \"EG\", \"xMm\": 312450000, \"yMm\": 23872000}, code: " +
                "\"Autodesk.Civil.DatabaseServices.Surface found = null; foreach (ObjectId id in civil.GetSurfaceIds()) { var s = (Autodesk.Civil.DatabaseServices.Surface)tr.GetObject(id, OpenMode.ForRead); if (s.Name == args.Str(\\\"surface\\\")) found = s; } " +
                "if (found == null) throw new ArgumentException(\\\"no such surface\\\"); " +
                "try { return new { elevation = found.FindElevationAtXY(units.ToDrawing(args.Double(\\\"xMm\\\")), units.ToDrawing(args.Double(\\\"yMm\\\"))), unit = units.Label }; } " +
                "catch (Exception ex) when (ex.GetType().Name.Contains(\\\"PointNotOnEntity\\\")) { return new { outside = true }; }\")"),
            new ChatMessage(ChatRole.User, question),
        ];
    }

    [McpServerPrompt(Name = "civil3d_modify_template", Title = "Modify the Civil 3D drawing")]
    [Description("Sets up a drawing change: dry run first (changed counts, nothing kept), then the real run with a clear label; U in Civil 3D reverts the AI's runs since the user's last command.")]
    public static ChatMessage[] Modify(
        [Description("The change the user wants, e.g. 'add a COGO point at E 1000 m, N 2000 m, elevation 12.5'")]
        string task)
    {
        return
        [
            new ChatMessage(ChatRole.System, Persona +
                " This task changes the drawing. First call get_civil3d_context to confirm the drawing and its Civil unit, then run the script with dryRun=true and report what would change (use the `changed` counts and `log`). " +
                "Only after the user confirms, run it again with dryRun=false, transaction=\"auto\" and a short label. A style or label set you name must already exist in the drawing (read civil.Styles first). Never run destructive code without a dry run."),
            new ChatMessage(ChatRole.User, "Add a COGO point at E 1000 m, N 2000 m with elevation 12.5 described SURVEY."),
            new ChatMessage(ChatRole.Assistant,
                "Dry run first so nothing is kept (x/y arrive in mm, the elevation stays in the drawing unit):\n\n" +
                "execute_civil3d_code(dryRun: true, transaction: \"auto\", label: \"cogo point\", " +
                "args: {\"xMm\": 1000000, \"yMm\": 2000000, \"elevation\": 12.5, \"description\": \"SURVEY\"}, code: " +
                "\"var location = new Point3d(units.ToDrawing(args.Double(\\\"xMm\\\")), units.ToDrawing(args.Double(\\\"yMm\\\")), args.Double(\\\"elevation\\\")); " +
                "var id = civil.CogoPoints.Add(location, args.Str(\\\"description\\\", \\\"MCP\\\"), true); " +
                "var point = (CogoPoint)tr.GetObject(id, OpenMode.ForRead); log($\\\"point {point.PointNumber} at {point.Easting}, {point.Northing} {units.Label}\\\"); " +
                "return new { handle = id.Handle.ToString(), number = point.PointNumber, elevation = point.Elevation, unit = units.Label };\")\n\n" +
                "The dry run reports `changed.added` and rolls back; I'll run it for real once you confirm."),
            new ChatMessage(ChatRole.User, task),
        ];
    }
}
