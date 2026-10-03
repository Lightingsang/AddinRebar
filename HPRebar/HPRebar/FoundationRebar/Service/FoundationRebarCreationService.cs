using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HPRebar.Core.FoundationRebar.Models;
using HPRebar.FoundationRebar.Model;
using HPRebar.Shared.Revit;
using Serilog;

namespace HPRebar.FoundationRebar.Service;

/// <summary>
/// Service creating native Revit <see cref="Rebar"/> elements from pure domain <see cref="FoundationMeshResult"/>.
/// Bars are created hook-less through <see cref="HPRebar.Shared.Revit.RebarCurveFactory.CreateWithoutHooks"/>.
/// </summary>
public static class FoundationRebarCreationService
{
    public static IReadOnlyList<Rebar> CreateRebars(
        Document document,
        Floor hostFloor,
        FoundationMeshResult mesh,
        FoundationSession session,
        Action<int>? onBarCreated = null)
    {
        if (document is null) throw new ArgumentNullException(nameof(document));
        if (hostFloor is null) throw new ArgumentNullException(nameof(hostFloor));
        if (mesh is null) throw new ArgumentNullException(nameof(mesh));
        if (session is null) throw new ArgumentNullException(nameof(session));

        var snapshot = session.Snapshot;
        var normX = new XYZ(snapshot.LocalY.X, snapshot.LocalY.Y, snapshot.LocalY.Z).Normalize();
        var normY = new XYZ(snapshot.LocalX.X, snapshot.LocalX.Y, snapshot.LocalX.Z).Normalize();

        var createdRebars = new List<Rebar>(mesh.Bars.Count);

        for (int i = 0; i < mesh.Bars.Count; i++)
        {
            var bar = mesh.Bars[i];
            var barType = session.FindBarTypeByDiameter(bar.Diameter);

            if (barType is null)
            {
                throw new InvalidOperationException(
                    $"No RebarBarType found for diameter {bar.Diameter:F1} mm in active document.");
            }

            // Normal vector to the plane of the bar
            // BottomX and TopX run along X -> plane normal is Local Y
            // BottomY and TopY run along Y -> plane normal is Local X
            bool isDirX = bar.Layer is FoundationBarLayer.BottomX or FoundationBarLayer.TopX;
            XYZ planeNormal = isDirX ? normX : normY;

            var curves = BuildRevitCurves(bar.Polyline);

            var rebar = RebarCurveFactory.CreateWithoutHooks(document, RebarStyle.Standard, barType, hostFloor, planeNormal, curves);

            var partitionParam = rebar.get_Parameter(BuiltInParameter.NUMBER_PARTITION_PARAM);
            if (partitionParam != null && !partitionParam.IsReadOnly)
            {
                partitionParam.Set("Foundation");
            }

            // Multi-version: ElementId
#if REVIT2024_OR_GREATER
            long barId = rebar.Id.Value;
#else
            int barId = rebar.Id.IntegerValue;
#endif

            createdRebars.Add(rebar);
            onBarCreated?.Invoke(i + 1);
        }

        Log.Information("Foundation Rebar created {Count} rebar elements.", createdRebars.Count);
        return createdRebars;
    }

    /// <summary>
    /// Converts a pure <see cref="Polyline3"/> (coordinates in millimetres) to Revit internal <see cref="Curve"/> list (feet).
    /// </summary>
    public static IList<Curve> BuildRevitCurves(Polyline3 polyline)
    {
        var simplified = polyline.Simplify(1.0); // 1.0 mm minimum segment
        if (simplified.Points.Count < 2)
            throw new InvalidOperationException("Polyline must have at least 2 points to form a curve.");

        var curves = new List<Curve>(simplified.Points.Count - 1);
        for (int i = 1; i < simplified.Points.Count; i++)
        {
            var p0 = simplified.Points[i - 1];
            var p1 = simplified.Points[i];

            var xyz0 = new XYZ(RevitUnits.MmToFt(p0.X), RevitUnits.MmToFt(p0.Y), RevitUnits.MmToFt(p0.Z));
            var xyz1 = new XYZ(RevitUnits.MmToFt(p1.X), RevitUnits.MmToFt(p1.Y), RevitUnits.MmToFt(p1.Z));

            if (xyz0.DistanceTo(xyz1) > 0.002) // exceed Revit internal short curve tolerance (~0.00256 ft)
            {
                curves.Add(Line.CreateBound(xyz0, xyz1));
            }
        }

        if (curves.Count == 0)
            throw new InvalidOperationException("Polyline produced no valid line segments above Revit tolerance.");

        return curves;
    }
}
