namespace HPGrasshopper.McpBridge.Tools;

using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

internal static class PlayerOutputExtractor
{
    public static Dictionary<string, object?> ExtractOutputs(
        IEnumerable<IGH_DocumentObject> objects,
        IReadOnlyList<string>? requestedOutputs)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var initialMatchedParams = new Dictionary<string, (IGH_Param Param, object? Value)>(StringComparer.OrdinalIgnoreCase);
        var filterSet = requestedOutputs?.Count > 0
            ? new HashSet<string>(requestedOutputs, StringComparer.OrdinalIgnoreCase)
            : null;

        foreach (var obj in objects)
        {
            if (obj is IGH_Param param)
            {
                if (MatchesOutput(param, filterSet, out var matchedKey) &&
                    param.Recipients.Count == 0 && param.VolatileDataCount > 0)
                {
                    AddOutputResult(result, initialMatchedParams, param, matchedKey);
                }
            }
            else if (obj is IGH_Component comp)
            {
                foreach (var output in comp.Params.Output)
                {
                    if (MatchesOutput(output, filterSet, out var matchedKey) &&
                        output.VolatileDataCount > 0)
                    {
                        AddOutputResult(result, initialMatchedParams, output, matchedKey);
                    }
                }
            }
        }
        return result;
    }

    private static void AddOutputResult(
        Dictionary<string, object?> result,
        Dictionary<string, (IGH_Param Param, object? Value)> initialMatchedParams,
        IGH_Param param,
        string? matchedKey)
    {
        var guidSuffix = param.InstanceGuid.ToString("D")[..8];
        var uniqueKey = $"{param.NickName}#{guidSuffix}";
        var defaultKey = result.ContainsKey(param.NickName) ? uniqueKey : param.NickName;
        var extractedVal = ExtractParamData(param);

        if (string.IsNullOrEmpty(matchedKey))
        {
            result[defaultKey] = extractedVal;
            return;
        }

        if (!result.ContainsKey(matchedKey))
        {
            result[matchedKey] = extractedVal;
            initialMatchedParams[matchedKey] = (param, extractedVal);
        }
        else
        {
            if (initialMatchedParams.TryGetValue(matchedKey, out var firstMatch))
            {
                var firstUniqueKey = $"{firstMatch.Param.NickName}#{firstMatch.Param.InstanceGuid.ToString("D")[..8]}";
                result[firstUniqueKey] = firstMatch.Value;
                initialMatchedParams.Remove(matchedKey);
            }

            result[uniqueKey] = extractedVal;

            if (result[matchedKey] is List<object?> list)
            {
                list.Add(extractedVal);
            }
            else
            {
                result[matchedKey] = new List<object?> { result[matchedKey], extractedVal };
            }
        }
    }

    public static bool MatchesOutput(
        IGH_Param param,
        HashSet<string>? filterSet,
        out string? matchedKey)
    {
        matchedKey = null;
        if (filterSet == null || filterSet.Count == 0) return true;

        if (!string.IsNullOrEmpty(param.NickName) && filterSet.Contains(param.NickName))
        {
            matchedKey = param.NickName;
            return true;
        }

        if (!string.IsNullOrEmpty(param.Name) && filterSet.Contains(param.Name))
        {
            matchedKey = param.Name;
            return true;
        }

        var guidD = param.InstanceGuid.ToString("D");
        if (filterSet.Contains(guidD))
        {
            matchedKey = guidD;
            return true;
        }

        var guidN = param.InstanceGuid.ToString("N");
        if (filterSet.Contains(guidN))
        {
            matchedKey = guidN;
            return true;
        }

        var guid8 = guidD[..8];
        if (filterSet.Contains(guid8))
        {
            matchedKey = guid8;
            return true;
        }

        var compound = $"{param.NickName}#{guid8}";
        if (filterSet.Contains(compound))
        {
            matchedKey = compound;
            return true;
        }

        return false;
    }

    public static object? ExtractParamData(IGH_Param param)
    {
        if (param.VolatileDataCount == 0) return null;

        if (param.VolatileData.PathCount > 1 || (param.VolatileData.PathCount == 1 && param.VolatileData.Paths[0].ToString() != "{0}"))
        {
            var branchDictionary = new Dictionary<string, object?>();
            foreach (var path in param.VolatileData.Paths)
            {
                var branchItems = new List<object?>();
                var branch = param.VolatileData.get_Branch(path);
                if (branch != null)
                {
                    foreach (var item in branch)
                    {
                        if (item is IGH_Goo typedGoo) branchItems.Add(ConvertGooToTypedValue(typedGoo));
                        else if (item != null) branchItems.Add(item.ToString());
                    }
                }
                branchDictionary[path.ToString()] = branchItems;
            }
            return branchDictionary;
        }

        var scalarOrList = new List<object?>();
        foreach (var item in param.VolatileData.AllData(true))
        {
            if (item is IGH_Goo typedGoo) scalarOrList.Add(ConvertGooToTypedValue(typedGoo));
            else if (item != null) scalarOrList.Add(item.ToString());
        }
        return scalarOrList.Count == 1 ? scalarOrList[0] : scalarOrList;
    }

    public static object? ConvertGooToTypedValue(IGH_Goo gooInstance)
    {
        if (gooInstance is GH_Number num) return num.Value;
        if (gooInstance is GH_Integer integer) return integer.Value;
        if (gooInstance is GH_Boolean boolean) return boolean.Value;
        if (gooInstance is GH_String str) return str.Value;
        if (gooInstance is GH_Point point) return new { x = point.Value.X, y = point.Value.Y, z = point.Value.Z };
        return gooInstance.ToString();
    }
}
