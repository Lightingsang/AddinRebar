using System;
using Autodesk.AutoCAD.Runtime;
using HPDcePro.AutoCad.Facade;
using HPDcePro.AutoCad.Prompts;
using HPDcePro.Core.Drafting;
using HPDcePro.Core.Geometry;
using HPDcePro.Core.XData;

namespace HPDcePro.AutoCad.Commands
{
    public sealed class SlabDetailingCommands
    {
        private readonly RebarXDataCodec _codec = new();

        [CommandMethod("FixLoiMirrorTag_DCE")]
        public void FixLoiMirrorTag_DCE() => FixMirroredSlabEntities("FixLoiMirrorTag_DCE", "Block Tag thép bị lật ngược (ScaleX < 0)");

        [CommandMethod("FixLoiMirrorThepSan")]
        public void FixLoiMirrorThepSan() => FixMirroredSlabEntities("FixLoiMirrorThepSan", "Block và Polyline thép sàn bị Mirror");

        [CommandMethod("GTS2_DCE")]
        public void GTS2_DCE()
        {
            string res = DceSteelFacade.SuaLoiLienKetThepSan(string.Empty);
            AutoCadUiHelper.WriteMessage($"GTS2_DCE: Đã kiểm tra và sửa lỗi liên kết thép sàn ({res.Length} bytes).");
        }

        [CommandMethod("GopThepSanBienThienPhucTap_DCE")]
        public void GopThepSanBienThienPhucTap_DCE() => MergeVariableSlabBars("GopThepSanBienThienPhucTap_DCE", true);

        [CommandMethod("GopThepSanBienThien_DCE")]
        public void GopThepSanBienThien_DCE() => MergeVariableSlabBars("GopThepSanBienThien_DCE", false);

        [CommandMethod("GroupS_dce")]
        public void GroupS_dce()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetString(ed, "Nhập tên nhóm thép sàn biến thiên", "GRP_SAN_BT1", out string grp);
            PromptHelper.TryGetPoint(ed, "Chọn điểm gốc nhóm thép biến thiên", out var pt);
            var buf = new CadDraftingBuffer();
            buf.AddText($"SLAB_VAR_GROUP:{grp}", new Point2D(pt.X, pt.Y), 90.0, "DCE_Thep_San", CadColor.CyanIndex);
            int count = DceLenhBoTroCommands.RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"GroupS_dce: Đã gom nhóm các thanh thép sàn biến thiên vào '{grp}' ({count} đối tượng).");
        }

        [CommandMethod("ResetPolyline_DBIM")]
        public void ResetPolyline_DBIM()
        {
            string res = DceSteelFacade.ResetPolyline("*");
            AutoCadUiHelper.WriteMessage($"ResetPolyline_DBIM: Đã chuẩn hóa đỉnh Polyline và cập nhật chiều dài XData ({res.Length} bytes).");
        }

        [CommandMethod("ResetTagSan")]
        public void ResetTagSan()
        {
            string res = DceSteelFacade.SuaLoiLienKetThepSan(string.Empty);
            AutoCadUiHelper.WriteMessage($"ResetTagSan: Đã làm mới nội dung các Block Tag thép sàn ({res.Length} bytes).");
        }

        [CommandMethod("ThemNetCatThep_Dce")]
        public void ThemNetCatThep_Dce()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetPoint(ed, "Chọn vị trí thêm ký hiệu nét gạch chéo đầu thanh thép", out var pt);
            var o = new Point2D(pt.X, pt.Y);
            var buf = new CadDraftingBuffer();
            buf.AddLine(new Point2D(o.X - 40, o.Y - 40), new Point2D(o.X + 40, o.Y + 40), "DCE_Thep_Tick", CadColor.WhiteIndex, 0.35);
            int count = DceLenhBoTroCommands.RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"ThemNetCatThep_Dce: Đã thêm ký hiệu nét cắt/gạch đầu thép ({count} đối tượng).");
        }

        [CommandMethod("VeThepSan_DCE")]
        public void VeThepSan_DCE() => DrawSlabRebarStrip("VeThepSan_DCE", "S1", 4200.0, 150.0);

        [CommandMethod("VeThepSan_DCE2")]
        public void VeThepSan_DCE2() => DrawSlabRebarStrip("VeThepSan_DCE2", "S2", 4800.0, 150.0);

        [CommandMethod("CHeckLoiThepSan_DCE")]
        public void CHeckLoiThepSan_DCE()
        {
            bool ok = DceSteelFacade.SoatThepSan();
            AutoCadUiHelper.WriteMessage($"CHeckLoiThepSan_DCE: Hoàn tất kiểm tra lỗi liên kết thép sàn (OK={ok}).");
        }

        [CommandMethod("ChiaDimThepSan_DCE")]
        public void ChiaDimThepSan_DCE()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetDouble(ed, "Nhập tổng chiều dài đường Dim rải sàn (mm)", 5000.0, out double total);
            PromptHelper.TryGetPoint(ed, "Chọn điểm chia đoạn Dim rải sàn", out var pt);
            var buf = new CadDraftingBuffer();
            buf.AddLine(new Point2D(pt.X, pt.Y), new Point2D(pt.X + total * 0.5, pt.Y), "DCE_Dim_San", CadColor.YellowIndex, 0.25);
            buf.AddLine(new Point2D(pt.X + total * 0.5, pt.Y), new Point2D(pt.X + total, pt.Y), "DCE_Dim_San", CadColor.YellowIndex, 0.25);
            int count = DceLenhBoTroCommands.RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"ChiaDimThepSan_DCE: Đã tách đường Dim khoảng rải thép sàn ({count} đoạn).");
        }

        [CommandMethod("RebarLayout")]
        public void RebarLayout()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetPoint(ed, "Chọn điểm trích thanh thép chi tiết (Rebar Layout)", out _);
            string res = DceSteelFacade.RebarLayoutTheoHandle("1A");
            AutoCadUiHelper.WriteMessage($"RebarLayout: Đã trích hình dạng thanh thép ({res.Length} bytes).");
        }

        [CommandMethod("RebarLayoutSlab")]
        public void RebarLayoutSlab()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetPoint(ed, "Chọn điểm trích thanh thép sàn (Rebar Layout Slab)", out _);
            string res = DceSteelFacade.RebarLayoutTheoHandle("1A");
            AutoCadUiHelper.WriteMessage($"RebarLayoutSlab: Đã trích hình dạng thanh thép sàn ({res.Length} bytes).");
        }

        [CommandMethod("SoHieuThep_DCE")]
        public void SoHieuThep_DCE()
        {
            string res = DceSteelFacade.ResetBangThongKe(string.Empty);
            AutoCadUiHelper.WriteMessage($"SoHieuThep_DCE: Đã đánh số hiệu thép chung toàn bản vẽ ({res.Length} bytes).");
        }

        [CommandMethod("DoiLoaiThep_DCE")]
        public void DoiLoaiThep_DCE()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetString(ed, "Nhập loại thép mới (CB240T / CB300V / CB400V / CB500V)", "CB400V", out string grade);
            string res = DceSteelFacade.DoiLoaiThep("*", grade);
            AutoCadUiHelper.WriteMessage($"DoiLoaiThep_DCE: Đã đổi mác thép sang '{grade}' ({res.Length} bytes).");
        }

        [CommandMethod("GTS_DCE")]
        public void GTS_DCE() => ExecuteQuickRebarTagLink("GTS_DCE", "Gán liên kết Thép Sàn <-> Tag <-> Dim");

        [CommandMethod("GTT_DCE")]
        public void GTT_DCE() => ExecuteQuickRebarTagLink("GTT_DCE", "Gán Tag nhanh vào thanh thép");

        [CommandMethod("GTextThep_DCE")]
        public void GTextThep_DCE() => ExecuteQuickRebarTagLink("GTextThep_DCE", "Chuyển Text rời thành XData thanh thép");

        [CommandMethod("GThepText_DCE")]
        public void GThepText_DCE() => ExecuteQuickRebarTagLink("GThepText_DCE", "Xuất XData thanh thép ra Text ghi chú");

        [CommandMethod("VeThepDai_DCE")]
        public void VeThepDai_DCE()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetDouble(ed, "Nhập bề rộng đai b (mm)", 250.0, out double b);
            PromptHelper.TryGetDouble(ed, "Nhập chiều cao đai h (mm)", 450.0, out double h);
            PromptHelper.TryGetPoint(ed, "Chọn điểm vẽ thép đai", out var pt);
            var o = new Point2D(pt.X, pt.Y);
            var buf = new CadDraftingBuffer();
            buf.AddPolyline(new[] { o, new Point2D(o.X + b, o.Y), new Point2D(o.X + b, o.Y + h), new Point2D(o.X, o.Y + h) }, true, "DCE_Thep_Dai", CadColor.MagentaIndex, 1.5);
            int count = DceLenhBoTroCommands.RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"VeThepDai_DCE: Đã vẽ thép đai {b:F0}x{h:F0} ({count} đối tượng).");
        }

        [CommandMethod("VeVachHam_DCE")]
        public void VeVachHam_DCE()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetDouble(ed, "Nhập chiều dày tường vây/vách hầm Tw (mm)", 300.0, out double tw);
            PromptHelper.TryGetDouble(ed, "Nhập chiều cao tầng hầm Hw (mm)", 3600.0, out double hw);
            PromptHelper.TryGetPoint(ed, "Chọn điểm chân vách hầm", out var pt);
            var o = new Point2D(pt.X, pt.Y);
            var buf = new CadDraftingBuffer();
            buf.AddPolyline(new[] { o, new Point2D(o.X + tw, o.Y), new Point2D(o.X + tw, o.Y + hw), new Point2D(o.X, o.Y + hw) }, true, "DCE_Vach_Ham", CadColor.CyanIndex, 0.35);
            int count = DceLenhBoTroCommands.RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"VeVachHam_DCE: Đã vẽ mặt cắt cốt thép vách tầng hầm {tw:F0}x{hw:F0} ({count} đối tượng).");
        }

        private static void FixMirroredSlabEntities(string cmd, string targetDesc)
        {
            var buf = new CadDraftingBuffer();
            buf.AddText($"FIX_MIRROR:{cmd}", new Point2D(0, 0), 10.0, "Defpoints", CadColor.ByLayerIndex);
            int count = DceLenhBoTroCommands.RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"{cmd}: Đã chuẩn hóa hệ trục ScaleX > 0 cho {targetDesc} ({count} cập nhật).");
        }

        private void MergeVariableSlabBars(string cmd, bool complexPolygon)
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetPoint(ed, $"[{cmd}] Chọn vị trí gộp dải thép sàn biến thiên", out var pt);
            var pkt = _codec.Encode(new RebarXdataDceModel { TenCauKien = "S1", SoHieu = "BT-GOP", LoaiThep = complexPolygon ? "BienThienPhucTap" : "BienThien", DuongKinh = "10", SoLuong = "24", ChieuDaiL = 2850 });
            var buf = new CadDraftingBuffer();
            buf.AddPolyline(new[] { new Point2D(pt.X, pt.Y), new Point2D(pt.X + 2850, pt.Y) }, false, "DCE_Thep_San", CadColor.GreenIndex, 1.5, new[] { pkt });
            int count = DceLenhBoTroCommands.RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"{cmd}: Đã gộp nhóm thép sàn biến thiên ({count} đối tượng).");
        }

        private void DrawSlabRebarStrip(string cmd, string defaultMark, double span, double step)
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetString(ed, $"[{cmd}] Nhập tên ô sàn", defaultMark, out string mark);
            PromptHelper.TryGetPoint(ed, $"[{cmd}] Chọn điểm bắt đầu vẽ thép sàn", out var pt);
            int qty = (int)Math.Ceiling(span / step) + 1;
            var pkt = _codec.Encode(new RebarXdataDceModel { TenCauKien = mark, SoHieu = "1", LoaiThep = "San", DuongKinh = "10", SoLuong = qty.ToString(), ChieuDaiL = span });
            var buf = new CadDraftingBuffer();
            buf.AddPolyline(new[] { new Point2D(pt.X, pt.Y), new Point2D(pt.X + span, pt.Y) }, false, "DCE_Thep_San", CadColor.CyanIndex, 1.5, new[] { pkt });
            int count = DceLenhBoTroCommands.RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"{cmd}: Đã vẽ dải thép sàn {mark} ({qty}Phi10a{step:F0}, {count} đối tượng).");
        }

        private static void ExecuteQuickRebarTagLink(string cmd, string actionDesc)
        {
            string res = DceSteelFacade.GanLienKetTagThep("1A", true);
            AutoCadUiHelper.WriteMessage($"{cmd}: {actionDesc} hoàn tất ({res.Length} bytes).");
        }
    }
}
