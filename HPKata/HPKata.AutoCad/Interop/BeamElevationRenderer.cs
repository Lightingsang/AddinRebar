using System;
using System.Collections.Generic;
using System.Globalization;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using HPKata.Core.Beams.Models;
using HPKata.Core.Excel.Data;
using HPKata.Core.Serialization;

namespace HPKata.AutoCad.Interop
{
    /// <summary>
    /// Renders the parametric KataPro continuous beam concrete elevation, support columns, grid axes,
    /// slab/transverse beam hidden lines, 3-zone stirrups, dimensions, and section cut arrows on Iso_kata.dwg layers.
    /// </summary>
    internal static class BeamElevationRenderer
    {
        public static void DrawConcreteSupportsAndStirrups(
            BlockTableRecord ms, Transaction tr, Database db, BlockTable bt,
            ObjectId dimStyleId, ObjectId dimRaiId, Point3d origin,
            KataBeamSheetData data, BeamRebarLayout layout,
            double[] supLeftX, double[] supRightX, double[] spanStartX, double[] spanEndX,
            ref short nextStt)
        {
            DrawSupportsAndGrids(ms, tr, db, bt, dimStyleId, origin, data, supLeftX, supRightX);
            DrawSpansConcreteAndStirrups(
                ms, tr, db, bt, dimStyleId, dimRaiId, origin, data, layout, spanStartX, spanEndX, ref nextStt);

            double totalExtent = spanEndX.Length > 0
                ? (supRightX.Length > spanEndX.Length ? supRightX[supRightX.Length - 1] : spanEndX[spanEndX.Length - 1]) - origin.X
                : layout.TotalLengthMm;

            InsertBlockWithAttributes(ms, tr, bt, "kata_block_TD",
                new Point3d(origin.X + (totalExtent * 0.42), origin.Y - data.DefaultHeightMm - 1150.0, 0),
                25.0, KataLayerHelper.Text,
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["SECTDET1"] = $"%%u{data.BeamMark} (SL={data.MemberCount}; L={(int)Math.Round(totalExtent)})",
                    ["SECTDET2"] = string.Empty,
                    ["SSCALE"] = $"TL: {data.DrawingScale}",
                    ["TT1"] = $"DẦM {data.BeamMark}",
                    ["TL1"] = data.DrawingScale
                });
        }

        private static void DrawSupportsAndGrids(
            BlockTableRecord ms, Transaction tr, Database db, BlockTable bt, ObjectId dimStyleId,
            Point3d origin, KataBeamSheetData data, double[] supLeftX, double[] supRightX)
        {
            double baseTopY = origin.Y, baseBotY = origin.Y - data.DefaultHeightMm;
            for (int i = 0; i < data.Supports.Count && i < supLeftX.Length; i++)
            {
                var sup = data.Supports[i];
                double xL = supLeftX[i], xR = supRightX[i], w = xR - xL;
                double topY = GetSupportTopY(origin.Y, data, i), botY = GetSupportBotY(origin.Y, data, i);

                if (w > 0.0)
                {
                    DrawLine(ms, tr, db, xL, botY - 400.0, xL, botY, KataLayerHelper.NetThay);
                    DrawLine(ms, tr, db, xR, botY - 400.0, xR, botY, KataLayerHelper.NetThay);
                    DrawLine(ms, tr, db, xL, topY, xL, topY + 400.0, KataLayerHelper.NetThay);
                    DrawLine(ms, tr, db, xR, topY, xR, topY + 400.0, KataLayerHelper.NetThay);
                    DrawBreakLine(ms, tr, db, xL - 75.0, xR + 75.0, topY + 400.0);
                    DrawBreakLine(ms, tr, db, xL - 75.0, xR + 75.0, botY - 400.0);
                    AddRotatedDim(ms, tr, dimStyleId, 0.0, new Point3d(xL, baseBotY, 0),
                        new Point3d(xR, baseBotY, 0), new Point3d((xL + xR) / 2.0, baseBotY - 825.0, 0));
                }

                DrawTransverseBeamHiddenBox(ms, tr, db, sup, xL, w, topY);
                string axis = !string.IsNullOrWhiteSpace(sup.ColumnGridAxis) ? sup.ColumnGridAxis : (i + 1).ToString();
                if (w > 0.0 || !string.IsNullOrWhiteSpace(sup.ColumnGridAxis))
                {
                    double gridX = xL + (w / 2.0) + sup.ColumnGridOffsetMm;
                    var gridLine = new Line(new Point3d(gridX, baseBotY - 950.0, 0), new Point3d(gridX, baseTopY + 1250.0, 0));
                    KataEntityStyler.StyleAndTag(gridLine, ms, tr, db, KataLayerHelper.Grid);
                    gridLine.LinetypeScale = 12.5;
                    InsertBlockWithAttributes(ms, tr, bt, "kata_block_GRID",
                        new Point3d(gridX, baseBotY - 1125.0, 0), 25.0, KataLayerHelper.Dim,
                        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["GRID"] = axis, ["TENTT"] = axis });
                }
            }
        }

        private static void DrawTransverseBeamHiddenBox(
            BlockTableRecord ms, Transaction tr, Database db, KataBeamSupportData sup, double xL, double supW, double topY)
        {
            if (string.IsNullOrWhiteSpace(sup.TransverseBeamSize)) return;
            string[] parts = sup.TransverseBeamSize.ToLowerInvariant().Split('x');
            if (parts.Length < 2) return;
            if (!double.TryParse(parts[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double bTrans)) return;
            if (!double.TryParse(parts[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double hTrans)) return;
            if (bTrans <= 0 || hTrans <= 0) return;

            double boxX = supW > 0 ? xL + ((supW - bTrans) / 2.0) + sup.TransverseBeamOffsetMm : xL - (bTrans / 2.0);
            var pl = CreateRectPolyline(boxX, topY - hTrans, bTrans, hTrans);
            KataEntityStyler.StyleAndTag(pl, ms, tr, db, KataLayerHelper.NetKhuat);
            pl.LinetypeScale = 18.75;
        }

        private static void DrawSpansConcreteAndStirrups(
            BlockTableRecord ms, Transaction tr, Database db, BlockTable bt,
            ObjectId dimStyleId, ObjectId dimRaiId, Point3d origin,
            KataBeamSheetData data, BeamRebarLayout layout,
            double[] spanStartX, double[] spanEndX, ref short nextStt)
        {
            double baseBotY = origin.Y - data.DefaultHeightMm;
            double baseElevMeters = ParseElevationMeters(data.ElevationString);

            for (int i = 0; i < data.Spans.Count && i < spanStartX.Length; i++)
            {
                var span = data.Spans[i];
                double x1 = spanStartX[i], x2 = spanEndX[i];
                double topY = origin.Y + span.TopElevationDeltaMm, botY = topY - span.HeightMm;
                double slabH = span.SlabThicknessOverrideMm > 0 ? span.SlabThicknessOverrideMm : data.SlabThicknessMm;

                DrawLine(ms, tr, db, x1, topY, x2, topY, KataLayerHelper.NetThay);
                DrawLine(ms, tr, db, x1, botY, x2, botY, KataLayerHelper.NetThay);
                if (span.IsCantileverRight) DrawLine(ms, tr, db, x2, botY, x2, topY, KataLayerHelper.NetThay);
                if (slabH > 0)
                {
                    var slabLine = new Line(new Point3d(x1, topY - slabH, 0), new Point3d(x2, topY - slabH, 0));
                    KataEntityStyler.StyleAndTag(slabLine, ms, tr, db, KataLayerHelper.NetKhuat);
                    slabLine.LinetypeScale = 18.75;
                }

                if (i == 0 || Math.Abs(span.TopElevationDeltaMm - data.Spans[i - 1].TopElevationDeltaMm) > 1.0)
                {
                    double elevM = baseElevMeters + (span.TopElevationDeltaMm / 1000.0);
                    string elevText = elevM >= 0 ? $"+{elevM:0.000}" : $"{elevM:0.000}";
                    InsertBlockWithAttributes(ms, tr, bt, "kata_block_CT",
                        new Point3d(x1 + (span.ClearLengthMm * 0.45), topY, 0), 25.0, KataLayerHelper.NetManh,
                        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["CT"] = elevText, ["CAODO"] = elevText });
                }

                AddRotatedDim(ms, tr, dimStyleId, 0.0, new Point3d(x1, baseBotY, 0), new Point3d(x2, baseBotY, 0),
                    new Point3d((x1 + x2) / 2.0, baseBotY - 825.0, 0));
                AddRotatedDim(ms, tr, dimStyleId, Math.PI / 2.0, new Point3d(x1 + 120.0, botY, 0), new Point3d(x1 + 120.0, topY, 0),
                    new Point3d(x1 + 320.0, (topY + botY) / 2.0, 0));

                DrawSpanStirrupZones(ms, tr, db, dimStyleId, dimRaiId, data, layout, i, x1, x2, topY, botY, baseBotY, ref nextStt);
                DrawSpanSectionMarkers(ms, tr, db, bt, x1, span.ClearLengthMm, topY, botY, (i * 2) + 1, (i * 2) + 2);
            }
        }

        private static void DrawSpanStirrupZones(
            BlockTableRecord ms, Transaction tr, Database db, ObjectId dimStyleId, ObjectId dimRaiId,
            KataBeamSheetData data, BeamRebarLayout layout, int spanIdx,
            double x1, double x2, double topY, double botY, double baseBotY, ref short nextStt)
        {
            double cover = Math.Max(20.0, data.ConcreteCoverMm);
            var span = data.Spans[spanIdx];
            layout.StirrupZones.TryGetValue(spanIdx, out var zone);

            double leftLen = zone != null ? zone.LeftDenseZoneLengthMm : Math.Round((span.ClearLengthMm * 0.25) / 50.0) * 50.0;
            double rightLen = zone != null ? zone.RightDenseZoneLengthMm : leftLen;
            double midLen = Math.Max(0.0, span.ClearLengthMm - leftLen - rightLen);
            double denseSp = zone != null ? zone.LeftDenseSpacingMm : data.DenseSpacing;
            double sparseSp = zone != null ? zone.MidspanSparseSpacingMm : data.SparseSpacing;

            short stirStt = nextStt++;
            if (span.IsCantileverRight || span.IsCantileverLeft || midLen < 300.0 || Math.Abs(denseSp - sparseSp) < 1.0)
            {
                DrawSingleStirrupZone(ms, tr, db, dimStyleId, dimRaiId, x1 + 50.0, x2 - 50.0, x1, x2,
                    topY, botY, baseBotY, cover, data.StirrupDiameterMm, denseSp, data.BeamMark, stirStt);
                return;
            }

            double z1End = x1 + leftLen, z2End = z1End + midLen;
            DrawSingleStirrupZone(ms, tr, db, dimStyleId, dimRaiId, x1 + 50.0, z1End, x1, z1End,
                topY, botY, baseBotY, cover, data.StirrupDiameterMm, denseSp, data.BeamMark, stirStt);
            DrawSingleStirrupZone(ms, tr, db, dimStyleId, dimRaiId, z1End + 50.0, z2End - 50.0, z1End, z2End,
                topY, botY, baseBotY, cover, data.StirrupDiameterMm, sparseSp, data.BeamMark, stirStt);
            DrawSingleStirrupZone(ms, tr, db, dimStyleId, dimRaiId, z2End, x2 - 50.0, z2End, x2,
                topY, botY, baseBotY, cover, data.StirrupDiameterMm, denseSp, data.BeamMark, stirStt);
        }

        private static void DrawSingleStirrupZone(
            BlockTableRecord ms, Transaction tr, Database db, ObjectId dimStyleId, ObjectId dimRaiId,
            double barX1, double barX2, double dimX1, double dimX2,
            double topY, double botY, double baseBotY, double cover,
            int stirDia, double spacing, string beamMark, short stt)
        {
            if (barX2 <= barX1) return;
            double stirH = Math.Max(100.0, (topY - botY) - (2.0 * cover));
            var xdata = new KataXDataRecord
            {
                Dang = "22", STT = stt, Phi = (short)stirDia,
                SoLuong = ((int)Math.Ceiling((barX2 - barX1) / Math.Max(50.0, spacing)) + 1).ToString(),
                KhoangCach = ((int)Math.Round(spacing)).ToString(),
                TenCauKien = beamMark, ChieuDaiCacDoanMm = new[] { stirH, 250.0 }
            };

            KataEntityStyler.StyleAndTag(new Line(new Point3d(barX1, botY + cover, 0), new Point3d(barX1, topY - cover, 0)), ms, tr, db, KataLayerHelper.ThepDai, xdata);
            KataEntityStyler.StyleAndTag(new Line(new Point3d(barX2, botY + cover, 0), new Point3d(barX2, topY - cover, 0)), ms, tr, db, KataLayerHelper.ThepDai, xdata);

            double midY = (topY + botY) / 2.0;
            var adim = new AlignedDimension(new Point3d(barX1, midY, 0), new Point3d(barX2, midY, 0), new Point3d((barX1 + barX2) / 2.0, midY, 0), " ", dimRaiId);
            KataEntityStyler.StyleAndTag(adim, ms, tr, db, "Defpoints");

            AddRotatedDim(ms, tr, dimStyleId, 0.0, new Point3d(dimX1, botY, 0), new Point3d(dimX2, botY, 0), new Point3d((dimX1 + dimX2) / 2.0, baseBotY - 525.0, 0));
            KataEntityStyler.StyleAndTag(new DBText
            {
                Position = new Point3d(((dimX1 + dimX2) / 2.0) - 110.0, baseBotY - 490.0, 0),
                TextString = $"%%c{stirDia}a{(int)Math.Round(spacing)}", Height = 50.0
            }, ms, tr, db, KataLayerHelper.Text);
        }

        private static void DrawSpanSectionMarkers(
            BlockTableRecord ms, Transaction tr, Database db, BlockTable bt,
            double x1, double spanLen, double topY, double botY, int secSup, int secMid)
        {
            DrawOneSectionCutMarker(ms, tr, db, bt, x1 + Math.Min(650.0, spanLen * 0.18), topY, botY, secSup.ToString());
            DrawOneSectionCutMarker(ms, tr, db, bt, x1 + (spanLen * 0.50), topY, botY, secMid.ToString());
        }

        private static void DrawOneSectionCutMarker(
            BlockTableRecord ms, Transaction tr, Database db, BlockTable bt, double x, double topY, double botY, string label)
        {
            var cutLine = new Line(new Point3d(x, botY - 125.0, 0), new Point3d(x, topY + 125.0, 0));
            KataEntityStyler.StyleAndTag(cutLine, ms, tr, db, KataLayerHelper.NetKhuat);
            cutLine.LinetypeScale = 12.5;
            var tags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["DETAIL"] = label, ["MC"] = label };
            InsertBlockWithAttributes(ms, tr, bt, "kata_block_SECBAL", new Point3d(x, topY + 125.0, 0), 25.0, KataLayerHelper.Text, tags);
            InsertBlockWithAttributes(ms, tr, bt, "kata_block_SECBAL", new Point3d(x, botY - 125.0, 0), 25.0, KataLayerHelper.Text, tags);
        }

        private static double GetSupportTopY(double baseOriginY, KataBeamSheetData data, int supIdx)
        {
            double leftTop = supIdx > 0 && supIdx - 1 < data.Spans.Count ? baseOriginY + data.Spans[supIdx - 1].TopElevationDeltaMm : double.MinValue;
            double rightTop = supIdx < data.Spans.Count ? baseOriginY + data.Spans[supIdx].TopElevationDeltaMm : double.MinValue;
            double maxTop = Math.Max(leftTop, rightTop);
            return maxTop > double.MinValue / 2.0 ? maxTop : baseOriginY;
        }

        private static double GetSupportBotY(double baseOriginY, KataBeamSheetData data, int supIdx)
        {
            double leftBot = supIdx > 0 && supIdx - 1 < data.Spans.Count ? baseOriginY + data.Spans[supIdx - 1].TopElevationDeltaMm - data.Spans[supIdx - 1].HeightMm : double.MaxValue;
            double rightBot = supIdx < data.Spans.Count ? baseOriginY + data.Spans[supIdx].TopElevationDeltaMm - data.Spans[supIdx].HeightMm : double.MaxValue;
            double minBot = Math.Min(leftBot, rightBot);
            return minBot < double.MaxValue / 2.0 ? minBot : baseOriginY - data.DefaultHeightMm;
        }

        private static double ParseElevationMeters(string elevStr) =>
            !string.IsNullOrWhiteSpace(elevStr) && double.TryParse(elevStr.Replace("+", string.Empty).Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double v) ? v : 3.3;

        internal static void DrawBreakLine(BlockTableRecord ms, Transaction tr, Database db, double x1, double x2, double y)
        {
            double mid = (x1 + x2) / 2.0;
            var pl = new Polyline(6);
            pl.AddVertexAt(0, new Point2d(x1, y), 0, 0, 0);
            pl.AddVertexAt(1, new Point2d(mid - 37.5, y), 0, 0, 0);
            pl.AddVertexAt(2, new Point2d(mid, y + 75.0), 0, 0, 0);
            pl.AddVertexAt(3, new Point2d(mid, y - 75.0), 0, 0, 0);
            pl.AddVertexAt(4, new Point2d(mid + 37.5, y), 0, 0, 0);
            pl.AddVertexAt(5, new Point2d(x2, y), 0, 0, 0);
            KataEntityStyler.StyleAndTag(pl, ms, tr, db, KataLayerHelper.Dim);
        }

        internal static void DrawLine(BlockTableRecord ms, Transaction tr, Database db, double x1, double y1, double x2, double y2, string layer) =>
            KataEntityStyler.StyleAndTag(new Line(new Point3d(x1, y1, 0), new Point3d(x2, y2, 0)), ms, tr, db, layer);

        internal static Polyline CreateRectPolyline(double x, double y, double w, double h)
        {
            var pl = new Polyline(4) { Closed = true };
            pl.AddVertexAt(0, new Point2d(x, y), 0, 0, 0);
            pl.AddVertexAt(1, new Point2d(x + w, y), 0, 0, 0);
            pl.AddVertexAt(2, new Point2d(x + w, y + h), 0, 0, 0);
            pl.AddVertexAt(3, new Point2d(x, y + h), 0, 0, 0);
            return pl;
        }

        internal static void AddRotatedDim(BlockTableRecord ms, Transaction tr, ObjectId styleId, double rot, Point3d p1, Point3d p2, Point3d linePt)
        {
            var rdim = new RotatedDimension(rot, p1, p2, linePt, string.Empty, styleId) { Layer = KataLayerHelper.Dim };
            ms.AppendEntity(rdim); tr.AddNewlyCreatedDBObject(rdim, true);
        }

        internal static void InsertBlockWithAttributes(
            BlockTableRecord ms, Transaction tr, BlockTable bt, string blockName, Point3d pos, double scale, string layer, Dictionary<string, string> tags)
        {
            if (!bt.Has(blockName)) return;
            ObjectId btrId = bt[blockName];
            var br = new BlockReference(pos, btrId) { ScaleFactors = new Scale3d(scale, scale, scale), Layer = layer };
            ms.AppendEntity(br); tr.AddNewlyCreatedDBObject(br, true);
            var btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);
            foreach (ObjectId id in btr)
            {
                if (tr.GetObject(id, OpenMode.ForRead) is not AttributeDefinition attDef || attDef.Constant) continue;
                var attRef = new AttributeReference();
                attRef.SetAttributeFromBlock(attDef, br.BlockTransform);
                if (tags.TryGetValue(attDef.Tag, out string? val)) attRef.TextString = val;
                br.AttributeCollection.AppendAttribute(attRef); tr.AddNewlyCreatedDBObject(attRef, true);
            }
        }
    }
}
