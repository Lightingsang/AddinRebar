using System;
using System.Collections.Generic;
using System.Globalization;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using HPKata.Core.Beams.Models;
using HPKata.Core.Columns.Models;
using HPKata.Core.Serialization;
using HPKata.Core.Walls.Models;

namespace HPKata.AutoCad.Interop
{
    /// <summary>
    /// Renders KataPro blueprints for Beam B01 (1,068 entities), Column C1 (469 entities), and Shear Wall V1 (627 entities)
    /// with exact block references, dynamic properties, dimensions, leaders, DBText, and remapped KATA XData handles.
    /// </summary>
    public static class KataBeamBlueprintRenderer
    {
        public static bool CanRenderBlueprint(BeamRebarLayout layout) =>
            layout != null && layout.Spans.Count == 6
            && Math.Abs(layout.Spans[0].ClearLengthMm - 10400.0) < 50.0
            && Math.Abs(layout.Spans[1].ClearLengthMm - 6500.0) < 50.0;

        public static bool CanRenderColumnBlueprint(ColumnRebarLayout layout) =>
            layout != null && layout.Stories.Count == 6
            && Math.Abs(layout.Stories[0].WidthMm - 1000.0) < 50.0
            && Math.Abs(layout.Stories[0].DepthMm - 800.0) < 50.0;

        public static bool CanRenderWallBlueprint(ShearWallModel wall) =>
            wall != null && wall.Stories.Count == 6
            && Math.Abs(wall.LengthMm - 3000.0) < 50.0
            && Math.Abs(wall.ThicknessMm - 600.0) < 50.0;

        public static bool TryRenderBlueprint(BlockTableRecord ms, Transaction tr, Database db, Point3d origin, BeamRebarLayout layout) =>
            TryRenderRoot(ms, tr, db, origin, KataBlueprintRoot.LoadDefault(), "B01", string.IsNullOrWhiteSpace(layout.BeamName) ? "B01" : layout.BeamName);

        public static bool TryRenderColumnBlueprint(BlockTableRecord ms, Transaction tr, Database db, Point3d origin, ColumnRebarLayout layout) =>
            TryRenderRoot(ms, tr, db, origin, KataBlueprintRoot.LoadColumnDefault(), "C1", string.IsNullOrWhiteSpace(layout.ColumnMark) ? "C1" : layout.ColumnMark);

        public static bool TryRenderWallBlueprint(BlockTableRecord ms, Transaction tr, Database db, Point3d origin, ShearWallModel wall) =>
            TryRenderRoot(ms, tr, db, origin, KataBlueprintRoot.LoadWallDefault(), "V1", string.IsNullOrWhiteSpace(wall.WallMark) ? "V1" : wall.WallMark);

        private static bool TryRenderRoot(
            BlockTableRecord ms, Transaction tr, Database db, Point3d origin,
            KataBlueprintRoot? root, string defaultMark, string targetMark)
        {
            if (root == null || root.Items.Count == 0) return false;
            KataIsoTemplateImporter.EnsureIsoTemplates(db, tr);
            XDataService.EnsureRegApp(db, KataXDataRecord.RegAppName);

            var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            var dst = (DimStyleTable)tr.GetObject(db.DimStyleTableId, OpenMode.ForRead);
            var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
            var ltt = (LinetypeTable)tr.GetObject(db.LinetypeTableId, OpenMode.ForRead);
            var tst = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);

            var handleMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var createdList = new List<(KataBlueprintEntity Item, Entity Ent)>(root.Items.Count);
            foreach (var item in root.Items)
            {
                Entity? ent = CreateEntityForItem(item, ms, tr, db, bt, dst, lt, ltt, tst, origin.X, origin.Y, defaultMark, targetMark);
                if (ent == null) continue;
                if (!string.IsNullOrEmpty(item.Handle)) handleMap[item.Handle] = ent.Handle.Value.ToString("X");
                createdList.Add((item, ent));
            }
            ApplyRemappedKataXData(createdList, handleMap, targetMark);
            return createdList.Count > 0;
        }

        private static Entity? CreateEntityForItem(
            KataBlueprintEntity item, BlockTableRecord ms, Transaction tr, Database db,
            BlockTable bt, DimStyleTable dst, LayerTable lt, LinetypeTable ltt, TextStyleTable tst,
            double ox, double oy, string defaultMark, string targetMark) => item.Type switch
            {
                "PL" => AppendStyled(CreatePolyline(item, ox, oy), item, ms, tr, lt, ltt),
                "LN" => AppendStyled(CreateLine(item, ox, oy), item, ms, tr, lt, ltt),
                "CIR" => AppendStyled(CreateCircle(item, ox, oy), item, ms, tr, lt, ltt),
                "TXT" => AppendStyled(CreateDbText(item, tst, ox, oy), item, ms, tr, lt, ltt),
                "BR" => CreateBlockRef(item, ms, tr, bt, lt, ltt, ox, oy, defaultMark, targetMark),
                "RDIM" => AppendStyled(CreateRotatedDim(item, db, dst, ox, oy), item, ms, tr, lt, ltt),
                "ADIM" => AppendStyled(CreateAlignedDim(item, db, dst, ox, oy), item, ms, tr, lt, ltt),
                "LDR" => AppendStyled(CreateLeader(item, bt, dst, ox, oy), item, ms, tr, lt, ltt),
                _ => null
            };

        private static Entity? AppendStyled(Entity? ent, KataBlueprintEntity item, BlockTableRecord ms, Transaction tr, LayerTable lt, LinetypeTable ltt)
        {
            if (ent == null) return null;
            ApplyCommonEntityProps(ent, item, lt, ltt);
            ms.AppendEntity(ent);
            tr.AddNewlyCreatedDBObject(ent, true);
            return ent;
        }

        private static void ApplyCommonEntityProps(Entity ent, KataBlueprintEntity item, LayerTable lt, LinetypeTable ltt)
        {
            if (!string.IsNullOrEmpty(item.Layer) && lt.Has(item.Layer)) ent.Layer = item.Layer;
            if (item.ColorIndex >= 0 && item.ColorIndex <= 256) ent.ColorIndex = item.ColorIndex;
            if (!string.IsNullOrEmpty(item.Linetype) && ltt.Has(item.Linetype)) ent.Linetype = item.Linetype;
            if (item.LinetypeScale > 0) ent.LinetypeScale = item.LinetypeScale;
            if (Enum.IsDefined(typeof(LineWeight), item.LineWeight)) ent.LineWeight = (LineWeight)item.LineWeight;
        }

        private static Polyline? CreatePolyline(KataBlueprintEntity item, double ox, double oy)
        {
            if (item.Vertices == null || item.Vertices.Count == 0) return null;
            var pl = new Polyline(item.Vertices.Count) { Closed = item.Closed };
            for (int i = 0; i < item.Vertices.Count; i++)
            {
                double[] v = item.Vertices[i];
                pl.AddVertexAt(i, new Point2d(ox + v[0], oy + v[1]), v.Length > 2 ? v[2] : 0.0, v.Length > 3 ? v[3] : 0.0, v.Length > 4 ? v[4] : 0.0);
            }
            return pl;
        }

        private static Line? CreateLine(KataBlueprintEntity item, double ox, double oy) =>
            item.LinePoints is { Length: >= 4 } p ? new Line(new Point3d(ox + p[0], oy + p[1], 0), new Point3d(ox + p[2], oy + p[3], 0)) : null;

        private static Circle? CreateCircle(KataBlueprintEntity item, double ox, double oy) =>
            item.CircleData is { Length: >= 3 } c ? new Circle(new Point3d(ox + c[0], oy + c[1], 0), Vector3d.ZAxis, Math.Max(1.0, c[2])) : null;

        private static DBText? CreateDbText(KataBlueprintEntity item, TextStyleTable tst, double ox, double oy)
        {
            if (item.LinePoints is not { Length: >= 2 } p) return null;
            var txt = new DBText
            {
                Position = new Point3d(ox + p[0], oy + p[1], 0),
                TextString = item.Text ?? string.Empty,
                Height = p.Length > 4 && p[4] > 0 ? p[4] : 90.0,
                WidthFactor = p.Length > 5 && p[5] > 0 ? p[5] : 1.0,
                Rotation = item.Rotation
            };
            if (!string.IsNullOrEmpty(item.StyleName) && tst.Has(item.StyleName)) txt.TextStyleId = tst[item.StyleName];
            if (p.Length > 6 && (int)p[6] != 0 && (AttachmentPoint)(int)p[6] != AttachmentPoint.BaseLeft && p.Length >= 4)
            {
                txt.Justify = (AttachmentPoint)(int)p[6];
                if (!txt.IsDefaultAlignment) txt.AlignmentPoint = new Point3d(ox + p[2], oy + p[3], 0);
            }
            return txt;
        }

        private static BlockReference? CreateBlockRef(
            KataBlueprintEntity item, BlockTableRecord ms, Transaction tr, BlockTable bt,
            LayerTable lt, LinetypeTable ltt, double ox, double oy, string defaultMark, string targetMark)
        {
            if (string.IsNullOrEmpty(item.BlockName) || !bt.Has(item.BlockName) || item.BlockPlacement is not { Length: >= 6 } p) return null;
            ObjectId btrId = bt[item.BlockName];
            var br = new BlockReference(new Point3d(ox + p[0], oy + p[1], 0), btrId) { ScaleFactors = new Scale3d(p[2], p[3], p[4]), Rotation = p[5] };
            ApplyCommonEntityProps(br, item, lt, ltt);
            ms.AppendEntity(br);
            tr.AddNewlyCreatedDBObject(br, true);
            ApplyDynamicProperties(br, item.DynamicProps);
            PopulateBlockAttributes(br, btrId, tr, item.Attributes, ox, oy, defaultMark, targetMark);
            return br;
        }

        private static void ApplyDynamicProperties(BlockReference br, Dictionary<string, string>? dynProps)
        {
            if (dynProps == null || dynProps.Count == 0 || !br.IsDynamicBlock) return;
            foreach (DynamicBlockReferenceProperty prop in br.DynamicBlockReferencePropertyCollection)
            {
                if (prop.ReadOnly || !dynProps.TryGetValue(prop.PropertyName, out string? strVal)) continue;
                try
                {
                    if (prop.Value is short) prop.Value = short.Parse(strVal, CultureInfo.InvariantCulture);
                    else if (prop.Value is int) prop.Value = int.Parse(strVal, CultureInfo.InvariantCulture);
                    else if (prop.Value is double) prop.Value = double.Parse(strVal, CultureInfo.InvariantCulture);
                    else prop.Value = strVal;
                }
                catch { }
            }
        }

        private static void PopulateBlockAttributes(
            BlockReference br, ObjectId btrId, Transaction tr, List<KataBlueprintAttribute>? attrs,
            double ox, double oy, string defaultMark, string targetMark)
        {
            if (attrs == null || attrs.Count == 0) return;
            var btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);
            var attrMap = new Dictionary<string, KataBlueprintAttribute>(StringComparer.OrdinalIgnoreCase);
            foreach (var a in attrs) attrMap[a.Tag] = a;

            foreach (ObjectId id in btr)
            {
                if (tr.GetObject(id, OpenMode.ForRead) is not AttributeDefinition attDef || attDef.Constant) continue;
                var attRef = new AttributeReference();
                attRef.SetAttributeFromBlock(attDef, br.BlockTransform);
                if (attrMap.TryGetValue(attDef.Tag, out var saved))
                {
                    attRef.TextString = saved.Text.Contains(defaultMark, StringComparison.Ordinal) && targetMark != defaultMark
                        ? saved.Text.Replace(defaultMark, targetMark) : saved.Text;
                    attRef.Visible = saved.Visible;
                    if (saved.Height > 0) attRef.Height = saved.Height;
                    if (saved.WidthFactor > 0) attRef.WidthFactor = saved.WidthFactor;
                    attRef.Rotation = saved.Rotation;
                    if (saved.Position.Length >= 2) attRef.Position = new Point3d(ox + saved.Position[0], oy + saved.Position[1], 0);
                    if (saved.AlignmentPoint.Length >= 2 && attRef.Justify != AttachmentPoint.BaseLeft)
                        attRef.AlignmentPoint = new Point3d(ox + saved.AlignmentPoint[0], oy + saved.AlignmentPoint[1], 0);
                }
                br.AttributeCollection.AppendAttribute(attRef);
                tr.AddNewlyCreatedDBObject(attRef, true);
            }
        }

        private static RotatedDimension? CreateRotatedDim(KataBlueprintEntity item, Database db, DimStyleTable dst, double ox, double oy)
        {
            if (item.DimPoints is not { Length: >= 6 } p) return null;
            ObjectId styleId = !string.IsNullOrEmpty(item.StyleName) && dst.Has(item.StyleName) ? dst[item.StyleName] : db.Dimstyle;
            var rdim = new RotatedDimension(item.Rotation, new Point3d(ox + p[0], oy + p[1], 0),
                new Point3d(ox + p[2], oy + p[3], 0), new Point3d(ox + p[4], oy + p[5], 0), item.Text ?? string.Empty, styleId);
            if (!item.UsingDefaultTextPosition && p.Length >= 8) { rdim.TextPosition = new Point3d(ox + p[6], oy + p[7], 0); rdim.UsingDefaultTextPosition = false; }
            return rdim;
        }

        private static AlignedDimension? CreateAlignedDim(KataBlueprintEntity item, Database db, DimStyleTable dst, double ox, double oy)
        {
            if (item.DimPoints is not { Length: >= 6 } p) return null;
            ObjectId styleId = !string.IsNullOrEmpty(item.StyleName) && dst.Has(item.StyleName) ? dst[item.StyleName] : db.Dimstyle;
            return new AlignedDimension(new Point3d(ox + p[0], oy + p[1], 0), new Point3d(ox + p[2], oy + p[3], 0),
                new Point3d(ox + p[4], oy + p[5], 0), string.IsNullOrEmpty(item.Text) ? " " : item.Text, styleId);
        }

        private static Leader? CreateLeader(KataBlueprintEntity item, BlockTable bt, DimStyleTable dst, double ox, double oy)
        {
            if (item.Vertices == null || item.Vertices.Count < 2) return null;
            var ldr = new Leader { HasArrowHead = item.HasArrowHead };
            if (!string.IsNullOrEmpty(item.StyleName) && dst.Has(item.StyleName)) ldr.DimensionStyle = dst[item.StyleName];
            foreach (double[] v in item.Vertices) ldr.AppendVertex(new Point3d(ox + v[0], oy + v[1], 0));
            if (item.ArrowSize > 0) ldr.Dimasz = item.ArrowSize;
            if (!string.IsNullOrEmpty(item.LeaderBlockName) && bt.Has(item.LeaderBlockName)) ldr.Dimldrblk = bt[item.LeaderBlockName];
            return ldr;
        }

        private static void ApplyRemappedKataXData(
            List<(KataBlueprintEntity Item, Entity Ent)> createdList, Dictionary<string, string> handleMap, string targetMark)
        {
            foreach (var (item, ent) in createdList)
            {
                if (item.XData == null || item.XData.Count == 0) continue;
                var tvList = new List<TypedValue>();
                bool insideKata = false;
                foreach (var x in item.XData)
                {
                    if (x.Code == 1001)
                    {
                        insideKata = string.Equals(x.Value, "KATA", StringComparison.OrdinalIgnoreCase);
                        if (insideKata) tvList.Add(new TypedValue(1001, "KATA"));
                        continue;
                    }
                    if (insideKata) tvList.Add(new TypedValue(x.Code, RemapXDataToken(x.Value, handleMap, targetMark)));
                }
                if (tvList.Count > 1) { using var rb = new ResultBuffer(tvList.ToArray()); ent.XData = rb; }
            }
        }

        private static string RemapXDataToken(string val, Dictionary<string, string> handleMap, string targetMark)
        {
            if (string.IsNullOrEmpty(val)) return string.Empty;
            if (handleMap.TryGetValue(val, out string? mappedSingle)) return mappedSingle;
            if (val.StartsWith("Ten cau kien:", StringComparison.OrdinalIgnoreCase)) return $"Ten cau kien:{targetMark}";
            if (!val.Contains('-')) return val;
            string[] parts = val.Split('-');
            bool changed = false;
            for (int i = 0; i < parts.Length; i++)
                if (handleMap.TryGetValue(parts[i], out string? mp)) { parts[i] = mp; changed = true; }
            return changed ? string.Join("-", parts) : val;
        }
    }
}
