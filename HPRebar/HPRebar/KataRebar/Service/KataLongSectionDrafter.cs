using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HPRebar.Core.BeamRebar.Models;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;
using Serilog;

namespace HPRebar.KataRebar.Service;

/// <summary>
/// The run's drafting after its bars exist. "Thể hiện móc cắt kết thúc thép": the long section (named after cell B3)
/// shows the bars unobscured with a cut mark at every bar end. "Tạo bản vẽ Kata": the long section also gets Kata's
/// dimensions, every section flag a cross section (<see cref="KataCrossSections"/>), and a sheet holds them
/// (<see cref="KataSheetComposer"/>). Everything the previous run drew (marks, dimensions, 2D lines) always goes first;
/// with both settings off, or no bar to show, nothing new is drawn and no view is made.
/// </summary>
internal static class KataLongSectionDrafter
{
    /// <param name="Superseded">Names of the run's previous views, kept untagged because they no longer fit the run.</param>
    public sealed record Outcome(string? ViewName, int Marks, int DeletedMarks, string? Superseded = null)
    {
        /// <summary>Why the step failed and was rolled back on its own (the bars stay); null when it ran.</summary>
        public string? Error { get; init; }

        public int CrossSections { get; init; }

        public int Dims { get; init; }

        public int RefusedDims { get; init; }

        public string? SheetNumber { get; init; }
    }

    /// <summary>Call inside an open transaction, after the bars of the run were created.</summary>
    public static Outcome Apply(Document doc, KataRebarPlan plan, KataBeamPlacement placement, IReadOnlyList<Element> hosts)
    {
        string runKey = KataDraftingStorage.RunKey(hosts);
        int deleted = KataBarEndMarkCreator.DeletePrevious(doc, runKey);
        DeletePreviousDrafting(doc, runKey);

        bool drawings = plan.Rules.CreateKataDrawings;
        var marks = plan.Rules.ShowBarEndMarks ? KataBarEndMarkLayout.Build(plan.Layout) : new List<KataBarEndMark>();
        if ((!drawings && marks.Count == 0) || !plan.Layout.LongitudinalBars.Any()) return new Outcome(null, 0, deleted);

        var mapper = placement.Mapper;
        string name = KataViewName.Clean(plan.Spec.BeamName, $"Kata {hosts[0].Id}");
        var cuts = KataSectionCuts.Build(plan.Spec, plan.Layout);
        var elevation = drawings
            ? KataElevationDrawingBuilder.Build(plan.Spec, plan.Layout, cuts, plan.Rules.StirrupDiameter,
                KataBarTagBuilder.StirrupRowOf(KataBarTagBuilder.Build(plan.Spec, plan.Layout, plan.Rules.StirrupDiameter)))
            : null;
        var section = KataLongSectionView.FindOrCreate(doc, runKey, plan, mapper, name, elevation);
        var view = section.View;
        var bars = KataRebarCleanupService.FindPrevious(doc, hosts).Select(doc.GetElement).OfType<Rebar>().ToList();
        foreach (var rebar in bars) rebar.SetUnobscuredInView(view, true);

        double planeY = KataLongSectionView.CutPlaneY(mapper, plan.Spec.Width);
        int drawn = KataBarEndMarkCreator.Draw(doc, view, runKey, marks, mapper, planeY);
        var superseded = new List<string>();
        if (section.Superseded is not null) superseded.Add(section.Superseded);

        int dims = 0, refused = 0, crossCount = 0;
        string? sheetNumber = null;
        if (elevation is not null)
        {
            (dims, refused) = KataDrawingDims.Create(doc, view, runKey, KataDimChains.From(elevation.Dims),
                (x, z) => mapper.ToXyz(new Point3(x, planeY, z)));
            var cross = KataCrossSections.Create(doc, plan, mapper, runKey, name, cuts, bars);
            dims += cross.Dims;
            refused += cross.RefusedDims;
            crossCount = cross.Views.Count;
            superseded.AddRange(cross.Superseded);

            var (left, right, bottom, top) = KataLongSectionView.Extents(plan, elevation);
            var sheet = KataSheetComposer.Compose(doc, runKey, name, new KataSheetComposer.Item(view, left, right, bottom, top), elevation.Bottom,
                cross.Views.Select(v => new KataSheetComposer.Item(v.View, v.Drawing.MinX, v.Drawing.MaxX, v.Drawing.Bottom, v.Drawing.Top)).ToList());
            sheetNumber = sheet.SheetNumber;
        }

        Log.Information("Kata Rebar: long section {View} (id {Id}, {How}): {Marks} cut marks, {Deleted} previous marks deleted; "
                        + "{Cross} cross sections, {Dims} dimensions ({Refused} refused), sheet {Sheet}",
            view.Name, view.Id, section.Reused ? "reused" : section.Superseded is null ? "created" : $"created, '{section.Superseded}' kept untagged",
            drawn, deleted, crossCount, dims, refused, sheetNumber ?? "-");
        return new Outcome(view.Name, drawn, deleted, superseded.Count > 0 ? string.Join(", ", superseded) : null)
        {
            CrossSections = crossCount,
            Dims = dims,
            RefusedDims = refused,
            SheetNumber = sheetNumber
        };
    }

    /// <summary>Dimensions, their ticks and the 2D lines the previous run drew (deleting the ticks takes their dimensions too).</summary>
    private static void DeletePreviousDrafting(Document doc, string runKey)
    {
        var ids = KataDraftingStorage.Find<Dimension>(doc, runKey, KataDraftingStorage.DraftingKind).Select(e => e.Id)
            .Concat(KataDraftingStorage.Find<CurveElement>(doc, runKey, KataDraftingStorage.DraftingKind).Select(e => e.Id))
            .Distinct().ToList();
        if (ids.Count > 0) doc.Delete(ids);
    }
}
