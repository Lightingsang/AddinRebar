using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using HPRebar.Core.KataRebar.ColumnStack;
using HPRebar.KataRebar.Service;
using Serilog;

namespace HPRebar.KataExport.Service;

/// <summary>
/// The drawing steps of a Kata Column Rebar run, after the bars exist: one section plan per storey, then the
/// elevation along b, each its own transaction. Whatever stops one (Revit refusing the commit, an exception the step
/// did not expect) costs only that drawing — the runner has already rolled the step back — and becomes a warning, so
/// the group still keeps the bars.
/// </summary>
internal sealed class KataColumnViewsStep
{
    private readonly Document _doc;
    private readonly IReadOnlyList<FamilyInstance> _columns;
    private readonly Transform _frame;
    private readonly KataColumnSpec _spec;
    private readonly IReadOnlyList<KataColumnStoreyGeometry> _storeys;
    private readonly KataColumnRebarDesign _design;

    public KataColumnViewsStep(Document doc, IReadOnlyList<FamilyInstance> columns, Transform frame, KataColumnSpec spec,
        IReadOnlyList<KataColumnStoreyGeometry> storeys, KataColumnRebarDesign design)
    {
        _doc = doc;
        _columns = columns;
        _frame = frame;
        _spec = spec;
        _storeys = storeys;
        _design = design;
    }

    /// <returns>What was drawn, for the run's log line.</returns>
    public string Draw(KataTransactionRunner runner, string name, IReadOnlyList<KataColumnCreatedRebar> created, List<string> warnings)
    {
        var annotator = new KataColumnViewAnnotator(_doc, _columns, _frame);
        var views = new KataColumnSectionViews(_doc, _columns, annotator, name);
        double spacing = _design.Storeys.SelectMany(s => s.HoopZones).Select(z => z.SpacingMm).DefaultIfEmpty(200).Max();
        Step(runner, "Kata Column Rebar: mặt cắt", "Mặt cắt", warnings, () =>
        {
            views.Create(_storeys, KataColumnSectionDrawings.Build(_spec, _storeys, _design), created, spacing);
            return views.Views;
        });
        warnings.AddRange(views.Warnings);
        var elevation = Step(runner, "Kata Column Rebar: mặt đứng", "Mặt đứng", warnings, () =>
        {
            var (dims, refused) = KataColumnElevationView.Create(_doc, annotator, name, _storeys, KataColumnElevationDims.Build(_spec, _storeys, _design), created);
            return $"{dims} dims ({refused} refused)";
        }) ?? "failed";

        if (annotator.TagsMissing)
        {
            warnings.Add("Model chưa có family tag thép (Structural Rebar Tags): số hiệu trên mặt cắt, mặt đứng ghi bằng chữ.");
        }

        if (annotator.RefusedTags > 0)
        {
            warnings.Add($"{annotator.RefusedTags} tag số hiệu bị Revit từ chối (thanh không thấy trong view); xem log.");
        }

        if (views.RefusedDims > 0)
        {
            warnings.Add($"{views.RefusedDims} chuỗi dim trên mặt cắt bị Revit từ chối; xem log.");
        }

        return $"{views.Views} section views, {views.Dims} dims ({views.RefusedDims} refused), elevation {elevation}, {annotator.Tags} tags ({annotator.TagWays})";
    }

    private static T? Step<T>(KataTransactionRunner runner, string step, string label, List<string> warnings, Func<T> body)
    {
        try
        {
            return runner.Run(step, body);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            warnings.Add($"{label}: không vẽ được ({ex.Message}).");
            Log.Warning(ex, "Kata Column Rebar: {Step} failed, the bars are kept", step);
            return default;
        }
    }
}
