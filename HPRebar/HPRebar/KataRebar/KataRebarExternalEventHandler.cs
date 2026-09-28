using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using HPRebar.BeamRebar.Service;
using HPRebar.Core.KataRebar.Models;
using HPRebar.KataRebar.Model;
using HPRebar.KataRebar.Service;
using HPRebar.KataRebar.ViewModel;
using Serilog;

namespace HPRebar.KataRebar;

/// <summary>
/// Executes requests from the modeless KataRebar UI on Revit's main API thread via ExternalEvent.
/// </summary>
public sealed class KataRebarExternalEventHandler : IExternalEventHandler, IKataRebarRunner, IDisposable
{
    private readonly ConcurrentQueue<KataRebarRequest> _queue = new();
    private readonly ExternalEvent _externalEvent;

    public KataRebarExternalEventHandler()
    {
        _externalEvent = ExternalEvent.Create(this);
    }

    public async Task<KataBeamMatchResult?> RepickBeamsAsync(KataBeamRebarSpec spec)
    {
        var req = new KataRebarRequest(KataRebarRequestKind.Repick) { Spec = spec };
        _queue.Enqueue(req);
        _externalEvent.Raise();
        var result = await req.Completion.Task;
        return result as KataBeamMatchResult;
    }

    public async Task<KataBeamMatchResult?> MatchBeamsAsync(IReadOnlyList<ElementId> beamIds, KataBeamRebarSpec spec)
    {
        var req = new KataRebarRequest(KataRebarRequestKind.Match) { BeamIds = beamIds, Spec = spec };
        _queue.Enqueue(req);
        _externalEvent.Raise();
        var result = await req.Completion.Task;
        return result as KataBeamMatchResult;
    }

    public Task HighlightBeamsAsync(IReadOnlyList<ElementId> beamIds)
    {
        var req = new KataRebarRequest(KataRebarRequestKind.Highlight) { BeamIds = beamIds };
        _queue.Enqueue(req);
        _externalEvent.Raise();
        return req.Completion.Task;
    }

    public async Task<KataRebarGenerationResult> GenerateRebarAsync(
        KataBeamMatchResult matchResult,
        KataBeamRebarSpec spec,
        KataRebarLayoutResult layout,
        IReadOnlyDictionary<double, RebarBarType> resolvedBarTypes)
    {
        var req = new KataRebarRequest(KataRebarRequestKind.Generate)
        {
            MatchResult = matchResult,
            Spec = spec,
            Layout = layout,
            ResolvedBarTypes = resolvedBarTypes
        };
        _queue.Enqueue(req);
        _externalEvent.Raise();
        var result = await req.Completion.Task;
        return (result as KataRebarGenerationResult) ?? new KataRebarGenerationResult
        {
            IsSuccess = false,
            Message = "Không nhận được phản hồi từ Revit API."
        };
    }

    public void Execute(UIApplication app)
    {
        var uidoc = app.ActiveUIDocument;
        if (uidoc is null)
        {
            while (_queue.TryDequeue(out var orphan))
                orphan.Completion.TrySetException(new InvalidOperationException("Không có tài liệu Revit nào đang mở."));
            return;
        }

        var doc = uidoc.Document;

        while (_queue.TryDequeue(out var req))
        {
            try
            {
                switch (req.Kind)
                {
                    case KataRebarRequestKind.Repick:
                    {
                        try
                        {
                            var refs = uidoc.Selection.PickObjects(
                                ObjectType.Element,
                                new KataRebarSelectionFilter(),
                                "Chọn dải dầm liên tục trong Revit (nhấn Finish khi xong)");

                            if (refs is null || refs.Count == 0)
                            {
                                req.Completion.TrySetResult(null);
                                break;
                            }

                            var beams = refs.Select(r => doc.GetElement(r)).Where(e => e is not null).ToList();
                            var matchResult = req.Spec is not null
                                ? KataBeamMatcher.Match(doc, beams!, req.Spec)
                                : null;

                            req.Completion.TrySetResult(matchResult);
                        }
                        catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                        {
                            req.Completion.TrySetResult(null);
                        }
                        break;
                    }

                    case KataRebarRequestKind.Match:
                    {
                        var beams = req.BeamIds is not null
                            ? req.BeamIds.Select(id => doc.GetElement(id)).Where(e => e is not null).ToList()
                            : new List<Element>();

                        var matchResult = req.Spec is not null
                            ? KataBeamMatcher.Match(doc, beams!, req.Spec)
                            : null;

                        req.Completion.TrySetResult(matchResult);
                        break;
                    }

                    case KataRebarRequestKind.Highlight:
                    {
                        if (req.BeamIds is not null && req.BeamIds.Count > 0)
                        {
                            uidoc.Selection.SetElementIds(req.BeamIds.ToList());
                        }
                        req.Completion.TrySetResult(null);
                        break;
                    }

                    case KataRebarRequestKind.Generate:
                    {
                        if (req.MatchResult is null || req.Spec is null || req.Layout is null || req.ResolvedBarTypes is null)
                        {
                            req.Completion.TrySetResult(new KataRebarGenerationResult
                            {
                                IsSuccess = false,
                                Message = "Dữ liệu yêu cầu tạo thép không đầy đủ."
                            });
                            break;
                        }

                        if (req.MatchResult.PointMapper is null || req.MatchResult.OrderedBeams.Count == 0)
                        {
                            req.Completion.TrySetResult(new KataRebarGenerationResult
                            {
                                IsSuccess = false,
                                Message = "Không có thông tin dầm và hệ tọa độ hình học."
                            });
                            break;
                        }

                        var shapes = KataRebarShapeResolver.Load(doc);
                        var genResult = KataRebarOrchestrator.Execute(
                            doc,
                            req.MatchResult.OrderedBeams,
                            req.MatchResult.PointMapper,
                            req.Spec,
                            req.Layout,
                            req.ResolvedBarTypes,
                            shapes);

                        req.Completion.TrySetResult(genResult);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi khi thực thi yêu cầu KataRebar trong Revit API thread");
                req.Completion.TrySetException(ex);
            }
        }
    }

    public string GetName() => "KataRebarExternalEventHandler";

    public void Dispose()
    {
        _externalEvent.Dispose();
    }
}
