using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HPRebar.KataRebar.Service;
using Serilog;

namespace HPRebar.KataExport.Service;

/// <summary>
/// One round hoop of Kata Column Rebar. Revit refuses a closed circle of two hooked arcs as free curves, so the hoop
/// is laid from the model's lapped-circle tie shape (e.g. M_T3) scaled to its out-to-out diameter; without such a shape,
/// or when Revit refuses it, the two arcs are tried with hooks and then without (the caller is told).
/// </summary>
internal sealed class KataColumnRoundHoopFactory
{
    private readonly Document _doc;
    private readonly RebarShape? _shape;

    public KataColumnRoundHoopFactory(Document doc)
    {
        _doc = doc;
        _shape = new FilteredElementCollector(doc).OfClass(typeof(RebarShape)).Cast<RebarShape>()
            .Where(s => s.RebarStyle == RebarStyle.StirrupTie && s.GetRebarShapeDefinition() is RebarShapeDefinitionByArc { Type: RebarShapeDefinitionByArcType.LappedCircle })
            .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    /// <summary>True when the last hoop had to be drawn as two arcs without hooks.</summary>
    public bool LastWithoutHooks { get; private set; }

    /// <param name="centre">Hoop centre at its elevation (model, ft).</param>
    /// <param name="radiusFt">Centreline radius.</param>
    /// <param name="axisX">Plan direction of the column frame's b axis.</param>
    /// <param name="axisY">Plan direction of the column frame's h axis.</param>
    public Rebar Create(RebarBarType barType, KataHookChoice? hook, Element host, XYZ centre, double radiusFt, XYZ axisX, XYZ axisY)
    {
        LastWithoutHooks = false;
        if (_shape is not null && FromShape(barType, host, centre, radiusFt, axisX, axisY) is { } shaped)
        {
            return shaped;
        }

        var arcs = new List<Curve>
        {
            Arc.Create(centre, radiusFt, 0, Math.PI, axisX, axisY),
            Arc.Create(centre, radiusFt, Math.PI, 2 * Math.PI, axisX, axisY),
        };
        if (hook is not null && Try(() => KataRebarCurveFactory.CreateHooked(_doc, hook.Style, barType, hook.Hook, host, XYZ.BasisZ, arcs, centre)) is { } hooked)
        {
            return hooked;
        }

        LastWithoutHooks = true;
        return KataRebarCurveFactory.Create(_doc, RebarStyle.StirrupTie, barType, host, XYZ.BasisZ, arcs);
    }

    /// <summary>The shape scaled to the hoop's out-to-out box; null (rolled back) when Revit refuses or misplaces it.</summary>
    private Rebar? FromShape(RebarBarType barType, Element host, XYZ centre, double radiusFt, XYZ axisX, XYZ axisY)
    {
        double outer = 2 * radiusFt + barType.BarNominalDiameter;
        var origin = centre - axisX * (outer / 2) - axisY * (outer / 2);
        using var sub = new SubTransaction(_doc);
        sub.Start();
        try
        {
            var rebar = Rebar.CreateFromRebarShape(_doc, _shape!, barType, host, origin, axisX, axisY)
                        ?? throw new InvalidOperationException("no rebar");
            rebar.GetShapeDrivenAccessor().ScaleToBox(origin, axisX * outer, axisY * outer);
            _doc.Regenerate();
            CheckCentre(rebar, centre, radiusFt);
            sub.Commit();
            return rebar;
        }
        catch (Exception ex) when (ex is Autodesk.Revit.Exceptions.ApplicationException or InvalidOperationException or ArgumentException)
        {
            sub.RollBack();
            Log.Warning("Kata Column Rebar: round hoop from shape {Shape} refused ({Reason}); trying arcs", _shape!.Name, ex.Message);
            return null;
        }
    }

    /// <summary>ScaleToBox compromises instead of failing: the hoop must sit on the planned centre and radius (±5 mm).</summary>
    private static void CheckCentre(Rebar rebar, XYZ centre, double radiusFt)
    {
        var points = rebar.GetCenterlineCurves(false, true, true, MultiplanarOption.IncludeOnlyPlanarCurves, 0)
            .OfType<Arc>()
            .ToList();
        double tolerance = RevitUnits.MmToFt(5);
        var main = points.OrderByDescending(a => a.Length).FirstOrDefault()
                   ?? throw new InvalidOperationException("the shape has no arc");
        var flat = new XYZ(main.Center.X - centre.X, main.Center.Y - centre.Y, 0);
        if (flat.GetLength() > tolerance || Math.Abs(main.Radius - radiusFt) > tolerance || Math.Abs(main.Center.Z - centre.Z) > tolerance)
        {
            throw new InvalidOperationException(
                $"hoop at {RevitUnits.FtToMm(flat.GetLength()):0} mm from the centre, radius {RevitUnits.FtToMm(main.Radius):0} mm instead of {RevitUnits.FtToMm(radiusFt):0}");
        }
    }

    private static Rebar? Try(Func<Rebar> create)
    {
        try
        {
            return create();
        }
        catch (Exception ex) when (ex is Autodesk.Revit.Exceptions.ApplicationException or InvalidOperationException or ArgumentException)
        {
            Log.Information("Kata Column Rebar: hooked round hoop refused ({Reason})", ex.Message);
            return null;
        }
    }
}
