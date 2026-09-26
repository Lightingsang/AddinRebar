using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using HPRebar.Core.KataExport.Models;
using HPRebar.KataExport.Model;

namespace HPRebar.KataExport.Service;

/// <summary>
/// Turns the picked structural framing into a straight run: checks every element is a straight line on
/// one axis, orders them along it and measures each section from its type parameters or its geometry.
/// Different depths, widths and top levels along the run are allowed (Kata draws the steps).
/// </summary>
public static class KataRunReader
{
    private const double MaxAngleDegrees = 1.0;
    private const double MaxLateralOffsetMm = 10.0;
    private const double MaxSlopeMm = 10.0;

    /// <summary>Type parameters holding the section, as named by the office's concrete beam families.</summary>
    public const string WidthParameter = "b";
    public const string HeightParameter = "h";

    public static KataRunGeometry Read(Document doc, IReadOnlyList<Element> beams)
    {
        if (beams is null || beams.Count == 0) throw new ArgumentException("No beam was selected.", nameof(beams));

        var lines = beams.Select(b => (Element: AsFraming(b), Line: LocationLine(b))).ToList();
        var frame = KataAxisFrame.FromLine(lines[0].Line);

        foreach (var (element, line) in lines)
        {
            if (!frame.IsParallel(line.Direction, MaxAngleDegrees))
                throw new InvalidOperationException($"Beam {element.Id} is not in line with the first beam; a Kata run must be straight.");

            double offset = Math.Max(Math.Abs(frame.Offset(line.GetEndPoint(0))), Math.Abs(frame.Offset(line.GetEndPoint(1))));
            if (offset > MaxLateralOffsetMm)
                throw new InvalidOperationException($"Beam {element.Id} is {offset:0} mm off the run axis; a Kata run must be straight.");

            if (RevitUnits.FtToMm(Math.Abs(line.GetEndPoint(1).Z - line.GetEndPoint(0).Z)) > MaxSlopeMm)
                throw new InvalidOperationException($"Beam {element.Id} is sloped; Kata runs must be horizontal.");
        }

        var pieces = lines
            .Select(item => Measure(doc, item.Element, item.Line, frame))
            .OrderBy(p => p.Stations.Start)
            .ToList();

        // One sheet describes one beam on one level: B10 is that level and every step is measured from it.
        if (pieces.Select(p => p.ReferenceLevel?.Id).Distinct().Count() > 1)
            throw new InvalidOperationException("The selected beams are hosted on different levels; pick one beam run per level.");

        return new KataRunGeometry
        {
            Frame = frame,
            Pieces = pieces,
            Extent = new Interval1D(pieces.Min(p => p.Stations.Start), pieces.Max(p => p.Stations.End)),
            CenterOffsetMm = pieces[0].CenterOffsetMm
        };
    }

    private static FamilyInstance AsFraming(Element element) =>
        element is FamilyInstance instance && element.Category?.BuiltInCategory == BuiltInCategory.OST_StructuralFraming
            ? instance
            : throw new InvalidOperationException($"Element {element.Id} is not a structural framing instance.");

    private static Line LocationLine(Element element) =>
        (element.Location as LocationCurve)?.Curve as Line
        ?? throw new InvalidOperationException($"Beam {element.Id} is not a straight line; curved beams are not supported.");

    private static KataBeamGeometry Measure(Document doc, FamilyInstance beam, Line line, KataAxisFrame frame)
    {
        var solids = KataSolidReader.GetOriginalSolids(beam);
        var points = KataSolidReader.Vertices(solids).ToList();

        double centerOffset = frame.Offset(line.Evaluate(0.5, true));
        double measuredWidth = 0.0;
        double bottom = Math.Min(line.GetEndPoint(0).Z, line.GetEndPoint(1).Z);
        double top = Math.Max(line.GetEndPoint(0).Z, line.GetEndPoint(1).Z);
        if (points.Count > 0)
        {
            var offsets = points.Select(frame.Offset).ToList();
            centerOffset = (offsets.Min() + offsets.Max()) / 2.0;
            measuredWidth = offsets.Max() - offsets.Min();
            bottom = points.Min(p => p.Z);
            top = points.Max(p => p.Z);
        }

        double width = TypeLength(beam, WidthParameter) ?? measuredWidth;
        double height = TypeLength(beam, HeightParameter) ?? RevitUnits.FtToMm(top - bottom);
        if (width <= 0 || height <= 0)
            throw new InvalidOperationException($"Beam {beam.Id} has no section size: no '{WidthParameter}'/'{HeightParameter}' type parameter and no solid.");

        var levelId = beam.get_Parameter(BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM)?.AsElementId();
        var level = levelId is null ? null : doc.GetElement(levelId) as Level;

        return new KataBeamGeometry
        {
            Element = beam,
            Stations = frame.Stations(line),
            CenterOffsetMm = centerOffset,
            BottomFt = bottom,
            TopFt = top,
            WidthMm = width,
            HeightMm = height,
            ZOffsetMm = RevitUnits.FtToMm(top - LevelZ(beam, line, level)),
            ReferenceLevel = level
        };
    }

    /// <summary>
    /// Elevation of the reference level in model coordinates, taken from the beam's own location line minus
    /// its start level offset so it is in the same coordinates as the geometry whatever the project base point.
    /// The top step written to Kata is then the real top of the beam above that level — z offset, start/end
    /// offsets and z justification included — not the "z Offset Value" parameter alone.
    /// </summary>
    private static double LevelZ(FamilyInstance beam, Line line, Level? level)
    {
        var startOffset = beam.get_Parameter(BuiltInParameter.STRUCTURAL_BEAM_END0_ELEVATION);
        if (startOffset is { HasValue: true, StorageType: StorageType.Double })
            return line.GetEndPoint(0).Z - startOffset.AsDouble();

        return level?.Elevation ?? line.GetEndPoint(0).Z;
    }

    /// <summary>A length type (or instance) parameter in mm, or null when the family has no such number.</summary>
    internal static double? TypeLength(FamilyInstance beam, string name)
    {
        var parameter = beam.Symbol?.LookupParameter(name) ?? beam.LookupParameter(name);
        return parameter is { StorageType: StorageType.Double, HasValue: true }
            ? RevitUnits.FtToMm(parameter.AsDouble())
            : null;
    }
}
