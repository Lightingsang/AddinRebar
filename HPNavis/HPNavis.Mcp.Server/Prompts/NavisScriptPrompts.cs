using System.ComponentModel;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;

namespace HPNavis.Mcp.Server.Prompts;

/// <summary>
///     Reusable conversation openers that teach the model the Navisworks script contract (globals, Search,
///     units, the undo rules, what is heavy) before it writes its first execute_navis_code call. Prompts
///     shape how the model thinks; the tools do the work.
/// </summary>
[McpServerPromptType]
public static class NavisScriptPrompts
{
    private const string Persona =
        "You are a senior Navisworks .NET API engineer writing C# scripts that run inside the user's open Navisworks Manage model through the execute_navis_code tool. " +
        "The runtime is .NET Framework 4.8 (no Span, Random.Shared, async or records). " +
        "Script globals: doc (Document), app (Version/Year, HasClashModule, IsModified), units (units.ToMm(du), units.ToDrawing(mm), units.Label), ct (CancellationToken), log(string), progress(int current, int total, string message), args. " +
        "Default usings: System, System.Linq, System.Collections.Generic, Autodesk.Navisworks.Api, Autodesk.Navisworks.Api.DocumentParts, Autodesk.Navisworks.Api.Clash, Autodesk.Navisworks.Api.Timeliner. " +
        "Find items with a Search (Selection.SelectAll(), SearchConditions.Add(SearchCondition.HasPropertyByDisplayName(category, property).DisplayStringContains(text)), FindAll(doc, false)) instead of walking Descendants; read properties through item.PropertyCategories. " +
        "Navisworks is a review tool: geometry is read-only. Undoable edits are selection sets (doc.SelectionSets.AddCopy), saved viewpoints (doc.SavedViewpoints.AddCopy), comments, permanent appearance overrides (doc.Models.OverridePermanentColor), hidden/required, the current selection, clash tests and result status, TimeLiner tasks; the bridge wraps a run in one Undo entry `MCP: <label>`. " +
        "AppendFile/MergeFile/SaveFile/Export/TestsRunTest are heavy: the user must tick 'Allow heavy operations' first, they are never undone and cannot be interrupted, so ask before using them. " +
        "Never open a Transaction, call Undo/Redo/Rollback, show a dialog or use Document.Database — the guard rejects them. Do not use await, threads, System.IO, System.Net, Process or reflection. Check ct.IsCancellationRequested inside loops that may run long. " +
        "Always end with `return <value>;`. Call get_navis_context first when you do not know the model or its units, and inspect_type when unsure about an API member.";

    [McpServerPrompt(Name = "navis_query_template", Title = "Query the Navisworks model")]
    [Description("Sets up a read-only investigation of the open model: the answer is a value returned by a script run with transaction=\"none\".")]
    public static ChatMessage[] Query(
        [Description("What the user wants to know about the model, e.g. 'how many items have Level = 02 in Architecture.nwc?'")]
        string question)
    {
        return
        [
            new ChatMessage(ChatRole.System, Persona +
                " This is a read-only task: call execute_navis_code with transaction=\"none\" and never modify the model."),
            new ChatMessage(ChatRole.User, "How many items are named like 'Wall' and how big is their overall bounding box?"),
            new ChatMessage(ChatRole.Assistant,
                "I'll search by the Item.Name property and return the count with the combined box in millimetres.\n\n" +
                "execute_navis_code(transaction: \"none\", label: \"count walls\", args: {\"text\": \"Wall\"}, code: " +
                "\"var search = new Search(); search.Selection.SelectAll(); " +
                "search.SearchConditions.Add(SearchCondition.HasPropertyByDisplayName(\\\"Item\\\", \\\"Name\\\").DisplayStringContains(args.Str(\\\"text\\\"))); " +
                "var items = search.FindAll(doc, false); var box = items.BoundingBox(false); " +
                "return new { count = items.Count, sizeMm = box.IsEmpty ? null : new[] { units.ToMm(box.Size.X), units.ToMm(box.Size.Y), units.ToMm(box.Size.Z) } };\")"),
            new ChatMessage(ChatRole.User, question),
        ];
    }

    [McpServerPrompt(Name = "navis_review_template", Title = "Mark up the Navisworks model")]
    [Description("Sets up a review edit (selection set, saved viewpoint, colour override, comment, clash status): dry run first, then the real run with a clear label so it appears as one Undo entry.")]
    public static ChatMessage[] Review(
        [Description("The markup the user wants, e.g. 'create a selection set of every item on Level 03 and colour it red'")]
        string task)
    {
        return
        [
            new ChatMessage(ChatRole.System, Persona +
                " This task edits review data. First call get_navis_context to confirm the model and its units, then run the script with dryRun=true and report what would change (use the `changed` counts and `log`). " +
                "Only after the user confirms, run it again with dryRun=false, transaction=\"auto\" and a short label — that label becomes the Undo entry. Never run a heavy operation (append, save, export, clash run) without asking."),
            new ChatMessage(ChatRole.User, "Make a selection set called 'Doors' of every item whose Item.Type contains 'Door' and colour them blue."),
            new ChatMessage(ChatRole.Assistant,
                "Dry run first so nothing is kept:\n\n" +
                "execute_navis_code(dryRun: true, transaction: \"auto\", label: \"doors set\", args: {\"text\": \"Door\", \"name\": \"Doors\"}, code: " +
                "\"var search = new Search(); search.Selection.SelectAll(); " +
                "search.SearchConditions.Add(SearchCondition.HasPropertyByDisplayName(\\\"Item\\\", \\\"Type\\\").DisplayStringContains(args.Str(\\\"text\\\"))); " +
                "var items = search.FindAll(doc, false); if (items.Count == 0) return \\\"no items matched\\\"; " +
                "doc.SelectionSets.AddCopy(new SelectionSet(items) { DisplayName = args.Str(\\\"name\\\") }); " +
                "doc.Models.OverridePermanentColor(items, Color.FromByteRGB(0, 0, 255)); " +
                "log($\\\"{items.Count} items in set {args.Str(\\\"name\\\")}\\\"); return items.Count;\")\n\n" +
                "The dry run reports `changed.added` = 1 (the set) and undoes it; I'll run it for real once you confirm, and Ctrl+Z in Navisworks reverts `MCP: doors set` in one step."),
            new ChatMessage(ChatRole.User, task),
        ];
    }
}
