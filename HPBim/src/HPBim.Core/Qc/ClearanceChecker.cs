namespace HPBim.Core.Qc;

using System;
using System.Collections.Generic;
using HPBim.Core.Geometry;
using HPBim.Core.Models;
using HPBim.Core.Tcvn;

/// <summary>
/// Checks architectural and functional clearances (door clearance, opening-to-column, wall thicknesses).
/// </summary>
public sealed class ClearanceChecker
{
    public const double MinOpeningToColumnClearanceMm = 100.0;

    public IReadOnlyList<QcViolation> CheckOpeningToColumnClearances(
        IReadOnlyList<WallDef> walls, IReadOnlyList<ColumnDef> columns)
    {
        var violations = new List<QcViolation>();
        if (walls == null || columns == null) return violations;

        foreach (var wall in walls)
        {
            if (wall.Openings == null || wall.Openings.Count == 0 || wall.Length <= 1e-4)
            {
                continue;
            }

            var dir = (wall.EndPoint - wall.StartPoint).Normalize();
            foreach (var op in wall.Openings)
            {
                var opStart = wall.StartPoint + dir * op.DistanceAlongWallMm;
                var opEnd = wall.StartPoint + dir * (op.DistanceAlongWallMm + op.WidthMm);

                foreach (var col in columns)
                {
                    if (!string.Equals(col.BaseLevel ?? string.Empty, wall.BaseLevel ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    double d1 = opStart.DistanceTo(col.CenterLocation) - (col.WidthMm / 2.0);
                    double d2 = opEnd.DistanceTo(col.CenterLocation) - (col.WidthMm / 2.0);
                    double minDist = Math.Min(d1, d2);

                    if (minDist > 0 && minDist < MinOpeningToColumnClearanceMm)
                    {
                        violations.Add(new QcViolation(
                            QcSeverity.Warning, "CLR-001",
                            $"Lỗ mở {op.Id} cách mép cột {col.Id} chỉ {minDist:F1}mm (< 100mm quy chuẩn TCVN).",
                            op.Id, opStart));
                    }
                }
            }
        }

        return violations;
    }

    public IReadOnlyList<QcViolation> CheckDoorSwingClearances(
        IReadOnlyList<WallDef> walls, IReadOnlyList<MepFixtureDef> fixtures, IReadOnlyList<RoomDef> rooms)
    {
        var violations = new List<QcViolation>();
        if (walls == null) return violations;

        foreach (var wall in walls)
        {
            if (wall.Openings == null || wall.Openings.Count == 0 || wall.Length <= 1e-4)
            {
                continue;
            }

            var dir = (wall.EndPoint - wall.StartPoint).Normalize();
            foreach (var op in wall.Openings)
            {
                if (op.Kind != OpeningKind.Door && op.Kind != OpeningKind.Door2)
                {
                    continue;
                }

                CheckSingleDoorSwing(wall, op, dir, fixtures, rooms, violations);
            }
        }

        return violations;
    }

    private static void CheckSingleDoorSwing(
        WallDef wall, OpeningDef op, Vector2D dir,
        IReadOnlyList<MepFixtureDef>? fixtures, IReadOnlyList<RoomDef>? rooms,
        List<QcViolation> violations)
    {
        var hinge = wall.StartPoint + dir * op.DistanceAlongWallMm;
        double swingRadius = op.WidthMm;

        if (rooms != null)
        {
            foreach (var room in rooms)
            {
                if (!string.IsNullOrEmpty(room.Level) && !string.IsNullOrEmpty(wall.BaseLevel) &&
                    !string.Equals(room.Level, wall.BaseLevel, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (hinge.DistanceTo(room.TagLocation) < swingRadius * 0.9)
                {
                    violations.Add(new QcViolation(
                        QcSeverity.Warning, "CLR-002",
                        $"Cung quét cánh cửa {op.Id} chạm vào nhãn định danh phòng '{room.Name}'.",
                        op.Id, room.TagLocation));
                }
            }
        }

        if (fixtures != null)
        {
            foreach (var fix in fixtures)
            {
                var fixPos2D = new Point2D(fix.Location.X, fix.Location.Y);
                if (hinge.DistanceTo(fixPos2D) < swingRadius * 0.8)
                {
                    violations.Add(new QcViolation(
                        QcSeverity.Warning, "CLR-003",
                        $"Cung quét cánh cửa {op.Id} chạm vào thiết bị {fix.FamilyName} ({fix.Id}).",
                        op.Id, fixPos2D));
                }
            }
        }
    }

    public IReadOnlyList<QcViolation> CheckWallThicknesses(IReadOnlyList<WallDef> walls)
    {
        var violations = new List<QcViolation>();
        if (walls == null) return violations;
        foreach (var wall in walls)
        {
            bool isStandard = Math.Abs(wall.ThicknessMm - TcvnStandards.WallInteriorThicknessMm) < 5.0 ||
                              Math.Abs(wall.ThicknessMm - TcvnStandards.WallExteriorThicknessMm) < 5.0;
            if (!isStandard)
            {
                violations.Add(new QcViolation(
                    QcSeverity.Warning, "TCVN-WALL-001",
                    $"Bề dày tường {wall.Id} ({wall.ThicknessMm:F1}mm) không thuộc quy chuẩn TCVN (110mm hoặc 220mm).",
                    wall.Id, wall.StartPoint));
            }
        }
        return violations;
    }

    public IReadOnlyList<QcViolation> CheckAll(BimModel model)
    {
        var allViolations = new List<QcViolation>();
        if (model == null) return allViolations;
        allViolations.AddRange(CheckOpeningToColumnClearances(model.Walls, model.Columns));
        allViolations.AddRange(CheckDoorSwingClearances(model.Walls, model.Fixtures, model.Rooms));
        allViolations.AddRange(CheckWallThicknesses(model.Walls));
        return allViolations;
    }
}
