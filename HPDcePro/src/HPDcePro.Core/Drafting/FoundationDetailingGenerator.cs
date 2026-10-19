using System;
using System.Collections.Generic;
using HPDcePro.Core.Calculations.Foundation;
using HPDcePro.Core.Calculations.Secondary;
using HPDcePro.Core.Calculations.Slab;
using HPDcePro.Core.Geometry;
using HPDcePro.Core.XData;

namespace HPDcePro.Core.Drafting
{
    public sealed class FoundationDetailingGenerator
    {
        private readonly RebarXDataCodec _rebarCodec = new();
        private readonly ScheduleXDataCodec _schedCodec = new();

        public CadDraftingBuffer GeneratePileCapDrawing(
            PileCapResult result, PileCapInput input, Point2D origin)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (input == null) throw new ArgumentNullException(nameof(input));
            var buffer = new CadDraftingBuffer();

            DrawPileCapPlan(buffer, input, origin);
            DrawPilesInCap(buffer, input, origin);
            DrawPileCapSection(buffer, result, input, new Point2D(origin.X + input.WidthBMm + 1500.0, origin.Y));
            DrawCoordinateScheduleTable(buffer, result, new Point2D(origin.X, origin.Y - 1500.0));

            return buffer;
        }

        private void DrawPileCapPlan(CadDraftingBuffer buffer, PileCapInput input, Point2D origin)
        {
            double halfB = input.WidthBMm * 0.5;
            double halfL = input.LengthLMm * 0.5;
            var capPoly = new[]
            {
                new Point2D(origin.X - halfB, origin.Y - halfL),
                new Point2D(origin.X + halfB, origin.Y - halfL),
                new Point2D(origin.X + halfB, origin.Y + halfL),
                new Point2D(origin.X - halfB, origin.Y + halfL)
            };
            var mongPkt = new XDataPacket(XDataRegistry.RegAppMongCoc);
            mongPkt.AddString(input.Mark);
            buffer.AddPolyline(capPoly, true, "DCE_Mong_Bao", CadColor.CyanIndex, 0.35, new[] { mongPkt });

            double colHalfB = input.ColumnWidthBMm * 0.5;
            double colHalfH = input.ColumnHeightHMm * 0.5;
            var colPoly = new[]
            {
                new Point2D(origin.X - colHalfB, origin.Y - colHalfH),
                new Point2D(origin.X + colHalfB, origin.Y - colHalfH),
                new Point2D(origin.X + colHalfB, origin.Y + colHalfH),
                new Point2D(origin.X - colHalfB, origin.Y + colHalfH)
            };
            buffer.AddPolyline(colPoly, true, "DCE_Cot_Bao", CadColor.YellowIndex, 0.25);
            buffer.AddText($"ĐÀI {input.Mark} ({input.WidthBMm:F0}x{input.LengthLMm:F0}x{input.ThicknessHMm:F0})",
                new Point2D(origin.X - halfB, origin.Y - halfL - 250.0), 120.0, "DCE_Mong_Text", CadColor.YellowIndex);
        }

        private void DrawPilesInCap(CadDraftingBuffer buffer, PileCapInput input, Point2D origin)
        {
            foreach (var p in input.Piles)
            {
                var pt = new Point2D(origin.X + p.XMm, origin.Y + p.YMm);
                double r = p.DiameterMm * 0.5;
                var cocPkt = new XDataPacket(XDataRegistry.RegAppNhanCoc);
                cocPkt.AddString(p.PileId);
                buffer.AddCircle(pt, r, "DCE_Coc_Bao", CadColor.MagentaIndex, new[] { cocPkt });
                buffer.AddLine(new Point2D(pt.X - r * 1.2, pt.Y), new Point2D(pt.X + r * 1.2, pt.Y), "DCE_Coc_Tim", CadColor.RedIndex, 0.15);
                buffer.AddLine(new Point2D(pt.X, pt.Y - r * 1.2), new Point2D(pt.X, pt.Y + r * 1.2), "DCE_Coc_Tim", CadColor.RedIndex, 0.15);
                buffer.AddText(p.PileId, new Point2D(pt.X + r + 50, pt.Y), 100.0, "DCE_Coc_Text", CadColor.YellowIndex);
            }
        }

        private void DrawPileCapSection(
            CadDraftingBuffer buffer, PileCapResult result, PileCapInput input, Point2D secOrigin)
        {
            double halfB = input.WidthBMm * 0.5;
            double h = input.ThicknessHMm;
            var secBox = new[]
            {
                new Point2D(secOrigin.X - halfB, secOrigin.Y),
                new Point2D(secOrigin.X + halfB, secOrigin.Y),
                new Point2D(secOrigin.X + halfB, secOrigin.Y + h),
                new Point2D(secOrigin.X - halfB, secOrigin.Y + h)
            };
            buffer.AddPolyline(secBox, true, "DCE_Mong_Bao", CadColor.CyanIndex, 0.35);

            double cover = input.ConcreteCoverMm;
            var botRebar = new[]
            {
                new Point2D(secOrigin.X - halfB + cover, secOrigin.Y + cover + 150.0),
                new Point2D(secOrigin.X - halfB + cover, secOrigin.Y + cover),
                new Point2D(secOrigin.X + halfB - cover, secOrigin.Y + cover),
                new Point2D(secOrigin.X + halfB - cover, secOrigin.Y + cover + 150.0)
            };
            var xdata = _rebarCodec.Encode(new RebarXdataDceModel
            {
                TenCauKien = input.Mark,
                SoHieu = "1",
                MaThep = "CB400-V",
                LoaiThep = "BotMatX",
                DuongKinh = "16",
                SoLuong = "10",
                ChieuDaiL = input.WidthBMm
            });
            buffer.AddPolyline(botRebar, false, "DCE_Thep_Mong", CadColor.GreenIndex, 2.5, new[] { xdata });
            buffer.AddText($"MẶT CẮT {input.Mark} (H={h:F0}) - As={result.RequiredAsXMm2:F0}mm2",
                new Point2D(secOrigin.X - halfB, secOrigin.Y - 200.0), 120.0, "DCE_Mong_Text", CadColor.YellowIndex);
        }

        public void DrawCoordinateScheduleTable(
            CadDraftingBuffer buffer, PileCapResult result, Point2D tableOrigin)
        {
            buffer.AddText($"BẢNG TỌA ĐỘ VÀ SỨC CHỊU TẢI CỌC - {result.Mark}", tableOrigin, 140.0, "DCE_Bang_Text", CadColor.CyanIndex);
            double curY = tableOrigin.Y - 200.0;
            buffer.AddText("STT | HIỆU CỌC | X (m) | Y (m) | P_tt (kN) | P_cp (kN) | TRẠNG THÁI",
                new Point2D(tableOrigin.X, curY), 100.0, "DCE_Bang_Text", CadColor.YellowIndex);
            curY -= 150.0;

            foreach (var item in result.CoordinateSchedule)
            {
                string line = $"{item.Index,2}  | {item.PileId,-8} | {item.XLocalM,6:F3} | {item.YLocalM,6:F3} | {item.ReactionKn,8:F1} | {item.DesignCapacityKn,8:F1} | {item.Status}";
                buffer.AddText(line, new Point2D(tableOrigin.X, curY), 90.0, "DCE_Bang_Text", CadColor.WhiteIndex);
                curY -= 120.0;
            }
        }

        public CadDraftingBuffer GeneratePadFootingDrawing(
            PadFootingResult result, PadFootingInput input, Point2D origin)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (input == null) throw new ArgumentNullException(nameof(input));
            var buffer = new CadDraftingBuffer();

            double halfL = input.LengthLMm * 0.5;
            double halfB = input.WidthBMm * 0.5;
            var poly = new[]
            {
                new Point2D(origin.X - halfL, origin.Y - halfB),
                new Point2D(origin.X + halfL, origin.Y - halfB),
                new Point2D(origin.X + halfL, origin.Y + halfB),
                new Point2D(origin.X - halfL, origin.Y + halfB)
            };
            buffer.AddPolyline(poly, true, "DCE_Mong_Bao", CadColor.CyanIndex, 0.35);

            double colHalfB = input.ColumnWidthBMm * 0.5;
            double colHalfH = input.ColumnHeightHMm * 0.5;
            var col = new[]
            {
                new Point2D(origin.X - colHalfB, origin.Y - colHalfH),
                new Point2D(origin.X + colHalfB, origin.Y - colHalfH),
                new Point2D(origin.X + colHalfB, origin.Y + colHalfH),
                new Point2D(origin.X - colHalfB, origin.Y + colHalfH)
            };
            buffer.AddPolyline(col, true, "DCE_Cot_Bao", CadColor.YellowIndex, 0.25);

            double cov = input.ConcreteCoverMm;
            var rebarX = new[]
            {
                new Point2D(origin.X - halfL + cov, origin.Y),
                new Point2D(origin.X + halfL - cov, origin.Y)
            };
            buffer.AddPolyline(rebarX, false, "DCE_Thep_Mong", CadColor.GreenIndex, 2.0);

            buffer.AddText($"MÓNG ĐƠN {input.Mark} ({input.LengthLMm:F0}x{input.WidthBMm:F0}) - Pmax={result.BasePressureMaxKpa:F1}kPa",
                new Point2D(origin.X - halfL, origin.Y - halfB - 200.0), 120.0, "DCE_Mong_Text", CadColor.YellowIndex);
            return buffer;
        }

        public CadDraftingBuffer GenerateSlabDrawing(
            TwoWaySlabResult result, TwoWaySlabInput input, Point2D origin)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (input == null) throw new ArgumentNullException(nameof(input));
            var buffer = new CadDraftingBuffer();

            double lx = input.ShortSpanLxMm;
            double ly = input.LongSpanLyMm;
            var box = new[]
            {
                new Point2D(origin.X, origin.Y), new Point2D(origin.X + lx, origin.Y),
                new Point2D(origin.X + lx, origin.Y + ly), new Point2D(origin.X, origin.Y + ly)
            };
            buffer.AddPolyline(box, true, "DCE_San_Bao", CadColor.CyanIndex, 0.35);

            // Draw bottom mesh
            buffer.AddLine(new Point2D(origin.X + 100, origin.Y + 200), new Point2D(origin.X + lx - 100, origin.Y + 200), "DCE_Thep_San_Bot", CadColor.CyanIndex, 1.5);
            buffer.AddLine(new Point2D(origin.X + 200, origin.Y + 100), new Point2D(origin.X + 200, origin.Y + ly - 100), "DCE_Thep_San_Bot", CadColor.CyanIndex, 1.5);

            // Draw top negative support bars
            double cutX = result.TopBarCutoffLengthXMm;
            buffer.AddLine(new Point2D(origin.X, origin.Y + ly * 0.5), new Point2D(origin.X + cutX, origin.Y + ly * 0.5), "DCE_Thep_San_Top", CadColor.RedIndex, 2.0);
            buffer.AddLine(new Point2D(origin.X + lx - cutX, origin.Y + ly * 0.5), new Point2D(origin.X + lx, origin.Y + ly * 0.5), "DCE_Thep_San_Top", CadColor.RedIndex, 2.0);

            // Spacer chair marker
            buffer.AddCircle(new Point2D(origin.X + lx * 0.5, origin.Y + ly * 0.5), 50.0, "DCE_Thep_San_ConKe", CadColor.MagentaIndex);
            buffer.AddText($"SÀN {input.PanelName} (H={input.SlabThicknessHMm:F0}) - {result.StatusSummary}",
                new Point2D(origin.X, origin.Y - 150.0), 120.0, "DCE_San_Text", CadColor.YellowIndex);

            return buffer;
        }

        public CadDraftingBuffer GenerateStaircaseDrawing(
            StaircaseResult result, StaircaseInput input, Point2D origin)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (input == null) throw new ArgumentNullException(nameof(input));
            var buffer = new CadDraftingBuffer();

            double l1 = input.Landing1LengthMm;
            double l2 = input.Landing2LengthMm;
            double span = input.FlightSpanLMm;
            double rise = input.TotalRiseHMm;

            var waistPoly = new[]
            {
                new Point2D(origin.X, origin.Y),
                new Point2D(origin.X + l1, origin.Y),
                new Point2D(origin.X + l1 + span, origin.Y + rise),
                new Point2D(origin.X + l1 + span + l2, origin.Y + rise)
            };
            var thangPkt = new XDataPacket(XDataRegistry.RegAppThangMoi);
            thangPkt.AddString(input.Mark);
            buffer.AddPolyline(waistPoly, false, "DCE_Thang_Bao", CadColor.CyanIndex, 0.35, new[] { thangPkt });

            // Draw cranked rebar with lap splice
            double cov = input.ConcreteCoverMm;
            var crankedBar = new[]
            {
                new Point2D(origin.X + cov, origin.Y - cov),
                new Point2D(origin.X + l1, origin.Y - cov),
                new Point2D(origin.X + l1 + span, origin.Y + rise - cov),
                new Point2D(origin.X + l1 + span + l2 - cov, origin.Y + rise - cov)
            };
            buffer.AddPolyline(crankedBar, false, "DCE_Thep_Thang", CadColor.GreenIndex, 2.5);
            buffer.AddText($"CẦU THANG {input.Mark} (b={result.TreadWidthMm:F0}, h={result.RiserHeightMm:F0}, 2h+b={result.BlondelFormulaValue:F0})",
                new Point2D(origin.X, origin.Y - 300.0), 120.0, "DCE_Thang_Text", CadColor.YellowIndex);

            return buffer;
        }

        public CadDraftingBuffer GenerateLintelDrawing(
            LintelResult result, LintelInput input, Point2D origin)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (input == null) throw new ArgumentNullException(nameof(input));
            var buffer = new CadDraftingBuffer();

            double lTot = result.TotalSpanLMm;
            double h = input.LintelDepthHMm;
            var poly = new[]
            {
                new Point2D(origin.X, origin.Y), new Point2D(origin.X + lTot, origin.Y),
                new Point2D(origin.X + lTot, origin.Y + h), new Point2D(origin.X, origin.Y + h)
            };
            buffer.AddPolyline(poly, true, "DCE_LanhTo_Bao", CadColor.CyanIndex, 0.35);

            // Rebars: bottom 2 bars and top 2 bars
            buffer.AddLine(new Point2D(origin.X + 30, origin.Y + 25), new Point2D(origin.X + lTot - 30, origin.Y + 25), "DCE_Thep_LanhTo", CadColor.GreenIndex, 2.0);
            buffer.AddLine(new Point2D(origin.X + 30, origin.Y + h - 25), new Point2D(origin.X + lTot - 30, origin.Y + h - 25), "DCE_Thep_LanhTo", CadColor.GreenIndex, 2.0);

            // Stirrup ticks
            for (double x = origin.X + 50; x < origin.X + lTot - 50; x += input.StirrupSpacingMm)
            {
                buffer.AddLine(new Point2D(x, origin.Y + 25), new Point2D(x, origin.Y + h - 25), "DCE_Dai_LanhTo", CadColor.MagentaIndex, 1.0);
            }

            buffer.AddText($"LANH TÔ {input.Mark} (L0={input.ClearSpanL0Mm:F0}, 2Phi{result.RecommendedMainBarDiameter})",
                new Point2D(origin.X, origin.Y - 150.0), 100.0, "DCE_LanhTo_Text", CadColor.YellowIndex);

            return buffer;
        }
    }
}
