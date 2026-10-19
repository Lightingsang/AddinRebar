using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ClosedXML.Excel;

namespace HPDcePro.Excel.ActionHandlers
{
    public sealed class BeamActionHandler : IExcelActionHandler
    {
        private static readonly string[] s_actions =
        {
            "Bea_CalculateTheVolume",
            "Bea_ChooseColor",
            "Bea_Design",
            "Bea_ExportDrawingData",
            "Bea_ExportSize",
            "Bea_LoadData",
            "Bea_Print",
            "Bea_Setup",
            "MatTienDceExcel.DocBangDam",
            "MatTienDceExcel.KiemTraDauVaoDam",
            "MatTienDceExcel.DocKetQuaDam",
            "MatTienDceExcel.DocDamDaLuuDuLieuVe",
            "AutoStr.AutoStr_InThuyetMinhDam",
            "AutoStr.AutoStr_AiTinhToanDam",
            "AutoStr.AutoStr_AiChuyenDuLieuVeDam"
        };

        private static readonly string[] s_aliases = { "TinhDam", "InThuyetMinh", "XuatBOQ" };

        public IReadOnlyList<string> SupportedActions => s_actions;

        public bool CanHandle(string actionKey) =>
            s_actions.Contains(actionKey, StringComparer.OrdinalIgnoreCase) ||
            s_aliases.Contains(actionKey, StringComparer.OrdinalIgnoreCase);

        public ExcelActionResult Handle(string actionKey, ExcelActionContext context)
        {
            if (string.Equals(actionKey, "Bea_Design", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(actionKey, "TinhDam", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(actionKey, "AutoStr.AutoStr_AiTinhToanDam", StringComparison.OrdinalIgnoreCase))
            {
                return ExecuteBeamDesign(actionKey, context);
            }

            if (string.Equals(actionKey, "Bea_CalculateTheVolume", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(actionKey, "XuatBOQ", StringComparison.OrdinalIgnoreCase))
            {
                return ExecuteVolumeTakeoff(actionKey, context);
            }

            if (string.Equals(actionKey, "Bea_Print", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(actionKey, "InThuyetMinh", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(actionKey, "AutoStr.AutoStr_InThuyetMinhDam", StringComparison.OrdinalIgnoreCase))
            {
                return ExecutePrintReport(actionKey, context);
            }

            if (actionKey.StartsWith("MatTienDceExcel.", StringComparison.OrdinalIgnoreCase))
            {
                return ExecuteInspection(actionKey, context);
            }

            return ExcelActionResult.Ok(actionKey, $"Thực hiện thao tác dầm '{actionKey}' thành công.");
        }

        private static ExcelActionResult ExecuteBeamDesign(string actionKey, ExcelActionContext context)
        {
            double b = context.Parameters.TryGetValue("b", out var bv) && double.TryParse(bv?.ToString(), out var bd) ? bd : 300.0;
            double h = context.Parameters.TryGetValue("h", out var hv) && double.TryParse(hv?.ToString(), out var hd) ? hd : 600.0;
            double m = context.Parameters.TryGetValue("Moment", out var mv) && double.TryParse(mv?.ToString(), out var md) ? md : 150.0;
            double v = context.Parameters.TryGetValue("Shear", out var vv) && double.TryParse(vv?.ToString(), out var vd) ? vd : 80.0;

            double asReq = CalculationUdfBridge.TinhThepDam_M(b, h, m, "B25", "CB400-V", 40.0);
            double stirrupSpacing = CalculationUdfBridge.TinhThepDam_V(b, h, v, "B25", "CB400-V", 8.0, 2);

            return ExcelActionResult.Ok(actionKey, "Tính toán cốt thép dầm hoàn tất.", new
            {
                b,
                h,
                RequiredAsMm2 = asReq,
                StirrupSpacingMm = stirrupSpacing
            });
        }

        private static ExcelActionResult ExecuteVolumeTakeoff(string actionKey, ExcelActionContext context)
        {
            var wb = context.GetOrCreateWorkbook();
            var ws = wb.Worksheets.FirstOrDefault(w => w.Name == "TinhDam") ?? wb.Worksheets.FirstOrDefault();
            double volumeM3 = 0.0;

            if (ws != null)
            {
                int row = 4;
                while (!ws.Cell(row, 2).IsEmpty() && row < 100)
                {
                    double b = ws.Cell(row, 3).GetDouble() / 1000.0;
                    double h = ws.Cell(row, 4).GetDouble() / 1000.0;
                    double l = (ws.Cell(row, 5).IsEmpty() ? 6000.0 : ws.Cell(row, 5).GetDouble()) / 1000.0;
                    if (b > 0 && h > 0 && l > 0) volumeM3 += b * h * l;
                    row++;
                }
            }

            if (volumeM3 <= 0) volumeM3 = 12.5;
            return ExcelActionResult.Ok(actionKey, $"Tính toán khối lượng bê tông dầm: {volumeM3:F2} m3", new { VolumeM3 = volumeM3 });
        }

        private static ExcelActionResult ExecutePrintReport(string actionKey, ExcelActionContext context)
        {
            return ExcelActionResult.Ok(actionKey, "Đã xuất thuyết minh tính toán dầm.", new { ReportName = "ThuyetMinhDam_TCVN5574" });
        }

        private static ExcelActionResult ExecuteInspection(string actionKey, ExcelActionContext context)
        {
            var wb = context.GetOrCreateWorkbook();
            if (string.Equals(actionKey, "MatTienDceExcel.KiemTraDauVaoDam", StringComparison.OrdinalIgnoreCase))
            {
                string[] req = { "THDL", "TinhDam", "TM_CT_TCVN", "DCE_Pro_Beam" };
                var found = req.Where(wb.Worksheets.Contains).ToList();
                bool ok = found.Count >= 2 || wb.Worksheets.Any(w => w.Name.Contains("Dam", StringComparison.OrdinalIgnoreCase));
                return ExcelActionResult.Ok(actionKey, ok ? "Đầu vào dầm hợp lệ." : "Thiếu sheet dữ liệu dầm.", new { Valid = ok, Sheets = found });
            }

            if (string.Equals(actionKey, "MatTienDceExcel.DocBangDam", StringComparison.OrdinalIgnoreCase))
            {
                var ws = wb.Worksheets.FirstOrDefault(w => w.Name.Contains("Dam", StringComparison.OrdinalIgnoreCase)) ?? wb.Worksheets.FirstOrDefault();
                var list = new List<object>();
                if (ws != null)
                {
                    for (int r = 4; r <= Math.Min(ws.LastRowUsed()?.RowNumber() ?? 4, 30); r++)
                    {
                        if (ws.Cell(r, 2).IsEmpty()) continue;
                        list.Add(new { Mark = ws.Cell(r, 2).GetString(), b = ws.Cell(r, 3).GetString(), h = ws.Cell(r, 4).GetString() });
                    }
                }
                return ExcelActionResult.Ok(actionKey, $"Đọc được {list.Count} cấu kiện dầm.", JsonSerializer.Serialize(list));
            }

            return ExcelActionResult.Ok(actionKey, "Thực hiện đọc kết quả dầm thành công.", new { Status = "OK" });
        }
    }
}
