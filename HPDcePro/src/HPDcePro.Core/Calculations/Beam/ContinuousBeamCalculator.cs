using System;
using System.Collections.Generic;

namespace HPDcePro.Core.Calculations.Beam
{
    public static class ContinuousBeamCalculator
    {
        public static ContinuousBeamResult Calculate(ContinuousBeamInput input)
        {
            ValidateInput(input);
            var spanDesigns = ProcessSpans(input);
            double totalLength = ComputeTotalLength(input.Spans);
            var contTop = GenerateContinuousBars(input, totalLength, true);
            var contBot = GenerateContinuousBars(input, totalLength, false);
            var extraTop = GenerateSupportExtraTopBars(input);
            var extraBot = GenerateSpanExtraBottomBars(input);
            var sideBars = GenerateSideSkinBars(input, totalLength);
            var stirrups = AggregateStirrups(spanDesigns);
            var lapSplices = GenerateLapSplices(input, totalLength, contTop, contBot);

            return new ContinuousBeamResult
            {
                BeamMark = input.BeamMark,
                TotalLengthMm = totalLength,
                Spans = spanDesigns,
                ContinuousTopBars = contTop,
                ContinuousBottomBars = contBot,
                SupportExtraTopBars = extraTop,
                SpanExtraBottomBars = extraBot,
                SideBars = sideBars,
                StirrupZones = stirrups,
                LapSplices = lapSplices
            };
        }

        private static void ValidateInput(ContinuousBeamInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (input.Spans == null || input.Spans.Count == 0)
                throw new ArgumentException("Continuous beam must contain at least one span.", nameof(input));

            for (int i = 0; i < input.Spans.Count; i++)
            {
                var s = input.Spans[i];
                if (s.ClearSpanMm <= 0) throw new ArgumentOutOfRangeException(nameof(s.ClearSpanMm), "Span length must be positive.");
                if (s.SectionWidthMm <= 0 || s.SectionHeightMm <= 0)
                    throw new ArgumentOutOfRangeException(nameof(s.SectionWidthMm), "Section dimensions must be positive.");
            }
        }

        private static List<BeamSpanDesign> ProcessSpans(ContinuousBeamInput input)
        {
            var results = new List<BeamSpanDesign>(input.Spans.Count);
            for (int i = 0; i < input.Spans.Count; i++)
            {
                var s = input.Spans[i];
                double beta = DetermineRedistributionRatio(input.Spans, i, input.MomentRedistributionRatio);
                double redLeft = s.TopMomentLeftKnm * (1.0 - beta);
                double redRight = s.TopMomentRightKnm * (1.0 - beta);
                double redMid = s.BottomMomentMidKnm + 0.5 * beta * Math.Max(s.TopMomentLeftKnm, s.TopMomentRightKnm);

                var flexL = BeamFlexureCalculator.Calculate(new BeamFlexureInput(s.SectionWidthMm, s.SectionHeightMm, redLeft, input.Concrete, input.Steel));
                var flexR = BeamFlexureCalculator.Calculate(new BeamFlexureInput(s.SectionWidthMm, s.SectionHeightMm, redRight, input.Concrete, input.Steel));
                var flexM = BeamFlexureCalculator.Calculate(new BeamFlexureInput(s.SectionWidthMm, s.SectionHeightMm, redMid, input.Concrete, input.Steel));

                var design = new BeamSpanDesign
                {
                    SpanIndex = i,
                    SectionWidthMm = s.SectionWidthMm,
                    SectionHeightMm = s.SectionHeightMm,
                    ClearSpanMm = s.ClearSpanMm,
                    LeftSupportWidthMm = s.LeftSupportWidthMm,
                    RightSupportWidthMm = s.RightSupportWidthMm,
                    RedistributedTopMomentLeftKnm = redLeft,
                    RedistributedTopMomentRightKnm = redRight,
                    RedistributedBottomMomentMidKnm = redMid,
                    SupportTopAsLeft = flexL.AsRequiredMm2,
                    SupportTopAsRight = flexR.AsRequiredMm2,
                    MidSpanBottomAs = flexM.AsRequiredMm2
                };
                AddStirrupZonesForSpan(design, s, input);
                results.Add(design);
            }
            return results;
        }

        private static double DetermineRedistributionRatio(IReadOnlyList<BeamSpanInput> spans, int i, double requestedRatio)
        {
            double maxAllowed = Math.Min(0.20, Math.Max(0.0, requestedRatio));
            if (spans.Count <= 1) return maxAllowed;

            double currentL = spans[i].ClearSpanMm;
            if (i > 0 && Math.Max(currentL, spans[i - 1].ClearSpanMm) / Math.Min(currentL, spans[i - 1].ClearSpanMm) > 2.0)
                maxAllowed = Math.Min(maxAllowed, 0.10);
            if (i < spans.Count - 1 && Math.Max(currentL, spans[i + 1].ClearSpanMm) / Math.Min(currentL, spans[i + 1].ClearSpanMm) > 2.0)
                maxAllowed = Math.Min(maxAllowed, 0.10);
            return maxAllowed;
        }

        private static void AddStirrupZonesForSpan(BeamSpanDesign design, BeamSpanInput s, ContinuousBeamInput input)
        {
            double denseLen = s.ClearSpanMm * 0.25;
            double midLen = s.ClearSpanMm - 2.0 * denseLen;
            double pitchDense = 100.0;
            if (s.ShearLeftKn > 0 || s.ShearRightKn > 0)
            {
                var sIn = new BeamShearInput(s.SectionWidthMm, s.SectionHeightMm, Math.Max(s.ShearLeftKn, s.ShearRightKn), input.Concrete, input.Steel)
                {
                    StirrupDiameterMm = 8.0,
                    StirrupLegs = 2
                };
                var shearL = BeamShearCalculator.Calculate(sIn);
                pitchDense = Math.Min(150.0, shearL.RecommendedSpacingMm > 0 ? shearL.RecommendedSpacingMm : 100.0);
            }

            design.StirrupZones.Add(new StirrupZone { SpanIndex = s.SpanIndex, StartX = 0, EndX = denseLen, DiameterMm = 8.0, SpacingMm = pitchDense, LegCount = 2 });
            design.StirrupZones.Add(new StirrupZone { SpanIndex = s.SpanIndex, StartX = denseLen, EndX = denseLen + midLen, DiameterMm = 8.0, SpacingMm = 200.0, LegCount = 2 });
            design.StirrupZones.Add(new StirrupZone { SpanIndex = s.SpanIndex, StartX = denseLen + midLen, EndX = s.ClearSpanMm, DiameterMm = 8.0, SpacingMm = pitchDense, LegCount = 2 });
        }

        private static double ComputeTotalLength(IReadOnlyList<BeamSpanInput> spans)
        {
            double len = spans[0].LeftSupportWidthMm;
            for (int i = 0; i < spans.Count; i++) len += spans[i].ClearSpanMm + spans[i].RightSupportWidthMm;
            return len;
        }

        private static List<RebarItem> GenerateContinuousBars(ContinuousBeamInput input, double totalLength, bool isTop)
        {
            double dia = 18.0;
            double hook = 250.0;
            double segB = totalLength - 2.0 * input.DefaultCoverMm;
            double eleY = isTop ? (input.Spans[0].SectionHeightMm - input.DefaultCoverMm) : input.DefaultCoverMm;
            return new List<RebarItem>
            {
                new RebarItem
                {
                    BarMark = isTop ? "1" : "2",
                    DiameterMm = dia,
                    BarCount = 2,
                    StartX = input.DefaultCoverMm,
                    EndX = totalLength - input.DefaultCoverMm,
                    ElevationY = eleY,
                    SegmentA = hook,
                    SegmentB = segB,
                    SegmentC = hook,
                    LengthMm = hook + segB + hook,
                    ShapeCode = 2,
                    PlacementType = isTop ? RebarPlacementType.MainTop : RebarPlacementType.MainBottom,
                    LoaiThep = isTop ? "CTop" : "CBot"
                }
            };
        }

        private static List<RebarItem> GenerateSupportExtraTopBars(ContinuousBeamInput input)
        {
            var bars = new List<RebarItem>();
            double currentX = 0;
            int markNum = 3;

            for (int i = 0; i <= input.Spans.Count; i++)
            {
                bool isExterior = (i == 0 || i == input.Spans.Count);
                double supW = (i < input.Spans.Count) ? input.Spans[i].LeftSupportWidthMm : input.Spans[i - 1].RightSupportWidthMm;
                double extLen = ComputeSupportExtensionLength(input.Spans, i);
                double leftExt = (i == 0) ? 0 : extLen;
                double rightExt = (i == input.Spans.Count) ? 0 : extLen;
                double barLen = leftExt + supW + rightExt;

                bars.Add(new RebarItem
                {
                    BarMark = markNum.ToString(),
                    DiameterMm = 20.0,
                    BarCount = 2,
                    StartX = currentX - leftExt,
                    EndX = currentX + supW + rightExt,
                    ElevationY = input.Spans[Math.Min(i, input.Spans.Count - 1)].SectionHeightMm - input.DefaultCoverMm,
                    SegmentB = barLen,
                    LengthMm = isExterior ? barLen + 250.0 : barLen,
                    SegmentA = isExterior ? 250.0 : 0.0,
                    ShapeCode = (short)(isExterior ? 2 : 1),
                    PlacementType = RebarPlacementType.ExtraTopSupport,
                    LoaiThep = "TopGoi"
                });
                markNum++;
                if (i < input.Spans.Count) currentX += supW + input.Spans[i].ClearSpanMm;
            }
            return bars;
        }

        private static double ComputeSupportExtensionLength(IReadOnlyList<BeamSpanInput> spans, int supportIdx)
        {
            double lanc = 30.0 * 20.0;
            if (supportIdx == 0) return spans[0].ClearSpanMm * 0.25 + lanc;
            if (supportIdx == spans.Count) return spans[spans.Count - 1].ClearSpanMm * 0.25 + lanc;

            double leftL = spans[supportIdx - 1].ClearSpanMm;
            double rightL = spans[supportIdx].ClearSpanMm;
            double factor = (Math.Max(leftL, rightL) / Math.Min(leftL, rightL) > 2.0) ? (1.0 / 3.0) : 0.25;
            return Math.Max(leftL, rightL) * factor + lanc;
        }

        private static List<RebarItem> GenerateSpanExtraBottomBars(ContinuousBeamInput input)
        {
            var bars = new List<RebarItem>();
            double currentX = input.Spans[0].LeftSupportWidthMm;
            int markNum = 10;

            for (int i = 0; i < input.Spans.Count; i++)
            {
                var s = input.Spans[i];
                double cutOffset = 0.15 * s.ClearSpanMm;
                double startX = currentX + cutOffset;
                double barLen = s.ClearSpanMm - 2.0 * cutOffset;

                bars.Add(new RebarItem
                {
                    BarMark = markNum.ToString(),
                    DiameterMm = 18.0,
                    BarCount = 2,
                    StartX = startX,
                    EndX = startX + barLen,
                    ElevationY = input.DefaultCoverMm,
                    SegmentB = barLen,
                    LengthMm = barLen,
                    ShapeCode = 1,
                    PlacementType = RebarPlacementType.ExtraBottomSpan,
                    LoaiThep = "BotNhip"
                });
                markNum++;
                currentX += s.ClearSpanMm + s.RightSupportWidthMm;
            }
            return bars;
        }

        private static List<RebarItem> GenerateSideSkinBars(ContinuousBeamInput input, double totalLength)
        {
            var bars = new List<RebarItem>();
            double maxH = 0;
            for (int i = 0; i < input.Spans.Count; i++) maxH = Math.Max(maxH, input.Spans[i].SectionHeightMm);
            if (maxH < 700.0) return bars;

            int sidePairs = (int)Math.Floor((maxH - 100.0) / 300.0);
            double step = (maxH - 100.0) / (sidePairs + 1);
            for (int p = 1; p <= sidePairs; p++)
            {
                bars.Add(new RebarItem
                {
                    BarMark = $"S{p}",
                    DiameterMm = 14.0,
                    BarCount = 2,
                    StartX = input.DefaultCoverMm,
                    EndX = totalLength - input.DefaultCoverMm,
                    ElevationY = 50.0 + p * step,
                    SegmentB = totalLength - 2.0 * input.DefaultCoverMm,
                    LengthMm = totalLength - 2.0 * input.DefaultCoverMm,
                    ShapeCode = 1,
                    PlacementType = RebarPlacementType.SideSkin,
                    LoaiThep = "Suon"
                });
            }
            return bars;
        }

        private static List<StirrupZone> AggregateStirrups(List<BeamSpanDesign> designs)
        {
            var list = new List<StirrupZone>();
            for (int i = 0; i < designs.Count; i++) list.AddRange(designs[i].StirrupZones);
            return list;
        }

        private static List<LapSplice> GenerateLapSplices(ContinuousBeamInput input, double totalLen, List<RebarItem> top, List<RebarItem> bot)
        {
            var splices = new List<LapSplice>();
            if (totalLen <= input.CommercialBarLengthMm) return splices;

            double lapTop = 35.0 * top[0].DiameterMm;
            double lapBot = 35.0 * bot[0].DiameterMm;
            splices.Add(new LapSplice
            {
                BarMark = top[0].BarMark,
                Layer = RebarPlacementType.MainTop,
                SpliceX = input.Spans[0].LeftSupportWidthMm + input.Spans[0].ClearSpanMm * 0.5,
                LapLengthMm = lapTop,
                StaggerOffsetMm = 1.3 * lapTop
            });
            splices.Add(new LapSplice
            {
                BarMark = bot[0].BarMark,
                Layer = RebarPlacementType.MainBottom,
                SpliceX = input.Spans[0].LeftSupportWidthMm + input.Spans[0].ClearSpanMm * 0.15,
                LapLengthMm = lapBot,
                StaggerOffsetMm = 1.3 * lapBot
            });
            return splices;
        }
    }
}
