using Autodesk.Revit.UI;
using HPBimToolkit.Core.Contracts;
using HPBimToolkit.Core.Domain.Dashboard;
using RevitView = Autodesk.Revit.DB.View;

namespace HPBimToolkit.Services;

/// <summary>
///     Selects elements for the page — a category of the current scope or explicit ids — and optionally zooms to them;
///     or zooms to what is already selected without touching the selection. Selection is UI state: no transaction.
/// </summary>
public static class ElementNavigator
{
    /// <param name="source">What the request names, from <see cref="ShowElementsRules.SourceOf" /> (already validated).</param>
    public static ShowElementsResult Show(UIApplication app, ShowElementsRequest request, ShowElementsSource source)
    {
        var uiDocument = ActiveDocument.Require(app);
        if (source == ShowElementsSource.CurrentSelection)
        {
            var selected = uiDocument.Selection.GetElementIds();
            return new ShowElementsResult(selected.Count, ZoomInActiveView(uiDocument, selected));
        }

        var ids = ResolveIds(uiDocument, request, source);
        uiDocument.Selection.SetElementIds(ids);
        var zoomed = request.Mode == ShowMode.Zoom ? ZoomInActiveView(uiDocument, ids) : 0;
        return new ShowElementsResult(ids.Count, zoomed);
    }

    /// <summary>
    ///     Zooms only to the elements the active view shows. <c>UIDocument.ShowElements</c> with elements no open view
    ///     shows opens a modal "searching through the closed views could take a long time — Continue?" dialog, which
    ///     blocks Revit's API thread (and every toolkit request) until the user answers — seen live.
    /// </summary>
    private static int ZoomInActiveView(UIDocument uiDocument, ICollection<ElementId> ids)
    {
        if (ids.Count == 0 || uiDocument.ActiveView is not { } view || !CanHoldElements(view))
        {
            return 0;
        }

        var visible = new FilteredElementCollector(uiDocument.Document, view.Id)
            .WherePasses(new ElementIdSetFilter(ids))
            .ToElementIds();
        if (visible.Count > 0)
        {
            uiDocument.ShowElements(visible);
        }

        return visible.Count;
    }

    private static bool CanHoldElements(RevitView view) =>
        !view.IsTemplate && view is not ViewSchedule &&
        view.ViewType is not (ViewType.ProjectBrowser or ViewType.SystemBrowser or ViewType.Internal or ViewType.Undefined);

    private static List<ElementId> ResolveIds(UIDocument uiDocument, ShowElementsRequest request, ShowElementsSource source)
    {
        if (source == ShowElementsSource.Ids)
        {
            var document = uiDocument.Document;
            return request.Ids!.Select(value => new ElementId(value)).Where(id => document.GetElement(id) is not null).ToList();
        }

        return ScopeCollector.Instances(uiDocument, request.Scope)
            .OfCategoryId(new ElementId(request.CategoryId!.Value))
            .ToElementIds()
            .ToList();
    }
}
