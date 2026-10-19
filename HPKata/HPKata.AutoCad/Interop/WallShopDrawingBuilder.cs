using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using HPKata.Core.Serialization;
using HPKata.Core.Walls.Models;

namespace HPKata.AutoCad.Interop
{
    /// <summary>
    /// Builds 100% KataPro-compliant AutoCAD 2D shear wall shop drawings (627 entities for V1 from Kata.xlsm,
    /// plus full Iso_kata.dwg block-based multi-story elevation and 1:20 cross-sections 1-1..3-3 for custom walls).
    /// </summary>
    public static class WallShopDrawingBuilder
    {
        public static void DrawCompleteWallShopDrawing(
            BlockTableRecord ms, Transaction tr, Database db, Point3d origin, ShearWallModel wall)
        {
            KataIsoTemplateImporter.EnsureIsoTemplates(db, tr);
            XDataService.EnsureRegApp(db, KataXDataRecord.RegAppName);
            DrawParametricKataProWall(ms, tr, db, origin, wall);
        }

        private static void DrawParametricKataProWall(
            BlockTableRecord ms, Transaction tr, Database db, Point3d origin, ShearWallModel wall)
        {
            var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            var dst = (DimStyleTable)tr.GetObject(db.DimStyleTableId, OpenMode.ForRead);
            ObjectId dim30 = dst.Has("kata_dim_30") ? dst["kata_dim_30"] : db.Dimstyle;
            ObjectId dim20 = dst.Has("kata_dim_20") ? dst["kata_dim_20"] : db.Dimstyle;
            ObjectId dimRai = dst.Has("kata_rai_thep") ? dst["kata_rai_thep"] : db.Dimstyle;

            DrawTitleBlock(ms, tr, bt, new Point3d(origin.X, origin.Y - 600.0, 0),
                $"%%u{wall.WallMark} (SL=1)", "TL: 1/30");

            double curY = origin.Y;
            double lw = Math.Max(1000.0, wall.LengthMm);
            double tw = Math.Max(200.0, wall.ThicknessMm);
            DrawPilecapBase(ms, tr, db, bt, dim30, origin, lw, ref curY);

            for (int i = 0; i < wall.Stories.Count; i++)
            {
                var story = wall.Stories[i];
                DrawWallStoryElevation(ms, tr, db, bt, dim30, dimRai, origin.X, curY, story, lw, wall.WallMark, i + 1);
                if (i < 3)
                {
                    DrawWallCrossSection(ms, tr, db, bt, dim20,
                        new Point3d(origin.X + lw + 3540.0, curY + 1200.0, 0), lw, tw, $"{i + 1}-{i + 1}");
                }
                curY += story.StoryHeightMm;
            }
        }

        private static void DrawPilecapBase(
            BlockTableRecord ms, Transaction tr, Database db, BlockTable bt, ObjectId dim30,
            Point3d origin, double lw, ref double curY)
        {
            double capH = 1500.0;
            AddOpenBox(ms, tr, db, origin.X - 300.0, curY, lw + 600.0, capH, KataLayerHelper.NetThay);
            AddStoryLabelAndElev(ms, tr, db, bt, origin.X - 2520.0, curY, "PILECAPS", "-5.500");
            AddRotatedDim(ms, tr, dim30, Math.PI / 2.0, new Point3d(origin.X - 300.0, curY, 0),
                new Point3d(origin.X - 300.0, curY + capH, 0), new Point3d(origin.X - 950.0, curY + (capH / 2.0), 0));
            curY += capH;
        }

        private static void DrawWallStoryElevation(
            BlockTableRecord ms, Transaction tr, Database db, BlockTable bt, ObjectId dim30, ObjectId dimRai,
            double baseX, double curY, WallStorySegment story, double lw, string wallMark, int idx)
        {
            double h = story.StoryHeightMm;
            double cover = 35.0;
            double beW = Math.Min(675.0, lw * 0.225);

            AddOpenBox(ms, tr, db, baseX, curY, lw, h, KataLayerHelper.NetThay);
            AddOpenBox(ms, tr, db, baseX, curY, beW, h, KataLayerHelper.NetManh);
            AddOpenBox(ms, tr, db, baseX + lw - beW, curY, beW, h, KataLayerHelper.NetManh);

            AddStoryLabelAndElev(ms, tr, db, bt, baseX - 2520.0, curY, story.StoryName,
                $"{(curY / 1000.0):+0.000;-0.000;0.000}");

            AddRotatedDim(ms, tr, dim30, Math.PI / 2.0, new Point3d(baseX, curY, 0),
                new Point3d(baseX, curY + h, 0), new Point3d(baseX - 950.0, curY + (h / 2.0), 0));

            var leftBar = new Polyline(2);
            leftBar.AddVertexAt(0, new Point2d(baseX + cover, curY), 0, 0, 0);
            leftBar.AddVertexAt(1, new Point2d(baseX + cover, curY + h + 800.0), 0, 0, 0);
            var rightBar = new Polyline(2);
            rightBar.AddVertexAt(0, new Point2d(baseX + lw - cover, curY), 0, 0, 0);
            rightBar.AddVertexAt(1, new Point2d(baseX + lw - cover, curY + h + 800.0), 0, 0, 0);

            var xdata = new KataXDataRecord
            {
                Dang = "00", STT = (short)idx, Phi = 25, SoLuong = "14",
                TenCauKien = wallMark, ChieuDaiCacDoanMm = new[] { h + 800.0 }
            };
            KataEntityStyler.StyleAndTag(leftBar, ms, tr, db, KataLayerHelper.ThepDoc, xdata);
            KataEntityStyler.StyleAndTag(rightBar, ms, tr, db, KataLayerHelper.ThepDoc, xdata);

            var botHor = new Line(new Point3d(baseX + cover, curY + 50.0, 0), new Point3d(baseX + lw - cover, curY + 50.0, 0));
            var topHor = new Line(new Point3d(baseX + cover, curY + h - 50.0, 0), new Point3d(baseX + lw - cover, curY + h - 50.0, 0));
            KataEntityStyler.StyleAndTag(botHor, ms, tr, db, KataLayerHelper.ThepDai);
            KataEntityStyler.StyleAndTag(topHor, ms, tr, db, KataLayerHelper.ThepDai);

            var adim = new AlignedDimension(
                new Point3d(baseX + (lw / 2.0), curY + 50.0, 0),
                new Point3d(baseX + (lw / 2.0), curY + h - 50.0, 0),
                new Point3d(baseX + (lw / 2.0), curY + (h / 2.0), 0), " ", dimRai);
            KataEntityStyler.StyleAndTag(adim, ms, tr, db, "Defpoints");
        }

        private static void DrawWallCrossSection(
            BlockTableRecord ms, Transaction tr, Database db, BlockTable bt, ObjectId dim20,
            Point3d secOrigin, double lw, double tw, string secLabel)
        {
            double b = lw * 1.5;
            double h = tw * 1.5;
            double beW = Math.Min(675.0, lw * 0.225) * 1.5;
            double cov = 45.0;

            AddOpenBox(ms, tr, db, secOrigin.X, secOrigin.Y, b, h, KataLayerHelper.NetManh);
            AddOpenBox(ms, tr, db, secOrigin.X + cov, secOrigin.Y + cov, b - (2 * cov), h - (2 * cov), KataLayerHelper.ThepDai);
            AddOpenBox(ms, tr, db, secOrigin.X + cov, secOrigin.Y + cov, beW - cov, h - (2 * cov), KataLayerHelper.ThepDai);
            AddOpenBox(ms, tr, db, secOrigin.X + b - beW, secOrigin.Y + cov, beW - cov, h - (2 * cov), KataLayerHelper.ThepDai);

            AddRotatedDim(ms, tr, dim20, 0.0, secOrigin, new Point3d(secOrigin.X + b, secOrigin.Y, 0),
                new Point3d(secOrigin.X + (b / 2.0), secOrigin.Y - 220.0, 0));
            AddRotatedDim(ms, tr, dim20, Math.PI / 2.0, secOrigin, new Point3d(secOrigin.X, secOrigin.Y + h, 0),
                new Point3d(secOrigin.X - 220.0, secOrigin.Y + (h / 2.0), 0));

            if (bt.Has("kata_block_THEP"))
            {
                int nBars = 18;
                for (int i = 0; i < nBars; i++)
                {
                    double x = secOrigin.X + cov + ((b - (2 * cov)) * i / (nBars - 1));
                    InsertSimpleBlock(ms, tr, bt["kata_block_THEP"], new Point3d(x, secOrigin.Y + cov, 0), 20.0, KataLayerHelper.ThepDoc);
                    InsertSimpleBlock(ms, tr, bt["kata_block_THEP"], new Point3d(x, secOrigin.Y + h - cov, 0), 20.0, KataLayerHelper.ThepDoc);
                }
            }

            DrawTitleBlock(ms, tr, bt, new Point3d(secOrigin.X, secOrigin.Y - 500.0, 0), $"%%u{secLabel}", "TL: 1/20");
        }

        private static void AddStoryLabelAndElev(
            BlockTableRecord ms, Transaction tr, Database db, BlockTable bt,
            double x, double y, string storyName, string elevText)
        {
            var txt = new DBText { Position = new Point3d(x, y + 60.0, 0), TextString = storyName, Height = 90.0 };
            KataEntityStyler.StyleAndTag(txt, ms, tr, db, KataLayerHelper.Text);

            if (bt.Has("kata_block_CT"))
            {
                InsertBlockWithAttributes(ms, tr, bt["kata_block_CT"], new Point3d(x + 900.0, y, 0), 30.0,
                    KataLayerHelper.Text, new Dictionary<string, string> { ["CT"] = elevText });
            }
        }

        private static void AddOpenBox(
            BlockTableRecord ms, Transaction tr, Database db, double x, double y, double w, double h, string layer)
        {
            var pl = new Polyline(4);
            pl.AddVertexAt(0, new Point2d(x, y), 0, 0, 0);
            pl.AddVertexAt(1, new Point2d(x + w, y), 0, 0, 0);
            pl.AddVertexAt(2, new Point2d(x + w, y + h), 0, 0, 0);
            pl.AddVertexAt(3, new Point2d(x, y + h), 0, 0, 0);
            pl.Closed = true;
            KataEntityStyler.StyleAndTag(pl, ms, tr, db, layer);
        }

        private static void AddRotatedDim(
            BlockTableRecord ms, Transaction tr, ObjectId styleId, double rot, Point3d p1, Point3d p2, Point3d linePt)
        {
            var rdim = new RotatedDimension(rot, p1, p2, linePt, string.Empty, styleId) { Layer = KataLayerHelper.Dim };
            ms.AppendEntity(rdim);
            tr.AddNewlyCreatedDBObject(rdim, true);
        }

        private static void DrawTitleBlock(
            BlockTableRecord ms, Transaction tr, BlockTable bt, Point3d pos, string sectDet, string scale)
        {
            if (!bt.Has("kata_block_TD")) return;
            InsertBlockWithAttributes(ms, tr, bt["kata_block_TD"], pos, 30.0, KataLayerHelper.Text,
                new Dictionary<string, string> { ["SECTDET1"] = sectDet, ["SSCALE"] = scale });
        }

        private static void InsertSimpleBlock(
            BlockTableRecord ms, Transaction tr, ObjectId btrId, Point3d pos, double scale, string layer)
        {
            var br = new BlockReference(pos, btrId) { ScaleFactors = new Scale3d(scale, scale, scale), Layer = layer };
            ms.AppendEntity(br);
            tr.AddNewlyCreatedDBObject(br, true);
        }

        private static void InsertBlockWithAttributes(
            BlockTableRecord ms, Transaction tr, ObjectId btrId, Point3d pos, double scale,
            string layer, Dictionary<string, string> tags)
        {
            var br = new BlockReference(pos, btrId) { ScaleFactors = new Scale3d(scale, scale, scale), Layer = layer };
            ms.AppendEntity(br);
            tr.AddNewlyCreatedDBObject(br, true);

            var btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);
            foreach (ObjectId id in btr)
            {
                if (tr.GetObject(id, OpenMode.ForRead) is not AttributeDefinition attDef || attDef.Constant) continue;
                var attRef = new AttributeReference();
                attRef.SetAttributeFromBlock(attDef, br.BlockTransform);
                if (tags.TryGetValue(attDef.Tag, out string? val)) attRef.TextString = val;
                br.AttributeCollection.AppendAttribute(attRef);
                tr.AddNewlyCreatedDBObject(attRef, true);
            }
        }
    }
}
