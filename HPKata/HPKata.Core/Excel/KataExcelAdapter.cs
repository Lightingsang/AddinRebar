using System;
using System.Collections.Generic;
using HPKata.Core.Configuration;
using HPKata.Core.Excel.Data;
using HPKata.Core.Excel.Mappers;
using HPKata.Core.Excel.ReadersWriters;
using HPKata.Core.Excel.Session;
using HPKata.Core.Models;

namespace HPKata.Core.Excel
{
    /// <summary>
    /// Master adapter coordinating Excel sessions, readers, writers, and schedule exporters.
    /// </summary>
    public sealed class KataExcelAdapter : IKataExcelAdapter
    {
        public bool IsDesktopExcelOpen => KataExcelSessionProvider.IsDesktopExcelOpen;

        public bool ReadBeamSheet(string? workbookPath, out KataBeamSheetData data)
        {
            data = new KataBeamSheetData();
            try
            {
                string path = ResolveWorkbookPath(workbookPath);
                using var session = KataExcelSessionProvider.OpenSession(path);
                return KataBeamSheetReader.Read(session, out data);
            }
            catch (Exception)
            {
                // Graceful fallback when workbook is corrupt, missing, locked, or inaccessible
                return false;
            }
        }

        public bool WriteBeamSheet(string? workbookPath, in KataBeamSheetData data)
        {
            try
            {
                string path = ResolveWorkbookPath(workbookPath);
                using var session = KataExcelSessionProvider.OpenSession(path);
                return KataBeamSheetWriter.Write(session, in data);
            }
            catch (Exception)
            {
                // Graceful fallback when workbook is corrupt, missing, locked, or inaccessible
                return false;
            }
        }

        public bool ReadColumnSheet(string? workbookPath, out KataColumnSheetData data)
        {
            data = new KataColumnSheetData();
            try
            {
                string path = ResolveWorkbookPath(workbookPath);
                using var session = KataExcelSessionProvider.OpenSession(path);
                return KataColumnSheetReader.Read(session, out data);
            }
            catch (Exception)
            {
                // Graceful fallback when workbook is corrupt, missing, locked, or inaccessible
                return false;
            }
        }

        public bool WriteColumnSheet(string? workbookPath, in KataColumnSheetData data)
        {
            try
            {
                string path = ResolveWorkbookPath(workbookPath);
                using var session = KataExcelSessionProvider.OpenSession(path);
                return KataColumnSheetWriter.Write(session, in data);
            }
            catch (Exception)
            {
                // Graceful fallback when workbook is corrupt, missing, locked, or inaccessible
                return false;
            }
        }

        public bool ReadWallSheet(string? workbookPath, out KataWallSheetData data)
        {
            data = new KataWallSheetData();
            try
            {
                string path = ResolveWorkbookPath(workbookPath);
                using var session = KataExcelSessionProvider.OpenSession(path);
                return KataWallSheetReader.Read(session, out data);
            }
            catch (Exception)
            {
                // Graceful fallback when workbook is corrupt, missing, locked, or inaccessible
                return false;
            }
        }

        public bool WriteWallSheet(string? workbookPath, in KataWallSheetData data)
        {
            try
            {
                string path = ResolveWorkbookPath(workbookPath);
                using var session = KataExcelSessionProvider.OpenSession(path);
                return KataWallSheetWriter.Write(session, in data);
            }
            catch (Exception)
            {
                // Graceful fallback when workbook is corrupt, missing, locked, or inaccessible
                return false;
            }
        }

        public bool ExportSchedule(string? workbookPath, IEnumerable<KataRebarScheduleItem> items)
        {
            try
            {
                string path = ResolveWorkbookPath(workbookPath);
                using var session = KataExcelSessionProvider.OpenSession(path);
                return KataScheduleSheetWriter.Write(session, items);
            }
            catch (Exception)
            {
                // Graceful fallback when workbook is corrupt, missing, locked, or inaccessible
                return false;
            }
        }

        public bool ExportSchedule(string? workbookPath, IEnumerable<RebarItem> items)
        {
            var dtos = KataScheduleItemMapper.ToScheduleItemList(items);
            return ExportSchedule(workbookPath, dtos);
        }

        public bool ExportQhPlusSchedule(string? workbookPath, IEnumerable<KataRebarScheduleItem> items)
        {
            try
            {
                return KataQhPlusExporter.Export(workbookPath, items);
            }
            catch (Exception)
            {
                // Graceful fallback when export fails or workbook is corrupt
                return false;
            }
        }

        public bool ExportQhPlusSchedule(string? workbookPath, IEnumerable<RebarItem> items)
        {
            var dtos = KataScheduleItemMapper.ToScheduleItemList(items);
            return ExportQhPlusSchedule(workbookPath, dtos);
        }

        private static string ResolveWorkbookPath(string? path)
        {
            return KataExcelSessionProvider.ResolveBestWorkbookPath(path);
        }
    }
}
