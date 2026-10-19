using System;
using System.Collections.Generic;
using HPDcePro.Core.Calculations.Column;
using HPDcePro.Core.Geometry;
using HPDcePro.Core.XData;

namespace HPDcePro.Core.Drafting
{
    public sealed class ColumnDetailingGenerator
    {
        private readonly RebarXDataCodec _xdataCodec = new RebarXDataCodec();
        private readonly ScheduleXDataCodec _schedCodec = new ScheduleXDataCodec();

        public CadDraftingBuffer GenerateElevationAndSlices(ColumnDetailingOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            var buffer = new CadDraftingBuffer();
            if (options.Stories == null || options.Stories.Count == 0) return buffer;

            DrawMultiStoryElevation(buffer, options);

            if (options.IncludeCrossSections)
            {
                double secOriginX = options.Origin.X + 2500.0;
                double curY = options.Origin.Y;
                for (int i = 0; i < options.Stories.Count; i++)
                {
                    var story = options.Stories[i];
                    DrawCrossSection(buffer, options, story, new Point2D(secOriginX, curY));
                    curY += Math.Max(story.HeightHMm, story.WidthBMm) + 1200.0;
                }
            }

            return buffer;
        }

        private void DrawMultiStoryElevation(CadDraftingBuffer buffer, ColumnDetailingOptions opt)
        {
            double curY = opt.Origin.Y;
            double ox = opt.Origin.X;

            for (int i = 0; i < opt.Stories.Count; i++)
            {
                var cur = opt.Stories[i];
                var next = i + 1 < opt.Stories.Count ? opt.Stories[i + 1] : null;

                DrawStoryConcrete(buffer, opt, cur, ox, curY);
                DrawStoryLongitudinalRebars(buffer, opt, cur, next, ox, curY);
                DrawStoryTies(buffer, opt, cur, ox, curY);

                buffer.AddText(
                    $"{cur.StoryName} (COTE +{cur.FloorElevationMm / 1000.0:F2}m)",
                    new Point2D(ox - 1200.0, curY + 100.0), 150.0, opt.TextLayer, CadColor.YellowIndex);

                curY += cur.StoryHeightMm;
            }
        }

        private static void DrawStoryConcrete(
            CadDraftingBuffer buffer, ColumnDetailingOptions opt, ColumnStoryDetail cur, double ox, double curY)
        {
            double halfB = cur.WidthBMm * 0.5;
            double topY = curY + cur.StoryHeightMm;

            buffer.AddLine(new Point2D(ox - halfB, curY), new Point2D(ox - halfB, topY), opt.ConcreteLayer, CadColor.CyanIndex, 0.35);
            buffer.AddLine(new Point2D(ox + halfB, curY), new Point2D(ox + halfB, topY), opt.ConcreteLayer, CadColor.CyanIndex, 0.35);
            buffer.AddLine(new Point2D(ox - halfB - 200, curY), new Point2D(ox + halfB + 200, curY), opt.AxisLayer, CadColor.RedIndex, 0.15);
        }

        private void DrawStoryLongitudinalRebars(
            CadDraftingBuffer buffer, ColumnDetailingOptions opt, ColumnStoryDetail cur,
            ColumnStoryDetail? next, double ox, double curY)
        {
            double cover = opt.ConcreteCoverMm;
            double halfB = cur.WidthBMm * 0.5;
            double clearH = cur.StoryHeightMm - cur.BeamDepthAboveMm;
            double spliceStart = curY + (clearH / 3.0);
            double spliceEnd = curY + (2.0 * clearH / 3.0);
            double lapLen = 40.0 * cur.MainBarDiameterMm;

            double leftX = ox - halfB + cover;
            double rightX = ox + halfB - cover;

            bool isCranked = false;
            double nextLeftX = leftX;
            if (next != null)
            {
                double delta = (cur.WidthBMm - next.WidthBMm) * 0.5;
                double slope = delta / cur.BeamDepthAboveMm;
                if (slope > 0.0 && slope <= 1.0 / 6.0)
                {
                    isCranked = true;
                    nextLeftX = ox - (next.WidthBMm * 0.5) + cover;
                }
            }

            DrawSingleVerticalBar(buffer, opt, cur, leftX, curY, curY + cur.StoryHeightMm, spliceStart, lapLen, isCranked, nextLeftX);
            DrawSingleVerticalBar(buffer, opt, cur, rightX, curY, curY + cur.StoryHeightMm, spliceEnd, lapLen, false, rightX);
        }

        private void DrawSingleVerticalBar(
            CadDraftingBuffer buffer, ColumnDetailingOptions opt, ColumnStoryDetail cur,
            double x, double yBot, double yTop, double spliceY, double lapLen, bool isCranked, double crankedX)
        {
            var pts = new List<Point2D> { new Point2D(x, yBot), new Point2D(x, spliceY + lapLen) };
            if (isCranked) pts.Add(new Point2D(crankedX, yTop));

            var xdata = CreateRebarXData(opt.ColumnMark, cur.MainBarDiameterMm, 1, pts);
            buffer.AddPolyline(pts, false, opt.RebarLayer, CadColor.GreenIndex, 3.0, new[] { xdata });
        }

        private static void DrawStoryTies(
            CadDraftingBuffer buffer, ColumnDetailingOptions opt, ColumnStoryDetail cur, double ox, double curY)
        {
            double clearH = cur.StoryHeightMm - cur.BeamDepthAboveMm;
            double hc = Math.Max(clearH / 6.0, Math.Max(cur.WidthBMm, Math.Max(cur.HeightHMm, 500.0)));
            double halfB = cur.WidthBMm * 0.5 - opt.ConcreteCoverMm;

            double sConf = Math.Min(cur.TieSpacingConfinementMm, 10.0 * cur.TieDiameterMm);
            double sMid = cur.TieSpacingMidMm;

            for (double y = curY; y <= curY + hc; y += sConf)
                buffer.AddLine(new Point2D(ox - halfB, y), new Point2D(ox + halfB, y), opt.RebarLayer, CadColor.MagentaIndex, 1.5);

            for (double y = curY + hc + sMid; y < curY + clearH - hc; y += sMid)
                buffer.AddLine(new Point2D(ox - halfB, y), new Point2D(ox + halfB, y), opt.RebarLayer, CadColor.MagentaIndex, 1.5);

            for (double y = curY + clearH - hc; y <= curY + clearH; y += sConf)
                buffer.AddLine(new Point2D(ox - halfB, y), new Point2D(ox + halfB, y), opt.RebarLayer, CadColor.MagentaIndex, 1.5);
        }

        private void DrawCrossSection(
            CadDraftingBuffer buffer, ColumnDetailingOptions opt, ColumnStoryDetail story, Point2D center)
        {
            if (opt.Style == ColumnSectionStyle.Ci)
            {
                DrawCircularCrossSection(buffer, opt, story, center);
                return;
            }

            DrawRectangularCrossSection(buffer, opt, story, center, opt.Style == ColumnSectionStyle.REC2);
        }

        private void DrawRectangularCrossSection(
            CadDraftingBuffer buffer, ColumnDetailingOptions opt, ColumnStoryDetail story, Point2D center, bool isMultiTie)
        {
            double b = story.WidthBMm;
            double h = story.HeightHMm;
            double cover = opt.ConcreteCoverMm;

            var rect = new[]
            {
                new Point2D(center.X - b * 0.5, center.Y - h * 0.5),
                new Point2D(center.X + b * 0.5, center.Y - h * 0.5),
                new Point2D(center.X + b * 0.5, center.Y + h * 0.5),
                new Point2D(center.X - b * 0.5, center.Y + h * 0.5)
            };
            buffer.AddPolyline(rect, true, opt.ConcreteLayer, CadColor.CyanIndex, 0.35);

            var tieRect = new[]
            {
                new Point2D(center.X - b * 0.5 + cover, center.Y - h * 0.5 + cover),
                new Point2D(center.X + b * 0.5 - cover, center.Y - h * 0.5 + cover),
                new Point2D(center.X + b * 0.5 - cover, center.Y + h * 0.5 - cover),
                new Point2D(center.X - b * 0.5 + cover, center.Y + h * 0.5 - cover)
            };
            var tieXData = CreateRebarXData(opt.ColumnMark, story.TieDiameterMm, 1, tieRect);
            buffer.AddPolyline(tieRect, true, opt.RebarLayer, CadColor.MagentaIndex, 2.0, new[] { tieXData });

            if (isMultiTie)
            {
                buffer.AddLine(
                    new Point2D(center.X - b * 0.5 + cover, center.Y),
                    new Point2D(center.X + b * 0.5 - cover, center.Y),
                    opt.RebarLayer, CadColor.MagentaIndex, 1.5);
            }

            DrawRectangularRebarDots(buffer, opt, story, center, b, h, cover);
            buffer.AddText(
                $"{opt.ColumnMark} - {story.StoryName} ({b}x{h})",
                new Point2D(center.X - b * 0.5, center.Y - h * 0.5 - 250.0), 150.0, opt.TextLayer, CadColor.YellowIndex);
        }

        private void DrawRectangularRebarDots(
            CadDraftingBuffer buffer, ColumnDetailingOptions opt, ColumnStoryDetail story,
            Point2D center, double b, double h, double cover)
        {
            double r = story.MainBarDiameterMm * 0.5;
            double left = center.X - b * 0.5 + cover + r;
            double right = center.X + b * 0.5 - cover - r;
            double bot = center.Y - h * 0.5 + cover + r;
            double top = center.Y + h * 0.5 - cover - r;

            buffer.AddCircle(new Point2D(left, bot), r, opt.RebarLayer, CadColor.RedIndex);
            buffer.AddCircle(new Point2D(right, bot), r, opt.RebarLayer, CadColor.RedIndex);
            buffer.AddCircle(new Point2D(left, top), r, opt.RebarLayer, CadColor.RedIndex);
            buffer.AddCircle(new Point2D(right, top), r, opt.RebarLayer, CadColor.RedIndex);
        }

        private void DrawCircularCrossSection(
            CadDraftingBuffer buffer, ColumnDetailingOptions opt, ColumnStoryDetail story, Point2D center)
        {
            double d = story.HeightHMm;
            double r = d * 0.5;
            double cover = opt.ConcreteCoverMm;

            buffer.AddCircle(center, r, opt.ConcreteLayer, CadColor.CyanIndex);
            buffer.AddCircle(center, r - cover, opt.RebarLayer, CadColor.MagentaIndex);

            int barCount = Math.Max(6, story.MainBarCount);
            double barR = story.MainBarDiameterMm * 0.5;
            double ringR = r - cover - barR;

            for (int i = 0; i < barCount; i++)
            {
                double ang = i * 2.0 * Math.PI / barCount;
                double bx = center.X + ringR * Math.Cos(ang);
                double by = center.Y + ringR * Math.Sin(ang);
                buffer.AddCircle(new Point2D(bx, by), barR, opt.RebarLayer, CadColor.RedIndex);
            }

            buffer.AddText(
                $"{opt.ColumnMark} - {story.StoryName} (D{d})",
                new Point2D(center.X - r, center.Y - r - 250.0), 150.0, opt.TextLayer, CadColor.YellowIndex);
        }

        private XDataPacket CreateRebarXData(string mark, int dia, int count, IReadOnlyList<Point2D> pts)
        {
            double totalLen = 0.0;
            for (int i = 0; i < pts.Count - 1; i++) totalLen += pts[i].DistanceTo(pts[i + 1]);

            return _xdataCodec.Encode(new RebarXdataDceModel
            {
                TenCauKien = mark,
                SoHieu = "1",
                MaThep = "CB400-V",
                DuongKinh = dia.ToString(),
                SoLuong = count.ToString(),
                LoaiThep = "ColMain",
                DoanA = 0.0,
                DoanB = totalLen,
                DoanC = 0.0,
                DoanD = 0.0,
                ChieuDaiL = totalLen,
                HinhDang = 1
            });
        }
    }
}
