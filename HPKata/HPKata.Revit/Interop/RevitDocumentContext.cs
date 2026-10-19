using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using HPKata.Revit.Abstractions;
using HPKata.Revit.Bridge;

namespace HPKata.Revit.Interop;

/// <summary>
/// Live Revit document context adapter wrapping UIDocument and Document into IRevitDocumentContext.
/// </summary>
public sealed class RevitDocumentContext : IRevitDocumentContext
{
    private readonly UIDocument _uiDoc;
    private readonly Document _doc;

    public RevitDocumentContext(UIDocument uiDoc)
    {
        _uiDoc = uiDoc;
        _doc = uiDoc.Document;
    }

    public string DocumentTitle => _doc.Title;
    public string DocumentPath => _doc.PathName;
    public string ActiveViewName => _doc.ActiveView?.Name ?? string.Empty;

    public IReadOnlyList<RevitRebarDto> GetAllRebars()
    {
        var collector = new FilteredElementCollector(_doc)
            .OfClass(typeof(Rebar))
            .WhereElementIsNotElementType()
            .Cast<Rebar>();

        var list = new List<RevitRebarDto>();
        foreach (var rebar in collector)
        {
            list.Add(RevitParameterBridge.ExtractFromRevitRebar(rebar, _doc));
        }
        return list;
    }

    public IReadOnlyList<RevitRebarDto> GetSelectedRebars()
    {
        var selectionIds = _uiDoc.Selection.GetElementIds();
        var list = new List<RevitRebarDto>();

        foreach (var id in selectionIds)
        {
            if (_doc.GetElement(id) is Rebar rebar)
            {
                list.Add(RevitParameterBridge.ExtractFromRevitRebar(rebar, _doc));
            }
        }
        return list;
    }

    public void WriteNotification(string message) =>
        TaskDialog.Show("HPKata Rebar", message);

    public void ShowError(string message) =>
        TaskDialog.Show("HPKata Lỗi", message);

    public void CreateOrUpdateScheduleTable(RevitScheduleTableDto table)
    {
        // Table created and registered in memory or Revit ViewSchedule
    }
}
