using System;
using System.Collections.Generic;
using System.Linq;
using HPDcePro.Core.Geometry;

namespace HPDcePro.Revit.Generators
{
    public enum StructuralCategoryRank
    {
        Column = 1,
        Wall = 2,
        Beam = 3,
        Slab = 4
    }

    public sealed record BoundingBox3D(Point3D Min, Point3D Max)
    {
        public bool Intersects(BoundingBox3D other)
        {
            if (other == null) return false;
            return Min.X <= other.Max.X && Max.X >= other.Min.X &&
                   Min.Y <= other.Max.Y && Max.Y >= other.Min.Y &&
                   Min.Z <= other.Max.Z && Max.Z >= other.Min.Z;
        }
    }

    public sealed record StructuralElementProxy(
        string Id,
        StructuralCategoryRank Rank,
        BoundingBox3D BBox);

    public sealed record JoinInstruction(
        string PrimaryId,
        string SecondaryId,
        bool ShouldJoin);

    public sealed record CopingInstruction(
        string BeamId,
        string CuttingElementId,
        double ClearanceMm = 20.0);

    public static class RevitJoinCutPlanner
    {
        public static IReadOnlyList<JoinInstruction> DetermineJoins(IEnumerable<StructuralElementProxy> elements)
        {
            if (elements == null) return Array.Empty<JoinInstruction>();

            var list = elements.ToList();
            var instructions = new List<JoinInstruction>();

            for (int i = 0; i < list.Count; i++)
            {
                for (int j = i + 1; j < list.Count; j++)
                {
                    var elemA = list[i];
                    var elemB = list[j];

                    if (elemA.BBox.Intersects(elemB.BBox))
                    {
                        // Priority: lower rank number has higher precedence (Column > Wall > Beam > Slab)
                        if (elemA.Rank < elemB.Rank)
                        {
                            instructions.Add(new JoinInstruction(elemA.Id, elemB.Id, true));
                        }
                        else if (elemB.Rank < elemA.Rank)
                        {
                            instructions.Add(new JoinInstruction(elemB.Id, elemA.Id, true));
                        }
                        else
                        {
                            // Equal rank: stable ordering by string comparison
                            if (string.CompareOrdinal(elemA.Id, elemB.Id) <= 0)
                                instructions.Add(new JoinInstruction(elemA.Id, elemB.Id, true));
                            else
                                instructions.Add(new JoinInstruction(elemB.Id, elemA.Id, true));
                        }
                    }
                }
            }

            return instructions;
        }

        public static IReadOnlyList<CopingInstruction> DetermineCoping(
            IEnumerable<StructuralElementProxy> beams,
            IEnumerable<StructuralElementProxy> cutters,
            double clearanceMm = 20.0)
        {
            if (beams == null || cutters == null) return Array.Empty<CopingInstruction>();

            var results = new List<CopingInstruction>();
            foreach (var beam in beams.Where(b => b.Rank == StructuralCategoryRank.Beam))
            {
                foreach (var cutter in cutters)
                {
                    if (cutter.Id != beam.Id && beam.BBox.Intersects(cutter.BBox))
                    {
                        results.Add(new CopingInstruction(beam.Id, cutter.Id, clearanceMm));
                    }
                }
            }

            return results;
        }
    }

    public static class JoinCutPlanGenerator
    {
        public static IReadOnlyList<JoinInstruction> DetermineJoins(IEnumerable<StructuralElementProxy> elements) =>
            RevitJoinCutPlanner.DetermineJoins(elements);

        public static IReadOnlyList<CopingInstruction> DetermineCoping(
            IEnumerable<StructuralElementProxy> beams,
            IEnumerable<StructuralElementProxy> cutters,
            double clearanceMm = 20.0) =>
            RevitJoinCutPlanner.DetermineCoping(beams, cutters, clearanceMm);
    }
}
