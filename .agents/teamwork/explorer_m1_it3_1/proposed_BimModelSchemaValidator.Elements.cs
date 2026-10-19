namespace HPBim.Core.Validation;

using System;
using System.Collections.Generic;
using HPBim.Core.Models;

public static partial class BimModelSchemaValidator
{
    private static readonly HashSet<string> ValidStructuralHostCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "Foundation", "Footing", "Pile", "PileCap", "GradeBeam",
        "Column", "Beam", "Wall", "Slab", "Floor", "Stair", "Framing", "StructuralFraming",
        "Superstructure", "Substructure"
    };

    private static void ValidateGrids(IReadOnlyList<AxisGrid>? grids, List<string> errors)
    {
        if (grids == null) return;
        var gridNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var g in grids)
        {
            if (string.IsNullOrWhiteSpace(g.Name))
            {
                errors.Add("[ERR_INVALID_GRID] Grid has empty or whitespace Name.");
            }
            else if (!gridNames.Add(g.Name))
            {
                errors.Add($"[ERR_DUPLICATE_GRID] Duplicate grid name detected: '{g.Name}'.");
            }

            if (double.IsNaN(g.StartPoint.X) || double.IsNaN(g.StartPoint.Y) ||
                double.IsNaN(g.EndPoint.X) || double.IsNaN(g.EndPoint.Y))
            {
                errors.Add($"[ERR_INVALID_GEOMETRY] Grid '{g.Name}' has non-finite coordinates.");
            }

            if (g.LengthMm < 1.0)
            {
                errors.Add($"[ERR_INVALID_GEOMETRY] Grid '{g.Name}' has degenerate length {g.LengthMm:F1}mm (< 1.0mm).");
            }
        }
    }

    private static void ValidateColumns(IReadOnlyList<ColumnDef>? columns, HashSet<string> levels, List<string> errors)
    {
        if (columns == null) return;
        var columnIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var c in columns)
        {
            if (!columnIds.Add(c.Id))
            {
                errors.Add($"[ERR_DUPLICATE_ID] Duplicate column ID detected: '{c.Id}'.");
            }
            if (string.IsNullOrWhiteSpace(c.BaseLevel) || !levels.Contains(c.BaseLevel))
            {
                errors.Add($"[ERR_BROKEN_REFERENCE] Column '{c.Id}' references non-existent BaseLevel '{c.BaseLevel}'.");
            }
            if (!string.IsNullOrWhiteSpace(c.TopLevel) && !levels.Contains(c.TopLevel))
            {
                errors.Add($"[ERR_BROKEN_REFERENCE] Column '{c.Id}' references non-existent TopLevel '{c.TopLevel}'.");
            }
            if (c.WidthMm <= 0 || c.DepthMm <= 0)
            {
                errors.Add($"[ERR_INVALID_GEOMETRY] Column '{c.Id}' has invalid cross-section dimensions: {c.WidthMm}x{c.DepthMm}mm.");
            }
            if (!string.IsNullOrWhiteSpace(c.TopLevel) && string.Equals(c.BaseLevel, c.TopLevel, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"[ERR_INVALID_GEOMETRY] Column '{c.Id}' has zero height (BaseLevel equals TopLevel '{c.BaseLevel}').");
            }
        }
    }

    private static void ValidateWalls(IReadOnlyList<WallDef>? walls, HashSet<string> levels, List<string> errors)
    {
        if (walls == null) return;
        var wallIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var w in walls)
        {
            if (!wallIds.Add(w.Id))
            {
                errors.Add($"[ERR_DUPLICATE_ID] Duplicate wall ID detected: '{w.Id}'.");
            }
            if (string.IsNullOrWhiteSpace(w.BaseLevel) || !levels.Contains(w.BaseLevel))
            {
                errors.Add($"[ERR_BROKEN_REFERENCE] Wall '{w.Id}' references non-existent BaseLevel '{w.BaseLevel}'.");
            }
            if (!string.IsNullOrWhiteSpace(w.TopLevel) && !levels.Contains(w.TopLevel))
            {
                errors.Add($"[ERR_BROKEN_REFERENCE] Wall '{w.Id}' references non-existent TopLevel '{w.TopLevel}'.");
            }
            if (w.LengthMm < 1.0)
            {
                errors.Add($"[ERR_INVALID_GEOMETRY] Wall '{w.Id}' has degenerate length {w.LengthMm:F1}mm (< 1.0mm).");
            }
            if (w.ThicknessMm <= 0)
            {
                errors.Add($"[ERR_INVALID_GEOMETRY] Wall '{w.Id}' has invalid thickness {w.ThicknessMm}mm.");
            }

            ValidateWallOpenings(w, errors);
        }
    }

    private static void ValidateWallOpenings(WallDef w, List<string> errors)
    {
        if (w.Openings == null) return;
        foreach (var op in w.Openings)
        {
            if (op.WidthMm <= 0 || op.HeightMm <= 0)
            {
                errors.Add($"[ERR_INVALID_GEOMETRY] Opening '{op.Id}' on wall '{w.Id}' has invalid size {op.WidthMm}x{op.HeightMm}mm.");
            }
            if (op.DistanceAlongWallMm < 0 || op.DistanceAlongWallMm + op.WidthMm > w.LengthMm + 10.0)
            {
                errors.Add($"[ERR_INVALID_GEOMETRY] Opening '{op.Id}' extends outside wall '{w.Id}' boundary (Offset {op.DistanceAlongWallMm:F0}mm + Width {op.WidthMm:F0}mm > Wall Length {w.LengthMm:F0}mm).");
            }
        }
    }

    private static void ValidateBeams(IReadOnlyList<BeamDef>? beams, HashSet<string> levels, List<string> errors)
    {
        if (beams == null) return;
        var beamIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var b in beams)
        {
            if (!beamIds.Add(b.Id))
            {
                errors.Add($"[ERR_DUPLICATE_ID] Duplicate beam ID detected: '{b.Id}'.");
            }
            if (string.IsNullOrWhiteSpace(b.Level) || !levels.Contains(b.Level))
            {
                errors.Add($"[ERR_BROKEN_REFERENCE] Beam '{b.Id}' references non-existent Level '{b.Level}'.");
            }
            if (b.LengthMm < 1.0)
            {
                errors.Add($"[ERR_INVALID_GEOMETRY] Beam '{b.Id}' has degenerate length {b.LengthMm:F1}mm.");
            }
            if (b.WidthMm <= 0 || b.HeightMm <= 0)
            {
                errors.Add($"[ERR_INVALID_GEOMETRY] Beam '{b.Id}' has invalid cross-section dimensions: {b.WidthMm}x{b.HeightMm}mm.");
            }
        }
    }

    private static void ValidateSlabs(IReadOnlyList<SlabDef>? slabs, HashSet<string> levels, List<string> errors)
    {
        if (slabs == null) return;
        var slabIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in slabs)
        {
            if (!slabIds.Add(s.Id))
            {
                errors.Add($"[ERR_DUPLICATE_ID] Duplicate slab ID detected: '{s.Id}'.");
            }
            if (string.IsNullOrWhiteSpace(s.Level) || !levels.Contains(s.Level))
            {
                errors.Add($"[ERR_BROKEN_REFERENCE] Slab '{s.Id}' references non-existent Level '{s.Level}'.");
            }
            if (s.ThicknessMm <= 0)
            {
                errors.Add($"[ERR_INVALID_GEOMETRY] Slab '{s.Id}' has invalid thickness {s.ThicknessMm}mm.");
            }
            if (s.Boundary == null || s.Boundary.Vertices == null || s.Boundary.Vertices.Count < 3)
            {
                errors.Add($"[ERR_INVALID_GEOMETRY] Slab '{s.Id}' has degenerate boundary polygon (fewer than 3 vertices).");
            }
        }
    }

    private static void ValidateRooms(IReadOnlyList<RoomDef>? rooms, HashSet<string> levels, List<string> errors)
    {
        if (rooms == null) return;
        foreach (var r in rooms)
        {
            if (string.IsNullOrWhiteSpace(r.Level) || !levels.Contains(r.Level))
            {
                errors.Add($"[ERR_BROKEN_REFERENCE] Room '{r.Name}' references non-existent Level '{r.Level}'.");
            }
        }
    }

    private static void ValidateFixtures(IReadOnlyList<MepFixtureDef>? fixtures, HashSet<string> levels, List<string> errors)
    {
        if (fixtures == null) return;
        foreach (var f in fixtures)
        {
            if (string.IsNullOrWhiteSpace(f.Level) || !levels.Contains(f.Level))
            {
                errors.Add($"[ERR_BROKEN_REFERENCE] Fixture '{f.Id}' references non-existent Level '{f.Level}'.");
            }
        }
    }

    private static void ValidatePipes(IReadOnlyList<PipeRunDef>? pipes, HashSet<string> levels, List<string> errors)
    {
        if (pipes == null) return;
        foreach (var p in pipes)
        {
            if (!string.IsNullOrWhiteSpace(p.Level) && !levels.Contains(p.Level))
            {
                errors.Add($"[ERR_BROKEN_REFERENCE] Pipe '{p.Id}' references non-existent Level '{p.Level}'.");
            }
            if (p.Points == null || p.Points.Count < 2)
            {
                errors.Add($"[ERR_INVALID_GEOMETRY] Pipe '{p.Id}' has fewer than 2 route points.");
            }
        }
    }

    private static void ValidateRebars(IReadOnlyList<RebarDef>? rebars, BimModel model, List<string> errors)
    {
        if (rebars == null || rebars.Count == 0) return;
        var validHostIds = CollectValidHostIds(model);

        foreach (var rb in rebars)
        {
            if (!string.IsNullOrWhiteSpace(rb.HostElementId) && !IsValidRebarHost(rb, validHostIds))
            {
                errors.Add($"[ERR_BROKEN_REFERENCE] Rebar '{rb.Id}' references non-existent HostElementId '{rb.HostElementId}'.");
            }
            if (rb.DiameterMm <= 0 || rb.LengthMm <= 0 || rb.Quantity <= 0)
            {
                errors.Add($"[ERR_INVALID_GEOMETRY] Rebar '{rb.Id}' has invalid geometry parameters (Dia: {rb.DiameterMm}, Length: {rb.LengthMm}, Qty: {rb.Quantity}).");
            }
        }
    }

    private static HashSet<string> CollectValidHostIds(BimModel model)
    {
        var validHostIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (model.Columns != null)
        {
            foreach (var c in model.Columns)
            {
                validHostIds.Add(c.Id);
                int us = c.Id.IndexOf('_');
                if (us > 0) validHostIds.Add(c.Id.Substring(0, us));
            }
        }
        if (model.Beams != null)
        {
            foreach (var b in model.Beams)
            {
                validHostIds.Add(b.Id);
                if (!string.IsNullOrWhiteSpace(b.BeamType)) validHostIds.Add(b.BeamType);
                int us = b.Id.IndexOf('_');
                if (us > 0) validHostIds.Add(b.Id.Substring(0, us));
            }
        }
        if (model.Walls != null) foreach (var w in model.Walls) validHostIds.Add(w.Id);
        if (model.Slabs != null)
        {
            foreach (var s in model.Slabs)
            {
                validHostIds.Add(s.Id);
                int us = s.Id.IndexOf('_');
                if (us > 0) validHostIds.Add(s.Id.Substring(0, us));
            }
        }
        return validHostIds;
    }

    private static bool IsValidRebarHost(RebarDef rb, HashSet<string> validHostIds)
    {
        if (string.IsNullOrWhiteSpace(rb.HostElementId)) return false;
        if (validHostIds.Contains(rb.HostElementId)) return true;

        if (!string.IsNullOrWhiteSpace(rb.Zone) && ValidStructuralHostCategories.Contains(rb.Zone!))
        {
            return true;
        }

        foreach (var cat in ValidStructuralHostCategories)
        {
            if (rb.HostElementId.StartsWith(cat, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return validHostIds.Count == 0;
    }
}
