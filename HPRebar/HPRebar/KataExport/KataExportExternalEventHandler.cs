using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using HPRebar.KataExport.Model;
using HPRebar.KataExport.Service;
using HPRebar.KataExport.ViewModel;

namespace HPRebar.KataExport;

/// <summary>
/// Executes requests from the modeless UI on Revit's main API thread via ExternalEvent.
/// </summary>
public sealed class KataExportExternalEventHandler : IExternalEventHandler, IKataExportRunner, IDisposable
{
    private readonly ConcurrentQueue<KataExportRequest> _queue = new();
    private readonly ExternalEvent _externalEvent;

    public KataExportExternalEventHandler()
    {
        _externalEvent = ExternalEvent.Create(this);
    }

    public Task HighlightAsync(IReadOnlyList<ElementId> beamIds)
    {
        var req = new KataExportRequest(KataExportRequestKind.Highlight, beamIds);
        _queue.Enqueue(req);
        _externalEvent.Raise();
        return req.Completion.Task;
    }

    public async Task<KataExportSession?> RepickAsync()
    {
        var req = new KataExportRequest(KataExportRequestKind.Repick);
        _queue.Enqueue(req);
        _externalEvent.Raise();
        var result = await req.Completion.Task;
        return result as KataExportSession;
    }

    public void Execute(UIApplication app)
    {
        var uidoc = app.ActiveUIDocument;
        if (uidoc is null)
        {
            // Never leave the window awaiting a request that can no longer run.
            while (_queue.TryDequeue(out var orphan))
                orphan.Completion.TrySetException(new InvalidOperationException("No Revit document is active."));
            return;
        }

        var doc = uidoc.Document;

        while (_queue.TryDequeue(out var req))
        {
            try
            {
                switch (req.Kind)
                {
                    case KataExportRequestKind.Highlight:
                        if (req.BeamIds is not null && req.BeamIds.Count > 0)
                        {
                            uidoc.Selection.SetElementIds(req.BeamIds.ToList());
                        }
                        req.Completion.TrySetResult(null);
                        break;

                    case KataExportRequestKind.Repick:
                        try
                        {
                            var refs = uidoc.Selection.PickObjects(
                                ObjectType.Element,
                                new KataExportSelectionFilter(),
                                "Chọn dải dầm liên tục trong Revit (nhấn Finish khi xong)");

                            if (refs is null || refs.Count == 0)
                            {
                                req.Completion.TrySetResult(null);
                                break;
                            }

                            var beams = refs.Select(r => doc.GetElement(r)).Where(e => e is not null).ToList();
                            var session = KataSessionReader.Read(doc, uidoc.ActiveView, beams!);
                            req.Completion.TrySetResult(session);
                        }
                        catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                        {
                            req.Completion.TrySetResult(null);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                req.Completion.TrySetException(ex);
            }
        }
    }

    public string GetName() => "Kata Export External Event Handler";

    public void Dispose()
    {
        _externalEvent.Dispose();
    }
}
