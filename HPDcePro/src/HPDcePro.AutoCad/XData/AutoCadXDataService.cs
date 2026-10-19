using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using HPDcePro.Core.XData;

namespace HPDcePro.AutoCad.XData
{
    /// <summary>
    /// Service for bidirectional translation between AutoCAD ResultBuffer XData and HPDcePro XDataPacket.
    /// </summary>
    public static class AutoCadXDataService
    {
        public static ResultBuffer ToResultBuffer(XDataPacket packet)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));

            var values = new List<TypedValue>
            {
                new TypedValue((int)DxfCode.ExtendedDataRegAppName, packet.RegAppName)
            };

            for (int i = 0; i < packet.Values.Count; i++)
            {
                var (code, val) = packet.Values[i];
                values.Add(new TypedValue(code, val));
            }

            return new ResultBuffer(values.ToArray());
        }

        public static XDataPacket? FromResultBuffer(ResultBuffer? rb, string regAppName)
        {
            if (rb == null) return null;

            var packet = new XDataPacket(regAppName);
            var enumerator = rb.GetEnumerator();
            bool foundApp = false;

            while (enumerator.MoveNext())
            {
                var tv = (TypedValue)enumerator.Current;
                if (tv.TypeCode == (short)DxfCode.ExtendedDataRegAppName)
                {
                    string app = tv.Value?.ToString() ?? string.Empty;
                    foundApp = string.Equals(app, regAppName, StringComparison.OrdinalIgnoreCase);
                    continue;
                }

                if (foundApp)
                {
                    packet.Values.Add((tv.TypeCode, tv.Value ?? string.Empty));
                }
            }

            return packet;
        }

        public static XDataPacket? ReadPacket(DBObject obj, string regAppName)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            if (string.IsNullOrEmpty(regAppName)) throw new ArgumentNullException(nameof(regAppName));

            using var rb = obj.GetXDataForApplication(regAppName);
            if (rb == null) return null;

            return FromResultBuffer(rb, regAppName);
        }

        public static void WritePacket(DBObject obj, XDataPacket packet, Transaction tr)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            if (packet == null) throw new ArgumentNullException(nameof(packet));
            if (tr == null) throw new ArgumentNullException(nameof(tr));

            var db = obj.Database;
            if (db != null)
            {
                EnsureRegApp(db, packet.RegAppName, tr);
            }

            using var rb = ToResultBuffer(packet);
            obj.XData = rb;
        }

        private static void EnsureRegApp(Database db, string regAppName, Transaction tr)
        {
            var regTable = (RegAppTable)tr.GetObject(db.RegAppTableId, OpenMode.ForRead);
            if (!regTable.Has(regAppName))
            {
                regTable.UpgradeOpen();
                var rec = new RegAppTableRecord { Name = regAppName };
                regTable.Add(rec);
                tr.AddNewlyCreatedDBObject(rec, true);
            }
        }
    }
}
