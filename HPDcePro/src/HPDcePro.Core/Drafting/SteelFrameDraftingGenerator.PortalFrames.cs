using System;
using System.Collections.Generic;
using HPDcePro.Core.Geometry;

namespace HPDcePro.Core.Drafting
{
    public sealed partial class SteelFrameDraftingGenerator
    {
        private void DrawElevationMiddleFrameBTInternal(CadDraftingBuffer buf, SteelFrameDraftingOptions opt)
        {
            double ox = opt.Origin.X, oy = opt.Origin.Y;
            double span = opt.SpanMm, eave = opt.EaveHeightMm;
            double rise = (span * 0.5) * (opt.RoofSlopePercent / 100.0);
            double apexY = oy + eave + rise;
            double colB = opt.ConcreteColumnBMm;

            // Concrete columns
            DrawRect(buf, new Point2D(ox - colB * 0.5, oy), colB, eave, opt.FrameLayer, CadColor.CyanIndex);
            DrawRect(buf, new Point2D(ox + span - colB * 0.5, oy), colB, eave, opt.FrameLayer, CadColor.CyanIndex);

            // Haunch bearing plates & steel rafters
            buf.AddLine(new Point2D(ox - colB * 0.5 - 50.0, oy + eave), new Point2D(ox + colB * 0.5 + 50.0, oy + eave), opt.FrameLayer, CadColor.YellowIndex, 0.35);
            buf.AddLine(new Point2D(ox + span - colB * 0.5 - 50.0, oy + eave), new Point2D(ox + span + colB * 0.5 + 50.0, oy + eave), opt.FrameLayer, CadColor.YellowIndex, 0.35);
            DrawTaperedRafter(buf, new Point2D(ox, oy + eave), new Point2D(ox + span * 0.5, apexY), opt.RafterStartDepthMm, opt.RafterEndDepthMm, opt);
            DrawTaperedRafter(buf, new Point2D(ox + span, oy + eave), new Point2D(ox + span * 0.5, apexY), opt.RafterStartDepthMm, opt.RafterEndDepthMm, opt);

            DrawAxis(buf, new Point2D(ox, oy), "A", eave + rise + 800.0, opt);
            DrawAxis(buf, new Point2D(ox + span, oy), "B", eave + rise + 800.0, opt);
            buf.AddDimension(new Point2D(ox, oy - 600.0), new Point2D(ox + span, oy - 600.0), new Point2D(ox + span * 0.5, oy - 900.0), opt.DimLayer);
            buf.AddText("KHUNG TIỀN CHẾ CỘT BTCT (MIDDLEFRAMEBT)", new Point2D(ox + span * 0.5 - 2500.0, oy - 1400.0), 350.0, opt.TextLayer, CadColor.CyanIndex);
        }

        private void DrawElevationMiddleFrame3Internal(CadDraftingBuffer buf, SteelFrameDraftingOptions opt)
        {
            double ox = opt.Origin.X, oy = opt.Origin.Y;
            double span = opt.SpanMm, eave = opt.EaveHeightMm, halfSpan = span * 0.5;
            double rise = halfSpan * (opt.RoofSlopePercent / 100.0);
            double apexY = oy + eave + rise;

            // 2 outer tapered steel columns + 1 center column
            DrawTaperedColumn(buf, new Point2D(ox, oy), eave, opt.ColumnStartDepthMm, opt.ColumnEndDepthMm, opt);
            DrawTaperedColumn(buf, new Point2D(ox + span, oy), eave, opt.ColumnStartDepthMm, opt.ColumnEndDepthMm, opt);
            DrawRect(buf, new Point2D(ox + halfSpan - 150.0, oy), 300.0, eave + rise, opt.FrameLayer, CadColor.YellowIndex);

            // 2-bay rafters
            DrawTaperedRafter(buf, new Point2D(ox, oy + eave), new Point2D(ox + halfSpan, apexY), opt.RafterStartDepthMm, opt.RafterEndDepthMm, opt);
            DrawTaperedRafter(buf, new Point2D(ox + span, oy + eave), new Point2D(ox + halfSpan, apexY), opt.RafterStartDepthMm, opt.RafterEndDepthMm, opt);

            DrawAxis(buf, new Point2D(ox, oy), "A", eave + rise + 800.0, opt);
            DrawAxis(buf, new Point2D(ox + halfSpan, oy), "B", eave + rise + 800.0, opt);
            DrawAxis(buf, new Point2D(ox + span, oy), "C", eave + rise + 800.0, opt);
            buf.AddDimension(new Point2D(ox, oy - 600.0), new Point2D(ox + halfSpan, oy - 600.0), new Point2D(ox + halfSpan * 0.5, oy - 900.0), opt.DimLayer);
            buf.AddDimension(new Point2D(ox + halfSpan, oy - 600.0), new Point2D(ox + span, oy - 600.0), new Point2D(ox + halfSpan * 1.5, oy - 900.0), opt.DimLayer);
            buf.AddText("KHUNG TIỀN CHẾ 3 CỘT (MIDDLEFRAME3)", new Point2D(ox + halfSpan - 2000.0, oy - 1400.0), 350.0, opt.TextLayer, CadColor.CyanIndex);
        }

        private void DrawElevationMiddleFrame3BTInternal(CadDraftingBuffer buf, SteelFrameDraftingOptions opt)
        {
            double ox = opt.Origin.X, oy = opt.Origin.Y;
            double span = opt.SpanMm, eave = opt.EaveHeightMm, halfSpan = span * 0.5;
            double rise = halfSpan * (opt.RoofSlopePercent / 100.0);
            double apexY = oy + eave + rise;
            double colB = opt.ConcreteColumnBMm;

            // 3 RC columns
            DrawRect(buf, new Point2D(ox - colB * 0.5, oy), colB, eave, opt.FrameLayer, CadColor.CyanIndex);
            DrawRect(buf, new Point2D(ox + halfSpan - colB * 0.5, oy), colB, eave + rise, opt.FrameLayer, CadColor.CyanIndex);
            DrawRect(buf, new Point2D(ox + span - colB * 0.5, oy), colB, eave, opt.FrameLayer, CadColor.CyanIndex);

            // Steel rafters resting on 3 RC columns
            DrawTaperedRafter(buf, new Point2D(ox, oy + eave), new Point2D(ox + halfSpan, apexY), opt.RafterStartDepthMm, opt.RafterEndDepthMm, opt);
            DrawTaperedRafter(buf, new Point2D(ox + span, oy + eave), new Point2D(ox + halfSpan, apexY), opt.RafterStartDepthMm, opt.RafterEndDepthMm, opt);

            DrawAxis(buf, new Point2D(ox, oy), "A", eave + rise + 800.0, opt);
            DrawAxis(buf, new Point2D(ox + halfSpan, oy), "B", eave + rise + 800.0, opt);
            DrawAxis(buf, new Point2D(ox + span, oy), "C", eave + rise + 800.0, opt);
            buf.AddDimension(new Point2D(ox, oy - 600.0), new Point2D(ox + span, oy - 600.0), new Point2D(ox + halfSpan, oy - 900.0), opt.DimLayer);
            buf.AddText("KHUNG TIỀN CHẾ 3 CỘT BTCT (MIDDLEFRAME3BT)", new Point2D(ox + halfSpan - 2500.0, oy - 1400.0), 350.0, opt.TextLayer, CadColor.CyanIndex);
        }

        private void DrawElevationMiddleFrame5Internal(CadDraftingBuffer buf, SteelFrameDraftingOptions opt)
        {
            double ox = opt.Origin.X, oy = opt.Origin.Y;
            double span = opt.SpanMm, eave = opt.EaveHeightMm;
            double bay = span / 4.0;
            double rise = (span * 0.5) * (opt.RoofSlopePercent / 100.0);
            double apexY = oy + eave + rise;

            // 5 columns (2 outer tapered, 3 inner)
            DrawTaperedColumn(buf, new Point2D(ox, oy), eave, opt.ColumnStartDepthMm, opt.ColumnEndDepthMm, opt);
            DrawTaperedColumn(buf, new Point2D(ox + span, oy), eave, opt.ColumnStartDepthMm, opt.ColumnEndDepthMm, opt);
            for (int i = 1; i <= 3; i++)
            {
                double cx = ox + i * bay;
                double cyTop = oy + eave + (i <= 2 ? i * rise * 0.5 : (4 - i) * rise * 0.5);
                DrawRect(buf, new Point2D(cx - 150.0, oy), 300.0, cyTop - oy, opt.FrameLayer, CadColor.YellowIndex);
                DrawAxis(buf, new Point2D(cx, oy), (i + 1).ToString(), eave + rise + 800.0, opt);
            }

            DrawTaperedRafter(buf, new Point2D(ox, oy + eave), new Point2D(ox + span * 0.5, apexY), opt.RafterStartDepthMm, opt.RafterEndDepthMm, opt);
            DrawTaperedRafter(buf, new Point2D(ox + span, oy + eave), new Point2D(ox + span * 0.5, apexY), opt.RafterStartDepthMm, opt.RafterEndDepthMm, opt);

            DrawAxis(buf, new Point2D(ox, oy), "1", eave + rise + 800.0, opt);
            DrawAxis(buf, new Point2D(ox + span, oy), "5", eave + rise + 800.0, opt);
            buf.AddDimension(new Point2D(ox, oy - 600.0), new Point2D(ox + span, oy - 600.0), new Point2D(ox + span * 0.5, oy - 900.0), opt.DimLayer);
            buf.AddText("KHUNG TIỀN CHẾ 5 CỘT (MIDDLEFRAME5)", new Point2D(ox + span * 0.5 - 2000.0, oy - 1400.0), 350.0, opt.TextLayer, CadColor.CyanIndex);
        }

        private void DrawElevationMiddleFrame5aInternal(CadDraftingBuffer buf, SteelFrameDraftingOptions opt)
        {
            double ox = opt.Origin.X, oy = opt.Origin.Y;
            double span = opt.SpanMm, eave = opt.EaveHeightMm;
            DrawElevationMiddleFrame5Internal(buf, opt);

            // Add crane brackets on exterior columns at 65% eave height
            double craneY = oy + eave * 0.65;
            DrawRect(buf, new Point2D(ox, craneY), 400.0, 300.0, opt.FrameLayer, CadColor.MagentaIndex);
            DrawRect(buf, new Point2D(ox + span - 400.0, craneY), 400.0, 300.0, opt.FrameLayer, CadColor.MagentaIndex);

            // Add central raised monitor frame over apex
            double halfSpan = span * 0.5;
            double rise = halfSpan * (opt.RoofSlopePercent / 100.0);
            double apexY = oy + eave + rise;
            double monW = opt.MonitorWidthMm, monH = opt.MonitorHeightMm;

            buf.AddLine(new Point2D(ox + halfSpan - monW * 0.5, apexY), new Point2D(ox + halfSpan - monW * 0.5, apexY + monH), opt.RafterLayer, CadColor.YellowIndex, 0.35);
            buf.AddLine(new Point2D(ox + halfSpan + monW * 0.5, apexY), new Point2D(ox + halfSpan + monW * 0.5, apexY + monH), opt.RafterLayer, CadColor.YellowIndex, 0.35);
            buf.AddLine(new Point2D(ox + halfSpan - monW * 0.5 - 300.0, apexY + monH), new Point2D(ox + halfSpan, apexY + monH + 300.0), opt.RafterLayer, CadColor.CyanIndex, 0.35);
            buf.AddLine(new Point2D(ox + halfSpan + monW * 0.5 + 300.0, apexY + monH), new Point2D(ox + halfSpan, apexY + monH + 300.0), opt.RafterLayer, CadColor.CyanIndex, 0.35);
            buf.AddText("CỬA TRỜI THÔNG GIÓ", new Point2D(ox + halfSpan - 1200.0, apexY + monH + 500.0), 250.0, opt.TextLayer, CadColor.YellowIndex);
        }

        private void DrawElevationVDS1Internal(CadDraftingBuffer buf, SteelFrameDraftingOptions opt)
        {
            double ox = opt.Origin.X, oy = opt.Origin.Y;
            double span = opt.SpanMm, eave = opt.EaveHeightMm;
            double rise = (span * 0.5) * (opt.RoofSlopePercent / 100.0);
            double apexX = ox + span * 0.5, apexY = oy + eave + rise;

            // Top chord, bottom chord
            buf.AddLine(new Point2D(ox, oy + eave), new Point2D(apexX, apexY), opt.RafterLayer, CadColor.CyanIndex, 0.35);
            buf.AddLine(new Point2D(ox + span, oy + eave), new Point2D(apexX, apexY), opt.RafterLayer, CadColor.CyanIndex, 0.35);
            buf.AddLine(new Point2D(ox, oy + eave), new Point2D(ox + span, oy + eave), opt.RafterLayer, CadColor.CyanIndex, 0.35);

            // Web struts & diagonals (8 panels)
            int panels = 8;
            double panelW = span / panels;
            for (int i = 1; i < panels; i++)
            {
                double px = ox + i * panelW;
                double topY = oy + eave + (i <= 4 ? i * panelW : (panels - i) * panelW) * (opt.RoofSlopePercent / 100.0);
                buf.AddLine(new Point2D(px, oy + eave), new Point2D(px, topY), opt.FrameLayer, CadColor.YellowIndex, 0.25);
                double prevTopY = oy + eave + ((i - 1) <= 4 ? (i - 1) * panelW : (panels - (i - 1)) * panelW) * (opt.RoofSlopePercent / 100.0);
                buf.AddLine(new Point2D(px - panelW, prevTopY), new Point2D(px, oy + eave), opt.BracingLayer, CadColor.GreenIndex, 0.18);
            }

            DrawAxis(buf, new Point2D(ox, oy), "A", eave + rise + 800.0, opt);
            DrawAxis(buf, new Point2D(ox + span, oy), "B", eave + rise + 800.0, opt);
            buf.AddDimension(new Point2D(ox, oy - 600.0), new Point2D(ox + span, oy - 600.0), new Point2D(apexX, oy - 900.0), opt.DimLayer);
            buf.AddText("VÌ KÈO THÉP HÌNH VDS1", new Point2D(apexX - 1500.0, oy - 1400.0), 350.0, opt.TextLayer, CadColor.CyanIndex);
        }

        private static void DrawTaperedRafter(CadDraftingBuffer buf, Point2D start, Point2D end, double dStart, double dEnd, SteelFrameDraftingOptions opt)
        {
            double dx = end.X - start.X, dy = end.Y - start.Y;
            double len = Math.Sqrt(dx * dx + dy * dy);
            if (len < 1e-4) return;
            double nx = -dy / len, ny = dx / len; // Normal pointing down-inward

            buf.AddLine(start, end, opt.RafterLayer, CadColor.CyanIndex, 0.35);
            Point2D botStart = new Point2D(start.X + nx * dStart, start.Y + ny * dStart);
            Point2D botEnd = new Point2D(end.X + nx * dEnd, end.Y + ny * dEnd);
            buf.AddLine(botStart, botEnd, opt.RafterLayer, CadColor.CyanIndex, 0.35);
            buf.AddLine(start, botStart, opt.FrameLayer, CadColor.YellowIndex, 0.25);
            buf.AddLine(end, botEnd, opt.FrameLayer, CadColor.YellowIndex, 0.25);
        }

        private static void DrawTaperedColumn(CadDraftingBuffer buf, Point2D basePt, double height, double dBase, double dTop, SteelFrameDraftingOptions opt)
        {
            double bx = basePt.X, by = basePt.Y;
            buf.AddLine(new Point2D(bx, by), new Point2D(bx, by + height), opt.FrameLayer, CadColor.CyanIndex, 0.35);
            buf.AddLine(new Point2D(bx + dBase, by), new Point2D(bx + dTop, by + height), opt.FrameLayer, CadColor.CyanIndex, 0.35);
            buf.AddLine(new Point2D(bx - 100.0, by), new Point2D(bx + dBase + 100.0, by), opt.FrameLayer, CadColor.YellowIndex, 0.35);
            buf.AddLine(new Point2D(bx, by + height), new Point2D(bx + dTop, by + height), opt.FrameLayer, CadColor.YellowIndex, 0.25);
        }

        private static void DrawRect(CadDraftingBuffer buf, Point2D origin, double width, double height, string layer, int color)
        {
            double x = origin.X, y = origin.Y;
            var pts = new[] { new Point2D(x, y), new Point2D(x + width, y), new Point2D(x + width, y + height), new Point2D(x, y + height) };
            buf.AddPolyline(pts, isClosed: true, layer: layer, colorIndex: color, constantWidth: 0.25);
        }
    }
}
