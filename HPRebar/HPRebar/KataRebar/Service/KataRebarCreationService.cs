using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HPRebar.BeamRebar.Service;
using HPRebar.Core.BeamRebar.Models;
using HPRebar.Core.KataRebar.Models;
using HPRebar.KataExport.Service;
using RevitUnits = HPRebar.KataExport.Service.RevitUnits;

namespace HPRebar.KataRebar.Service;

/// <summary>
/// Creates native Revit 3D Rebar elements inside host structural framing elements.
/// Uses Rebar.CreateFromCurves for longitudinal/additional/side bars and
/// Rebar.CreateFromRebarShape (with fallback to CreateFromCurves) for stirrup distribution sets.
/// Tags every created bar with Partition = beamName and Schedule Mark = barMark.
/// </summary>
public static class KataRebarCreationService
{
    /// <summary>
    /// Creates continuous top and bottom longitudinal reinforcement bars.
    /// </summary>
    public static IReadOnlyList<Rebar> CreateMainBars(
        Document doc,
        IReadOnlyList<FamilyInstance> hostBeams,
        PointMapper mapper,
        KataBeamRebarSpec spec,
        KataRebarLayoutResult layout,
        IReadOnlyDictionary<double, RebarBarType> resolvedBarTypes)
    {
        var created = new List<Rebar>();
        var host = hostBeams[0];

        foreach (var bar in layout.MainTopBars)
        {
            var barType = GetBarType(resolvedBarTypes, bar.Diameter);
            var curves = BuildCurves(bar.Polyline, mapper);
            var rebar = CreateFromCurvesInternal(doc, RebarStyle.Standard, barType, host, mapper.AxisY, curves);
            StampRebar(rebar, spec.BeamName, bar.BarMark);
            created.Add(rebar);
        }

        foreach (var bar in layout.MainBottomBars)
        {
            var barType = GetBarType(resolvedBarTypes, bar.Diameter);
            var curves = BuildCurves(bar.Polyline, mapper);
            var rebar = CreateFromCurvesInternal(doc, RebarStyle.Standard, barType, host, mapper.AxisY, curves);
            StampRebar(rebar, spec.BeamName, bar.BarMark);
            created.Add(rebar);
        }

        return created;
    }

    /// <summary>
    /// Creates additional support top cutoffs and midspan bottom cutoffs.
    /// </summary>
    public static IReadOnlyList<Rebar> CreateAdditionalBars(
        Document doc,
        IReadOnlyList<FamilyInstance> hostBeams,
        PointMapper mapper,
        KataBeamRebarSpec spec,
        KataRebarLayoutResult layout,
        IReadOnlyDictionary<double, RebarBarType> resolvedBarTypes)
    {
        var created = new List<Rebar>();

        foreach (var bar in layout.ExtraTopBars)
        {
            int hostIdx = Math.Clamp(bar.HostSpanIndex, 0, hostBeams.Count - 1);
            var host = hostBeams[hostIdx];
            var barType = GetBarType(resolvedBarTypes, bar.Diameter);
            var curves = BuildCurves(bar.Polyline, mapper);
            var rebar = CreateFromCurvesInternal(doc, RebarStyle.Standard, barType, host, mapper.AxisY, curves);
            StampRebar(rebar, spec.BeamName, bar.BarMark);
            created.Add(rebar);
        }

        foreach (var bar in layout.ExtraBottomBars)
        {
            int hostIdx = Math.Clamp(bar.HostSpanIndex, 0, hostBeams.Count - 1);
            var host = hostBeams[hostIdx];
            var barType = GetBarType(resolvedBarTypes, bar.Diameter);
            var curves = BuildCurves(bar.Polyline, mapper);
            var rebar = CreateFromCurvesInternal(doc, RebarStyle.Standard, barType, host, mapper.AxisY, curves);
            StampRebar(rebar, spec.BeamName, bar.BarMark);
            created.Add(rebar);
        }

        return created;
    }

    /// <summary>
    /// Creates longitudinal web skin / side bars and cross-ties.
    /// </summary>
    public static IReadOnlyList<Rebar> CreateSideBars(
        Document doc,
        IReadOnlyList<FamilyInstance> hostBeams,
        PointMapper mapper,
        KataBeamRebarSpec spec,
        KataRebarLayoutResult layout,
        IReadOnlyDictionary<double, RebarBarType> resolvedBarTypes)
    {
        var created = new List<Rebar>();
        var host = hostBeams[0];

        foreach (var bar in layout.SideBars)
        {
            var barType = GetBarType(resolvedBarTypes, bar.Diameter);
            var curves = BuildCurves(bar.Polyline, mapper);
            var rebar = CreateFromCurvesInternal(doc, RebarStyle.Standard, barType, host, mapper.AxisY, curves);
            StampRebar(rebar, spec.BeamName, bar.BarMark);
            created.Add(rebar);
        }

        return created;
    }

    /// <summary>
    /// Creates stirrups using ShapeDriven Rebar sets (when standard rectangular shape exists),
    /// or falls back to individual curves for custom shapes and templates without standard shape families.
    /// </summary>
    public static IReadOnlyList<Rebar> CreateStirrups(
        Document doc,
        IReadOnlyList<FamilyInstance> hostBeams,
        PointMapper mapper,
        KataBeamRebarSpec spec,
        KataRebarLayoutResult layout,
        IReadOnlyDictionary<double, RebarBarType> resolvedBarTypes,
        KataRebarShapeResolver? shapes)
    {
        var created = new List<Rebar>();
        var mainShape = shapes?.MainStirrup();

        if (mainShape is not null && layout.StirrupZones.Count > 0)
        {
            bool usedShapeDriven = true;
            try
            {
                foreach (var zone in layout.StirrupZones)
                {
                    if (zone.Count <= 0 || zone.OutToOutWidth <= 0 || zone.OutToOutHeight <= 0)
                        continue;

                    int hostIdx = Math.Clamp(zone.SpanIndex, 0, hostBeams.Count - 1);
                    var host = hostBeams[hostIdx];
                    var barType = GetBarType(resolvedBarTypes, spec.GlobalStirrup.Diameter);

                    if (zone.StirrupType == KataStirrupShapeType.ClosedHoop)
                    {
                        var localOrigin = new Point3(
                            zone.StartStationX,
                            -(spec.Width / 2.0) + spec.CoverStirrup,
                            -spec.Height + spec.CoverStirrup);

                        XYZ originXyz = mapper.ToXyz(localOrigin);
                        XYZ xVec = mapper.AxisY; // Transverse width vector
                        XYZ yVec = XYZ.BasisZ;   // Vertical height vector

                        var rebar = Rebar.CreateFromRebarShape(doc, mainShape, barType, host, originXyz, xVec, yVec);
                        var accessor = rebar.GetShapeDrivenAccessor();
                        accessor.ScaleToBox(
                            originXyz,
                            xVec * RevitUnits.MmToFt(zone.OutToOutWidth),
                            yVec * RevitUnits.MmToFt(zone.OutToOutHeight));

                        if (zone.Count <= 1)
                        {
                            accessor.SetLayoutAsSingle();
                        }
                        else
                        {
                            accessor.SetLayoutAsNumberWithSpacing(
                                Math.Clamp(zone.Count, 2, 1002),
                                RevitUnits.MmToFt(zone.Spacing),
                                true,
                                true,
                                true);
                        }

                        StampRebar(rebar, spec.BeamName, zone.BarMark);
                        created.Add(rebar);
                    }
                    else
                    {
                        // Custom stirrup (Cap U or Cross tie C): create from curves for this zone
                        var zoneStirrups = layout.IndividualStirrups
                            .Where(s => s.HostSpanIndex == zone.SpanIndex && s.Role != KataBarRole.StirrupClosed);

                        foreach (var s in zoneStirrups)
                        {
                            var sBarType = GetBarType(resolvedBarTypes, s.Diameter);
                            var curves = BuildCurves(s.Polyline, mapper);
                            var rebar = CreateFromCurvesInternal(doc, RebarStyle.StirrupTie, sBarType, host, mapper.AxisX, curves);
                            StampRebar(rebar, spec.BeamName, s.BarMark);
                            created.Add(rebar);
                        }
                    }
                }
            }
            catch
            {
                // In case RebarShape Driven fails due to unexpected family parameters, roll back and use curves fallback
                usedShapeDriven = false;
                created.Clear();
            }

            if (usedShapeDriven && created.Count > 0)
                return created;
        }

        // Fallback: create individual stirrup curves
        foreach (var s in layout.IndividualStirrups)
        {
            int hostIdx = Math.Clamp(s.HostSpanIndex, 0, hostBeams.Count - 1);
            var host = hostBeams[hostIdx];
            var barType = GetBarType(resolvedBarTypes, s.Diameter);
            var curves = BuildCurves(s.Polyline, mapper);
            var rebar = CreateFromCurvesInternal(doc, RebarStyle.StirrupTie, barType, host, mapper.AxisX, curves);
            StampRebar(rebar, spec.BeamName, s.BarMark);
            created.Add(rebar);
        }

        return created;
    }

    /// <summary>
    /// Builds Revit Curves from local space Polyline3 using the PointMapper.
    /// Enforces Revit's minimum curve tolerance via Polyline3.Simplify(1.0).
    /// </summary>
    public static IList<Curve> BuildCurves(Polyline3 polyline, PointMapper mapper)
    {
        var simplified = polyline.Simplify(1.0);
        if (simplified.Points.Count < 2)
            throw new InvalidOperationException("Polyline collapsed to fewer than 2 points.");

        var curves = new List<Curve>(simplified.Points.Count - 1);
        for (int i = 1; i < simplified.Points.Count; i++)
        {
            var p0 = mapper.ToXyz(simplified.Points[i - 1]);
            var p1 = mapper.ToXyz(simplified.Points[i]);
            if (p0.DistanceTo(p1) > 2.0e-3)
            {
                curves.Add(Line.CreateBound(p0, p1));
            }
        }

        if (curves.Count == 0)
            throw new InvalidOperationException("All curve segments were shorter than tolerance.");

        return curves;
    }

    private static Rebar CreateFromCurvesInternal(
        Document doc,
        RebarStyle style,
        RebarBarType barType,
        Element host,
        XYZ norm,
        IList<Curve> curves)
    {
#pragma warning disable CS0618 // Multi-version: Rebar.CreateFromCurves / RebarHookOrientation deprecated in Revit 2026, required for Revit 2023-2025 compatibility
        return Rebar.CreateFromCurves(
            doc,
            style,
            barType,
            startHook: null,
            endHook: null,
            host: host,
            norm: norm,
            curves: curves,
            startHookOrient: RebarHookOrientation.Right,
            endHookOrient: RebarHookOrientation.Right,
            useExistingShapeIfPossible: true,
            createNewShape: true);
#pragma warning restore CS0618
    }

    private static void StampRebar(Rebar rebar, string beamName, string barMark = "")
    {
        var partitionParam = rebar.get_Parameter(BuiltInParameter.NUMBER_PARTITION_PARAM);
        if (partitionParam != null && !partitionParam.IsReadOnly)
        {
            partitionParam.Set(beamName.Trim());
        }

        if (!string.IsNullOrEmpty(barMark))
        {
            var markParam = rebar.get_Parameter(BuiltInParameter.REBAR_ELEM_SCHEDULE_MARK);
            if (markParam != null && !markParam.IsReadOnly)
            {
                markParam.Set(barMark);
            }
        }
    }

    private static RebarBarType GetBarType(
        IReadOnlyDictionary<double, RebarBarType> resolvedBarTypes,
        double diameterMm)
    {
        if (resolvedBarTypes.TryGetValue(diameterMm, out var barType))
            return barType;

        var closest = resolvedBarTypes
            .OrderBy(kv => Math.Abs(kv.Key - diameterMm))
            .Select(kv => kv.Value)
            .FirstOrDefault();

        if (closest is not null) return closest;

        throw new InvalidOperationException($"Không tìm thấy kiểu thép (RebarBarType) cho đường kính Ø{diameterMm:0.#} mm.");
    }
}
