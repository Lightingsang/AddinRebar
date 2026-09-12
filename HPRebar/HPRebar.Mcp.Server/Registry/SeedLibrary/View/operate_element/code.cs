string action = args.Require("action");
var ids = args.Longs("elementIds").Select(v => new ElementId(v)).Where(id => doc.GetElement(id) != null).ToList();
var view = doc.ActiveView;
if (ids.Count == 0 && !string.Equals(action, "ResetIsolate", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("elementIds is empty (or none of the ids exist).");

OverrideGraphicSettings ColorOverride(int r, int g, int b)
{
    var color = new Color((byte)Math.Clamp(r, 0, 255), (byte)Math.Clamp(g, 0, 255), (byte)Math.Clamp(b, 0, 255));
    var ogs = new OverrideGraphicSettings();
    ogs.SetProjectionLineColor(color);
    ogs.SetCutLineColor(color);
    ogs.SetSurfaceForegroundPatternColor(color);
    ogs.SetCutForegroundPatternColor(color);
    var solid = new FilteredElementCollector(doc).OfClass(typeof(FillPatternElement)).Cast<FillPatternElement>().FirstOrDefault(p => p.GetFillPattern().IsSolidFill);
    if (solid != null)
    {
        ogs.SetSurfaceForegroundPatternId(solid.Id); ogs.SetSurfaceForegroundPatternVisible(true);
        ogs.SetCutForegroundPatternId(solid.Id); ogs.SetCutForegroundPatternVisible(true);
    }
    return ogs;
}

string outcome;
switch (action.ToLowerInvariant())
{
    case "select":
        uidoc.Selection.SetElementIds(ids);
        outcome = $"selected {ids.Count}";
        break;
    case "setcolor":
    case "highlight":
    {
        var rgb = args.Longs("colorValue");
        int r = 255, g = 0, b = 0;
        if (action.Equals("SetColor", StringComparison.OrdinalIgnoreCase) && rgb.Count >= 3) { r = (int)rgb[0]; g = (int)rgb[1]; b = (int)rgb[2]; }
        var ogs = ColorOverride(r, g, b);
        foreach (var id in ids) view.SetElementOverrides(id, ogs);
        outcome = $"coloured {ids.Count} with rgb({r},{g},{b}) in view '{view.Name}'";
        break;
    }
    case "settransparency":
    {
        int t = Math.Clamp(args.Int("transparencyValue", 50), 0, 100);
        foreach (var id in ids)
        {
            var ogs = view.GetElementOverrides(id);
            ogs.SetSurfaceTransparency(t);
            view.SetElementOverrides(id, ogs);
        }
        outcome = $"transparency {t}% on {ids.Count} in view '{view.Name}'";
        break;
    }
    case "hide":
    {
        var hideable = ids.Where(id => doc.GetElement(id).CanBeHidden(view)).ToList();
        if (hideable.Count > 0) view.HideElements(hideable);
        outcome = $"hidden {hideable.Count}/{ids.Count} in view '{view.Name}'";
        break;
    }
    case "temphide":
        view.HideElementsTemporary(ids);
        outcome = $"temporarily hidden {ids.Count}";
        break;
    case "isolate":
        view.IsolateElementsTemporary(ids);
        outcome = $"isolated {ids.Count}";
        break;
    case "unhide":
        view.UnhideElements(ids);
        outcome = $"unhidden {ids.Count}";
        break;
    case "resetisolate":
        view.DisableTemporaryViewMode(TemporaryViewMode.TemporaryHideIsolate);
        outcome = "temporary hide/isolate cleared";
        break;
    case "delete":
    {
        var deleted = doc.Delete(ids);
        outcome = $"deleted {ids.Count} requested, {deleted.Count} elements removed including dependents";
        break;
    }
    case "selectionbox":
        throw new NotSupportedException("SelectionBox needs the user to drag a rectangle in Revit; it cannot run through MCP. Use ai_element_filter with a bounding box instead.");
    default:
        throw new ArgumentException($"Unknown action '{action}'.");
}

return new { action, view = view.Name, elementCount = ids.Count, outcome };
