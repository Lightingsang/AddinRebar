using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using HPKata.Core.Beams.Models;
using HPKata.Core.Excel.Data;
using HPKata.Core.Excel.Mappers;

namespace HPKata.AutoCad.Interop
{
    /// <summary>
    /// Renders all 2N parametric T-beam cross-sections (1-1..2N-2N) below the continuous beam elevation
    /// using Iso_kata.dwg layers, kata_block_THEP rebar dots, combing leaders, kata_block_KHT callouts, and kata_block_TD titles.
    /// </summary>
    internal static class BeamCrossSectionRenderer
    {
        public static void DrawAllCrossSections(
            BlockTableRecord ms, Transaction tr, Database db, BlockTable bt, ObjectId dimStyleId,
            Point3d origin, KataBeamSheetData data, BeamRebarLayout layout, double[] spanStartX)
        {
            double sectionTopY = origin.Y - data.DefaultHeightMm - 1950.0;

            for (int i = 0; i < data.Spans.Count && i < spanStartX.Length; i++)
            {
                var span = data.Spans[i];
                double xBase = spanStartX[i];
                double secSpacing = Math.Max(1400.0, span.ClearLengthMm * 0.45);

                int secSupNum = (i * 2) + 1;
                int secMidNum = (i * 2) + 2;

                DrawSingleCrossSection(
                    ms, tr, db, bt, dimStyleId,
                    new Point3d(xBase + 150.0, sectionTopY, 0),
                    data, layout, i, isSupportSection: true, $"{secSupNum}-{secSupNum}");

                DrawSingleCrossSection(
                    ms, tr, db, bt, dimStyleId,
                    new Point3d(xBase + 150.0 + Math.Min(1950.0, secSpacing), sectionTopY, 0),
                    data, layout, i, isSupportSection: false, $"{secMidNum}-{secMidNum}");
            }
        }

        private static void DrawSingleCrossSection(
            BlockTableRecord ms, Transaction tr, Database db, BlockTable bt, ObjectId dimStyleId,
            Point3d secOrigin, KataBeamSheetData data, BeamRebarLayout layout,
            int spanIdx, bool isSupportSection, string sectionLabel)
        {
            var span = data.Spans[spanIdx];
            double b = Math.Max(200.0, span.WidthMm);
            double h = Math.Max(300.0, span.HeightMm);
            double hs = span.SlabThicknessOverrideMm > 0 ? span.SlabThicknessOverrideMm : data.SlabThicknessMm;
            double flangeExt = 150.0;
            double cover = Math.Max(25.0, data.ConcreteCoverMm);

            DrawTBeamOutlineAndStirrups(ms, tr, db, secOrigin, b, h, hs, flangeExt, cover, data.LegCount);
            DrawSectionDimensionsAndTitle(ms, tr, bt, dimStyleId, secOrigin, b, h, hs, flangeExt, sectionLabel, data.SectionScale);
            DrawSectionRebarAndCallouts(ms, tr, db, bt, secOrigin, b, h, cover, data, layout, spanIdx, isSupportSection);
        }

        private static void DrawTBeamOutlineAndStirrups(
            BlockTableRecord ms, Transaction tr, Database db, Point3d secOrigin,
            double b, double h, double hs, double flangeExt, double cover, int legCount)
        {
            double x0 = secOrigin.X;
            double y0 = secOrigin.Y;

            if (hs > 0 && hs < h)
            {
                BeamElevationRenderer.DrawLine(ms, tr, db, x0 - flangeExt, y0, x0 + b + flangeExt, y0, KataLayerHelper.NetThay);
                BeamElevationRenderer.DrawLine(ms, tr, db, x0 - flangeExt, y0 - hs, x0, y0 - hs, KataLayerHelper.NetThay);
                BeamElevationRenderer.DrawLine(ms, tr, db, x0 + b, y0 - hs, x0 + b + flangeExt, y0 - hs, KataLayerHelper.NetThay);
                BeamElevationRenderer.DrawLine(ms, tr, db, x0, y0 - hs, x0, y0 - h, KataLayerHelper.NetThay);
                BeamElevationRenderer.DrawLine(ms, tr, db, x0, y0 - h, x0 + b, y0 - h, KataLayerHelper.NetThay);
                BeamElevationRenderer.DrawLine(ms, tr, db, x0 + b, y0 - h, x0 + b, y0 - hs, KataLayerHelper.NetThay);

                DrawVerticalBreakLine(ms, tr, db, x0 - flangeExt, y0 + 50.0, y0 - hs - 50.0);
                DrawVerticalBreakLine(ms, tr, db, x0 + b + flangeExt, y0 + 50.0, y0 - hs - 50.0);
            }
            else
            {
                var box = BeamElevationRenderer.CreateRectPolyline(x0, y0 - h, b, h);
                KataEntityStyler.StyleAndTag(box, ms, tr, db, KataLayerHelper.NetThay);
            }

            var stirrup = BeamElevationRenderer.CreateRectPolyline(
                x0 + cover, y0 - h + cover, b - (2.0 * cover), h - (2.0 * cover));
            KataEntityStyler.StyleAndTag(stirrup, ms, tr, db, KataLayerHelper.ThepDai);

            if (legCount >= 4 && b >= 350.0)
            {
                double innerW = (b - (2.0 * cover)) / 3.0;
                var innerStir = BeamElevationRenderer.CreateRectPolyline(
                    x0 + cover + innerW, y0 - h + cover, innerW, h - (2.0 * cover));
                KataEntityStyler.StyleAndTag(innerStir, ms, tr, db, KataLayerHelper.ThepDai);
            }
        }

        private static void DrawVerticalBreakLine(
            BlockTableRecord ms, Transaction tr, Database db, double x, double yTop, double yBot)
        {
            double midY = (yTop + yBot) / 2.0;
            var pl = new Polyline(6);
            pl.AddVertexAt(0, new Point2d(x, yTop), 0, 0, 0);
            pl.AddVertexAt(1, new Point2d(x, midY + 25.0), 0, 0, 0);
            pl.AddVertexAt(2, new Point2d(x - 35.0, midY), 0, 0, 0);
            pl.AddVertexAt(3, new Point2d(x + 35.0, midY), 0, 0, 0);
            pl.AddVertexAt(4, new Point2d(x, midY - 25.0), 0, 0, 0);
            pl.AddVertexAt(5, new Point2d(x, yBot), 0, 0, 0);
            KataEntityStyler.StyleAndTag(pl, ms, tr, db, KataLayerHelper.Dim);
        }

        private static void DrawSectionDimensionsAndTitle(
            BlockTableRecord ms, Transaction tr, BlockTable bt, ObjectId dimStyleId,
            Point3d secOrigin, double b, double h, double hs, double flangeExt, string sectionLabel, string sectionScale)
        {
            double x0 = secOrigin.X;
            double y0 = secOrigin.Y;

            BeamElevationRenderer.AddRotatedDim(ms, tr, dimStyleId, 0.0,
                new Point3d(x0, y0 - h, 0), new Point3d(x0 + b, y0 - h, 0),
                new Point3d(x0 + (b / 2.0), y0 - h - 175.0, 0));

            BeamElevationRenderer.AddRotatedDim(ms, tr, dimStyleId, Math.PI / 2.0,
                new Point3d(x0 - flangeExt, y0 - h, 0), new Point3d(x0 - flangeExt, y0, 0),
                new Point3d(x0 - flangeExt - 175.0, y0 - (h / 2.0), 0));

            if (hs > 0 && hs < h)
            {
                BeamElevationRenderer.AddRotatedDim(ms, tr, dimStyleId, Math.PI / 2.0,
                    new Point3d(x0 + b + flangeExt, y0 - hs, 0), new Point3d(x0 + b + flangeExt, y0, 0),
                    new Point3d(x0 + b + flangeExt + 150.0, y0 - (hs / 2.0), 0));
            }

            BeamElevationRenderer.InsertBlockWithAttributes(ms, tr, bt, "kata_block_TD",
                new Point3d(x0 + (b / 2.0), y0 - h - 380.0, 0), 25.0, KataLayerHelper.Text,
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["SECTDET1"] = $"%%uMẶT CẮT {sectionLabel}",
                    ["SECTDET2"] = string.Empty,
                    ["SSCALE"] = $"TL: {sectionScale}",
                    ["TT1"] = sectionLabel,
                    ["TL1"] = sectionScale
                });
        }

        private static void DrawSectionRebarAndCallouts(
            BlockTableRecord ms, Transaction tr, Database db, BlockTable bt,
            Point3d secOrigin, double b, double h, double cover,
            KataBeamSheetData data, BeamRebarLayout layout, int spanIdx, bool isSupportSection)
        {
            var span = data.Spans[spanIdx];
            string topSpec = !string.IsNullOrWhiteSpace(span.TopMainRebarOverride) ? span.TopMainRebarOverride : data.TopContinuousRebar;
            string botSpec = !string.IsNullOrWhiteSpace(span.BotMainRebarOverride) ? span.BotMainRebarOverride : data.BotContinuousRebar;

            KataBeamDataMapper.ParseBarSpec(topSpec, out int topCnt, out int topDia);
            KataBeamDataMapper.ParseBarSpec(botSpec, out int botCnt, out int botDia);

            double xLeft = secOrigin.X + cover + 18.0;
            double xRight = secOrigin.X + b - cover - 18.0;
            double yTop1 = secOrigin.Y - cover - 18.0;
            double yBot1 = secOrigin.Y - h + cover + 18.0;

            PlaceRebarLayerWithCallout(ms, tr, db, bt, xLeft, xRight, yTop1, topCnt, topDia, "1", secOrigin.X + b + 240.0, yTop1 + 80.0);
            PlaceRebarLayerWithCallout(ms, tr, db, bt, xLeft, xRight, yBot1, botCnt, botDia, "2", secOrigin.X + b + 240.0, yBot1 - 80.0);

            if (isSupportSection && spanIdx < data.Supports.Count)
            {
                var sup = data.Supports[spanIdx];
                TryDrawExtraSectionLayer(ms, tr, db, bt, sup.TopCurtailmentLayer1, xLeft, xRight, yTop1 - 45.0, "3", secOrigin.X + b + 240.0, yTop1 - 20.0);
                TryDrawExtraSectionLayer(ms, tr, db, bt, sup.TopCurtailmentLayer2, xLeft, xRight, yTop1 - 90.0, "4", secOrigin.X + b + 240.0, yTop1 - 110.0);
            }
            else if (!isSupportSection)
            {
                TryDrawExtraSectionLayer(ms, tr, db, bt, span.BotCurtailmentLayer1, xLeft, xRight, yBot1 + 45.0, "5", secOrigin.X + b + 240.0, yBot1 + 20.0);
                TryDrawExtraSectionLayer(ms, tr, db, bt, span.BotCurtailmentLayer2, xLeft, xRight, yBot1 + 90.0, "6", secOrigin.X + b + 240.0, yBot1 + 110.0);
            }

            DrawSectionSkinBars(ms, tr, db, bt, secOrigin, b, h, xLeft, xRight, data, span);

            layout.StirrupZones.TryGetValue(spanIdx, out var zone);
            double sp = isSupportSection
                ? (zone?.LeftDenseSpacingMm ?? data.DenseSpacing)
                : (zone?.MidspanSparseSpacingMm ?? data.SparseSpacing);
            BeamShopDrawingBuilder.DrawLeaderAndKht(
                ms, tr, db, bt,
                secOrigin.X + b - cover, secOrigin.Y - (h * 0.50),
                secOrigin.X + b + 240.0, secOrigin.Y - (h * 0.50),
                "Đ", $"%%c{data.StirrupDiameterMm}a{(int)Math.Round(sp)}", string.Empty);
        }

        private static void DrawSectionSkinBars(
            BlockTableRecord ms, Transaction tr, Database db, BlockTable bt,
            Point3d secOrigin, double b, double h, double xLeft, double xRight,
            KataBeamSheetData data, KataBeamSpanData span)
        {
            int layers = data.SkinBarLayers;
            int dia = data.SkinBarDiameterMm;
            if (!string.IsNullOrWhiteSpace(span.SkinBarSpec) && span.SkinBarSpec.IndexOf('f') >= 0)
            {
                KataBeamDataMapper.ParseBarSpec(span.SkinBarSpec, out layers, out dia);
            }
            if (layers <= 0 || dia <= 0 || h < 500.0) return;

            for (int r = 1; r <= layers; r++)
            {
                double y = secOrigin.Y - ((h * r) / (layers + 1.0));
                InsertRebarDot(ms, tr, bt, xLeft, y);
                InsertRebarDot(ms, tr, bt, xRight, y);
                BeamElevationRenderer.DrawLine(ms, tr, db, xLeft, y, xRight, y, KataLayerHelper.ThepDai);
            }
        }

        private static void TryDrawExtraSectionLayer(
            BlockTableRecord ms, Transaction tr, Database db, BlockTable bt,
            string spec, double xLeft, double xRight, double y, string mark, double calloutX, double calloutY)
        {
            if (string.IsNullOrWhiteSpace(spec) || spec.Trim() == "-") return;
            KataBeamDataMapper.ParseBarSpec(spec, out int cnt, out int dia);
            if (cnt <= 0 || dia <= 0) return;
            PlaceRebarLayerWithCallout(ms, tr, db, bt, xLeft, xRight, y, cnt, dia, mark, calloutX, calloutY);
        }

        private static void PlaceRebarLayerWithCallout(
            BlockTableRecord ms, Transaction tr, Database db, BlockTable bt,
            double xLeft, double xRight, double y, int count, int dia, string stt, double calloutX, double calloutY)
        {
            int n = Math.Max(2, count);
            for (int i = 0; i < n; i++)
            {
                double x = xLeft + ((xRight - xLeft) * i / (n - 1.0));
                InsertRebarDot(ms, tr, bt, x, y);
            }

            BeamShopDrawingBuilder.DrawLeaderAndKht(
                ms, tr, db, bt, xLeft, y, calloutX, calloutY, stt, $"{count}%%c{dia}", string.Empty);
        }

        private static void InsertRebarDot(
            BlockTableRecord ms, Transaction tr, BlockTable bt, double x, double y)
        {
            if (!bt.Has("kata_block_THEP")) return;
            var br = new BlockReference(new Point3d(x, y, 0), bt["kata_block_THEP"])
            {
                ScaleFactors = new Scale3d(2.0, 2.0, 2.0),
                Layer = KataLayerHelper.ThepDoc
            };
            ms.AppendEntity(br);
            tr.AddNewlyCreatedDBObject(br, true);
        }
    }
}
