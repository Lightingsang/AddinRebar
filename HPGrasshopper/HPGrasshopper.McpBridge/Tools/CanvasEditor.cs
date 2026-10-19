namespace HPGrasshopper.McpBridge.Tools;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.Json;
using Grasshopper.Kernel;
using HPGrasshopper.McpBridge.Execution;
using Serilog;

/// <summary>
///     Batch editing operations on the active Grasshopper canvas with Level 2 CanvasUndoScope.
/// </summary>
public sealed class CanvasEditor
{
    public object ExecuteBatch(
        GH_Document? doc, JsonElement args, bool dryRun,
        CancellationToken cancellationToken = default, CanvasUndoScope? undoScope = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (doc is null) return new { success = false, error = "No active Grasshopper document is open." };
        var preflight = CanvasBatchValidator.Preflight(doc, args, CanCreateObject, GetPortBounds);
        if (!preflight.IsValid) return CreatePreflightFailure(preflight);

        cancellationToken.ThrowIfCancellationRequested();
        var isExternalScope = undoScope != null;
        using var localScope = isExternalScope ? null : new CanvasUndoScope(doc, "MCP Batch Edit Canvas");
        var scope = undoScope ?? localScope!;

        try
        {
            var m = ApplyMutations(doc, args, scope, cancellationToken);
            if (dryRun)
            {
                var rb = scope.Rollback();
                return CreateBatchSuccess(dryRun: true, rolledBack: rb.Success, m, rb.Errors);
            }

            cancellationToken.ThrowIfCancellationRequested();
            doc.NewSolution(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (!isExternalScope) scope.Commit();
            return CreateBatchSuccess(dryRun: false, rolledBack: false, m);
        }
        catch (OperationCanceledException)
        {
            var rb = scope.Rollback();
            return new { success = false, error = "Operation was cancelled.", rolledBack = rb.Success, rollbackErrors = rb.Errors };
        }
        catch (Exception ex)
        {
            return HandleBatchException(ex, scope);
        }
    }

    private static (int Inputs, int Outputs)? GetPortBounds(string typeStr)
    {
        var obj = CreateObject(typeStr);
        if (obj is IGH_Component comp && comp.Params != null)
            return (comp.Params.Input?.Count ?? 0, comp.Params.Output?.Count ?? 0);
        if (obj is IGH_Param) return (1, 1);
        return null;
    }

    private static object CreatePreflightFailure(CanvasBatchPreflightResult preflight) => new
    {
        success = false,
        error = $"Preflight validation failed with {preflight.Errors.Count} error(s).",
        rolledBack = false,
        added = 0, removed = 0, connected = 0, disconnected = 0,
        createdComponents = Array.Empty<CreatedComponentInfo>(),
        errors = preflight.Errors
    };

    private static (int added, int removed, int connected, int disconnected, List<CreatedComponentInfo> created)
        ApplyMutations(GH_Document doc, JsonElement args, CanvasUndoScope undoScope, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var tempMap = new Dictionary<string, IGH_DocumentObject>(StringComparer.OrdinalIgnoreCase);
        var (added, created) = AddComponents(doc, args, tempMap, undoScope);
        ct.ThrowIfCancellationRequested();
        var removed = RemoveComponents(doc, args, undoScope);
        ct.ThrowIfCancellationRequested();
        var connected = ConnectWires(doc, args, tempMap, undoScope);
        ct.ThrowIfCancellationRequested();
        var disconnected = DisconnectWires(doc, args, tempMap, undoScope);
        return (added, removed, connected, disconnected, created);
    }

    private static object CreateBatchSuccess(
        bool dryRun, bool rolledBack,
        (int added, int removed, int connected, int disconnected, List<CreatedComponentInfo> created) m,
        IReadOnlyList<string>? rollbackErrors = null) => new
    {
        success = !dryRun || rolledBack,
        dryRun,
        rolledBack,
        added = m.added, removed = m.removed, connected = m.connected, disconnected = m.disconnected,
        createdComponents = m.created,
        errors = Array.Empty<CanvasBatchError>(),
        rollbackErrors = rollbackErrors ?? Array.Empty<string>()
    };

    private static object HandleBatchException(Exception ex, CanvasUndoScope undoScope)
    {
        Log.Error(ex, "CanvasEditor: Error applying batch canvas edits, rolling back");
        var rb = undoScope.Rollback();
        return new { success = false, error = $"{ex.GetType().Name}: {ex.Message}", rolledBack = rb.Success, rollbackErrors = rb.Errors };
    }

    private static (int count, List<CreatedComponentInfo> created) AddComponents(
        GH_Document doc, JsonElement args, Dictionary<string, IGH_DocumentObject> tempMap, CanvasUndoScope undoScope)
    {
        var createdList = new List<CreatedComponentInfo>();
        if (!args.TryGetProperty("componentsToAdd", out var list) || list.ValueKind == JsonValueKind.Null || list.ValueKind == JsonValueKind.Undefined)
            return (0, createdList);
        if (list.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException($"Container 'componentsToAdd' must be a JSON array, got {list.ValueKind}");

        var index = 0;
        foreach (var item in list.EnumerateArray())
        {
            var typeStr = item.GetProperty("type").GetString() ?? string.Empty;
            var xCoordinate = item.TryGetProperty("x", out var xProp) ? xProp.GetSingle() : 100f;
            var yCoordinate = item.TryGetProperty("y", out var yProp) ? yProp.GetSingle() : 100f;

            var compObject = CreateObject(typeStr)
                ?? throw new InvalidOperationException($"Failed to create component of type '{typeStr}' at index {index}.");

            compObject.CreateAttributes();
            if (compObject.Attributes != null) compObject.Attributes.Pivot = new PointF(xCoordinate, yCoordinate);
            if (item.TryGetProperty("nickName", out var nick)) compObject.NickName = nick.GetString() ?? compObject.NickName;

            doc.AddObject(compObject, false);
            undoScope.TrackAddedObject(compObject.InstanceGuid);

            var tempIdValue = item.TryGetProperty("tempId", out var tid) ? tid.GetString() : null;
            if (!string.IsNullOrEmpty(tempIdValue)) tempMap[tempIdValue!] = compObject;

            createdList.Add(new CreatedComponentInfo(
                index, tempIdValue, compObject.InstanceGuid.ToString("D"), compObject.Name, compObject.NickName, typeStr));
            index++;
        }
        return (createdList.Count, createdList);
    }

    private static bool CanCreateObject(string typeStr)
    {
        var server = Grasshopper.Instances.ComponentServer;
        if (server?.ObjectProxies == null || server.ObjectProxies.Count == 0) return true;
        if (Guid.TryParse(typeStr, out var compGuid)) return server.ObjectProxies.Any(p => p.Guid == compGuid);
        return server.ObjectProxies.Any(p =>
            string.Equals(p.Desc.Name, typeStr, StringComparison.OrdinalIgnoreCase)
            || string.Equals(p.Desc.NickName, typeStr, StringComparison.OrdinalIgnoreCase));
    }

    private static IGH_DocumentObject? CreateObject(string typeStr)
    {
        if (Guid.TryParse(typeStr, out var compGuid)) return Grasshopper.Instances.ComponentServer.EmitObject(compGuid);
        var proxy = Grasshopper.Instances.ComponentServer.ObjectProxies
            .FirstOrDefault(p => string.Equals(p.Desc.Name, typeStr, StringComparison.OrdinalIgnoreCase)
                              || string.Equals(p.Desc.NickName, typeStr, StringComparison.OrdinalIgnoreCase));
        return proxy?.CreateInstance();
    }

    private static int RemoveComponents(GH_Document doc, JsonElement args, CanvasUndoScope undoScope)
    {
        if (!args.TryGetProperty("componentsToRemove", out var list) || list.ValueKind == JsonValueKind.Null || list.ValueKind == JsonValueKind.Undefined)
            return 0;
        if (list.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException($"Container 'componentsToRemove' must be a JSON array, got {list.ValueKind}");

        var count = 0;
        foreach (var item in list.EnumerateArray())
        {
            if (Guid.TryParse(item.GetString(), out var targetGuid))
            {
                var compObject = doc.FindObject(targetGuid, true);
                if (compObject != null)
                {
                    undoScope.TrackRemovedObject(compObject);
                    doc.RemoveObject(compObject, false);
                    count++;
                }
            }
        }
        return count;
    }

    private static int ConnectWires(
        GH_Document doc, JsonElement args, Dictionary<string, IGH_DocumentObject> tempMap, CanvasUndoScope undoScope)
    {
        if (!args.TryGetProperty("wiresToConnect", out var list) || list.ValueKind == JsonValueKind.Null || list.ValueKind == JsonValueKind.Undefined)
            return 0;
        if (list.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException($"Container 'wiresToConnect' must be a JSON array, got {list.ValueKind}");

        var count = 0;
        foreach (var item in list.EnumerateArray())
        {
            var sourceId = item.GetProperty("source").GetString() ?? string.Empty;
            var targetId = item.GetProperty("target").GetString() ?? string.Empty;
            var sourcePort = ExtractPortIndex(item, "sourceOutput");
            var targetPort = ExtractPortIndex(item, "targetInput");

            var srcParam = ResolveParam(doc, sourceId, tempMap, false, sourcePort)
                ?? throw new InvalidOperationException($"Cannot connect wire: source component '{sourceId}' output port {sourcePort} is invalid or out of range.");
            var tgtParam = ResolveParam(doc, targetId, tempMap, true, targetPort)
                ?? throw new InvalidOperationException($"Cannot connect wire: target component '{targetId}' input port {targetPort} is invalid or out of range.");

            undoScope.TrackConnectedWire(tgtParam, srcParam);
            tgtParam.AddSource(srcParam);
            count++;
        }
        return count;
    }

    private static int DisconnectWires(
        GH_Document doc, JsonElement args, Dictionary<string, IGH_DocumentObject> tempMap, CanvasUndoScope undoScope)
    {
        if (!args.TryGetProperty("wiresToDisconnect", out var list) || list.ValueKind == JsonValueKind.Null || list.ValueKind == JsonValueKind.Undefined)
            return 0;
        if (list.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException($"Container 'wiresToDisconnect' must be a JSON array, got {list.ValueKind}");

        var count = 0;
        foreach (var item in list.EnumerateArray())
        {
            var sourceId = item.GetProperty("source").GetString() ?? string.Empty;
            var targetId = item.GetProperty("target").GetString() ?? string.Empty;
            var sourcePort = ExtractPortIndex(item, "sourceOutput");
            var targetPort = ExtractPortIndex(item, "targetInput");

            var srcParam = ResolveParam(doc, sourceId, tempMap, false, sourcePort)
                ?? throw new InvalidOperationException($"Cannot disconnect wire: source component '{sourceId}' output port {sourcePort} is invalid or out of range.");
            var tgtParam = ResolveParam(doc, targetId, tempMap, true, targetPort)
                ?? throw new InvalidOperationException($"Cannot disconnect wire: target component '{targetId}' input port {targetPort} is invalid or out of range.");

            if (tgtParam.Sources.Contains(srcParam))
            {
                undoScope.TrackDisconnectedWire(tgtParam, srcParam);
                tgtParam.RemoveSource(srcParam);
                count++;
            }
        }
        return count;
    }

    private static IGH_Param? ResolveParam(
        GH_Document doc, string identifier, Dictionary<string, IGH_DocumentObject>? tempMap, bool isInput, int portIndex)
    {
        IGH_DocumentObject? targetObject = null;
        if (tempMap != null && tempMap.TryGetValue(identifier, out var tempObj))
            targetObject = tempObj;
        else if (Guid.TryParse(identifier, out var parsedGuid))
            targetObject = doc.FindObject(parsedGuid, true);

        if (targetObject is IGH_Param param) return portIndex == 0 ? param : null;
        if (targetObject is IGH_Component comp)
        {
            var ports = isInput ? comp.Params.Input : comp.Params.Output;
            if (portIndex >= 0 && portIndex < ports.Count) return ports[portIndex];
        }
        return null;
    }

    private static int ExtractPortIndex(JsonElement item, string propertyName) =>
        item.TryGetProperty(propertyName, out var p) && p.ValueKind == JsonValueKind.Number && p.TryGetInt32(out var idx)
            ? idx
            : 0;
}
