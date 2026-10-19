namespace HPDynamo.McpBridge.Execution;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Dynamo.Engine;
using Dynamo.Graph;
using Dynamo.Graph.Nodes;
using Dynamo.Graph.Workspaces;
using Dynamo.Models;
using Dynamo.ViewModels;
using Serilog;

/// <summary>
///     Executes graphs and DesignScript on the active live canvas when Dynamo UI is open.
/// </summary>
public sealed class CanvasGraphRunner
{
    public object? RunActiveGraph(
        DynamoViewModel viewModel,
        bool dryRun,
        CancellationToken ct,
        Action<string?> log)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        var ws = viewModel.Model.CurrentWorkspace as HomeWorkspaceModel
                 ?? throw new InvalidOperationException("Current workspace is not a HomeWorkspace.");

        log($"Running active canvas workspace '{ws.Name}' (Nodes: {ws.Nodes.Count()})");

        bool evalOk = DynamoEvaluationSync.RunAndWait(
            viewModel.Model,
            () => ws.Run(),
            TimeSpan.FromSeconds(60),
            ct,
            log);

        if (!evalOk)
        {
            if (ct.IsCancellationRequested)
                throw new OperationCanceledException("Dynamo active graph evaluation was cancelled.", ct);
            throw new InvalidOperationException("Dynamo active graph evaluation failed or timed out.");
        }

        int errorCount = ws.Nodes.Count(n => n.State == ElementState.Error);
        log($"Active graph run finished. Nodes in error: {errorCount}");

        if (errorCount > 0)
        {
            throw new InvalidOperationException($"Dynamo active graph evaluation completed with {errorCount} node(s) in error state.");
        }

        return new
        {
            Success = true,
            WorkspaceName = ws.Name,
            TotalNodes = ws.Nodes.Count(),
            ErrorNodesCount = errorCount,
            HasUnsavedChanges = ws.HasUnsavedChanges
        };
    }

    public object? RunDynFile(
        DynamoViewModel viewModel,
        string filePath,
        JsonElement? args,
        bool dryRun,
        CancellationToken ct,
        Action<string?> log)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        var ws = LoadWorkspaceFromFile(viewModel, filePath, args, log);

        bool evalOk = DynamoEvaluationSync.RunAndWait(
            viewModel.Model,
            () => ws.Run(),
            TimeSpan.FromSeconds(60),
            ct,
            log);

        if (!evalOk)
        {
            if (ct.IsCancellationRequested)
                throw new OperationCanceledException("Dynamo graph evaluation was cancelled.", ct);
            throw new InvalidOperationException($"Dynamo graph evaluation failed or timed out for '{Path.GetFileName(filePath)}'.");
        }

        int errorCount = ws.Nodes.Count(n => n.State == ElementState.Error);
        if (errorCount > 0)
        {
            throw new InvalidOperationException($"Dynamo graph evaluation completed with {errorCount} node(s) in error state.");
        }

        return new
        {
            Success = true,
            FilePath = filePath,
            NodesCount = ws.Nodes.Count(),
            ConnectorsCount = ws.Connectors.Count(),
            ErrorNodesCount = errorCount
        };
    }

    public object? ExecuteDesignScript(
        DynamoViewModel viewModel,
        string code,
        JsonElement? args,
        bool dryRun,
        CancellationToken ct,
        Action<string?> log)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("DesignScript code cannot be empty", nameof(code));

        var ws = viewModel.Model.CurrentWorkspace as HomeWorkspaceModel
                 ?? throw new InvalidOperationException("Active workspace is not available.");

        var cbn = PlaceCodeBlockNode(viewModel, ws, code, log);

        try
        {
            var outputs = EvaluateDesignScriptNode(viewModel, ws, cbn, ct, log);

            if (dryRun)
            {
                log("dryRun=true: Removing ephemeral CodeBlock from canvas");
                viewModel.Model.ExecuteCommand(new DynamoModel.DeleteModelCommand(cbn.GUID));
            }

            return new { Success = true, NodeId = cbn.GUID, Outputs = outputs };
        }
        catch (Exception ex)
        {
            viewModel.Model.ExecuteCommand(new DynamoModel.DeleteModelCommand(cbn.GUID));
            if (ex is OperationCanceledException)
                throw;
            throw new InvalidOperationException($"DesignScript canvas evaluation failed: {ex.Message}", ex);
        }
    }

    private static HomeWorkspaceModel LoadWorkspaceFromFile(
        DynamoViewModel viewModel,
        string filePath,
        JsonElement? args,
        Action<string?> log)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Dynamo file not found", filePath);

        log($"Opening and executing file '{Path.GetFileName(filePath)}' on canvas");

        viewModel.OpenIfSavedCommand.Execute(new DynamoModel.OpenFileCommand(filePath, false));
        var ws = viewModel.Model.CurrentWorkspace as HomeWorkspaceModel
                 ?? throw new InvalidOperationException("Failed to load workspace onto canvas.");

        if (args.HasValue && args.Value.ValueKind == JsonValueKind.Object)
        {
            ApplyParameterOverrides(ws, args.Value, log);
        }

        return ws;
    }

    private static CodeBlockNodeModel PlaceCodeBlockNode(
        DynamoViewModel viewModel,
        HomeWorkspaceModel ws,
        string code,
        Action<string?> log)
    {
        log("Placing DesignScript CodeBlock node on canvas");

        double x = 100.0;
        double y = 100.0;
        if (ws.Nodes.Any())
        {
#pragma warning disable CS0618
            x = ws.Nodes.Max(n => n.X) + 250.0;
            y = ws.Nodes.First().Y;
#pragma warning restore CS0618
        }

        var cbn = new CodeBlockNodeModel(code, Guid.NewGuid(), x, y, viewModel.Model.LibraryServices, ws.ElementResolver);
        viewModel.Model.ExecuteCommand(new DynamoModel.CreateNodeCommand(cbn, x, y, false, false));
        return cbn;
    }

    private static Dictionary<string, object?> EvaluateDesignScriptNode(
        DynamoViewModel viewModel,
        HomeWorkspaceModel ws,
        CodeBlockNodeModel cbn,
        CancellationToken ct,
        Action<string?> log)
    {
        bool evalOk = DynamoEvaluationSync.RunAndWait(
            viewModel.Model,
            () => ws.Run(),
            TimeSpan.FromSeconds(30),
            ct,
            log);

        if (!evalOk)
        {
            if (ct.IsCancellationRequested)
                throw new OperationCanceledException("DesignScript evaluation was cancelled.", ct);
            throw new InvalidOperationException("DesignScript evaluation failed or timed out.");
        }

        if (cbn.State == ElementState.Error)
        {
            throw new InvalidOperationException("DesignScript CodeBlock node evaluated to error state.");
        }

        var outputs = ExtractOutputs(viewModel.Model.EngineController, cbn);
        log($"DesignScript executed on canvas with {outputs.Count} outputs");
        return outputs;
    }

    private static void ApplyParameterOverrides(WorkspaceModel ws, JsonElement overrides, Action<string?> log)
    {
        foreach (var prop in overrides.EnumerateObject())
        {
            var target = ws.Nodes.FirstOrDefault(n =>
                n.Name.Equals(prop.Name, StringComparison.OrdinalIgnoreCase) ||
                n.GUID.ToString().Equals(prop.Name, StringComparison.OrdinalIgnoreCase));

            if (target != null)
            {
                log($"Overriding node '{target.Name}' with value '{prop.Value}'");
                target.UpdateValue(new UpdateValueParams("Value", prop.Value.ToString()));
            }
        }
    }

    private static Dictionary<string, object?> ExtractOutputs(EngineController? controller, NodeModel node)
    {
        var result = new Dictionary<string, object?>();
        if (controller == null) return result;

        for (int i = 0; i < node.OutPorts.Count; i++)
        {
            try
            {
                var val = node.GetValue(i, controller);
                result[node.OutPorts[i].Name] = val?.Data?.ToString();
            }
            catch (Exception ex)
            {
                // Port evaluation error recorded as null output
                Log.Debug(ex, "Failed to extract port output at index {Index}", i);
                result[node.OutPorts[i].Name] = null;
            }
        }
        return result;
    }
}
