namespace HPGrasshopper.McpBridge.Tools;

using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Serilog;

/// <summary>
///     Reads active Grasshopper canvas graph structure, components, parameters, wires, and solver status.
/// </summary>
public sealed class CanvasReader
{
    public sealed record CanvasReadResult(
        object Document,
        IReadOnlyList<object> Components,
        IReadOnlyList<object> Wires);

    public CanvasReadResult Read(
        GH_Document? document,
        bool includeValues = false,
        bool selectedOnly = false,
        string? categoryFilter = null)
    {
        if (document is null)
        {
            return new CanvasReadResult(
                new { name = "None", open = false, objectCount = 0 },
                Array.Empty<object>(),
                Array.Empty<object>());
        }

        var docInfo = new
        {
            documentId = document.DocumentID.ToString("D"),
            name = document.DisplayName,
            filePath = document.FilePath ?? string.Empty,
            isModified = document.IsModified,
            objectCount = document.ObjectCount,
            solutionState = document.SolutionState.ToString()
        };

        var objects = document.Objects.AsEnumerable();
        if (selectedOnly) objects = objects.Where(o => o.Attributes?.Selected == true);

        var components = new List<object>();
        var wires = new List<object>();

        foreach (var obj in objects)
        {
            if (obj is IGH_Component comp)
            {
                if (!MatchesCategory(comp, categoryFilter)) continue;
                components.Add(ExtractComponent(comp, includeValues));
                ExtractComponentWires(comp, wires);
            }
            else if (obj is IGH_Param param)
            {
                components.Add(ExtractParamObject(param, includeValues));
                ExtractParamWires(param, wires);
            }
        }

        return new CanvasReadResult(docInfo, components, wires);
    }

    internal static bool MatchesCategory(IGH_Component comp, string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter)) return true;
        return string.Equals(comp.Category, filter, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(comp.SubCategory, filter, StringComparison.OrdinalIgnoreCase);
    }

    internal static object ExtractComponent(IGH_Component comp, bool includeValues)
    {
        var bounds = comp.Attributes?.Bounds ?? System.Drawing.RectangleF.Empty;
        var inputs = comp.Params.Input.Select(p => ExtractParam(p, includeValues)).ToList();
        var outputs = comp.Params.Output.Select(p => ExtractParam(p, includeValues)).ToList();

        var messages = comp.RuntimeMessages(GH_RuntimeMessageLevel.Error)
            .Concat(comp.RuntimeMessages(GH_RuntimeMessageLevel.Warning))
            .ToList();

        return new
        {
            guid = comp.InstanceGuid.ToString("D"),
            componentId = comp.ComponentGuid.ToString("D"),
            name = comp.Name,
            nickName = comp.NickName,
            category = comp.Category,
            subCategory = comp.SubCategory,
            bounds = new { x = bounds.X, y = bounds.Y, width = bounds.Width, height = bounds.Height },
            status = comp.Phase.ToString(),
            messages,
            inputs,
            outputs
        };
    }

    private static object ExtractParamObject(IGH_Param param, bool includeValues)
    {
        var bounds = param.Attributes?.Bounds ?? System.Drawing.RectangleF.Empty;
        var messages = param.RuntimeMessages(GH_RuntimeMessageLevel.Error)
            .Concat(param.RuntimeMessages(GH_RuntimeMessageLevel.Warning))
            .ToList();

        return new
        {
            guid = param.InstanceGuid.ToString("D"),
            componentId = param.ComponentGuid.ToString("D"),
            name = param.Name,
            nickName = param.NickName,
            category = param.Category,
            subCategory = param.SubCategory,
            bounds = new { x = bounds.X, y = bounds.Y, width = bounds.Width, height = bounds.Height },
            status = param.Phase.ToString(),
            messages,
            inputs = Array.Empty<object>(),
            outputs = new[] { ExtractParam(param, includeValues) }
        };
    }

    internal static object ExtractParam(IGH_Param param, bool includeValues)
    {
        string? summary = null;
        List<object?>? preview = null;

        if (includeValues)
        {
            var count = param.VolatileDataCount;
            if (count > 0)
            {
                summary = $"{count} item(s)";
                preview = new List<object?>();
                foreach (var item in param.VolatileData.AllData(true).Take(10))
                {
                    if (item != null) preview.Add(item.ToString());
                }
            }
        }

        return new
        {
            guid = param.InstanceGuid.ToString("D"),
            name = param.Name,
            nickName = param.NickName,
            typeName = param.TypeName,
            sourceGuids = param.Sources.Select(s => s.InstanceGuid.ToString("D")).ToArray(),
            recipientGuids = param.Recipients.Select(r => r.InstanceGuid.ToString("D")).ToArray(),
            optional = param.Optional,
            valueSummary = summary,
            previewValues = preview
        };
    }

    internal static void ExtractComponentWires(IGH_Component comp, List<object> wires)
    {
        foreach (var input in comp.Params.Input)
        {
            foreach (var src in input.Sources)
            {
                wires.Add(new
                {
                    fromComponent = src.Attributes?.GetTopLevel?.DocObject?.InstanceGuid.ToString("D"),
                    fromParam = src.InstanceGuid.ToString("D"),
                    toComponent = comp.InstanceGuid.ToString("D"),
                    toParam = input.InstanceGuid.ToString("D")
                });
            }
        }
    }

    internal static void ExtractParamWires(IGH_Param param, List<object> wires)
    {
        foreach (var src in param.Sources)
        {
            wires.Add(new
            {
                fromComponent = src.Attributes?.GetTopLevel?.DocObject?.InstanceGuid.ToString("D"),
                fromParam = src.InstanceGuid.ToString("D"),
                toComponent = param.InstanceGuid.ToString("D"),
                toParam = param.InstanceGuid.ToString("D")
            });
        }
    }
}
