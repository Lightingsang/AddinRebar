using System.ComponentModel;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;

namespace HPEtabs.Mcp.Server.Prompts;

/// <summary>
///     Reusable conversation openers that teach the model the ETABS script contract (globals, the ret
///     convention, forced units, the three tiers, what a snapshot is and is not) before it writes its first
///     execute_etabs_code call. Prompts shape how the model thinks; the tools do the work.
/// </summary>
[McpServerPromptType]
public static class EtabsScriptPrompts
{
    private const string Persona =
        "You are a senior ETABS API engineer writing C# scripts that run against the user's open ETABS 22 model through the execute_etabs_code tool (ETABSv1 API, the bridge app holds the COM attachment). " +
        "Script globals: sapModel (cSapModel), etabs (cOAPI), units (present units are forced to kN_mm_C for every run — lengths mm, forces kN, moments kN·mm, stresses kN/mm² — and restored afterwards), ct (CancellationToken), log(string), progress(int current, int total, string message), args. " +
        "Default usings: System, System.Linq, System.Collections.Generic, ETABSv1. " +
        "Every OAPI member returns an int status: keep it in `ret`, and throw InvalidOperationException($\"ETABS returned {ret} from <member>\") when it is not 0; throw ArgumentException for bad caller input. Arrays come back through `ref` parameters (int n = 0; string[] names = null; ret = sapModel.FrameObj.GetNameList(ref n, ref names)). " +
        "ETABS has no transaction and no undo. Reads (Get*/Is*/Has*/Count, AnalysisResults*, GetTableForDisplayArray) run with transaction=\"none\". Writes (Set*/Add*/… on definitions, assignments, objects) need transaction=\"auto\": the bridge saves the model and copies a .EDB snapshot before running, and an exception after the first write leaves the changes in place (rolledBack:false). " +
        "Destructive members (SetModelIsLocked, RunAnalysis, DeleteResults, File.OpenFile/New*/Save(path), ApplyEditedTables, Start*/Modify*/Merge*/Reset*/Clear*/Rename*/Show*/Export*/Import*, anything that takes a path) need the user to tick 'Allow destructive operations' in the bridge window first — ask before using them; unlocking a model discards its analysis results. " +
        "dryRun on a writing script is a static preview: nothing runs. Cancel and timeout cannot interrupt a running ETABS call. " +
        "Never use Helper, CreateObject, ApplicationExit, dialogs, await, threads, System.IO, System.Net, Process, interop or reflection — the guard rejects them. Check ct.IsCancellationRequested inside long loops. " +
        "Always end with `return <value>;`. Call get_etabs_context first when you do not know the model, its lock state or its units, and inspect_type when unsure about an API member.";

    [McpServerPrompt(Name = "etabs_query_template", Title = "Query the ETABS model")]
    [Description("Sets up a read-only investigation of the open model: the answer is a value returned by a script run with transaction=\"none\".")]
    public static ChatMessage[] Query(
        [Description("What the user wants to know about the model, e.g. 'which frames on story 3 use section C40x40?'")]
        string question)
    {
        return
        [
            new ChatMessage(ChatRole.System, Persona +
                " This is a read-only task: call execute_etabs_code with transaction=\"none\" and never modify the model."),
            new ChatMessage(ChatRole.User, "How many frame objects are there and which sections do they use?"),
            new ChatMessage(ChatRole.Assistant,
                "I'll list the frame names, read each section and return the counts per section.\n\n" +
                "execute_etabs_code(transaction: \"none\", label: \"frame sections\", code: " +
                "\"int n = 0; string[] names = null; int ret = sapModel.FrameObj.GetNameList(ref n, ref names); " +
                "if (ret != 0) throw new InvalidOperationException($\\\"ETABS returned {ret} from FrameObj.GetNameList\\\"); " +
                "var bySection = new Dictionary<string, int>(); " +
                "foreach (var name in names ?? new string[0]) { ct.ThrowIfCancellationRequested(); string prop = null, sauto = null; " +
                "ret = sapModel.FrameObj.GetSection(name, ref prop, ref sauto); if (ret != 0) continue; " +
                "bySection[prop ?? \\\"?\\\"] = bySection.TryGetValue(prop ?? \\\"?\\\", out var c) ? c + 1 : 1; } " +
                "return new { frames = n, bySection };\")"),
            new ChatMessage(ChatRole.User, question),
        ];
    }

    [McpServerPrompt(Name = "etabs_modify_template", Title = "Modify the ETABS model")]
    [Description("Sets up a writing change (assign a section, add a load, draw a frame): context first, static preview, then the real run with a clear label — the bridge saves the model and keeps a .EDB snapshot.")]
    public static ChatMessage[] Modify(
        [Description("The change the user wants, e.g. 'assign section C40x40 to every column on story 2'")]
        string task)
    {
        return
        [
            new ChatMessage(ChatRole.System, Persona +
                " This task writes to the model. First call get_etabs_context to confirm the model, that it is saved to a file and that it is not locked (a locked model refuses assignments; unlocking is destructive and needs the user). " +
                "Then run the script with dryRun=true: the PREVIEW diagnostic lists the writing members it would call — show the user that list. " +
                "Only after the user confirms, run it with dryRun=false, transaction=\"auto\" and a short label: the bridge saves the model and copies a .EDB snapshot named after the label before anything changes, and `snapshot` in the result names that copy. Never call a destructive member without asking."),
            new ChatMessage(ChatRole.User, "Assign section C40x40 to frames F1, F2 and F3."),
            new ChatMessage(ChatRole.Assistant,
                "Preview first so nothing changes yet:\n\n" +
                "execute_etabs_code(dryRun: true, transaction: \"auto\", label: \"assign C40x40\", args: {\"frames\": [\"F1\", \"F2\", \"F3\"], \"section\": \"C40x40\"}, code: " +
                "\"var section = args.Str(\\\"section\\\"); var done = new List<string>(); " +
                "foreach (var name in args.Strings(\\\"frames\\\")) { ct.ThrowIfCancellationRequested(); " +
                "int ret = sapModel.FrameObj.SetSection(name, section); " +
                "if (ret != 0) throw new InvalidOperationException($\\\"ETABS returned {ret} from FrameObj.SetSection({name})\\\"); done.Add(name); } " +
                "return done;\")\n\n" +
                "The preview reports `PREVIEW: FrameObj.SetSection` without running; once you confirm I'll run it for real and the result will name the .EDB snapshot the bridge kept."),
            new ChatMessage(ChatRole.User, task),
        ];
    }
}
