using System;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;

namespace HPKata.AutoCad.Interop
{
    /// <summary>
    /// Helper ensuring standard AutoCAD layers and styling conventions matching Iso_kata.dwg.
    /// </summary>
    public static class KataLayerHelper
    {
        public const string ThepDoc = "kata_thep chu";
        public const string ThepDai = "kata_thep dai";
        public const string NetThay = "kata_net thay";
        public const string NetKhung = "kata_net khung";
        public const string NetKhuat = "kata_net khuat";
        public const string Dim = "kata_dim";
        public const string Text = "kata_text";
        public const string ThepWallV = "kata_thep - V";
        public const string ThepWallH = "kata_thep - H";
        public const string Grid = "kata_grid";
        public const string NetManh = "kata_net manh";
        public const string Defpoints = "Defpoints";

        public static void EnsureStandardLayers(Transaction tr, Database db)
        {
            if (tr == null) throw new ArgumentNullException(nameof(tr));
            if (db == null) throw new ArgumentNullException(nameof(db));

            var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
            var ltt = (LinetypeTable)tr.GetObject(db.LinetypeTableId, OpenMode.ForRead);

            EnsureLayerSpec(tr, lt, ltt, ThepDoc, 1, "Continuous", LineWeight.LineWeight035, true);
            EnsureLayerSpec(tr, lt, ltt, ThepDai, 220, "Continuous", LineWeight.LineWeight020, true);
            EnsureLayerSpec(tr, lt, ltt, NetThay, 4, "Continuous", LineWeight.LineWeight020, true);
            EnsureLayerSpec(tr, lt, ltt, NetKhung, 3, "Continuous", LineWeight.LineWeight020, true);
            EnsureLayerSpec(tr, lt, ltt, NetKhuat, 9, "HIDDEN", LineWeight.LineWeight005, true);
            EnsureLayerSpec(tr, lt, ltt, Dim, 8, "Continuous", LineWeight.LineWeight005, true);
            EnsureLayerSpec(tr, lt, ltt, Text, 7, "Continuous", LineWeight.LineWeight030, true);
            EnsureLayerSpec(tr, lt, ltt, ThepWallV, 2, "Continuous", LineWeight.LineWeight035, true);
            EnsureLayerSpec(tr, lt, ltt, ThepWallH, 1, "Continuous", LineWeight.LineWeight035, true);
            EnsureLayerSpec(tr, lt, ltt, Grid, 8, "CENTER", LineWeight.LineWeight005, true);
            EnsureLayerSpec(tr, lt, ltt, NetManh, 8, "Continuous", LineWeight.LineWeight005, true);
            EnsureLayerSpec(tr, lt, ltt, Defpoints, 7, "Continuous", LineWeight.ByLineWeightDefault, false);
        }

        public static ObjectId EnsureLayer(Transaction tr, LayerTable lt, string layerName, Color color)
        {
            if (lt.Has(layerName)) return lt[layerName];

            lt.UpgradeOpen();
            using var ltr = new LayerTableRecord
            {
                Name = layerName,
                Color = color
            };
            ObjectId id = lt.Add(ltr);
            tr.AddNewlyCreatedDBObject(ltr, true);
            return id;
        }

        public static ObjectId EnsureLayerSpec(
            Transaction tr,
            LayerTable lt,
            LinetypeTable ltt,
            string layerName,
            short aciColor,
            string linetypeName,
            LineWeight lineWeight,
            bool isPlottable)
        {
            Color color = Color.FromColorIndex(ColorMethod.ByAci, aciColor);
            ObjectId ltypeId = ltt.Has(linetypeName)
                ? ltt[linetypeName]
                : (ltt.Has("Continuous") ? ltt["Continuous"] : ObjectId.Null);

            if (lt.Has(layerName))
            {
                ObjectId existingId = lt[layerName];
                var existing = (LayerTableRecord)tr.GetObject(existingId, OpenMode.ForWrite);
                existing.Color = color;
                if (!ltypeId.IsNull) existing.LinetypeObjectId = ltypeId;
                existing.LineWeight = lineWeight;
                existing.IsPlottable = isPlottable;
                return existingId;
            }

            lt.UpgradeOpen();
            using var ltr = new LayerTableRecord
            {
                Name = layerName,
                Color = color,
                LineWeight = lineWeight,
                IsPlottable = isPlottable
            };
            if (!ltypeId.IsNull) ltr.LinetypeObjectId = ltypeId;
            ObjectId id = lt.Add(ltr);
            tr.AddNewlyCreatedDBObject(ltr, true);
            return id;
        }
    }
}
