using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HPRebar.Core.FoundationRebar.Models;
using HPRebar.FoundationRebar.Service;

namespace HPRebar.FoundationRebar.Model;

/// <summary>
/// Session state bridging Revit floor element, extracted geometry snapshot,
/// available RebarBarType elements, and active user specification.
/// </summary>
public sealed class FoundationSession
{
    public Document Document { get; }
    public Floor Floor { get; }
    public FoundationGeometrySnapshot Snapshot { get; set; }
    public IReadOnlyList<RebarBarType> AvailableBarTypes { get; }
    public FoundationRebarSpec Spec { get; set; }

    public FoundationSession(
        Document document,
        Floor floor,
        FoundationGeometrySnapshot snapshot,
        IReadOnlyList<RebarBarType> availableBarTypes,
        FoundationRebarSpec spec)
    {
        Document = document ?? throw new ArgumentNullException(nameof(document));
        Floor = floor ?? throw new ArgumentNullException(nameof(floor));
        Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        AvailableBarTypes = availableBarTypes ?? Array.Empty<RebarBarType>();
        Spec = spec ?? new FoundationRebarSpec();
    }

    /// <summary>
    /// Resolves the best matching RebarBarType from available types for a given target diameter in mm.
    /// </summary>
    public RebarBarType? FindBarTypeByDiameter(double diameterMm)
    {
        if (AvailableBarTypes.Count == 0) return null;

        RebarBarType? best = null;
        double bestDiff = double.MaxValue;

        foreach (var bt in AvailableBarTypes)
        {
            double dMm = RevitUnits.FtToMm(bt.BarNominalDiameter);
            double diff = Math.Abs(dMm - diameterMm);
            if (diff < bestDiff)
            {
                bestDiff = diff;
                best = bt;
            }
        }

        return best ?? AvailableBarTypes[0];
    }
}
