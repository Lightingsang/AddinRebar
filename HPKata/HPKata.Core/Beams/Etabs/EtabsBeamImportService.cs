using System;
using System.Collections.Generic;
using HPKata.Core.Beams.Calculators;
using HPKata.Core.Beams.Models;

namespace HPKata.Core.Beams.Etabs
{
    /// <summary>
    /// Coordinates importing, chaining, and auto-detailing ETABS beam analysis results.
    /// </summary>
    public sealed class EtabsBeamImportService
    {
        public static List<BeamRebarLayout> ImportFromTableContent(string textContent)
        {
            var rows = EtabsTextTableParser.ParseDelimitedRows(textContent);
            var mappedFrames = EtabsDesignAreaMapper.MapDesignTable(rows);

            var layouts = new List<BeamRebarLayout>();
            int spanIdx = 0;

            foreach (var kvp in mappedFrames)
            {
                var frame = kvp.Value;
                var layout = ConvertFrameToBeamLayout(frame, spanIdx++);
                layouts.Add(layout);
            }

            return layouts;
        }

        public static BeamRebarLayout ConvertFrameToBeamLayout(EtabsFrameDesign frame, int index)
        {
            var layout = new BeamRebarLayout
            {
                BeamName = frame.FrameName,
                TotalLengthMm = frame.LengthMm
            };

            layout.Spans.Add(new BeamSpanModel
            {
                SpanIndex = 0,
                SpanName = frame.FrameName,
                ClearLengthMm = frame.LengthMm,
                LeftSupportWidthMm = 300.0,
                RightSupportWidthMm = 300.0,
                WidthMm = frame.WidthMm,
                HeightMm = frame.HeightMm
            });

            // 1. Optimize top and bottom continuous bars
            PopulateLongitudinalBars(layout, frame);

            // 2. Stirrups
            var stirrupModel = BeamStirrupZoneCalculator.CalculateZones(
                0, frame.LengthMm, frame.WidthMm, frame.HeightMm);
            layout.StirrupZones[0] = stirrupModel;

            // 3. Skin bars if h >= 700mm
            if (BeamSkinBarCalculator.IsSkinReinforcementRequired(frame.HeightMm))
            {
                var skinRuns = BeamSkinBarCalculator.GenerateSkinBarRuns(
                    frame.LengthMm, frame.HeightMm);
                layout.SkinBars.AddRange(skinRuns);
            }

            return layout;
        }

        private static void PopulateLongitudinalBars(BeamRebarLayout layout, EtabsFrameDesign frame)
        {
            double maxTopAs = Math.Max(frame.AsTopLeftRequired, Math.Max(frame.AsTopMidRequired, frame.AsTopRightRequired));
            double maxBotAs = Math.Max(frame.AsBotLeftRequired, Math.Max(frame.AsBotMidRequired, frame.AsBotRightRequired));

            var topOpt = TcvnBeamRebarOptimizer.Optimize(maxTopAs, frame.WidthMm, isTopBar: true, beamHeightMm: frame.HeightMm);
            var botOpt = TcvnBeamRebarOptimizer.Optimize(maxBotAs, frame.WidthMm, isTopBar: false, beamHeightMm: frame.HeightMm);

            AddTopBarRuns(layout, frame, topOpt);
            AddBottomBarRuns(layout, frame, botOpt);
        }

        private static void AddTopBarRuns(BeamRebarLayout layout, EtabsFrameDesign frame, RebarOptimizationResult topOpt)
        {
            layout.TopContinuousBars.Add(new RebarRunModel
            {
                Mark = "1",
                DiameterMm = topOpt.PrimaryDiameterMm,
                BarCount = topOpt.PrimaryCount,
                StartXMm = 0.0,
                LengthMm = frame.LengthMm,
                IsTopBar = true,
                LayerIndex = 1,
                ElevationZMm = frame.HeightMm - 30.0,
                ShapeName = "dang01"
            });

            if (topOpt.SecondaryCount > 0 && topOpt.SecondaryDiameterMm > 0)
            {
                layout.TopContinuousBars.Add(new RebarRunModel
                {
                    Mark = "1B",
                    DiameterMm = topOpt.SecondaryDiameterMm,
                    BarCount = topOpt.SecondaryCount,
                    StartXMm = 0.0,
                    LengthMm = frame.LengthMm,
                    IsTopBar = true,
                    LayerIndex = topOpt.LayerCount > 1 ? 2 : 1,
                    ElevationZMm = topOpt.LayerCount > 1 ? frame.HeightMm - 80.0 : frame.HeightMm - 30.0,
                    ShapeName = "dang01"
                });
            }
        }

        private static void AddBottomBarRuns(BeamRebarLayout layout, EtabsFrameDesign frame, RebarOptimizationResult botOpt)
        {
            layout.BottomContinuousBars.Add(new RebarRunModel
            {
                Mark = "2",
                DiameterMm = botOpt.PrimaryDiameterMm,
                BarCount = botOpt.PrimaryCount,
                StartXMm = 0.0,
                LengthMm = frame.LengthMm,
                IsTopBar = false,
                LayerIndex = 1,
                ElevationZMm = 30.0,
                ShapeName = "dang01"
            });

            if (botOpt.SecondaryCount > 0 && botOpt.SecondaryDiameterMm > 0)
            {
                layout.BottomContinuousBars.Add(new RebarRunModel
                {
                    Mark = "2B",
                    DiameterMm = botOpt.SecondaryDiameterMm,
                    BarCount = botOpt.SecondaryCount,
                    StartXMm = 0.0,
                    LengthMm = frame.LengthMm,
                    IsTopBar = false,
                    LayerIndex = botOpt.LayerCount > 1 ? 2 : 1,
                    ElevationZMm = botOpt.LayerCount > 1 ? 80.0 : 30.0,
                    ShapeName = "dang01"
                });
            }
        }
    }
}
