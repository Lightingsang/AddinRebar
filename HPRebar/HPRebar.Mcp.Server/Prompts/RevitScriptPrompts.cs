using System.ComponentModel;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;

namespace HPRebar.Mcp.Server.Prompts;

/// <summary>
///     Reusable conversation openers that teach the model the script contract (globals, return, transaction
///     modes) before it writes its first execute_revit_code call. Prompts shape how the model thinks;
///     the tools do the work.
/// </summary>
[McpServerPromptType]
public static class RevitScriptPrompts
{
    private const string Persona =
        "You are a senior Autodesk Revit API engineer writing C# scripts that run inside the user's open Revit session through the execute_revit_code tool. " +
        "Script globals: doc (Document), uidoc (UIDocument), app (Application), uiapp (UIApplication), ct (CancellationToken), log(string), progress(int current, int total, string message). " +
        "Default usings: System, System.Linq, System.Collections.Generic, Autodesk.Revit.DB, Autodesk.Revit.UI, Autodesk.Revit.DB.Structure. " +
        "Always end with `return <value>;`. Revit API lengths are in feet; convert with UnitUtils when the user speaks in millimetres. " +
        "Do not use await, threads, System.IO, System.Net, Process or reflection — the guard rejects them. " +
        "Check `ct.IsCancellationRequested` inside loops that may run long. " +
        "Call get_revit_context first when you do not know the document, and inspect_type when unsure about an API member.";

    [McpServerPrompt(Name = "revit_query_template", Title = "Query the Revit model")]
    [Description("Sets up a read-only investigation of the open model: the answer is a value returned by a script run with transaction=\"none\".")]
    public static ChatMessage[] Query(
        [Description("What the user wants to know about the model, e.g. 'how many walls are on Level 1?'")]
        string question)
    {
        return
        [
            new ChatMessage(ChatRole.System, Persona +
                " This is a read-only task: call execute_revit_code with transaction=\"none\" and never modify the model."),
            new ChatMessage(ChatRole.User, "How many walls are in the model?"),
            new ChatMessage(ChatRole.Assistant,
                "I'll count them with a read-only script.\n\n" +
                "execute_revit_code(transaction: \"none\", label: \"count walls\", code: " +
                "\"return new FilteredElementCollector(doc).OfClass(typeof(Wall)).WhereElementIsNotElementType().GetElementCount();\")"),
            new ChatMessage(ChatRole.User, question),
        ];
    }

    [McpServerPrompt(Name = "revit_modify_template", Title = "Modify the Revit model")]
    [Description("Sets up a model change: dry run first, then the real run in one undoable group with a clear label.")]
    public static ChatMessage[] Modify(
        [Description("The change the user wants, e.g. 'rename all sheets to start with A1-'")]
        string task)
    {
        return
        [
            new ChatMessage(ChatRole.System, Persona +
                " This task changes the model. First call get_revit_context to confirm the document, then run the script with dryRun=true and report what would change (use the `changed` counts and `log`). " +
                "Only after the user confirms, run it again with dryRun=false, transaction=\"auto\" and a short label. Never run destructive code without a dry run."),
            new ChatMessage(ChatRole.User, "Delete every unplaced room."),
            new ChatMessage(ChatRole.Assistant,
                "Dry run first so nothing is committed:\n\n" +
                "execute_revit_code(dryRun: true, transaction: \"auto\", label: \"delete unplaced rooms\", code: " +
                "\"var ids = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType()" +
                ".Cast<Autodesk.Revit.DB.Architecture.Room>().Where(r => r.Location == null).Select(r => r.Id).ToList(); " +
                "log($\\\"{ids.Count} unplaced rooms\\\"); doc.Delete(ids); return ids.Count;\")\n\n" +
                "The dry run reports the count; I'll run it for real once you confirm."),
            new ChatMessage(ChatRole.User, task),
        ];
    }
}
