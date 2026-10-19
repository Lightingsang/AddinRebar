namespace HPDynamo.McpBridge.Support;

using System;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB.Events;
using HPRebar.Mcp.Contracts.Messages;

/// <summary>
///     Hooks Revit DocumentChanged event to capture added, modified, and deleted element counts.
/// </summary>
public sealed class DocumentChangeCounter : IDisposable
{
    private readonly Application _application;
    private int _added;
    private int _modified;
    private int _deleted;

    private DocumentChangeCounter(Application application)
    {
        _application = application;
        _application.DocumentChanged += OnDocumentChanged;
    }

    public static DocumentChangeCounter Begin(Application application) =>
        new(application);

    public ChangedCounts Counts => new(_added, _modified, _deleted);

    private void OnDocumentChanged(object? sender, DocumentChangedEventArgs e)
    {
        _added += e.GetAddedElementIds().Count;
        _modified += e.GetModifiedElementIds().Count;
        _deleted += e.GetDeletedElementIds().Count;
    }

    public void Dispose() => _application.DocumentChanged -= OnDocumentChanged;
}
