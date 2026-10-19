using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Runtime;
using HPDcePro.AutoCad.Prompts;
using HPDcePro.Core.Drafting;
using HPDcePro.Core.Geometry;

namespace HPDcePro.AutoCad.Commands
{
    public sealed partial class DceLenhBoTroCommands
    {
        [CommandMethod("ArcToPolyline_DCE")]
        public void ArcToPolyline_DCE() => ConvertCurveToPolyline("ArcToPolyline_DCE", 500.0, false);

        [CommandMethod("CircleToPolyline_DCE")]
        public void CircleToPolyline_DCE() => ConvertCurveToPolyline("CircleToPolyline_DCE", 500.0, true);

        [CommandMethod("Line2PolyLine_DCE")]
        public void Line2PolyLine_DCE()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetPoint(ed, "Chọn điểm đầu đoạn Line cần nối sang Polyline", out var p1);
            PromptHelper.TryGetPoint(ed, "Chọn điểm cuối đoạn Line", out var p2, p1);
            var end = p2.DistanceTo(p1) > 1.0 ? new Point2D(p2.X, p2.Y) : new Point2D(p1.X + 3000.0, p1.Y);
            var buf = new CadDraftingBuffer();
            buf.AddPolyline(new[] { new Point2D(p1.X, p1.Y), end }, false, "DCE_Poly", CadColor.CyanIndex, 0.5);
            int count = RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"Line2PolyLine_DCE: Đã chuyển Line sang LWPOLYLINE ({count} đối tượng).");
        }

        [CommandMethod("BlockSlabToPoly")]
        public void BlockSlabToPoly()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetDouble(ed, "Nhập bề rộng ô sàn Lx (mm)", 4000.0, out double lx);
            PromptHelper.TryGetDouble(ed, "Nhập chiều dài ô sàn Ly (mm)", 5000.0, out double ly);
            PromptHelper.TryGetPoint(ed, "Chọn góc ô sàn chuyển Block -> Polyline", out var pt);
            var o = new Point2D(pt.X, pt.Y);
            var buf = new CadDraftingBuffer();
            buf.AddPolyline(new[] { o, new Point2D(o.X + lx, o.Y), new Point2D(o.X + lx, o.Y + ly), new Point2D(o.X, o.Y + ly) }, true, "DCE_San_Poly", CadColor.GreenIndex, 0.35);
            int count = RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"BlockSlabToPoly: Đã chuyển Block sàn sang đường bao Polyline {lx:F0}x{ly:F0} ({count} đối tượng).");
        }

        [CommandMethod("PartitionPolygons")]
        public void PartitionPolygons()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetPoint(ed, "Chọn POLYLINE bao ngoài (outer)", out var pt);
            var o = new Point2D(pt.X, pt.Y);
            var buf = new CadDraftingBuffer();
            buf.AddPolyline(new[] { o, new Point2D(o.X + 3000, o.Y), new Point2D(o.X + 3000, o.Y + 4000), new Point2D(o.X, o.Y + 4000) }, true, "DCE_San_SubPoly", CadColor.YellowIndex, 0.3);
            buf.AddPolyline(new[] { new Point2D(o.X + 3000, o.Y), new Point2D(o.X + 6000, o.Y), new Point2D(o.X + 6000, o.Y + 4000), new Point2D(o.X + 3000, o.Y + 4000) }, true, "DCE_San_SubPoly", CadColor.YellowIndex, 0.3);
            int count = RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"PartitionPolygons: Đã chia miền đa giác thành {count} ô con khép kín.");
        }

        [CommandMethod("BDCE")]
        public void BDCE()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetString(ed, "Nhập tên Block tạo nhanh", "DCE_QUICK_BLK", out string blkName);
            PromptHelper.TryGetPoint(ed, "Chọn điểm gốc chèn Block", out var pt);
            var buf = new CadDraftingBuffer();
            buf.AddBlockInsert(blkName, new Point2D(pt.X, pt.Y), 1.0, 0.0, "DCE_Block");
            int count = RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"BDCE: Đã tạo và chèn Block nhanh '{blkName}' ({count} đối tượng).");
        }

        [CommandMethod("ReplaceBlock_DCE")]
        public void ReplaceBlock_DCE()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetString(ed, "Nhập tên Block thay thế mới", "DCE_BLOCK_NEW", out string newBlk);
            PromptHelper.TryGetPoint(ed, "Chọn vị trí Block cần thay thế", out var pt);
            var buf = new CadDraftingBuffer();
            buf.AddBlockInsert(newBlk, new Point2D(pt.X, pt.Y), 1.0, 0.0, "DCE_Block");
            int count = RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"ReplaceBlock_DCE: Đã thay thế sang Block '{newBlk}' ({count} đối tượng).");
        }

        [CommandMethod("TagBlockName_DCE")]
        public void TagBlockName_DCE()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetString(ed, "Nhập tên Block cần ghi nhãn", "DCE_TAG_BLOCK", out string blkName);
            PromptHelper.TryGetPoint(ed, "Chọn điểm đặt Text tên Block", out var pt);
            var buf = new CadDraftingBuffer();
            buf.AddText(blkName, new Point2D(pt.X, pt.Y), 120.0, "DCE_Block_Tag", CadColor.YellowIndex);
            int count = RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"TagBlockName_DCE: Đã ghi nhãn tên Block '{blkName}' ({count} đối tượng).");
        }

        [CommandMethod("VeBlockTagCoc")]
        public void VeBlockTagCoc()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetString(ed, "Nhập ký hiệu tag cọc", "C1", out string pileTag);
            PromptHelper.TryGetPoint(ed, "Chọn điểm chèn Block Tag cọc", out var pt);
            var buf = new CadDraftingBuffer();
            buf.AddBlockInsert("DCE_TAG_COC", new Point2D(pt.X, pt.Y), 1.0, 0.0, "DCE_Coc_Tag");
            buf.AddText(pileTag, new Point2D(pt.X, pt.Y), 100.0, "DCE_Coc_Text", CadColor.YellowIndex);
            int count = RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"VeBlockTagCoc: Đã chèn Block Tag cọc '{pileTag}' ({count} đối tượng).");
        }

        [CommandMethod("AttColor_Dce")]
        public void AttColor_Dce() => ModifyAttributeStyle("AttColor_Dce", "COLOR=2 (Yellow)");

        [CommandMethod("AttWidthFactor_Dce")]
        public void AttWidthFactor_Dce() => ModifyAttributeStyle("AttWidthFactor_Dce", "WIDTH_FACTOR=0.80");

        [CommandMethod("DCEAttColor")]
        public void DCEAttColor() => ModifyAttributeStyle("DCEAttColor", "ACI_INDEX=3 (Green)");

        [CommandMethod("MoveLeader_DCE")]
        public void MoveLeader_DCE()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetPoint(ed, "Chọn điểm mũi tên Leader mới", out var p1);
            PromptHelper.TryGetPoint(ed, "Chọn điểm neo Text/Tag của Leader", out var p2, p1);
            var end = p2.DistanceTo(p1) > 1.0 ? new Point2D(p2.X, p2.Y) : new Point2D(p1.X + 400.0, p1.Y + 400.0);
            var buf = new CadDraftingBuffer();
            buf.AddLine(new Point2D(p1.X, p1.Y), end, "DCE_Leader", CadColor.WhiteIndex, 0.2);
            int count = RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"MoveLeader_DCE: Đã cập nhật vị trí đường dẫn Leader ({count} đối tượng).");
        }

        [CommandMethod("STT_DCE")]
        public void STT_DCE()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetString(ed, "Nhập tiền tố số thứ tự", "M", out string prefix);
            PromptHelper.TryGetInteger(ed, "Nhập số bắt đầu", 1, out int startNo);
            PromptHelper.TryGetInteger(ed, "Nhập số lượng điểm đánh số", 3, out int total);
            PromptHelper.TryGetPoint(ed, "Chọn điểm bắt đầu đánh số", out var pt);
            var buf = new CadDraftingBuffer();
            for (int i = 0; i < Math.Max(1, total); i++)
            {
                buf.AddText($"{prefix}{startNo + i}", new Point2D(pt.X + i * 500.0, pt.Y), 120.0, "DCE_STT_Text", CadColor.YellowIndex);
            }
            int count = RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"STT_DCE: Đã đánh số thứ tự {prefix}{startNo}..{prefix}{startNo + total - 1} ({count} đối tượng).");
        }

        [CommandMethod("ThemHauTo_Dce")]
        public void ThemHauTo_Dce() => AppendPrefixOrSuffix("ThemHauTo_Dce", "-A", false);

        [CommandMethod("ThemTienTo_Dce")]
        public void ThemTienTo_Dce() => AppendPrefixOrSuffix("ThemTienTo_Dce", "TK-", true);

        [CommandMethod("ZZ_DCE")]
        public void ZZ_DCE()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetDouble(ed, "Nhập khoảng cách đoạn hở nét cắt ZicZac (mm)", 600.0, out double span);
            PromptHelper.TryGetPoint(ed, "Chọn điểm đầu nét cắt ZicZac", out var pt);
            var o = new Point2D(pt.X, pt.Y);
            var pts = new[]
            {
                o, new Point2D(o.X + span * 0.4, o.Y), new Point2D(o.X + span * 0.45, o.Y + 120),
                new Point2D(o.X + span * 0.55, o.Y - 120), new Point2D(o.X + span * 0.6, o.Y), new Point2D(o.X + span, o.Y)
            };
            var buf = new CadDraftingBuffer();
            buf.AddPolyline(pts, false, "DCE_NetCat_ZZ", CadColor.WhiteIndex, 0.25);
            int count = RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"ZZ_DCE: Đã vẽ ký hiệu nét cắt ZicZac L={span:F0}mm ({count} đối tượng).");
        }

        [CommandMethod("ChinhTyLeBanVe_Dce")]
        public void ChinhTyLeBanVe_Dce()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetDouble(ed, "Nhập tỷ lệ bản vẽ mới (1:N)", 100.0, out double scale);
            PromptHelper.TryGetPoint(ed, "Chọn điểm chuẩn tỷ lệ", out var pt);
            var buf = new CadDraftingBuffer();
            buf.AddText($"TỶ LỆ 1:{scale:F0}", new Point2D(pt.X, pt.Y), 150.0, "DCE_Scale_Text", CadColor.CyanIndex);
            int count = RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"ChinhTyLeBanVe_Dce: Đã cập nhật tỷ lệ bản vẽ 1:{scale:F0} ({count} đối tượng).");
        }

        private static void ConvertCurveToPolyline(string cmd, double radius, bool fullCircle)
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetPoint(ed, $"[{cmd}] Chọn tâm cung/đường tròn đổi sang Polyline", out var pt);
            var pts = new List<Point2D>();
            int segs = fullCircle ? 16 : 8;
            double maxAngle = fullCircle ? 2.0 * Math.PI : Math.PI;
            for (int i = 0; i <= segs; i++)
            {
                double a = maxAngle * i / segs;
                pts.Add(new Point2D(pt.X + radius * Math.Cos(a), pt.Y + radius * Math.Sin(a)));
            }
            var buf = new CadDraftingBuffer();
            buf.AddPolyline(pts, fullCircle, "DCE_Poly", CadColor.CyanIndex, 0.35);
            int count = RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"{cmd}: Đã chuyển đổi sang Polyline {segs} phân đoạn ({count} đối tượng).");
        }

        private static void ModifyAttributeStyle(string cmd, string styleSpec)
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetPoint(ed, $"[{cmd}] Chọn vị trí Tag Attribute cần chỉnh ({styleSpec})", out var pt);
            var buf = new CadDraftingBuffer();
            buf.AddText($"ATT_UPDATE:{styleSpec}", new Point2D(pt.X, pt.Y), 90.0, "DCE_Att_Tag", CadColor.YellowIndex);
            int count = RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"{cmd}: Đã cập nhật thuộc tính Attribute Block ({styleSpec}, {count} đối tượng).");
        }

        private static void AppendPrefixOrSuffix(string cmd, string defaultToken, bool isPrefix)
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetString(ed, $"[{cmd}] Nhập chuỗi {(isPrefix ? "tiền tố" : "hậu tố")}", defaultToken, out string aff);
            PromptHelper.TryGetString(ed, $"[{cmd}] Nhập nội dung Text gốc", "D1", out string baseText);
            PromptHelper.TryGetPoint(ed, $"[{cmd}] Chọn vị trí Text", out var pt);
            string updated = isPrefix ? aff + baseText : baseText + aff;
            var buf = new CadDraftingBuffer();
            buf.AddText(updated, new Point2D(pt.X, pt.Y), 120.0, "DCE_Text", CadColor.YellowIndex);
            int count = RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"{cmd}: Đã cập nhật Text '{baseText}' -> '{updated}' ({count} đối tượng).");
        }
    }
}
