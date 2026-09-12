using Autodesk.Revit.DB.Events;
using HPRebar.Mcp.Contracts.Messages;
using RevitApplication = Autodesk.Revit.ApplicationServices.Application;

namespace HPRebar.McpBridge.Service;

/// <summary>
///     Counts what a script changed by listening to DocumentChanged for the duration of the run. The event
///     fires on commit, so a dry run still reports what it would have changed before the group rolls back.
/// </summary>
public sealed class DocumentChangeCounter : IDisposable
{
    private readonly RevitApplication _application;
    private int _added;
    private int _modified;
    private int _deleted;

    private DocumentChangeCounter(RevitApplication application)
    {
        _application = application;
        _application.DocumentChanged += OnDocumentChanged;
    }

    public static DocumentChangeCounter Begin(RevitApplication application) => new DocumentChangeCounter(application);

    public ChangedCounts Counts => new ChangedCounts(_added, _modified, _deleted);

    private void OnDocumentChanged(object? sender, DocumentChangedEventArgs e)
    {
        _added += e.GetAddedElementIds().Count;
        _modified += e.GetModifiedElementIds().Count;
        _deleted += e.GetDeletedElementIds().Count;
    }

    public void Dispose() => _application.DocumentChanged -= OnDocumentChanged;
}
