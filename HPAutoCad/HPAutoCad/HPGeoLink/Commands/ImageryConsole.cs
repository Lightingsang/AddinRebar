using System.Globalization;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using HPAutoCad.Core.HPGeoLink.Projection;
using HPAutoCad.Core.HPGeoLink.Settings;
using HPAutoCad.Core.HPGeoLink.Units;
using HPAutoCad.HPGeoLink.Cad;
using HPAutoCad.HPGeoLink.Imagery;
using HPAutoCad.HPGeoLink.Support;

namespace HPAutoCad.HPGeoLink.Commands;

/// <summary>
/// What both imagery commands (script and dialog) say and remember: the refusal line, the outcome report with
/// the log line the acceptance harness greps, and the settings written after a successful insert. One place, so
/// the two commands cannot drift apart in wording or in what they store.
/// </summary>
internal static class ImageryConsole
{
    public static void Refuse(Editor ed, string commandName, string code, string message)
    {
        ed.WriteMessage($"\nHPGeo Error: {message}\nHPGeo: FAILED — nothing inserted.\n");
        HPGeoLog.Warning($"{commandName} failed: {code}");
    }

    public static void Report(Editor ed, string commandName, ImageryOutcome outcome)
    {
        var ci = CultureInfo.InvariantCulture;
        foreach (var issue in outcome.Issues)
            ed.WriteMessage($"\nHPGeo {issue.Severity}: {issue.Message}");
        if (!outcome.Success)
        {
            ed.WriteMessage("\nHPGeo: FAILED — nothing inserted.\n");
            HPGeoLog.Warning($"{commandName} failed: {string.Join(" | ", outcome.Errors.Select(e => e.Code))}");
            return;
        }
        var plan = outcome.Plan!;
        var inserted = outcome.Inserted!;
        var warped = plan.Output;
        ed.WriteMessage($"\nHPGeo: OK — RasterImage {inserted.Handle} on {inserted.Layer}, {warped.WidthPx}×{warped.HeightPx} px @ {warped.PixelSizeM.ToString("F3", ci)} m/px, góc dưới-trái E {warped.LowerLeft.Easting.ToString("F3", ci)} N {warped.LowerLeft.Northing.ToString("F3", ci)}");
        if (inserted.DisplayQualityRaised)
            ed.WriteMessage("\nHPGeo: IMAGEQUALITY của bản vẽ đang là Draft → chuyển sang High (ảnh raster hiển thị đủ nét; cùng một bước Undo)");
        if (outcome.Fit is { } fit)
            ed.WriteMessage($"\nHPGeo: sai số affine max {fit.MaxResidualM.ToString("F3", ci)} m, RMS {fit.RmsResidualM.ToString("F3", ci)} m (ảnh đã nắn theo lưới khống chế)");
        ed.WriteMessage($"\nHPGeo: ảnh {inserted.ImagePath} (tham chiếu {(inserted.SourceRelative ? "tương đối" : "tuyệt đối")})\nHPGeo: world file {inserted.WorldFilePath}\nHPGeo: nguồn — {outcome.Request.Provider.Attribution}\n");
        HPGeoLog.Information($"HPGEOIMAGE fetched {outcome.TilesFetched} tiles z={plan.Zoom} ({outcome.TilesFromCache} cached, {outcome.TileSource}) stitched {plan.MosaicWidthPx}x{plan.MosaicHeightPx} warped {warped.WidthPx}x{warped.HeightPx} inserted {inserted.Handle} on {inserted.Layer} -> {inserted.ImagePath} ({(inserted.SourceRelative ? "relative" : "absolute")} source, def {inserted.DefinitionName}, affine max {outcome.Fit?.MaxResidualM.ToString("F3", ci) ?? "-"} m); attribution: {outcome.Request.Provider.Attribution}");
    }

    /// <summary>
    /// After a successful insert the drawing remembers the zone it was projected with (every value, resolved), the
    /// unit only when the user chose one, and the imagery choices the dialog offers as defaults next time; the
    /// user's settings.json remembers the same.
    /// </summary>
    public static void Remember(Document doc, GeoSettings? stored, GeoSettings? user, TmParameters tm, DrawingUnit? chosenUnit, string providerId, double resolutionMPerPx, double marginM, bool useCurrentCatalog = true, string? provinceName = null, double? areaRatio = null)
    {
        var drawing = (stored ?? new GeoSettings()) with
        {
            UseCurrentCatalog = useCurrentCatalog, ProvinceName = provinceName ?? stored?.ProvinceName,
            CentralMeridianDeg = tm.CentralMeridianDeg, ScaleFactor = tm.ScaleFactor, FalseEasting = tm.FalseEasting, FalseNorthing = tm.FalseNorthing,
            Unit = chosenUnit ?? stored?.Unit,
            ImageryProvider = providerId, ImageryResolutionMPerPx = resolutionMPerPx, ImageryMarginM = marginM, ImageryAreaRatio = areaRatio ?? stored?.ImageryAreaRatio,
        };
        DocumentSettingsStore.Write(doc.Database, drawing);
        UserSettingsStore.Save((user ?? new GeoSettings()) with
        {
            UseCurrentCatalog = useCurrentCatalog, ProvinceName = provinceName ?? user?.ProvinceName,
            CentralMeridianDeg = tm.CentralMeridianDeg, ScaleFactor = tm.ScaleFactor, FalseEasting = tm.FalseEasting, FalseNorthing = tm.FalseNorthing,
            ImageryProvider = providerId, ImageryResolutionMPerPx = resolutionMPerPx, ImageryMarginM = marginM, ImageryAreaRatio = areaRatio ?? user?.ImageryAreaRatio,
        });
    }
}
