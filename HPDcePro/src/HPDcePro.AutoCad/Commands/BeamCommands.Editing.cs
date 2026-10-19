using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Runtime;
using HPDcePro.AutoCad.Drafting;
using HPDcePro.AutoCad.Prompts;
using HPDcePro.Core.Calculations.Beam;
using HPDcePro.Core.Drafting;
using HPDcePro.Core.Geometry;
using HPDcePro.Core.Rebar;
using HPDcePro.Core.XData;

namespace HPDcePro.AutoCad.Commands
{
    public sealed partial class BeamCommands
    {
        [CommandMethod("BlockBeam_Size")]
        public void BlockBeam_Size()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetDouble(ed, "Nhập bề rộng dầm b (mm)", 220.0, out double b);
            PromptHelper.TryGetDouble(ed, "Nhập chiều cao dầm h (mm)", 400.0, out double h);
            PromptHelper.TryGetPoint(ed, "Chọn điểm đặt kích thước dầm", out var pt);
            var buffer = new CadDraftingBuffer();
            var origin = new Point2D(pt.X, pt.Y);
            buffer.AddText($"{b:F0}x{h:F0}", origin, 120.0, "DCE_Beam_Dim", CadColor.YellowIndex);
            buffer.AddLine(new Point2D(origin.X - b * 0.5, origin.Y - 50), new Point2D(origin.X + b * 0.5, origin.Y - 50), "DCE_Beam_Dim", CadColor.CyanIndex, 0.25);
            int count = RenderBufferToActiveDocument(buffer);
            AutoCadUiHelper.WriteMessage($"BlockBeam_Size: Hiển thị kích thước {b:F0}x{h:F0} ({count} đối tượng).");
        }

        [CommandMethod("BlockBeam_Name")]
        public void BlockBeam_Name()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetString(ed, "Nhập tên dầm", "D1", out string beamName);
            PromptHelper.TryGetPoint(ed, "Chọn điểm đặt tên dầm", out var pt);
            var buffer = new CadDraftingBuffer();
            var pkt = new XDataPacket(XDataRegistry.RegAppLinksXDataObject);
            pkt.AddString(beamName);
            var txt = new CadEntityPrimitive { EntityType = CadEntityType.Text, TextString = beamName, Position = new Point2D(pt.X, pt.Y), TextHeight = 150.0, Layer = "DCE_Beam_Name", Color = new CadColor(CadColor.YellowIndex) };
            txt.AttachXData(pkt);
            buffer.AddPrimitive(txt);
            int count = RenderBufferToActiveDocument(buffer);
            AutoCadUiHelper.WriteMessage($"BlockBeam_Name: Đặt tên dầm {beamName} ({count} đối tượng).");
        }

        [CommandMethod("BlockBeam_NameSize")]
        public void BlockBeam_NameSize()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetString(ed, "Nhập tên dầm", "D1", out string name);
            PromptHelper.TryGetDouble(ed, "Nhập bề rộng b (mm)", 220.0, out double b);
            PromptHelper.TryGetDouble(ed, "Nhập chiều cao h (mm)", 400.0, out double h);
            PromptHelper.TryGetPoint(ed, "Chọn điểm đặt nhãn dầm", out var pt);
            var buffer = new CadDraftingBuffer();
            var origin = new Point2D(pt.X, pt.Y);
            buffer.AddText($"{name} ({b:F0}x{h:F0})", origin, 150.0, "DCE_Beam_Name", CadColor.YellowIndex);
            buffer.AddPolyline(new[] { new Point2D(origin.X - 300, origin.Y - 100), new Point2D(origin.X + 300, origin.Y - 100), new Point2D(origin.X + 300, origin.Y + 100), new Point2D(origin.X - 300, origin.Y + 100) }, true, "DCE_Beam_Box", CadColor.CyanIndex, 0.35);
            int count = RenderBufferToActiveDocument(buffer);
            AutoCadUiHelper.WriteMessage($"BlockBeam_NameSize: Hiển thị {name} ({b:F0}x{h:F0}) ({count} đối tượng).");
        }

        [CommandMethod("BlockBeam_Hide")]
        public void BlockBeam_Hide()
        {
            var ed = PromptHelper.GetActiveEditor();
            string[] keywords = { "An", "Hien" };
            PromptHelper.TryGetKeywords(ed, "Chọn trạng thái hiển thị nhãn dầm [An/Hien]", keywords, "An", out string choice);
            var buffer = new CadDraftingBuffer();
            string status = choice.Equals("An", StringComparison.OrdinalIgnoreCase) ? "LAYER_FROZEN: DCE_Beam_Name" : "LAYER_THAWED: DCE_Beam_Name";
            buffer.AddText(status, new Point2D(0, 0), 10.0, "Defpoints", CadColor.ByLayerIndex);
            RenderBufferToActiveDocument(buffer);
            AutoCadUiHelper.WriteMessage($"BlockBeam_Hide: Đã chuyển trạng thái nhãn dầm: {choice}.");
        }

        [CommandMethod("BlockBeam_Width")]
        public void BlockBeam_Width()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetDouble(ed, "Nhập bề rộng dầm b (mm)", 220.0, out double b);
            PromptHelper.TryGetPoint(ed, "Chọn điểm đầu dầm", out var p1);
            PromptHelper.TryGetPoint(ed, "Chọn điểm cuối dầm", out var p2, p1);
            var pStart = new Point2D(p1.X, p1.Y);
            var pEnd = p2.DistanceTo(p1) > 1.0 ? new Point2D(p2.X, p2.Y) : new Point2D(p1.X + 4000.0, p1.Y);
            var buffer = new CadDraftingBuffer();
            double halfB = b * 0.5;
            buffer.AddLine(new Point2D(pStart.X, pStart.Y - halfB), new Point2D(pEnd.X, pEnd.Y - halfB), "DCE_Beam_Flange", CadColor.CyanIndex, 0.35);
            buffer.AddLine(new Point2D(pStart.X, pStart.Y + halfB), new Point2D(pEnd.X, pEnd.Y + halfB), "DCE_Beam_Flange", CadColor.CyanIndex, 0.35);
            buffer.AddText($"B={b:F0}", new Point2D((pStart.X + pEnd.X) * 0.5, (pStart.Y + pEnd.Y) * 0.5), 120.0, "DCE_Beam_Text", CadColor.YellowIndex);
            int count = RenderBufferToActiveDocument(buffer);
            AutoCadUiHelper.WriteMessage($"BlockBeam_Width: Hiển thị bề rộng B={b:F0} ({count} đối tượng).");
        }

        [CommandMethod("DatTenCauKienDam")]
        public void DatTenCauKienDam()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetString(ed, "Nhập tiền tố tên dầm", "D", out string prefix);
            PromptHelper.TryGetInteger(ed, "Nhập số lượng dầm cần đánh tên", 3, out int countBeams);
            PromptHelper.TryGetPoint(ed, "Chọn điểm bắt đầu đặt tên dầm", out var originPt);
            var buffer = new CadDraftingBuffer();
            for (int i = 0; i < countBeams; i++)
            {
                var pt = new Point2D(originPt.X + i * 4000.0, originPt.Y);
                buffer.AddText($"{prefix}{i + 1}", pt, 150.0, "DCE_Beam_Name", CadColor.YellowIndex);
                buffer.AddLine(pt, new Point2D(pt.X + 3500.0, pt.Y), "DCE_Beam_Center", CadColor.RedIndex, 0.15);
            }
            int count = RenderBufferToActiveDocument(buffer);
            AutoCadUiHelper.WriteMessage($"DatTenCauKienDam: Đã tự động đặt tên {countBeams} dầm ({count} đối tượng).");
        }

        [CommandMethod("GanTrucBlockDam")]
        public void GanTrucBlockDam()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetString(ed, "Nhập tiền tố trục", "TRỤC ", out string prefix);
            PromptHelper.TryGetInteger(ed, "Nhập số lượng trục", 2, out int axisCount);
            PromptHelper.TryGetPoint(ed, "Chọn điểm đặt trục đầu tiên", out var originPt);
            var buffer = new CadDraftingBuffer();
            for (int i = 0; i < axisCount; i++)
            {
                var pt = new Point2D(originPt.X + i * 6000.0, originPt.Y);
                buffer.AddCircle(pt, 250.0, "DCE_Truc_Circle", CadColor.RedIndex);
                buffer.AddText($"{prefix}{i + 1}", new Point2D(pt.X - 100, pt.Y - 50), 120.0, "DCE_Truc_Text", CadColor.YellowIndex);
            }
            int count = RenderBufferToActiveDocument(buffer);
            AutoCadUiHelper.WriteMessage($"GanTrucBlockDam: Đã gán {axisCount} trục định vị cho dầm ({count} đối tượng).");
        }

        [CommandMethod("UpDateDLDam")]
        public void UpDateDLDam()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetString(ed, "Nhập tên dầm cần đồng bộ", "D1", out string mark);
            PromptHelper.TryGetInteger(ed, "Nhập số nhịp", 2, out int spanCount);
            PromptHelper.TryGetDouble(ed, "Nhập chiều dài nhịp (mm)", 6000.0, out double spanLen);
            PromptHelper.TryGetDouble(ed, "Nhập bề rộng b (mm)", 220.0, out double b);
            PromptHelper.TryGetDouble(ed, "Nhập chiều cao h (mm)", 400.0, out double h);
            var beam = CalculateBeamReinforcement(mark, Math.Max(1, spanCount), spanLen, b, h);
            AutoCadUiHelper.WriteMessage($"UpDateDLDam: Đồng bộ dữ liệu dầm {beam.BeamMark}: {beam.Spans.Count} nhịp, Top={beam.ContinuousTopBars.Count} thanh, Bot={beam.ContinuousBottomBars.Count} thanh.");
        }

        [CommandMethod("XemLienKetThep")]
        public void XemLienKetThep()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetString(ed, "Nhập mô tả thép", "2Phi18 (L=5950)", out string rebarDesc);
            PromptHelper.TryGetPoint(ed, "Chọn điểm bắt đầu thanh thép", out var pt);
            PromptHelper.TryGetDouble(ed, "Nhập chiều dài thanh L (mm)", 6000.0, out double len);
            var buffer = new CadDraftingBuffer();
            var pStart = new Point2D(pt.X, pt.Y);
            var pEnd = new Point2D(pt.X + len, pt.Y);
            buffer.AddPolyline(new[] { pStart, pEnd }, false, "DCE_Thep_Doc", CadColor.RedIndex, 2.0);
            var midPt = new Point2D((pStart.X + pEnd.X) * 0.5, pStart.Y);
            var leaderEnd = new Point2D(midPt.X + 200.0, midPt.Y + 300.0);
            buffer.AddLine(midPt, leaderEnd, "DCE_Leader", CadColor.WhiteIndex, 0.2);
            buffer.AddText(rebarDesc, new Point2D(leaderEnd.X + 50.0, leaderEnd.Y), 120.0, "DCE_Leader_Text", CadColor.YellowIndex);
            int count = RenderBufferToActiveDocument(buffer);
            AutoCadUiHelper.WriteMessage($"XemLienKetThep: Hiển thị liên kết thép {rebarDesc} ({count} đối tượng).");
        }

        [CommandMethod("DatTenLineDam2")]
        public void DatTenLineDam2()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetString(ed, "Nhập tên tim dầm", "TIM DẦM D2", out string timName);
            PromptHelper.TryGetPoint(ed, "Chọn điểm đầu tim dầm", out var p1);
            PromptHelper.TryGetPoint(ed, "Chọn điểm cuối tim dầm", out var p2, p1);
            var pStart = new Point2D(p1.X, p1.Y);
            var pEnd = p2.DistanceTo(p1) > 1.0 ? new Point2D(p2.X, p2.Y) : new Point2D(p1.X + 5000.0, p1.Y);
            var buffer = new CadDraftingBuffer();
            buffer.AddLine(pStart, pEnd, "DCE_Beam_Center", CadColor.RedIndex, 0.25);
            var midPt = new Point2D((pStart.X + pEnd.X) * 0.5, (pStart.Y + pEnd.Y) * 0.5 + 50.0);
            buffer.AddText(timName, midPt, 100.0, "DCE_Beam_Text", CadColor.YellowIndex);
            int count = RenderBufferToActiveDocument(buffer);
            AutoCadUiHelper.WriteMessage($"DatTenLineDam2: Đặt tên {timName} theo tim polyline ({count} đối tượng).");
        }

        [CommandMethod("GanLienKetThep")]
        public void GanLienKetThep()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetString(ed, "Nhập tên cấu kiện", "D1", out string mark);
            PromptHelper.TryGetString(ed, "Nhập số hiệu thanh", "1", out string barMark);
            PromptHelper.TryGetString(ed, "Nhập đường kính (mm)", "18", out string dia);
            PromptHelper.TryGetString(ed, "Nhập số lượng", "2", out string qty);
            PromptHelper.TryGetDouble(ed, "Nhập chiều dài thanh L (mm)", 5950.0, out double len);
            PromptHelper.TryGetPoint(ed, "Chọn điểm bắt đầu thanh thép", out var pt);
            var buffer = new CadDraftingBuffer();
            var codec = new RebarXDataCodec();
            var model = new RebarXdataDceModel { TenCauKien = mark, SoHieu = barMark, DuongKinh = dia, SoLuong = qty, ChieuDaiL = len };
            var pkt = codec.Encode(model);
            var pStart = new Point2D(pt.X, pt.Y);
            var pEnd = new Point2D(pt.X + len, pt.Y);
            buffer.AddPolyline(new[] { pStart, pEnd }, false, "DCE_Thep_Doc", CadColor.RedIndex, 2.0, new[] { pkt });
            int count = RenderBufferToActiveDocument(buffer);
            AutoCadUiHelper.WriteMessage($"GanLienKetThep: Gán liên kết thép {mark} #{barMark} d{dia} L={len:F0} ({count} đối tượng).");
        }

        [CommandMethod("XoaLienKetThep")]
        public void XoaLienKetThep()
        {
            var ed = PromptHelper.GetActiveEditor();
            string[] kws = { "XacNhan", "Huy" };
            PromptHelper.TryGetKeywords(ed, "Xác nhận xóa liên kết thép RebarXdataDCE? [XacNhan/Huy]", kws, "XacNhan", out string confirm);
            if (confirm.Equals("XacNhan", StringComparison.OrdinalIgnoreCase))
            {
                var buffer = new CadDraftingBuffer();
                buffer.AddText("CLEAR_XDATA: RebarXdataDCE", new Point2D(0, 0), 10.0, "Defpoints", CadColor.ByLayerIndex);
                RenderBufferToActiveDocument(buffer);
                AutoCadUiHelper.WriteMessage("XoaLienKetThep: Đã xóa liên kết thép và làm sạch XData.");
            }
        }

        [CommandMethod("GanDLLineDam")]
        public void GanDLLineDam()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetDouble(ed, "Nhập bề rộng b (mm)", 220.0, out double b);
            PromptHelper.TryGetDouble(ed, "Nhập chiều cao h (mm)", 400.0, out double h);
            PromptHelper.TryGetDouble(ed, "Nhập chiều dài L (mm)", 6000.0, out double length);
            PromptHelper.TryGetPoint(ed, "Chọn điểm gốc tim dầm", out var pt);
            var buffer = new CadDraftingBuffer();
            var pkt = new XDataPacket(XDataRegistry.RegAppLinksXDataObject);
            pkt.AddString($"SECTION:B={b:F0},H={h:F0},L={length:F0}");
            var pStart = new Point2D(pt.X, pt.Y);
            var pEnd = new Point2D(pt.X + length, pt.Y);
            buffer.AddPolyline(new[] { pStart, pEnd }, false, "DCE_Beam_Center", CadColor.RedIndex, 0.35, new[] { pkt });
            int count = RenderBufferToActiveDocument(buffer);
            AutoCadUiHelper.WriteMessage($"GanDLLineDam: Gán thuộc tính B={b:F0}, H={h:F0}, L={length:F0} cho tim dầm ({count} đối tượng).");
        }

        [CommandMethod("XoaDLDataDCE")]
        public void XoaDLDataDCE()
        {
            var ed = PromptHelper.GetActiveEditor();
            string[] kws = { "XacNhan", "Huy" };
            PromptHelper.TryGetKeywords(ed, "Xác nhận xóa toàn bộ XData DCE trên bản vẽ? [XacNhan/Huy]", kws, "XacNhan", out string confirm);
            if (confirm.Equals("XacNhan", StringComparison.OrdinalIgnoreCase))
            {
                var buffer = new CadDraftingBuffer();
                buffer.AddText("PURGE_XDATA: DCE_Apps", new Point2D(0, 0), 10.0, "Defpoints", CadColor.ByLayerIndex);
                RenderBufferToActiveDocument(buffer);
                AutoCadUiHelper.WriteMessage("XoaDLDataDCE: Đã làm sạch toàn bộ XData DCE trên bản vẽ.");
            }
        }

        [CommandMethod("XemLienKetMBCK")]
        public void XemLienKetMBCK()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetPoint(ed, "Chọn góc thứ nhất mặt bằng", out var p1);
            PromptHelper.TryGetPoint(ed, "Chọn góc đối diện mặt bằng", out var p2, p1);
            double w = Math.Abs(p2.X - p1.X) > 10.0 ? Math.Abs(p2.X - p1.X) : 8000.0;
            double h = Math.Abs(p2.Y - p1.Y) > 10.0 ? Math.Abs(p2.Y - p1.Y) : 6000.0;
            var buffer = new CadDraftingBuffer();
            var origin = new Point2D(p1.X, p1.Y);
            buffer.AddPolyline(new[] { origin, new Point2D(origin.X + w, origin.Y), new Point2D(origin.X + w, origin.Y + h), new Point2D(origin.X, origin.Y + h) }, true, "DCE_Framing_Plan", CadColor.CyanIndex, 0.35);
            int count = RenderBufferToActiveDocument(buffer);
            AutoCadUiHelper.WriteMessage($"XemLienKetMBCK: Kiểm tra liên kết mặt bằng {w:F0}x{h:F0} ({count} đối tượng).");
        }

        [CommandMethod("XuatDuLieuDam")]
        public void XuatDuLieuDam()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetString(ed, "Nhập tên dầm cần xuất dữ liệu", "D1", out string mark);
            PromptHelper.TryGetInteger(ed, "Nhập số lượng cấu kiện", 1, out int elemCount);
            var items = new List<ScheduleItem>
            {
                new ScheduleItem { ElementMark = mark, BarMark = "1", DiameterMm = 18, LengthMm = 5950, QuantityPerElement = 4, ElementCount = elemCount },
                new ScheduleItem { ElementMark = mark, BarMark = "2", DiameterMm = 8, LengthMm = 1350, QuantityPerElement = 30, ElementCount = elemCount }
            };
            var agg = RebarScheduleAggregator.Aggregate(items);
            AutoCadUiHelper.WriteMessage($"XuatDuLieuDam: Xuất dữ liệu dầm {mark} ({elemCount} cấu kiện): Tổng {agg.TotalWeightKg:F1}kg ({agg.TotalWeightTons:F3} Tấn).");
        }
    }
}
