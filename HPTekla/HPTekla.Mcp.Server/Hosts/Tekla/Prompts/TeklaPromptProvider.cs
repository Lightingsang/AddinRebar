using System.ComponentModel;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;

namespace HPTekla.Mcp.Server.Hosts.Tekla.Prompts;

/// <summary>
///     Reusable conversation openers that teach the model the Tekla Structures script contract
///     (globals, millimetre units, 3 safety tiers, atomic dryRun rollback) before writing execute_tekla_code calls.
/// </summary>
[McpServerPromptType]
public static class TeklaPromptProvider
{
    private const string Persona =
        "You are a senior structural and BIM engineer writing C# scripts for Trimble Tekla Structures 2025.0 via execute_tekla_code (Tekla Open API). " +
        "Globals: model (Tekla.Structures.Model.Model), selector (ModelObjectSelector), ct (CancellationToken), log(string), progress(int current, int total, string message), args (ScriptArgs). " +
        "Units: coordinates and dimensions are in millimetres (mm). " +
        "Default usings: System, System.Linq, System.Collections.Generic, Tekla.Structures, Tekla.Structures.Model, Tekla.Structures.Geometry3d, Tekla.Structures.Catalogs. " +
        "Tiers: " +
        "1. R read-only: Get*/Is*/Has*/Count/Find*/Exist/Query; transaction=\"none\". " +
        "2. W write: Insert/Modify/Delete; transaction=\"auto\": the bridge executes changes in model memory and commits with model.CommitChanges() upon success. " +
        "dryRun=true runs script logic in memory without calling model.CommitChanges(), ensuring zero persistence. " +
        "3. D destructive/heavy: IFC export, drawing numbering, batch model operations — requires the user to tick 'Allow heavy operations' in HPTekla MCP Bridge. " +
        "Always end with `return <value>;`. Call get_tekla_context first when you do not know the active model or its connection status.";

    [McpServerPrompt(Name = "tekla_query_template", Title = "Query the Tekla model")]
    [Description("Sets up a read-only investigation of the open Tekla model: values returned by a script with transaction=\"none\".")]
    public static ChatMessage[] Query(
        [Description("What the user wants to inspect in the model, e.g. 'how many beams are there and what profiles do they use?'")]
        string question)
    {
        return
        [
            new ChatMessage(ChatRole.System, Persona +
                " This is a read-only task: call execute_tekla_code with transaction=\"none\" and never modify the model."),
            new ChatMessage(ChatRole.User, "How many beams are there and what profiles are assigned?"),
            new ChatMessage(ChatRole.Assistant,
                "I'll query the beams from the model object selector, read their profile strings and count them.\n\n" +
                "execute_tekla_code(transaction: \"none\", label: \"query beam profiles\", code: " +
                "\"var enumerator = model.GetModelObjectSelector().GetAllObjectsWithType(ModelObject.ModelObjectEnum.BEAM); " +
                "var byProfile = new Dictionary<string, int>(); int total = 0; " +
                "while (enumerator.MoveNext()) { ct.ThrowIfCancellationRequested(); " +
                "if (enumerator.Current is Part part) { " +
                "string prof = part.Profile.ProfileString ?? \\\"(none)\\\"; " +
                "byProfile[prof] = byProfile.TryGetValue(prof, out var c) ? c + 1 : 1; total++; } } " +
                "return new { totalBeams = total, profiles = byProfile };\")"),
            new ChatMessage(ChatRole.User, question),
        ];
    }

    [McpServerPrompt(Name = "tekla_modify_template", Title = "Modify the Tekla model")]
    [Description("Sets up a writing change (create beams, columns, plates): context first, test preview with dryRun=true, then apply with transaction=\"auto\".")]
    public static ChatMessage[] Modify(
        [Description("The modification task, e.g. 'create a column at (0,0) from Z=0 to Z=4000 with HEB300'")]
        string task)
    {
        return
        [
            new ChatMessage(ChatRole.System, Persona +
                " This task writes to the model. First call get_tekla_context to confirm that a model is open and connected. " +
                "Run with dryRun=true first to preview what would be inserted; then run with dryRun=false and transaction=\"auto\". Always provide a clear label."),
            new ChatMessage(ChatRole.User, "Create a steel column at (0,0) from Z=0 to Z=3500 with profile HEB300"),
            new ChatMessage(ChatRole.Assistant,
                "I'll create the column with dryRun=true first to verify, then commit with transaction=\"auto\".\n\n" +
                "execute_tekla_code(transaction: \"auto\", dryRun: true, label: \"create HEB300 column\", code: " +
                "\"var col = new Beam { StartPoint = new Point(0, 0, 0), EndPoint = new Point(0, 0, 3500), Name = \\\"COLUMN\\\", Class = \\\"2\\\" }; " +
                "col.Profile.ProfileString = \\\"HEB300\\\"; col.Material.MaterialString = \\\"S235JR\\\"; " +
                "col.Position.Plane = Position.PlaneEnum.MIDDLE; col.Position.Depth = Position.DepthEnum.MIDDLE; " +
                "col.Position.Rotation = Position.RotationEnum.FRONT; " +
                "bool ok = col.Insert(); if (!ok) throw new InvalidOperationException(\\\"Insert failed\\\"); " +
                "return new { id = col.Identifier.ID, name = col.Name, height = 3500 };\")"),
            new ChatMessage(ChatRole.User, task),
        ];
    }

    [McpServerPrompt(Name = "tekla_rebar_template", Title = "Model reinforcement in Tekla")]
    [Description("Sets up concrete reinforcement workflows (RebarGroup, SingleRebar, stirrups) hosted on structural parts.")]
    public static ChatMessage[] Rebar(
        [Description("Reinforcement task, e.g. 'create stirrups D10@150 inside concrete beam 12345'")]
        string task)
    {
        return
        [
            new ChatMessage(ChatRole.System, Persona +
                " This is a concrete reinforcement task. Ensure the host Part exists (query via get_part_properties or select_objects), " +
                "then create RebarGroup or SingleRebar with Father assigned to the host Part."),
            new ChatMessage(ChatRole.User, "Create a stirrup group for concrete beam ID 100"),
            new ChatMessage(ChatRole.Assistant,
                "I'll create a RebarGroup hosted on beam 100.\n\n" +
                "execute_tekla_code(transaction: \"auto\", dryRun: true, label: \"create stirrup group\", code: " +
                "\"var host = model.SelectModelObject(new Identifier(100)) as Part; " +
                "if (host == null) throw new ArgumentException(\\\"Host beam 100 not found\\\"); " +
                "var group = new RebarGroup { Father = host, Name = \\\"STIRRUP\\\", Class = 7, Grade = \\\"B500B\\\", Size = \\\"10\\\", " +
                "StartPoint = new Point(0, 0, 0), EndPoint = new Point(5000, 0, 0) }; " +
                "group.SpacingType = BaseRebarGroup.RebarGroupSpacingTypeEnum.SPACING_TYPE_TARGET_SPACE; " +
                "group.Spacings.Add(150.0); " +
                "var poly = new Polygon(); poly.Points.Add(new Point(0, 50, 50)); poly.Points.Add(new Point(0, 350, 50)); " +
                "poly.Points.Add(new Point(0, 350, 550)); poly.Points.Add(new Point(0, 50, 550)); poly.Points.Add(new Point(0, 50, 50)); " +
                "group.Polygons.Add(poly); " +
                "bool ok = group.Insert(); if (!ok) throw new InvalidOperationException(\\\"Insert rebar failed\\\"); " +
                "return new { id = group.Identifier.ID, spacing = 150 };\")"),
            new ChatMessage(ChatRole.User, task),
        ];
    }
}
