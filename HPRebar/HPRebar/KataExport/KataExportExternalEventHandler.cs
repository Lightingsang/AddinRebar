using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using HPRebar.Core.KataRebar.Models;
using HPRebar.KataExport.Model;
using HPRebar.KataExport.Service;
using HPRebar.KataExport.ViewModel;
using HPRebar.KataRebar.Model;
using HPRebar.KataRebar.Service;

namespace HPRebar.KataExport;

/// <summary>
/// Executes requests from the modeless UI on Revit's main API thread via ExternalEvent.
/// </summary>
public sealed class KataExportExternalEventHandler : IExternalEventHandler, IKataExportRunner, IDisposable
{
    private readonly ConcurrentQueue<KataExportRequest> _queue = new();
    private readonly ExternalEvent _externalEvent;
    private readonly Document _document;
    private ElementId _viewId;

    /// <param name="document">The model the window was opened on; the beam ids and bar types belong to it.</param>
    /// <param name="view">The view the run was read in: its visibility decides which supports are found.</param>
    public KataExportExternalEventHandler(Document document, Autodesk.Revit.DB.View view)
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));
        _viewId = view?.Id ?? ElementId.InvalidElementId;
        _externalEvent = ExternalEvent.Create(this);
    }

    public Task HighlightAsync(IReadOnlyList<ElementId> beamIds) =>
        Enqueue(new KataExportRequest(KataExportRequestKind.Highlight, beamIds));

    public async Task<KataExportSession?> RepickAsync() =>
        await Enqueue(new KataExportRequest(KataExportRequestKind.Repick)) as KataExportSession;

    public async Task<KataRebarPreview> PreviewRebarAsync(IReadOnlyList<ElementId> beamIds, KataBeamRebarSpec spec, KataSettings settings, bool preferReversed) =>
        await Enqueue(new KataExportRequest(KataExportRequestKind.PreviewRebar, beamIds) { Spec = spec, Settings = settings, PreferReversed = preferReversed }) as KataRebarPreview
        ?? KataRebarPreview.Failed("Revit không trả kết quả đo dầm.");

    public async Task<KataRebarGenerationResult> GenerateRebarAsync(
        IReadOnlyList<ElementId> beamIds,
        KataBeamRebarSpec spec,
        KataSettings settings,
        bool preferReversed,
        IReadOnlyDictionary<double, ElementId> barTypeIds,
        IReadOnlyCollection<string> removedKeys,
        string? plannedFingerprint) =>
        await Enqueue(new KataExportRequest(KataExportRequestKind.GenerateRebar, beamIds)
        {
            Spec = spec,
            Settings = settings,
            PreferReversed = preferReversed,
            BarTypeIds = barTypeIds,
            RemovedKeys = removedKeys,
            PlannedFingerprint = plannedFingerprint
        }) as KataRebarGenerationResult
        ?? KataRebarGenerationResult.Failed("Revit không trả kết quả tạo thép.");

    /// <summary>Queues a request; one Revit refuses to schedule fails at once instead of leaving the window waiting.</summary>
    private Task<object?> Enqueue(KataExportRequest request)
    {
        _queue.Enqueue(request);
        var raised = _externalEvent.Raise();
        if (raised is not (ExternalEventRequest.Accepted or ExternalEventRequest.Pending))
            request.Completion.TrySetException(new InvalidOperationException($"Revit không nhận yêu cầu ({raised}); thử lại sau khi Revit rảnh."));
        return request.Completion.Task;
    }

    /// <summary>The view the run was read in, or the active one when it is gone.</summary>
    private Autodesk.Revit.DB.View View(UIDocument uidoc) =>
        uidoc.Document.GetElement(_viewId) as Autodesk.Revit.DB.View is { IsTemplate: false } view ? view : uidoc.ActiveView;

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
                if (!doc.Equals(_document))
                    throw new InvalidOperationException($"Mô hình đang mở là '{doc.Title}', không phải '{_document.Title}' của cửa sổ Kata Export; chuyển lại mô hình đó hoặc mở lại cửa sổ.");

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
                            _viewId = uidoc.ActiveView.Id;
                            req.Completion.TrySetResult(session);
                        }
                        catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                        {
                            req.Completion.TrySetResult(null);
                        }
                        break;

                    case KataExportRequestKind.PreviewRebar:
                        req.Completion.TrySetResult(PreviewRebar(uidoc, req));
                        break;

                    case KataExportRequestKind.GenerateRebar:
                        req.Completion.TrySetResult(req.Spec is null
                            ? KataRebarGenerationResult.Failed("Chưa có dữ liệu sheet Dam.")
                            : KataRebarWorkflow.Generate(doc, View(uidoc), req.BeamIds ?? Array.Empty<ElementId>(), req.Spec,
                                req.Settings ?? KataSettings.Default, req.BarTypeIds ?? new Dictionary<double, ElementId>(), req.PreferReversed,
                                req.RemovedKeys, req.PlannedFingerprint));
                        break;
                }
            }
            catch (Exception ex)
            {
                req.Completion.TrySetException(ex);
            }
        }
    }

    private KataRebarPreview PreviewRebar(UIDocument uidoc, KataExportRequest request)
    {
        if (request.Spec is null) return KataRebarPreview.Failed("Chưa có dữ liệu sheet Dam.");
        if (request.BeamIds is null || request.BeamIds.Count == 0) return KataRebarPreview.Failed("Chưa chọn dầm trong Revit.");

        var prepared = KataRebarWorkflow.Prepare(uidoc.Document, View(uidoc), request.BeamIds, request.Spec, request.Settings ?? KataSettings.Default, request.PreferReversed);
        return prepared.Plan is null
            ? KataRebarPreview.Failed($"Không đo được dầm: {prepared.Match.Message}")
            : new KataRebarPreview { IsSuccess = true, Plan = prepared.Plan, MeasureWarnings = prepared.Match.Warnings };
    }

    public string GetName() => "Kata Export External Event Handler";

    public void Dispose()
    {
        _externalEvent.Dispose();
    }
}
