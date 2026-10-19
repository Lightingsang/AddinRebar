using System;
using System.Collections.Generic;
using HPDcePro.Core.Calculations.Beam;
using HPDcePro.Core.Geometry;
using HPDcePro.Core.XData;

namespace HPDcePro.Core.Drafting
{
    public sealed class BeamShopDrawingGenerator
    {
        private readonly RebarXDataCodec _xdataCodec = new RebarXDataCodec();

        public CadDraftingBuffer GenerateShopDrawing(ContinuousBeamResult beam, BeamDetailingOptions? options = null)
        {
            if (beam == null) throw new ArgumentNullException(nameof(beam));
            var opt = options ?? new BeamDetailingOptions();
            var buffer = new CadDraftingBuffer();

            DrawLongitudinalConcrete(buffer, beam, opt);
            DrawLongitudinalRebars(buffer, beam, opt);
            DrawLongitudinalAnnotations(buffer, beam, opt);

            if (opt.IncludeCrossSections && beam.Spans.Count > 0)
            {
                double sec1X = opt.Origin.X + beam.TotalLengthMm + 1000.0;
                DrawCrossSection(buffer, beam, opt, new Point2D(sec1X, opt.Origin.Y), isSupport: false, "MẶT CẮT 1-1");
                double sec2X = sec1X + beam.Spans[0].SectionWidthMm + 1200.0;
                DrawCrossSection(buffer, beam, opt, new Point2D(sec2X, opt.Origin.Y), isSupport: true, "MẶT CẮT 2-2");
            }

            return buffer;
        }

        private void DrawLongitudinalConcrete(CadDraftingBuffer buffer, ContinuousBeamResult beam, BeamDetailingOptions opt)
        {
            double ox = opt.Origin.X;
            double oy = opt.Origin.Y;
            double h = beam.Spans[0].SectionHeightMm;
            double totalL = beam.TotalLengthMm;

            buffer.AddLine(new Point2D(ox, oy + h), new Point2D(ox + totalL, oy + h), opt.BeamLayer, CadColor.CyanIndex, 0.35);
            buffer.AddLine(new Point2D(ox, oy), new Point2D(ox + totalL, oy), opt.BeamLayer, CadColor.CyanIndex, 0.35);
            buffer.AddLine(new Point2D(ox, oy), new Point2D(ox, oy + h), opt.BeamLayer, CadColor.CyanIndex, 0.35);
            buffer.AddLine(new Point2D(ox + totalL, oy), new Point2D(ox + totalL, oy + h), opt.BeamLayer, CadColor.CyanIndex, 0.35);

            double curX = ox;
            for (int i = 0; i < beam.Spans.Count; i++)
            {
                double colW = beam.Spans[i].LeftSupportWidthMm;
                buffer.AddLine(new Point2D(curX + colW, oy - 400.0), new Point2D(curX + colW, oy), opt.BeamLayer, CadColor.CyanIndex, 0.25);
                curX += colW + beam.Spans[i].ClearSpanMm;
                buffer.AddLine(new Point2D(curX, oy - 400.0), new Point2D(curX, oy), opt.BeamLayer, CadColor.CyanIndex, 0.25);
            }
        }

        private void DrawLongitudinalRebars(CadDraftingBuffer buffer, ContinuousBeamResult beam, BeamDetailingOptions opt)
        {
            double ox = opt.Origin.X;
            double oy = opt.Origin.Y;

            void DrawBarList(IReadOnlyList<RebarItem> bars)
            {
                for (int i = 0; i < bars.Count; i++)
                {
                    var bar = bars[i];
                    var xdata = _xdataCodec.Encode(new RebarXdataDceModel
                    {
                        TenCauKien = beam.BeamMark,
                        SoHieu = bar.BarMark,
                        MaThep = "CB400-V",
                        DuongKinh = bar.DiameterMm.ToString(),
                        SoLuong = bar.BarCount.ToString(),
                        LoaiThep = bar.LoaiThep,
                        DoanA = bar.SegmentA,
                        DoanB = bar.SegmentB,
                        DoanC = bar.SegmentC,
                        DoanD = bar.SegmentD,
                        ChieuDaiL = bar.LengthMm,
                        HinhDang = bar.ShapeCode
                    });

                    var pts = new List<Point2D>();
                    if (bar.ShapeCode == 2 && bar.SegmentA > 0)
                    {
                        double hookSign = (bar.PlacementType == RebarPlacementType.MainTop || bar.PlacementType == RebarPlacementType.ExtraTopSupport) ? -1.0 : 1.0;
                        pts.Add(new Point2D(ox + bar.StartX, oy + bar.ElevationY + hookSign * bar.SegmentA));
                    }
                    pts.Add(new Point2D(ox + bar.StartX, oy + bar.ElevationY));
                    pts.Add(new Point2D(ox + bar.EndX, oy + bar.ElevationY));
                    if (bar.ShapeCode == 2 && bar.SegmentC > 0)
                    {
                        double hookSign = (bar.PlacementType == RebarPlacementType.MainTop || bar.PlacementType == RebarPlacementType.ExtraTopSupport) ? -1.0 : 1.0;
                        pts.Add(new Point2D(ox + bar.EndX, oy + bar.ElevationY + hookSign * bar.SegmentC));
                    }

                    buffer.AddPolyline(pts, isClosed: false, layer: opt.RebarLayer, colorIndex: CadColor.RedIndex, constantWidth: 2.0, xdata: new[] { xdata });
                }
            }

            DrawBarList(beam.ContinuousTopBars);
            DrawBarList(beam.ContinuousBottomBars);
            DrawBarList(beam.SupportExtraTopBars);
            DrawBarList(beam.SpanExtraBottomBars);
            DrawBarList(beam.SideBars);
        }

        private void DrawLongitudinalAnnotations(CadDraftingBuffer buffer, ContinuousBeamResult beam, BeamDetailingOptions opt)
        {
            double ox = opt.Origin.X;
            double oy = opt.Origin.Y;
            double h = beam.Spans[0].SectionHeightMm;
            double totalL = beam.TotalLengthMm;

            buffer.AddText($"{beam.BeamMark} ({beam.Spans[0].SectionWidthMm}x{h})", new Point2D(ox + totalL * 0.5 - 200.0, oy + h + 400.0), 100.0, opt.TextLayer, CadColor.YellowIndex);
            buffer.AddText($"▼ +{opt.FloorElevationM:F3}", new Point2D(ox - 300.0, oy + h + 50.0), 80.0, opt.TextLayer, CadColor.MagentaIndex);

            double curX = ox;
            for (int i = 0; i < beam.Spans.Count; i++)
            {
                var s = beam.Spans[i];
                double spanMidX = curX + s.LeftSupportWidthMm + s.ClearSpanMm * 0.5;
                buffer.AddDimension(new Point2D(curX + s.LeftSupportWidthMm, oy - 200.0), new Point2D(curX + s.LeftSupportWidthMm + s.ClearSpanMm, oy - 200.0), new Point2D(spanMidX, oy - 300.0), opt.DimLayer, CadColor.GreenIndex, $"{s.ClearSpanMm:F0}");
                buffer.AddText($"Nhịp {i + 1}", new Point2D(spanMidX - 100.0, oy + h * 0.5), 80.0, opt.TextLayer, CadColor.WhiteIndex);
                curX += s.LeftSupportWidthMm + s.ClearSpanMm;
            }
        }

        private void DrawCrossSection(CadDraftingBuffer buffer, ContinuousBeamResult beam, BeamDetailingOptions opt, Point2D secOrigin, bool isSupport, string title)
        {
            double b = beam.Spans[0].SectionWidthMm;
            double h = beam.Spans[0].SectionHeightMm;
            double c = opt.ClearCoverMm;
            double ox = secOrigin.X;
            double oy = secOrigin.Y;

            var contour = new[] { new Point2D(ox, oy), new Point2D(ox + b, oy), new Point2D(ox + b, oy + h), new Point2D(ox, oy + h) };
            buffer.AddPolyline(contour, isClosed: true, layer: opt.BeamLayer, colorIndex: CadColor.CyanIndex, constantWidth: 0.35);

            var stirrup = new[] { new Point2D(ox + c, oy + c), new Point2D(ox + b - c, oy + c), new Point2D(ox + b - c, oy + h - c), new Point2D(ox + c, oy + h - c) };
            buffer.AddPolyline(stirrup, isClosed: true, layer: opt.RebarLayer, colorIndex: CadColor.BlueIndex, constantWidth: 1.5);

            double rBar = 10.0;
            buffer.AddCircle(new Point2D(ox + c + rBar, oy + h - c - rBar), rBar, opt.RebarLayer, CadColor.RedIndex);
            buffer.AddCircle(new Point2D(ox + b - c - rBar, oy + h - c - rBar), rBar, opt.RebarLayer, CadColor.RedIndex);
            buffer.AddCircle(new Point2D(ox + c + rBar, oy + c + rBar), rBar, opt.RebarLayer, CadColor.RedIndex);
            buffer.AddCircle(new Point2D(ox + b - c - rBar, oy + c + rBar), rBar, opt.RebarLayer, CadColor.RedIndex);

            if (isSupport)
            {
                buffer.AddCircle(new Point2D(ox + b * 0.5, oy + h - c - rBar), rBar, opt.RebarLayer, CadColor.RedIndex);
            }
            else
            {
                buffer.AddCircle(new Point2D(ox + b * 0.5, oy + c + rBar), rBar, opt.RebarLayer, CadColor.RedIndex);
            }

            buffer.AddText(title, new Point2D(ox + b * 0.5 - 150.0, oy + h + 200.0), 80.0, opt.TextLayer, CadColor.YellowIndex);
            buffer.AddDimension(new Point2D(ox, oy - 100.0), new Point2D(ox + b, oy - 100.0), new Point2D(ox + b * 0.5, oy - 150.0), opt.DimLayer, CadColor.GreenIndex, $"{b:F0}");
            buffer.AddDimension(new Point2D(ox + b + 100.0, oy), new Point2D(ox + b + 100.0, oy + h), new Point2D(ox + b + 150.0, oy + h * 0.5), opt.DimLayer, CadColor.GreenIndex, $"{h:F0}");
        }
    }
}
