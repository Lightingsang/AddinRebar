namespace HPGrasshopper.McpBridge.Tools;

using System;
using System.Collections.Generic;
using System.Text.Json;
using Grasshopper.Kernel;

public sealed record CanvasBatchError(
    string Operation,
    int Index,
    string Reason);

public sealed record CanvasBatchPreflightResult(
    bool IsValid,
    IReadOnlyList<CanvasBatchError> Errors)
{
    public static CanvasBatchPreflightResult Success() =>
        new(true, Array.Empty<CanvasBatchError>());

    public static CanvasBatchPreflightResult Failure(IReadOnlyList<CanvasBatchError> errors) =>
        new(false, errors);
}

public static class CanvasBatchValidator
{
    public static CanvasBatchPreflightResult Preflight(
        JsonElement args,
        Func<Guid, IGH_DocumentObject?>? findObject = null,
        Func<string, bool>? canCreateObject = null,
        Func<string, (int Inputs, int Outputs)?>? getPortBounds = null)
    {
        var errors = new List<CanvasBatchError>();
        if (args.ValueKind != JsonValueKind.Object && args.ValueKind != JsonValueKind.Undefined)
        {
            errors.Add(new CanvasBatchError("batch", -1, $"Batch arguments must be a JSON object, but found '{args.ValueKind}'."));
            return CanvasBatchPreflightResult.Failure(errors);
        }

        var definedTempIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var tempIdToType = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var removedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        ValidateComponentsToAdd(args, errors, definedTempIds, tempIdToType, canCreateObject);
        ValidateComponentsToRemove(findObject, args, errors, removedIds);
        ValidateWires("wiresToConnect", findObject, args, errors, definedTempIds, tempIdToType, removedIds, getPortBounds);
        ValidateWires("wiresToDisconnect", findObject, args, errors, null, null, removedIds, getPortBounds);

        return errors.Count == 0
            ? CanvasBatchPreflightResult.Success()
            : CanvasBatchPreflightResult.Failure(errors);
    }

    public static CanvasBatchPreflightResult Preflight(
        GH_Document? doc,
        JsonElement args,
        Func<string, bool>? canCreateObject = null,
        Func<string, (int Inputs, int Outputs)?>? getPortBounds = null) =>
        Preflight(args, doc == null ? null : (g => doc.FindObject(g, true)), canCreateObject, getPortBounds);

    private static bool TryGetContainerArray(
        JsonElement args, string propName, List<CanvasBatchError> errors, out JsonElement array)
    {
        array = default;
        if (!args.TryGetProperty(propName, out var prop) || prop.ValueKind == JsonValueKind.Null || prop.ValueKind == JsonValueKind.Undefined)
            return false;

        if (prop.ValueKind != JsonValueKind.Array)
        {
            errors.Add(new CanvasBatchError(propName, -1, $"Container '{propName}' must be a JSON array, got {prop.ValueKind}"));
            return false;
        }

        array = prop;
        return true;
    }

    private static void ValidateComponentsToAdd(
        JsonElement args,
        List<CanvasBatchError> errors,
        HashSet<string> definedTempIds,
        Dictionary<string, string> tempIdToType,
        Func<string, bool>? canCreateObject)
    {
        if (!TryGetContainerArray(args, "componentsToAdd", errors, out var list))
            return;

        var index = 0;
        foreach (var item in list.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                errors.Add(new CanvasBatchError("componentsToAdd", index++, $"Item at index {index - 1} must be a JSON object, got {item.ValueKind}."));
                continue;
            }

            string? typeString = null;
            if (!item.TryGetProperty("type", out var typeProp) || string.IsNullOrWhiteSpace(typeProp.GetString()))
                errors.Add(new CanvasBatchError("componentsToAdd", index, "Missing or empty required property 'type'."));
            else
            {
                typeString = typeProp.GetString()!;
                if (canCreateObject != null && !canCreateObject(typeString))
                    errors.Add(new CanvasBatchError("componentsToAdd", index, $"Unknown component type or plugin not available: '{typeString}'."));
            }

            ValidateCoordinate(item, "x", index, errors);
            ValidateCoordinate(item, "y", index, errors);

            if (item.TryGetProperty("tempId", out var tidProp) && !string.IsNullOrWhiteSpace(tidProp.GetString()))
            {
                var identifier = tidProp.GetString()!;
                if (!definedTempIds.Add(identifier))
                    errors.Add(new CanvasBatchError("componentsToAdd", index, $"Duplicate tempId '{identifier}' defined in componentsToAdd."));
                if (typeString != null)
                    tempIdToType[identifier] = typeString;
            }
            index++;
        }
    }

    private static void ValidateCoordinate(JsonElement item, string propName, int index, List<CanvasBatchError> errors)
    {
        if (item.TryGetProperty(propName, out var prop) &&
            (prop.ValueKind != JsonValueKind.Number || !prop.TryGetSingle(out var val) || float.IsNaN(val) || float.IsInfinity(val)))
        {
            errors.Add(new CanvasBatchError("componentsToAdd", index, $"Coordinate '{propName}' must be a finite number."));
        }
    }

    private static void ValidateComponentsToRemove(
        Func<Guid, IGH_DocumentObject?>? findObject,
        JsonElement args,
        List<CanvasBatchError> errors,
        HashSet<string> removedIds)
    {
        if (!TryGetContainerArray(args, "componentsToRemove", errors, out var list))
            return;

        var index = 0;
        foreach (var item in list.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String)
            {
                errors.Add(new CanvasBatchError("componentsToRemove", index++, $"Item at index {index - 1} must be a GUID string, got {item.ValueKind}."));
                continue;
            }

            var guidString = item.GetString();
            if (!Guid.TryParse(guidString, out var targetGuid))
            {
                errors.Add(new CanvasBatchError("componentsToRemove", index, $"Invalid GUID format: '{guidString}'."));
            }
            else
            {
                removedIds.Add(guidString!);
                removedIds.Add(targetGuid.ToString("D"));
                if (findObject != null && findObject(targetGuid) == null)
                    errors.Add(new CanvasBatchError("componentsToRemove", index, $"Component with GUID '{targetGuid}' does not exist on canvas."));
            }
            index++;
        }
    }

    private static void ValidateWires(
        string operation,
        Func<Guid, IGH_DocumentObject?>? findObject,
        JsonElement args,
        List<CanvasBatchError> errors,
        HashSet<string>? definedTempIds,
        Dictionary<string, string>? tempIdToType,
        HashSet<string> removedIds,
        Func<string, (int Inputs, int Outputs)?>? getPortBounds)
    {
        if (!TryGetContainerArray(args, operation, errors, out var list))
            return;

        var index = 0;
        foreach (var item in list.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                errors.Add(new CanvasBatchError(operation, index++, $"Item at index {index - 1} must be a JSON object, got {item.ValueKind}."));
                continue;
            }

            var sourceId = item.TryGetProperty("source", out var sp) ? sp.GetString() : null;
            var targetId = item.TryGetProperty("target", out var tp) ? tp.GetString() : null;

            ValidateWireEndpoint(sourceId, "source", operation, index, errors, definedTempIds, removedIds, findObject);
            ValidateWireEndpoint(targetId, "target", operation, index, errors, definedTempIds, removedIds, findObject);

            ValidatePortIndex(item, "sourceOutput", operation, index, errors);
            ValidatePortIndex(item, "targetInput", operation, index, errors);

            ValidatePortBounds(sourceId, "sourceOutput", false, operation, item, index, errors, findObject, tempIdToType, getPortBounds);
            ValidatePortBounds(targetId, "targetInput", true, operation, item, index, errors, findObject, tempIdToType, getPortBounds);

            index++;
        }
    }

    private static void ValidateWireEndpoint(
        string? endpointId,
        string role,
        string operation,
        int index,
        List<CanvasBatchError> errors,
        HashSet<string>? definedTempIds,
        HashSet<string> removedIds,
        Func<Guid, IGH_DocumentObject?>? findObject)
    {
        if (string.IsNullOrWhiteSpace(endpointId))
        {
            errors.Add(new CanvasBatchError(operation, index, $"Missing required property '{role}'."));
            return;
        }

        if (removedIds.Contains(endpointId!))
        {
            errors.Add(new CanvasBatchError(operation, index, $"{char.ToUpper(role[0]) + role[1..]} component '{endpointId}' is marked for removal in componentsToRemove."));
            return;
        }

        if (definedTempIds != null && definedTempIds.Contains(endpointId!))
            return;

        if (!Guid.TryParse(endpointId, out var guid) || (findObject != null && findObject(guid) == null))
        {
            var msg = definedTempIds != null
                ? $"{char.ToUpper(role[0]) + role[1..]} '{endpointId}' not found in canvas or componentsToAdd tempIds."
                : $"{char.ToUpper(role[0]) + role[1..]} component '{endpointId}' not found on canvas.";
            errors.Add(new CanvasBatchError(operation, index, msg));
        }
    }

    private static void ValidatePortIndex(
        JsonElement item,
        string propName,
        string operation,
        int index,
        List<CanvasBatchError> errors)
    {
        if (!item.TryGetProperty(propName, out var prop))
            return;

        if (prop.ValueKind != JsonValueKind.Number || !prop.TryGetInt32(out var port))
            errors.Add(new CanvasBatchError(operation, index, $"{propName} port index must be a valid integer."));
        else if (port < 0)
            errors.Add(new CanvasBatchError(operation, index, $"{propName} port index must be non-negative."));
    }

    private static void ValidatePortBounds(
        string? identifier,
        string propName,
        bool isInput,
        string operation,
        JsonElement item,
        int index,
        List<CanvasBatchError> errors,
        Func<Guid, IGH_DocumentObject?>? findObject,
        Dictionary<string, string>? tempIdToType,
        Func<string, (int Inputs, int Outputs)?>? getPortBounds)
    {
        if (string.IsNullOrWhiteSpace(identifier) || !item.TryGetProperty(propName, out var prop))
            return;
        if (prop.ValueKind != JsonValueKind.Number || !prop.TryGetInt32(out var port) || port < 0)
            return;

        if (tempIdToType != null && tempIdToType.TryGetValue(identifier!, out var typeStr) && getPortBounds != null)
        {
            var bounds = getPortBounds(typeStr);
            if (bounds.HasValue)
            {
                int maxPorts = isInput ? bounds.Value.Inputs : bounds.Value.Outputs;
                if (maxPorts > 0 && port >= maxPorts)
                    errors.Add(new CanvasBatchError(operation, index, $"Port index {port} on created component '{identifier}' ({typeStr}) exceeds {(isInput ? "input" : "output")} port count ({maxPorts})."));
            }
            return;
        }

        if (Guid.TryParse(identifier, out var guid) && findObject != null)
        {
            var docObj = findObject(guid);
            if (docObj is IGH_Component comp && comp.Params != null)
            {
                var ports = isInput ? comp.Params.Input : comp.Params.Output;
                if (ports != null && ports.Count > 0 && port >= ports.Count)
                    errors.Add(new CanvasBatchError(operation, index, $"Port index {port} on component '{identifier}' exceeds {(isInput ? "input" : "output")} port count ({ports.Count})."));
            }
            else if (docObj is IGH_Param && port > 0)
            {
                errors.Add(new CanvasBatchError(operation, index, $"Port index {port} on parameter '{identifier}' is invalid; parameters only have port index 0."));
            }
        }
    }
}
