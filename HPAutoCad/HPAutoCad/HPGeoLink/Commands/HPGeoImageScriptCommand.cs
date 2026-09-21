using System.Globalization;
using System.IO;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using HPAutoCad.Core.HPGeoLink.Catalog;
using HPAutoCad.Core.HPGeoLink.Conversion;
using HPAutoCad.Core.HPGeoLink.Imagery;
using HPAutoCad.Core.HPGeoLink.Model;
using HPAutoCad.Core.HPGeoLink.Text;
using HPAutoCad.Core.HPGeoLink.Units;
using HPAutoCad.Core.HPGeoLink.Validation;
using HPAutoCad.HPGeoLink.Cad;
using HPAutoCad.HPGeoLink.Imagery;
using HPAutoCad.HPGeoLink.Support;

namespace HPAutoCad.HPGeoLink.Commands;

/// <summary>
/// <c>-HPGEOIMAGE</c> (registered by the loader): satellite imagery under a land boundary, no dialog. One prompt takes a
/// <c>key=value</c> line (<see cref="ImageArguments"/>); the closed LWPOLYLINEs it names (one handle, a layer
/// wildcard, or all of model space) give the extent, the tiles are fetched, warped onto the VN-2000 grid and
/// inserted as one RasterImage on <see cref="RasterInserter.LayerName"/>. Zone and unit follow
/// <see cref="ImageZoneResolver"/> (argument → drawing record → user settings → defaults, value by value).
/// Everything — the image, the layer, the settings record — is one undo step under the command's own marker.
/// </summary>
internal static class HPGeoImageScriptCommand
{
    private const string Name = "-HPGEOIMAGE";

    public static void Run()
    {
        var doc = Application.DocumentManager.MdiActiveDocument;
        if (doc is null) return;
        var ed = doc.Editor;

        var input = ed.GetString(new PromptStringOptions("\nHPGeo image arguments ([cm=<KTT>] [handle=<hex>|layer=<wildcard>] [res=0.3|zoom=19] [margin=30] [unit=m|mm] [out=<file.png>] [provider=esri] [k0= fe= fn=]): ") { AllowSpaces = true });
        if (input.Status != PromptStatus.OK) return;

        try
        {
            var args = ImageArguments.Parse(input.StringResult);
            var outcome = Execute(doc, args, ed);
            if (outcome is null) return;
            ImageryConsole.Report(ed, Name, outcome);
        }
        catch (ImageZoneException exception)
        {
            ImageryConsole.Refuse(ed, Name, exception.Code, exception.Message);
        }
        catch (ArgumentException exception)
        {
            ed.WriteMessage($"\nHPGeo: {exception.Message}\n");
            HPGeoLog.Warning($"-HPGEOIMAGE refused: {exception.Message}");
        }
        catch (System.Exception exception)
        {
            ed.WriteMessage($"\nHPGeo: lỗi — {exception.Message}\n");
            HPGeoLog.Error("-HPGEOIMAGE failed", exception);
        }
    }

    /// <summary>Resolves zone, unit and boundary, runs the pipeline; null when a refusal was already printed.</summary>
    private static ImageryOutcome? Execute(Document doc, ImageArguments args, Editor ed)
    {
        var ci = CultureInfo.InvariantCulture;
        var ctx = DrawingContext.Read(doc);
        var stored = DocumentSettingsStore.Read(doc.Database);
        var user = UserSettingsStore.Load();
        var zone = ImageZoneResolver.Resolve(args, stored, user, ctx.Unit); // ImageZoneException → refusal with its code
        var factor = DrawingUnitFactor.MetersPerUnit(zone.Unit)!.Value;
        var provider = ImageryProviders.Resolve(args.ProviderId);

        if (RasterInserter.CheckLayer(doc.Database) is { } layerProblem)
        {
            ImageryConsole.Refuse(ed, Name, "LAYER_LOCKED", layerProblem);
            return null;
        }
        var boundary = ReadBoundary(doc.Database, args, factor);
        if (boundary.Error is { } error)
        {
            ImageryConsole.Refuse(ed, Name, error.Code, error.Message);
            return null;
        }
        ed.WriteMessage($"\nHPGeo: ranh {boundary.Handles.Count} LWPOLYLINE kín ({string.Join(", ", boundary.Handles)}), E {boundary.Box.MinE.ToString("F3", ci)}–{boundary.Box.MaxE.ToString("F3", ci)}, N {boundary.Box.MinN.ToString("F3", ci)}–{boundary.Box.MaxN.ToString("F3", ci)} m");
        ed.WriteMessage($"\nHPGeo: KTT {CentralMeridian.Format(zone.Tm.CentralMeridianDeg)} ({zone.ZoneSource}), k0 {zone.Tm.ScaleFactor.ToString(ci)}, FE {zone.Tm.FalseEasting.ToString(ci)}, FN {zone.Tm.FalseNorthing.ToString(ci)}; đơn vị {DrawingUnitFactor.Label(zone.Unit)} ({zone.UnitSource})");

        string imagePath;
        if (args.OutputPath is { } requested)
        {
            imagePath = Path.IsPathRooted(requested) ? requested : Path.Combine(ctx.DirectoryPath ?? RasterInserter.UnsavedImageRoot, requested);
        }
        else
        {
            var (path, unsaved) = RasterInserter.ImagePathFor(ctx, $"{provider.Id}-{DateTime.Now.ToString("yyyyMMdd-HHmmss", ci)}");
            imagePath = path;
            if (unsaved) ed.WriteMessage($"\nHPGeo Warning: bản vẽ chưa lưu — ảnh ghi vào {RasterInserter.UnsavedImageRoot} (đường dẫn tuyệt đối; lưu bản vẽ rồi chạy lại nếu muốn ảnh nằm cạnh DWG).");
        }

        // margin= in metres wins; else the image covers area= (default 10) times the boundary's box — the plot in its surroundings.
        var areaRatio = args.MarginM is null ? args.AreaRatio ?? stored?.ImageryAreaRatio ?? TileCoverage.DefaultAreaRatio : (double?)null;
        var marginM = args.MarginM ?? TileCoverage.MarginForAreaRatio(boundary.Box, areaRatio!.Value);
        ed.WriteMessage($"\nHPGeo: biên {marginM.ToString("F1", ci)} m{(areaRatio is { } r ? $" (vùng ảnh ≈ ×{r.ToString("0.#", ci)} diện tích khung ranh)" : "")}");
        // No explicit res=/zoom=: the finest zoom that fits the caps is taken (a wide view gets coarser pixels, never a refusal).
        var request = new ImageryRequest(boundary.Box, zone.Tm, factor, marginM, args.ResolutionMPerPx, args.Zoom, provider, imagePath, FitToCaps: args.ResolutionMPerPx is null && args.Zoom is null);
        var outcome = ImageryPipeline.Run(doc.Database, request, message => ed.WriteMessage($"\nHPGeo: {message}"), () => HostApplicationServices.Current.UserBreak());

        if (outcome.Success)
        {
            var requestedRes = args.ResolutionMPerPx ?? (args.Zoom is null ? TileCoverage.DefaultResolutionMPerPx : outcome.Plan!.ResolutionMPerPx);
            ImageryConsole.Remember(doc, stored, user, zone.Tm, args.UnitOverride, provider.Id, requestedRes, marginM, stored?.UseCurrentCatalog ?? true, areaRatio: areaRatio);
        }
        return outcome;
    }

    private sealed record BoundaryExtent(GridBoundingBox Box, IReadOnlyList<string> Handles, ConversionIssue? Error);

    /// <summary>The closed polylines the arguments select, as one extent in VN-2000 metres.</summary>
    private static BoundaryExtent ReadBoundary(Database db, ImageArguments args, double metersPerUnit)
    {
        using var tr = db.TransactionManager.StartTransaction();
        IEnumerable<ObjectId> ids;
        if (args.Handle is { } handleText)
        {
            if (!long.TryParse(handleText, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
                return Fail("NO_BOUNDARY", $"handle='{handleText}' không phải handle hex.");
            ObjectId id;
            try { id = db.GetObjectId(false, new Handle(value), 0); }
            catch (Autodesk.AutoCAD.Runtime.Exception) { return Fail("NO_BOUNDARY", $"Không có đối tượng với handle {handleText}."); }
            if (id.IsNull || id.IsErased) return Fail("NO_BOUNDARY", $"Không có đối tượng với handle {handleText}.");
            ids = new[] { id };
        }
        else
        {
            var layer = args.LayerFilter is null ? null : new WildcardPattern(args.LayerFilter);
            ids = DrawingReader.ModelSpaceIds(tr, db, layer is null ? null : e => layer.IsMatch(e.Layer));
        }
        var read = DrawingReader.Read(tr, ids, DrawingContext.ChordToleranceFor(metersPerUnit));
        tr.Commit();

        var rings = read.Boundaries.Where(b => b.Closed && b.DrawingVertices.Count >= 3).ToList();
        if (rings.Count == 0)
        {
            var scope = args.Handle is not null ? $"handle {args.Handle}" : args.LayerFilter is not null ? $"layer {args.LayerFilter}" : "model space";
            return Fail("NO_BOUNDARY", $"Không có LWPOLYLINE kín nào ({scope}: {read.Boundaries.Count} polyline, {read.Points.Count} POINT). Vẽ/đóng ranh đất rồi chạy lại.");
        }
        var vertices = rings.SelectMany(r => r.DrawingVertices).Select(v => new PlanePoint(v.Easting * metersPerUnit, v.Northing * metersPerUnit));
        return new BoundaryExtent(GridBoundingBox.Of(vertices), rings.Select(r => r.SourceHandle ?? "?").ToList(), null);

        static BoundaryExtent Fail(string code, string message) => new(default, Array.Empty<string>(), new ConversionIssue(IssueSeverity.Error, code, message));
    }
}
