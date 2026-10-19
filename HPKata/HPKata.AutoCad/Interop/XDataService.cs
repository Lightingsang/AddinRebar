using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using HPKata.Core.Serialization;

namespace HPKata.AutoCad.Interop
{
    /// <summary>
    /// Bidirectional converter and interop service between AutoCAD ResultBuffer / TypedValue
    /// and KataXDataRecord using RegApp "KATA".
    /// </summary>
    public static class XDataService
    {
        public static void EnsureRegApp(Database db, string regAppName = KataXDataRecord.RegAppName)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));

            using var tr = db.TransactionManager.StartTransaction();
            var regTable = (RegAppTable)tr.GetObject(db.RegAppTableId, OpenMode.ForRead);
            if (!regTable.Has(regAppName))
            {
                regTable.UpgradeOpen();
                using var regRec = new RegAppTableRecord { Name = regAppName };
                regTable.Add(regRec);
                tr.AddNewlyCreatedDBObject(regRec, true);
            }
            tr.Commit();
        }

        public static TypedValue[] ToTypedValues(KataXDataRecord record)
        {
            if (record is null) throw new ArgumentNullException(nameof(record));

            List<KataTypedValue> dxfValues = KataXDataSerializer.SerializeToDxf(record);
            var typedValues = new TypedValue[dxfValues.Count];

            for (int i = 0; i < dxfValues.Count; i++)
            {
                typedValues[i] = new TypedValue(dxfValues[i].DxfCode, dxfValues[i].Value);
            }

            return typedValues;
        }

        public static ResultBuffer ToResultBuffer(KataXDataRecord record)
        {
            return new ResultBuffer(ToTypedValues(record));
        }

        public static KataXDataRecord FromTypedValues(IEnumerable<TypedValue>? values)
        {
            if (values is null) return new KataXDataRecord();

            var list = new List<KataTypedValue>();
            foreach (TypedValue tv in values)
            {
                list.Add(new KataTypedValue(tv.TypeCode, tv.Value));
            }

            return KataXDataSerializer.DeserializeFromDxf(list);
        }

        public static KataXDataRecord FromResultBuffer(ResultBuffer? rb)
        {
            if (rb is null) return new KataXDataRecord();

            var list = new List<KataTypedValue>();
            foreach (TypedValue tv in rb)
            {
                list.Add(new KataTypedValue(tv.TypeCode, tv.Value));
            }

            return KataXDataSerializer.DeserializeFromDxf(list);
        }

        public static KataXDataRecord ReadXData(DBObject obj, string regAppName = KataXDataRecord.RegAppName)
        {
            if (obj is null) return new KataXDataRecord();

            using ResultBuffer? rb = obj.GetXDataForApplication(regAppName);
            return FromResultBuffer(rb);
        }

        public static void WriteXData(DBObject obj, KataXDataRecord record)
        {
            if (obj is null) throw new ArgumentNullException(nameof(obj));
            if (record is null) throw new ArgumentNullException(nameof(record));

            using ResultBuffer rb = ToResultBuffer(record);
            obj.XData = rb;
        }

        public static bool HasKataXData(DBObject obj, string regAppName = KataXDataRecord.RegAppName)
        {
            if (obj is null) return false;
            using ResultBuffer? rb = obj.GetXDataForApplication(regAppName);
            return rb is not null;
        }
    }
}
