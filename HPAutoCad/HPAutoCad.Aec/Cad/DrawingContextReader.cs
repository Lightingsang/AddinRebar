using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using HPAutoCad.Aec.Geometry;
using HPRebar.McpBridge.Core.Scripting;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;
using AcadException = Autodesk.AutoCAD.Runtime.Exception;

namespace HPAutoCad.Aec.Cad;

/// <summary>
///     The drawing as the AI should picture it before touching anything: identity, units, active
///     space/layout/layer, UCS, extents, annotation scale and styles, the current view, and table
///     counts. One transaction read, no entity geometry, so it is cheap on any drawing size.
/// </summary>
public static class DrawingContextReader
{
    public static object Read(Document doc, Database db, Editor ed, Transaction tr, ScriptUnits units, CancellationToken ct, bool includeLayouts, bool includeLayers, int layerLimit = 200)
    {
        double Mm(double du) => Math.Round(units.ToMm(du), 1);
        Pt P(Point3d p) => new Pt(Mm(p.X), Mm(p.Y), Mm(p.Z));

        var layoutManager = LayoutManager.Current;
        var currentLayout = Safe(() => layoutManager.CurrentLayout) ?? (db.TileMode ? "Model" : "Layout");
        var ucs = ed.CurrentUserCoordinateSystem;
        var cs = ucs.CoordinateSystem3d;

        var modelEntities = 0;
        var paperEntities = 0;
        var blockDefinitions = 0;
        var xrefs = 0;
        var layouts = new List<LayoutInfo>();
        var blockTable = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
        foreach (ObjectId id in blockTable)
        {
            ct.ThrowIfCancellationRequested();
            var btr = (BlockTableRecord)tr.GetObject(id, OpenMode.ForRead);
            if (btr.IsLayout)
            {
                var count = 0;
                foreach (ObjectId _ in btr) count++;
                var isModel = id == SymbolUtilityServices.GetBlockModelSpaceId(db);
                if (isModel) modelEntities += count;
                else paperEntities += count;
                if (includeLayouts && tr.GetObject(btr.LayoutId, OpenMode.ForRead) is Layout layout)
                    layouts.Add(new LayoutInfo(layout.LayoutName, isModel, layout.TabOrder, count, string.Equals(layout.LayoutName, currentLayout, StringComparison.OrdinalIgnoreCase)));
                continue;
            }

            if (btr.IsFromExternalReference || btr.IsFromOverlayReference) xrefs++;
            else if (!btr.IsAnonymous) blockDefinitions++;
        }

        var layerTable = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
        var layerCount = 0;
        var layers = new List<object>();
        foreach (ObjectId id in layerTable)
        {
            layerCount++;
            if (!includeLayers || layers.Count >= layerLimit) continue;
            var layer = (LayerTableRecord)tr.GetObject(id, OpenMode.ForRead);
            layers.Add(new
            {
                name = layer.Name, isOn = !layer.IsOff, isFrozen = layer.IsFrozen, isLocked = layer.IsLocked, isPlottable = layer.IsPlottable,
                color = layer.Color.IsByAci ? layer.Color.ColorIndex.ToString() : layer.Color.ColorNameForDisplay,
                linetype = Safe(() => ((LinetypeTableRecord)tr.GetObject(layer.LinetypeObjectId, OpenMode.ForRead)).Name),
                lineweight = layer.LineWeight.ToString(),
            });
        }

        object? currentView = null;
        try
        {
            using var view = ed.GetCurrentView();
            currentView = new { centerMm = new Pt(Mm(view.CenterPoint.X), Mm(view.CenterPoint.Y), 0), heightMm = Mm(view.Height), widthMm = Mm(view.Width), isPaperSpace = view.IsPaperspaceView };
        }
        catch (AcadException)
        {
            // no current view while a document is being created
        }

        var extentsValid = db.Extmin.X <= db.Extmax.X && Math.Abs(db.Extmin.X) < 1e19;
        var paperExtentsValid = db.Pextmin.X <= db.Pextmax.X && Math.Abs(db.Pextmin.X) < 1e19;

        return new
        {
            drawing = doc.Name,
            path = doc.IsNamedDrawing ? db.Filename : null,
            isNamedDrawing = doc.IsNamedDrawing,
            isReadOnly = doc.IsReadOnly,
            autocadVersion = new { release = AcadApp.Version.ToString(), acadver = Safe(() => AcadApp.GetSystemVariable("ACADVER")?.ToString()), product = Safe(() => HostApplicationServices.Current.Product) },
            units = new
            {
                label = units.Label, mmPerUnit = units.MmPerUnit, insunits = db.Insunits.ToString(), measurement = db.Measurement.ToString(),
                lunits = db.Lunits, luprec = db.Luprec, aunits = db.Aunits, note = units.Note,
            },
            activeSpace = db.TileMode ? "Model" : "Paper",
            activeLayout = currentLayout,
            currentLayer = Safe(() => ((LayerTableRecord)tr.GetObject(db.Clayer, OpenMode.ForRead)).Name),
            currentColor = db.Cecolor.IsByLayer ? "ByLayer" : db.Cecolor.IsByBlock ? "ByBlock" : db.Cecolor.ColorNameForDisplay,
            currentLinetype = Safe(() => ((LinetypeTableRecord)tr.GetObject(db.Celtype, OpenMode.ForRead)).Name),
            ltScale = db.Ltscale,
            celtScale = db.Celtscale,
            textStyle = Safe(() => ((TextStyleTableRecord)tr.GetObject(db.Textstyle, OpenMode.ForRead)).Name),
            dimStyle = Safe(() => ((DimStyleTableRecord)tr.GetObject(db.Dimstyle, OpenMode.ForRead)).Name),
            annotationScale = Safe(() => db.Cannoscale is { } scale ? new AnnotationScaleInfo(scale.Name, scale.Scale, scale.PaperUnits, scale.DrawingUnits) : null),
            ucs = new
            {
                isWorld = ucs.IsEqualTo(Matrix3d.Identity),
                originMm = P(cs.Origin),
                xAxis = new Pt(Math.Round(cs.Xaxis.X, 6), Math.Round(cs.Xaxis.Y, 6), Math.Round(cs.Xaxis.Z, 6)),
                yAxis = new Pt(Math.Round(cs.Yaxis.X, 6), Math.Round(cs.Yaxis.Y, 6), Math.Round(cs.Yaxis.Z, 6)),
            },
            // EXTMIN/EXTMAX hold ±1e20 sentinels until the drawing has extents; they refresh on regen/save.
            extentsMm = extentsValid ? new Box(P(db.Extmin), P(db.Extmax)) : (Box?)null,
            paperExtentsMm = paperExtentsValid ? new Box(P(db.Pextmin), P(db.Pextmax)) : (Box?)null,
            currentView,
            counts = new
            {
                modelSpaceEntities = modelEntities,
                paperSpaceEntities = paperEntities,
                layers = layerCount,
                blockDefinitions,
                xrefs,
                layouts = layoutManager.LayoutCount,
                textStyles = Count(tr, db.TextStyleTableId),
                dimStyles = Count(tr, db.DimStyleTableId),
                linetypes = Count(tr, db.LinetypeTableId),
            },
            layouts = includeLayouts ? layouts.OrderBy(l => l.TabOrder).ToList() : null,
            layers = includeLayers ? layers : null,
            layersTruncated = includeLayers && layerCount > layerLimit ? layerCount - layerLimit : (int?)null,
            isQuiescent = AcadApp.IsQuiescent,
            isModified = Safe(() => Convert.ToInt32(AcadApp.GetSystemVariable("DBMOD"))) > 0,
        };
    }

    public sealed record AnnotationScaleInfo(string Name, double Scale, double PaperUnits, double DrawingUnits);

    /// <summary>One layout tab: serialised camelCase like every other record.</summary>
    public sealed record LayoutInfo(string Name, bool IsModel, int TabOrder, int Entities, bool IsCurrent);

    private static int Count(Transaction tr, ObjectId tableId)
    {
        var count = 0;
        if (tr.GetObject(tableId, OpenMode.ForRead) is SymbolTable table)
            foreach (ObjectId _ in table) count++;
        return count;
    }

    private static T? Safe<T>(Func<T?> read)
    {
        try { return read(); }
        catch (AcadException) { return default; }
    }
}
