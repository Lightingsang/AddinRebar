using System.IO;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using HPAutoCad.Core.HPGeoLink.Imagery;
using HPAutoCad.HPGeoLink.Cad;
using HPAutoCad.HPGeoLink.Support;

namespace HPAutoCad.HPGeoLink.Imagery;

/// <summary>What one insertion produced: the RasterImage handle, the files on disk, the definition key.</summary>
internal sealed record RasterInsertResult(string Handle, string ImagePath, string WorldFilePath, string Layer, string DefinitionName, bool SourceRelative, bool DisplayQualityRaised);

/// <summary>
/// Puts a warped, north-up PNG into model space as a georeferenced <see cref="RasterImage"/>: a fresh image
/// definition in the drawing's image dictionary (created on demand; an existing key is never repointed — a
/// user's own attachment keeps its pixels), the entity anchored at the raster's lower-left corner with
/// U = its width and V = its height in drawing units — no rotation, because the warp already put every pixel
/// on the VN-2000 grid —, on the layer <see cref="LayerName"/> created on demand, sent to the bottom of the
/// draw order so the survey stays visible on top. One transaction, so one <c>U</c> removes all of it.
/// </summary>
internal static class RasterInserter
{
    public const string LayerName = "HPGEO-IMAGE";
    private const short LayerColorIndex = 8; // grey: a raster's frame should never compete with the survey

    public static readonly string UnsavedImageRoot =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HPGeo", "images");

    /// <summary>
    /// <c>&lt;dwg&gt;_hpgeo_&lt;label&gt;.png</c> beside the drawing; an unsaved drawing has no folder, so its images go to
    /// <see cref="UnsavedImageRoot"/> (the caller warns, because the image path then never follows the DWG).
    /// </summary>
    public static (string Path, bool UnsavedFallback) ImagePathFor(DrawingContext ctx, string label)
    {
        var name = $"{ctx.BaseName}_hpgeo_{label}.png";
        return ctx.DirectoryPath is { } dir ? (Path.Combine(dir, name), false) : (Path.Combine(UnsavedImageRoot, name), true);
    }

    public static string WorldFilePathFor(string imagePath) => Path.ChangeExtension(imagePath, ".pgw");

    /// <summary>
    /// Before any tile is downloaded: the layer must be writable. Returns the refusal message, null when fine.
    /// Read-only — opens nothing for write.
    /// </summary>
    public static string? CheckLayer(Database db)
    {
        using var tr = db.TransactionManager.StartTransaction();
        var layers = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
        if (layers.Has(LayerName) && tr.GetObject(layers[LayerName], OpenMode.ForRead) is LayerTableRecord { } existing && (existing.IsLocked || existing.IsFrozen))
            return $"Layer {LayerName} đang bị khoá/đóng băng — mở khoá rồi chạy lại.";
        return null;
    }

    /// <summary>
    /// Inserts <paramref name="imagePath"/> (already written) so that its top-left pixel corner sits at
    /// <see cref="OutputRaster.OriginE"/>/<see cref="OutputRaster.OriginN"/>, in drawing units of
    /// <paramref name="metersPerUnit"/>. The world file is written beside the PNG here too, so the pair is always consistent.
    /// </summary>
    public static RasterInsertResult Insert(Database db, string imagePath, OutputRaster raster, double metersPerUnit)
    {
        if (!(metersPerUnit > 0)) throw new ArgumentException("Hệ số đơn vị bản vẽ phải > 0.");
        var fullPath = Path.GetFullPath(imagePath);
        if (!File.Exists(fullPath)) throw new FileNotFoundException("Không tìm thấy ảnh đã ghi.", fullPath);
        var worldFile = WorldFilePathFor(fullPath);
        File.WriteAllText(worldFile, raster.WorldFile());
        var (sourceName, relative) = SourceNameFor(db, fullPath);

        using var tr = db.TransactionManager.StartTransaction();
        var layerId = EnsureLayer(tr, db);

        // The image dictionary (ACAD_IMAGE_DICT) holds one definition per attached file; the key is made unique so a
        // second run onto the same file name, or a user's own "site" attachment, is never hijacked.
        var dictId = RasterImageDef.GetImageDictionary(db);
        if (dictId.IsNull) dictId = RasterImageDef.CreateImageDictionary(db);
        var dict = (DBDictionary)tr.GetObject(dictId, OpenMode.ForWrite);
        var definitionName = RasterImageDef.SuggestName(dict, fullPath);

        var def = new RasterImageDef();
        try
        {
            def.SourceFileName = sourceName;   // what the DWG stores (relative beside the drawing)
            def.ActiveFileName = fullPath;     // where AutoCAD reads it now, before the definition is resident
            relative = Load(def, fullPath, relative);
            dict.SetAt(definitionName, def);
            tr.AddNewlyCreatedDBObject(def, true);
        }
        catch
        {
            if (def.ObjectId.IsNull) def.Dispose(); // never leave a non-resident wrapper to the finalizer thread
            throw;
        }

        var lowerLeft = raster.LowerLeft;
        var origin = new Point3d(lowerLeft.Easting / metersPerUnit, lowerLeft.Northing / metersPerUnit, 0);
        var u = new Vector3d(raster.WidthM / metersPerUnit, 0, 0);
        var v = new Vector3d(0, raster.HeightM / metersPerUnit, 0);

        var blockTable = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
        var modelSpace = (BlockTableRecord)tr.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
        var image = new RasterImage();
        try
        {
            image.ImageDefId = def.ObjectId;
            image.LayerId = layerId;
            image.Orientation = new CoordinateSystem3d(origin, u, v);
            image.ShowImage = true;
            modelSpace.AppendEntity(image);
            tr.AddNewlyCreatedDBObject(image, true);
        }
        catch
        {
            if (image.ObjectId.IsNull) image.Dispose();
            throw;
        }
        RasterImage.EnableReactors(true);
        image.AssociateRasterDef(def); // the reactor that reloads the entity when the definition changes; needs both database-resident

        var drawOrder = (DrawOrderTable)tr.GetObject(modelSpace.DrawOrderTableId, OpenMode.ForWrite);
        drawOrder.MoveToBottom(new ObjectIdCollection { image.ObjectId });
        var qualityRaised = EnsureHighDisplayQuality(tr, db);

        tr.Commit();
        return new RasterInsertResult(image.Handle.ToString(), fullPath, worldFile, LayerName, definitionName, relative, qualityRaised);
    }

    private const string RasterVariablesKey = "ACAD_IMAGE_VARS";

    /// <summary>
    /// The drawing's IMAGEQUALITY (the RasterVariables record in the named object dictionary) set to Draft shows
    /// every raster degraded — the imagery looks blurred whatever the tiles hold. Raised to High inside the insert
    /// transaction (so it is part of the same undo step); true when it was Draft. A missing record means AutoCAD's
    /// default, High, and is left alone.
    /// </summary>
    private static bool EnsureHighDisplayQuality(Transaction tr, Database db)
    {
        try
        {
            var nod = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead);
            if (!nod.Contains(RasterVariablesKey)) return false;
            if (tr.GetObject(nod.GetAt(RasterVariablesKey), OpenMode.ForRead) is not RasterVariables vars || vars.ImageQuality == ImageQuality.High) return false;
            vars.UpgradeOpen();
            vars.ImageQuality = ImageQuality.High;
            return true;
        }
        catch (System.Exception exception)
        {
            HPGeoLog.Warning("IMAGEQUALITY could not be raised to High: " + exception.Message);
            return false;
        }
    }

    /// <summary>The drawing's IMAGEQUALITY as a label for HPGEOINFO ("High (default)" when the record does not exist yet).</summary>
    public static string DisplayQualityLabel(Transaction tr, Database db)
    {
        try
        {
            var nod = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead);
            if (!nod.Contains(RasterVariablesKey)) return "High (default)";
            return tr.GetObject(nod.GetAt(RasterVariablesKey), OpenMode.ForRead) is RasterVariables vars ? vars.ImageQuality.ToString() : "?";
        }
        catch (System.Exception) { return "?"; }
    }

    /// <summary>Counts the RasterImage entities of model space on <see cref="LayerName"/> (HPGEOINFO).</summary>
    public static int CountImages(Transaction tr, Database db)
    {
        var count = 0;
        foreach (var id in DrawingReader.ModelSpaceIds(tr, db))
        {
            if (tr.GetObject(id, OpenMode.ForRead, false) is RasterImage image && string.Equals(image.Layer, LayerName, StringComparison.OrdinalIgnoreCase))
                count++;
        }
        return count;
    }

    /// <summary>
    /// A PNG beside the DWG is referenced by file name only, so the pair still resolves after the folder moves;
    /// anything else by full path. AutoCAD resolves a bare name against the drawing's folder first.
    /// </summary>
    private static (string Name, bool Relative) SourceNameFor(Database db, string fullPath)
    {
        try
        {
            var dwg = db.Filename;
            if (!string.IsNullOrWhiteSpace(dwg) && Path.IsPathRooted(dwg) && File.Exists(dwg))
            {
                var dwgDir = Path.GetFullPath(Path.GetDirectoryName(dwg)!).TrimEnd('\\');
                var imgDir = Path.GetFullPath(Path.GetDirectoryName(fullPath)!).TrimEnd('\\');
                if (string.Equals(dwgDir, imgDir, StringComparison.OrdinalIgnoreCase))
                    return (Path.GetFileName(fullPath), true);
            }
        }
        catch (System.Exception)
        {
            // an unsaved drawing's name is not a path; fall through to the full path
        }
        return (fullPath, false);
    }

    /// <summary>Loads the definition; if the relative name still does not resolve, the full path is stored instead. Returns whether the stored name stayed relative.</summary>
    private static bool Load(RasterImageDef def, string fullPath, bool relative)
    {
        try
        {
            def.Load();
            if (def.IsLoaded) return relative;
        }
        catch (Autodesk.AutoCAD.Runtime.Exception exception) when (relative)
        {
            HPGeoLog.Warning($"image definition '{def.SourceFileName}' did not load by relative name ({exception.ErrorStatus}); using the full path");
        }
        if (!relative) throw new InvalidOperationException($"AutoCAD không nạp được ảnh {fullPath}.");
        def.SourceFileName = fullPath;
        def.Load();
        if (!def.IsLoaded) throw new InvalidOperationException($"AutoCAD không nạp được ảnh {fullPath}.");
        return false;
    }

    private static ObjectId EnsureLayer(Transaction tr, Database db)
    {
        var layers = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
        if (layers.Has(LayerName))
        {
            var existing = (LayerTableRecord)tr.GetObject(layers[LayerName], OpenMode.ForRead);
            if (existing.IsLocked || existing.IsFrozen)
                throw new InvalidOperationException($"Layer {LayerName} đang bị khoá/đóng băng — mở khoá rồi chạy lại.");
            return layers[LayerName];
        }
        layers.UpgradeOpen();
        var record = new LayerTableRecord { Name = LayerName, Color = Color.FromColorIndex(ColorMethod.ByAci, LayerColorIndex) };
        var id = layers.Add(record);
        tr.AddNewlyCreatedDBObject(record, true);
        return id;
    }
}
