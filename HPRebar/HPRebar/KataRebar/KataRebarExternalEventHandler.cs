using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;
using HPRebar.KataRebar.Model;
using HPRebar.KataRebar.Service;
using HPRebar.KataRebar.ViewModel;
using Serilog;

namespace HPRebar.KataRebar;

/// <summary>
/// Runs the window's requests on Revit's API thread. Generation always measures the beams again and
/// re-plans the sheet on that measurement, so the model is never changed from a stale preview.
/// </summary>
public sealed class KataRebarExternalEventHandler : IExternalEventHandler, IKataRebarRunner, IDisposable
{
    private readonly ConcurrentQueue<KataRebarRequest> _queue = new();
    private readonly ExternalEvent _externalEvent;
    private readonly Document _document;

    /// <param name="document">The model the window was opened on; element ids and bar types belong to it.</param>
    public KataRebarExternalEventHandler(Document document)
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));
        _externalEvent = ExternalEvent.Create(this);
    }

    public async Task<KataBeamMatchResult?> PickBeamsAsync() =>
        await Enqueue(new KataRebarRequest(KataRebarRequestKind.Pick)) as KataBeamMatchResult;

    public async Task<KataBeamMatchResult> MeasureBeamsAsync(IReadOnlyList<ElementId> beamIds) =>
        await Enqueue(new KataRebarRequest(KataRebarRequestKind.Measure) { BeamIds = beamIds }) as KataBeamMatchResult
        ?? KataBeamMatchResult.Failed("Revit không trả kết quả đo dầm.");

    public async Task<KataRebarGenerationResult> GenerateAsync(
        IReadOnlyList<ElementId> beamIds,
        KataBeamRebarSpec spec,
        IReadOnlyDictionary<double, ElementId> barTypeIds) =>
        await Enqueue(new KataRebarRequest(KataRebarRequestKind.Generate) { BeamIds = beamIds, Spec = spec, BarTypeIds = barTypeIds }) as KataRebarGenerationResult
        ?? KataRebarGenerationResult.Failed("Revit không trả kết quả tạo thép.");

    public void Execute(UIApplication app)
    {
        var uidoc = app.ActiveUIDocument;
        while (_queue.TryDequeue(out var request))
        {
            try
            {
                if (uidoc is null)
                    throw new InvalidOperationException("Không có mô hình Revit nào đang mở.");
                if (!uidoc.Document.Equals(_document))
                    throw new InvalidOperationException($"Mô hình đang mở là '{uidoc.Document.Title}', không phải '{_document.Title}' của cửa sổ Kata Rebar; chuyển lại mô hình đó hoặc mở lại cửa sổ.");

                request.Completion.TrySetResult(request.Kind switch
                {
                    KataRebarRequestKind.Pick => Pick(uidoc),
                    KataRebarRequestKind.Measure => Measure(uidoc, request.BeamIds),
                    _ => Generate(uidoc, request)
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Kata Rebar: request {Kind} failed on the Revit API thread ({Reason})", request.Kind, ex.Message);
                request.Completion.TrySetException(ex);
            }
        }
    }

    public string GetName() => "Kata Rebar";

    public void Dispose() => _externalEvent.Dispose();

    private Task<object?> Enqueue(KataRebarRequest request)
    {
        _queue.Enqueue(request);
        _externalEvent.Raise();
        return request.Completion.Task;
    }

    private static KataBeamMatchResult? Pick(UIDocument uidoc)
    {
        try
        {
            var picked = uidoc.Selection.PickObjects(ObjectType.Element, new KataRebarSelectionFilter(), "Chọn dầm cần vẽ thép rồi bấm Finish");
            return Measure(uidoc, picked.Select(r => r.ElementId).ToList());
        }
        catch (Autodesk.Revit.Exceptions.OperationCanceledException)
        {
            return null;
        }
    }

    private static KataBeamMatchResult Measure(UIDocument uidoc, IReadOnlyList<ElementId> beamIds)
    {
        var doc = uidoc.Document;
        var beams = beamIds.Select(doc.GetElement).Where(e => e is not null).ToList();
        return KataBeamMatcher.Measure(doc, uidoc.ActiveView, beams);
    }

    private static KataRebarGenerationResult Generate(UIDocument uidoc, KataRebarRequest request)
    {
        var doc = uidoc.Document;
        if (request.Spec is null) return KataRebarGenerationResult.Failed("Chưa có dữ liệu sheet Dam.");

        var match = Measure(uidoc, request.BeamIds);
        if (!match.IsSuccess || match.Measured is null)
            return KataRebarGenerationResult.Failed($"Không đo được dầm: {match.Message}");

        var plan = KataRebarPlanner.Plan(request.Spec, match.Measured);
        if (!plan.CanGenerate)
            return KataRebarGenerationResult.Failed("Không vẽ: " + string.Join(" ", plan.Blocking));

        var barTypes = new Dictionary<double, RebarBarType>();
        var missing = new List<string>();
        foreach (double diameter in Diameters(plan))
        {
            if (request.BarTypeIds.TryGetValue(diameter, out var id) && doc.GetElement(id) is RebarBarType type)
                barTypes[diameter] = type;
            else
                missing.Add($"Ø{diameter:0.#}");
        }

        if (missing.Count > 0)
            return KataRebarGenerationResult.Failed($"Thiếu RebarBarType cho {string.Join(", ", missing)}: tải kiểu thép vào dự án hoặc chọn kiểu khác trong bảng.");

        var shape = KataRebarShapeResolver.ClosedStirrup(doc);
        if (shape is null)
            Log.Warning("Kata Rebar: no closed stirrup shape ({Names}) in the project; stirrups are drawn one by one", string.Join(", ", KataRebarShapeResolver.ClosedStirrupNames));

        return KataRebarOrchestrator.Execute(doc, match, plan, barTypes, shape);
    }

    private static IEnumerable<double> Diameters(KataRebarPlan plan)
    {
        var diameters = plan.Layout.MainTopBars.Concat(plan.Layout.MainBottomBars).Concat(plan.Layout.ExtraTopBars).Select(b => b.Diameter).ToList();
        if (plan.Layout.StirrupZones.Count > 0) diameters.Add(plan.Rules.StirrupDiameter);
        return diameters.Distinct();
    }
}
