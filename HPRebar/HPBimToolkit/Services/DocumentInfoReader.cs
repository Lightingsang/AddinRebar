using Autodesk.Revit.UI;
using HPBimToolkit.Core.Contracts;
using RevitView = Autodesk.Revit.DB.View;

namespace HPBimToolkit.Services;

/// <summary>Reads what the dashboard header shows about the active document and view. Cheap: no collector runs here.</summary>
public static class DocumentInfoReader
{
    public static DocumentSnapshot Read(UIApplication app)
    {
        if (app.ActiveUIDocument is not { } uiDocument)
        {
            return DocumentSnapshot.None;
        }

        var document = uiDocument.Document;
        return new DocumentSnapshot(
            HasDocument: true,
            Title: document.Title,
            Path: string.IsNullOrEmpty(document.PathName) ? null : document.PathName,
            IsWorkshared: document.IsWorkshared,
            IsModified: document.IsModified,
            IsReadOnly: document.IsReadOnly,
            IsFamily: document.IsFamilyDocument,
            ActiveView: Summarize(uiDocument.ActiveView));
    }

    private static ActiveViewSummary? Summarize(RevitView? view) =>
        view is null ? null : new ActiveViewSummary(view.Id.Value, view.Name, view.ViewType.ToString(), ScaleOf(view));

    /// <summary>Views that are not drawn to a scale (schedules, sheets, browsers) report 0 instead of a meaningless value.</summary>
    private static int ScaleOf(RevitView view) => view.ViewType switch
    {
        ViewType.Schedule or ViewType.ColumnSchedule or ViewType.PanelSchedule or ViewType.DrawingSheet or
            ViewType.ProjectBrowser or ViewType.SystemBrowser or ViewType.Report or ViewType.Internal or
            ViewType.Undefined => 0,
        _ => view.Scale,
    };
}
