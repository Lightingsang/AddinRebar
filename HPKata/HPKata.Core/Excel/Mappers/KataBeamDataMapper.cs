using System;
using System.Collections.Generic;
using HPKata.Core.Beams.Calculators;
using HPKata.Core.Beams.Models;
using HPKata.Core.Excel.Data;
using HPKata.Core.Excel.ReadersWriters;

namespace HPKata.Core.Excel.Mappers
{
    /// <summary>
    /// Bi-directional mapper between KataBeamSheetData and BeamRebarLayout.
    /// Preserves all Kata.xlsm sheet 'Dam' curtailment bars, stirrup zones, skin bars, elevation steps, and support axes.
    /// </summary>
    public static class KataBeamDataMapper
    {
        public static BeamRebarLayout ToRebarLayout(KataBeamSheetData sheetData)
        {
            if (sheetData == null) throw new ArgumentNullException(nameof(sheetData));

            var layout = new BeamRebarLayout { BeamName = sheetData.BeamMark, SheetData = sheetData };
            double totalLength = MapSpansAndStirrupZones(sheetData, layout);
            layout.TotalLengthMm = totalLength;

            MapSupportsAndTopCurtailment(sheetData, layout);
            ParseContinuousRebar(sheetData.TopContinuousRebar, totalLength, isTop: true, layout.TopContinuousBars);
            ParseContinuousRebar(sheetData.BotContinuousRebar, totalLength, isTop: false, layout.BottomContinuousBars);

            if (sheetData.HasSideBars && sheetData.SkinBarLayers > 0 && sheetData.SkinBarDiameterMm > 0)
            {
                layout.SkinBars.Add(new RebarRunModel
                {
                    Mark = "G1", DiameterMm = sheetData.SkinBarDiameterMm, BarCount = sheetData.SkinBarLayers * 2,
                    StartXMm = 0.0, LengthMm = totalLength, IsTopBar = false
                });
            }
            return layout;
        }

        private static double MapSpansAndStirrupZones(KataBeamSheetData sheetData, BeamRebarLayout layout)
        {
            double totalLength = 0.0, curX = 0.0;
            for (int i = 0; i < sheetData.Spans.Count; i++)
            {
                var s = sheetData.Spans[i];
                if (i == 0 && s.LeftSupportWidthMm > 0) curX += s.LeftSupportWidthMm;

                double slabH = s.SlabThicknessOverrideMm > 0 ? s.SlabThicknessOverrideMm : sheetData.SlabThicknessMm;
                var spanModel = new BeamSpanModel
                {
                    SpanIndex = s.SpanIndex, SpanName = s.SpanName, ClearLengthMm = s.ClearLengthMm,
                    WidthMm = s.WidthMm, HeightMm = s.HeightMm, TopElevationDeltaMm = s.TopElevationDeltaMm,
                    SoffitDropMm = s.BottomStepUpMm, LeftSlabThicknessMm = slabH, RightSlabThicknessMm = slabH,
                    LeftSupportWidthMm = s.LeftSupportWidthMm, RightSupportWidthMm = s.RightSupportWidthMm,
                    IsCantileverLeft = s.IsCantileverLeft, IsCantileverRight = s.IsCantileverRight
                };
                layout.Spans.Add(spanModel);
                totalLength += spanModel.ClearLengthMm;

                MapSpanBottomCurtailment(s, sheetData, curX, layout);
                MapSpanStirrupZone(s, sheetData, i, layout);
                curX += s.ClearLengthMm + Math.Max(0.0, s.RightSupportWidthMm);
            }
            return totalLength;
        }

        private static void MapSpanBottomCurtailment(
            KataBeamSpanData s, KataBeamSheetData sheetData, double spanStartX, BeamRebarLayout layout)
        {
            var layers = new List<ExtraBarLayerModel>();
            double ratio = sheetData.BotCurtailmentRatio > 0 ? sheetData.BotCurtailmentRatio : 0.25;
            double cutL = s.IsCantileverLeft ? 0.0 : Math.Round((s.ClearLengthMm * ratio) / 50.0) * 50.0;
            double cutR = s.IsCantileverRight ? 0.0 : Math.Round((s.ClearLengthMm * ratio) / 50.0) * 50.0;

            TryAddExtraLayer(s.BotCurtailmentLayer1, 1, spanStartX, spanStartX + s.ClearLengthMm, cutL, cutR, layers);
            TryAddExtraLayer(s.BotCurtailmentLayer2, 2, spanStartX + cutL, spanStartX + s.ClearLengthMm - cutR, cutL, cutR, layers);
            TryAddExtraLayer(s.BotCurtailmentLayer3, 3, spanStartX + cutL, spanStartX + s.ClearLengthMm - cutR, cutL, cutR, layers);
            if (layers.Count > 0) layout.MidspanBottomExtraBars[s.SpanIndex] = layers;
        }

        private static void MapSpanStirrupZone(
            KataBeamSpanData s, KataBeamSheetData sheetData, int index, BeamRebarLayout layout)
        {
            double dense = sheetData.DenseSpacing > 0 ? sheetData.DenseSpacing : 150.0;
            double sparse = sheetData.SparseSpacing > 0 ? sheetData.SparseSpacing : 200.0;
            if (!string.IsNullOrWhiteSpace(s.StirrupOverride))
                dense = sparse = KataBeamSheetReader.ParseSpacingMm(s.StirrupOverride, dense);
            if (s.IsCantileverLeft || s.IsCantileverRight)
                dense = sparse = KataBeamSheetReader.ParseSpacingMm(sheetData.StirrupCantilever, dense);

            var zone = BeamStirrupZoneCalculator.CalculateZones(
                index, s.ClearLengthMm, s.WidthMm, s.HeightMm, sheetData.StirrupDiameterMm, dense, sparse);
            zone.TotalLegCount = Math.Max(2, sheetData.LegCount);
            layout.StirrupZones[index] = zone;
        }

        private static void MapSupportsAndTopCurtailment(KataBeamSheetData sheetData, BeamRebarLayout layout)
        {
            double curX = 0.0;
            for (int i = 0; i < sheetData.Supports.Count; i++)
            {
                var sp = sheetData.Supports[i];
                string axisName = !string.IsNullOrWhiteSpace(sp.ColumnGridAxis) ? sp.ColumnGridAxis : (i + 1).ToString();
                layout.Supports.Add(new BeamSupportModel
                {
                    SupportIndex = sp.Index, SupportName = sp.Name, ConnectingColumnName = axisName,
                    WidthMm = sp.WidthMm, CoordinateXMm = curX + (sp.WidthMm / 2.0)
                });

                double leftSpanL = (i > 0 && i - 1 < sheetData.Spans.Count) ? sheetData.Spans[i - 1].ClearLengthMm : 0.0;
                double rightSpanL = (i < sheetData.Spans.Count) ? sheetData.Spans[i].ClearLengthMm : 0.0;
                double maxAdjSpan = Math.Max(leftSpanL, rightSpanL);
                double ratio = sheetData.TopCurtailmentRatio > 0 ? sheetData.TopCurtailmentRatio : 0.25;
                double ext = Math.Max(sheetData.TopCurtailmentExtensionMm, Math.Round((maxAdjSpan * ratio) / 50.0) * 50.0);

                double leftExt = i > 0 ? ext : 0.0;
                double rightExt = i < sheetData.Spans.Count ? ext : 0.0;

                var layers = new List<ExtraBarLayerModel>();
                TryAddExtraLayer(sp.TopCurtailmentLayer1, 1, curX - leftExt, curX + sp.WidthMm + rightExt, leftExt, rightExt, layers);
                TryAddExtraLayer(sp.TopCurtailmentLayer2, 2, curX - leftExt, curX + sp.WidthMm + rightExt, leftExt, rightExt, layers);
                TryAddExtraLayer(sp.TopCurtailmentLayer3, 3, curX - (leftExt * 0.75), curX + sp.WidthMm + (rightExt * 0.75), leftExt * 0.75, rightExt * 0.75, layers);
                if (layers.Count > 0) layout.SupportTopExtraBars[i] = layers;

                curX += Math.Max(0.0, sp.WidthMm);
                if (i < sheetData.Spans.Count) curX += sheetData.Spans[i].ClearLengthMm;
            }
        }

        private static void TryAddExtraLayer(
            string spec, int layerIndex, double startX, double endX, double cutL, double cutR, List<ExtraBarLayerModel> list)
        {
            if (string.IsNullOrWhiteSpace(spec) || spec.Trim() == "-") return;
            ParseBarSpec(spec, out int cnt, out int dia);
            if (cnt <= 0 || dia <= 0) return;

            list.Add(new ExtraBarLayerModel
            {
                LayerIndex = layerIndex, BarCount = cnt, DiameterMm = dia, RawSpec = spec.Trim(),
                StartStationXMm = startX, EndStationXMm = endX, CutoffLeftMm = cutL, CutoffRightMm = cutR,
                HasLeftHook = cutL <= 0.0, HasRightHook = cutR <= 0.0
            });
        }

        public static KataBeamSheetData ToSheetData(BeamRebarLayout layout)
        {
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            if (layout.SheetData != null) return layout.SheetData;

            var data = new KataBeamSheetData { BeamMark = layout.BeamName };
            foreach (var span in layout.Spans)
            {
                data.Spans.Add(new KataBeamSpanData
                {
                    SpanIndex = span.SpanIndex, SpanName = span.SpanName, ClearLengthMm = span.ClearLengthMm,
                    WidthMm = span.WidthMm, HeightMm = span.HeightMm,
                    LeftSupportWidthMm = span.LeftSupportWidthMm, RightSupportWidthMm = span.RightSupportWidthMm,
                    IsCantileverLeft = span.IsCantileverLeft, IsCantileverRight = span.IsCantileverRight
                });
            }
            foreach (var sup in layout.Supports)
            {
                data.Supports.Add(new KataBeamSupportData
                {
                    Index = sup.SupportIndex, Name = sup.SupportName,
                    WidthMm = sup.WidthMm, ColumnGridAxis = sup.ConnectingColumnName
                });
            }
            MapContinuousBars(layout, data);
            return data;
        }

        private static void MapContinuousBars(BeamRebarLayout layout, KataBeamSheetData data)
        {
            if (layout.TopContinuousBars.Count > 0)
            {
                var top = layout.TopContinuousBars[0];
                data.TopContinuousRebar = $"{top.BarCount}f{top.DiameterMm}";
                data.TopContinuousCount = top.BarCount;
                data.TopContinuousDiameter = top.DiameterMm;
            }
            if (layout.BottomContinuousBars.Count > 0)
            {
                var bot = layout.BottomContinuousBars[0];
                data.BotContinuousRebar = $"{bot.BarCount}f{bot.DiameterMm}";
                data.BotContinuousCount = bot.BarCount;
                data.BotContinuousDiameter = bot.DiameterMm;
            }
        }

        public static void ParseBarSpec(string spec, out int count, out int diameter)
        {
            count = 0; diameter = 0;
            if (string.IsNullOrWhiteSpace(spec)) { count = 2; diameter = 20; return; }

            string firstPart = spec.Split(';')[0].Trim();
            if (firstPart == "-" || firstPart == "0") return;

            string s = firstPart.ToLowerInvariant();
            int fIdx = s.IndexOfAny(new[] { 'f', 'd', 'ø', 't', 'p' });
            count = (fIdx > 0 && int.TryParse(s.Substring(0, fIdx).Trim(), out int c)) ? c : 2;

            if (fIdx >= 0 && fIdx + 1 < s.Length)
            {
                string digits = string.Empty;
                foreach (char ch in s.Substring(fIdx + 1))
                {
                    if (char.IsDigit(ch)) digits += ch; else break;
                }
                if (int.TryParse(digits, out int d) && d > 0) diameter = d;
            }
            if (diameter <= 0) diameter = 20;
        }

        private static void ParseContinuousRebar(string spec, double length, bool isTop, List<RebarRunModel> list)
        {
            ParseBarSpec(spec, out int count, out int dia);
            list.Add(new RebarRunModel
            {
                Mark = isTop ? "1" : "2", DiameterMm = dia > 0 ? dia : 20, BarCount = count > 0 ? count : 2,
                StartXMm = 0.0, LengthMm = length, IsTopBar = isTop,
                HasStartHook = true, HasEndHook = true, StartHookLengthMm = 300.0, EndHookLengthMm = 300.0
            });
        }
    }
}
