using System;
using System.Collections.Generic;
using HPKata.Core.Excel.Data;
using HPKata.Core.Models;

namespace HPKata.Core.Excel
{
    /// <summary>
    /// Bi-directional adapter for reading and writing structural detailing sheets
    /// and schedule reports from/to Kata.xlsm and Iso_QHPlus.xls.
    /// </summary>
    public interface IKataExcelAdapter
    {
        bool IsDesktopExcelOpen { get; }

        bool ReadBeamSheet(string? workbookPath, out KataBeamSheetData data);
        bool WriteBeamSheet(string? workbookPath, in KataBeamSheetData data);

        bool ReadColumnSheet(string? workbookPath, out KataColumnSheetData data);
        bool WriteColumnSheet(string? workbookPath, in KataColumnSheetData data);

        bool ReadWallSheet(string? workbookPath, out KataWallSheetData data);
        bool WriteWallSheet(string? workbookPath, in KataWallSheetData data);

        bool ExportSchedule(string? workbookPath, IEnumerable<KataRebarScheduleItem> items);
        bool ExportSchedule(string? workbookPath, IEnumerable<RebarItem> items);

        bool ExportQhPlusSchedule(string? workbookPath, IEnumerable<KataRebarScheduleItem> items);
        bool ExportQhPlusSchedule(string? workbookPath, IEnumerable<RebarItem> items);
    }
}
