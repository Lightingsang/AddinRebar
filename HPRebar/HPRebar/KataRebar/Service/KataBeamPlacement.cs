using System;
using System.Linq;
using Autodesk.Revit.DB;
using HPRebar.BeamRebar.Service;
using HPRebar.KataRebar.Model;

namespace HPRebar.KataRebar.Service;

/// <summary>
/// Places the layout's local frame in the model: x = 0 at the outer face of the first support in sheet
/// order, y = 0 on the centre of the concrete section (not the location line, which a family may offset),
/// z = 0 on the top of the beam. When the sheet lists the run from Revit's far end the local X and Y both
/// run against the Revit axis so Z stays up.
/// </summary>
public sealed class KataBeamPlacement
{
    private readonly KataBeamMatchResult _match;
    private readonly double _originStation;
    private readonly int _direction;

    public KataBeamPlacement(KataBeamMatchResult match, bool reversed)
    {
        _match = match ?? throw new ArgumentNullException(nameof(match));
        var run = match.Run ?? throw new ArgumentException("The match has no measured run.", nameof(match));
        if (match.SegmentExtents.Count == 0) throw new ArgumentException("The match has no segments.", nameof(match));

        var first = run.Pieces[0];
        _direction = reversed ? -1 : 1;
        _originStation = reversed ? match.SegmentExtents[match.SegmentExtents.Count - 1].End : match.SegmentExtents[0].Start;

        // Local z = 0 is the level the tops are measured from (row 19 and the measured tops are offsets from it), not
        // the top of whichever element Revit lists first.
        double datumFt = first.TopFt - RevitUnits.MmToFt(first.ZOffsetMm);
        var origin = run.Frame.Point(_originStation, first.CenterOffsetMm, datumFt);
        var axis = reversed ? run.Frame.Axis.Negate() : run.Frame.Axis;
        var across = reversed ? run.Frame.Transverse.Negate() : run.Frame.Transverse;
        Mapper = new PointMapper(origin, axis, across, XYZ.BasisZ);
    }

    public PointMapper Mapper { get; }

    /// <summary>The framing element under a local station: the piece containing it, else the nearest one.</summary>
    public FamilyInstance HostAt(double localX)
    {
        double station = _originStation + _direction * localX;
        return _match.Run!.Pieces
            .OrderBy(p => p.Stations.Contains(station) ? 0.0 : Math.Min(Math.Abs(p.Stations.Start - station), Math.Abs(p.Stations.End - station)))
            .First()
            .Element;
    }
}
