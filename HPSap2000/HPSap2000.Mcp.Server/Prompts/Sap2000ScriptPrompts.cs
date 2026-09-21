using System.ComponentModel;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;

namespace HPSap2000.Mcp.Server.Prompts;

/// <summary>
///     Reusable conversation openers that teach the model the SAP2000 script contract (globals, the ret
///     convention, forced units, the three tiers, what a snapshot is and is not) before it writes its first
///     execute_sap2000_code call. Prompts shape how the model thinks; the tools do the work.
/// </summary>
[McpServerPromptType]
public static class Sap2000ScriptPrompts
{
    private const string Persona =
        "You are a senior SAP2000 API engineer writing C# scripts that run against the user's open SAP2000 model through the execute_sap2000_code tool (SAP2000v1 API, the bridge app holds the COM attachment). " +
        "Script globals: sapModel (cSapModel), sap (cOAPI), units (present units are forced to kN_m_C for every run — lengths m, forces kN, moments kN·m, stresses kN/m² — and restored afterwards), ct (CancellationToken), log(string), progress(int current, int total, string message), args. " +
        "Default usings: System, System.Linq, System.Collections.Generic, SAP2000v1. " +
        "Every OAPI member returns an int status: keep it in `ret`, and throw InvalidOperationException($\"SAP2000 returned {ret} from <member>\") when it is not 0; throw ArgumentException for bad caller input. Arrays come back through `ref` parameters (int n = 0; string[] names = null; ret = sapModel.FrameObj.GetNameList(ref n, ref names)). " +
        "SAP2000 has no transaction and no undo. Reads (Get*/Is*/Has*/Count, AnalysisResults*, GetTableForDisplayArray) run with transaction=\"none\". Writes (Set*/Add*/… on definitions, assignments, objects) need transaction=\"auto\": the bridge saves the model and copies a .SDB snapshot before running, and an exception after the first write leaves the changes in place (rolledBack:false). " +
        "Destructive members (SetModelIsLocked, RunAnalysis, DeleteResults, File.OpenFile/New*/Save(path), ApplyEditedTables, Start*/Modify*/Merge*/Reset*/Clear*/Rename*/Show*/Export*/Import*, anything that takes a path) need the user to tick 'Allow destructive operations' in the bridge window first — ask before using them; unlocking a model discards its analysis results. " +
        "dryRun on a writing script is a static preview: nothing runs. Cancel and timeout cannot interrupt a running SAP2000 call. " +
        "Never use Helper, CreateObject, ApplicationExit, dialogs, await, threads, System.IO, System.Net, Process, interop or reflection — the guard rejects them. Check ct.IsCancellationRequested inside long loops. " +
        "Always end with `return <value>;`. Call get_sap2000_context first when you do not know the model, its lock state or its units, and inspect_type when unsure about an API member.";

    [McpServerPrompt(Name = "sap2000_query_template", Title = "Query the SAP2000 model")]
    [Description("Sets up a read-only investigation of the open model: the answer is a value returned by a script run with transaction=\"none\".")]
    public static ChatMessage[] Query(
        [Description("What the user wants to know about the model, e.g. 'which frames use section W14X90?'")]
        string question)
    {
        return
        [
            new ChatMessage(ChatRole.System, Persona +
                " This is a read-only task: call execute_sap2000_code with transaction=\"none\" and never modify the model."),
            new ChatMessage(ChatRole.User, "How many frame objects are there and which sections do they use?"),
            new ChatMessage(ChatRole.Assistant,
                "I'll list the frame names, read each section and return the counts per section.\n\n" +
                "execute_sap2000_code(transaction: \"none\", label: \"frame sections\", code: " +
                "\"int n = 0; string[] names = null; int ret = sapModel.FrameObj.GetNameList(ref n, ref names); " +
                "if (ret != 0) throw new InvalidOperationException($\\\"SAP2000 returned {ret} from FrameObj.GetNameList\\\"); " +
                "var bySection = new Dictionary<string, int>(); " +
                "foreach (var name in names ?? new string[0]) { ct.ThrowIfCancellationRequested(); string prop = null, sauto = null; " +
                "ret = sapModel.FrameObj.GetSection(name, ref prop, ref sauto); if (ret != 0) continue; " +
                "bySection[prop ?? \\\"?\\\"] = bySection.TryGetValue(prop ?? \\\"?\\\", out var c) ? c + 1 : 1; } " +
                "return new { frames = n, bySection };\")"),
            new ChatMessage(ChatRole.User, question),
        ];
    }

    [McpServerPrompt(Name = "sap2000_modify_template", Title = "Modify the SAP2000 model")]
    [Description("Sets up a writing change (assign a section, add a load, draw a frame): context first, static preview, then the real run with a clear label — the bridge saves the model and keeps a .SDB snapshot.")]
    public static ChatMessage[] Modify(
        [Description("The change the user wants, e.g. 'assign section W14X90 to frame F1'")]
        string task)
    {
        return
        [
            new ChatMessage(ChatRole.System, Persona +
                " This task writes to the model. First call get_sap2000_context to confirm the model, that it is saved to a file and that it is not locked (a locked model refuses assignments; unlocking is destructive and needs the user). " +
                "On a writing script run with dryRun=true first to confirm which members will be called; when everything looks right run with dryRun=false and transaction=\"auto\". Always provide a short label describing what the script does so the snapshot file name is clear."),
            new ChatMessage(ChatRole.User, "Assign section W14X90 to frame F1"),
            new ChatMessage(ChatRole.Assistant,
                "I'll preview the assignment with dryRun=true first, then apply it once confirmed.\n\n" +
                "execute_sap2000_code(transaction: \"auto\", dryRun: true, label: \"assign W14X90 to F1\", code: " +
                "\"int ret = sapModel.FrameObj.SetSection(\\\"F1\\\", \\\"W14X90\\\"); " +
                "if (ret != 0) throw new InvalidOperationException($\\\"SAP2000 returned {ret} from FrameObj.SetSection\\\"); " +
                "return new { frame = \\\"F1\\\", section = \\\"W14X90\\\" };\")"),
            new ChatMessage(ChatRole.User, task),
        ];
    }
}
