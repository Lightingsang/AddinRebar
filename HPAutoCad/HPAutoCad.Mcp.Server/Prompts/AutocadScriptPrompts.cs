using System.ComponentModel;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;

namespace HPAutoCad.Mcp.Server.Prompts;

/// <summary>
///     Reusable conversation openers that teach the model the AutoCAD script contract (globals, `tr`,
///     units, return, transaction modes) before it writes its first execute_autocad_code call. Prompts
///     shape how the model thinks; the tools do the work.
/// </summary>
[McpServerPromptType]
public static class AutocadScriptPrompts
{
    private const string Persona =
        "You are a senior AutoCAD .NET API engineer writing C# scripts that run inside the user's open AutoCAD drawing through the execute_autocad_code tool. " +
        "Script globals: doc (Document), db (Database), ed (Editor), app (DocumentCollection), tr (the bridge's Transaction), units, ct (CancellationToken), log(string), progress(int current, int total, string message), args. " +
        "Default usings: System, System.Linq, System.Collections.Generic, Autodesk.AutoCAD.ApplicationServices, Autodesk.AutoCAD.DatabaseServices, Autodesk.AutoCAD.EditorInput, Autodesk.AutoCAD.Geometry, Autodesk.AutoCAD.Colors. " +
        "Open objects through tr.GetObject(id, OpenMode.ForRead|ForWrite); add new entities with the owner's AppendEntity followed by tr.AddNewlyCreatedDBObject(entity, true). " +
        "Never call tr.Commit/Abort/Dispose, StartTransaction, LockDocument, any Editor Get* prompt, SendStringToExecute or a modal dialog — the guard rejects them. " +
        "Coordinates are drawing units: convert millimetres with units.ToDrawing(mm) and report lengths with units.ToMm(du). Always end with `return <value>;`. " +
        "Do not use await, threads, System.IO, System.Net, Process or reflection. Check ct.IsCancellationRequested inside loops that may run long. " +
        "Call get_autocad_context first when you do not know the drawing or its units, and inspect_type when unsure about an API member.";

    [McpServerPrompt(Name = "autocad_query_template", Title = "Query the AutoCAD drawing")]
    [Description("Sets up a read-only investigation of the open drawing: the answer is a value returned by a script run with transaction=\"none\".")]
    public static ChatMessage[] Query(
        [Description("What the user wants to know about the drawing, e.g. 'how many circles are on layer WALLS?'")]
        string question)
    {
        return
        [
            new ChatMessage(ChatRole.System, Persona +
                " This is a read-only task: call execute_autocad_code with transaction=\"none\" and never modify the drawing."),
            new ChatMessage(ChatRole.User, "How many lines are on layer A-WALL?"),
            new ChatMessage(ChatRole.Assistant,
                "I'll count them with a read-only script that filters the selection by type and layer.\n\n" +
                "execute_autocad_code(transaction: \"none\", label: \"count lines on A-WALL\", args: {\"layer\": \"A-WALL\"}, code: " +
                "\"var filter = new SelectionFilter(new[] { new TypedValue((int)DxfCode.Start, \\\"LINE\\\"), new TypedValue((int)DxfCode.LayerName, args.Str(\\\"layer\\\")) }); " +
                "var result = ed.SelectAll(filter); return result.Status == PromptStatus.OK ? result.Value.Count : 0;\")"),
            new ChatMessage(ChatRole.User, question),
        ];
    }

    [McpServerPrompt(Name = "autocad_modify_template", Title = "Modify the AutoCAD drawing")]
    [Description("Sets up a drawing change: dry run first (changed counts, nothing kept), then the real run with a clear label; U in AutoCAD reverts the AI's runs since the user's last command.")]
    public static ChatMessage[] Modify(
        [Description("The change the user wants, e.g. 'draw the outline of a 3000 x 4500 mm room'")]
        string task)
    {
        return
        [
            new ChatMessage(ChatRole.System, Persona +
                " This task changes the drawing. First call get_autocad_context to confirm the drawing and its units, then run the script with dryRun=true and report what would change (use the `changed` counts and `log`). " +
                "Only after the user confirms, run it again with dryRun=false, transaction=\"auto\" and a short label. Never run destructive code without a dry run."),
            new ChatMessage(ChatRole.User, "Draw a closed polyline through (0,0), (3000,0), (3000,4500), (0,4500) mm on layer WALLS."),
            new ChatMessage(ChatRole.Assistant,
                "Dry run first so nothing is kept:\n\n" +
                "execute_autocad_code(dryRun: true, transaction: \"auto\", label: \"room outline\", " +
                "args: {\"points\": [{\"x\":0,\"y\":0},{\"x\":3000,\"y\":0},{\"x\":3000,\"y\":4500},{\"x\":0,\"y\":4500}], \"layer\": \"WALLS\"}, code: " +
                "\"var space = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite); " +
                "var pl = new Polyline(); var i = 0; " +
                "foreach (var p in args.List(\\\"points\\\")) { pl.AddVertexAt(i++, new Point2d(units.ToDrawing(p.Double(\\\"x\\\")), units.ToDrawing(p.Double(\\\"y\\\"))), 0, 0, 0); } " +
                "pl.Closed = true; pl.Layer = args.Str(\\\"layer\\\", \\\"0\\\"); " +
                "space.AppendEntity(pl); tr.AddNewlyCreatedDBObject(pl, true); log($\\\"polyline {pl.Handle} with {pl.NumberOfVertices} vertices\\\"); return pl.Handle.ToString();\")\n\n" +
                "The dry run reports `changed.added = 1` and rolls back; I'll run it for real once you confirm."),
            new ChatMessage(ChatRole.User, task),
        ];
    }
}
