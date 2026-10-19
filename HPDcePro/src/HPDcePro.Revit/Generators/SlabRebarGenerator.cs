using System;
using System.Collections.Generic;
using HPDcePro.Core.Geometry;

namespace HPDcePro.Revit.Generators
{
    public sealed record SlabRebarInput(
        string SlabMark,
        IReadOnlyList<Point3D> BoundaryMm,
        double ThicknessMm,
        double SpanXMm = 4000.0,
        double SpanYMm = 5000.0,
        double BottomDiaMm = 10.0,
        double BottomSpacingMm = 150.0,
        double TopDiaMm = 10.0,
        double TopSpacingMm = 150.0,
        double CoverMm = 20.0);

    public static class SlabRebarGenerator
    {
        public static RevitRebarPlacementResult GenerateSlabRebar(SlabRebarInput input, Point3D slabOrigin)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));

            string tagPrefix = RevitRebarPlacementService.BuildIdempotentTagPrefix(input.SlabMark);
            int countX = Math.Max(2, (int)Math.Floor((input.SpanYMm - 2.0 * input.CoverMm) / input.BottomSpacingMm) + 1);
            double lenXMm = Math.Max(100.0, input.SpanXMm - 2.0 * input.CoverMm);
            int countY = Math.Max(2, (int)Math.Floor((input.SpanXMm - 2.0 * input.CoverMm) / input.BottomSpacingMm) + 1);
            double lenYMm = Math.Max(100.0, input.SpanYMm - 2.0 * input.CoverMm);

            var rebars = new List<RevitRebarDescriptor>
            {
                CreateMeshDescriptor(input.SlabMark, "Slab_Bot_X", input.BottomDiaMm, countX, input.BottomSpacingMm, lenXMm, $"{tagPrefix}Bot_X"),
                CreateMeshDescriptor(input.SlabMark, "Slab_Bot_Y", input.BottomDiaMm, countY, input.BottomSpacingMm, lenYMm, $"{tagPrefix}Bot_Y")
            };

            int totalCount = countX + countY;
            double totalWeight = (countX * 0.00617 * input.BottomDiaMm * input.BottomDiaMm * lenXMm / 1000.0) +
                                 (countY * 0.00617 * input.BottomDiaMm * input.BottomDiaMm * lenYMm / 1000.0);

            return new RevitRebarPlacementResult
            {
                ElementMark = input.SlabMark,
                TotalRebarCount = totalCount,
                TotalWeightKg = totalWeight,
                Rebars = rebars,
                PreviousRebarTagsToDelete = new[] { $"{tagPrefix}*" },
                IsSuccess = true,
                Message = $"Mapped slab {input.SlabMark} rebar: {totalCount} bars ({totalWeight:F1}kg)."
            };
        }

        private static RevitRebarDescriptor CreateMeshDescriptor(
            string slabMark, string barMark, double dia, int qty, double spacing, double len, string tag)
        {
            return new RevitRebarDescriptor
            {
                BarMark = barMark,
                ElementMark = slabMark,
                DiameterMm = dia,
                Quantity = qty,
                SpacingMm = spacing,
                LengthMm = len,
                ShapeType = RevitShapeType.Straight,
                CommentsTag = tag
            };
        }
    }
}
