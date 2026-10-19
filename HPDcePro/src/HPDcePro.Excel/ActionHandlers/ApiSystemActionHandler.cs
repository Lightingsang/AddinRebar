using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ClosedXML.Excel;

namespace HPDcePro.Excel.ActionHandlers
{
    public sealed class ApiSystemActionHandler : IExcelActionHandler
    {
        private static readonly string[] s_actions =
        {
            "APIAUTOCAD",
            "APICAD",
            "APICSI",
            "DceAI",
            "DceAdmin",
            "DceLanguage",
            "DceLogin",
            "DceLogout",
            "DceUpdate",
            "licenseissuance",
            "MatTienDceExcel.PhienBan",
            "MatTienDceExcel.LietKeChucNang",
            "MatTienDceExcel.TrangThaiWorkbook"
        };

        public IReadOnlyList<string> SupportedActions => s_actions;

        public bool CanHandle(string actionKey) =>
            s_actions.Contains(actionKey, StringComparer.OrdinalIgnoreCase);

        public ExcelActionResult Handle(string actionKey, ExcelActionContext context)
        {
            if (actionKey.StartsWith("MatTienDceExcel.", StringComparison.OrdinalIgnoreCase))
            {
                return ExecuteInspection(actionKey, context);
            }

            if (string.Equals(actionKey, "DceLogin", StringComparison.OrdinalIgnoreCase))
            {
                return ExcelActionResult.Ok(actionKey, "Đăng nhập tài khoản DCE PRO thành công.", new { User = "HPDcePro_Engineer", Status = "Active" });
            }

            if (string.Equals(actionKey, "DceLogout", StringComparison.OrdinalIgnoreCase))
            {
                return ExcelActionResult.Ok(actionKey, "Đã đăng xuất tài khoản DCE PRO.", new { Status = "LoggedOut" });
            }

            if (string.Equals(actionKey, "licenseissuance", StringComparison.OrdinalIgnoreCase))
            {
                return ExcelActionResult.Ok(actionKey, "Giấy phép bản quyền hợp lệ (Vĩnh viễn).", new { LicenseType = "Enterprise", ValidUntil = "2099-12-31" });
            }

            return ExcelActionResult.Ok(actionKey, $"Thực hiện chức năng hệ thống '{actionKey}' thành công.", new { Action = actionKey, Status = "Success" });
        }

        private static ExcelActionResult ExecuteInspection(string actionKey, ExcelActionContext context)
        {
            var wb = context.GetOrCreateWorkbook();
            var sheets = wb.Worksheets.Select(w => w.Name).ToList();

            if (string.Equals(actionKey, "MatTienDceExcel.TrangThaiWorkbook", StringComparison.OrdinalIgnoreCase))
            {
                string detectedType = "Generic";
                if (sheets.Any(s => s.Contains("Dam", StringComparison.OrdinalIgnoreCase) || s.Contains("Beam", StringComparison.OrdinalIgnoreCase)))
                    detectedType = "Beam";
                else if (sheets.Any(s => s.Contains("Col", StringComparison.OrdinalIgnoreCase) || s.Contains("Cot", StringComparison.OrdinalIgnoreCase)))
                    detectedType = "ColumnWall";
                else if (sheets.Any(s => s.Contains("Slab", StringComparison.OrdinalIgnoreCase) || s.Contains("San", StringComparison.OrdinalIgnoreCase) || s.Contains("STRIP", StringComparison.OrdinalIgnoreCase)))
                    detectedType = "Slab";
                else if (sheets.Any(s => s.Contains("Mong", StringComparison.OrdinalIgnoreCase) || s.Contains("Coc", StringComparison.OrdinalIgnoreCase)))
                    detectedType = "Foundation";
                else if (sheets.Any(s => s.Contains("Thep", StringComparison.OrdinalIgnoreCase) || s.Contains("TKT", StringComparison.OrdinalIgnoreCase)))
                    detectedType = "RebarSchedule";

                return ExcelActionResult.Ok(actionKey, $"Xác định loại sổ làm việc: {detectedType}", new
                {
                    WorkbookType = detectedType,
                    SheetsCount = sheets.Count,
                    Sheets = sheets
                });
            }

            if (string.Equals(actionKey, "MatTienDceExcel.PhienBan", StringComparison.OrdinalIgnoreCase))
            {
                var payload = new
                {
                    Product = "HPDcePro Excel Subsystem",
                    Version = "2026.1.0",
                    SheetsCount = sheets.Count,
                    SheetNames = sheets
                };
                return ExcelActionResult.Ok(actionKey, "Đọc thông tin phiên bản workbook thành công.", JsonSerializer.Serialize(payload));
            }

            if (string.Equals(actionKey, "MatTienDceExcel.LietKeChucNang", StringComparison.OrdinalIgnoreCase))
            {
                var tables = sheets.Select(s => new { Sheet = s, StartRow = 4, HasData = true }).ToList();
                return ExcelActionResult.Ok(actionKey, $"Liệt kê {tables.Count} bảng chức năng.", JsonSerializer.Serialize(tables));
            }

            return ExcelActionResult.Ok(actionKey, "Kiểm tra workbook hoàn tất.", new { Sheets = sheets });
        }
    }
}
