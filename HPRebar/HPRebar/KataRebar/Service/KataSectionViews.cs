using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using HPRebar.BeamRebar.Service;
using Serilog;
using RevitView = Autodesk.Revit.DB.View;

namespace HPRebar.KataRebar.Service;

/// <summary>
/// The section views Kata Rebar makes (the run's long section, a cross section per section flag), all made the same
/// way: from a box with no view template, at 1:25, found again by <see cref="KataDraftingStorage"/>, re-cropped when
/// they still look the same way from the same plane, otherwise kept for the user untagged and made anew.
/// </summary>
internal static class KataSectionViews
{
    public const int Scale = 25;

    /// <summary>A view of the run and how it was obtained.</summary>
    public sealed record Result(ViewSection View, bool Reused, string? Superseded);

    /// <summary>
    /// Where the view looks from and what it shows: <paramref name="Origin"/> on its cut plane, the screen's right and
    /// the direction it looks along; the crop in screen millimetres (right, up) from the origin and its depth.
    /// </summary>
    public sealed record Frame(XYZ Origin, XYZ Right, XYZ Look, double Left, double RightEdge, double Bottom, double Top, double DepthMm);

    public static Result FindOrCreate(Document doc, string runKey, string kind, Frame frame, string viewName)
    {
        var box = Box(frame);
        var found = KataDraftingStorage.Find<ViewSection>(doc, runKey, kind).OrderBy(v => IdValue(v.Id)).ToList();
        if (found.Count > 1)
            Log.Warning("Kata Rebar: {Count} views carry the tag {Kind} of the run (duplicated with detailing?); using id {Id}", found.Count, kind, found[0].Id);

        var existing = found.FirstOrDefault();
        if (existing is not null && SameOrientation(existing, frame))
        {
            Recrop(existing, box);
            return new Result(existing, true, null);
        }

        // Never delete a view the user may have annotated or placed on a sheet: drop its tag and make a new one.
        if (existing is not null) KataDraftingStorage.Forget(existing);

        var view = ViewSection.CreateSection(doc, SectionType(doc).Id, box);
        // A section type may hand new views a template that locks scale and detail level: this view has none.
        view.ViewTemplateId = ElementId.InvalidElementId;
        view.Name = UniqueName(doc, viewName);
        view.Scale = Scale;
        view.DetailLevel = ViewDetailLevel.Fine;
        KataDraftingStorage.Write(view, runKey, kind);
        return new Result(view, false, existing?.Name);
    }

    /// <summary>
    /// The section box. A section created from a box looks along the box's +Z with its Min.Z as the cut plane and the
    /// screen's right along −BasisX (measured: a box with BasisX = the beam axis showed the run mirrored). So BasisX =
    /// −right, BasisZ = BasisX × up = the look direction; the crop's right-hand millimetres map to −box X.
    /// </summary>
    private static BoundingBoxXYZ Box(Frame f)
    {
        var right = f.Right.Normalize();
        var transform = Transform.Identity;
        transform.Origin = f.Origin;
        transform.BasisX = right.Negate();
        transform.BasisY = XYZ.BasisZ;
        transform.BasisZ = right.Negate().CrossProduct(XYZ.BasisZ);
        return new BoundingBoxXYZ
        {
            Transform = transform,
            Min = new XYZ(-RevitUnits.MmToFt(f.RightEdge), RevitUnits.MmToFt(f.Bottom), 0.0),
            Max = new XYZ(-RevitUnits.MmToFt(f.Left), RevitUnits.MmToFt(f.Top), RevitUnits.MmToFt(f.DepthMm))
        };
    }

    /// <summary>
    /// The view still shows the run the same way: the same screen right and its plane where the frame cuts. Compared
    /// through the view's own directions — Revit stores the crop box of a created section in a transform of its own,
    /// which differs from the box it was created from (measured: compared directly, every re-run recreated it).
    /// </summary>
    private static bool SameOrientation(ViewSection view, Frame f) =>
        view.RightDirection.IsAlmostEqualTo(f.Right.Normalize(), 1e-6)
        && Math.Abs((view.Origin - f.Origin).DotProduct(f.Look.Normalize())) < RevitUnits.MmToFt(1.0);

    /// <summary>
    /// Re-crops in the view's own frame (the CropBox setter keeps the view's transform and reads only Min/Max there),
    /// width, height and depth alike.
    /// </summary>
    private static void Recrop(ViewSection view, BoundingBoxXYZ box)
    {
        var crop = view.CropBox;
        var toView = crop.Transform.Inverse;
        var a = toView.OfPoint(box.Transform.OfPoint(box.Min));
        var b = toView.OfPoint(box.Transform.OfPoint(box.Max));
        crop.Min = new XYZ(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Min(a.Z, b.Z));
        crop.Max = new XYZ(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y), Math.Max(a.Z, b.Z));
        view.CropBox = crop;
        view.CropBoxActive = true;
    }

    /// <summary>A section view type, one without a template for new views first, in a stable order.</summary>
    private static ViewFamilyType SectionType(Document doc) =>
        new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>()
            .Where(t => t.ViewFamily == ViewFamily.Section)
            .OrderBy(t => t.DefaultTemplateId == ElementId.InvalidElementId ? 0 : 1)
            .ThenBy(t => IdValue(t.Id))
            .FirstOrDefault()
        ?? throw new InvalidOperationException("Model không có loại view Section để tạo mặt cắt dầm.");

    // Multi-version: ElementId.Value is long since 2024
    public static long IdValue(ElementId id) =>
#if REVIT2024_OR_GREATER
        id.Value;
#else
        id.IntegerValue;
#endif

    /// <summary><paramref name="name"/>, or "<paramref name="name"/> (Kata)", "(Kata 2)"… when another view has it (any case).</summary>
    private static string UniqueName(Document doc, string name)
    {
        var taken = new HashSet<string>(new FilteredElementCollector(doc).OfClass(typeof(RevitView)).Cast<RevitView>().Select(v => v.Name), StringComparer.OrdinalIgnoreCase);
        if (!taken.Contains(name)) return name;
        for (int i = 1; ; i++)
        {
            string candidate = i == 1 ? $"{name} (Kata)" : $"{name} (Kata {i})";
            if (!taken.Contains(candidate)) return candidate;
        }
    }
}
