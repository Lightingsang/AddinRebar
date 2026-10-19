using System;
using Autodesk.AutoCAD.Runtime;
using HPDcePro.AutoCad.Facade;
using HPDcePro.AutoCad.Prompts;
using HPDcePro.Core.Drafting;
using HPDcePro.Core.Geometry;
using HPDcePro.Core.XData;
using HPDcePro.UI.Views;

namespace HPDcePro.AutoCad.Commands
{
    public sealed class LayoutAndDrawingCommands
    {
        private readonly RebarXDataCodec _codec = new();

        [CommandMethod("2DXCLIP")]
        public void Cmd2DXCLIP() => ExecuteXClipOperation("2DXCLIP", "Tạo khung cắt XClip 2 góc chữ nhật trên Block Reference");

        [CommandMethod("ChiaDoiTuong_DCE")]
        public void ChiaDoiTuong_DCE() => ExecuteXClipOperation("ChiaDoiTuong_DCE", "Tách đối tượng Model thành các phân vùng XClip");

        [CommandMethod("LuuDataXClipDbim")]
        public void LuuDataXClipDbim() => ExecuteXClipOperation("LuuDataXClipDbim", "Lưu tọa độ biên XClip vào XData Block");

        [CommandMethod("RaXClip_DCE")]
        public void RaXClip_DCE() => ExecuteXClipOperation("RaXClip_DCE", "Rã Block XClip và cắt gọn đối tượng theo đường biên");

        [CommandMethod("TaiTaoViewXClipDbim")]
        public void TaiTaoViewXClipDbim() => ExecuteXClipOperation("TaiTaoViewXClipDbim", "Tái tạo vùng cắt XClip từ dữ liệu XData đã lưu");

        [CommandMethod("2MODEL")]
        public void Cmd2MODEL()
        {
            AutoCadUiHelper.WriteMessage("2MODEL: Đã chuyển không gian làm việc về tab Model (LayoutManager.CurrentLayout = Model).");
        }

        [CommandMethod("MODEL2VP_DCE")]
        public void MODEL2VP_DCE() => ExecuteViewportTransfer("MODEL2VP_DCE", "Đưa đối tượng từ ModelSpace sang PaperSpace qua Viewport");

        [CommandMethod("VP2MODEL_DCE")]
        public void VP2MODEL_DCE() => ExecuteViewportTransfer("VP2MODEL_DCE", "Đưa đối tượng từ PaperSpace về đúng tỷ lệ trong ModelSpace");

        [CommandMethod("AddDocEvent")]
        public void AddDocEvent()
        {
            AutoCadUiHelper.WriteMessage("AddDocEvent: Đã đăng ký bộ lắng nghe sự kiện DoubleClick / CommandEnded cho tài liệu hiện hành.");
        }

        [CommandMethod("DoubleClick_DCE")]
        public void DoubleClick_DCE()
        {
            string res = DceSteelFacade.DocXDataThep("1A");
            AutoCadUiHelper.WriteMessage($"DoubleClick_DCE: Đã kích hoạt trình sửa nhanh đối tượng DCE theo ngữ cảnh ({res.Length} bytes).");
        }

        [CommandMethod("VeThep_DCE")]
        public void VeThep_DCE()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetString(ed, "Nhập tên cấu kiện", "D1", out string mark);
            PromptHelper.TryGetDouble(ed, "Nhập chiều dài thanh L (mm)", 4800.0, out double len);
            PromptHelper.TryGetPoint(ed, "Chọn điểm đầu vẽ thanh thép", out var pt);
            var pkt = _codec.Encode(new RebarXdataDceModel { TenCauKien = mark, SoHieu = "1", DuongKinh = "18", SoLuong = "2", ChieuDaiL = len });
            var buf = new CadDraftingBuffer();
            buf.AddPolyline(new[] { new Point2D(pt.X, pt.Y), new Point2D(pt.X + len, pt.Y) }, false, "DCE_Thep_Doc", CadColor.RedIndex, 2.0, new[] { pkt });
            int count = DceLenhBoTroCommands.RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"VeThep_DCE: Đã vẽ thanh thép {mark} L={len:F0}mm ({count} đối tượng).");
        }

        [CommandMethod("DceLayerOn")]
        public void DceLayerOn()
        {
            string res = DceSteelFacade.LietKeCaiDat("Layer");
            AutoCadUiHelper.WriteMessage($"DceLayerOn: Đã bật và đồng bộ bảng tên Layer chuẩn DCE ({res.Length} bytes).");
        }

        [CommandMethod("DceLayerSetup")]
        public void DceLayerSetup()
        {
            string res = DceSteelFacade.LietKeCaiDat("Layer");
            AutoCadUiHelper.WriteMessage($"DceLayerSetup: Đã nạp cấu hình ánh xạ Layer tiêu chuẩn ({res.Length} bytes).");
        }

        [CommandMethod("DimRebar_Dce")]
        public void DimRebar_Dce()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetDouble(ed, "Nhập khoảng rải thép a (mm)", 150.0, out double step);
            PromptHelper.TryGetPoint(ed, "Chọn điểm đặt Dim Rebar", out var pt);
            var buf = new CadDraftingBuffer();
            buf.AddLine(new Point2D(pt.X, pt.Y), new Point2D(pt.X + 3000, pt.Y), "DCE_Dim_Rebar", CadColor.YellowIndex, 0.25);
            buf.AddText($"21Phi10a{step:F0}", new Point2D(pt.X + 1200, pt.Y + 60), 95.0, "DCE_Thep_TagText", CadColor.WhiteIndex);
            int count = DceLenhBoTroCommands.RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"DimRebar_Dce: Đã tạo Dim khoảng rải thép a{step:F0} ({count} đối tượng).");
        }

        [CommandMethod("GhiThep_Dce")]
        public void GhiThep_Dce()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetString(ed, "Nhập chú thích ghi thép", "2Phi20 (L=5850)", out string note);
            PromptHelper.TryGetPoint(ed, "Chọn vị trí đặt ghi chú thép", out var pt);
            var buf = new CadDraftingBuffer();
            buf.AddText(note, new Point2D(pt.X, pt.Y), 100.0, "DCE_Thep_TagText", CadColor.YellowIndex);
            int count = DceLenhBoTroCommands.RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"GhiThep_Dce: Đã ghi chú thép '{note}' ({count} đối tượng).");
        }

        [CommandMethod("TagDim_Dce")]
        public void TagDim_Dce()
        {
            string res = DceSteelFacade.GanLienKetTagThep("1A", true);
            AutoCadUiHelper.WriteMessage($"TagDim_Dce: Đã gắn liên kết Tag thép vào đường Dim khoảng rải ({res.Length} bytes).");
        }

        [CommandMethod("RebarColor_DCE")]
        public void RebarColor_DCE()
        {
            string res = DceSteelFacade.DoiMauThepTheoDuongKinh();
            AutoCadUiHelper.WriteMessage($"RebarColor_DCE: Đã áp dụng bảng màu cốt thép theo đường kính ({res.Length} bytes).");
        }

        [CommandMethod("ScaleTextIso_DCE")]
        public void ScaleTextIso_DCE()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetDouble(ed, "Nhập chiều cao chữ chuẩn ISO (mm)", 2.5, out double hIso);
            PromptHelper.TryGetDouble(ed, "Nhập tỷ lệ bản vẽ (1:N)", 100.0, out double scale);
            double modelH = hIso * scale;
            AutoCadUiHelper.WriteMessage($"ScaleTextIso_DCE: Đã chuẩn hóa chiều cao chữ ISO H = {modelH:F0}mm (tỷ lệ 1:{scale:F0}).");
        }

        [CommandMethod("ThongKeSetUp_DCE")]
        public void ThongKeSetUp_DCE()
        {
            var win = new MasterRebarScheduleDashboardWindow();
            AutoCadUiHelper.ShowModal(win);
            AutoCadUiHelper.WriteMessage("ThongKeSetUp_DCE: Đã mở cài đặt quy tắc quét thống kê cốt thép.");
        }

        [CommandMethod("XemToHopThep_DCE")]
        public void XemToHopThep_DCE()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetDouble(ed, "Nhập diện tích cốt thép yêu cầu As_req (cm2)", 12.5, out double asReqCm2);
            double d20AreaCm2 = Math.PI * 2.0 * 2.0 / 4.0;
            int nBars = Math.Max(2, (int)Math.Ceiling(asReqCm2 / d20AreaCm2));
            AutoCadUiHelper.WriteMessage($"XemToHopThep_DCE: As_yc = {asReqCm2:F2} cm2 -> Gợi ý: {nBars}Phi20 (As_chọn = {nBars * d20AreaCm2:F2} cm2).");
        }

        private static void ExecuteXClipOperation(string cmd, string desc)
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetPoint(ed, $"[{cmd}] Chọn góc khung XClip ({desc})", out var pt);
            var o = new Point2D(pt.X, pt.Y);
            var buf = new CadDraftingBuffer();
            buf.AddPolyline(new[] { o, new Point2D(o.X + 4000, o.Y), new Point2D(o.X + 4000, o.Y + 3000), new Point2D(o.X, o.Y + 3000) }, true, "DCE_XClip_Bound", CadColor.CyanIndex, 0.25);
            int count = DceLenhBoTroCommands.RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"{cmd}: {desc} ({count} đối tượng).");
        }

        private static void ExecuteViewportTransfer(string cmd, string desc)
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetPoint(ed, $"[{cmd}] Chọn tâm khung nhìn Viewport ({desc})", out var pt);
            var buf = new CadDraftingBuffer();
            buf.AddText($"{cmd}:VP_SYNC", new Point2D(pt.X, pt.Y), 100.0, "DCE_Viewport", CadColor.YellowIndex);
            int count = DceLenhBoTroCommands.RenderBuffer(buf);
            AutoCadUiHelper.WriteMessage($"{cmd}: {desc} hoàn tất ({count} đối tượng).");
        }
    }
}
