using Autodesk.Revit.UI;
using HPBimToolkit.Core.Contracts;
using RevitView = Autodesk.Revit.DB.View;

namespace HPBimToolkit.Services;

/// <summary>
///     The one definition of "elements in scope" shared by statistics and by "show this category", so a count the
///     page displays and the elements it selects always agree.
/// </summary>
public static class ScopeCollector
{
    public static FilteredElementCollector Instances(UIDocument uiDocument, StatisticsScope scope)
    {
        var document = uiDocument.Document;
        var collector = scope == StatisticsScope.ActiveView
            ? new FilteredElementCollector(document, RequireGraphicalView(uiDocument.ActiveView).Id)
            : new FilteredElementCollector(document);
        return collector.WhereElementIsNotElementType();
    }

    /// <summary>A view-scoped collector throws for schedules and browsers; refuse those as the caller's choice.</summary>
    private static RevitView RequireGraphicalView(RevitView? view)
    {
        if (view is null || view.IsTemplate || view is ViewSchedule ||
            view.ViewType is ViewType.ProjectBrowser or ViewType.SystemBrowser or ViewType.Internal or ViewType.Undefined)
        {
            throw new BridgeException(BridgeErrorCodes.BadRequest,
                "The active view has no model elements to count. Open a plan, section, 3D view or sheet, or use the whole model.");
        }

        return view;
    }
}
