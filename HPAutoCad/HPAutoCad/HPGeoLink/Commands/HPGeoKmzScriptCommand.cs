using System.Globalization;
using System.IO;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using HPAutoCad.Core.HPGeoLink.Catalog;
using HPAutoCad.Core.HPGeoLink.Conversion;
using HPAutoCad.Core.HPGeoLink.Kml;
using HPAutoCad.Core.HPGeoLink.Settings;
using HPAutoCad.Core.HPGeoLink.Text;
using HPAutoCad.Core.HPGeoLink.Units;
using HPAutoCad.HPGeoLink.Cad;
using HPAutoCad.HPGeoLink.Support;

namespace HPAutoCad.HPGeoLink.Commands;

/// <summary>
/// <c>-HPGEOKMZ</c> (registered by the loader): the no-dialog variant for scripts and the acceptance harness. One prompt takes a
/// <c>key=value</c> line (see <see cref="ExportArguments"/>); every POINT and LWPOLYLINE of model space
/// (optionally one layer wildcard) is converted and written to the KMZ. Prints the summary the harness
/// checks. The drawing itself is untouched except for the settings record (the zone used) in its named
/// object dictionary after a successful export — one undoable change under the command's own undo marker.
/// </summary>
internal static class HPGeoKmzScriptCommand
{
    public static void Run()
    {
        var doc = Application.DocumentManager.MdiActiveDocument;
        if (doc is null) return;
        var ed = doc.Editor;

        var prompt = new PromptStringOptions("\nHPGeo arguments (cm=<KTT> out=<file.kmz> [type=both|points|boundaries] [unit=m|mm] [layer=<wildcard>] [k0= fe= fn= name= pcolor= lcolor=]): ")
        {
            AllowSpaces = true,
        };
        var input = ed.GetString(prompt);
        if (input.Status != PromptStatus.OK) return;

        try
        {
            var args = ExportArguments.Parse(input.StringResult);
            var outcome = Export(doc, args);
            Report(ed, outcome, args);
            if (outcome.Success)
            {
                // The drawing remembers the zone it was exported with (the dialog reads it back first).
                var existing = DocumentSettingsStore.Read(doc.Database) ?? new GeoSettings();
                DocumentSettingsStore.Write(doc.Database, existing with
                {
                    CentralMeridianDeg = args.CentralMeridianDeg, ScaleFactor = args.ScaleFactor, FalseEasting = args.FalseEasting, FalseNorthing = args.FalseNorthing,
                    Unit = args.UnitOverride, Output = args.Output, PointColor = args.PointColor, LineColor = args.LineColor,
                });
            }
        }
        catch (ArgumentException exception)
        {
            ed.WriteMessage($"\nHPGeo: {exception.Message}\n");
            HPGeoLog.Warning($"-HPGEOKMZ refused: {exception.Message}");
        }
        catch (System.Exception exception)
        {
            ed.WriteMessage($"\nHPGeo: lỗi — {exception.Message}\n");
            HPGeoLog.Error("-HPGEOKMZ failed", exception);
        }
    }

    private static KmzExportOutcome Export(Document doc, ExportArguments args)
    {
        var ctx = DrawingContext.Read(doc);
        var unit = args.UnitOverride ?? ctx.Unit;
        var metersPerUnit = DrawingUnitFactor.MetersPerUnit(unit) ?? 0; // 0 → the converter reports UNKNOWN_UNIT

        DrawingReadResult read;
        using (var tr = doc.Database.TransactionManager.StartTransaction())
        {
            var layer = args.LayerFilter is null ? null : new WildcardPattern(args.LayerFilter);
            var ids = DrawingReader.ModelSpaceIds(tr, doc.Database, layer is null ? null : e => layer.IsMatch(e.Layer));
            read = DrawingReader.Read(tr, ids, DrawingContext.ChordToleranceFor(metersPerUnit));
            tr.Commit();
        }

        var conversion = new ConversionOptions(args.Tm, metersPerUnit);
        var kml = new KmlExportOptions(args.DocumentName ?? ctx.BaseName)
        {
            Output = args.Output,
            PointColor = args.PointColor,
            LineColor = args.LineColor,
        };
        var outputPath = Path.IsPathRooted(args.OutputPath) ? args.OutputPath : Path.Combine(ctx.DirectoryPath ?? Environment.CurrentDirectory, args.OutputPath);
        return KmzExportPipeline.Run(new KmzExportRequest(read.Points, read.Boundaries, conversion, kml, outputPath));
    }

    private static void Report(Editor ed, KmzExportOutcome outcome, ExportArguments args)
    {
        var ci = CultureInfo.InvariantCulture;
        var c = outcome.Conversion;
        foreach (var issue in c.Issues)
            ed.WriteMessage($"\nHPGeo {issue.Severity}: {issue.Message}");

        if (!outcome.Success)
        {
            ed.WriteMessage($"\nHPGeo: FAILED — {c.Errors.Count()} error(s), nothing written.\n");
            HPGeoLog.Warning($"-HPGEOKMZ failed: {string.Join(" | ", c.Errors.Select(e => e.Code))} (warnings: {string.Join(" | ", c.Warnings.Select(w => w.Code).Distinct())})");
            return;
        }

        var kml = outcome.Kml!;
        ed.WriteMessage($"\nHPGeo: OK — {c.Points.Count} points, {c.Boundaries.Count} boundaries → {kml.MarkerCount} markers, {kml.BoundaryCount} boundary placemark(s){(kml.BoundaryFromPoints ? " (boundary built from the point order)" : "")}; KTT {CentralMeridian.Format(args.CentralMeridianDeg)}");
        if (c.Center is { } center)
            ed.WriteMessage($"\nHPGeo: center Lat {center.LatDeg.ToString("F6", ci)}, Lon {center.LonDeg.ToString("F6", ci)}");
        ed.WriteMessage($"\nHPGeo: written {outcome.WrittenPath}\n");
        HPGeoLog.Information($"-HPGEOKMZ wrote {outcome.WrittenPath}: {c.Points.Count} points, {c.Boundaries.Count} boundaries, KTT {args.CentralMeridianDeg.ToString(ci)}");
    }
}
