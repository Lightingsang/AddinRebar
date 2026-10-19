using System;
using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;

namespace HPDcePro.Excel
{
    public sealed class ExcelActionContext
    {
        public IXLWorkbook? Workbook { get; set; }
        public IXLWorkbook? ClosedXmlWorkbook
        {
            get => Workbook;
            set => Workbook = value;
        }

        public string? WorkbookPath { get; set; }
        public string? ActiveWorkbookPath
        {
            get => WorkbookPath;
            set => WorkbookPath = value;
        }

        public string? ActiveSheetName { get; set; }
        public string ProjectName { get; set; } = "Dự án mẫu";
        public string OutputDirectory { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public IXLWorkbook GetOrCreateWorkbook()
        {
            if (Workbook != null) return Workbook;

            if (!string.IsNullOrWhiteSpace(WorkbookPath) && File.Exists(WorkbookPath))
            {
                Workbook = new XLWorkbook(WorkbookPath);
                return Workbook;
            }

            Workbook = new XLWorkbook();
            return Workbook;
        }
    }
}
