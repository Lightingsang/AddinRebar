using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using HPRebar.Core.KataExport.ColumnStack;

namespace HPRebar.KataExport.Service;

/// <summary>What was read for one column stack: the input for the sheet (with what looked odd), or why it cannot be built.</summary>
public sealed record KataColumnStackReading(
    KataColumnStackInput? Input,
    IReadOnlyList<ElementId> ColumnIds,
    string? Error,
    IReadOnlyList<string> Warnings)
{
    /// <summary>The columns bottom to top, one per storey of <see cref="Input"/>.</summary>
    public IReadOnlyList<FamilyInstance> Columns { get; init; } = Array.Empty<FamilyInstance>();

    /// <summary>The plan frame the sections are measured in (origin and X/Y of the lowest rectangular column).</summary>
    public Transform? Frame { get; init; }

    /// <summary>Another structural column stands on the top one (its bars lap into it instead of anchoring).</summary>
    public bool TopContinues { get; init; }
}

/// <summary>
/// Measures a column stack picked in Revit: one structural column per storey, bottom to top. Columns are measured on
/// their geometry before joins and cuts (a floor or beam cutting a column must not shorten its storey). Sections are
/// taken in the plan frame of the lowest rectangular column (its own X along b, Y along h), so the storeys above keep
/// their real offsets. Beams, the floor and the pile cap are found by bounding box around the column ends.
/// </summary>
public static class KataColumnStackReader
{
    /// <summary>Elements this close (mm) to a column end belong to that joint.</summary>
    private const double JointReachMm = 50.0;

    /// <summary>A beam whose top is within this distance (mm) below a column head frames into that head.</summary>
    private const double BeamTopReachMm = 300.0;

    /// <summary>Shared parameters of HP projects holding the member name and how many identical members there are.</summary>
    private const string ElementNameParameter = "STR_ElementName";
    private const string ElementCountParameter = "STR_ElementCount";

    /// <summary>Two plan axes closer than this (degrees, modulo 90) are the same orientation.</summary>
    private const double AxisToleranceDeg = 1.0;

    /// <summary>A circle's plan extents and its cylinder's diameter agree within this (mm).</summary>
    private const double RoundToleranceMm = 2.0;

    /// <summary>A section whose area differs from b × h (or the circle) by more than this fraction is not a plain one.</summary>
    private const double SectionAreaTolerance = 0.02;

    public static KataColumnStackReading Read(Document doc, IReadOnlyList<FamilyInstance> columns)
    {
        var ids = columns.Select(c => c.Id).ToList();
        var measured = columns.Select(Measure).Where(m => m is not null).Select(m => m!).OrderBy(m => m.BottomFt).ToList();
        if (measured.Count != columns.Count)
        {
            return Refused(ids, "Có cột không đọc được hình khối (solid) — bỏ cột đó ra khỏi lựa chọn.");
        }

        if (measured.Any(m => m.IsSlanted))
        {
            return Refused(ids, "Có cột nghiêng; Kata chỉ vẽ cột đứng.");
        }

        // A round column has no b direction of its own: the frame comes from the first rectangular one.
        var frame = (measured.FirstOrDefault(m => !m.IsRound) ?? measured[0]).Transform;
        if (measured.Any(m => !m.IsRound && !SameAxes(m.Transform, frame)))
        {
            return Refused(ids, "Các cột xoay khác hướng nhau; Kata cần một hướng b/h cho cả chồng cột.");
        }

        var warnings = new List<string>();
        var storeys = measured.Select(m => Storey(doc, m, frame, warnings)).ToList();
        if (!OverlapInPlan(storeys))
        {
            return Refused(ids, "Các cột không chồng lên nhau trên mặt bằng — hãy chọn các cột của cùng một trục cột.");
        }

        var first = measured[0];
        var cap = PileCap(doc, first);
        if (cap is { } found && found.BottomElevationMm + found.HeightMm > storeys[0].BaseElevationMm + JointReachMm)
        {
            // The column is modelled from inside the cap: Kata's first storey starts on the cap top.
            storeys[0] = storeys[0] with { BaseElevationMm = found.BottomElevationMm + found.HeightMm };
            warnings.Add($"{storeys[0].LevelName}: the column starts inside the pile cap; its storey is counted from the cap top.");
        }

        var input = new KataColumnStackInput(
            ColumnName(first.Instance),
            ColumnCount(first.Instance),
            storeys,
            cap,
            Beams(doc, first).Select(b => (double?)BeamWidthMm(b)).DefaultIfEmpty(null).Max(),
            SlabThicknessMm(doc, first));
        var top = measured[measured.Count - 1];
        bool continues = Around(doc, top, top.TopFt, top.TopFt, BuiltInCategory.OST_StructuralColumns)
            .Any(c => !ids.Contains(c.Id) && c.get_BoundingBox(null) is { } box && box.Min.Z >= top.TopFt - RevitUnits.MmToFt(JointReachMm));
        return new KataColumnStackReading(input, ids, null, warnings)
        {
            Columns = measured.Select(m => m.Instance).ToList(),
            Frame = frame,
            TopContinues = continues,
        };
    }

    private static KataColumnStackReading Refused(IReadOnlyList<ElementId> ids, string error) =>
        new(null, ids, error, Array.Empty<string>());

    private static MeasuredColumn? Measure(FamilyInstance column)
    {
        var solids = KataSolidReader.GetOriginalSolids(column);
        var range = KataSolidReader.VerticalRange(solids);
        var box = column.get_BoundingBox(null);
        if (range is null || box is null)
        {
            return null;
        }

        var transform = column.GetTransform();
        double? diameterFt = solids.SelectMany(s => s.Faces.Cast<Face>())
            .OfType<CylindricalFace>()
            .Where(f => Math.Abs(f.Axis.Z) > 0.99)
            .Select(f => (double?)(2 * f.get_Radius(0).GetLength()))
            .Max();
        return new MeasuredColumn(column, transform, solids, range.Value.BottomFt, range.Value.TopFt, diameterFt, IsSlanted(column, transform), box);
    }

    private static KataColumnStorey Storey(Document doc, MeasuredColumn column, Transform frame, List<string> warnings)
    {
        var points = KataSolidReader.Vertices(column.Solids).Select(p => ToFrameMm(p, frame)).ToList();
        double minX = points.Min(p => p.X), maxX = points.Max(p => p.X);
        double minY = points.Min(p => p.Y), maxY = points.Max(p => p.Y);
        string level = LevelName(doc, column.Instance);

        var section = RoundSection(column, minX, maxX, minY, maxY) ?? KataColumnSection.Rectangle(minX, maxX, minY, maxY);
        WarnIfNotPlain(column, section, level, warnings);

        return new KataColumnStorey(
            level,
            RevitUnits.FtToMm(column.BottomFt),
            RevitUnits.FtToMm(column.TopFt),
            section,
            BeamDepthMm(doc, column),
            LevelName(doc, column.Instance, BuiltInParameter.FAMILY_TOP_LEVEL_PARAM));
    }

    /// <summary>
    /// Round only when the plan extents are square and a vertical cylinder spans them: a rectangle with rounded
    /// corners, a sleeve or a void also has cylindrical faces.
    /// </summary>
    private static KataColumnSection? RoundSection(MeasuredColumn column, double minX, double maxX, double minY, double maxY)
    {
        if (column.CylinderDiameterFt is not { } diameterFt)
        {
            return null;
        }

        double diameterMm = RevitUnits.FtToMm(diameterFt);
        bool square = Math.Abs((maxX - minX) - (maxY - minY)) <= RoundToleranceMm;
        bool spans = Math.Abs((maxX - minX) - diameterMm) <= RoundToleranceMm;
        return square && spans ? KataColumnSection.Circle((minX + maxX) / 2, (minY + maxY) / 2, diameterMm) : null;
    }

    /// <summary>An L or T section, or a column with a notch, is written as its bounding rectangle: say so.</summary>
    private static void WarnIfNotPlain(MeasuredColumn column, KataColumnSection section, string level, List<string> warnings)
    {
        double heightFt = column.TopFt - column.BottomFt;
        double volumeFt3 = column.Solids.Sum(s => s.Volume);
        if (heightFt <= 0 || volumeFt3 <= 0)
        {
            return;
        }

        double areaMm2 = volumeFt3 / heightFt * Math.Pow(RevitUnits.FtToMm(1), 2);
        double expectedMm2 = section.Shape == KataColumnShape.Circle
            ? Math.PI * section.WidthMm * section.WidthMm / 4
            : section.WidthMm * section.DepthMm;
        if (Math.Abs(areaMm2 - expectedMm2) > SectionAreaTolerance * expectedMm2)
        {
            warnings.Add($"{level}: the column section is not a plain rectangle or circle; its bounding box is written.");
        }
    }

    /// <summary>
    /// Kata draws the beam soffit K below the column head, so K is the head minus the lowest soffit of the beams that
    /// frame into it (a beam set down below the level counts with its offset); 0 when no beam does.
    /// </summary>
    private static double BeamDepthMm(Document doc, MeasuredColumn column)
    {
        var soffits = Beams(doc, column)
            .OfType<FamilyInstance>()
            .Select(b => KataSolidReader.VerticalRange(KataSolidReader.GetOriginalSolids(b)))
            .Where(r => r is not null)
            .Select(r => r!.Value.BottomFt)
            .ToList();
        return soffits.Count == 0 ? 0 : RevitUnits.FtToMm(column.TopFt - soffits.Min());
    }

    /// <summary>Beams whose top lies near the column head and whose soffit is below it.</summary>
    private static IEnumerable<Element> Beams(Document doc, MeasuredColumn column)
    {
        double head = column.TopFt;
        double reach = RevitUnits.MmToFt(JointReachMm);
        return Around(doc, column, head - RevitUnits.MmToFt(BeamTopReachMm), head, BuiltInCategory.OST_StructuralFraming)
            .Where(b => b.get_BoundingBox(null) is { } box && box.Max.Z > head - RevitUnits.MmToFt(BeamTopReachMm) && box.Min.Z < head - reach);
    }

    /// <summary>The highest foundation under the column base, or the one the column starts inside.</summary>
    private static KataPileCap? PileCap(Document doc, MeasuredColumn first)
    {
        double reach = RevitUnits.MmToFt(JointReachMm);
        var cap = Around(doc, first, first.BottomFt, first.BottomFt, BuiltInCategory.OST_StructuralFoundation)
            .Select(f => f.get_BoundingBox(null))
            .Where(b => b is not null && b.Min.Z < first.BottomFt - reach)
            .OrderByDescending(b => b!.Max.Z)
            .FirstOrDefault();
        if (cap is null)
        {
            cap = Around(doc, first, first.BottomFt, first.BottomFt + reach, BuiltInCategory.OST_StructuralFoundation)
                .Select(f => f.get_BoundingBox(null))
                .Where(b => b is not null && b.Min.Z <= first.BottomFt + reach && b.Max.Z > first.BottomFt + reach)
                .OrderBy(b => b!.Min.Z)
                .FirstOrDefault();
        }

        return cap is null
            ? null
            : new KataPileCap(RevitUnits.FtToMm(cap.Min.Z), RevitUnits.FtToMm(cap.Max.Z - cap.Min.Z));
    }

    private static double? SlabThicknessMm(Document doc, MeasuredColumn first)
    {
        var thicknesses = Around(doc, first, first.TopFt, first.TopFt, BuiltInCategory.OST_Floors)
            .Select(f => f.get_Parameter(BuiltInParameter.FLOOR_ATTR_THICKNESS_PARAM)?.AsDouble() ?? 0)
            .Where(t => t > 0)
            .ToList();
        return thicknesses.Count == 0 ? null : RevitUnits.FtToMm(thicknesses.Max());
    }

    /// <summary>Width of a beam across its own axis, from its uncut geometry.</summary>
    private static double BeamWidthMm(Element beam)
    {
        if (beam is not FamilyInstance { Location: LocationCurve { Curve: var curve } } instance)
        {
            return 0;
        }

        var along = curve.GetEndPoint(1) - curve.GetEndPoint(0);
        var across = new XYZ(-along.Y, along.X, 0);
        if (across.GetLength() < 1e-9)
        {
            return 0;
        }

        across = across.Normalize();
        var offsets = KataSolidReader.Vertices(KataSolidReader.GetOriginalSolids(instance)).Select(p => p.DotProduct(across)).ToList();
        return offsets.Count == 0 ? 0 : RevitUnits.FtToMm(offsets.Max() - offsets.Min());
    }

    /// <summary>Built elements of a category whose box meets the column's plan box (grown by the joint reach) between two elevations.</summary>
    private static IEnumerable<Element> Around(Document doc, MeasuredColumn column, double fromFt, double toFt, BuiltInCategory category)
    {
        double reach = RevitUnits.MmToFt(JointReachMm);
        var box = column.Box;
        var outline = new Outline(
            new XYZ(box.Min.X - reach, box.Min.Y - reach, fromFt - reach),
            new XYZ(box.Max.X + reach, box.Max.Y + reach, toFt + reach));
        return new FilteredElementCollector(doc)
            .OfCategory(category)
            .WhereElementIsNotElementType()
            .WherePasses(new BoundingBoxIntersectsFilter(outline))
            .Where(e => e.DesignOption is null or { IsPrimary: true } && e.Id != column.Instance.Id);
    }

    private static string LevelName(Document doc, FamilyInstance column) =>
        LevelName(doc, column, BuiltInParameter.FAMILY_BASE_LEVEL_PARAM) ?? column.Name;

    private static string? LevelName(Document doc, FamilyInstance column, BuiltInParameter parameter)
    {
        var levelId = column.get_Parameter(parameter)?.AsElementId();
        return levelId is not null && doc.GetElement(levelId) is Level level ? level.Name : null;
    }

    /// <summary>C3: HP projects keep the member name in their element-name parameter ("C24"); else Mark, else the type name.</summary>
    private static object ColumnName(FamilyInstance column)
    {
        string? elementName = column.LookupParameter(ElementNameParameter)?.AsString();
        if (!string.IsNullOrWhiteSpace(elementName))
        {
            return elementName!.Trim();
        }

        string? mark = column.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString();
        return string.IsNullOrWhiteSpace(mark) ? column.Symbol.Name : mark!.Trim();
    }

    /// <summary>C4: how many identical columns, from the HP element-count parameter (integer, number or text); 1 without it.</summary>
    private static int ColumnCount(FamilyInstance column)
    {
        var parameter = column.LookupParameter(ElementCountParameter);
        int count = parameter?.StorageType switch
        {
            StorageType.Integer => parameter.AsInteger(),
            StorageType.Double => (int)Math.Round(parameter.AsDouble()),
            StorageType.String => int.TryParse(parameter.AsString()?.Trim(), out var parsed) ? parsed : 0,
            _ => 0,
        };
        return count >= 1 ? count : 1;
    }

    private static (double X, double Y) ToFrameMm(XYZ point, Transform frame)
    {
        var d = point - frame.Origin;
        return (RevitUnits.FtToMm(d.DotProduct(frame.BasisX)), RevitUnits.FtToMm(d.DotProduct(frame.BasisY)));
    }

    /// <summary>Slanted by its column style (Revit keeps the instance transform upright for some slanted columns) or by its axis.</summary>
    private static bool IsSlanted(FamilyInstance column, Transform transform)
    {
        int style = column.get_Parameter(BuiltInParameter.SLANTED_COLUMN_TYPE_PARAM)?.AsInteger() ?? 0;
        return style != 0 || Math.Abs(transform.BasisZ.Z) < 0.999;
    }

    private static bool SameAxes(Transform a, Transform b)
    {
        double angle = Math.Abs(a.BasisX.AngleTo(b.BasisX) * 180 / Math.PI) % 90;
        return Math.Min(angle, 90 - angle) <= AxisToleranceDeg;
    }

    private static bool OverlapInPlan(IReadOnlyList<KataColumnStorey> storeys) =>
        storeys.Zip(storeys.Skip(1), (lower, upper) =>
                upper.Section.MinXMm < lower.Section.MaxXMm && lower.Section.MinXMm < upper.Section.MaxXMm
                && upper.Section.MinYMm < lower.Section.MaxYMm && lower.Section.MinYMm < upper.Section.MaxYMm)
            .All(overlaps => overlaps);

    private sealed record MeasuredColumn(
        FamilyInstance Instance,
        Transform Transform,
        IReadOnlyList<Solid> Solids,
        double BottomFt,
        double TopFt,
        double? CylinderDiameterFt,
        bool IsSlanted,
        BoundingBoxXYZ Box)
    {
        /// <summary>Has a vertical cylinder; whether the section is really round is decided against its extents.</summary>
        public bool IsRound => CylinderDiameterFt is not null;
    }
}
