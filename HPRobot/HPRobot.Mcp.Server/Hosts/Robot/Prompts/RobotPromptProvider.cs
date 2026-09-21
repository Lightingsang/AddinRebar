using System.ComponentModel;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;

namespace HPRobot.Mcp.Server.Hosts.Robot.Prompts;

/// <summary>
///     Reusable conversation openers that teach the model the Robot Structural Analysis script contract
///     (globals, metric units, 3 safety tiers, RTD snapshots) before writing execute_robot_code calls.
/// </summary>
[McpServerPromptType]
public static class RobotPromptProvider
{
    private const string Persona =
        "You are a senior structural engineer writing C# scripts for Autodesk Robot Structural Analysis Professional 2026 via execute_robot_code (RobotOM COM API). " +
        "Globals: robot (IRobotApplication), structure (IRobotStructure), units (IRobotUnitMngr; metric units forced during execution: m, kN, kN·m, MPa), ct (CancellationToken), log(string), progress(int current, int total, string message), args. " +
        "Default usings: System, System.Linq, System.Collections.Generic, RobotOM. " +
        "Tiers: " +
        "1. R read-only: Get*/Is*/Has*/Count/Find*/Exist/Query; transaction=\"none\". " +
        "2. W write: Create/Add*/SetLabel*/SetValue*/Store*/Update; transaction=\"auto\": the bridge saves the model and copies a .rtd snapshot before running. " +
        "3. D destructive/heavy: Calculate, Delete*, project.New/Open/Close/SaveAs — requires the user to tick 'Allow heavy/destructive operations' in HPRobot MCP Bridge. " +
        "Always end with `return <value>;`. Call get_robot_context first when you do not know the active model or its calculation status.";

    [McpServerPrompt(Name = "robot_query_template", Title = "Query the Robot model")]
    [Description("Sets up a read-only investigation of the open Robot model: values returned by a script with transaction=\"none\".")]
    public static ChatMessage[] Query(
        [Description("What the user wants to inspect in the model, e.g. 'how many bars are there and what sections do they use?'")]
        string question)
    {
        return
        [
            new ChatMessage(ChatRole.System, Persona +
                " This is a read-only task: call execute_robot_code with transaction=\"none\" and never modify the model."),
            new ChatMessage(ChatRole.User, "How many bars are there and what sections are assigned?"),
            new ChatMessage(ChatRole.Assistant,
                "I'll query the bars from the structure collection, read their section names and count them.\n\n" +
                "execute_robot_code(transaction: \"none\", label: \"query bar sections\", code: " +
                "\"var bCol = structure.Bars.GetAll(); " +
                "var bySec = new Dictionary<string, int>(); " +
                "for (int i = 1; i <= bCol.Count; i++) { ct.ThrowIfCancellationRequested(); " +
                "var bar = (IRobotBar)bCol.Get(i); " +
                "string sec = bar.HasLabel(IRobotLabelType.I_LT_BAR_SECTION) != 0 ? bar.GetLabelName(IRobotLabelType.I_LT_BAR_SECTION) : \\\"(none)\\\"; " +
                "bySec[sec] = bySec.TryGetValue(sec, out var c) ? c + 1 : 1; } " +
                "return new { totalBars = bCol.Count, sections = bySec };\")"),
            new ChatMessage(ChatRole.User, question),
        ];
    }

    [McpServerPrompt(Name = "robot_modify_template", Title = "Modify the Robot model")]
    [Description("Sets up a writing change (draw bars, assign supports or sections, apply loads): context first, static preview with dryRun=true, then apply with transaction=\"auto\".")]
    public static ChatMessage[] Modify(
        [Description("The modification task, e.g. 'create a column from (0,0,0) to (0,0,4) and assign HEA 300'")]
        string task)
    {
        return
        [
            new ChatMessage(ChatRole.System, Persona +
                " This task writes to the model. First call get_robot_context to confirm that a model is open and saved to a file. " +
                "Run with dryRun=true first to preview what would be called; then run with dryRun=false and transaction=\"auto\". Always provide a clear label for the snapshot."),
            new ChatMessage(ChatRole.User, "Assign support 'Fixed' to node 1"),
            new ChatMessage(ChatRole.Assistant,
                "I'll assign support Fixed to node 1 with transaction=\"auto\".\n\n" +
                "execute_robot_code(transaction: \"auto\", dryRun: true, label: \"assign Fixed to node 1\", code: " +
                "\"var sel = structure.Selections.Create(IRobotObjectType.I_OT_NODE); sel.AddOne(1); " +
                "structure.Nodes.SetLabel(sel, IRobotLabelType.I_LT_SUPPORT, \\\"Fixed\\\"); " +
                "return new { node = 1, support = \\\"Fixed\\\" };\")"),
            new ChatMessage(ChatRole.User, task),
        ];
    }

    [McpServerPrompt(Name = "robot_analysis_template", Title = "Run structural calculations")]
    [Description("Sets up FEA analysis: checks calculation status first, confirms AllowHeavyOperations, then runs project.CalcEngine.Calculate().")]
    public static ChatMessage[] Analysis(
        [Description("Analysis task, e.g. 'run structural calculations and check reaction forces'")]
        string task)
    {
        return
        [
            new ChatMessage(ChatRole.System, Persona +
                " This is a heavy calculation task. Call get_robot_context to verify that the model is valid and heavy operations are enabled."),
            new ChatMessage(ChatRole.User, "Run calculations"),
            new ChatMessage(ChatRole.Assistant,
                "I'll run the structural calculations.\n\n" +
                "execute_robot_code(transaction: \"auto\", timeoutSeconds: 300, label: \"run calculations\", code: " +
                "\"var project = robot.Project; project.CalcEngine.AutoGenerateModel = true; " +
                "int ret = project.CalcEngine.Calculate(); " +
                "if (ret != 0) throw new InvalidOperationException($\\\"Calculation returned {ret}\\\"); " +
                "return new { calculated = structure.Results.Available != 0, status = structure.Results.Status.ToString() };\")"),
            new ChatMessage(ChatRole.User, task),
        ];
    }
}
