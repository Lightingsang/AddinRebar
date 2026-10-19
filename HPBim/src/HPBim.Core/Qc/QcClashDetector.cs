namespace HPBim.Core.Qc;

using System;
using System.Collections.Generic;
using HPBim.Core.Geometry;
using HPBim.Core.Models;

/// <summary>
/// Detects physical structural and architectural clashes before Revit execution.
/// </summary>
public sealed class QcClashDetector
{
    public IReadOnlyList<QcClash> DetectColumnOpeningClashes(
        IReadOnlyList<ColumnDef> columns, IReadOnlyList<WallDef> walls)
    {
        var clashes = new List<QcClash>();
        if (columns == null || walls == null || columns.Count == 0 || walls.Count == 0)
        {
            return clashes;
        }

        var colIndex = BuildColumnSpatialIndex(columns);

        foreach (var wall in walls)
        {
            if (wall.Openings == null || wall.Openings.Count == 0 || wall.Length <= 1e-4)
            {
                continue;
            }

            CheckWallOpeningsClashes(wall, colIndex, clashes);
        }

        return clashes;
    }

    private static SpatialGridIndex<ColumnDef> BuildColumnSpatialIndex(IReadOnlyList<ColumnDef> columns)
    {
        var colIndex = new SpatialGridIndex<ColumnDef>();
        foreach (var col in columns)
        {
            double halfW = col.WidthMm / 2.0;
            double halfD = col.DepthMm / 2.0;
            var box = new BoundingBox2D(
                new Point2D(col.CenterLocation.X - halfW, col.CenterLocation.Y - halfD),
                new Point2D(col.CenterLocation.X + halfW, col.CenterLocation.Y + halfD));
            colIndex.Insert(col, box);
        }
        return colIndex;
    }

    private static void CheckWallOpeningsClashes(
        WallDef wall, SpatialGridIndex<ColumnDef> colIndex, List<QcClash> clashes)
    {
        var dir = (wall.EndPoint - wall.StartPoint).Normalize();
        foreach (var op in wall.Openings)
        {
            var opCenter = wall.StartPoint + dir * (op.DistanceAlongWallMm + op.WidthMm / 2.0);
            double halfW = op.WidthMm / 2.0;
            var opBox = new BoundingBox2D(
                new Point2D(opCenter.X - halfW, opCenter.Y - halfW),
                new Point2D(opCenter.X + halfW, opCenter.Y + halfW));

            var nearbyCols = colIndex.Query(opBox);
            foreach (var col in nearbyCols)
            {
                if (!string.Equals(col.BaseLevel ?? string.Empty, wall.BaseLevel ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                double dist = opCenter.DistanceTo(col.CenterLocation);
                double clashLimit = halfW + (col.WidthMm / 2.0);
                if (dist < clashLimit)
                {
                    double penetration = clashLimit - dist;
                    clashes.Add(new QcClash(
                        col.Id, op.Id, "ColumnVsOpening",
                        $"Cột {col.Id} bị lỗ mở {op.Id} (loại {op.Kind}) đè lên phạm vi {penetration:F1}mm!",
                        opCenter, penetration));
                }
            }
        }
    }

    public IReadOnlyList<QcClash> DetectBeamSlabClashes(
        IReadOnlyList<BeamDef> beams, IReadOnlyList<SlabDef> slabs)
    {
        var clashes = new List<QcClash>();
        if (beams == null || slabs == null || beams.Count == 0 || slabs.Count == 0)
        {
            return clashes;
        }

        foreach (var beam in beams)
        {
            if (beam.IsTieBeam)
            {
                continue;
            }

            var beamMid = new Point2D(
                (beam.StartPoint.X + beam.EndPoint.X) / 2.0,
                (beam.StartPoint.Y + beam.EndPoint.Y) / 2.0);

            foreach (var slab in slabs)
            {
                if (!string.Equals(beam.Level ?? string.Empty, slab.Level ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (slab.Boundary != null && slab.Boundary.Contains(beamMid, includeBoundary: false))
                {
                    // Secondary beam passing through undivided slab interior
                    clashes.Add(new QcClash(
                        beam.Id, slab.Id, "SecondaryBeamSlicesUndividedSlab",
                        $"Dầm phụ {beam.Id} chạy xuyên qua giữa ô sàn {slab.Id} mà chưa được chia tách ô sàn!",
                        beamMid, beam.WidthMm));
                }
            }
        }

        return clashes;
    }

    public IReadOnlyList<QcClash> DetectAll(BimModel model)
    {
        var allClashes = new List<QcClash>();
        if (model == null) return allClashes;
        allClashes.AddRange(DetectColumnOpeningClashes(model.Columns, model.Walls));
        allClashes.AddRange(DetectBeamSlabClashes(model.Beams, model.Slabs));
        return allClashes;
    }
}
