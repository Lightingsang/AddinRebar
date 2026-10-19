using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using HPKata.Core.Beams.Models;
using HPKata.Core.Excel.Data;
using HPKata.Core.Excel.Mappers;
using HPKata.Core.Serialization;

namespace HPKata.AutoCad.Interop
{
    /// <summary>
    /// Builds 100% parametric KataPro-compliant AutoCAD 2D beam shop drawings from BeamRebarLayout / KataBeamSheetData
    /// using Iso_kata.dwg layers, blocks (kata_block_TD, kata_block_SECBAL, kata_block_GRID, kata_block_KHT,
    /// kata_block_THEP, kata_block_CT), dimstyles (kata_dim_25, kata_rai_thep), and KATA XData.
    /// Responds dynamically to every parameter change in Kata.xlsm (Sheets["Dam"]).
    /// </summary>
    public static class BeamShopDrawingBuilder
    {
        public static void DrawCompleteBeamShopDrawing(
            BlockTableRecord ms, Transaction tr, Database db, Point3d origin, BeamRebarLayout layout, bool isShopMode)
        {
            if (layout == null || layout.Spans.Count == 0) return;

            KataIsoTemplateImporter.EnsureIsoTemplates(db, tr);
            XDataService.EnsureRegApp(db, KataXDataRecord.RegAppName);

            var data = KataBeamDataMapper.ToSheetData(layout);
            EnsureSupportsPopulated(data);
            BuildStationTable(data, origin.X, out var supLeftX, out var supRightX, out var spanStartX, out var spanEndX);

            var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            var dst = (DimStyleTable)tr.GetObject(db.DimStyleTableId, OpenMode.ForRead);
            ObjectId dim25 = dst.Has("kata_dim_25") ? dst["kata_dim_25"] : db.Dimstyle;
            ObjectId dimRai = dst.Has("kata_rai_thep") ? dst["kata_rai_thep"] : db.Dimstyle;

            short nextStt = 1;
            DrawMainAndCurtailmentBars(
                ms, tr, db, bt, dim25, origin, data, supLeftX, supRightX, spanStartX, spanEndX, ref nextStt);

            BeamElevationRenderer.DrawConcreteSupportsAndStirrups(
                ms, tr, db, bt, dim25, dimRai, origin, data, layout,
                supLeftX, supRightX, spanStartX, spanEndX, ref nextStt);

            BeamCrossSectionRenderer.DrawAllCrossSections(
                ms, tr, db, bt, dim25, origin, data, layout, spanStartX);
        }

        private static void EnsureSupportsPopulated(KataBeamSheetData data)
        {
            if (data.Supports.Count > 0) return;
            for (int i = 0; i <= data.Spans.Count; i++)
            {
                double w = i < data.Spans.Count ? data.Spans[i].LeftSupportWidthMm : data.Spans[data.Spans.Count - 1].RightSupportWidthMm;
                data.Supports.Add(new KataBeamSupportData
                {
                    Index = i, Name = $"Gối {i + 1}", WidthMm = w > 0 ? w : 400.0, ColumnGridAxis = (i + 1).ToString()
                });
            }
        }

        private static void BuildStationTable(
            KataBeamSheetData data, double startX,
            out double[] supLeftX, out double[] supRightX, out double[] spanStartX, out double[] spanEndX)
        {
            int nSpans = data.Spans.Count;
            int nSups = Math.Max(nSpans + 1, data.Supports.Count);
            supLeftX = new double[nSups];
            supRightX = new double[nSups];
            spanStartX = new double[nSpans];
            spanEndX = new double[nSpans];

            double curX = startX;
            for (int i = 0; i < nSpans; i++)
            {
                double leftW = i < data.Supports.Count ? Math.Max(0.0, data.Supports[i].WidthMm) : Math.Max(0.0, data.Spans[i].LeftSupportWidthMm);
                supLeftX[i] = curX;
                supRightX[i] = curX + leftW;
                curX += leftW;
                spanStartX[i] = curX;
                spanEndX[i] = curX + data.Spans[i].ClearLengthMm;
                curX = spanEndX[i];
            }

            int lastSup = nSpans;
            double lastW = lastSup < data.Supports.Count ? Math.Max(0.0, data.Supports[lastSup].WidthMm) : Math.Max(0.0, data.Spans[nSpans - 1].RightSupportWidthMm);
            supLeftX[lastSup] = curX;
            supRightX[lastSup] = curX + lastW;
        }

        private static void DrawMainAndCurtailmentBars(
            BlockTableRecord ms, Transaction tr, Database db, BlockTable bt, ObjectId dimStyleId,
            Point3d origin, KataBeamSheetData data,
            double[] supLeftX, double[] supRightX, double[] spanStartX, double[] spanEndX, ref short nextStt)
        {
            double cover = Math.Max(25.0, data.ConcreteCoverMm);
            double totalLeftX = supLeftX[0] + cover;
            double totalRightX = supRightX[data.Spans.Count] - cover;

            DrawContinuousMainBar(ms, tr, db, bt, origin, data, spanStartX, spanEndX, totalLeftX, totalRightX, isTop: true, nextStt++);
            DrawContinuousMainBar(ms, tr, db, bt, origin, data, spanStartX, spanEndX, totalLeftX, totalRightX, isTop: false, nextStt++);
            DrawTopSupportCurtailmentBars(ms, tr, db, bt, dimStyleId, origin, data, supLeftX, supRightX, ref nextStt);
            DrawBottomSpanCurtailmentBars(ms, tr, db, bt, dimStyleId, origin, data, spanStartX, spanEndX, ref nextStt);
            DrawWebSkinBars(ms, tr, db, bt, origin, data, spanStartX, spanEndX, ref nextStt);
        }

        private static void DrawContinuousMainBar(
            BlockTableRecord ms, Transaction tr, Database db, BlockTable bt,
            Point3d origin, KataBeamSheetData data, double[] spanStartX, double[] spanEndX,
            double xStart, double xEnd, bool isTop, short stt)
        {
            double cover = Math.Max(25.0, data.ConcreteCoverMm);
            KataBeamDataMapper.ParseBarSpec(isTop ? data.TopContinuousRebar : data.BotContinuousRebar, out int cnt, out int dia);
            double firstSpanY = GetMainBarY(origin.Y, data.Spans[0], cover, isTop);
            double lastSpanY = GetMainBarY(origin.Y, data.Spans[data.Spans.Count - 1], cover, isTop);
            double hookDy = isTop ? -Math.Max(200.0, 12.0 * dia) : Math.Max(200.0, 12.0 * dia);

            var pts = new List<Point2d> { new(xStart, firstSpanY + hookDy), new(xStart, firstSpanY) };
            for (int i = 0; i < data.Spans.Count; i++)
            {
                double y = GetMainBarY(origin.Y, data.Spans[i], cover, isTop);
                if (i > 0 && Math.Abs(y - pts[pts.Count - 1].Y) > 1.0)
                {
                    pts.Add(new Point2d(spanStartX[i], pts[pts.Count - 1].Y));
                    pts.Add(new Point2d(spanStartX[i], y));
                }
            }
            pts.Add(new Point2d(xEnd, lastSpanY));
            pts.Add(new Point2d(xEnd, lastSpanY + hookDy));
            AppendRebarPolyline(ms, tr, db, pts, "01", stt, dia, cnt, data.BeamMark);

            for (int i = 0; i < data.Spans.Count; i++)
            {
                var span = data.Spans[i];
                string ov = isTop ? span.TopMainRebarOverride : span.BotMainRebarOverride;
                int spanCnt = cnt, spanDia = dia;
                if (!string.IsNullOrWhiteSpace(ov)) KataBeamDataMapper.ParseBarSpec(ov, out spanCnt, out spanDia);
                double barY = GetMainBarY(origin.Y, span, cover, isTop);
                double calloutX = spanStartX[i] + (span.ClearLengthMm * 0.48);
                DrawLeaderAndKht(ms, tr, db, bt, calloutX - 180.0, barY, calloutX, isTop ? barY + 260.0 : barY - 260.0,
                    stt.ToString(), $"{spanCnt}%%c{spanDia}", string.Empty);
            }
        }

        private static void DrawTopSupportCurtailmentBars(
            BlockTableRecord ms, Transaction tr, Database db, BlockTable bt, ObjectId dimStyleId,
            Point3d origin, KataBeamSheetData data, double[] supLeftX, double[] supRightX, ref short nextStt)
        {
            double cover = Math.Max(25.0, data.ConcreteCoverMm);
            double ratio = data.TopCurtailmentRatio > 0 ? data.TopCurtailmentRatio : 0.25;
            for (int i = 0; i < data.Supports.Count && i < supLeftX.Length; i++)
            {
                var sup = data.Supports[i];
                string[] specs = { sup.TopCurtailmentLayer1, sup.TopCurtailmentLayer2, sup.TopCurtailmentLayer3 };
                double maxAdj = Math.Max(
                    i > 0 && i - 1 < data.Spans.Count ? data.Spans[i - 1].ClearLengthMm : 0.0,
                    i < data.Spans.Count ? data.Spans[i].ClearLengthMm : 0.0);
                double baseExt = Math.Max(data.TopCurtailmentExtensionMm, Math.Round((maxAdj * ratio) / 50.0) * 50.0);

                for (int layerIdx = 0; layerIdx < specs.Length; layerIdx++)
                {
                    if (string.IsNullOrWhiteSpace(specs[layerIdx]) || specs[layerIdx].Trim() == "-") continue;
                    KataBeamDataMapper.ParseBarSpec(specs[layerIdx], out int cnt, out int dia);
                    if (cnt <= 0 || dia <= 0) continue;

                    double factor = layerIdx == 2 ? 0.70 : 1.0;
                    double extL = i > 0 ? Math.Round((baseExt * factor) / 50.0) * 50.0 : 0.0;
                    double extR = i < data.Spans.Count ? Math.Round((baseExt * factor) / 50.0) * 50.0 : 0.0;
                    double x1 = extL > 0 ? supLeftX[i] - extL : supLeftX[i] + cover;
                    double x2 = extR > 0 ? supRightX[i] + extR : supRightX[i] - cover;
                    double topDelta = i < data.Spans.Count ? data.Spans[i].TopElevationDeltaMm : 0.0;
                    double y = origin.Y + topDelta - cover - ((layerIdx + 1) * 35.0);

                    var pts = new List<Point2d>();
                    if (extL <= 0) pts.Add(new Point2d(x1, y - (12.0 * dia)));
                    pts.Add(new Point2d(x1, y));
                    pts.Add(new Point2d(x2, y));
                    if (extR <= 0) pts.Add(new Point2d(x2, y - (12.0 * dia)));

                    short stt = nextStt++;
                    AppendRebarPolyline(ms, tr, db, pts, extL <= 0 || extR <= 0 ? "02" : "00", stt, dia, cnt, data.BeamMark);
                    double calloutX = extR > 0 ? supRightX[i] + (extR * 0.55) : supLeftX[i] - (extL * 0.55);
                    DrawLeaderAndKht(ms, tr, db, bt, calloutX - 120.0, y, calloutX, y + 210.0 + (layerIdx * 95.0),
                        stt.ToString(), $"{cnt}%%c{dia}", string.Empty);

                    if (extR > 0 && layerIdx == 1)
                        BeamElevationRenderer.AddRotatedDim(ms, tr, dimStyleId, 0.0,
                            new Point3d(supRightX[i], origin.Y + topDelta, 0), new Point3d(x2, origin.Y + topDelta, 0),
                            new Point3d((supRightX[i] + x2) / 2.0, origin.Y + topDelta + 380.0, 0));
                    if (extL > 0 && layerIdx == 1)
                        BeamElevationRenderer.AddRotatedDim(ms, tr, dimStyleId, 0.0,
                            new Point3d(x1, origin.Y + topDelta, 0), new Point3d(supLeftX[i], origin.Y + topDelta, 0),
                            new Point3d((x1 + supLeftX[i]) / 2.0, origin.Y + topDelta + 380.0, 0));
                }
            }
        }

        private static void DrawBottomSpanCurtailmentBars(
            BlockTableRecord ms, Transaction tr, Database db, BlockTable bt, ObjectId dimStyleId,
            Point3d origin, KataBeamSheetData data, double[] spanStartX, double[] spanEndX, ref short nextStt)
        {
            double cover = Math.Max(25.0, data.ConcreteCoverMm);
            double ratio = data.BotCurtailmentRatio > 0 ? data.BotCurtailmentRatio : 0.25;
            for (int i = 0; i < data.Spans.Count && i < spanStartX.Length; i++)
            {
                var span = data.Spans[i];
                string[] specs = { span.BotCurtailmentLayer1, span.BotCurtailmentLayer2, span.BotCurtailmentLayer3 };
                double botY = origin.Y + span.TopElevationDeltaMm - span.HeightMm;

                for (int layerIdx = 0; layerIdx < specs.Length; layerIdx++)
                {
                    if (string.IsNullOrWhiteSpace(specs[layerIdx]) || specs[layerIdx].Trim() == "-") continue;
                    KataBeamDataMapper.ParseBarSpec(specs[layerIdx], out int cnt, out int dia);
                    if (cnt <= 0 || dia <= 0) continue;

                    double cut = layerIdx == 0 ? 0.0 : Math.Round((span.ClearLengthMm * ratio) / 50.0) * 50.0;
                    double x1 = spanStartX[i] + (span.IsCantileverLeft ? cover : cut);
                    double x2 = spanEndX[i] - (span.IsCantileverRight ? cover : cut);
                    if (x2 <= x1 + 200.0) continue;

                    double y = botY + cover + ((layerIdx + 1) * 35.0);
                    short stt = nextStt++;
                    AppendRebarPolyline(ms, tr, db, new List<Point2d> { new(x1, y), new(x2, y) }, "00", stt, dia, cnt, data.BeamMark);

                    double calloutX = (x1 + x2) / 2.0;
                    DrawLeaderAndKht(ms, tr, db, bt, calloutX - 140.0, y, calloutX, botY - 190.0 - (layerIdx * 90.0),
                        stt.ToString(), $"{cnt}%%c{dia}", string.Empty);

                    if (cut > 0)
                        BeamElevationRenderer.AddRotatedDim(ms, tr, dimStyleId, 0.0,
                            new Point3d(spanStartX[i], botY, 0), new Point3d(x1, botY, 0),
                            new Point3d((spanStartX[i] + x1) / 2.0, botY - 260.0, 0));
                }
            }
        }

        private static void DrawWebSkinBars(
            BlockTableRecord ms, Transaction tr, Database db, BlockTable bt,
            Point3d origin, KataBeamSheetData data, double[] spanStartX, double[] spanEndX, ref short nextStt)
        {
            for (int i = 0; i < data.Spans.Count && i < spanStartX.Length; i++)
            {
                var span = data.Spans[i];
                int layers = data.SkinBarLayers, dia = data.SkinBarDiameterMm;
                if (!string.IsNullOrWhiteSpace(span.SkinBarSpec) && span.SkinBarSpec.IndexOf('f') >= 0)
                    KataBeamDataMapper.ParseBarSpec(span.SkinBarSpec, out layers, out dia);
                if (layers <= 0 || dia <= 0 || span.HeightMm < 500.0) continue;

                double topY = origin.Y + span.TopElevationDeltaMm, botY = topY - span.HeightMm;
                short stt = nextStt++;
                for (int r = 1; r <= layers; r++)
                {
                    double y = topY - ((span.HeightMm * r) / (layers + 1.0));
                    AppendRebarPolyline(ms, tr, db, new List<Point2d> { new(spanStartX[i] - 150.0, y), new(spanEndX[i] + 150.0, y) },
                        "00", stt, dia, 2, data.BeamMark);
                }
                double midY = (topY + botY) / 2.0, calloutX = spanStartX[i] + (span.ClearLengthMm * 0.32);
                DrawLeaderAndKht(ms, tr, db, bt, calloutX - 120.0, midY, calloutX, midY + 90.0,
                    stt.ToString(), $"{layers * 2}%%c{dia}", "(cốt giá)");
            }
        }

        private static double GetMainBarY(double originY, KataBeamSpanData span, double cover, bool isTop) =>
            isTop ? originY + span.TopElevationDeltaMm - cover : originY + span.TopElevationDeltaMm - span.HeightMm + cover;

        private static void AppendRebarPolyline(
            BlockTableRecord ms, Transaction tr, Database db, List<Point2d> pts,
            string dang, short stt, int dia, int count, string beamMark)
        {
            var pl = new Polyline(pts.Count);
            var segs = new List<double>();
            for (int i = 0; i < pts.Count; i++) { pl.AddVertexAt(i, pts[i], 0, 0, 0); if (i > 0) segs.Add(Math.Round(pts[i - 1].GetDistanceTo(pts[i]))); }
            KataEntityStyler.StyleAndTag(pl, ms, tr, db, KataLayerHelper.ThepDoc, new KataXDataRecord
            {
                Dang = dang, STT = stt, Phi = (short)dia, SoLuong = count.ToString(), TenCauKien = beamMark, ChieuDaiCacDoanMm = segs.ToArray()
            });
        }

        internal static void DrawLeaderAndKht(
            BlockTableRecord ms, Transaction tr, Database db, BlockTable bt,
            double targetX, double targetY, double calloutX, double calloutY, string sh, string sl, string kc)
        {
            var leader = new Leader();
            leader.AppendVertex(new Point3d(targetX, targetY, 0));
            leader.AppendVertex(new Point3d(calloutX, calloutY, 0));
            KataEntityStyler.StyleAndTag(leader, ms, tr, db, KataLayerHelper.NetManh);

            BeamElevationRenderer.InsertBlockWithAttributes(ms, tr, bt, "kata_block_KHT",
                new Point3d(calloutX, calloutY, 0), 25.0, KataLayerHelper.NetManh,
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["SH"] = sh, ["SH1"] = sh, ["SH2"] = sh, ["SH3"] = sh,
                    ["SL"] = sl, ["DK1"] = sl, ["DKKC1"] = string.IsNullOrEmpty(kc) ? sl : $"{sl} {kc}", ["KC"] = kc, ["KC1"] = kc
                });
        }
    }
}
