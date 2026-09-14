using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.Messages;
using RevitView = Autodesk.Revit.DB.View;

namespace HPRebar.McpBridge.Service;

/// <summary>Builds the session snapshot the AI asks for before scripting. Runs on Revit's API thread.</summary>
public static class RevitContextReader
{
    public static ContextResult Read(UIApplication uiapp, bool includeSelection, bool executionEnabled)
    {
        var uidoc = uiapp.ActiveUIDocument;
        var doc = uidoc?.Document;

        var result = new ContextResult
        {
            RevitVersion = uiapp.Application.VersionNumber,
            Host = PipeNaming.RevitHost,
            HostVersion = uiapp.Application.VersionNumber,
            ExecutionEnabled = executionEnabled,
            OpenDocs = uiapp.Application.Documents.Cast<Document>().Select(d => d.Title).ToArray(),
        };

        if (doc is null) return result;

        result.DocTitle = doc.Title;
        result.DocPath = string.IsNullOrEmpty(doc.PathName) ? null : doc.PathName;
        result.IsFamily = doc.IsFamilyDocument;
        result.IsReadOnly = doc.IsReadOnly;
        result.IsModifiable = doc.IsModifiable;
        result.Units = new UnitsInfo(LengthUnitLabel(doc));

        if (uidoc!.ActiveView is RevitView view)
        {
            result.ActiveView = new ViewInfo(view.Id.Value, view.Name, view.ViewType.ToString());
        }

        if (includeSelection)
        {
            result.Selection = uidoc.Selection.GetElementIds()
                .Select(doc.GetElement)
                .Where(e => e is not null)
                .Select(Describe)
                .ToArray();
        }

        return result;
    }

    public static ElementInfo Describe(Element element)
    {
        string? name;
        try { name = element.Name; }
        catch { name = null; } // some system elements throw on Name

        return new ElementInfo(element.Id.Value, element.Category?.Name, name);
    }

    private static string LengthUnitLabel(Document doc)
    {
        try
        {
            var unit = doc.GetUnits().GetFormatOptions(SpecTypeId.Length).GetUnitTypeId();
            return LabelUtils.GetLabelForUnit(unit);
        }
        catch
        {
            return "unknown";
        }
    }
}
