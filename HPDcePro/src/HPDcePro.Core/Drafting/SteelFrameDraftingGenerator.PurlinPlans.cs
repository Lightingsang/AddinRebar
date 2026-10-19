using System;
using System.Collections.Generic;
using HPDcePro.Core.Geometry;

namespace HPDcePro.Core.Drafting
{
    public sealed partial class SteelFrameDraftingGenerator
    {
        private void DrawPlanPurlinRoofInternal(CadDraftingBuffer buf, SteelFrameDraftingOptions opt)
        {
            double ox = opt.Origin.X, oy = opt.Origin.Y;
            double span = opt.SpanMm, len = opt.BuildingLengthMm;
            double bay = opt.BaySpacingMm, ridgeX = ox + span * 0.5;
            int numBays = Math.Max(1, (int)Math.Round(len / bay));

            DrawPlanGrids(buf, ox, oy, span, len, bay, numBays, opt);
            DrawPurlinLines(buf, ox, oy, span, len, opt.PurlinSpacingMm, ridgeX, opt);
            DrawSagRodsAndBracing(buf, ox, oy, span, len, bay, numBays, opt);

            buf.AddDimension(new Point2D(ox, oy - 800.0), new Point2D(ox + span, oy - 800.0), new Point2D(ridgeX, oy - 1200.0), opt.DimLayer);
            buf.AddDimension(new Point2D(ox - 800.0, oy), new Point2D(ox - 800.0, oy + len), new Point2D(ox - 1200.0, oy + len * 0.5), opt.DimLayer);
            buf.AddText("MẶT BẰNG XÀ GỒ MÁI & GIẰNG MÁI", new Point2D(ridgeX - 3000.0, oy - 1800.0), 400.0, opt.TextLayer, CadColor.CyanIndex);
            buf.AddText($"QUY CÁCH: {opt.PurlinProfile} @{opt.PurlinSpacingMm:F0}", new Point2D(ridgeX - 2200.0, oy - 2300.0), 250.0, opt.TextLayer, CadColor.YellowIndex);
        }

        private void DrawPlanPurlinMonitorInternal(CadDraftingBuffer buf, SteelFrameDraftingOptions opt)
        {
            double ox = opt.Origin.X, oy = opt.Origin.Y;
            double span = opt.SpanMm, len = opt.BuildingLengthMm;
            double bay = opt.BaySpacingMm, ridgeX = ox + span * 0.5;
            double monW = opt.MonitorWidthMm, monLeft = ridgeX - monW * 0.5, monRight = ridgeX + monW * 0.5;
            int numBays = Math.Max(1, (int)Math.Round(len / bay));

            // Outline of monitor zone
            buf.AddLine(new Point2D(monLeft, oy), new Point2D(monLeft, oy + len), opt.FrameLayer, CadColor.CyanIndex, 0.35);
            buf.AddLine(new Point2D(monRight, oy), new Point2D(monRight, oy + len), opt.FrameLayer, CadColor.CyanIndex, 0.35);
            buf.AddLine(new Point2D(ridgeX, oy), new Point2D(ridgeX, oy + len), opt.AxisLayer, CadColor.RedIndex, 0.15);

            // Transverse frames at each bay line
            for (int i = 0; i <= numBays; i++)
            {
                double by = oy + i * bay;
                buf.AddLine(new Point2D(monLeft - 300.0, by), new Point2D(monRight + 300.0, by), opt.FrameLayer, CadColor.YellowIndex, 0.25);
            }

            // Monitor roof purlins and louvers
            double pSpacing = opt.MonitorPurlinSpacingMm;
            for (double x = monLeft + pSpacing; x < monRight - 50.0; x += pSpacing)
            {
                buf.AddLine(new Point2D(x, oy), new Point2D(x, oy + len), opt.PurlinLayer, CadColor.YellowIndex, 0.20);
            }
            DrawMonitorLouvers(buf, monLeft, monRight, oy, len, opt);

            buf.AddDimension(new Point2D(monLeft, oy - 600.0), new Point2D(monRight, oy - 600.0), new Point2D(ridgeX, oy - 900.0), opt.DimLayer);
            buf.AddText("MẶT BẰNG XÀ GỒ CỬA TRỜI THÔNG GIÓ", new Point2D(ridgeX - 2500.0, oy - 1400.0), 350.0, opt.TextLayer, CadColor.CyanIndex);
        }

        private static void DrawPlanGrids(CadDraftingBuffer buf, double ox, double oy, double span, double len, double bay, int numBays, SteelFrameDraftingOptions opt)
        {
            // Boundary grid lines
            buf.AddLine(new Point2D(ox, oy), new Point2D(ox + span, oy), opt.AxisLayer, CadColor.RedIndex, 0.15);
            buf.AddLine(new Point2D(ox, oy + len), new Point2D(ox + span, oy + len), opt.AxisLayer, CadColor.RedIndex, 0.15);
            buf.AddLine(new Point2D(ox, oy), new Point2D(ox, oy + len), opt.AxisLayer, CadColor.RedIndex, 0.15);
            buf.AddLine(new Point2D(ox + span, oy), new Point2D(ox + span, oy + len), opt.AxisLayer, CadColor.RedIndex, 0.15);

            // Bay axis lines
            for (int i = 0; i <= numBays; i++)
            {
                double by = oy + i * bay;
                buf.AddLine(new Point2D(ox - 400.0, by), new Point2D(ox + span + 400.0, by), opt.AxisLayer, CadColor.RedIndex, 0.15);
                buf.AddCircle(new Point2D(ox - 600.0, by), 250.0, opt.AxisLayer, CadColor.RedIndex);
                buf.AddText((i + 1).ToString(), new Point2D(ox - 680.0, by - 90.0), 200.0, opt.TextLayer, CadColor.CyanIndex);
            }
        }

        private static void DrawPurlinLines(CadDraftingBuffer buf, double ox, double oy, double span, double len, double pSpace, double ridgeX, SteelFrameDraftingOptions opt)
        {
            // Left slope purlins
            for (double x = ox + pSpace; x < ridgeX - 150.0; x += pSpace)
            {
                buf.AddLine(new Point2D(x, oy), new Point2D(x, oy + len), opt.PurlinLayer, CadColor.YellowIndex, 0.20);
            }
            // Double ridge purlins
            buf.AddLine(new Point2D(ridgeX - 150.0, oy), new Point2D(ridgeX - 150.0, oy + len), opt.PurlinLayer, CadColor.YellowIndex, 0.25);
            buf.AddLine(new Point2D(ridgeX + 150.0, oy), new Point2D(ridgeX + 150.0, oy + len), opt.PurlinLayer, CadColor.YellowIndex, 0.25);
            // Right slope purlins
            for (double x = ridgeX + pSpace; x < ox + span - 50.0; x += pSpace)
            {
                buf.AddLine(new Point2D(x, oy), new Point2D(x, oy + len), opt.PurlinLayer, CadColor.YellowIndex, 0.20);
            }
        }

        private static void DrawSagRodsAndBracing(CadDraftingBuffer buf, double ox, double oy, double span, double len, double bay, int numBays, SteelFrameDraftingOptions opt)
        {
            // Cross bracing in first bay, last bay, and middle bay
            int midBay = numBays / 2;
            var braceBays = new HashSet<int> { 0, numBays - 1, midBay };

            for (int b = 0; b < numBays; b++)
            {
                double y1 = oy + b * bay, y2 = y1 + bay;
                // Sag rods at 1/3 and 2/3 of each bay
                buf.AddLine(new Point2D(ox, y1 + bay / 3.0), new Point2D(ox + span, y1 + bay / 3.0), opt.BracingLayer, CadColor.GreenIndex, 0.15);
                buf.AddLine(new Point2D(ox, y1 + 2.0 * bay / 3.0), new Point2D(ox + span, y1 + 2.0 * bay / 3.0), opt.BracingLayer, CadColor.GreenIndex, 0.15);

                if (braceBays.Contains(b))
                {
                    buf.AddLine(new Point2D(ox, y1), new Point2D(ox + span * 0.5, y2), opt.BracingLayer, CadColor.MagentaIndex, 0.25);
                    buf.AddLine(new Point2D(ox, y2), new Point2D(ox + span * 0.5, y1), opt.BracingLayer, CadColor.MagentaIndex, 0.25);
                    buf.AddLine(new Point2D(ox + span * 0.5, y1), new Point2D(ox + span, y2), opt.BracingLayer, CadColor.MagentaIndex, 0.25);
                    buf.AddLine(new Point2D(ox + span * 0.5, y2), new Point2D(ox + span, y1), opt.BracingLayer, CadColor.MagentaIndex, 0.25);
                }
            }
        }

        private static void DrawMonitorLouvers(CadDraftingBuffer buf, double monLeft, double monRight, double oy, double len, SteelFrameDraftingOptions opt)
        {
            double step = 1500.0;
            for (double y = oy; y < oy + len; y += step)
            {
                buf.AddLine(new Point2D(monLeft - 200.0, y), new Point2D(monLeft, y + 200.0), opt.FrameLayer, CadColor.GreenIndex, 0.18);
                buf.AddLine(new Point2D(monRight, y), new Point2D(monRight + 200.0, y + 200.0), opt.FrameLayer, CadColor.GreenIndex, 0.18);
            }
            buf.AddText("LOUVER", new Point2D(monLeft - 500.0, oy + len * 0.5), 180.0, opt.TextLayer, CadColor.GreenIndex, Math.PI * 0.5);
            buf.AddText("LOUVER", new Point2D(monRight + 300.0, oy + len * 0.5), 180.0, opt.TextLayer, CadColor.GreenIndex, Math.PI * 0.5);
        }
    }
}
