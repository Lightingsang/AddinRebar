using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using HPKata.AutoCad.Abstractions;
using HPKata.Core.Serialization;

namespace HPKata.AutoCad.Interop
{
    /// <summary>
    /// Service for auditing drawing rebar marks, detecting duplicate STT numbers and geometric conflicts.
    /// </summary>
    public static class RebarMarkAuditService
    {
        public static int CheckDuplicateMarks(
            IEnumerable<KataXDataRecord> records,
            out List<string> conflictReports)
        {
            conflictReports = new List<string>();
            if (records == null) return 0;

            var validRecords = records.Where(r => r != null && r.STT > 0).ToList();
            if (validRecords.Count == 0) return 0;

            var groups = validRecords.GroupBy(r => r.STT).OrderBy(g => g.Key);

            foreach (var group in groups)
            {
                if (group.Count() <= 1) continue;

                KataXDataRecord first = group.First();
                double firstLen = CalculateTotalLength(first.ChieuDaiCacDoanMm);

                KataXDataRecord? conflict = group.FirstOrDefault(item =>
                    item.Phi != first.Phi ||
                    !string.Equals(item.Dang, first.Dang, StringComparison.OrdinalIgnoreCase) ||
                    Math.Abs(CalculateTotalLength(item.ChieuDaiCacDoanMm) - firstLen) > 1.0);

                if (conflict != null)
                {
                    double conflictLen = CalculateTotalLength(conflict.ChieuDaiCacDoanMm);
                    conflictReports.Add(
                        $"Conflicting mark {group.Key}: Different geometry or diameter detected " +
                        $"(Ø{first.Phi} shape '{first.Dang}' L={firstLen:F0}mm vs Ø{conflict.Phi} shape '{conflict.Dang}' L={conflictLen:F0}mm).");
                }
                else
                {
                    string componentInfo = string.IsNullOrWhiteSpace(first.TenCauKien)
                        ? string.Empty
                        : $" in component '{first.TenCauKien}'";
                    conflictReports.Add(
                        $"Duplicate mark {group.Key}: {group.Count()} rebar entities share mark number {group.Key}{componentInfo}.");
                }
            }

            return conflictReports.Count;
        }

        public static int CheckDuplicateMarks(
            ICadDocumentContext context,
            out List<string> conflictReports)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            IReadOnlyList<CadEntityDto> entities = context.GetAllRebarEntities();
            var records = new List<KataXDataRecord>();
            foreach (CadEntityDto ent in entities)
            {
                if (ent.XDataRecord != null && ent.XDataRecord.STT > 0)
                {
                    records.Add(ent.XDataRecord);
                }
            }

            int count = CheckDuplicateMarks(records, out conflictReports);
            if (count == 0)
            {
                context.WriteMessage($"\n[HPKata] Scanned {records.Count} rebar entities: 0 duplicate marks detected.\n");
            }
            else
            {
                context.WriteMessage($"\n[HPKata] Scanned {records.Count} rebar entities: {count} duplicate marks detected.\n");
                foreach (string report in conflictReports)
                {
                    context.WriteMessage($"  - {report}\n");
                }
            }

            return count;
        }

        public static int CheckDuplicateMarksInDatabase(Database db, Editor ed)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            if (ed == null) throw new ArgumentNullException(nameof(ed));

            var records = new List<KataXDataRecord>();
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                foreach (ObjectId id in ms)
                {
                    if (tr.GetObject(id, OpenMode.ForRead) is Entity ent && XDataService.HasKataXData(ent))
                    {
                        KataXDataRecord r = XDataService.ReadXData(ent);
                        if (r.STT > 0)
                        {
                            records.Add(r);
                        }
                    }
                }
                tr.Commit();
            }

            int count = CheckDuplicateMarks(records, out List<string> reports);
            if (count == 0)
            {
                ed.WriteMessage($"\n[HPKata] Scanned drawing: {records.Count} rebar entities inspected, 0 duplicate rebar marks detected.\n");
            }
            else
            {
                ed.WriteMessage($"\n[HPKata] Scanned drawing: {records.Count} rebar entities inspected, {count} duplicate/conflicting marks detected:\n");
                foreach (string report in reports)
                {
                    ed.WriteMessage($"  * {report}\n");
                }
            }

            return count;
        }

        private static double CalculateTotalLength(IReadOnlyList<double>? segments)
        {
            if (segments == null || segments.Count == 0) return 0.0;
            double sum = 0.0;
            for (int i = 0; i < segments.Count; i++)
            {
                sum += segments[i];
            }
            return sum;
        }
    }
}
