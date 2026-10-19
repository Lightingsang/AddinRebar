using System;
using System.Collections.Generic;
using System.Linq;
using HPDcePro.Core.Calculations.Beam;
using HPDcePro.Core.Calculations.Column;
using HPDcePro.Core.Calculations.Foundation;
using HPDcePro.Core.Geometry;

namespace HPDcePro.Revit
{
    public enum RevitShapeType
    {
        Straight = 0,
        StandardHook90 = 1,
        StandardHook135 = 2,
        StandardHook180 = 3,
        CrankedLap = 4,
        ClosedStirrup = 5,
        UShapedCap = 6
    }

    public sealed class RevitHookDescriptor
    {
        public double HookAngleDeg { get; set; } = 90.0;
        public double HookLengthMm { get; set; }
        public double HookLengthFeet => RevitDataExchangeService.MmToFeet(HookLengthMm);
    }

    public sealed class RevitRebarDescriptor
    {
        public string BarMark { get; set; } = string.Empty;
        public string ElementMark { get; set; } = string.Empty;
        public double DiameterMm { get; set; } = 20.0;
        public double DiameterFeet => RevitDataExchangeService.MmToFeet(DiameterMm);
        public int Quantity { get; set; } = 1;
        public double SpacingMm { get; set; }
        public double SpacingFeet => RevitDataExchangeService.MmToFeet(SpacingMm);
        public double LengthMm { get; set; }
        public double LengthFeet => RevitDataExchangeService.MmToFeet(LengthMm);
        public RevitShapeType ShapeType { get; set; } = RevitShapeType.Straight;
        public RevitHookDescriptor? StartHook { get; set; }
        public RevitHookDescriptor? EndHook { get; set; }
        public List<Point3D> CurvePointsMm { get; set; } = new();
        public IEnumerable<Point3D> CurvePointsFeet =>
            CurvePointsMm.Select(p => new Point3D(
                RevitDataExchangeService.MmToFeet(p.X),
                RevitDataExchangeService.MmToFeet(p.Y),
                RevitDataExchangeService.MmToFeet(p.Z)));
        public string CommentsTag { get; set; } = string.Empty;
    }

    public sealed class RevitRebarPlacementResult
    {
        public string ElementMark { get; set; } = string.Empty;
        public int TotalRebarCount { get; set; }
        public double TotalWeightKg { get; set; }
        public IReadOnlyList<RevitRebarDescriptor> Rebars { get; set; } = Array.Empty<RevitRebarDescriptor>();
        public IReadOnlyList<string> PreviousRebarTagsToDelete { get; set; } = Array.Empty<string>();
        public bool IsSuccess { get; set; } = true;
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Service mapping domain detailing results to Revit rebar parameters with idempotent mark tags.
    /// </summary>
    public static class RevitRebarPlacementService
    {
        public static string BuildIdempotentTagPrefix(string elementMark) => $"HPDcePro_Rebar_{elementMark}_";

        public static RevitRebarPlacementResult MapBeamRebar(
            ContinuousBeamResult beamResult, Point3D beamOrigin)
        {
            if (beamResult == null) throw new ArgumentNullException(nameof(beamResult));

            var list = new List<RevitRebarDescriptor>();
            string tagPrefix = BuildIdempotentTagPrefix(beamResult.BeamMark);

            // 1. Continuous Top Bars
            foreach (var bar in beamResult.ContinuousTopBars)
            {
                list.Add(CreateBeamBarDescriptor(beamResult.BeamMark, "Top", bar, beamOrigin, RevitShapeType.StandardHook90));
            }

            // 2. Continuous Bottom Bars
            foreach (var bar in beamResult.ContinuousBottomBars)
            {
                list.Add(CreateBeamBarDescriptor(beamResult.BeamMark, "Bot", bar, beamOrigin, RevitShapeType.StandardHook90));
            }

            // 3. Stirrups
            int stirrupIdx = 1;
            foreach (var stz in beamResult.StirrupZones)
            {
                list.Add(CreateStirrupDescriptor(beamResult.BeamMark, stirrupIdx++, stz, beamOrigin));
            }

            int totalCount = list.Sum(r => r.Quantity);
            double totalWeight = list.Sum(r => (Math.PI * Math.Pow(r.DiameterMm * 0.5, 2) * 7850e-9) * r.LengthMm * r.Quantity);

            return new RevitRebarPlacementResult
            {
                ElementMark = beamResult.BeamMark,
                TotalRebarCount = totalCount,
                TotalWeightKg = totalWeight,
                Rebars = list,
                PreviousRebarTagsToDelete = new[] { $"{tagPrefix}*" },
                IsSuccess = true,
                Message = $"Mapped {list.Count} rebar sets ({totalCount} bars, {totalWeight:F1}kg) for beam {beamResult.BeamMark}."
            };
        }

        public static RevitRebarPlacementResult MapColumnRebar(
            ColumnDetailingOptions colOptions, Point3D columnBase)
        {
            if (colOptions == null) throw new ArgumentNullException(nameof(colOptions));

            var list = new List<RevitRebarDescriptor>();
            string tagPrefix = BuildIdempotentTagPrefix(colOptions.ColumnMark);

            foreach (var story in colOptions.Stories)
            {
                list.AddRange(CreateColumnStoryDescriptors(colOptions.ColumnMark, story, columnBase, tagPrefix));
            }

            int totalCount = list.Sum(r => r.Quantity);
            double totalWeight = list.Sum(r => (Math.PI * Math.Pow(r.DiameterMm * 0.5, 2) * 7850e-9) * r.LengthMm * r.Quantity);

            return new RevitRebarPlacementResult
            {
                ElementMark = colOptions.ColumnMark,
                TotalRebarCount = totalCount,
                TotalWeightKg = totalWeight,
                Rebars = list,
                PreviousRebarTagsToDelete = new[] { $"{tagPrefix}*" },
                IsSuccess = true,
                Message = $"Mapped column {colOptions.ColumnMark}: {list.Count} sets, {totalCount} total bars."
            };
        }

        public static RevitRebarPlacementResult MapPileCapRebar(
            PileCapResult capResult, PileCapInput capInput, Point3D capOrigin)
        {
            if (capResult == null) throw new ArgumentNullException(nameof(capResult));
            if (capInput == null) throw new ArgumentNullException(nameof(capInput));

            var list = new List<RevitRebarDescriptor>();
            string tagPrefix = BuildIdempotentTagPrefix(capInput.Mark);

            list.Add(CreatePileCapMeshDescriptor(capInput.Mark, "BottomX", capInput.WidthBMm, capInput.LengthLMm, tagPrefix));
            list.Add(CreatePileCapMeshDescriptor(capInput.Mark, "BottomY", capInput.LengthLMm, capInput.WidthBMm, tagPrefix));

            int totalCount = list.Sum(r => r.Quantity);
            double totalWeight = list.Sum(r => (Math.PI * Math.Pow(r.DiameterMm * 0.5, 2) * 7850e-9) * r.LengthMm * r.Quantity);

            return new RevitRebarPlacementResult
            {
                ElementMark = capInput.Mark,
                TotalRebarCount = totalCount,
                TotalWeightKg = totalWeight,
                Rebars = list,
                PreviousRebarTagsToDelete = new[] { $"{tagPrefix}*" },
                IsSuccess = true,
                Message = $"Mapped pile cap {capInput.Mark}: {list.Count} mesh layers ({totalCount} bars)."
            };
        }

        private static IEnumerable<RevitRebarDescriptor> CreateColumnStoryDescriptors(
            string colMark, ColumnStoryDetail story, Point3D columnBase, string tagPrefix)
        {
            yield return new RevitRebarDescriptor
            {
                BarMark = $"{story.StoryName}_Main",
                ElementMark = colMark,
                DiameterMm = story.MainBarDiameterMm,
                Quantity = story.MainBarCount,
                LengthMm = story.StoryHeightMm + 600.0,
                ShapeType = RevitShapeType.CrankedLap,
                CurvePointsMm = new List<Point3D>
                {
                    new(columnBase.X, columnBase.Y, columnBase.Z + story.FloorElevationMm),
                    new(columnBase.X, columnBase.Y, columnBase.Z + story.FloorElevationMm + story.StoryHeightMm + 600.0)
                },
                CommentsTag = $"{tagPrefix}{story.StoryName}_Main"
            };

            double tieSpacing = story.TieSpacingMidMm > 0 ? story.TieSpacingMidMm : 200.0;
            int tieCount = (int)Math.Floor(story.StoryHeightMm / tieSpacing);
            yield return new RevitRebarDescriptor
            {
                BarMark = $"{story.StoryName}_Tie",
                ElementMark = colMark,
                DiameterMm = story.TieDiameterMm,
                Quantity = tieCount,
                SpacingMm = tieSpacing,
                LengthMm = 2.0 * (story.WidthBMm + story.HeightHMm) + 200.0,
                ShapeType = RevitShapeType.ClosedStirrup,
                CommentsTag = $"{tagPrefix}{story.StoryName}_Tie"
            };
        }

        private static RevitRebarDescriptor CreatePileCapMeshDescriptor(
            string mark, string layer, double spanWidth, double spanLength, string tagPrefix)
        {
            int count = Math.Max(4, (int)Math.Ceiling(spanWidth / 150.0));
            return new RevitRebarDescriptor
            {
                BarMark = layer,
                ElementMark = mark,
                DiameterMm = 16.0,
                Quantity = count,
                SpacingMm = 150.0,
                LengthMm = spanLength - 100.0 + 400.0,
                ShapeType = RevitShapeType.StandardHook90,
                StartHook = new RevitHookDescriptor { HookAngleDeg = 90.0, HookLengthMm = 200.0 },
                EndHook = new RevitHookDescriptor { HookAngleDeg = 90.0, HookLengthMm = 200.0 },
                CommentsTag = $"{tagPrefix}{layer}"
            };
        }

        private static RevitRebarDescriptor CreateBeamBarDescriptor(
            string beamMark, string layer, RebarItem bar, Point3D origin, RevitShapeType shape)
        {
            double z = origin.Z + (layer == "Top" ? 350.0 : 50.0);
            return new RevitRebarDescriptor
            {
                BarMark = $"{layer}_{bar.BarMark}",
                ElementMark = beamMark,
                DiameterMm = bar.DiameterMm,
                Quantity = bar.BarCount,
                LengthMm = bar.LengthMm,
                ShapeType = shape,
                StartHook = new RevitHookDescriptor { HookAngleDeg = 90.0, HookLengthMm = bar.DiameterMm * 12.0 },
                EndHook = new RevitHookDescriptor { HookAngleDeg = 90.0, HookLengthMm = bar.DiameterMm * 12.0 },
                CurvePointsMm = new List<Point3D>
                {
                    new(origin.X + bar.StartX, origin.Y, z),
                    new(origin.X + bar.EndX, origin.Y, z)
                },
                CommentsTag = $"{BuildIdempotentTagPrefix(beamMark)}{layer}_{bar.BarMark}"
            };
        }

        private static RevitRebarDescriptor CreateStirrupDescriptor(
            string beamMark, int idx, StirrupZone zone, Point3D origin)
        {
            double zoneLen = Math.Max(100.0, zone.EndX - zone.StartX);
            int qty = Math.Max(1, (int)Math.Ceiling(zoneLen / Math.Max(50.0, zone.SpacingMm)));
            return new RevitRebarDescriptor
            {
                BarMark = $"Stirrup_{idx}",
                ElementMark = beamMark,
                DiameterMm = zone.DiameterMm,
                Quantity = qty,
                SpacingMm = zone.SpacingMm,
                LengthMm = 1200.0,
                ShapeType = RevitShapeType.ClosedStirrup,
                CommentsTag = $"{BuildIdempotentTagPrefix(beamMark)}Stirrup_{idx}"
            };
        }
    }
}
