using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using HPGeo.Core.Import;

namespace HPGeo.AutoCad.Cad;

/// <summary>What one import wrote: counts and the handles, for the console and the log.</summary>
internal sealed record DrawingWriteResult(int PointCount, int PolylineCount, string Layer, IReadOnlyList<string> Handles);

/// <summary>
/// Draws an <see cref="ImportPlan"/> into model space in one transaction — one Undo step — on the layer
/// <see cref="LayerName"/>, created on demand (green, so imported geometry is never mistaken for surveyed
/// geometry). POINT entities for points, LWPOLYLINE for lines and rings. Nothing else in the drawing is touched.
/// </summary>
internal static class DrawingWriter
{
    public const string LayerName = "HPGEO-IMPORT";
    private const short LayerColorIndex = 3; // green

    public static DrawingWriteResult Write(Database db, ImportPlan plan)
    {
        var handles = new List<string>();
        using var tr = db.TransactionManager.StartTransaction();
        var layerId = EnsureLayer(tr, db);
        var blockTable = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
        var modelSpace = (BlockTableRecord)tr.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

        foreach (var p in plan.Points)
        {
            var point = new DBPoint(new Point3d(p.DrawingXY.Easting, p.DrawingXY.Northing, 0)) { LayerId = layerId };
            modelSpace.AppendEntity(point);
            tr.AddNewlyCreatedDBObject(point, true);
            handles.Add(point.Handle.ToString());
        }
        foreach (var pl in plan.Polylines)
        {
            var polyline = new Polyline(pl.DrawingVertices.Count) { LayerId = layerId };
            for (var i = 0; i < pl.DrawingVertices.Count; i++)
                polyline.AddVertexAt(i, new Point2d(pl.DrawingVertices[i].Easting, pl.DrawingVertices[i].Northing), 0, 0, 0);
            polyline.Closed = pl.Closed;
            modelSpace.AppendEntity(polyline);
            tr.AddNewlyCreatedDBObject(polyline, true);
            handles.Add(polyline.Handle.ToString());
        }
        tr.Commit();
        return new DrawingWriteResult(plan.Points.Count, plan.Polylines.Count, LayerName, handles);
    }

    private static ObjectId EnsureLayer(Transaction tr, Database db)
    {
        var layers = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
        if (layers.Has(LayerName))
        {
            var existing = (LayerTableRecord)tr.GetObject(layers[LayerName], OpenMode.ForRead);
            if (existing.IsLocked || existing.IsFrozen)
                throw new InvalidOperationException($"Layer {LayerName} đang bị khoá/đóng băng — mở khoá rồi nhập lại.");
            return layers[LayerName];
        }
        layers.UpgradeOpen();
        var record = new LayerTableRecord { Name = LayerName, Color = Color.FromColorIndex(ColorMethod.ByAci, LayerColorIndex) };
        var id = layers.Add(record);
        tr.AddNewlyCreatedDBObject(record, true);
        return id;
    }
}
