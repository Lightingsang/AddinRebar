namespace HPDynamo.McpBridge.Execution;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Runtime.CompilerServices;
using Autodesk.Revit.UI;
using Dynamo.Applications;
using Dynamo.Applications.Models;
using Dynamo.Engine;
using Dynamo.Graph.Nodes;
using Dynamo.Graph.Workspaces;
using Dynamo.Models;
using Serilog;

/// <summary>
///     Executes graphs and DesignScript headlessly when Dynamo UI is closed.
/// </summary>
public sealed class HeadlessGraphRunner
{
    public object? RunDynFile(
        UIApplication uiapp,
        string dynPath,
        JsonElement? args,
        bool dryRun,
        CancellationToken ct,
        Action<string?> log)
    {
        if (!File.Exists(dynPath))
            throw new FileNotFoundException("Dynamo file not found on disk", dynPath);

        log($"Headless runner: loading graph '{Path.GetFileName(dynPath)}'");

        var journalData = new Dictionary<string, string>
        {
            ["dynPath"] = dynPath,
            ["dynShowUI"] = "false",
            ["dynPathExecute"] = "true",
            ["dynForceManualRun"] = "false"
        };

        if (args.HasValue && args.Value.ValueKind == JsonValueKind.Object)
        {
            journalData["dynModelNodesInfo"] = FormatParameterOverrides(args.Value);
        }

        var app = new DynamoRevitApp();
        var result = app.ExecuteDynamoCommand(journalData, uiapp);

        var model = DynamoRevit.RevitDynamoModel;
        var ws = model?.CurrentWorkspace;

        int nodeCount = ws != null ? ws.Nodes.Count() : 0;
        int errorCount = ws != null ? ws.Nodes.Count(n => n.State == ElementState.Error) : 0;

        log($"Headless evaluation finished with result: {result} (Nodes: {nodeCount}, Errors: {errorCount})");

        if (errorCount > 0)
        {
            var errList = ws != null
                ? string.Join("; ", ws.Nodes.Where(n => n.State == ElementState.Error).Select(n => $"{n.Name} (State: {n.State})"))
                : "Unknown";
            throw new InvalidOperationException($"Headless graph execution failed with {errorCount} error node(s): {errList}");
        }

        return new
        {
            Status = result.ToString(),
            FilePath = dynPath,
            NodeCount = nodeCount,
            ErrorsCount = errorCount,
            Errors = ws?.Nodes.Where(n => n.State == ElementState.Error).Select(n => $"{n.Name} (State: {n.State})").ToList()
        };
    }

    public object? ExecuteDesignScript(
        object? uiapp,
        string code,
        JsonElement? args,
        bool dryRun,
        CancellationToken ct,
        Action<string?> log)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("DesignScript code cannot be empty", nameof(code));

        if (uiapp == null)
            throw new ArgumentNullException(nameof(uiapp), "UIApplication is required for DesignScript execution.");

        return ExecuteDesignScriptCore(uiapp, code, args, dryRun, ct, log);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private object? ExecuteDesignScriptCore(
        object? uiapp,
        string code,
        JsonElement? args,
        bool dryRun,
        CancellationToken ct,
        Action<string?> log)
    {
        var app = uiapp as UIApplication
                  ?? throw new ArgumentNullException(nameof(uiapp), "UIApplication is required for DesignScript execution.");
        var model = EnsureHeadlessModel(app);
        var ws = model.CurrentWorkspace as HomeWorkspaceModel
                 ?? throw new InvalidOperationException("No active headless HomeWorkspace available.");

        log("Headless runner: Creating ephemeral CodeBlock node for DesignScript evaluation");

        var cbn = new CodeBlockNodeModel(code, Guid.NewGuid(), 0, 0, model.LibraryServices, ws.ElementResolver);
        model.ExecuteCommand(new DynamoModel.CreateNodeCommand(cbn, 0.0, 0.0, false, false));

        try
        {
            ws.Run();
            var outputs = ExtractNodeOutputs(model.EngineController, cbn);
            log($"Headless DesignScript evaluated successfully with {outputs.Count} outputs");
            return new { Success = true, Outputs = outputs };
        }
        finally
        {
            model.ExecuteCommand(new DynamoModel.DeleteModelCommand(cbn.GUID));
        }
    }

    private static RevitDynamoModel EnsureHeadlessModel(UIApplication uiapp)
    {
        if (DynamoRevit.RevitDynamoModel != null)
            return DynamoRevit.RevitDynamoModel;

        var journalData = new Dictionary<string, string>
        {
            ["dynShowUI"] = "false",
            ["dynForceManualRun"] = "true"
        };

        var app = new DynamoRevitApp();
        app.ExecuteDynamoCommand(journalData, uiapp);

        return DynamoRevit.RevitDynamoModel
               ?? throw new InvalidOperationException("Failed to initialize headless RevitDynamoModel.");
    }

    internal static string FormatParameterOverrides(JsonElement overrides)
    {
        var items = new List<Dictionary<string, string>>();
        foreach (var prop in overrides.EnumerateObject())
        {
            items.Add(new Dictionary<string, string>
            {
                ["Name"] = prop.Name,
                ["Value"] = prop.Value.ToString()
            });
        }
        return JsonSerializer.Serialize(items);
    }

    internal static Dictionary<string, object?> ExtractNodeOutputs(EngineController? controller, NodeModel node)
    {
        var result = new Dictionary<string, object?>();
        if (controller == null) return result;

        for (int i = 0; i < node.OutPorts.Count; i++)
        {
            try
            {
                var val = node.GetValue(i, controller);
                result[$"out_{i}"] = val?.Data?.ToString();
            }
            catch (Exception ex)
            {
                // Port evaluation error recorded as null output
                Log.Debug(ex, "Failed to extract port output at index {Index}", i);
                result[$"out_{i}"] = null;
            }
        }
        return result;
    }
}
