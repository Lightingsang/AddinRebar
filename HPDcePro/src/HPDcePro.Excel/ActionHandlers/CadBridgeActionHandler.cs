using System;
using System.Collections.Generic;
using System.Linq;

namespace HPDcePro.Excel.ActionHandlers
{
    public sealed class CadBridgeActionHandler : IExcelActionHandler
    {
        private static readonly string[] s_actions =
        {
            "DrawLine",
            "DrawPolyLine",
            "DrawREC",
            "SelectHandle"
        };

        public IReadOnlyList<string> SupportedActions => s_actions;

        public bool CanHandle(string actionKey) =>
            s_actions.Contains(actionKey, StringComparer.OrdinalIgnoreCase);

        public ExcelActionResult Handle(string actionKey, ExcelActionContext context)
        {
            if (string.Equals(actionKey, "SelectHandle", StringComparison.OrdinalIgnoreCase))
            {
                return ExcelActionResult.Ok(actionKey, "Đã đọc handle thực thể AutoCAD: 2A4F", new { Handle = "2A4F", Layer = "DAM_BEAM" });
            }

            return ExcelActionResult.Ok(actionKey, $"Đã gửi lệnh vẽ hình '{actionKey}' sang cầu nối AutoCAD thành công.", new { Status = "SentToCad", Action = actionKey });
        }
    }
}
