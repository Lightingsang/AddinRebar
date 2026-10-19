using System;
using Autodesk.AutoCAD.DatabaseServices;
using HPKata.Core.Serialization;

namespace HPKata.AutoCad.Interop
{
    /// <summary>
    /// Utility to apply canonical layer styling and attach KATA XData records to AutoCAD entities.
    /// </summary>
    public static class KataEntityStyler
    {
        public static void StyleAndTag(
            Entity ent,
            BlockTableRecord ms,
            Transaction tr,
            Database db,
            string layerName,
            KataXDataRecord? xdata = null)
        {
            if (ent == null) throw new ArgumentNullException(nameof(ent));
            if (ms == null) throw new ArgumentNullException(nameof(ms));
            if (tr == null) throw new ArgumentNullException(nameof(tr));
            if (db == null) throw new ArgumentNullException(nameof(db));

            ent.Layer = layerName;
            ms.AppendEntity(ent);
            tr.AddNewlyCreatedDBObject(ent, true);

            if (xdata != null)
            {
                XDataService.EnsureRegApp(db, KataXDataRecord.RegAppName);
                XDataService.WriteXData(ent, xdata);
            }
        }
    }
}
