using System;
using System.Collections.Generic;
using HPDcePro.Core.Geometry;

namespace HPDcePro.Core.Drafting
{
    public sealed partial class SteelFrameDraftingGenerator
    {
        private void DrawDetailAnchorBoltInternal(CadDraftingBuffer buf, SteelFrameDraftingOptions opt)
        {
            double ox = opt.Origin.X, oy = opt.Origin.Y;
            DrawAnchorBoltPlan(buf, ox, oy, opt);

            double secX = ox + opt.PedestalLengthMm + 800.0;
            DrawAnchorBoltSection(buf, secX, oy, opt);
        }

        private static void DrawAnchorBoltPlan(CadDraftingBuffer buf, double ox, double oy, SteelFrameDraftingOptions opt)
        {
            double pL = opt.PedestalLengthMm, pW = opt.PedestalWidthMm;
            double bpL = opt.BasePlateLengthMm, bpW = opt.BasePlateWidthMm;
            double cx = ox + pL * 0.5, cy = oy + pW * 0.5;

            // Pedestal & Base plate outlines
            DrawRect(buf, new Point2D(ox, oy), pL, pW, opt.FrameLayer, CadColor.CyanIndex);
            DrawRect(buf, new Point2D(cx - bpL * 0.5, cy - bpW * 0.5), bpL, bpW, opt.FrameLayer, CadColor.YellowIndex);

            // Centerlines
            buf.AddLine(new Point2D(ox - 100.0, cy), new Point2D(ox + pL + 100.0, cy), opt.AxisLayer, CadColor.RedIndex, 0.15);
            buf.AddLine(new Point2D(cx, oy - 100.0), new Point2D(cx, oy + pW + 100.0), opt.AxisLayer, CadColor.RedIndex, 0.15);

            // Column H-profile footprint (400x200x10x14)
            DrawColumnFootprint(buf, cx, cy, 400.0, 200.0, 10.0, 14.0, opt);

            // 4 Anchor bolts
            double boltDistX = bpL * 0.5 - 60.0;
            double boltDistY = bpW * 0.5 - 50.0;
            double rHole = opt.AnchorBoltDiameterMm * 0.5 + 2.0;

            buf.AddCircle(new Point2D(cx - boltDistX, cy - boltDistY), rHole, opt.AnchorLayer, CadColor.MagentaIndex);
            buf.AddCircle(new Point2D(cx + boltDistX, cy - boltDistY), rHole, opt.AnchorLayer, CadColor.MagentaIndex);
            buf.AddCircle(new Point2D(cx - boltDistX, cy + boltDistY), rHole, opt.AnchorLayer, CadColor.MagentaIndex);
            buf.AddCircle(new Point2D(cx + boltDistX, cy + boltDistY), rHole, opt.AnchorLayer, CadColor.MagentaIndex);

            // Dimensions & Titles
            buf.AddDimension(new Point2D(cx - bpL * 0.5, cy - bpW * 0.5 - 200.0), new Point2D(cx + bpL * 0.5, cy - bpW * 0.5 - 200.0), new Point2D(cx, cy - bpW * 0.5 - 350.0), opt.DimLayer);
            buf.AddDimension(new Point2D(cx - bpL * 0.5 - 200.0, cy - bpW * 0.5), new Point2D(cx - bpL * 0.5 - 200.0, cy + bpW * 0.5), new Point2D(cx - bpL * 0.5 - 350.0, cy), opt.DimLayer);
            buf.AddText($"MẶT BẰNG ĐỊNH VỊ BU LÔNG NEO - CỘT {opt.ColumnMark}", new Point2D(cx - 2000.0, oy - 700.0), 300.0, opt.TextLayer, CadColor.CyanIndex);
        }

        private static void DrawAnchorBoltSection(CadDraftingBuffer buf, double sx, double oy, SteelFrameDraftingOptions opt)
        {
            double pL = opt.PedestalLengthMm, bpL = opt.BasePlateLengthMm;
            double gT = opt.GroutThicknessMm, bpT = opt.BasePlateThicknessMm;
            double emb = opt.AnchorEmbedmentLengthMm, hook = opt.AnchorHookLengthMm;
            double cx = sx + pL * 0.5, pedTopY = oy + 700.0;

            // Pedestal block & Grout bed
            DrawRect(buf, new Point2D(sx, pedTopY - 700.0), pL, 700.0, opt.FrameLayer, CadColor.CyanIndex);
            DrawRect(buf, new Point2D(cx - (bpL + 60.0) * 0.5, pedTopY), bpL + 60.0, gT, opt.FrameLayer, CadColor.GreenIndex);

            // Base plate & Column stub
            DrawRect(buf, new Point2D(cx - bpL * 0.5, pedTopY + gT), bpL, bpT, opt.FrameLayer, CadColor.YellowIndex);
            double colTopY = pedTopY + gT + bpT + 500.0;
            buf.AddLine(new Point2D(cx - 150.0, pedTopY + gT + bpT), new Point2D(cx - 150.0, colTopY), opt.FrameLayer, CadColor.YellowIndex, 0.35);
            buf.AddLine(new Point2D(cx + 150.0, pedTopY + gT + bpT), new Point2D(cx + 150.0, colTopY), opt.FrameLayer, CadColor.YellowIndex, 0.35);

            // Stiffener triangles
            DrawStiffener(buf, new Point2D(cx - 150.0, pedTopY + gT + bpT), -100.0, 250.0, opt);
            DrawStiffener(buf, new Point2D(cx + 150.0, pedTopY + gT + bpT), 100.0, 250.0, opt);

            // 2 L-anchor bolts in section
            double boltOffX = bpL * 0.5 - 60.0;
            DrawLAnchorBolt(buf, cx - boltOffX, pedTopY + gT + bpT, emb, hook, opt, hookRight: false);
            DrawLAnchorBolt(buf, cx + boltOffX, pedTopY + gT + bpT, emb, hook, opt, hookRight: true);

            // Callout annotations
            buf.AddText($"BẢN ĐẾ THÉP -t{bpT:F0}", new Point2D(cx + bpL * 0.5 + 80.0, pedTopY + gT + bpT), 180.0, opt.TextLayer, CadColor.YellowIndex);
            buf.AddText($"VỮA KHÔNG CO NGÓT SIKA GROUT t={gT:F0}mm", new Point2D(cx + bpL * 0.5 + 80.0, pedTopY + gT * 0.5), 180.0, opt.TextLayer, CadColor.GreenIndex);
            buf.AddText($"BU LÔNG NEO {opt.AnchorBoltCount}xM{opt.AnchorBoltDiameterMm:F0} L={emb:F0}", new Point2D(cx + bpL * 0.5 + 80.0, pedTopY - emb * 0.5), 180.0, opt.TextLayer, CadColor.MagentaIndex);
            buf.AddText("CHI TIẾT CHÂN CỘT & BU LÔNG NEO", new Point2D(cx - 1500.0, oy - 700.0), 300.0, opt.TextLayer, CadColor.CyanIndex);
        }

        private static void DrawLAnchorBolt(CadDraftingBuffer buf, double bx, double plateTopY, double emb, double hook, SteelFrameDraftingOptions opt, bool hookRight)
        {
            double rodTopY = plateTopY + 120.0;
            double rodBotY = plateTopY - emb;
            buf.AddLine(new Point2D(bx, rodTopY), new Point2D(bx, rodBotY), opt.AnchorLayer, CadColor.MagentaIndex, 0.35);

            double hx = hookRight ? bx + hook : bx - hook;
            buf.AddLine(new Point2D(bx, rodBotY), new Point2D(hx, rodBotY), opt.AnchorLayer, CadColor.MagentaIndex, 0.35);

            // Double nut & washer representation
            buf.AddLine(new Point2D(bx - 30.0, plateTopY + 15.0), new Point2D(bx + 30.0, plateTopY + 15.0), opt.AnchorLayer, CadColor.YellowIndex, 0.25);
            buf.AddLine(new Point2D(bx - 25.0, plateTopY + 35.0), new Point2D(bx + 25.0, plateTopY + 35.0), opt.AnchorLayer, CadColor.YellowIndex, 0.25);
            buf.AddLine(new Point2D(bx - 25.0, plateTopY + 55.0), new Point2D(bx + 25.0, plateTopY + 55.0), opt.AnchorLayer, CadColor.YellowIndex, 0.25);
        }

        private static void DrawStiffener(CadDraftingBuffer buf, Point2D basePt, double dx, double height, SteelFrameDraftingOptions opt)
        {
            var pts = new[]
            {
                basePt,
                new Point2D(basePt.X + dx, basePt.Y),
                new Point2D(basePt.X + dx, basePt.Y + 30.0),
                new Point2D(basePt.X, basePt.Y + height)
            };
            buf.AddPolyline(pts, isClosed: true, layer: opt.FrameLayer, colorIndex: CadColor.YellowIndex, constantWidth: 0.25);
        }

        private static void DrawColumnFootprint(CadDraftingBuffer buf, double cx, double cy, double d, double bf, double tw, double tf, SteelFrameDraftingOptions opt)
        {
            // Web line
            buf.AddLine(new Point2D(cx, cy - d * 0.5 + tf), new Point2D(cx, cy + d * 0.5 - tf), opt.FrameLayer, CadColor.YellowIndex, tw / 100.0);
            // Bottom flange
            buf.AddLine(new Point2D(cx - bf * 0.5, cy - d * 0.5 + tf * 0.5), new Point2D(cx + bf * 0.5, cy - d * 0.5 + tf * 0.5), opt.FrameLayer, CadColor.YellowIndex, tf / 100.0);
            // Top flange
            buf.AddLine(new Point2D(cx - bf * 0.5, cy + d * 0.5 - tf * 0.5), new Point2D(cx + bf * 0.5, cy + d * 0.5 - tf * 0.5), opt.FrameLayer, CadColor.YellowIndex, tf / 100.0);
        }
    }
}
