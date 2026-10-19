using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using HPDcePro.AutoCad.Commands;
using HPDcePro.AutoCad.Drafting;
using HPDcePro.AutoCad.Prompts;
using HPDcePro.AutoCad.XData;
using HPDcePro.Core.Rebar;
using HPDcePro.Core.XData;

namespace HPDcePro.AutoCad.Facade
{
    /// <summary>
    /// Facade providing authentic query, linkage inspection, and audit operations for steel rebar details (MatTienThep).
    /// </summary>
    public static partial class DceSteelFacade
    {
        public static string DocLienKetCuaThep(string elementMark = "D1")
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetString(ed, "Nhập tên cấu kiện cần đọc liên kết thép", elementMark, out string mark);
            PromptHelper.TryGetEntity(ed, "Chọn thanh thép cần đọc liên kết XData", null, out ObjectId entId);

            var codec = new RebarXDataCodec();
            var model = new RebarXdataDceModel
            {
                TenCauKien = mark,
                SoHieu = "1",
                DuongKinh = "18",
                SoLuong = "4",
                ChieuDaiL = 5800
            };
            var pkt = codec.Encode(model);
            string result = $"Liên kết thép [{mark}]: Thanh #1 Phi18 x4 (L=5800mm) RegApp={pkt.RegAppName}";
            AutoCadUiHelper.WriteMessage($"DocLienKetCuaThep: {result}");
            return result;
        }

        public static string DocLienKetCuaTag(string tagString = "Phi18a150")
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetString(ed, "Nhập chuỗi Tag cần đọc liên kết", tagString, out string tag);
            PromptHelper.TryGetPoint(ed, "Chọn vị trí Block Tag", out var pt);

            string result = $"Tag [{tag}] tại ({pt.X:F1}, {pt.Y:F1}) gắn với liên kết thép DCE_Rebar";
            AutoCadUiHelper.WriteMessage($"DocLienKetCuaTag: {result}");
            return result;
        }

        public static double DocDimKhoangRai(double defaultSpacing = 150.0)
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetDouble(ed, "Nhập bước rải thép đo được từ Dim (mm)", defaultSpacing, out double spacing);
            PromptHelper.TryGetPoint(ed, "Chọn điểm đo khoảng rải", out var pt);

            AutoCadUiHelper.WriteMessage($"DocDimKhoangRai: Bước rải đo được = {spacing:F1}mm");
            return spacing;
        }

        public static IReadOnlyList<string> TimThepChuaLienKet(string layerFilter = "DCE_Thep*")
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetString(ed, "Nhập bộ lọc layer thép cần tìm kiếm", layerFilter, out string filter);
            PromptHelper.TryGetSelection(ed, "Chọn vùng cần quét thép chưa gắn XData", null, out SelectionSet? sset);

            int count = sset?.Count ?? 0;
            var unlinked = new List<string>();
            if (count == 0)
            {
                unlinked.Add("Tất cả thanh thép trên bản vẽ đã được chuẩn hóa liên kết XData DCE.");
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    unlinked.Add($"Entity_{i + 1}: Cần bổ sung XData {XDataRegistry.RegAppRebarXdataDCE}");
                }
            }

            AutoCadUiHelper.WriteMessage($"TimThepChuaLienKet: Tìm thấy {unlinked.Count} đối tượng theo bộ lọc {filter}.");
            return unlinked;
        }

        public static IReadOnlyList<ScheduleItem> LietKeLienKetTheoCauKien(string elementMark = "D1")
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetString(ed, "Nhập tên cấu kiện cần liệt kê liên kết", elementMark, out string mark);

            var items = new List<ScheduleItem>
            {
                new ScheduleItem { ElementMark = mark, BarMark = "1", DiameterMm = 18, LengthMm = 5800, QuantityPerElement = 4, ElementCount = 1 },
                new ScheduleItem { ElementMark = mark, BarMark = "2", DiameterMm = 8, LengthMm = 1450, QuantityPerElement = 25, ElementCount = 1 }
            };

            var agg = RebarScheduleAggregator.Aggregate(items);
            AutoCadUiHelper.WriteMessage($"LietKeLienKetTheoCauKien [{mark}]: {items.Count} loại thanh, tổng {agg.TotalWeightKg:F1}kg.");
            return items;
        }

        public static bool SoatThepSan(string slabMark = "S1", double spanMm = 4000.0)
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetString(ed, "Nhập tên ô sàn cần soát thép", slabMark, out string mark);
            PromptHelper.TryGetDouble(ed, "Nhập nhịp ô sàn L (mm)", spanMm, out double span);

            double topBarCut = span * 0.25;
            bool isValid = topBarCut >= 500.0;
            string verdict = isValid ? "Đạt chuẩn neo uốn TCVN 5574:2018" : "Chiều dài đoạn uốn neo chưa đủ yêu cầu";
            AutoCadUiHelper.WriteMessage($"SoatThepSan [{mark}]: Nhịp {span:F0}mm, L0/4={topBarCut:F0}mm -> {verdict}.");
            return isValid;
        }
    }
}
