namespace HPGrasshopper.McpBridge.Tools;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Serilog;

internal readonly record struct StagedItem(GH_Path Path, int Index, IGH_Goo Goo);

internal static class PlayerDataTreeHelper
{
    public static bool SetParamValue(IGH_Param param, JsonElement val, out string? failureReason)
    {
        try
        {
            if (!TryStageParamValue(param, val, out var stagedItems, out failureReason))
                return false;
            return ApplyStagedItems(param, stagedItems!, out failureReason);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "PlayerDataTreeHelper: Failed setting input parameter {Param}", param.NickName);
            failureReason = ex.Message;
            return false;
        }
    }

    public static bool SetParamValue(IGH_Param param, JsonElement val) =>
        SetParamValue(param, val, out _);

    public static bool TryStageParamValue(
        IGH_Param param,
        JsonElement val,
        out List<StagedItem>? stagedItems,
        out string? failureReason)
    {
        stagedItems = null;
        failureReason = null;

        if (val.ValueKind == JsonValueKind.Number && val.TryGetDouble(out var numValue))
            return SingleItem(new GH_Number(numValue), out stagedItems);
        if (val.ValueKind is JsonValueKind.True or JsonValueKind.False)
            return SingleItem(new GH_Boolean(val.GetBoolean()), out stagedItems);
        if (val.ValueKind == JsonValueKind.String)
            return SingleItem(new GH_String(val.GetString()), out stagedItems);
        if (val.ValueKind == JsonValueKind.Array)
            return TryStageArray(param, val, out stagedItems, out failureReason);
        if (val.ValueKind == JsonValueKind.Object)
        {
            if (TryParsePoint(val, out var pt))
                return SingleItem(new GH_Point(pt), out stagedItems);
            return TryStageTree(param, val, out stagedItems, out failureReason);
        }

        failureReason = $"Unsupported JSON value kind '{val.ValueKind}'.";
        return false;
    }

    private static bool SingleItem(IGH_Goo goo, out List<StagedItem>? stagedItems)
    {
        stagedItems = new List<StagedItem> { new(new GH_Path(0), 0, goo) };
        return true;
    }

    private static bool TryStageArray(IGH_Param param, JsonElement arrayElement, out List<StagedItem>? stagedItems, out string? failureReason)
    {
        stagedItems = null;
        failureReason = null;
        if (IsPointTarget(param) && TryParsePoint(arrayElement, out var pt))
            return SingleItem(new GH_Point(pt), out stagedItems);

        var list = new List<StagedItem>();
        int itemIndex = 0;
        foreach (var element in arrayElement.EnumerateArray())
        {
            if (element.ValueKind == JsonValueKind.Null)
            {
                failureReason = $"Array item at index {itemIndex} is null; null items are not supported.";
                return false;
            }
            var goo = ConvertJsonToGoo(element, param);
            if (goo == null)
            {
                failureReason = $"Array item at index {itemIndex} could not be converted ({element.ValueKind}).";
                return false;
            }
            list.Add(new StagedItem(new GH_Path(0), itemIndex++, goo));
        }

        if (list.Count == 0)
        {
            failureReason = "Array cannot be empty.";
            return false;
        }

        stagedItems = list;
        return true;
    }

    private static bool TryStageTree(IGH_Param param, JsonElement objectElement, out List<StagedItem>? stagedItems, out string? failureReason)
    {
        stagedItems = null;
        failureReason = null;
        if (!objectElement.EnumerateObject().Any(p => p.Name.StartsWith("{") && p.Name.EndsWith("}")))
        {
            failureReason = "Object is neither a 3D Point nor a DataTree.";
            return false;
        }

        var list = new List<StagedItem>();
        foreach (var prop in objectElement.EnumerateObject())
        {
            if (!TryParsePath(prop.Name, out var path))
            {
                failureReason = $"Malformed branch path '{prop.Name}'; paths must match '{{index;...}}' format.";
                return false;
            }

            var items = prop.Value.ValueKind == JsonValueKind.Array
                ? prop.Value.EnumerateArray()
                : Enumerable.Repeat(prop.Value, 1);
            int bIdx = 0;
            foreach (var item in items)
            {
                if (item.ValueKind == JsonValueKind.Null)
                {
                    failureReason = $"Branch '{prop.Name}' item at index {bIdx} is null; null items are not supported.";
                    return false;
                }
                var goo = ConvertJsonToGoo(item, param);
                if (goo == null)
                {
                    failureReason = $"Branch '{prop.Name}' item at index {bIdx} could not be converted ({item.ValueKind}).";
                    return false;
                }
                list.Add(new StagedItem(path, bIdx++, goo));
            }
        }

        if (list.Count == 0)
        {
            failureReason = "DataTree branches contain no valid items.";
            return false;
        }

        stagedItems = list;
        return true;
    }

    public static bool ApplyStagedItems(IGH_Param param, IReadOnlyList<StagedItem> stagedItems, out string? failureReason)
    {
        failureReason = null;
        param.ClearData();
        foreach (var item in stagedItems)
        {
            bool accepted = param.AddVolatileData(item.Path, item.Index, item.Goo);
            if (!accepted)
            {
                failureReason = $"Parameter '{param.NickName}' refused data of type '{item.Goo?.TypeName ?? "Unknown"}' at path {item.Path}[{item.Index}].";
                return false;
            }
        }
        return true;
    }

    public static bool TryParsePoint(JsonElement element, out Point3d point)
    {
        point = default;
        if (element.ValueKind == JsonValueKind.Object)
        {
            var hasX = element.TryGetProperty("x", out var px) || element.TryGetProperty("X", out px);
            var hasY = element.TryGetProperty("y", out var py) || element.TryGetProperty("Y", out py);
            if (hasX && hasY && px.ValueKind == JsonValueKind.Number && py.ValueKind == JsonValueKind.Number)
            {
                var z = (element.TryGetProperty("z", out var pz) || element.TryGetProperty("Z", out pz)) && pz.ValueKind == JsonValueKind.Number ? pz.GetDouble() : 0.0;
                point = new Point3d(px.GetDouble(), py.GetDouble(), z);
                return true;
            }
        }
        else if (element.ValueKind == JsonValueKind.Array && element.GetArrayLength() == 3 &&
                 element[0].ValueKind == JsonValueKind.Number && element[1].ValueKind == JsonValueKind.Number && element[2].ValueKind == JsonValueKind.Number)
        {
            point = new Point3d(element[0].GetDouble(), element[1].GetDouble(), element[2].GetDouble());
            return true;
        }
        return false;
    }

    public static bool IsPointTarget(IGH_Param param)
    {
        var typeName = param.TypeName ?? string.Empty;
        if (typeName.Contains("Point", StringComparison.OrdinalIgnoreCase) ||
            typeName.Contains("Vector", StringComparison.OrdinalIgnoreCase) ||
            typeName.Contains("Plane", StringComparison.OrdinalIgnoreCase))
            return true;

        var nick = param.NickName ?? string.Empty;
        var name = param.Name ?? string.Empty;
        return nick.Equals("Pt", StringComparison.OrdinalIgnoreCase) ||
               nick.Equals("Point", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("Point", StringComparison.OrdinalIgnoreCase);
    }

    private static IGH_Goo? ConvertJsonToGoo(JsonElement element, IGH_Param? targetParam = null)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetDouble(out var d)) return new GH_Number(d);
        if (element.ValueKind is JsonValueKind.True or JsonValueKind.False) return new GH_Boolean(element.GetBoolean());
        if (element.ValueKind == JsonValueKind.String) return new GH_String(element.GetString());
        if (element.ValueKind == JsonValueKind.Object && TryParsePoint(element, out var ptFromObj)) return new GH_Point(ptFromObj);
        if (element.ValueKind == JsonValueKind.Array && (targetParam == null || IsPointTarget(targetParam)) && TryParsePoint(element, out var ptFromArray))
            return new GH_Point(ptFromArray);
        return null;
    }

    public static bool TryParsePath(string pathNotation, out GH_Path path)
    {
        path = new GH_Path(0);
        if (string.IsNullOrWhiteSpace(pathNotation)) return false;

        var trimmed = pathNotation.Trim();
        if (!trimmed.StartsWith("{") || !trimmed.EndsWith("}")) return false;

        var inner = trimmed[1..^1].Trim();
        if (inner.Length == 0) return true;

        var segments = inner.Split(';', StringSplitOptions.None);
        var indices = new int[segments.Length];
        for (int i = 0; i < segments.Length; i++)
        {
            var seg = segments[i].Trim();
            if (!int.TryParse(seg, out var idx) || idx < 0)
                return false;
            indices[i] = idx;
        }

        path = new GH_Path(indices);
        return true;
    }

    public static GH_Path ParsePath(string pathNotation)
    {
        var cleaned = pathNotation.Trim('{', '}').Trim();
        if (string.IsNullOrWhiteSpace(cleaned)) return new GH_Path(0);
        var indices = cleaned.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.TryParse(s.Trim(), out var parsed) ? parsed : 0).ToArray();
        return new GH_Path(indices);
    }

    public static IGH_Param? FindParam(
        IEnumerable<IGH_DocumentObject> objects,
        string nameOrGuid,
        Func<Guid, IGH_DocumentObject?>? findById = null)
    {
        if (Guid.TryParse(nameOrGuid, out var guid))
        {
            if (findById != null && findById(guid) is IGH_Param foundByIdParam)
                return foundByIdParam;

            foreach (var docObj in objects)
            {
                if (docObj.InstanceGuid == guid && docObj is IGH_Param directParam)
                    return directParam;
                if (docObj is IGH_Component comp)
                {
                    var match = comp.Params.Input.FirstOrDefault(i => i.InstanceGuid == guid);
                    if (match != null) return match;
                }
            }
        }

        foreach (var obj in objects)
        {
            if (obj is IGH_Param param && string.Equals(param.NickName, nameOrGuid, StringComparison.OrdinalIgnoreCase))
                return param;
            if (obj is IGH_Component comp)
            {
                var match = comp.Params.Input.FirstOrDefault(i => string.Equals(i.NickName, nameOrGuid, StringComparison.OrdinalIgnoreCase));
                if (match != null) return match;
            }
        }
        return null;
    }

    public static object? ExtractParamData(IGH_Param param) => PlayerOutputExtractor.ExtractParamData(param);
    public static object? ConvertGooToTypedValue(IGH_Goo gooInstance) => PlayerOutputExtractor.ConvertGooToTypedValue(gooInstance);
}
