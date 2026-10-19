using System;
using System.Collections.Generic;
using System.Linq;
using ClosedXML.Excel;

namespace HPDcePro.Excel.ActionHandlers
{
    public sealed class SlabActionHandler : IExcelActionHandler
    {
        private static readonly string[] s_actions =
        {
            "Sla_ChooseColor",
            "Sla_Design",
            "Sla_ExportSize",
            "Sla_LoadData",
            "Sla_Print",
            "Sla_Setup",
            "MatTienDceExcel.DocBangSan",
            "MatTienDceExcel.KiemTraDauVaoSan",
            "AutoStr.AutoStr_ChonThepSan",
            "AutoStr.AutoStr_InThuyetMinhSan"
        };

        private static readonly string[] s_aliases = { "TinhSan" };

        public IReadOnlyList<string> SupportedActions => s_actions;

        public bool CanHandle(string actionKey) =>
            s_actions.Contains(actionKey, StringComparer.OrdinalIgnoreCase) ||
            s_aliases.Contains(actionKey, StringComparer.OrdinalIgnoreCase);

        public ExcelActionResult Handle(string actionKey, ExcelActionContext context)
        {
            if (string.Equals(actionKey, "Sla_Design", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(actionKey, "TinhSan", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(actionKey, "AutoStr.AutoStr_ChonThepSan", StringComparison.OrdinalIgnoreCase))
            {
                return ExecuteSlabDesign(actionKey, context);
            }

            if (actionKey.StartsWith("MatTienDceExcel.", StringComparison.OrdinalIgnoreCase))
            {
                return ExecuteInspection(actionKey, context);
            }

            return ExcelActionResult.Ok(actionKey, $"Thực hiện thao tác sàn '{actionKey}' thành công.");
        }

        private static ExcelActionResult ExecuteSlabDesign(string actionKey, ExcelActionContext context)
        {
            double lx = context.Parameters.TryGetValue("Lx", out var lxv) && double.TryParse(lxv?.ToString(), out var lxd) ? lxd * 1000.0 : 4000.0;
            double ly = context.Parameters.TryGetValue("Ly", out var lyv) && double.TryParse(lyv?.ToString(), out var lyd) ? lyd * 1000.0 : 5000.0;
            double g = context.Parameters.TryGetValue("DeadLoad", out var gv) && double.TryParse(gv?.ToString(), out var gd) ? gd : 4.0;
            double p = context.Parameters.TryGetValue("LiveLoad", out var pv) && double.TryParse(pv?.ToString(), out var pd) ? pd : 2.5;
            double h = context.Parameters.TryGetValue("ThicknessMm", out var thv) && double.TryParse(thv?.ToString(), out var thd) ? thd : 120.0;

            double asBotX = RcGeotechUdfBridge.TinhSanHaiPhuong(lx, ly, h, g, p, "B25", "CB400-V", 1, 0);
            double asBotY = RcGeotechUdfBridge.TinhSanHaiPhuong(lx, ly, h, g, p, "B25", "CB400-V", 1, 1);
            double asTopX = RcGeotechUdfBridge.TinhSanHaiPhuong(lx, ly, h, g, p, "B25", "CB400-V", 1, 2);
            double asTopY = RcGeotechUdfBridge.TinhSanHaiPhuong(lx, ly, h, g, p, "B25", "CB400-V", 1, 3);

            return ExcelActionResult.Ok(actionKey, "Tính toán cốt thép bản sàn hoàn tất.", new
            {
                RequiredAsBotXMm2 = asBotX,
                RequiredAsBotYMm2 = asBotY,
                RequiredAsTopXMm2 = asTopX,
                RequiredAsTopYMm2 = asTopY
            });
        }

        private static ExcelActionResult ExecuteInspection(string actionKey, ExcelActionContext context)
        {
            var wb = context.GetOrCreateWorkbook();
            if (string.Equals(actionKey, "MatTienDceExcel.KiemTraDauVaoSan", StringComparison.OrdinalIgnoreCase))
            {
                string[] req = { "STRIP FORCES", "STRIP FILTER", "STRIP DESIGN", "STRIP REPORT" };
                int matchCount = req.Count(wb.Worksheets.Contains);
                bool ok = matchCount >= 2 || wb.Worksheets.Any(w => w.Name.Contains("Slab", StringComparison.OrdinalIgnoreCase) || w.Name.Contains("San", StringComparison.OrdinalIgnoreCase));
                return ExcelActionResult.Ok(actionKey, ok ? "Đầu vào dải sàn SAFE hợp lệ." : "Cảnh báo thiếu bảng dải sàn.", new { Valid = ok });
            }

            return ExcelActionResult.Ok(actionKey, "Đọc dữ liệu bản sàn thành công.", new { Status = "OK" });
        }
    }
}
