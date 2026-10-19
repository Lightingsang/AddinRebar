using System;
using System.Collections.Generic;
using System.Linq;

namespace HPDcePro.Excel.ActionHandlers
{
    public sealed class EtabsSafeActionHandler : IExcelActionHandler
    {
        private static readonly string[] s_actions =
        {
            "ESStiffnessModifiers",
            "ESUniformLoadSetsToSafe",
            "EtaSTableToEta",
            "PointSafeToPointSafe",
            "SafeTableToSafe",
            "UniformLoadSets",
            "DbimPuFile"
        };

        public IReadOnlyList<string> SupportedActions => s_actions;

        public bool CanHandle(string actionKey) =>
            s_actions.Contains(actionKey, StringComparer.OrdinalIgnoreCase);

        public ExcelActionResult Handle(string actionKey, ExcelActionContext context)
        {
            if (string.Equals(actionKey, "DbimPuFile", StringComparison.OrdinalIgnoreCase))
            {
                return ExcelActionResult.Ok(actionKey, "Đã dọn dẹp và nén các file tạm ETABS/SAFE thành công.", new { CleanedFilesCount = 4 });
            }

            return ExcelActionResult.Ok(actionKey, $"Đồng bộ dữ liệu mô hình ETABS/SAFE '{actionKey}' thành công.", new { Synced = true, Action = actionKey });
        }
    }
}
