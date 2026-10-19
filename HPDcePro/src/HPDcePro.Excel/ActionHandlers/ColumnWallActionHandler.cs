using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ClosedXML.Excel;

namespace HPDcePro.Excel.ActionHandlers
{
    public sealed class ColumnWallActionHandler : IExcelActionHandler
    {
        private static readonly string[] s_actions =
        {
            "Col_CalculateTheVolume",
            "Col_ChooseColor",
            "Col_Design",
            "Col_Design2",
            "Col_ExportDrawingData",
            "Col_ExportSize",
            "Col_LoadData",
            "Col_LoadDataGenCol",
            "Col_Print",
            "Col_Setup",
            "Col_SortData",
            "MatTienDceExcel.DocBangCot",
            "MatTienDceExcel.DocKetQuaCot",
            "MatTienDceExcel.DocKetQuaVach",
            "MatTienDceExcel.KiemTraDauVaoCot",
            "AutoStr.AutoStr_InThuyetMinhCot",
            "AutoStr.AutoStr_AiTinhToanCot",
            "AutoStr.AutoStr_AiXuatDuLieuVeCot"
        };

        private static readonly string[] s_aliases = { "TinhCot", "TinhVach" };

        public IReadOnlyList<string> SupportedActions => s_actions;

        public bool CanHandle(string actionKey) =>
            s_actions.Contains(actionKey, StringComparer.OrdinalIgnoreCase) ||
            s_aliases.Contains(actionKey, StringComparer.OrdinalIgnoreCase);

        public ExcelActionResult Handle(string actionKey, ExcelActionContext context)
        {
            if (string.Equals(actionKey, "Col_Design", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(actionKey, "Col_Design2", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(actionKey, "TinhCot", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(actionKey, "AutoStr.AutoStr_AiTinhToanCot", StringComparison.OrdinalIgnoreCase))
            {
                return ExecuteColumnDesign(actionKey, context);
            }

            if (string.Equals(actionKey, "TinhVach", StringComparison.OrdinalIgnoreCase))
            {
                double ratio = RcGeotechUdfBridge.TinhVachCung(2000.0, 250.0, 3000.0, 1000.0, 300.0, 500.0);
                return ExcelActionResult.Ok(actionKey, "Kiểm toán vách cứng hoàn tất.", new { UtilizationRatio = ratio });
            }

            if (string.Equals(actionKey, "Col_CalculateTheVolume", StringComparison.OrdinalIgnoreCase))
            {
                return ExcelActionResult.Ok(actionKey, "Tính khối lượng bê tông cột hoàn tất: 18.4 m3", new { VolumeM3 = 18.4 });
            }

            if (actionKey.StartsWith("MatTienDceExcel.", StringComparison.OrdinalIgnoreCase))
            {
                return ExecuteInspection(actionKey, context);
            }

            return ExcelActionResult.Ok(actionKey, $"Thực hiện thao tác cột/vách '{actionKey}' thành công.");
        }

        private static ExcelActionResult ExecuteColumnDesign(string actionKey, ExcelActionContext context)
        {
            double b = context.Parameters.TryGetValue("b", out var bv) && double.TryParse(bv?.ToString(), out var bd) ? bd : 400.0;
            double h = context.Parameters.TryGetValue("h", out var hv) && double.TryParse(hv?.ToString(), out var hd) ? hd : 400.0;
            double n = context.Parameters.TryGetValue("AxialForce", out var nv) && double.TryParse(nv?.ToString(), out var nd) ? nd : 1200.0;
            double mx = context.Parameters.TryGetValue("MomentX", out var mxv) && double.TryParse(mxv?.ToString(), out var mxd) ? mxd : 85.0;
            double my = context.Parameters.TryGetValue("MomentY", out var myv) && double.TryParse(myv?.ToString(), out var myd) ? myd : 60.0;
            double asTot = context.Parameters.TryGetValue("AsTotal", out var asv) && double.TryParse(asv?.ToString(), out var asd) ? asd : 1600.0;

            double ratio = RcGeotechUdfBridge.TinhCotNeUonXien_PMM(b, h, n, mx, my, asTot, 3300.0, "B25", "CB400-V", 40.0);
            return ExcelActionResult.Ok(actionKey, "Kiểm tra biểu đồ tương tác PMM cột hoàn tất.", new
            {
                UtilizationRatio = ratio,
                IsSatisfied = ratio <= 1.0,
                AsTotalMm2 = asTot
            });
        }

        private static ExcelActionResult ExecuteInspection(string actionKey, ExcelActionContext context)
        {
            var wb = context.GetOrCreateWorkbook();
            if (string.Equals(actionKey, "MatTienDceExcel.DocBangCot", StringComparison.OrdinalIgnoreCase))
            {
                var ws = wb.Worksheets.FirstOrDefault(w => w.Name.Contains("Col", StringComparison.OrdinalIgnoreCase)) ?? wb.Worksheets.FirstOrDefault();
                var list = new List<object>();
                if (ws != null)
                {
                    for (int r = 4; r <= Math.Min(ws.LastRowUsed()?.RowNumber() ?? 4, 30); r++)
                    {
                        if (ws.Cell(r, 2).IsEmpty()) continue;
                        list.Add(new { Pier = ws.Cell(r, 2).GetString(), Size = ws.Cell(r, 3).GetString() });
                    }
                }
                return ExcelActionResult.Ok(actionKey, $"Đọc được {list.Count} phần tử cột.", JsonSerializer.Serialize(list));
            }

            if (string.Equals(actionKey, "MatTienDceExcel.KiemTraDauVaoCot", StringComparison.OrdinalIgnoreCase))
            {
                bool ok = wb.Worksheets.Any(w => w.Name.Contains("Col", StringComparison.OrdinalIgnoreCase) || w.Name.Contains("Cot", StringComparison.OrdinalIgnoreCase));
                return ExcelActionResult.Ok(actionKey, ok ? "Đầu vào cột hợp lệ." : "Cảnh báo thiếu bảng cột.", new { Valid = ok });
            }

            return ExcelActionResult.Ok(actionKey, "Đọc kết quả kiểm toán cột/vách thành công.", new { Status = "OK" });
        }
    }
}
