using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HPRebar.Core.KataRebar.ColumnStack;
using HPRebar.KataRebar.Service;
using Serilog;
using RevitView = Autodesk.Revit.DB.View;

namespace HPRebar.KataExport.Service;

/// <summary>What a rebar Kata Column Rebar created is, for the views' tags.</summary>
internal enum KataColumnRebarRole
{
    /// <summary>A main bar of the storey (starters are not tagged).</summary>
    Bar,

    /// <summary>An outer hoop set of one zone.</summary>
    Hoop,

    /// <summary>An inner □ or C tie set.</summary>
    Tie
}

/// <summary>
/// A rebar Kata Column Rebar created, what it is and where its tag goes (section mm of its storey); a hoop set also
/// its elevations (mm), so the section tags the set it cuts.
/// </summary>
internal sealed record KataColumnCreatedRebar(Rebar Rebar, int Storey, KataColumnRebarRole Role, double XMm, double YMm, double FromMm, double ToMm);

/// <summary>
/// One horizontal section per storey as a structural plan of the storey's base level, cut where the section drawing
/// says (<see cref="KataColumnSectionDrawing.CutElevationMm" />), cropped to the column with its dimensions, 1:20, no
/// template. Revit's sections look sideways, so a plan is the view that cuts a column across. Each view carries the
/// stack's drafting tag and is found again on the next run (re-cut, re-cropped, its old dimensions and notes replaced);
/// tags of the bars go with the bars a re-run deletes.
/// </summary>
internal sealed class KataColumnSectionViews
{
    public const int Scale = 20;
    private const string KindPrefix = "ColumnSection:";
    private const double TopAboveCutMm = 300;
    private const double DepthBelowHoopMm = 50;

    private readonly Document _doc;
    private readonly IReadOnlyList<FamilyInstance> _columns;
    private readonly KataColumnViewAnnotator _annotator;
    private readonly string _runKey;
    private readonly string _name;

    public KataColumnSectionViews(Document doc, IReadOnlyList<FamilyInstance> columns, KataColumnViewAnnotator annotator, string name)
    {
        _doc = doc;
        _columns = columns;
        _annotator = annotator;
        _runKey = annotator.RunKey;
        _name = name;
    }

    public List<string> Warnings { get; } = new();

    public int Views { get; private set; }

    public int Dims { get; private set; }

    public int RefusedDims { get; private set; }

    public void Create(IReadOnlyList<KataColumnStoreyGeometry> storeys, IReadOnlyList<KataColumnSectionDrawing> drawings, IReadOnlyList<KataColumnCreatedRebar> rebars, double maxSpacingMm)
    {
        for (int k = 0; k < drawings.Count; k++)
        {
            try
            {
                Storey(k, storeys[k], drawings[k], rebars, maxSpacingMm);
                Views++;
            }
            catch (Exception ex) when (ex is Autodesk.Revit.Exceptions.ApplicationException or InvalidOperationException or ArgumentException)
            {
                Warnings.Add($"Mặt cắt {drawings[k].Level}: không tạo được view ({ex.Message}).");
                Log.Warning(ex, "Kata Column Rebar: section view of {Storey} failed", drawings[k].Level);
            }
        }

        RemoveExtra(drawings.Count);
    }

    /// <summary>
    /// Section views of storeys the stack no longer has (it lost storeys since the last run) are deleted with their
    /// drafting; one a sheet shows or the user has open is kept, untagged, and named in a warning.
    /// </summary>
    private void RemoveExtra(int count)
    {
        IReadOnlyList<ViewPlan> extra = count == 0 ? Array.Empty<ViewPlan>() : KataDraftingStorage.Find<ViewPlan>(_doc, _runKey, kind => Index(kind) >= count);
        if (extra.Count == 0)
        {
            return;
        }

        var kept = new FilteredElementCollector(_doc).OfClass(typeof(Viewport)).Cast<Viewport>().Select(v => v.ViewId).ToHashSet();
        if (_doc.ActiveView is { } active)
        {
            kept.Add(active.Id);
        }

        foreach (var view in extra)
        {
            if (kept.Contains(view.Id))
            {
                KataDraftingStorage.Forget(view);
                Warnings.Add($"View '{view.Name}' thuộc tầng cột không còn nữa: đang mở hoặc đặt trên sheet nên được giữ lại, xoá tay nếu không cần.");
            }
            else
            {
                _doc.Delete(view.Id);
            }
        }

        Log.Information("Kata Column Rebar: {Count} section views of storeys the stack no longer has removed", extra.Count);
    }

    /// <summary>The storey index of a section view's kind, or −1 for any other kind.</summary>
    private static int Index(string kind) =>
        kind.StartsWith(KindPrefix, StringComparison.Ordinal)
        && int.TryParse(kind.Substring(KindPrefix.Length), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int k)
            ? k
            : -1;

    private void Storey(int k, KataColumnStoreyGeometry g, KataColumnSectionDrawing d, IReadOnlyList<KataColumnCreatedRebar> rebars, double maxSpacingMm)
    {
        var level = _doc.GetElement(_columns[k].get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_PARAM)?.AsElementId() ?? ElementId.InvalidElementId) as Level
                    ?? throw new InvalidOperationException("cột không có Base Level");
        var view = FindOrCreate(k, level, $"{_name} – {d.Level}");

        XYZ At(double x, double y) => _annotator.ToModel(g.MinXMm + x, g.MinYMm + y, d.CutElevationMm);
        // Range and crop first: when a template locks them the old drawing stays as it was.
        Cut(view, level, d.CutElevationMm, maxSpacingMm);
        Crop(view, d, At);
        _annotator.ClearDrafting(view);

        foreach (var created in rebars)
        {
            created.Rebar.SetUnobscuredInView(view, true);
        }

        // A new view shows nothing to tag until its range, crop and the bars' unobscured setting are regenerated.
        _doc.Regenerate();
        Log.Information("Kata Column Rebar: {View} cut at {Cut:0} mm shows {Count} rebar", view.Name, d.CutElevationMm,
            new FilteredElementCollector(_doc, view.Id).OfClass(typeof(Rebar)).GetElementCount());
        var (dims, refused) = KataDrawingDims.Create(_doc, view, _runKey, KataColumnSectionDims.Chains(d), (x, y) => At(x, y), _annotator.AlongB, _annotator.AlongH);
        Dims += dims;
        RefusedDims += refused;
        Tag(view, d, rebars.Where(r => r.Storey == k).ToList(), At);
    }

    private ViewPlan FindOrCreate(int k, Level level, string name)
    {
        string kind = KindPrefix + k.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var found = KataDraftingStorage.Find<ViewPlan>(_doc, _runKey, kind).OrderBy(v => KataSectionViews.IdValue(v.Id)).ToList();
        if (found.Count > 1)
        {
            Log.Warning("Kata Column Rebar: {Count} views carry the tag {Kind} of the stack (duplicated with detailing?); using id {Id}", found.Count, kind, found[0].Id);
        }

        var existing = found.FirstOrDefault();
        if (existing is not null && existing.GenLevel?.Id == level.Id)
        {
            return existing;
        }

        // A view of another level (the stack was re-modelled) is left to the user, untagged; a new one is made.
        if (existing is not null)
        {
            KataDraftingStorage.Forget(existing);
        }

        var view = ViewPlan.Create(_doc, PlanType().Id, level.Id);
        view.ViewTemplateId = ElementId.InvalidElementId;
        // Tagged before it is named, so a view whose naming fails is still found (and reused) by the next run.
        KataDraftingStorage.Write(view, _runKey, kind);
        view.Name = KataSectionViews.UniqueName(_doc, name);
        view.Scale = Scale;
        view.DetailLevel = ViewDetailLevel.Fine;
        return view;
    }

    /// <summary>Cut plane at the section's elevation, bottom and depth one hoop spacing below so a hoop shows.</summary>
    private static void Cut(ViewPlan view, Level level, double cutMm, double maxSpacingMm)
    {
        double cut = RevitUnits.MmToFt(cutMm) - level.ProjectElevation;
        double top = cut + RevitUnits.MmToFt(TopAboveCutMm);
        double bottom = cut - RevitUnits.MmToFt(maxSpacingMm + DepthBelowHoopMm);
        var range = view.GetViewRange();
        foreach (var (plane, offset) in new[]
                 {
                     (PlanViewPlane.TopClipPlane, top), (PlanViewPlane.CutPlane, cut),
                     (PlanViewPlane.BottomClipPlane, bottom), (PlanViewPlane.ViewDepthPlane, bottom),
                 })
        {
            range.SetLevelId(plane, level.Id);
            range.SetOffset(plane, offset);
        }

        view.SetViewRange(range);
    }

    /// <summary>The section plus room for the two dimension rows and the tags on every side.</summary>
    private static void Crop(ViewPlan view, KataColumnSectionDrawing d, Func<double, double, XYZ> at)
    {
        double size = Math.Max(d.WidthMm, d.DepthMm);
        double margin = Math.Max(250, 0.6 * size);
        var corners = new[] { at(-margin, -margin), at(d.WidthMm + margin, -margin), at(-margin, d.DepthMm + margin), at(d.WidthMm + margin, d.DepthMm + margin) };
        var crop = view.CropBox;
        var toView = crop.Transform.Inverse;
        var local = corners.Select(toView.OfPoint).ToList();
        crop.Min = new XYZ(local.Min(p => p.X), local.Min(p => p.Y), crop.Min.Z);
        crop.Max = new XYZ(local.Max(p => p.X), local.Max(p => p.Y), crop.Max.Z);
        view.CropBox = crop;
        view.CropBoxActive = true;
        view.CropBoxVisible = false;
    }

    /// <summary>Every bar outside its face, the hoop at the top left corner, each tie at its label point.</summary>
    private void Tag(RevitView view, KataColumnSectionDrawing d, IReadOnlyList<KataColumnCreatedRebar> rebars, Func<double, double, XYZ> at)
    {
        double size = Math.Max(d.WidthMm, d.DepthMm);
        double reach = Math.Max(60, 0.18 * size);
        double cx = d.WidthMm / 2, cy = d.DepthMm / 2;
        double cut = d.CutElevationMm;
        // The hoop set the section cuts, or the nearest one when the cut falls between two zones.
        var hoop = rebars.Where(r => r.Role == KataColumnRebarRole.Hoop)
            .OrderBy(r => cut >= r.FromMm && cut <= r.ToMm ? 0 : Math.Min(Math.Abs(cut - r.FromMm), Math.Abs(cut - r.ToMm)))
            .FirstOrDefault();
        foreach (var r in rebars)
        {
            switch (r.Role)
            {
                case KataColumnRebarRole.Bar:
                {
                    double dx = r.XMm - cx, dy = r.YMm - cy, len = Math.Max(Math.Sqrt(dx * dx + dy * dy), 1);
                    _annotator.Tag(view, r.Rebar, at(r.XMm + dx / len * reach, r.YMm + dy / len * reach));
                    break;
                }
                case KataColumnRebarRole.Hoop when ReferenceEquals(r, hoop):
                    _annotator.Tag(view, r.Rebar, at(-reach, d.DepthMm + reach));
                    break;
                case KataColumnRebarRole.Tie:
                    _annotator.Tag(view, r.Rebar, at(r.XMm, r.YMm));
                    break;
            }
        }
    }

    private ViewFamilyType PlanType() =>
        new FilteredElementCollector(_doc).OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>()
            .Where(t => t.ViewFamily == ViewFamily.StructuralPlan)
            .OrderBy(t => t.DefaultTemplateId == ElementId.InvalidElementId ? 0 : 1)
            .ThenBy(t => KataSectionViews.IdValue(t.Id))
            .FirstOrDefault()
        ?? throw new InvalidOperationException("model không có loại view Structural Plan");
}
