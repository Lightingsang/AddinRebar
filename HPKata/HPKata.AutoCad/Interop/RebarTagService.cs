using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace HPKata.AutoCad.Interop
{
    /// <summary>
    /// Service for creating and inserting genuine rebar tag callout blocks into AutoCAD drawings.
    /// </summary>
    public static class RebarTagService
    {
        public const string DefaultTagBlockName = "thep";
        public const string AlternateTagBlockName = "kata_block_KHT";

        public static BlockReference InsertRebarTag(
            Database db,
            Point3d position,
            Polyline rebar,
            Transaction tr)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            if (rebar == null) throw new ArgumentNullException(nameof(rebar));
            if (tr == null) throw new ArgumentNullException(nameof(tr));

            ObjectId btrId = EnsureTagBlockTableRecord(db, tr);
            var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

            var blockRef = new BlockReference(position, btrId);
            ms.AppendEntity(blockRef);
            tr.AddNewlyCreatedDBObject(blockRef, true);

            AppendAttributesFromBlockRecord(blockRef, btrId, tr);
            HandleLinkService.CreateLink(rebar, blockRef, tr);

            return blockRef;
        }

        public static ObjectId EnsureTagBlockTableRecord(Database db, Transaction tr)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            if (tr == null) throw new ArgumentNullException(nameof(tr));

            var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            if (bt.Has(DefaultTagBlockName)) return bt[DefaultTagBlockName];
            if (bt.Has(AlternateTagBlockName)) return bt[AlternateTagBlockName];

            bt.UpgradeOpen();
            var btr = new BlockTableRecord { Name = DefaultTagBlockName };
            bt.Add(btr);
            tr.AddNewlyCreatedDBObject(btr, true);

            string[] tags = { "SH", "DK", "KC", "SL", "CD1T", "DKKC" };
            for (int i = 0; i < tags.Length; i++)
            {
                var attDef = new AttributeDefinition
                {
                    Tag = tags[i],
                    Prompt = tags[i],
                    TextString = string.Empty,
                    Position = new Point3d(0.0, i * 2.5, 0.0),
                    Height = 2.5
                };
                btr.AppendEntity(attDef);
                tr.AddNewlyCreatedDBObject(attDef, true);
            }

            return btr.ObjectId;
        }

        private static void AppendAttributesFromBlockRecord(
            BlockReference blockRef,
            ObjectId btrId,
            Transaction tr)
        {
            var btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);
            foreach (ObjectId entId in btr)
            {
                if (tr.GetObject(entId, OpenMode.ForRead) is AttributeDefinition attDef && !attDef.Constant)
                {
                    var attRef = new AttributeReference();
                    attRef.SetAttributeFromBlock(attDef, blockRef.BlockTransform);
                    attRef.Position = attDef.Position.TransformBy(blockRef.BlockTransform);
                    blockRef.AttributeCollection.AppendAttribute(attRef);
                    tr.AddNewlyCreatedDBObject(attRef, true);
                }
            }
        }
    }
}
