using System;
using System.Collections.Generic;
using HPDcePro.Core.Geometry;

namespace HPDcePro.Revit.Generators
{
    public sealed record WallRebarInput(
        string WallMark,
        double ThicknessMm,
        double LengthMm,
        double HeightMm,
        double VertDiaMm = 12.0,
        double HorizDiaMm = 10.0,
        double SpacingMm = 200.0,
        double CoverMm = 25.0);

    public static class WallRebarGenerator
    {
        public static RevitRebarPlacementResult GenerateWallRebar(WallRebarInput input, Point3D wallOrigin)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));

            string tagPrefix = RevitRebarPlacementService.BuildIdempotentTagPrefix(input.WallMark);
            int vertCount = Math.Max(2, (int)Math.Floor((input.LengthMm - 2.0 * input.CoverMm) / input.SpacingMm) + 1);
            double vertLen = input.HeightMm + 600.0;
            int horizCount = Math.Max(2, (int)Math.Floor((input.HeightMm - 2.0 * input.CoverMm) / input.SpacingMm) + 1);
            double horizLen = input.LengthMm - 2.0 * input.CoverMm + 400.0;

            var rebars = new List<RevitRebarDescriptor>();
            AddLayerPair(rebars, input.WallMark, "Vert", input.VertDiaMm, vertCount, input.SpacingMm, vertLen, RevitShapeType.Straight, tagPrefix);
            AddLayerPair(rebars, input.WallMark, "Horiz", input.HorizDiaMm, horizCount, input.SpacingMm, horizLen, RevitShapeType.StandardHook90, tagPrefix);

            int totalCount = 2 * (vertCount + horizCount);
            double totalWeight = (2 * vertCount * (0.00617 * input.VertDiaMm * input.VertDiaMm * vertLen / 1000.0)) +
                                 (2 * horizCount * (0.00617 * input.HorizDiaMm * input.HorizDiaMm * horizLen / 1000.0));

            return new RevitRebarPlacementResult
            {
                ElementMark = input.WallMark,
                TotalRebarCount = totalCount,
                TotalWeightKg = totalWeight,
                Rebars = rebars,
                PreviousRebarTagsToDelete = new[] { $"{tagPrefix}*" },
                IsSuccess = true,
                Message = $"Mapped wall {input.WallMark} rebar: {totalCount} bars ({totalWeight:F1}kg)."
            };
        }

        private static void AddLayerPair(
            List<RevitRebarDescriptor> rebars,
            string wallMark,
            string dir,
            double dia,
            int count,
            double spacing,
            double length,
            RevitShapeType shape,
            string tagPrefix)
        {
            rebars.Add(new RevitRebarDescriptor
            {
                BarMark = $"Wall_{dir}_Layer1",
                ElementMark = wallMark,
                DiameterMm = dia,
                Quantity = count,
                SpacingMm = spacing,
                LengthMm = length,
                ShapeType = shape,
                CommentsTag = $"{tagPrefix}{dir}_L1"
            });
            rebars.Add(new RevitRebarDescriptor
            {
                BarMark = $"Wall_{dir}_Layer2",
                ElementMark = wallMark,
                DiameterMm = dia,
                Quantity = count,
                SpacingMm = spacing,
                LengthMm = length,
                ShapeType = shape,
                CommentsTag = $"{tagPrefix}{dir}_L2"
            });
        }
    }
}
