using System;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using HPDcePro.Core.Drafting;
using HPDcePro.Core.XData;

namespace HPDcePro.AutoCad.Drafting
{
    public static class CadDraftingBufferRenderer
    {
        public static int Render(CadDraftingBuffer buffer, Database db, Transaction tr, BlockTableRecord btr)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (db == null) throw new ArgumentNullException(nameof(db));
            if (tr == null) throw new ArgumentNullException(nameof(tr));
            if (btr == null) throw new ArgumentNullException(nameof(btr));

            int renderedCount = 0;
            var regTable = (RegAppTable)tr.GetObject(db.RegAppTableId, OpenMode.ForWrite);

            for (int i = 0; i < buffer.Count; i++)
            {
                var p = buffer.Entities[i];
                Entity? ent = CreateEntity(p);
                if (ent == null) continue;

                ApplyCommonProperties(ent, p);
                AttachXData(ent, p, regTable, tr);

                btr.AppendEntity(ent);
                tr.AddNewlyCreatedDBObject(ent, true);
                renderedCount++;
            }
            return renderedCount;
        }

        private static Entity? CreateEntity(CadEntityPrimitive p)
        {
            switch (p.EntityType)
            {
                case CadEntityType.Line:
                    return new Line(new Point3d(p.StartPoint.X, p.StartPoint.Y, 0), new Point3d(p.EndPoint.X, p.EndPoint.Y, 0));

                case CadEntityType.Polyline:
                    return CreatePolyline(p);

                case CadEntityType.Circle:
                    return new Circle(new Point3d(p.Center.X, p.Center.Y, 0), Vector3d.ZAxis, p.Radius);

                case CadEntityType.Text:
                    return new DBText
                    {
                        Position = new Point3d(p.Position.X, p.Position.Y, 0),
                        Height = p.TextHeight,
                        TextString = p.TextString,
                        Rotation = p.RotationRad
                    };

                case CadEntityType.Dimension:
                    return new AlignedDimension(
                        new Point3d(p.DimensionPoint1.X, p.DimensionPoint1.Y, 0),
                        new Point3d(p.DimensionPoint2.X, p.DimensionPoint2.Y, 0),
                        new Point3d(p.TextPoint.X, p.TextPoint.Y, 0),
                        p.DimensionText,
                        ObjectId.Null);

                default:
                    return null;
            }
        }

        private static Polyline CreatePolyline(CadEntityPrimitive p)
        {
            var pline = new Polyline();
            for (int idx = 0; idx < p.Points.Count; idx++)
            {
                pline.AddVertexAt(idx, new Point2d(p.Points[idx].X, p.Points[idx].Y), 0.0, p.ConstantWidth, p.ConstantWidth);
            }
            pline.Closed = p.IsClosed;
            return pline;
        }

        private static void ApplyCommonProperties(Entity ent, CadEntityPrimitive p)
        {
            if (!string.IsNullOrEmpty(p.Layer))
            {
                ent.Layer = p.Layer;
            }
            if (p.Color.ColorIndex > 0 && p.Color.ColorIndex <= 256)
            {
                ent.Color = Color.FromColorIndex(ColorMethod.ByAci, (short)p.Color.ColorIndex);
            }
            if (p.LineWeight > 0)
            {
                ent.LineWeight = (LineWeight)Math.Min(211, (int)(p.LineWeight * 100));
            }
        }

        private static void AttachXData(Entity ent, CadEntityPrimitive p, RegAppTable regTable, Transaction tr)
        {
            if (p.AttachedXData.Count == 0) return;

            for (int i = 0; i < p.AttachedXData.Count; i++)
            {
                var packet = p.AttachedXData[i];
                EnsureRegApp(regTable, packet.RegAppName, tr);

                var list = new System.Collections.Generic.List<TypedValue>
                {
                    new TypedValue((int)DxfCode.ExtendedDataRegAppName, packet.RegAppName)
                };

                for (int v = 0; v < packet.Values.Count; v++)
                {
                    var (code, val) = packet.Values[v];
                    list.Add(new TypedValue(code, val));
                }

                ent.XData = new ResultBuffer(list.ToArray());
            }
        }

        private static void EnsureRegApp(RegAppTable regTable, string regAppName, Transaction tr)
        {
            if (!regTable.Has(regAppName))
            {
                var rec = new RegAppTableRecord { Name = regAppName };
                regTable.Add(rec);
                tr.AddNewlyCreatedDBObject(rec, true);
            }
        }
    }
}
